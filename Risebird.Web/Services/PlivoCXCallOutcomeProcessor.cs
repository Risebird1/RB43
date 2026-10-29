using Newtonsoft.Json;
using RestSharp;
using Risebird.Business.Interface;
using Risebird.Business.PlivoCX;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.PlivoCXModels;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using Risebird.Web.Extensions;
using System;
using System.Threading.Tasks;

namespace Risebird.Web.Services
{
    /// <summary>
    /// What happens when an AI call ends, whichever provider placed it: find the call log, apply
    /// the outcome (status, transcript, summary, extracted date / time / confirmation /
    /// disposition), book the interview through the Schedule API when the agent collected a
    /// confirmed slot, capture a Plivo recording, write the disposition back to the candidate,
    /// and advance the PlivoCX campaign candidate.
    ///
    /// Fed by PlivoCXInterviewCallController.HangupCallback (Plivo CX hangup event) and
    /// PlivoCXInterviewCallController.ElevenLabsCallback / PhoneScreenerController.ElevenLabsWebhook
    /// (ElevenLabs post-call event), each of which first maps its payload to PlivoCXHangupPayload.
    /// Moved out of HangupCallback unchanged so both providers share one booking path.
    /// </summary>
    public class PlivoCXCallOutcomeProcessor
    {
        private readonly IPlivoCXService         _plivoCXService;
        private readonly IPlivoCXCallRepository  _repository;
        private readonly IApplicationErrorLogger _logger;
        private readonly IPlivoCXCampaignService _campaignService;

        public PlivoCXCallOutcomeProcessor(
            IPlivoCXService         plivoCXService,
            IPlivoCXCallRepository  repository,
            IApplicationErrorLogger logger,
            IPlivoCXCampaignService campaignService)
        {
            _plivoCXService  = plivoCXService;
            _repository      = repository;
            _logger          = logger;
            _campaignService = campaignService;
        }

