using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.PlivoCXModels;
using Risebird.Entities.ViewModel;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Risebird.AzureTimerFunctions
{
    /// <summary>
    /// Automated prospect onboarding pipeline — runs every 5 minutes.
    /// Processes up to 2 eligible prospects per invocation, advancing each through:
    ///   1. Parse Resume (extract email/phone, duplicate check)
    ///   2. Select MAG & generate application link
    ///   3. Send invitation email (with registration link and response options)
    ///   4. Trigger Plivo CX recruitment call — only if no email response within 1 hour
    ///
    /// If the prospect responds to the email (positively or negatively), no call is needed.
    /// Call retry policy: max 2 per day, max 5 total.
    /// After 5 failed attempts the prospect is marked Cold.
    /// </summary>
    public class ProspectOnboardingAutomationJob
    {
        private readonly IConfiguration              _configuration;
        private readonly INaukriOnboardingRepository  _repository;
        private readonly IPdfReaderHelper             _pdfReaderHelper;
        private readonly IAzureStorageHelper          _azureStorageHelper;
        private readonly IPlivoCXService              _plivoCXService;
        private readonly INotificationHelper          _notificationHelper;
        private readonly IEncryptionHelper            _encryptionHelper;
        private readonly IApplicationErrorLogger      _errorLogger;
        // Auto-continue (positive intent → EV → AI fitment → onboarding → acknowledgement) dependencies
        private readonly IAadhaarService              _aadhaarService;
        private readonly IAssistantServiceHelper      _assistantServiceHelper;
        private readonly IInterviewerService          _interviewerService;
        private readonly IHttpRequestHelper           _httpRequestHelper;
        private readonly ICallPreferenceRepository    _callPreferenceRepository;
        private readonly IWhatsAppHelper              _whatsAppHelper;
        private readonly string                       _evApiKey;
        private readonly string                       _evEndpoint;
        private readonly ILogger                      _log;

        private const int MaxCallsPerDay  = 2;
        private const int MaxCallsTotal   = 5;

        public ProspectOnboardingAutomationJob(
            ILoggerFactory              loggerFactory,
            IConfiguration              configuration,
            INaukriOnboardingRepository  repository,
            IPdfReaderHelper            pdfReaderHelper,
            IAzureStorageHelper         azureStorageHelper,
            IPlivoCXService             plivoCXService,
            INotificationHelper         notificationHelper,
            IEncryptionHelper           encryptionHelper,
            IApplicationErrorLogger     errorLogger,
            IAadhaarService             aadhaarService,
            IAssistantServiceHelper     assistantServiceHelper,
            IInterviewerService         interviewerService,
            IHttpRequestHelper          httpRequestHelper,
            IWhatsAppHelper             whatsAppHelper,
            ICallPreferenceRepository   callPreferenceRepository)
        {
            _callPreferenceRepository = callPreferenceRepository;
            _configuration     = configuration;
            _repository        = repository;
            _pdfReaderHelper   = pdfReaderHelper;
            _azureStorageHelper = azureStorageHelper;
            _plivoCXService    = plivoCXService;
            _notificationHelper = notificationHelper;
            _encryptionHelper  = encryptionHelper;
            _errorLogger       = errorLogger;
            _aadhaarService    = aadhaarService;
            _assistantServiceHelper = assistantServiceHelper;
            _interviewerService = interviewerService;
            _httpRequestHelper = httpRequestHelper;
            _whatsAppHelper    = whatsAppHelper;
            _evApiKey         = _configuration["EmploymentVerfication:apikey"];
            _evEndpoint        = _configuration["EmploymentVerfication:endPoint"];
            _log = loggerFactory.CreateLogger<ProspectOnboardingAutomationJob>();
        }

        [Function("ProspectOnboardingAutomationJob")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
        {
            _log.LogInformation($"[OnboardAuto] Triggered at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            try
            {
                // Business-hours (9 AM – 9 PM IST) applies ONLY to prospect-facing communication steps —
                // the invitation email, the recruitment call, and the onboarding/acknowledgement-email
                // finalization. Those are guarded individually below via IsWithinBusinessHours().
                // Non-communication steps (resume parse, MAG resolution, employment verification, AI
                // résumé fitment) run around the clock, so the job itself is NOT skipped off-hours.
                var istHour = DateTime.UtcNow.AddHours(5).AddMinutes(30).Hour;
                _log.LogInformation($"[OnboardAuto] IST hour {istHour}:xx — prospect communications {(IsWithinBusinessHours() ? "ENABLED" : "DEFERRED (off-hours)")}.");

                var prospects = await _repository.GetEligibleProspectsForAutomationAsync(2);
                if (prospects == null || !prospects.Any())
                {
                    _log.LogInformation("[OnboardAuto] No eligible prospects.");
                    return;
                }

                _log.LogInformation($"[OnboardAuto] Processing {prospects.Count} prospect(s).");

                foreach (var p in prospects)
                {
                    try
                    {
                        await ProcessProspectAsync(p);
                    }
                    catch (Exception ex)
                    {
                        _log.LogError($"[OnboardAuto] Unhandled error for #{p.Id} ({p.CandidateName}): {ex.Message}");
                        await _errorLogger.LogMessage(LogLevelInfo.Exception,
                            new Exception($"OnboardAuto failed for prospect #{p.Id}", ex));
                        await _repository.UpdateAutomationStatusAsync(p.Id, "Error");
                    }

                    await Task.Delay(2000);
                }

                _log.LogInformation("[OnboardAuto] Batch done.");
            }
            catch (Exception ex)
            {
                _log.LogError($"[OnboardAuto] Job-level exception: {ex.Message}");
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        private static readonly TimeSpan EmailToCallGracePeriod = TimeSpan.FromHours(1);

        private async Task ProcessProspectAsync(Entities.ViewModel.NaukriProspectiveInterviewerVM p)
        {
            _log.LogInformation($"[OnboardAuto] #{p.Id} '{p.CandidateName}' — resume={p.ResumeParseCompleted}, email={p.InvitationEmailSent}, call={p.PlivoCallStatus}, response={p.ProspectResponse}");

            // ── Step 1: Parse Resume ──────────────────────────────────────────────
            if (p.ResumeParseCompleted != true)
            {
                if (!string.IsNullOrWhiteSpace(p.ParsedEmail) || !string.IsNullOrWhiteSpace(p.ParsedPhone))
                {
                    _log.LogInformation($"[OnboardAuto] #{p.Id} — email/phone pre-populated by Relay, marking parse complete.");
                    await _repository.UpdateResumeParseResultAsync(p.Id, p.ParsedEmail, p.ParsedPhone);
                    p.ResumeParseCompleted = true;
                    var refreshed0 = await _repository.GetByIdAsync(p.Id);
                    if (refreshed0 != null)
                    {
                        p.IsExistingInterviewer = refreshed0.IsExistingInterviewer;
                        p.ResolvedAssessmentGroupId = refreshed0.ResolvedAssessmentGroupId;
                    }
                }
                else
                {
                    await _repository.UpdateAutomationStatusAsync(p.Id, "ParsingResume");
                    var parsed = await ParseResumeAsync(p);
                    if (!parsed)
                    {
                        await _repository.UpdateAutomationStatusAsync(p.Id, "ManualRequired");
                        await _repository.InsertHistoryAsync(p.Id, "AutomationNote", "—",
                            "Automation: resume parse failed — manual intervention needed.", "AutomationJob");
                        return;
                    }

                    var refreshed = await _repository.GetByIdAsync(p.Id);
                    if (refreshed == null) return;

                    p.ParsedEmail = refreshed.ParsedEmail;
                    p.ParsedPhone = refreshed.ParsedPhone;
                    p.IsExistingInterviewer = refreshed.IsExistingInterviewer;
                    p.ResumeParseCompleted = refreshed.ResumeParseCompleted;
                    p.ResolvedAssessmentGroupId = refreshed.ResolvedAssessmentGroupId;
                }
            }
            else
            {
                _log.LogInformation($"[OnboardAuto] #{p.Id} — resume already parsed, skipping Step 1.");
            }

            // ── Funnel: EmailParsed -> PhoneParsed ────────────────────────────────
            // Only advance when this genuinely moves the prospect forward — the advance SP
            // unconditionally re-stamps TicketStatus/UpdatedAt, so a blind call each pass would
            // churn the row (and re-open status) for prospects already deeper in the funnel.
            if (p.ResumeParseCompleted == true && !string.IsNullOrWhiteSpace(p.ParsedEmail)
                && !IsTerminalFunnelStage(p.FunnelStage))
            {
                string parseTarget = string.IsNullOrWhiteSpace(p.ParsedPhone) ? "EmailParsed" : "PhoneParsed";
                if (FunnelRank(parseTarget) > FunnelRank(p.FunnelStage))
                    await _repository.AdvanceFunnelStageAsync(p.Id, parseTarget, "InProgress");
            }

            // ── Step 1 (gate): Duplicate check — the FIRST funnel stage ───────────
            // An email can exist only once in UserList (unique constraint), as ANY profile
            // (interviewer, candidate, etc.). If the prospect's email already exists in the
            // system they cannot be onboarded again, so they exit the funnel at 'Duplicate'.
            // The operator still reviews these (AutomationStatus = ManualRequired).
            if (p.IsExistingInterviewer == true)
            {
                _log.LogInformation($"[OnboardAuto] #{p.Id} — duplicate email, exits funnel at 'Duplicate'.");
                await _repository.SetFunnelOutcomeAsync(p.Id, "Duplicate", "InProgress");
                await _repository.UpdateAutomationStatusAsync(p.Id, "ManualRequired");
                await _repository.InsertHistoryAsync(p.Id, "DuplicateDetected", "—",
                    $"Automation: email ({p.ParsedEmail}) already exists in the system — exits at Duplicate. Operator review required.", "AutomationJob");
                return;
            }

            // ── Step 1.5: Detect self-registration via the MAG link ───────────────
            // If the prospect completed the MAG application form themselves, an active
            // UserList entry now exists for their email. This must be reflected in the
            // funnel as 'Onboarded' (mirrors NaukriOnboardingController.CheckSelfRegistration,
            // which previously only ran when an operator opened the record). Detecting it
            // here also prevents the pipeline from calling them again or auto-creating a
            // duplicate profile.
            //   • OnboardedUserId == null  → the system has NOT auto-created a profile, so a
            //     user found by email means the prospect registered themselves.
            if (await TryReflectSelfRegistrationAsync(p))
                return;

            // Passed the duplicate screen → advance to 'DuplicateChecked' (only when moving forward).
            if (p.ResumeParseCompleted == true && !string.IsNullOrWhiteSpace(p.ParsedEmail)
                && !IsTerminalFunnelStage(p.FunnelStage)
                && FunnelRank("DuplicateChecked") > FunnelRank(p.FunnelStage))
            {
                await _repository.AdvanceFunnelStageAsync(p.Id, "DuplicateChecked", "InProgress");
            }

            // ── Step 2: Resolve MAG & generate application link ───────────────────
            if (string.IsNullOrWhiteSpace(p.ApplicationLink))
            {
                await _repository.UpdateAutomationStatusAsync(p.Id, "ResolvingMAG");
                var magResolved = await ResolveMAGAndGenerateLinkAsync(p);
                if (!magResolved)
                {
                    await _repository.UpdateAutomationStatusAsync(p.Id, "ManualRequired");
                    await _repository.InsertHistoryAsync(p.Id, "AutomationNote", "—",
                        "Automation: could not resolve MAG — manual intervention needed.", "AutomationJob");
                    return;
                }

                var refreshed = await _repository.GetByIdAsync(p.Id);
                if (refreshed == null) return;
                p.ApplicationLink = refreshed.ApplicationLink;
                p.ResolvedAssessmentGroupId = refreshed.ResolvedAssessmentGroupId;

                await _repository.AdvanceFunnelStageAsync(p.Id, "MAGConfirmed", "InProgress");
            }

            // ── Step 3: Send Invitation Email ─────────────────────────────────────
            if (p.InvitationEmailSent != true)
            {
                var latest = await _repository.GetByIdAsync(p.Id);
                if (latest != null)
                {
                    p.ParsedEmail = latest.ParsedEmail;
                    p.ApplicationLink = latest.ApplicationLink;
                    p.MagName = latest.MagName;
                    p.CandidateName = latest.CandidateName;
                }

                if (string.IsNullOrWhiteSpace(p.ApplicationLink))
                {
                    _log.LogWarning($"[OnboardAuto] #{p.Id} — no application link available. Cannot send email.");
                    await _repository.UpdateAutomationStatusAsync(p.Id, "ManualRequired");
                    await _repository.InsertHistoryAsync(p.Id, "AutomationNote", "—",
                        "Automation: no MAG application link available.", "AutomationJob");
                    return;
                }

                // Communication step — do not email a prospect outside business hours (9 AM–9 PM IST).
                if (!IsWithinBusinessHours())
                {
                    _log.LogInformation($"[OnboardAuto] #{p.Id} — outside business hours. Deferring invitation email until 9 AM–9 PM IST.");
                    return;
                }

                await _repository.UpdateAutomationStatusAsync(p.Id, "SendingEmail");
                var emailOk = await SendInvitationEmailAsync(p);
                if (emailOk)
                {
                    await _repository.UpdateAutomationStatusAsync(p.Id, "EmailSent");
                    await _repository.AdvanceFunnelStageAsync(p.Id, "InterestCollection", "InProgress");
                    await _repository.InsertHistoryAsync(p.Id, "AutomationNote", "—",
                        $"Automation: invitation email sent to {p.ParsedEmail}. Call will be deferred for {EmailToCallGracePeriod.TotalMinutes:0} min to allow prospect to respond.", "AutomationJob");
                }
                else
                {
                    await _repository.UpdateAutomationStatusAsync(p.Id, "ManualRequired");
                    await _repository.InsertHistoryAsync(p.Id, "AutomationNote", "—",
                        "Automation: email send failed — manual intervention needed.", "AutomationJob");
                }
                return;
            }

            // ── Step 3.5: Positive intent → auto-continue to acknowledgement ───────
            // Once the prospect's intent is confirmed as 'Interested' (via email reply or AI Plivo
            // call) and they have NOT self-registered via the MAG link, push them automatically
            // through EV → AI fitment → onboarding → acknowledgement email. Onboarding only truly
            // COMPLETES when the interviewer accepts the acknowledgement email (handled in the web app).
            if (string.Equals(p.Intent, "Interested", StringComparison.OrdinalIgnoreCase)
                && p.SelfRegistrationCompleted != true
                && p.OnboardingCompleted != true)
            {
                await AutoContinueToAcknowledgementAsync(p);
                return;
            }

            // ── Step 4: Recruitment Call (only after email grace period) ───────────
            // If the prospect already responded to the email, the call is unnecessary.
            if (HasProspectRespondedToEmail(p))
            {
                _log.LogInformation($"[OnboardAuto] #{p.Id} — prospect responded to email (response={p.ProspectResponse}, selfReg={p.SelfRegistrationCompleted}). Skipping call.");
                return;
            }

            // Invited, no positive intent yet, no response yet, and not yet cold → 'Awaiting Response'.
            // Only fire while the prospect is genuinely still waiting: never bump a call that already
            // concluded (handled below) or a prospect that already landed on a terminal outcome
            // (Duplicate/NotInterested/Unsubscribe/Cold/Onboarded) back into the active funnel — those
            // stages rank 0, so the forward-only guard alone would happily re-advance them.
            bool callConcluded = string.Equals(p.PlivoCallStatus, "completed", StringComparison.OrdinalIgnoreCase)
                              || string.Equals(p.PlivoCallStatus, "answered", StringComparison.OrdinalIgnoreCase);
            if (!callConcluded
                && !IsTerminalFunnelStage(p.FunnelStage)
                && !string.Equals(p.FunnelStage, "InterestCollection", StringComparison.OrdinalIgnoreCase))
            {
                await _repository.AdvanceFunnelStageAsync(p.Id, "InterestCollection", "InProgress");
            }

            // Enforce grace period: wait at least 1 hour after email was sent
            if (p.InvitationEmailSentAt.HasValue)
            {
                var elapsed = DateTime.UtcNow - p.InvitationEmailSentAt.Value;
                if (elapsed < EmailToCallGracePeriod)
                {
                    var remaining = EmailToCallGracePeriod - elapsed;
                    _log.LogInformation($"[OnboardAuto] #{p.Id} — email sent {elapsed.TotalMinutes:0} min ago. Waiting {remaining.TotalMinutes:0} more min before calling.");
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(p.ParsedPhone))
            {
                _log.LogInformation($"[OnboardAuto] #{p.Id} — no phone number parsed. Needs manual intervention.");
                await _repository.UpdateAutomationStatusAsync(p.Id, "ManualRequired");
                await _repository.InsertHistoryAsync(p.Id, "AutomationNote", "—",
                    "Automation: no phone number found in resume — operator must add manually.", "AutomationJob");
                return;
            }

            // If call completed — check disposition
            if (string.Equals(p.PlivoCallStatus, "completed", StringComparison.OrdinalIgnoreCase)
                || string.Equals(p.PlivoCallStatus, "answered", StringComparison.OrdinalIgnoreCase))
            {
                var disp = p.PlivoProspectDisposition ?? "unknown";
                if (string.Equals(disp, "interested", StringComparison.OrdinalIgnoreCase))
                {
                    // Defensive: normally the Plivo hangup webhook stamps Intent='Interested', which is
                    // caught earlier at Step 3.5. If the webhook didn't sync the unified intent, treat a
                    // completed 'interested' call as positive and auto-continue to the acknowledgement.
                    _log.LogInformation($"[OnboardAuto] #{p.Id} — call completed with 'interested' disposition. Auto-continuing to acknowledgement.");
                    if (!string.Equals(p.Intent, "Interested", StringComparison.OrdinalIgnoreCase))
                    {
                        await _repository.SetUnifiedIntentAsync(p.Id, "Interested", "Call", DateTime.UtcNow);
                        p.Intent = "Interested";
                    }
                    await AutoContinueToAcknowledgementAsync(p);
                }
                else
                {
                    _log.LogInformation($"[OnboardAuto] #{p.Id} — call completed with disposition '{disp}'.");

                    bool isUnsubscribe   = string.Equals(disp, "unsubscribe", StringComparison.OrdinalIgnoreCase);
                    bool isNotInterested = string.Equals(disp, "not_interested", StringComparison.OrdinalIgnoreCase);

                    if (isUnsubscribe || isNotInterested)
                    {
                        // Explicit negative intent on the call → terminal funnel outcome + ticket Closed.
                        // Drop/Unsubscribe folds into 'NotInterested' for the funnel, but the unified
                        // Intent keeps 'Unsubscribe' so the profile-deactivation logic still fires.
                        string intentVal = isUnsubscribe ? "Unsubscribe" : "NotInterested";
                        await _repository.SetUnifiedIntentAsync(p.Id, intentVal, "Call", DateTime.UtcNow);
                        await _repository.SetFunnelOutcomeAsync(p.Id, "NotInterested", "Closed");
                        await _repository.UpdateAutomationStatusAsync(p.Id, "Completed");
                        await _repository.InsertHistoryAsync(p.Id, "IntentConfirmed", "—",
                            $"Automation: call completed — disposition '{disp}'. Funnel → Not Interested.", "AutomationJob");
                    }
                    else
                    {
                        // Ambiguous/other disposition → needs a human to look at it.
                        await _repository.UpdateAutomationStatusAsync(p.Id, "ManualRequired");
                        await _repository.InsertHistoryAsync(p.Id, "AutomationNote", "—",
                            $"Automation: call completed — disposition: {disp}. Marked ManualRequired.", "AutomationJob");
                    }
                }
                return;
            }

            // Call not yet completed — check if we should (re)trigger
            bool callInProgress = string.Equals(p.PlivoCallStatus, "initiated", StringComparison.OrdinalIgnoreCase)
                               || string.Equals(p.PlivoCallStatus, "triggered", StringComparison.OrdinalIgnoreCase);
            if (callInProgress)
            {
                _log.LogInformation($"[OnboardAuto] #{p.Id} — call in progress ({p.PlivoCallStatus}). Waiting.");
                return;
            }

            int totalAttempts = p.PlivoCallAttemptsTotal;
            int todayAttempts = p.PlivoCallAttemptsToday;

            if (totalAttempts >= MaxCallsTotal)
            {
                // Outreach exhausted (1 invitation email + up to 5 calls) with no response → terminal 'Cold'.
                _log.LogInformation($"[OnboardAuto] #{p.Id} — max total attempts ({MaxCallsTotal}) reached. Marking Cold.");
                await _repository.UpdateAutomationStatusAsync(p.Id, "Cold");
                await _repository.SetFunnelOutcomeAsync(p.Id, "Cold", "Closed");
                await _repository.InsertHistoryAsync(p.Id, "MarkedCold", "—",
                    $"Automation: {totalAttempts} call attempt(s) + invitation email exhausted with no response. Funnel → Cold.", "AutomationJob");
                return;
            }

            if (todayAttempts >= MaxCallsPerDay)
            {
                _log.LogInformation($"[OnboardAuto] #{p.Id} — daily limit ({MaxCallsPerDay}) reached. Will retry tomorrow.");
                await _repository.UpdateAutomationStatusAsync(p.Id, "AwaitingRetry");
                return;
            }

            if (!IsWithinCallWindow())
            {
                _log.LogInformation($"[OnboardAuto] #{p.Id} — outside 9 AM–9 PM IST. Deferring call.");
                return;
            }

            // Pre-dial check: the number belongs to an interviewer who opted out of calls. Not a failed call, so no
            // attempt is used up; hand it to an operator instead of retrying every run.
            var canCall = await _callPreferenceRepository.CanCallContact("Interviewer", p.ParsedPhone);
            if (!canCall.CanCall)
            {
                _log.LogInformation($"[OnboardAuto] #{p.Id} — not called: {canCall.Reason}");
                await _repository.UpdateAutomationStatusAsync(p.Id, "ManualRequired");
                await _repository.InsertHistoryAsync(p.Id, "AutomationNote", "—",
                    $"Automation: not called — {canCall.Reason} ({p.ParsedPhone}). Operator review required.", "AutomationJob");
                return;
            }

            var callOk = await TriggerCallAsync(p);
            if (callOk)
            {
                await _repository.UpdateAutomationStatusAsync(p.Id, "CallTriggered", incrementCallAttempt: true);
                await _repository.InsertHistoryAsync(p.Id, "AutomationNote", "—",
                    $"Automation: call #{totalAttempts + 1} triggered to {p.ParsedPhone} (no email response after {EmailToCallGracePeriod.TotalMinutes:0} min).", "AutomationJob");
            }
            else
            {
                await _repository.UpdateAutomationStatusAsync(p.Id, "CallFailed", incrementCallAttempt: true);
                await _repository.InsertHistoryAsync(p.Id, "AutomationNote", "—",
                    "Automation: call trigger failed.", "AutomationJob");
            }
        }

        /// <summary>
        /// Returns true if the prospect has taken any action in response to the invitation
        /// email — self-registration, clicking a response link, or completing onboarding.
        /// When true, the Plivo call is unnecessary.
        /// </summary>
        /// <summary>
        /// Detects whether the prospect has self-registered via the MAG application link and,
        /// if so, reflects it in the funnel (stage → 'Onboarded', ticket → 'Closed') and stops
        /// further automation. Returns true when self-registration was detected and finalized.
        ///
        /// A prospect counts as "self-registered via the MAG link" ONLY when their email maps to an
        /// active interviewer who is genuinely mapped to the CONFIRMED assessment group (the durable
        /// trace a real MAG application leaves: an AG-scoped skill channel and/or the AG id on the
        /// profile). Matching the email against any active UserList row (the old behaviour) produced
        /// false positives — pre-existing interviewers in other MAGs, or non-interviewer accounts —
        /// which linked the prospect to the wrong account and marked it Onboarded under the wrong role.
        /// </summary>
        private async Task<bool> TryReflectSelfRegistrationAsync(NaukriProspectiveInterviewerVM p)
        {
            if (p.SelfRegistrationCompleted == true
                || p.OnboardingCompleted == true
                || p.OnboardedUserId.HasValue
                || string.IsNullOrWhiteSpace(p.ParsedEmail))
                return false;

            // The confirmed/locked AG (also the AG the MAG link was built from). Without it we cannot
            // verify the self-registration actually landed on THIS MAG, so we do not finalize.
            int agId = (p.AssessmentGroupId.HasValue && p.AssessmentGroupId.Value > 0)
                ? p.AssessmentGroupId.Value
                : (p.ResolvedAssessmentGroupId ?? 0);
            if (agId <= 0)
                return false;

            var (userId, profileId) = await _repository.FindSelfRegisteredInterviewerForMagAsync(p.ParsedEmail, agId);
            if (!userId.HasValue || userId.Value <= 0)
                return false;

            await _repository.UpdateSelfRegistrationAsync(p.Id, true, userId.Value, profileId);
            await _repository.SetOnboardingTypeAsync(p.Id, "SelfOnboarded");
            await _repository.AdvanceFunnelStageAsync(p.Id, "Onboarded", "Closed");
            await _repository.UpdateAutomationStatusAsync(p.Id, "Completed");
            await _repository.InsertHistoryAsync(p.Id, "SelfRegistered", "—",
                $"Prospect self-registered via MAG link (UserId: {userId.Value}, AG: {agId}). Funnel advanced to Onboarded.", "AutomationJob");

            _log.LogInformation($"[OnboardAuto] #{p.Id} — detected self-registration (UserId {userId.Value}, AG {agId}). Marked Onboarded.");
            return true;
        }

        private static bool HasProspectRespondedToEmail(Entities.ViewModel.NaukriProspectiveInterviewerVM p)
        {
            if (p.SelfRegistrationCompleted == true)
                return true;

            if (string.IsNullOrWhiteSpace(p.ProspectResponse))
                return false;

            return true;
        }

        // Terminal funnel exits — once a prospect lands on one of these the automation must never
        // pull them back into the active pipeline (e.g. bumping them to 'AwaitingResponse').
        private static readonly string[] TerminalFunnelStages =
            { "Duplicate", "NotInterested", "Unsubscribe", "Cold", "NotQualified", "Onboarded" };

        private static bool IsTerminalFunnelStage(string funnelStage)
            => !string.IsNullOrWhiteSpace(funnelStage)
               && TerminalFunnelStages.Contains(funnelStage, StringComparer.OrdinalIgnoreCase);

        // Ordinal rank of the linear happy-path stages — mirrors dbo.fn_NaukriFunnelStageRank.
        // Terminal exits and New/blank rank 0. Used to keep forward-only advances from re-stamping
        // the row (and TicketStatus) on every pass for prospects already deeper in the funnel.
        private static readonly string[] FunnelOrder =
        {
            "EmailParsed", "PhoneParsed", "DuplicateChecked", "MAGConfirmed", "InterestCollection",
            "IntentConfirmed", "EmploymentVerified", "ResumeFitment", "AcknowledgementSent", "Onboarded"
        };

        private static int FunnelRank(string stage)
        {
            if (string.IsNullOrWhiteSpace(stage)) return 0;
            int idx = Array.FindIndex(FunnelOrder, s => string.Equals(s, stage, StringComparison.OrdinalIgnoreCase));
            return idx < 0 ? 0 : idx + 1;
        }

        // ── Auto-continue: positive intent → EV → AI fitment → onboarding → acknowledgement ──

        /// <summary>
        /// When a prospect's intent is confirmed as 'Interested' (via email reply or AI Plivo call)
        /// and they have NOT self-registered, automatically advances them through the remaining
        /// pipeline: Employment Verification → AI Resume Fitment → onboarding (profile + AG/channel
        /// mapping) → acknowledgement email.
        ///
        /// STRICT GATES — auto-onboarding only proceeds when ALL of the following hold; otherwise the
        /// prospect is routed to manual review ('ManualRequired'):
        ///   • the prospect is NOT a duplicate/existing interviewer,
        ///   • Employment Verification PASSES (a non-blacklisted company is found),
        ///   • the AI resume fitment verdict is POSITIVE,
        ///   • the resume is readable.
        ///
        /// Onboarding only truly COMPLETES once the interviewer ACCEPTS the acknowledgement email
        /// (handled in InterviewerController.InterviewerAcknowledgementResponse). This method's
        /// terminal success state is 'AwaitingAcknowledgement'.
        /// This method is idempotent and resumable: EV / AI / onboarding are skipped if already done.
        /// </summary>
        private async Task AutoContinueToAcknowledgementAsync(NaukriProspectiveInterviewerVM p)
        {
            // Always operate on the freshest copy — earlier steps in this run (and other touchpoints)
            // may have updated EV / AI / onboarding flags.
            var rec = await _repository.GetByIdAsync(p.Id);
            if (rec == null) return;

            if (rec.OnboardingCompleted == true)
            {
                _log.LogInformation($"[OnboardAuto] #{p.Id} — already onboarded. Nothing to auto-continue.");
                return;
            }

            // Gate (duplicate / existing user): never auto-create a profile for an existing interviewer.
            if (rec.IsExistingInterviewer == true)
            {
                await MarkManualAsync(rec.Id, $"Auto-onboard halted: duplicate/existing interviewer ({rec.ParsedEmail}). Operator must review.");
                return;
            }

            if (string.IsNullOrWhiteSpace(rec.ParsedEmail))
            {
                await MarkManualAsync(rec.Id, "Auto-onboard halted: no parsed email available.");
                return;
            }

            // Record the positive 'IntentConfirmed' funnel stage before progressing the pipeline
            // (forward-only; a no-op if already past it).
            await _repository.AdvanceFunnelStageAsync(rec.Id, "IntentConfirmed", "InProgress");

            // An assessment group is required to onboard. Prefer the CONFIRMED/locked AG stored on the
            // record (AssessmentGroupId — also the AG the MAG application link was built from) over the
            // one re-derived from the MAG NAME text, so the interviewer is mapped to the same role that
            // was confirmed (avoids the "onboarded for X but mapped under Y" mismatch). Fall back to the
            // name-resolved AG, then to a fresh MAG resolution.
            int? agId = (rec.AssessmentGroupId.HasValue && rec.AssessmentGroupId.Value > 0)
                ? rec.AssessmentGroupId
                : rec.ResolvedAssessmentGroupId;
            if (agId == null || agId <= 0)
            {
                var resolved = await ResolveMAGAndGenerateLinkAsync(rec);
                if (resolved)
                {
                    var refreshed = await _repository.GetByIdAsync(rec.Id);
                    if (refreshed != null)
                    {
                        rec = refreshed;
                        agId = (rec.AssessmentGroupId.HasValue && rec.AssessmentGroupId.Value > 0)
                            ? rec.AssessmentGroupId
                            : rec.ResolvedAssessmentGroupId;
                    }
                }
                if (agId == null || agId <= 0)
                {
                    await MarkManualAsync(rec.Id, $"Auto-onboard halted: no assessment group resolved for MAG '{rec.MagName}'.");
                    return;
                }
            }

            _log.LogInformation($"[OnboardAuto] #{p.Id} — positive intent confirmed (source={rec.IntentSource}). Auto-continuing EV → AI → onboard → ack.");
            await _repository.UpdateAutomationStatusAsync(rec.Id, "AutoOnboarding");

            // ── Gate 1: Employment Verification (must PASS) ──────────────────────────
            // Signzy is the PRIMARY employment-verification check. If Signzy does not pass (for any
            // reason — no phone, not configured, API error, blacklisted company, exit marked, or no
            // records), we FALL BACK to the LinkedIn background verdict sent by the Relay agent: when a
            // LinkedIn profile link is present with confidence >= 80, we accept the prospect as employed.
            // (Surepass is not used for EV at this time.)
            if (rec.EmploymentVerified != true)
            {
                var ev = await RunEmploymentVerificationAsync(rec.ParsedEmail, rec.ParsedPhone);
                bool passed        = ev.passed;
                string companyName = ev.companyName;
                bool isEmployed    = ev.isEmployed;
                string message     = ev.message;
                string historyPrefix = "Auto-EV (Signzy)";

                // Fallback: LinkedIn, only when Signzy did not pass.
                if (!passed && LinkedInFallbackPasses(rec))
                {
                    passed        = true;
                    isEmployed    = true;
                    companyName   = companyName ?? ExtractCompanyFromRole(rec.CurrentRole);
                    message       = $"Signzy did not pass ({ev.message}); accepted via LinkedIn fallback (confidence {rec.LinkedInConfidence}). Profile: {rec.LinkedInUrl}";
                    historyPrefix = "Auto-EV (LinkedIn fallback)";
                }

                await _repository.UpdateEmploymentVerificationAsync(rec.Id, passed, companyName, isEmployed, message);
                await _repository.InsertHistoryAsync(rec.Id, "EmploymentVerification", "—",
                    $"{historyPrefix} — Company: {companyName ?? "N/A"}, Employed: {(isEmployed ? "Yes" : "No")} — {(passed ? "PASSED" : "FAILED")}. {message}", "AutomationJob");

                if (!passed)
                {
                    // Employment Verification failed → terminal 'Not Qualified' funnel outcome (ticket Closed).
                    await _repository.SetFunnelOutcomeAsync(rec.Id, "NotQualified", "Closed");
                    await _repository.UpdateAutomationStatusAsync(rec.Id, "Completed");
                    await _repository.InsertHistoryAsync(rec.Id, "NotQualified", "—",
                        $"Automation: Employment Verification did not pass ({message}). Funnel → Not Qualified.", "AutomationJob");
                    _log.LogInformation($"[OnboardAuto] #{rec.Id} — EV failed ({message}). Funnel → Not Qualified.");
                    return;
                }

                await _repository.AdvanceFunnelStageAsync(rec.Id, "EmploymentVerified", "InProgress");
                rec.EmploymentVerified = true;
                rec.EmploymentCompanyName = companyName;
                rec.EmploymentIsEmployed = isEmployed;
            }

            // ── Gates 2 & 3: AI Resume Fitment (verdict must be POSITIVE; resume must be readable) ──
            bool aiPositive;
            if (rec.ResumeAIAnalysisCompleted == true)
            {
                aiPositive = !string.IsNullOrEmpty(rec.AIVerdict) && rec.AIVerdict.ToLower().Contains("yes");
            }
            else
            {
                var ai = await RunResumeFitmentAsync(rec);
                if (!ai.readable)
                {
                    await MarkManualAsync(rec.Id, "Auto-onboard halted: resume could not be read for AI fitment. Operator must upload a valid resume.");
                    return;
                }
                if (!ai.analysed)
                {
                    await MarkManualAsync(rec.Id, "Auto-onboard halted: AI fitment analysis failed. Operator must review.");
                    return;
                }

                await _repository.UpdateAIAnalysisAsync(rec.Id, true, ai.analysisJson, ai.verdict, ai.score, ai.positive ? "Verified" : "InProgress");
                await _repository.InsertHistoryAsync(rec.Id, "ResumeAnalysis", "—",
                    $"Auto-AI — Verdict: {ai.verdict ?? "N/A"}, Score: {ai.score?.ToString() ?? "N/A"}", "AutomationJob");
                await _repository.AdvanceFunnelStageAsync(rec.Id, "ResumeFitment", "InProgress");
                aiPositive = ai.positive;
            }

            if (!aiPositive)
            {
                await MarkManualAsync(rec.Id, "Auto-onboard halted: AI resume fitment verdict is not positive. Operator must review.");
                return;
            }

            // ── Onboard + acknowledgement email ──────────────────────────────────────
            // This finalization sends the acknowledgement email to the prospect, so it is a
            // communication step gated to business hours (9 AM–9 PM IST). Employment Verification and
            // AI fitment above have already run and been persisted, so on the next in-hours run this
            // method resumes here (those gates are skipped) and proceeds to onboard.
            if (!IsWithinBusinessHours())
            {
                _log.LogInformation($"[OnboardAuto] #{rec.Id} — EV & AI fitment complete; deferring onboarding + acknowledgement email until business hours (9 AM–9 PM IST).");
                await _repository.InsertHistoryAsync(rec.Id, "AutomationNote", "—",
                    "Automation: EV & AI fitment passed; onboarding + acknowledgement email deferred until business hours (9 AM–9 PM IST).", "AutomationJob");
                return;
            }

            await AutoOnboardInterviewerAsync(rec, agId.Value);
        }

        /// <summary>
        /// Creates the interviewer profile (mapping to the resolved AG + active channels) and sends the
        /// MANDATORY acknowledgement email. Onboarding is only marked complete once the interviewer
        /// accepts that email, so the terminal state here is 'AwaitingAcknowledgement' (ack sent) or
        /// 'ManualRequired' (profile created but ack email failed / profile creation failed).
        /// </summary>
        private async Task AutoOnboardInterviewerAsync(NaukriProspectiveInterviewerVM rec, int agId)
        {
            string candidateName = (rec.CandidateName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(candidateName)) candidateName = "Interviewer";
            string[] nameParts = candidateName.Split(new[] { ' ' }, 2);
            string firstName = nameParts[0];
            string lastName = nameParts.Length > 1 ? nameParts[1] : "";
            if (firstName.Length < 2) firstName = firstName.PadRight(2, 'x');

            decimal experience = 0;
            if (!string.IsNullOrWhiteSpace(rec.ExperienceYears))
            {
                var numMatch = Regex.Match(rec.ExperienceYears, @"(\d+)");
                if (numMatch.Success) decimal.TryParse(numMatch.Value, out experience);
            }

            string phone = rec.ParsedPhone ?? "";
            if (phone.Length > 0 && phone.Length < 10) phone = phone.PadRight(10, '0');

            var portalBase = _configuration["ApplicationUrl"] ?? "https://portal.risebird.io";

            // Guard: if the prospect's email already maps to an existing interviewer, do NOT auto-create
            // a profile here. usp_AddOrEditInterviewerProfile's UPDATE path REPLACES the profile's
            // AssessmentGroupId list, which would wipe their other specialisations and can surface them
            // under a pre-existing role instead of the confirmed MAG. These cases need a human decision,
            // so route to manual — the operator's "adopt existing" onboarding path appends the confirmed
            // MAG (never overwrites) and gates activation on the acknowledgement.
            var existingIv = await _repository.FindInterviewerByEmailAnyStatusAsync(rec.ParsedEmail);
            if (existingIv.UserId.HasValue && existingIv.UserId.Value > 0)
            {
                await MarkManualAsync(rec.Id,
                    $"Auto-onboard halted: email {rec.ParsedEmail} already maps to an existing interviewer account (UserId {existingIv.UserId.Value}). Onboard manually so the confirmed MAG (AG {agId}) is added without overwriting their existing role.");
                return;
            }

            var profileVM = new InterviewerProfileVM
            {
                EmailId = rec.ParsedEmail.Trim().ToLower(),
                FirstName = firstName,
                LastName = lastName,
                MobileNumber = phone,
                Country = "26",
                City = rec.Location ?? "",
                Experience = experience,
                CurrentRole = rec.CurrentRole ?? "",
                ResumeLink = rec.ResumeLink ?? "",
                Gender = "Male",
                AssessmentGroupId = agId.ToString(),
                AssessmentGroupArray = new[] { agId.ToString() },
                PreviousAssessmentGroupArray = new string[0],
                AssessmentGroupDifference = agId.ToString(),
                RemovedAssessmentGroups = "",
                PrimarySpecializationForAcknowledgement = rec.AssessmentGroupName ?? rec.MagName ?? "",
                IsActive = true,
                Isavailable = 1,
                CompanyName = rec.EmploymentCompanyName ?? "",
                IsEmployed = rec.EmploymentIsEmployed == true,
                ResourceUrl = $"{portalBase}/NaukriOnboarding/Index"
            };

            bool addResult;
            string assessmentGroupList = null;
            int userId = 0;
            string uniqueid = null;
            try
            {
                addResult = _interviewerService.AddInterviewerProfile(
                    profileVM, "AutomationJob",
                    out assessmentGroupList, out userId, out uniqueid);
            }
            catch (Exception svcEx)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, svcEx);
                await MarkManualAsync(rec.Id, $"Auto-onboard halted: failed to create interviewer profile ({svcEx.Message}).");
                return;
            }

            if (!addResult || userId <= 0)
            {
                string detail = !string.IsNullOrEmpty(assessmentGroupList)
                    ? $"AG capacity issue: {assessmentGroupList}"
                    : (_interviewerService.LastAddProfileException?.Message ?? "Unknown reason");
                await MarkManualAsync(rec.Id, $"Auto-onboard halted: profile creation failed. Reason: {detail} [Email: {profileVM.EmailId}, AG: {agId}].");
                return;
            }

            int? profileId = await _repository.GetInterviewerProfileIdByUserIdAsync(userId);

            bool ackSent = false;
            try
            {
                string yesButton  = $"{portalBase}/Interviewer/InterviewerAcknowledgementResponse?status=1&callConsent=1&user={userId}";
                string dropButton = $"{portalBase}/Interviewer/InterviewerAcknowledgementResponse?status=4&user={userId}";
                string agNames    = rec.AssessmentGroupName ?? rec.MagName ?? "";

                await _notificationHelper.SendAcknowledgementEmailToInterviewer(
                    rec.ParsedEmail, rec.CandidateName ?? firstName,
                    yesButton, dropButton, agNames,
                    rec.AssessmentGroupName ?? rec.MagName ?? "",
                    uniqueid ?? "");

                ackSent = true;
            }
            catch (Exception ackEx)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ackEx);
            }

            await _repository.UpdateOnboardingResultAsync(rec.Id, userId, profileId, agId, ackSent);
            await _repository.SetOnboardingTypeAsync(rec.Id, "IntentAutoProcessed");

            if (ackSent)
            {
                // Mandatory acknowledgement stage. Onboarding completes only when the interviewer
                // accepts the email (status=1) — handled in InterviewerAcknowledgementResponse.
                await _repository.AdvanceFunnelStageAsync(rec.Id, "AcknowledgementSent", "InProgress");
                await _repository.UpdateAutomationStatusAsync(rec.Id, "AwaitingAcknowledgement");
                await _repository.InsertHistoryAsync(rec.Id, "AcknowledgementSent", "—",
                    $"Automation: profile created (UserId {userId}, AG {agId}, Type IntentAutoProcessed) and acknowledgement email sent. Awaiting interviewer's acceptance to complete onboarding.", "AutomationJob");
                _log.LogInformation($"[OnboardAuto] #{rec.Id} — auto-onboarded UserId {userId}; acknowledgement email sent. Awaiting acceptance.");
            }
            else
            {
                await _repository.AdvanceFunnelStageAsync(rec.Id, "ResumeFitment", "InProgress");
                await _repository.UpdateAutomationStatusAsync(rec.Id, "ManualRequired");
                await _repository.InsertHistoryAsync(rec.Id, "AcknowledgementPending", "—",
                    $"Automation: profile created (UserId {userId}, AG {agId}) but acknowledgement email FAILED. Operator must use Resend.", "AutomationJob");
                _log.LogWarning($"[OnboardAuto] #{rec.Id} — auto-onboarded UserId {userId} but acknowledgement email FAILED. Marked ManualRequired.");
            }
        }

        // Minimum LinkedIn confidence to accept a prospect as employed via the fallback path.
        private const int LinkedInFallbackMinConfidence = 80;

        /// <summary>
        /// True when the LinkedIn background verdict is strong enough to accept the prospect as employed
        /// as a FALLBACK to Signzy: a LinkedIn profile link must be present (not empty / "None") and the
        /// confidence must be >= 80. Used only after Signzy fails to pass.
        /// </summary>
        private static bool LinkedInFallbackPasses(Entities.ViewModel.NaukriProspectiveInterviewerVM rec)
            => rec != null
               && !string.IsNullOrWhiteSpace(rec.LinkedInUrl)
               && !string.Equals(rec.LinkedInUrl.Trim(), "None", StringComparison.OrdinalIgnoreCase)
               && rec.LinkedInConfidence.HasValue
               && rec.LinkedInConfidence.Value >= LinkedInFallbackMinConfidence;

        /// <summary>
        /// Best-effort extraction of the employer name from a "designation at company" style current-role
        /// string (e.g. "Development Team Lead at RSS Softech / gistec" → "RSS Softech / gistec"). Falls
        /// back to the whole role string when no " at " delimiter is present.
        /// </summary>
        private static string ExtractCompanyFromRole(string currentRole)
        {
            if (string.IsNullOrWhiteSpace(currentRole)) return null;
            int idx = currentRole.LastIndexOf(" at ", StringComparison.OrdinalIgnoreCase);
            string company = idx >= 0 ? currentRole.Substring(idx + 4) : currentRole;
            company = company?.Trim();
            return string.IsNullOrWhiteSpace(company) ? null : company;
        }

        /// <summary>
        /// Performs Employment Verification server-side: first reuses any cached result for the mobile
        /// number, otherwise calls Signzy. EV PASSES only when a non-blacklisted employer/company is found
        /// AND the prospect is currently employed there. Every other outcome — verification could not run
        /// (no phone / not configured / API error / no records), blacklisted company, or isEmployed = false
        /// (exit marked) — returns passed = false, which routes the prospect to the terminal 'Not Qualified'
        /// funnel stage.
        /// </summary>
        private async Task<(bool passed, string companyName, bool isEmployed, string message)>
            RunEmploymentVerificationAsync(string email, string mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile))
                return (false, null, false, "No phone number available for employment verification.");

            // Signzy's EV API rejects numbers carrying a country code (e.g. "+91"), so strip it
            // up front — this also keeps the cache lookup consistent with the bare number.
            mobile = Risebird.CommonHelper.PhoneNumberHelper.StripCountryCode(mobile);

            try
            {
                // 1) Reuse a cached EV result if present for this number.
                var cached = await _aadhaarService.GetEmployementDetailsByMobileNumber(mobile);
                if (cached != null && (!string.IsNullOrWhiteSpace(cached.CompanyName) || cached.isVerified))
                {
                    string company = cached.CompanyName;
                    bool blacklisted = !string.IsNullOrWhiteSpace(company)
                                       && (await _aadhaarService.CheckCompanyStatus(company)) != null;
                    bool hasEmployer = !string.IsNullOrWhiteSpace(company) && !blacklisted;
                    // EV qualifies ONLY when a non-blacklisted employer is found AND the prospect is
                    // currently employed there. Otherwise → Not Qualified.
                    bool passed = hasEmployer && cached.Isemployed;
                    string msg = blacklisted     ? "Company is blacklisted in our system."
                               : !hasEmployer    ? (cached.message ?? "No employment records found.")
                               : !cached.Isemployed ? "Prospect is not currently employed (exit marked)."
                               :                    "Verified (cached employment record).";
                    return (passed, company, cached.Isemployed, msg);
                }

                // 2) No cached record — call Signzy.
                if (string.IsNullOrWhiteSpace(_evEndpoint) || string.IsNullOrWhiteSpace(_evApiKey))
                    return (false, null, false, "Employment verification is not configured.");

                var mobiledata = new SignzyEmployeeRequest { mobileNumber = mobile, CandidateEmail = email, panNumber = "" };
                var headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "Authorization", _evApiKey }
                };

                var response = await _httpRequestHelper.MakeHttpRequest(_evEndpoint, Method.Post, mobiledata, headers);
                var resultset = JsonConvert.DeserializeObject<SignzyEVResponseResult>(response.Content);
                var conv = resultset?.result?.result;

                if (conv == null)
                {
                    string err = resultset?.error?.message ?? resultset?.message ?? "No employment records found.";
                    return (false, null, false, err);
                }

                string companyName = conv.summary?.recentEmployerData?.establishmentName;
                bool isEmployed = conv.summary != null && !conv.summary.dateOfExitMarked;

                if (string.IsNullOrWhiteSpace(companyName))
                    return (false, companyName, isEmployed, "No employment records found.");

                bool isBlacklisted = (await _aadhaarService.CheckCompanyStatus(companyName)) != null;
                if (isBlacklisted)
                    return (false, companyName, isEmployed, "Company is blacklisted in our system.");

                // A company was found, but if the prospect has exited (not currently employed) → Not Qualified.
                if (!isEmployed)
                    return (false, companyName, isEmployed, "Prospect is not currently employed (exit marked).");

                return (true, companyName, isEmployed, "Verified via Signzy.");
            }
            catch (Exception ex)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return (false, null, false, $"Employment verification error: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads the resume text and runs the OpenAI fitment analysis for the prospect's specialization.
        /// Returns whether the resume was readable, whether analysis succeeded, the verdict/score, and
        /// whether the verdict is positive (verdict contains "yes" — includes "conditional yes").
        /// </summary>
        private async Task<(bool analysed, bool positive, string verdict, int? score, string analysisJson, bool readable)>
            RunResumeFitmentAsync(NaukriProspectiveInterviewerVM rec)
        {
            string resumeUrl = rec.ResumeLink;
            if (string.IsNullOrWhiteSpace(resumeUrl))
                return (false, false, null, null, null, false);

            string resumeText = null;
            try { resumeText = await _pdfReaderHelper.ReadPdfFromUrlAsync(resumeUrl); }
            catch { }

            if (string.IsNullOrWhiteSpace(resumeText))
            {
                try
                {
                    string converted = await _azureStorageHelper.DownloadAndConvertFromDocToPdfAsync(resumeUrl);
                    if (!string.IsNullOrWhiteSpace(converted))
                        resumeText = await _pdfReaderHelper.ReadPdfFromUrlAsync(converted);
                }
                catch { }
            }

            if (string.IsNullOrWhiteSpace(resumeText))
                return (false, false, null, null, null, false);

            try
            {
                string specialization = rec.AssessmentGroupName ?? rec.MagName ?? rec.KeySkills ?? "General";
                var aiResponse = await _assistantServiceHelper.GenerateResumeAnalysis(resumeText, specialization);
                var analysisJson = aiResponse?.Data?.First()?.Content?.FirstOrDefault()?.Text?.Value;
                if (string.IsNullOrWhiteSpace(analysisJson))
                    return (false, false, null, null, null, true);

                var analysis = JsonConvert.DeserializeObject<InterviewerResumeFitmentAnalysisSchema>(analysisJson);
                bool positive = analysis != null
                    && !string.IsNullOrEmpty(analysis.verdict)
                    && analysis.verdict.ToLower().Contains("yes");
                return (true, positive, analysis?.verdict, analysis?.fitmentScore, analysisJson, true);
            }
            catch (Exception ex)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return (false, false, null, null, null, true);
            }
        }

        private async Task MarkManualAsync(int id, string note)
        {
            await _repository.UpdateAutomationStatusAsync(id, "ManualRequired");
            await _repository.InsertHistoryAsync(id, "AutomationNote", "—", note, "AutomationJob");
            _log.LogInformation($"[OnboardAuto] #{id} — {note}");
        }

        // ── Pipeline step implementations ─────────────────────────────────────────

        private async Task<bool> ParseResumeAsync(Entities.ViewModel.NaukriProspectiveInterviewerVM p)
        {
            try
            {
                string resumeUrl = p.ResumeLink;
                if (string.IsNullOrWhiteSpace(resumeUrl)) return false;

                string resumeText = null;
                bool isDoc = resumeUrl.EndsWith(".doc", StringComparison.OrdinalIgnoreCase)
                          || resumeUrl.EndsWith(".docx", StringComparison.OrdinalIgnoreCase);

                if (!isDoc)
                {
                    try { resumeText = await _pdfReaderHelper.ReadPdfFromUrlAsync(resumeUrl); }
                    catch { }
                }

                if (string.IsNullOrWhiteSpace(resumeText))
                {
                    try
                    {
                        string converted = await _azureStorageHelper.DownloadAndConvertFromDocToPdfAsync(resumeUrl);
                        if (!string.IsNullOrWhiteSpace(converted))
                            resumeText = await _pdfReaderHelper.ReadPdfFromUrlAsync(converted);
                    }
                    catch { }
                }

                if (string.IsNullOrWhiteSpace(resumeText) && isDoc)
                {
                    try { resumeText = await _pdfReaderHelper.ReadPdfFromUrlAsync(resumeUrl); }
                    catch { }
                }

                if (string.IsNullOrWhiteSpace(resumeText))
                {
                    _log.LogWarning($"[OnboardAuto] #{p.Id} — could not read resume text.");
                    return false;
                }

                string email = ExtractEmail(resumeText);
                string phone = ExtractPhone(resumeText);

                await _repository.UpdateResumeParseResultAsync(p.Id, email, phone);
                await _repository.InsertHistoryAsync(p.Id, "ResumeParse", "—",
                    $"Email: {email ?? "N/A"}, Phone: {phone ?? "N/A"}", "AutomationJob");

                _log.LogInformation($"[OnboardAuto] #{p.Id} — resume parsed: email={email}, phone={phone}");
                return true;
            }
            catch (Exception ex)
            {
                _log.LogError($"[OnboardAuto] #{p.Id} — parse error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Resolves the MAG for this prospect (by exact match or subject-line fuzzy match)
        /// and generates a MAG-level application link (not IRF-specific).
        /// </summary>
        private async Task<bool> ResolveMAGAndGenerateLinkAsync(Entities.ViewModel.NaukriProspectiveInterviewerVM p)
        {
            try
            {
                int? agId = p.ResolvedAssessmentGroupId;

                if ((agId == null || agId == 0) && !string.IsNullOrWhiteSpace(p.Subject))
                {
                    _log.LogInformation($"[OnboardAuto] #{p.Id} — exact MAG match failed for '{p.MagName}'. Trying subject-line match.");

                    var allMags = await _repository.GetAllMasterAssessmentGroupsAsync();
                    if (allMags != null && allMags.Count > 0)
                    {
                        string subjectLower = p.Subject.ToLower();

                        MagDropdownItem bestMatch = null;
                        int bestLen = 0;
                        foreach (var mag in allMags)
                        {
                            if (string.IsNullOrWhiteSpace(mag.MagName)) continue;
                            string magLower = mag.MagName.Trim().ToLower();
                            if (magLower.Length > bestLen && subjectLower.Contains(magLower))
                            {
                                bestMatch = mag;
                                bestLen = magLower.Length;
                            }
                        }

                        if (bestMatch != null && bestMatch.AssessmentGroupId > 0)
                        {
                            agId = bestMatch.AssessmentGroupId;
                            _log.LogInformation($"[OnboardAuto] #{p.Id} — subject-line matched MAG '{bestMatch.MagName}' → AG #{agId}.");
                            await _repository.UpdateMagNameAsync(p.Id, bestMatch.MagName);
                        }
                    }
                }

                if (agId == null || agId == 0)
                {
                    _log.LogWarning($"[OnboardAuto] #{p.Id} — no assessment group resolved for MAG '{p.MagName}' or subject '{p.Subject}'.");
                    return false;
                }

                var encryptedAgId = await _encryptionHelper.EncryptStringValue(agId.Value.ToString());
                var portalBase = _configuration["ApplicationUrl"] ?? "https://portal.risebird.io";
                string appLink = $"{portalBase}/Interviewer/MagApplicationForm?id={Uri.EscapeDataString(encryptedAgId)}";

                await _repository.UpdateSelectedIRFAsync(p.Id, 0, null, appLink, agId.Value);
                await _repository.InsertHistoryAsync(p.Id, "MAGLinkGenerated", "—",
                    $"MAG link generated for AG #{agId}. Link: {appLink}", "AutomationJob");

                _log.LogInformation($"[OnboardAuto] #{p.Id} — MAG link generated for AG #{agId}.");
                return true;
            }
            catch (Exception ex)
            {
                _log.LogError($"[OnboardAuto] #{p.Id} — MAG resolve error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Business hours for all prospect-facing communication (emails &amp; calls): 9 AM–9 PM IST.
        /// Non-communication pipeline steps ignore this window.
        /// </summary>
        private static bool IsWithinBusinessHours()
        {
            var ist = DateTime.UtcNow.AddHours(5).AddMinutes(30);
            return ist.Hour >= 9 && ist.Hour < 21;
        }

        private static bool IsWithinCallWindow() => IsWithinBusinessHours();

        private async Task<bool> TriggerCallAsync(Entities.ViewModel.NaukriProspectiveInterviewerVM p)
        {
            try
            {
                string phone = p.ParsedPhone;
                if (string.IsNullOrWhiteSpace(phone))
                {
                    _log.LogWarning($"[OnboardAuto] #{p.Id} — no phone number.");
                    return false;
                }

                if (phone.Length == 10 && !phone.StartsWith("+"))
                    phone = "+91" + phone;
                else if (phone.Length > 10 && !phone.StartsWith("+"))
                    phone = "+" + phone;

                await _repository.UpdatePlivoCallStatusAsync(p.Id, "initiated", null,
                    DateTime.UtcNow, null, null, null, null, null, null, "AutomationJob");

                var callerId = _configuration["PlivoCX:OnboardingCallerId"]
                            ?? _configuration["Plivo:FromNumber"]
                            ?? "+918031136547";

                var request = new PlivoCXOnboardingTriggerRequest
                {
                    CallerId        = callerId,
                    PhoneNumber     = phone,
                    ProspectName    = p.CandidateName ?? "there",
                    ProspectEmail   = p.ParsedEmail,
                    Specialization  = p.MagName ?? p.KeySkills ?? "Technical",
                    ExperienceYears = p.ExperienceYears,
                    LogId           = p.Id.ToString(),
                    TodayDate       = DateTime.Now.ToString("dddd, dd MMMM yyyy")
                };

                var response = await _plivoCXService.TriggerOnboardingCallAsync(request);

                if (!string.IsNullOrEmpty(response?.Error))
                {
                    _log.LogWarning($"[OnboardAuto] #{p.Id} — trigger failed: {response.Error}");
                    await _repository.UpdatePlivoCallStatusAsync(p.Id, "trigger-failed", null,
                        null, null, null, response.Error, null, null, null, null);
                    return false;
                }

                await _repository.UpdatePlivoCallStatusAsync(p.Id, "triggered", response?.ConversationId,
                    null, null, null, null, null, null, null, null);

                _log.LogInformation($"[OnboardAuto] #{p.Id} — call triggered. ConvId: {response?.ConversationId}");
                return true;
            }
            catch (Exception ex)
            {
                _log.LogError($"[OnboardAuto] #{p.Id} — call trigger error: {ex.Message}");
                await _repository.UpdatePlivoCallStatusAsync(p.Id, "trigger-failed", null,
                    null, null, null, ex.Message, null, null, null, null);
                return false;
            }
        }

        private async Task<bool> SendInvitationEmailAsync(Entities.ViewModel.NaukriProspectiveInterviewerVM p)
        {
            try
            {
                string email = p.ParsedEmail;
                if (string.IsNullOrWhiteSpace(email)) return false;
                if (string.IsNullOrWhiteSpace(p.ApplicationLink)) return false;

                string candidateName = p.CandidateName ?? "there";
                string magName = p.MagName ?? p.SelectedIRFName ?? "Interviewer";
                string appLink = p.ApplicationLink;

                var portalBase = _configuration["ApplicationUrl"] ?? "https://portal.risebird.io";
                string responseBaseUrl = $"{portalBase}/NaukriOnboarding/ProspectResponseAction";
                string needClarityLink    = $"{responseBaseUrl}?id={p.Id}&response=NeedClarity";
                string manualSupportLink  = $"{responseBaseUrl}?id={p.Id}&response=ManualSupport";
                string notInterestedLink  = $"{responseBaseUrl}?id={p.Id}&response=NotInterested";

                string htmlBody = BuildInvitationEmailHtml(candidateName, magName, appLink,
                    needClarityLink, manualSupportLink, notInterestedLink);

                var mail = new Mail
                {
                    TO      = email,
                    Subject = $"Risebird Interviewer Opportunity \u2014 {magName}",
                    Body    = htmlBody
                };
                await _notificationHelper.SendHelpsupportMail(mail);
                await _repository.UpdateInvitationEmailSentAsync(p.Id);

                _log.LogInformation($"[OnboardAuto] #{p.Id} — invitation email sent to {email}.");

                // I1: the same next step over WhatsApp, as the manual send in NaukriOnboardingController does.
                if (!string.IsNullOrWhiteSpace(p.ParsedPhone))
                {
                    try
                    {
                        var whatsAppResult = await _whatsAppHelper.WhatsAppSendNaukriInvitation(new WhatsAppVM
                        {
                            Emailid = email,
                            Phone = p.ParsedPhone,
                            UserName = p.CandidateName,
                            MessageValue = magName,
                            buttonlink = appLink,
                            ProspectId = p.Id,
                            SentBy = "AutomationJob"
                        });
                        await _repository.InsertHistoryAsync(p.Id, "InvitationWhatsAppSent", "—",
                            $"WhatsApp to {p.ParsedPhone}: {whatsAppResult}", "AutomationJob");
                    }
                    catch (Exception ex)
                    {
                        // The email went out; a WhatsApp failure must not mark the invitation as failed.
                        _log.LogError($"[OnboardAuto] #{p.Id} — WhatsApp error: {ex.Message}");
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _log.LogError($"[OnboardAuto] #{p.Id} — email error: {ex.Message}");
                return false;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string ExtractEmail(string text)
        {
            // Resume text glues words together ("x@gmail.comlinkedin", "|x@gmail.com"); the normalizer cuts that off.
            return CandidateEmailNormalizer.ExtractFirst(text);
        }

        private static string ExtractPhone(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var matches = Regex.Matches(text, @"(?:\+?\d{1,3}[\s\-]?)?\(?\d{3,5}\)?[\s\-]?\d{3,5}[\s\-]?\d{3,5}");
            foreach (Match m in matches)
            {
                string digits = Regex.Replace(m.Value, @"[^\d]", "");
                if (digits.Length >= 10 && digits.Length <= 13) return digits;
            }
            return null;
        }

        private static string BuildInvitationEmailHtml(string candidateName, string magName,
            string appLink, string needClarityLink, string manualSupportLink, string notInterestedLink)
        {
            return $@"
<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px;'>
  <div style='text-align:center;padding:20px 0;'>
    <h2 style='color:#2c3e50;margin:0;'>Risebird Interview Panel</h2>
    <p style='color:#7f8c8d;margin:5px 0 0;'>Interviewer Opportunity</p>
  </div>
  <div style='background:#fff;border:1px solid #e0e0e0;border-radius:8px;padding:30px;'>
    <p style='font-size:15px;color:#333;'>Hi {System.Net.WebUtility.HtmlEncode(candidateName)},</p>
    <p style='font-size:14px;color:#555;line-height:1.7;'>
      We came across your profile and believe you would be an excellent fit for our interview panel as a
      <strong style='color:#2c3e50;'>{System.Net.WebUtility.HtmlEncode(magName)}</strong> specialist.
    </p>
    <p style='font-size:14px;color:#555;line-height:1.7;'>
      Risebird connects seasoned professionals with top companies to conduct technical interviews.
      It's 100% remote, fully flexible, and compensated transparently.
    </p>
    <div style='text-align:center;margin:25px 0;'>
      <a href='{appLink}' style='display:inline-block;background:#27ae60;color:#fff;padding:14px 32px;border-radius:6px;text-decoration:none;font-size:15px;font-weight:600;'>
        I'm Interested &mdash; Register Now
      </a>
    </div>
    <p style='font-size:13px;color:#888;text-align:center;'>Click the button above to complete a quick registration (takes ~2 minutes)</p>
    <hr style='border:none;border-top:1px solid #eee;margin:25px 0;'/>
    <p style='font-size:14px;color:#555;'>Not sure yet? Choose an option below:</p>
    <table style='width:100%;border-collapse:collapse;margin-top:10px;'>
      <tr>
        <td style='padding:8px 5px;text-align:center;'>
          <a href='{needClarityLink}' style='display:inline-block;background:#3498db;color:#fff;padding:10px 20px;border-radius:5px;text-decoration:none;font-size:13px;'>
            I Need More Details
          </a>
        </td>
        <td style='padding:8px 5px;text-align:center;'>
          <a href='{manualSupportLink}' style='display:inline-block;background:#e67e22;color:#fff;padding:10px 20px;border-radius:5px;text-decoration:none;font-size:13px;'>
            I Need Help Registering
          </a>
        </td>
        <td style='padding:8px 5px;text-align:center;'>
          <a href='{notInterestedLink}' style='display:inline-block;background:#95a5a6;color:#fff;padding:10px 20px;border-radius:5px;text-decoration:none;font-size:13px;'>
            Not Interested
          </a>
        </td>
      </tr>
    </table>
  </div>
  <div style='text-align:center;padding:20px 0;'>
    <p style='font-size:12px;color:#aaa;'>Risebird Technologies Pvt. Ltd. &bull; <a href='https://portal.risebird.io' style='color:#3498db;'>portal.risebird.io</a></p>
  </div>
</div>";
        }
    }
}
