using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Risebird.Entities.Data.ElevenLabsModels;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data.PlivoCXModels;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Business.PlivoCX
{
    /// <summary>
    /// The PlivoCX campaign calling engine (handoff §7, §11d-f). One dial path for every agent:
    /// IPlivoCXService.TriggerByUrlAsync(agent.TriggerUrl, request). The rest of the call - the
    /// webhook, booking, transcript and recording - is the existing PlivoCX machinery.
    /// </summary>
    public class PlivoCXCampaignService : IPlivoCXCampaignService
    {
        // How long a dial may hold a candidate before its lock expires (covers a crash mid-dial).
        private const int DialLockSeconds = 120;

        // How soon a candidate is retried after Plivo rejected the call for account capacity.
        private const int CapacityRetryMinutes = 1;

        // Throughput settings (PlivoCX config section). Plivo India Professional allows 50
        // concurrent calls account-wide (calls above it fail instantly with hangup cause 5030) and
        // 2 calls per second (excess queued). The campaign budget leaves room for the legacy timer
        // jobs, the phone screener and the interviewer agents on the same account.
        private readonly int _concurrencyBudget;   // PlivoCX:CampaignConcurrencyBudget   (default 40)
        private readonly int _dialIntervalMs;      // PlivoCX:CampaignDialIntervalMs      (default 600, about 1.6 calls/s)
        private readonly int _inFlightMinutes;     // PlivoCX:InFlightTimeoutMinutes      (default 8)

        private readonly IConfiguration             _configuration;
        private readonly IPlivoCXService            _plivoCX;
        private readonly IPlivoCXCallRepository     _callRepository;
        private readonly IPlivoCXCampaignRepository _campaignRepository;
        private readonly IPhoneNumberHelper         _phoneNumberHelper;
        private readonly IApplicationErrorLogger    _logger;
        private readonly IElevenLabsAgentService    _elevenLabs;

        // ElevenLabs runs its own concurrency limit (set by the ElevenLabs plan), separate from
        // Plivo's 50 lines: ElevenLabs:Campaigns:ConcurrencyBudget (default 10).
        private readonly int _elevenLabsBudget;

        public PlivoCXCampaignService(
            IConfiguration             configuration,
            IPlivoCXService            plivoCX,
            IPlivoCXCallRepository     callRepository,
            IPlivoCXCampaignRepository campaignRepository,
            IPhoneNumberHelper         phoneNumberHelper,
            IApplicationErrorLogger    logger,
            IElevenLabsAgentService    elevenLabs)
        {
            _configuration      = configuration;
            _plivoCX            = plivoCX;
            _callRepository     = callRepository;
            _campaignRepository = campaignRepository;
            _phoneNumberHelper  = phoneNumberHelper;
            _logger             = logger;
            _elevenLabs         = elevenLabs;
            _elevenLabsBudget   = Setting("ElevenLabs:Campaigns:ConcurrencyBudget", 10, 1, 500);

            _concurrencyBudget  = Setting("PlivoCX:CampaignConcurrencyBudget", 40, 1, 500);
            _dialIntervalMs     = Setting("PlivoCX:CampaignDialIntervalMs", 600, 0, 10000);
            // A call holds a line for its ring + conversation (the flows cap it at 5 minutes) plus the
            // hangup webhook; a call still "triggered" after this never got its webhook.
            _inFlightMinutes    = Setting("PlivoCX:InFlightTimeoutMinutes", 8, 2, 60);
        }

        public int ConcurrencyBudget      => _concurrencyBudget;

        public int ConcurrencyBudgetFor(string provider) =>
            PlivoCXCampaignProvider.Normalize(provider) == PlivoCXCampaignProvider.ElevenLabs ? _elevenLabsBudget : _concurrencyBudget;

        /// <summary>Why this agent cannot be dialled, or null. Each provider addresses its agents differently.</summary>
        private string AgentProblem(PlivoCXAgent agent)
        {
            if (agent == null) return "The campaign's agent was not found.";
            switch (PlivoCXCampaignProvider.Normalize(agent.Provider))
            {
                case PlivoCXCampaignProvider.ElevenLabs:
                    if (string.IsNullOrWhiteSpace(agent.ProviderAgentId))
                        return "The ElevenLabs agent has not been created yet - run Dashboard/CreateElevenLabsCampaignAgents once.";
                    return _elevenLabs.CampaignCallingProblem;
                case PlivoCXCampaignProvider.Plivo:
                    return string.IsNullOrWhiteSpace(agent.TriggerUrl) ? "The campaign's agent has no trigger URL." : null;
                default:
                    return $"{agent.Provider} campaigns are not available yet.";
            }
        }
        public int InFlightTimeoutMinutes => _inFlightMinutes;

        private int Setting(string key, int fallback, int min, int max) =>
            int.TryParse(_configuration[key], out var v) ? Math.Clamp(v, min, max) : fallback;

        // ── Agents ────────────────────────────────────────────────────────────────

        // (config key, name, description, purpose, target, collects slot)
        private static readonly (string Key, string Name, string Description, string Purpose, string Target, bool CollectsSlot)[] SeedAgents =
        {
            ("PlivoCX:TriggerEndpointUrl",             "Schedule",                 "collect availability & book",     "schedule",     "Candidate",   true),
            ("PlivoCX:RescheduleTriggerEndpointUrl",   "Reschedule",               "move an existing meeting",        "reschedule",   "Candidate",   true),
            ("PlivoCX:ConfirmTriggerEndpointUrl",      "Confirm attendance",       "no slot collection",              "confirm",      "Candidate",   false),
            ("PlivoCX:AvailabilityTriggerEndpointUrl", "Interviewer availability", "collect an interviewer's availability", "availability", "Interviewer", false),
            ("PlivoCX:OnboardingTriggerEndpointUrl",   "Interviewer onboarding",   "onboard a prospective interviewer", "onboard",    "Interviewer", false),
        };

        public async Task EnsureAgentsSeededAsync()
        {
            try
            {
                var existing = await _campaignRepository.GetAgentsAsync(activeOnly: false);
                foreach (var seed in SeedAgents)
                {
                    var url = _configuration[seed.Key];
                    if (string.IsNullOrWhiteSpace(url)) continue;
                    // Seeds the Plivo agents; ElevenLabs agents are created by ProvisionElevenLabsAgentsAsync.
                    if (existing.Any(a => PlivoCXCampaignProvider.Normalize(a.Provider) == PlivoCXCampaignProvider.Plivo
                                       && string.Equals(a.Purpose, seed.Purpose, StringComparison.OrdinalIgnoreCase))) continue;

                    await _campaignRepository.InsertAgentAsync(new PlivoCXAgent
                    {
                        Name         = seed.Name,
                        Description  = seed.Description,
                        TriggerUrl   = url.Trim(),
                        Purpose      = seed.Purpose,
                        TargetType   = seed.Target,
                        CollectsSlot = seed.CollectsSlot,
                        Channel      = "Voice",
                        IsActive     = true
                    });
                }
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task<List<PlivoCXAgent>> GetAgentsAsync()
        {
            await EnsureAgentsSeededAsync();
            return await _campaignRepository.GetAgentsAsync(activeOnly: true);
        }

        /// <summary>
        /// Creates the ElevenLabs Schedule, Reschedule and Confirm attendance agents (or updates
        /// them when they already exist) and records each as a PlivoCXAgent row with Provider
        /// ElevenLabs, so ElevenLabs campaigns can pick them on Create. Safe to run again: it
        /// updates the same ElevenLabs agents instead of creating duplicates.
        /// </summary>
        public async Task<List<ElevenLabsAgentProvisionResult>> ProvisionElevenLabsAgentsAsync()
        {
            var results = new List<ElevenLabsAgentProvisionResult>();
            var options = AgentOptions();
            var existing = await _campaignRepository.GetAgentsAsync(activeOnly: false);

            foreach (var d in ElevenLabsCampaignAgents.Definitions)
            {
                var result = new ElevenLabsAgentProvisionResult { Purpose = d.Purpose, Name = d.Name };
                try
                {
                    var row = existing.FirstOrDefault(a => PlivoCXCampaignProvider.Normalize(a.Provider) == PlivoCXCampaignProvider.ElevenLabs
                                                        && string.Equals(a.Purpose, d.Purpose, StringComparison.OrdinalIgnoreCase));
                    var request = ElevenLabsCampaignAgents.BuildRequest(d, options);
                    // Our row knows the agent; failing that, an agent of the same name made by an
                    // earlier run whose row was not saved. Only a genuinely new agent is created.
                    var agentId = !string.IsNullOrWhiteSpace(row?.ProviderAgentId)
                        ? row.ProviderAgentId
                        : await _elevenLabs.FindCampaignAgentIdByNameAsync(d.ElevenLabsName);
                    bool update = !string.IsNullOrWhiteSpace(agentId);
                    var response = update
                        ? await _elevenLabs.UpdateCampaignAgentAsync(agentId, request)
                        : await _elevenLabs.CreateCampaignAgentAsync(request);
                    if (!response.Success)
                    {
                        result.Error = response.Error;
                        results.Add(result);
                        continue;
                    }

                    var agent = row ?? new PlivoCXAgent();
                    agent.Name            = d.Name;
                    agent.Description     = d.Description;
                    agent.Provider        = PlivoCXCampaignProvider.ElevenLabs;
                    agent.ProviderAgentId = response.AgentId;
                    agent.TriggerUrl      = string.Empty;
                    agent.Purpose         = d.Purpose;
                    agent.TargetType      = "Candidate";
                    agent.CollectsSlot    = d.CollectsSlot;
                    agent.Channel         = "Voice";
                    agent.IsActive        = true;
                    result.AgentId = response.AgentId;
                    if (row == null)
                    {
                        agent.Id = await _campaignRepository.InsertAgentAsync(agent);
                        if (agent.Id <= 0)
                        {
                            // The ElevenLabs agent exists; running this again finds it by name and saves the row.
                            result.Error = $"ElevenLabs agent {response.AgentId} is ready, but it could not be saved in PlivoCXAgent - " +
                                           "run Risebird.DB/Tables/PlivoCXAgent.sql (Provider / ProviderAgentId columns) and open this page again.";
                            results.Add(result);
                            continue;
                        }
                    }
                    else await _campaignRepository.UpdateAgentAsync(agent);

                    result.PlivoCXAgentId = agent.Id;
                    result.Action = update ? "updated" : "created";
                }
                catch (Exception ex)
                {
                    await _logger.LogMessage(LogLevelInfo.Exception, ex);
                    result.Error = ex.Message;
                }
                results.Add(result);
            }
            return results;
        }

        /// <summary>ElevenLabs:Campaigns:Agent overrides on top of the defaults (Ananya voice, IST).</summary>
        private ElevenLabsCampaignAgentOptions AgentOptions()
        {
            var o = new ElevenLabsCampaignAgentOptions();
            string S(string key) => _configuration["ElevenLabs:Campaigns:Agent:" + key]?.Trim();
            double? D(string key) => double.TryParse(S(key), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : (double?)null;
            if (!string.IsNullOrEmpty(S("VoiceId")))    o.VoiceId    = S("VoiceId");
            if (!string.IsNullOrEmpty(S("TtsModelId"))) o.TtsModelId = S("TtsModelId");
            if (!string.IsNullOrEmpty(S("Llm")))        o.Llm        = S("Llm");
            if (!string.IsNullOrEmpty(S("Timezone")))   o.Timezone   = S("Timezone");
            if (!string.IsNullOrEmpty(S("Language")))   o.Language   = S("Language");
            o.Temperature     = D("Temperature")     ?? o.Temperature;
            o.Stability       = D("Stability")       ?? o.Stability;
            o.Speed           = D("Speed")           ?? o.Speed;
            o.SimilarityBoost = D("SimilarityBoost") ?? o.SimilarityBoost;
            o.TurnTimeout     = D("TurnTimeout")     ?? o.TurnTimeout;
            return o;
        }

        // ── Dialling ──────────────────────────────────────────────────────────────

        public async Task<PlivoCXDialResult> DialCandidateAsync(PlivoCXCampaign campaign, PlivoCXCampaignCandidate cc, string actor)
        {
            var agent = await _campaignRepository.GetAgentAsync(campaign.AgentId);
            var agentProblem = AgentProblem(agent);
            if (agentProblem != null)
                return new PlivoCXDialResult { Error = agentProblem };

            if (!_phoneNumberHelper.TrySanitizeIndianPhoneNumber(cc.CandidatePhone, out var phone, out var phoneError))
            {
                await _campaignRepository.SetCampaignCandidateStateAsync(cc.Id, PlivoCXCampaignCandidateState.Cold, "invalid-number", null, null);
                return new PlivoCXDialResult { Error = "Invalid phone number: " + phoneError };
            }

            // Eligibility guard for list-sourced rows: never ask someone to schedule an interview
            // they already have. Reschedule / confirm agents are about an existing meeting, so
            // they are not guarded.
            if (string.Equals(campaign.CallPurpose, "schedule", StringComparison.OrdinalIgnoreCase)
                && cc.IRFId.HasValue && !string.IsNullOrWhiteSpace(cc.CandidateEmail)
                && await _callRepository.HasConfirmedInterviewAsync(cc.CandidateEmail, cc.IRFId.Value))
            {
                await _campaignRepository.SetCampaignCandidateStateAsync(cc.Id, PlivoCXCampaignCandidateState.Booked, "has-meeting", "Already scheduled", null);
                return new PlivoCXDialResult { Error = "Already has a confirmed interview - marked Booked." };
            }

            // Call now, a per-row Call and the scheduler can reach the same candidate at once; only
            // the one that takes the lock dials. Once the call is logged it counts as in flight,
            // which keeps it from being dialled again, so the bump below releases the lock.
            if (!await _campaignRepository.TryClaimForDialAsync(cc.Id, DialLockSeconds))
                return new PlivoCXDialResult { Error = "Another dial is already calling this candidate." };

            var callLog = new PlivoCXCallLog
            {
                RecipientName   = cc.CandidateName,
                RecipientPhone  = phone,
                RecipientEmail  = cc.CandidateEmail,
                IRFId           = cc.IRFId,
                CandidateId     = cc.CandidateId,
                JobTitle        = cc.JobTitle,
                ClientName      = cc.ClientName,
                CallStatus      = "initiated",
                InterviewBooked = false,
                CallPurpose     = Truncate(campaign.CallPurpose, 20),
                TriggeredBy     = actor,
                CampaignId      = campaign.Id
            };
            int logId;
            try
            {
                logId = await _callRepository.SaveCallLogAsync(callLog);
            }
            catch
            {
                await _campaignRepository.ReleaseDialClaimAsync(cc.Id);
                throw;
            }
            if (logId <= 0)
            {
                await _campaignRepository.ReleaseDialClaimAsync(cc.Id);
                return new PlivoCXDialResult { Error = "Could not save the call log." };
            }
            callLog.Id = logId;
            await _campaignRepository.SetCallLogCampaignAsync(logId, campaign.Id);

            var request = BuildTriggerRequest(campaign, phone, cc.CandidateName, cc.CandidateEmail,
                cc.IRFId, cc.JobTitle, cc.ClientName, cc.OriginalInterviewDate, logId);
            var response = await TriggerAsync(agent, request, logId);

            // Counted whether or not the trigger worked, so a failing number cannot be retried forever.
            await _campaignRepository.BumpCampaignCandidateAsync(cc.Id, logId, campaign.MinGapMinutes);
            if (cc.CandidateId > 0)
                await _callRepository.IncrementCallAttemptsAsync(cc.CandidateId);

            await UpdateLogAfterTriggerAsync(callLog, response);

            // The provider turned the call away for capacity (e.g. ElevenLabs HTTP 429 - too many
            // concurrent calls): the candidate was never rung, so the attempt is given back.
            if (!string.IsNullOrEmpty(response?.Error) && PlivoCXOutcomeParser.IsCapacityRejection(response.Error))
                await _campaignRepository.RefundAttemptAsync(cc.Id, DateTime.UtcNow.AddMinutes(CapacityRetryMinutes));

            return new PlivoCXDialResult { Success = string.IsNullOrEmpty(response?.Error), CallLogId = logId, Error = response?.Error };
        }

        public async Task<PlivoCXCampaignBatchResult> DialDueCandidatesAsync(PlivoCXCampaign campaign, string actor, bool respectWindow, int? maxNewCalls = null)
        {
            var result = new PlivoCXCampaignBatchResult();
            var nowUtc = DateTime.UtcNow;
            var tz = PlivoCXCampaignRules.ResolveTimeZone(campaign.TimeZone);

            await _campaignRepository.RetireExhaustedCandidatesAsync(campaign.Id, _inFlightMinutes);

            if (respectWindow)
            {
                var why = PlivoCXCampaignRules.WhyNotDialling(campaign, nowUtc, tz);
                if (why != null)
                {
                    result.Remaining = await _campaignRepository.CountPendingAsync(campaign.Id);
                    result.Message = "Not dialling: " + why + ".";
                    return result;
                }
            }

            result.InFlight = await _campaignRepository.CountInFlightAsync(campaign.Id, _inFlightMinutes);
            int slots = Math.Max(0, campaign.ConcurrentCalls - result.InFlight);

            // Never exceed the account's lines: the scheduler passes this campaign's share of the
            // shared budget; Call now / a direct call works it out here.
            int budget = ConcurrencyBudgetFor(campaign.Provider);
            int accountFree = maxNewCalls ?? Math.Max(0, budget - await _campaignRepository.CountAllInFlightAsync(_inFlightMinutes, PlivoCXCampaignProvider.Normalize(campaign.Provider)));
            bool accountLimited = accountFree < slots;
            slots = Math.Min(slots, accountFree);

            var due = slots == 0
                ? new List<PlivoCXCampaignCandidate>()
                : await _campaignRepository.GetDueCandidatesAsync(campaign.Id, slots, nowUtc, PlivoCXCampaignRules.LocalDayStartUtc(nowUtc, tz), _inFlightMinutes);

            for (int i = 0; i < due.Count; i++)
            {
                try
                {
                    var r = await DialCandidateAsync(campaign, due[i], actor);
                    if (r.Success) result.Dialled++; else result.Failed++;
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    await _logger.LogMessage(LogLevelInfo.Exception,
                        new Exception($"PlivoCX campaign {campaign.Id}: dialling candidate {due[i].CandidateId} failed", ex));
                }
                // Paced under Plivo's 2 calls/second, which the other agents on the account share.
                if (i < due.Count - 1 && _dialIntervalMs > 0) await Task.Delay(_dialIntervalMs);
            }

            result.Remaining = await _campaignRepository.CountPendingAsync(campaign.Id);
            result.Message = due.Count > 0
                ? $"Dialled {result.Dialled}" + (result.Failed > 0 ? $", {result.Failed} failed" : "") + $". {result.Remaining} still pending."
                : slots == 0
                    ? accountLimited
                        ? $"All {budget} {PlivoCXCampaignProvider.Normalize(campaign.Provider)} campaign lines are busy - calls resume as lines free up."
                        : $"{result.InFlight} call(s) still connecting - the Concurrent limit ({campaign.ConcurrentCalls}) is reached."
                    : result.Remaining == 0
                        ? "Nobody is left pending."
                        : $"No pending candidate is due yet ({result.Remaining} pending - waiting out the minimum gap or today's attempt limit).";
            return result;
        }

        public async Task<PlivoCXDialResult> TestCallAsync(PlivoCXCampaign campaign, string phoneRaw, string name, string email, string actor)
        {
            var agent = await _campaignRepository.GetAgentAsync(campaign.AgentId);
            var agentProblem = AgentProblem(agent);
            if (agentProblem != null)
                return new PlivoCXDialResult { Error = agentProblem };
            if (!_phoneNumberHelper.TrySanitizeIndianPhoneNumber(phoneRaw, out var phone, out var phoneError))
                return new PlivoCXDialResult { Error = "Invalid phone number: " + phoneError };

            name = string.IsNullOrWhiteSpace(name) ? "Test Candidate" : name.Trim();
            var callLog = new PlivoCXCallLog
            {
                RecipientName   = name,
                RecipientPhone  = phone,
                RecipientEmail  = email,
                IRFId           = campaign.IRFId,
                CallStatus      = "initiated",
                InterviewBooked = false,
                CallPurpose     = Truncate(campaign.CallPurpose, 20),
                TriggeredBy     = Truncate("test:" + actor, 255),
                CampaignId      = campaign.Id
            };
            int logId = await _callRepository.SaveCallLogAsync(callLog);
            if (logId <= 0)
                return new PlivoCXDialResult { Error = "Could not save the call log." };
            callLog.Id = logId;
            await _campaignRepository.SetCallLogCampaignAsync(logId, campaign.Id);

            var request = BuildTriggerRequest(campaign, phone, name, email, campaign.IRFId,
                campaign.IRFName, campaign.ClientName, null, logId);
            var response = await TriggerAsync(agent, request, logId);
            await UpdateLogAfterTriggerAsync(callLog, response);
            return new PlivoCXDialResult { Success = string.IsNullOrEmpty(response?.Error), CallLogId = logId, Error = response?.Error };
        }

        private static PlivoCXTriggerRequest BuildTriggerRequest(
            PlivoCXCampaign campaign, string phone, string name, string email, int? irfId,
            string jobTitle, string clientName, string originalInterviewDate, int logId)
        {
            // The legacy jobs build the dates from DateTime.Now, i.e. IST on Risebird's servers;
            // here the campaign's time zone makes that explicit.
            var tz = PlivoCXCampaignRules.ResolveTimeZone(campaign.TimeZone);
            var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            return new PlivoCXTriggerRequest
            {
                PhoneNumber           = phone,
                CandidateName         = string.IsNullOrWhiteSpace(name) ? "Candidate" : name,
                CandidateEmail        = email,
                IrfId                 = irfId?.ToString(),
                JobTitle              = jobTitle,
                ClientName            = clientName,
                LogId                 = logId.ToString(),
                TodayDate             = today.ToString("dddd, dd MMMM yyyy"),
                TomorrowDate          = today.AddDays(1).ToString("dddd, dd MMMM yyyy"),
                DayAfterTomorrowDate  = today.AddDays(2).ToString("dddd, dd MMMM yyyy"),
                CallPurpose           = campaign.CallPurpose,
                OriginalInterviewDate = originalInterviewDate
            };
        }

        /// <summary>
        /// Places the call on ElevenLabs. The dynamic variables are exactly the fields the Plivo
        /// trigger sends (snake_case, e.g. candidate_name, log_id, tomorrow_date), so the agent
        /// prompts and the webhook read the same names on both providers; call_source marks the
        /// event as a campaign call on the shared ElevenLabs webhook. Blank values are sent as ""
        /// because ElevenLabs rejects a call whose prompt variable is missing.
        /// </summary>
        private async Task<PlivoCXTriggerResponse> TriggerElevenLabsAsync(PlivoCXAgent agent, PlivoCXTriggerRequest request)
        {
            var variables = JObject.FromObject(request).Properties()
                .ToDictionary(p => p.Name, p => (object)(p.Value.Type == JTokenType.Null ? string.Empty : p.Value.ToString()));
            variables[ElevenLabsCampaignAgents.CallSourceVariable] = ElevenLabsCampaignAgents.CallSourceValue;

            var r = await _elevenLabs.PlaceCampaignCallAsync(agent.ProviderAgentId, request.PhoneNumber, variables);
            return new PlivoCXTriggerResponse
            {
                ConversationId  = r.ConversationId,
                ApiId           = r.CallSid,
                Error           = string.IsNullOrEmpty(r.Error) && string.IsNullOrEmpty(r.ConversationId) ? "ElevenLabs returned no conversation_id." : r.Error,
                RawResponseBody = r.RawResponseBody,
                HttpStatusCode  = r.HttpStatusCode
            };
        }

        private async Task<PlivoCXTriggerResponse> TriggerAsync(PlivoCXAgent agent, PlivoCXTriggerRequest request, int logId)
        {
            PlivoCXTriggerResponse response;
            try
            {
                response = PlivoCXCampaignProvider.Normalize(agent.Provider) == PlivoCXCampaignProvider.ElevenLabs
                    ? await TriggerElevenLabsAsync(agent, request)
                    : await _plivoCX.TriggerByUrlAsync(agent.TriggerUrl, request);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                response = new PlivoCXTriggerResponse { Error = ex.Message };
            }

            // Same raw request / response capture as InitiateInterviewCall.
            try
            {
                var status = string.IsNullOrEmpty(response?.Error) ? "success" : "failed";
                await _callRepository.SaveWebhookDebugLogAsync(new PlivoCXWebhookDebugLog
                {
                    LogType = "trigger-request", HttpStatusCode = response?.HttpStatusCode, PlivoCXId = response?.ApiId,
                    CallLogId = logId, RawPayload = JsonConvert.SerializeObject(request), ProcessingStatus = status, ErrorMessage = response?.Error
                });
                await _callRepository.SaveWebhookDebugLogAsync(new PlivoCXWebhookDebugLog
                {
                    LogType = "trigger-response", HttpStatusCode = response?.HttpStatusCode, PlivoCXId = response?.ApiId,
                    CallLogId = logId, RawPayload = response?.RawResponseBody, ProcessingStatus = status, ErrorMessage = response?.Error
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return response ?? new PlivoCXTriggerResponse { Error = "No response from Plivo CX." };
        }

        private async Task UpdateLogAfterTriggerAsync(PlivoCXCallLog callLog, PlivoCXTriggerResponse response)
        {
            if (!string.IsNullOrEmpty(response?.Error))
            {
                callLog.CallStatus  = "trigger-failed";
                callLog.HangupCause = Truncate(response.Error, 255);
            }
            else
            {
                callLog.PlivoCXConversationId = response?.ConversationId;
                callLog.PlivoCXRequestId      = response?.ApiId;
                callLog.CallStatus            = "triggered";
            }
            callLog.UpdatedAt = DateTime.UtcNow;
            await _callRepository.UpdateCallLogAsync(callLog);
        }

        // ── Scheduler ─────────────────────────────────────────────────────────────

        /// <summary>
        /// One dispatcher pass (the timer runs it every 15 seconds): housekeeping for every Running
        /// campaign, then the account's free campaign lines are shared out between the campaigns
        /// that may dial now - in turn, each up to its own Concurrent limit - and dialled.
        /// </summary>
        public async Task RunSchedulerTickAsync()
        {
            var running = await _campaignRepository.GetRunningCampaignsAsync();
            if (running.Count == 0) return;

            var ready = new List<(PlivoCXCampaign Campaign, int InFlight)>();
            foreach (var campaign in running)
            {
                try
                {
                    var nowUtc = DateTime.UtcNow;
                    var tz = PlivoCXCampaignRules.ResolveTimeZone(campaign.TimeZone);

                    if (PlivoCXCampaignRules.HasEnded(campaign, nowUtc, tz))
                    {
                        await _campaignRepository.SetCampaignStatusAsync(campaign.Id, PlivoCXCampaignStatus.Completed);
                        continue;
                    }

                    await _campaignRepository.RetireExhaustedCandidatesAsync(campaign.Id, _inFlightMinutes);
                    int inFlight = await _campaignRepository.CountInFlightAsync(campaign.Id, _inFlightMinutes);
                    if (await _campaignRepository.CountPendingAsync(campaign.Id) == 0)
                    {
                        // Everyone is resolved; wait for calls still connecting so their outcome lands first.
                        if (inFlight == 0)
                            await _campaignRepository.SetCampaignStatusAsync(campaign.Id, PlivoCXCampaignStatus.Completed);
                        continue;
                    }

                    if (PlivoCXCampaignRules.WhyNotDialling(campaign, nowUtc, tz) == null)
                        ready.Add((campaign, inFlight));
                }
                catch (Exception ex)
                {
                    await _logger.LogMessage(LogLevelInfo.Exception,
                        new Exception($"PlivoCX campaign scheduler failed for campaign {campaign.Id}", ex));
                }
            }
            if (ready.Count == 0) return;

            // Each provider has its own line budget (Plivo's account limit, ElevenLabs' plan limit),
            // shared in turn between that provider's ready campaigns.
            var plan = new Dictionary<int, int>();
            foreach (var group in ready.GroupBy(r => PlivoCXCampaignProvider.Normalize(r.Campaign.Provider) ?? PlivoCXCampaignProvider.Plivo))
            {
                int free = Math.Max(0, ConcurrencyBudgetFor(group.Key) - await _campaignRepository.CountAllInFlightAsync(_inFlightMinutes, group.Key));
                foreach (var kv in PlivoCXCampaignRules.PlanSlots(
                             group.Select(r => (r.Campaign.Id, r.Campaign.ConcurrentCalls, r.InFlight)).ToList(), free))
                    plan[kv.Key] = kv.Value;
            }

            foreach (var (campaign, _) in ready)
            {
                if (!plan.TryGetValue(campaign.Id, out var lines) || lines <= 0) continue;
                try
                {
                    await DialDueCandidatesAsync(campaign, "PlivoCXCampaignScheduler", respectWindow: true, maxNewCalls: lines);
                }
                catch (Exception ex)
                {
                    await _logger.LogMessage(LogLevelInfo.Exception,
                        new Exception($"PlivoCX campaign scheduler failed for campaign {campaign.Id}", ex));
                }
            }
        }

        // ── Webhook hooks ─────────────────────────────────────────────────────────

        public async Task<bool> ShouldBookAsync(PlivoCXCallLog callLog)
        {
            if (callLog?.CampaignId == null) return true;
            var campaign = await _campaignRepository.GetCampaignAsync(callLog.CampaignId.Value);
            if (campaign == null) return true;
            var agent = await _campaignRepository.GetAgentAsync(campaign.AgentId);
            return agent?.CollectsSlot ?? true;
        }

        public async Task ApplyCallOutcomeAsync(PlivoCXCallLog callLog, bool candidateConfirmed)
        {
            if (callLog?.CampaignId == null || !callLog.CandidateId.HasValue) return;

            var campaign = await _campaignRepository.GetCampaignAsync(callLog.CampaignId.Value);
            if (campaign == null) return;
            var cc = await _campaignRepository.GetCampaignCandidateAsync(campaign.Id, callLog.CandidateId.Value);
            if (cc == null) return;

            // A webhook for an older call must not overwrite the outcome of a newer one.
            if (cc.LastCallLogId.HasValue && cc.LastCallLogId.Value != callLog.Id) return;
            // A hand-marked or already-resolved candidate keeps its state.
            if (cc.State != PlivoCXCampaignCandidateState.Pending) return;

            // Plivo turned the call away for account capacity (concurrency 5030 / CPS 5180): the
            // candidate was never rung, so the attempt is given back and they are retried shortly.
            if (PlivoCXOutcomeParser.IsCapacityRejection(callLog.HangupCause))
            {
                await _campaignRepository.RefundAttemptAsync(cc.Id, DateTime.UtcNow.AddMinutes(CapacityRetryMinutes));
                return;
            }

            var agent = await _campaignRepository.GetAgentAsync(campaign.AgentId);
            var t = PlivoCXCampaignRules.NextState(
                agent?.CollectsSlot ?? true, callLog, candidateConfirmed,
                cc.Attempts, campaign.TotalAttempts, campaign.MinGapMinutes, DateTime.UtcNow,
                cc.OriginalInterviewDate);

            await _campaignRepository.SetCampaignCandidateStateAsync(cc.Id, t.State, Truncate(t.Disposition, 30), Truncate(t.BookedSlot, 60), t.NextAttemptAt);

            // A confirm-only agent got a yes: record it on the meeting the way the candidate's
            // confirmation link does, so the meeting leaves the Unconfirmed list. A "no" is left
            // for Ops - the call never cancels a meeting.
            if (agent != null && !agent.CollectsSlot
                && t.State == PlivoCXCampaignCandidateState.Booked
                && cc.InterviewId.HasValue && !string.IsNullOrWhiteSpace(cc.CandidateEmail))
            {
                await _campaignRepository.MarkMeetingConfirmedByCandidateAsync(cc.InterviewId.Value, cc.CandidateEmail);
            }
        }

        private static string Truncate(string s, int max) =>
            string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max);
    }
}