        /// <summary>Applies a finished call. Never throws: webhooks must always be answered 200.</summary>
        public async Task ProcessAsync(PlivoCXHangupPayload payload)
        {
            try
            {
                // ── Step 5: Find matching call log ───────────────────────────────────
                // Priority: log_id (our DB PK) → conversation_id → fallback to request_id
                PlivoCXCallLog callLog = null;
                string lookupMethod    = null;

                if (!string.IsNullOrEmpty(payload.LogId) && int.TryParse(payload.LogId, out int logId))
                {
                    callLog      = await _repository.GetCallLogByIdAsync(logId);
                    lookupMethod = $"log_id={logId}";
                }
                if (callLog == null && !string.IsNullOrEmpty(payload.ConversationId))
                {
                    callLog      = await _repository.GetCallLogByConversationIdAsync(payload.ConversationId);
                    lookupMethod = $"conversation_id={payload.ConversationId}";
                }
                if (callLog == null && !string.IsNullOrEmpty(payload.ConversationId))
                {
                    callLog      = await _repository.GetCallLogByRequestIdAsync(payload.ConversationId);
                    lookupMethod = $"request_id={payload.ConversationId}";
                }

                if (callLog == null)
                {
                    await SaveCpAsync(CheckpointNames.HangupLogNotFound,
                        $"No call log matched — tried: {lookupMethod}",
                        convId: payload.ConversationId);
                    return;
                }

                await SaveCpAsync(CheckpointNames.HangupLogFound,
                    $"Call log #{callLog.Id} found via {lookupMethod}",
                    convId: payload.ConversationId,
                    callLogId: callLog.Id,
                    extra: $"phone: {callLog.RecipientPhone}, status_was: {callLog.CallStatus}");

                // ── Step 6: Apply call outcome fields ────────────────────────────────
                callLog.PlivoCXConversationId = payload.ConversationId  ?? callLog.PlivoCXConversationId;
                callLog.CallStatus            = payload.CallStatus      ?? callLog.CallStatus;
                // A hangup means the call is over. Without a call_status in the event the row stayed
                // "triggered" and kept holding a campaign line until the in-flight timeout.
                if (callLog.CallStatus == "initiated" || callLog.CallStatus == "triggered")
                    callLog.CallStatus = !string.IsNullOrWhiteSpace(payload.CandidateDisposition)
                                      || !string.IsNullOrWhiteSpace(payload.Transcript)
                                      || (int.TryParse(payload.Duration, out var heardSeconds) && heardSeconds > 0)
                        ? "completed"
                        : "no-answer";
                callLog.HangupCause           = payload.HangupCause;
                // Don't wipe a transcript we already have: Plivo CX often delivers the
                // hangup webhook before the transcript is ready, so payload.Transcript is null.
                callLog.CallTranscript        = payload.Transcript ?? callLog.CallTranscript;
                callLog.ConversationSummary   = payload.Summary ?? callLog.ConversationSummary;

                // If still no transcript, pull it from Plivo by call_uuid (the Recording +
                // Transcription APIs). FetchCallMediaAsync creates the transcription if one
                // doesn't exist yet; a later backfill run picks it up once Plivo finishes.
                if (string.IsNullOrWhiteSpace(callLog.CallTranscript) && !string.IsNullOrWhiteSpace(payload.CallUUID))
                {
                    try
                    {
                        var media = await _plivoCXService.FetchCallMediaAsync(payload.CallUUID);
                        if (!string.IsNullOrWhiteSpace(media?.Transcript))
                            callLog.CallTranscript = media.Transcript;
                        if ((callLog.DurationSeconds ?? 0) == 0 && media?.DurationSeconds > 0)
                            callLog.DurationSeconds = media.DurationSeconds;
                        await SaveCpAsync("transcript_fetch_attempted",
                            string.IsNullOrWhiteSpace(media?.Transcript)
                                ? $"No transcript from Plivo yet for call_uuid={payload.CallUUID} ({media?.Error ?? "pending"})"
                                : $"Fetched transcript from Plivo ({media.Transcript.Length} chars) for call_uuid={payload.CallUUID}",
                            convId: payload.ConversationId, callLogId: callLog.Id);
                    }
                    catch (Exception fx)
                    {
                        await SaveCpAsync("transcript_fetch_failed", fx.Message,
                            convId: payload.ConversationId, callLogId: callLog.Id);
                    }
                }
                callLog.BookedInterviewDate   = ResolveRelativeDate(payload.InterviewDate);
                callLog.BookedInterviewTime   = payload.InterviewTime;
                callLog.CandidateDisposition  = PlivoCXOutcomeParser.NormaliseDisposition(payload.CandidateDisposition);
                callLog.CallEndedAt           = DateTime.UtcNow;
                callLog.UpdatedAt             = DateTime.UtcNow;

                await SaveCpAsync("disposition_extracted",
                    $"Disposition: {callLog.CandidateDisposition ?? "(none)"} — raw: {payload.CandidateDisposition}",
                    convId: payload.ConversationId, callLogId: callLog.Id);

                if (!string.IsNullOrEmpty(payload.Duration) &&
                    int.TryParse(payload.Duration, out int dur))
                    callLog.DurationSeconds = dur;

                // ── Step 7: Attempt booking if candidate confirmed ────────────────────
                // The agent's LLM writes this value: accept "true", "\"true\"", "Yes", "true." etc.
                bool candidateConfirmed = PlivoCXOutcomeParser.IsConfirmed(payload.CandidateConfirmed);

                // A campaign running a confirm-only agent (CollectsSlot = false) records the
                // confirmation but never books a slot.
                bool agentCollectsSlot = true;
                try { agentCollectsSlot = await _campaignService.ShouldBookAsync(callLog); }
                catch (Exception campEx) { await _logger.LogMessage(LogLevelInfo.Exception, campEx); }

                if (candidateConfirmed && agentCollectsSlot &&
                    !string.IsNullOrWhiteSpace(payload.InterviewDate) &&
                    !string.IsNullOrWhiteSpace(payload.InterviewTime))
                {
                    await SaveCpAsync(CheckpointNames.HangupBookingStarted,
                        $"Booking — date: {payload.InterviewDate}, time: {payload.InterviewTime}",
                        convId: payload.ConversationId, callLogId: callLog.Id);

                    await ProcessBookingAsync(callLog, payload, payload.ConversationId);

                    await SaveCpAsync(CheckpointNames.HangupBookingDone,
                        $"Booking result — success: {callLog.BookingApiSuccess}, response: {callLog.BookingApiResponse?.Substring(0, Math.Min(200, callLog.BookingApiResponse?.Length ?? 0))}",
                        convId: payload.ConversationId, callLogId: callLog.Id);
                }
                else
                {
                    await SaveCpAsync(CheckpointNames.HangupBookingSkipped,
                        $"Booking skipped — confirmed: {payload.CandidateConfirmed}, date: {payload.InterviewDate}, time: {payload.InterviewTime}",
                        convId: payload.ConversationId, callLogId: callLog.Id);
                }

                // ── Step 7b: Capture the call recording by call UUID ──────────────────
                // Store the UUID (key for the Recording API) and pull the recording URL now.
                // If the recording is not final yet (it lands ~1s after hangup) it stays NULL
                // and PlivoCXRecordingBackfillJob fills it in on a later run.
                if (!string.IsNullOrWhiteSpace(payload.CallUUID))
                {
                    callLog.PlivoCallUuid = payload.CallUUID.Trim();
                    try
                    {
                        callLog.RecordingUrl = await _plivoCXService.GetRecordingUrlAsync(callLog.PlivoCallUuid)
                                               ?? callLog.RecordingUrl;
                        await SaveCpAsync("recording_lookup",
                            string.IsNullOrEmpty(callLog.RecordingUrl)
                                ? $"Recording not ready yet for call_uuid {callLog.PlivoCallUuid} — backfill job will retry"
                                : $"Recording captured for call_uuid {callLog.PlivoCallUuid}",
                            convId: payload.ConversationId, callLogId: callLog.Id);
                    }
                    catch (Exception recEx)
                    {
                        await _logger.LogMessage(LogLevelInfo.Exception, recEx);
                    }
                }

                await _repository.UpdateCallLogAsync(callLog);

                // Write disposition back to Candidate table so it's visible across the platform
                if (callLog.CandidateId.HasValue && !string.IsNullOrEmpty(callLog.CandidateDisposition))
                {
                    await _repository.UpdateCandidateDispositionAsync(
                        callLog.CandidateId.Value, callLog.CandidateDisposition);
                    await SaveCpAsync("disposition_saved",
                        $"Candidate #{callLog.CandidateId} disposition set to '{callLog.CandidateDisposition}'",
                        convId: payload.ConversationId, callLogId: callLog.Id);
                }

                // PlivoCX campaign: advance the campaign candidate (Booked / NotBooked / retry / Cold).
                if (callLog.CampaignId.HasValue)
                {
                    try
                    {
                        await _campaignService.ApplyCallOutcomeAsync(callLog, candidateConfirmed);
                        await SaveCpAsync("campaign_state_applied",
                            $"Campaign #{callLog.CampaignId} candidate #{callLog.CandidateId} updated from call #{callLog.Id}",
                            convId: payload.ConversationId, callLogId: callLog.Id);
                    }
                    catch (Exception campEx)
                    {
                        await _logger.LogMessage(LogLevelInfo.Exception, campEx);
                    }
                }

                await SaveCpAsync(CheckpointNames.HangupComplete,
                    $"Call log #{callLog.Id} updated — final status: {callLog.CallStatus}, disposition: {callLog.CandidateDisposition}",
                    convId: payload.ConversationId, callLogId: callLog.Id);

            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                await SaveCpAsync("hangup_exception", $"Unhandled exception: {ex.Message}",
                    convId: payload?.ConversationId,
                    extra: ex.ToString());
            }
        }

