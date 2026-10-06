using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class InterviewerCoachingService : IInterviewerCoachingService
    {
        private const string LoadErrorMessage = "Could not load the coaching brief. Please try again.";
        private const string MailSentResult = "Mail sent successfully.";

        private readonly IInterviewerCoachingRepository _repository;
        private readonly INotificationHelper _notificationHelper;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly Func<DateTime> _utcNow;

        public InterviewerCoachingService(IInterviewerCoachingRepository repository, INotificationHelper notificationHelper,
            IApplicationErrorLogger applicationErrorLogger)
            : this(repository, notificationHelper, applicationErrorLogger, () => DateTime.UtcNow)
        {
        }

        /// <summary>Test seam for the clock. Internal so DI only sees the public constructor.</summary>
        internal InterviewerCoachingService(IInterviewerCoachingRepository repository, INotificationHelper notificationHelper,
            IApplicationErrorLogger applicationErrorLogger, Func<DateTime> utcNow)
        {
            _repository = repository;
            _notificationHelper = notificationHelper;
            _applicationErrorLogger = applicationErrorLogger;
            _utcNow = utcNow;
        }

        private DateTime TodayIst => _utcNow().AddMinutes(330).Date;

        public async Task<CoachingBriefVM> GetBriefAsync(string interviewerEmail, DateTime? fromIst, DateTime? toIst)
        {
            var (from, to, _) = QCActivityDashboardService.NormalizeRange(fromIst, toIst, TodayIst);
            var email = Clean(interviewerEmail);
            if (email == null)
                return new CoachingBriefVM { Error = "Pick an interviewer.", FromIst = Ymd(from), ToIst = Ymd(to) };

            try
            {
                var exposure = await _repository.GetExposureAsync(email, from, to);
                if (exposure?.Profile == null)
                    return new CoachingBriefVM { InterviewerEmail = email, Error = "No interviewer found with that email.", FromIst = Ymd(from), ToIst = Ymd(to) };
                return InterviewerCoachingBriefBuilder.Build(exposure, from, to, _utcNow());
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new CoachingBriefVM { InterviewerEmail = email, Error = LoadErrorMessage, ErrorDetail = ex.GetBaseException().Message, FromIst = Ymd(from), ToIst = Ymd(to) };
            }
        }

        public async Task<CoachingEmailPreviewVM> PreviewEmailAsync(CoachingEmailRequestVM request)
        {
            var (preview, _) = await BuildPreviewAsync(request);
            return preview;
        }

        private async Task<(CoachingEmailPreviewVM Preview, CoachingBriefVM Brief)> BuildPreviewAsync(CoachingEmailRequestVM request)
        {
            var brief = await GetBriefAsync(request?.InterviewerEmail, request?.From, request?.To);
            if (!string.IsNullOrEmpty(brief.Error))
                return (new CoachingEmailPreviewVM { CanSend = false, Message = brief.Error }, brief);

            var note = request?.Note?.Trim();
            if (note != null && note.Length > CoachingConstants.MaxNoteLength)
                note = note.Substring(0, CoachingConstants.MaxNoteLength);

            var (subject, html, count) = InterviewerCoachingBriefBuilder.BuildEmail(brief, request?.Themes, note);
            var preview = new CoachingEmailPreviewVM
            {
                To = brief.InterviewerEmail,
                Subject = subject,
                Html = html,
                ThemeCount = count,
                CanSend = count > 0 && brief.CanEmail
            };
            if (count == 0)
                preview.Message = "Pick at least one area to include.";
            else if (!brief.CanEmail)
                preview.Message = !brief.IsActive
                    ? "This interviewer's account is inactive, so the summary cannot be emailed."
                    : brief.EmailAvailableAtUtc.HasValue
                    ? $"A summary was already sent to this interviewer. The next one can go after {brief.EmailAvailableAtUtc.Value.AddMinutes(330):yyyy-MM-dd HH:mm} IST."
                    : "This interviewer cannot be emailed.";
            return (preview, brief);
        }

        public async Task<CoachingActionResultVM> SendEmailAsync(CoachingEmailRequestVM request, string coachEmail)
        {
            var coach = Clean(coachEmail);
            if (coach == null)
                return Fail("Your session has expired. Please sign in again.");

            int? reservedSessionId = null;
            var sent = false;
            try
            {
                var (preview, brief) = await BuildPreviewAsync(request);
                if (!preview.CanSend)
                    return Fail(preview.Message ?? "The summary cannot be sent.");

                var themeKeys = brief.Themes.Where(t => !t.IsInternal && t.ItemCount > 0
                        && (request?.Themes ?? new List<string>()).Contains(t.Key, StringComparer.OrdinalIgnoreCase))
                    .Select(t => t.Key).ToList();
                var issueCount = brief.Themes.Where(t => themeKeys.Contains(t.Key)).Sum(t => t.ItemCount);

                // Reserve the 12-hour slot first (atomic in the procedure), then send.
                var saved = await _repository.SaveSessionAsync(brief.InterviewerEmail, coach, CoachingConstants.EmailChannel,
                    CoachingConstants.EmailOnlyOutcome, string.Join(",", themeKeys), Clean(request?.Note), preview.Subject, issueCount,
                    ParseYmd(brief.FromIst), ParseYmd(brief.ToIst), linkRecentCall: false);
                if (saved.Result == -1)
                    return Fail($"A summary was already sent to this interviewer in the last {CoachingConstants.EmailCooldownHours} hours.");
                if (saved.Result <= 0)
                    return Fail("No interviewer found with that email.");
                reservedSessionId = saved.Result;

                var sendResult = await _notificationHelper.SendInterviewerCoachingEmail(
                    brief.InterviewerEmail, brief.InterviewerName, coach, preview.Subject, preview.Html);
                sent = string.Equals(sendResult, MailSentResult, StringComparison.OrdinalIgnoreCase);
                if (!sent)
                {
                    await ReleaseReservationAsync(reservedSessionId.Value);
                    return Fail("The email could not be sent. Please try again.");
                }

                return new CoachingActionResultVM { Ok = true, Message = "Summary sent to " + brief.InterviewerEmail + ".", SessionId = saved.Result };
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex, coach);
                // Anything that failed after the slot was reserved must not use up the 12-hour cooldown.
                if (reservedSessionId.HasValue && !sent)
                    await ReleaseReservationAsync(reservedSessionId.Value);
                return Fail("The email could not be sent. Please try again.");
            }
        }

        private async Task ReleaseReservationAsync(int sessionId)
        {
            try
            {
                await _repository.CancelEmailSessionAsync(sessionId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task<CoachingActionResultVM> LogSessionAsync(LogCoachingSessionRequestVM request, string coachEmail)
        {
            var coach = Clean(coachEmail);
            if (coach == null)
                return Fail("Your session has expired. Please sign in again.");

            var email = Clean(request?.InterviewerEmail);
            if (email == null)
                return Fail("Pick an interviewer.");

            var channel = CoachingConstants.CanonicalKey(CoachingConstants.Channels, request.Channel);
            if (channel == null || channel == CoachingConstants.EmailChannel)
                return Fail("Choose how you reached the interviewer (use Email summary to send an email).");

            var outcome = CoachingConstants.CanonicalKey(CoachingConstants.Outcomes, request.Outcome);
            if (outcome == null)
                return Fail("Choose an outcome.");

            var agreed = request.Agreed?.Trim();
            if (string.IsNullOrEmpty(agreed) || agreed.Length < CoachingConstants.MinAgreedLength)
                return Fail($"Describe what was agreed in at least {CoachingConstants.MinAgreedLength} characters.");
            if (agreed.Length > CoachingConstants.MaxAgreedLength)
                agreed = agreed.Substring(0, CoachingConstants.MaxAgreedLength);

            var validThemes = InterviewerCoachingBriefBuilder.ThemeKeys;
            var themes = (request.Themes ?? new List<string>())
                .Select(t => validThemes.FirstOrDefault(k => string.Equals(k, t?.Trim(), StringComparison.OrdinalIgnoreCase)))
                .Where(t => t != null)
                .Distinct()
                .ToList();

            var (from, to, _) = QCActivityDashboardService.NormalizeRange(request.From, request.To, TodayIst);

            try
            {
                var saved = await _repository.SaveSessionAsync(email, coach, channel, outcome, string.Join(",", themes), agreed,
                    null, null, from, to, linkRecentCall: channel == "Call");
                if (saved.Result <= 0)
                    return Fail("No interviewer found with that email.");

                return new CoachingActionResultVM
                {
                    Ok = true,
                    SessionId = saved.Result,
                    LinkedCallSid = saved.LinkedCallSid,
                    Message = string.IsNullOrEmpty(saved.LinkedCallSid)
                        ? "Coaching session saved."
                        : "Coaching session saved and linked to your call."
                };
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex, coach);
                return Fail("The session could not be saved. Please try again.");
            }
        }

        private static CoachingActionResultVM Fail(string message) => new CoachingActionResultVM { Ok = false, Message = message };

        private static string Clean(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            var trimmed = value.Trim().Replace("\"", "");
            return trimmed.Length == 0 ? null : trimmed;
        }

        private static string Ymd(DateTime d) => d.ToString("yyyy-MM-dd");

        private static DateTime? ParseYmd(string ymd) =>
            DateTime.TryParseExact(ymd, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var d) ? d : (DateTime?)null;
    }
}
