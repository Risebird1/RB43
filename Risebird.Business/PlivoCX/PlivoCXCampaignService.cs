using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
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
        // The stagger between dials the legacy timer jobs use.
        private const int DialStaggerMs = 2000;

        private readonly IConfiguration             _configuration;
        private readonly IPlivoCXService            _plivoCX;
        private readonly IPlivoCXCallRepository     _callRepository;
        private readonly IPlivoCXCampaignRepository _campaignRepository;
        private readonly IPhoneNumberHelper         _phoneNumberHelper;
        private readonly IApplicationErrorLogger    _logger;

        public PlivoCXCampaignService(
            IConfiguration             configuration,
            IPlivoCXService            plivoCX,
            IPlivoCXCallRepository     callRepository,
            IPlivoCXCampaignRepository campaignRepository,
            IPhoneNumberHelper         phoneNumberHelper,
            IApplicationErrorLogger    logger)
        {
            _configuration      = configuration;
            _plivoCX            = plivoCX;
            _callRepository     = callRepository;
            _campaignRepository = campaignRepository;
            _phoneNumberHelper  = phoneNumberHelper;
            _logger             = logger;
        }

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
                    if (existing.Any(a => string.Equals(a.Purpose, seed.Purpose, StringComparison.OrdinalIgnoreCase))) continue;

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

        // ── Dialling ──────────────────────────────────────────────────────────────

        public async Task<PlivoCXDialResult> DialCandidateAsync(PlivoCXCampaign campaign, PlivoCXCampaignCandidate cc, string actor)
        {
            var agent = await _campaignRepository.GetAgentAsync(campaign.AgentId);
            if (agent == null || string.IsNullOrWhiteSpace(agent.TriggerUrl))
                return new PlivoCXDialResult { Error = "The campaign's agent has no trigger URL." };

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
            int logId = await _callRepository.SaveCallLogAsync(callLog);
            if (logId <= 0)
                return new PlivoCXDialResult { Error = "Could not save the call log." };
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
            return new PlivoCXDialResult { Success = string.IsNullOrEmpty(response?.Error), CallLogId = logId, Error = response?.Error };
        }

        public async Task<PlivoCXCampaignBatchResult> DialDueCandidatesAsync(PlivoCXCampaign campaign, string actor, bool respectWindow)
        {
            var result = new PlivoCXCampaignBatchResult();
            var nowUtc = DateTime.UtcNow;
            var tz = PlivoCXCampaignRules.ResolveTimeZone(campaign.TimeZone);

            await _campaignRepository.RetireExhaustedCandidatesAsync(campaign.Id);

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

            result.InFlight = await _campaignRepository.CountInFlightAsync(campaign.Id);
            int slots = Math.Max(0, campaign.ConcurrentCalls - result.InFlight);
            var due = slots == 0
                ? new List<PlivoCXCampaignCandidate>()
                : await _campaignRepository.GetDueCandidatesAsync(campaign.Id, slots, nowUtc, PlivoCXCampaignRules.LocalDayStartUtc(nowUtc, tz));

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
                if (i < due.Count - 1) await Task.Delay(DialStaggerMs);
            }

            result.Remaining = await _campaignRepository.CountPendingAsync(campaign.Id);
            result.Message = due.Count > 0
                ? $"Dialled {result.Dialled}" + (result.Failed > 0 ? $", {result.Failed} failed" : "") + $". {result.Remaining} still pending."
                : slots == 0
                    ? $"{result.InFlight} call(s) still connecting - the Concurrent limit ({campaign.ConcurrentCalls}) is reached."
                    : result.Remaining == 0
                        ? "Nobody is left pending."
                        : $"No pending candidate is due yet ({result.Remaining} pending - waiting out the minimum gap or today's attempt limit).";
            return result;
        }

        public async Task<PlivoCXDialResult> TestCallAsync(PlivoCXCampaign campaign, string phoneRaw, string name, string email, string actor)
        {
            var agent = await _campaignRepository.GetAgentAsync(campaign.AgentId);
            if (agent == null || string.IsNullOrWhiteSpace(agent.TriggerUrl))
                return new PlivoCXDialResult { Error = "The campaign's agent has no trigger URL." };
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

        private async Task<PlivoCXTriggerResponse> TriggerAsync(PlivoCXAgent agent, PlivoCXTriggerRequest request, int logId)
        {
            PlivoCXTriggerResponse response;
            try
            {
                response = await _plivoCX.TriggerByUrlAsync(agent.TriggerUrl, request);
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

        public async Task RunSchedulerTickAsync()
        {
            foreach (var campaign in await _campaignRepository.GetRunningCampaignsAsync())
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

                    await _campaignRepository.RetireExhaustedCandidatesAsync(campaign.Id);
                    if (await _campaignRepository.CountPendingAsync(campaign.Id) == 0)
                    {
                        // Everyone is resolved; wait for calls still connecting so their outcome lands first.
                        if (await _campaignRepository.CountInFlightAsync(campaign.Id) == 0)
                            await _campaignRepository.SetCampaignStatusAsync(campaign.Id, PlivoCXCampaignStatus.Completed);
                        continue;
                    }

                    await DialDueCandidatesAsync(campaign, "PlivoCXCampaignScheduler", respectWindow: true);
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

            var agent = await _campaignRepository.GetAgentAsync(campaign.AgentId);
            var t = PlivoCXCampaignRules.NextState(
                agent?.CollectsSlot ?? true, callLog, candidateConfirmed,
                cc.Attempts, campaign.TotalAttempts, campaign.MinGapMinutes, DateTime.UtcNow,
                cc.OriginalInterviewDate);

            await _campaignRepository.SetCampaignCandidateStateAsync(cc.Id, t.State, Truncate(t.Disposition, 30), Truncate(t.BookedSlot, 60), t.NextAttemptAt);
        }

        private static string Truncate(string s, int max) =>
            string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max);
    }
}