        /// <summary>
        /// Converts relative date words to a real date string (yyyy-MM-dd).
        /// If the AI returns an actual date, it passes through unchanged.
        /// </summary>
        private static string ResolveRelativeDate(string rawDate)
        {
            if (string.IsNullOrWhiteSpace(rawDate)) return rawDate;

            switch (rawDate.ToLowerInvariant().Trim())
            {
                case "tomorrow":
                    return DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
                case "day after tomorrow":
                case "dayaftertomorrow":
                    return DateTime.Now.AddDays(2).ToString("yyyy-MM-dd");
                default:
                    return rawDate; // already a real date string
            }
        }

        // ─── Booking logic ────────────────────────────────────────────────────────

        private async Task ProcessBookingAsync(
            PlivoCXCallLog callLog, PlivoCXHangupPayload payload, string convId = null)
        {
            try
            {
                var irfIdStr = payload.IrfId ?? callLog.IRFId?.ToString();

                // ── Resolve candidate email ──────────────────────────────────────────
                // Priority: payload → call log → DB lookup by CandidateId (same pattern as Vapi)
                var candidateEmail = payload.CandidateEmail ?? callLog.RecipientEmail;
                if (string.IsNullOrEmpty(candidateEmail) && callLog.CandidateId.HasValue)
                {
                    candidateEmail = await _repository.GetCandidateEmailByIdAsync(callLog.CandidateId.Value);
                    await SaveCpAsync("booking_email_from_db",
                        $"Email resolved from CandidateId {callLog.CandidateId}: {candidateEmail}",
                        convId: convId, callLogId: callLog.Id);
                }

                if (string.IsNullOrEmpty(candidateEmail) ||
                    string.IsNullOrEmpty(irfIdStr) ||
                    !int.TryParse(irfIdStr, out int irfId))
                {
                    var missing = string.IsNullOrEmpty(candidateEmail) ? "candidateEmail" : "irfId";
                    callLog.BookingApiResponse = $"Missing {missing} — booking skipped.";
                    await SaveCpAsync("booking_missing_data",
                        callLog.BookingApiResponse, convId: convId, callLogId: callLog.Id);
                    return;
                }

                // ── Parse and normalise date+time ────────────────────────────────────
                // interview_date should already be a real date (e.g. "2026-03-07") thanks to
                // the AI instructions using {{tomorrow_date}}. ResolveRelativeDate handles any
                // edge case where the AI still returns a relative word.
                var resolvedDate   = ResolveRelativeDate(payload.InterviewDate);
                var dateTimeString = $"{resolvedDate} {payload.InterviewTime}";

                string[] formats =
                {
                    // ISO date (what the AI should return after our instruction fix)
                    "yyyy-MM-dd hh:mm tt",  "yyyy-MM-dd h:mm tt",
                    "yyyy-MM-dd HH:mm",     "yyyy-MM-dd H:mm",
                    // Full long date — "Saturday, 07 March 2026 10:00 AM"
                    // (what the AI returns when it echoes back {{tomorrow_date}})
                    "dddd, dd MMMM yyyy hh:mm tt", "dddd, dd MMMM yyyy h:mm tt",
                    "dddd, dd MMMM yyyy HH:mm",    "dddd, dd MMMM yyyy H:mm",
                    // Short date variants
                    "dd MMMM yyyy hh:mm tt", "dd MMMM yyyy h:mm tt",
                    "dd MMMM yyyy HH:mm",    "dd MMMM yyyy H:mm",
                    "MMMM dd yyyy hh:mm tt", "MMMM dd yyyy HH:mm",
                    "dd/MM/yyyy hh:mm tt",   "dd/MM/yyyy HH:mm"
                };

                DateTime? parsedDateTime = null;
                foreach (var fmt in formats)
                {
                    if (DateTime.TryParseExact(dateTimeString, fmt,
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None, out var dt))
                    {
                        parsedDateTime = dt;
                        break;
                    }
                }
                // Last-resort: let the runtime try (handles locale-specific variations)
                if (!parsedDateTime.HasValue && DateTime.TryParse(dateTimeString, out var dtFallback))
                    parsedDateTime = dtFallback;

                if (!parsedDateTime.HasValue)
                {
                    callLog.BookingApiResponse = $"Could not parse datetime: '{dateTimeString}'";
                    await SaveCpAsync("booking_datetime_parse_failed",
                        callLog.BookingApiResponse, convId: convId, callLogId: callLog.Id,
                        extra: $"raw date: {payload.InterviewDate}, raw time: {payload.InterviewTime}");
                    return;
                }

                // Normalise BookedInterviewDate to yyyy-MM-dd regardless of what
                // the AI returned (e.g. "Saturday, 07 March 2026" → "2026-03-07")
                callLog.BookedInterviewDate     = parsedDateTime.Value.ToString("yyyy-MM-dd");
                callLog.BookedInterviewDateTime = parsedDateTime.Value;
                callLog.InterviewBooked         = true;

                await SaveCpAsync("booking_datetime_parsed",
                    $"Parsed datetime: {parsedDateTime.Value:yyyy-MM-dd HH:mm} from '{dateTimeString}' — stored date: {callLog.BookedInterviewDate}",
                    convId: convId, callLogId: callLog.Id);

                // ── Guard: don't double-book ─────────────────────────────────────────
                var hasConfirmed = await _repository.HasConfirmedInterviewAsync(candidateEmail, irfId);
                if (hasConfirmed)
                {
                    callLog.BookingApiResponse = "Already has a confirmed interview — skipped duplicate booking.";
                    callLog.BookingApiSuccess  = false;
                    await SaveCpAsync("booking_duplicate_skipped",
                        callLog.BookingApiResponse, convId: convId, callLogId: callLog.Id);
                    return;
                }

                await Task.Delay(2000); // brief wait for DB consistency

                // ── Fetch full candidate details (same SP as Vapi) ───────────────────
                var candidateDetails = await _repository.GetCandidateDetailsForScheduleAsync(irfId, candidateEmail);
                if (candidateDetails == null)
                {
                    callLog.BookingApiResponse = $"Candidate details not found in DB — irfId: {irfId}, email: {candidateEmail}";
                    await SaveCpAsync("booking_candidate_not_found",
                        callLog.BookingApiResponse, convId: convId, callLogId: callLog.Id);
                    return;
                }

                await SaveCpAsync("booking_candidate_found",
                    $"Candidate: {candidateDetails.CandidateName}, tenantId: {candidateDetails.tenantId}, duration: {candidateDetails.duration}",
                    convId: convId, callLogId: callLog.Id);

                // ── Build Schedule API request ────────────────────────────────────────
                var dto = new ScheduleCandidatesRequest
                {
                    skillChannelId = candidateDetails.IRFId,
                    requestedBy    = candidateDetails.ClientHRAttemptContact,
                    requestType    = callLog.CallPurpose == "reschedule"
                                        ? "reschedule_voiceAgent"
                                        : "schedule_voiceAgent"
                };
                var roundedDate = parsedDateTime.Value.RoundToNearSlot(candidateDetails.duration);
                dto.CandidateList.Add(new ScheduleCandidateList
                {
                    EmailId       = candidateEmail,
                    Interviewdate = roundedDate,
                    ResumeLink    = candidateDetails.ResumeLink,
                    Phone         = candidateDetails.Phone,
                    Name          = candidateDetails.CandidateName
                });

                await SaveCpAsync("booking_api_request",
                    $"Calling Schedule API — slot: {roundedDate:yyyy-MM-dd HH:mm}, tenant: {candidateDetails.tenantId}",
                    convId: convId, callLogId: callLog.Id,
                    extra: JsonConvert.SerializeObject(dto));

                // ── Call the Schedule API ─────────────────────────────────────────────
                const string apiUrl = "https://api.portal.risebird.io/api/Candidates/Schedule";
                var restClient  = new RestClient(apiUrl);
                var restRequest = new RestRequest(apiUrl, Method.Post);
                restRequest.AddHeader("content-type", "application/json");
                restRequest.AddHeader("x-functions-key", "SJJvpXEzy-1fOQN2mygBaTT6gXctL406VhpMwn0BqhKUAzFuBDDP5Q==");
                restRequest.AddHeader("tenant-id", candidateDetails.tenantId);
                restRequest.AddParameter("application/json", JsonConvert.SerializeObject(dto), ParameterType.RequestBody);

                var apiResponse = await restClient.ExecuteAsync(restRequest);

                callLog.BookingApiResponse = apiResponse.Content;
                callLog.BookingApiSuccess  = apiResponse.IsSuccessful;

                await SaveCpAsync("booking_api_response",
                    $"Schedule API — status: {apiResponse.StatusCode}, success: {apiResponse.IsSuccessful}",
                    convId: convId, callLogId: callLog.Id,
                    extra: $"response: {apiResponse.Content?.Substring(0, Math.Min(500, apiResponse.Content?.Length ?? 0))}");
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                callLog.BookingApiResponse = $"Exception: {ex.Message}";
                await SaveCpAsync("booking_exception",
                    $"Exception: {ex.Message}", convId: convId, callLogId: callLog.Id,
                    extra: ex.ToString());
            }
        }

        // ─── Checkpoints ──────────────────────────────────────────────────────────

        private async Task SaveCpAsync(
            string name, string msg, string convId = null, int? callLogId = null, string extra = null)
        {
            try
            {
                await _repository.SaveCheckpointAsync(new PlivoCXWebhookCheckpoint
                {
                    CheckpointName = name,
                    Message        = msg,
                    ConversationId = convId,
                    CallLogId      = callLogId,
                    AdditionalData = extra
                });
            }
            catch { }
        }
    }
}
