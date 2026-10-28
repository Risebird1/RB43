using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class InterviewAlignmentService : IInterviewAlignmentService
    {
        private const string SettingsSection = "InterviewAlignment";
        private const string DefaultTrialPolicyVersion = "2026-10";

        /// <summary>Planned length used when the channel's plan has no duration.</summary>
        private const int FallbackInterviewMinutes = 90;

        /// <summary>A token stays usable this long after the interview's planned end, for overruns and late submits.</summary>
        private const int TokenGraceMinutes = 180;

        /// <summary>Without an interview date a token lives this long from creation.</summary>
        private const int UndatedTokenLifetimeHours = 24;

        private const int MaxSourceCharacters = 100000;
        private const int MaxStdinCharacters = 10000;

        private readonly IInterviewAlignmentRepository _repository;
        private readonly ICandidateService _candidateService;
        private readonly IIRFService _irfService;
        private readonly ICodeExecutionService _codeExecutionService;
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _logger;

        public InterviewAlignmentService(IInterviewAlignmentRepository repository, ICandidateService candidateService,
            IIRFService irfService, ICodeExecutionService codeExecutionService, IConfiguration configuration,
            IApplicationErrorLogger logger)
        {
            _repository = repository;
            _candidateService = candidateService;
            _irfService = irfService;
            _codeExecutionService = codeExecutionService;
            _configuration = configuration;
            _logger = logger;
        }

        private string TrialPolicyVersion =>
            string.IsNullOrWhiteSpace(_configuration[$"{SettingsSection}:TrialPolicyVersion"])
                ? DefaultTrialPolicyVersion
                : _configuration[$"{SettingsSection}:TrialPolicyVersion"].Trim();

        public async Task<bool> IsAlignmentRequiredAsync(int interviewId)
        {
            if (interviewId <= 0)
            {
                return false;
            }

            return await _repository.IsAlignmentRequiredAsync(interviewId);
        }

        public async Task<bool?> SetAlignmentRequiredAsync(int irfId, bool isRequired)
        {
            if (irfId <= 0)
            {
                return false;
            }

            return await _repository.SetAlignmentRequiredAsync(irfId, isRequired);
        }

        public async Task<InterviewAlignmentViewModel> BuildAlignmentAsync(int interviewId, string interviewerEmail, Meeting meeting,
            int defaultCodingTimeLimitInMinutes, int maxQuestionLength)
        {
            try
            {
                var context = await _repository.GetAlignmentContextAsync(interviewId, interviewerEmail);
                // The switch is re-read with the rest of the context: switched off mid-flight means today's page.
                if (context == null || !context.IsInterviewerAlignmentRequired)
                {
                    return null;
                }

                var model = new InterviewAlignmentViewModel
                {
                    InterviewId = interviewId,
                    IRFIntId = context.IRFIntId,
                    IRFCode = context.IRFCode,
                    RoleTitle = context.RoleTitle?.Trim(),
                    DurationMinutes = context.DurationMinutes,
                    CodeSnippetRequired = context.CodeSnippetRequired,
                    IsTrialInterview = context.IsTrialInterview,
                    ShowTrialOnboarding = context.IsTrialInterview && !context.TrialOnboardingAcknowledged,
                    TrialPolicyUrl = _configuration[$"{SettingsSection}:TrialPolicyUrl"],
                    TrialPolicyVersion = TrialPolicyVersion,
                    CompletedOnChannelBefore = context.CompletedOnChannelBefore,
                    HasCandidatePreCheck = !string.IsNullOrWhiteSpace(meeting?.PreReportLink),
                    CodeRunnerEnabled = _codeExecutionService.IsEnabled,
                    DefaultCodingTimeLimitInMinutes = defaultCodingTimeLimitInMinutes,
                    MaxQuestionLength = maxQuestionLength
                };

                // Coded identity only: the canonical CandidatePseudonym mechanism, never a name built here.
                // The page's Meeting can come back without the candidate (usp_FetchAudioComment inner joins the
                // candidate tables), so the interview's own candidate email is the fallback key.
                var candidateEmail = !string.IsNullOrWhiteSpace(meeting?.candidateEmail) ? meeting.candidateEmail.Trim() : context.CandidateEmail?.Trim();
                if (!string.IsNullOrWhiteSpace(candidateEmail))
                {
                    var pseudonyms = await _candidateService.EnsureCandidatePseudonymsAsync(new[] { candidateEmail }, "InterviewAlignment");
                    if (pseudonyms != null && pseudonyms.TryGetValue(candidateEmail, out var nickName) && !string.IsNullOrWhiteSpace(nickName))
                    {
                        model.CandidateCodedName = nickName;
                    }
                }

                // The card then says "Coded name not assigned yet". Say why in the log - by interview id only, never
                // the candidate's email. The pseudonym lookup logs its own exception just before this, if it threw.
                if (string.IsNullOrWhiteSpace(model.CandidateCodedName))
                {
                    await _logger.LogMessage(LogLevelInfo.Warning, string.IsNullOrWhiteSpace(candidateEmail)
                        ? $"InterviewAlignment: interview {interviewId} has no candidate email, so no coded name can be assigned."
                        : $"InterviewAlignment: no coded name came back from CandidatePseudonym for interview {interviewId}; see the preceding exception, if any.",
                        interviewerEmail);
                }

                var (questions, sessions) = await GetCodeQuestionsAsync(interviewId, interviewerEmail);
                model.Questions = questions;
                model.Sessions = sessions;

                // "What went wrong before". Loaded on its own so that if it fails the card says so and everything
                // else on the page still renders.
                await LoadFlaggedInterviewsAsync(model, interviewId, interviewerEmail);

                if (context.IRFIntId > 0)
                {
                    var skills = await _irfService.GetIRFTechSkills(context.IRFIntId);
                    model.Skills = skills?
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList() ?? new List<string>();
                }

                return model;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        private static readonly System.Text.RegularExpressions.Regex EmailPattern =
            new System.Text.RegularExpressions.Regex(@"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}", System.Text.RegularExpressions.RegexOptions.Compiled);

        private const int MaxReasoningCharacters = 400;

        private async Task LoadFlaggedInterviewsAsync(InterviewAlignmentViewModel model, int interviewId, string interviewerEmail)
        {
            try
            {
                var rows = await _repository.GetFlaggedInterviewsAsync(interviewId, interviewerEmail);
                if (rows == null)
                {
                    model.FlaggedInterviewsLoaded = false;
                    return;
                }

                var flagged = rows.Where(r => r != null && r.InterviewId > 0).ToList();

                // Coded identity only: the same CandidatePseudonym names as everywhere else, then the emails are cleared.
                var emails = flagged.Select(r => r.CandidateEmail?.Trim()).Where(e => !string.IsNullOrWhiteSpace(e))
                    .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                IDictionary<string, string> pseudonyms = null;
                if (emails.Count > 0)
                {
                    pseudonyms = await _candidateService.EnsureCandidatePseudonymsAsync(emails, "InterviewAlignment");
                }

                foreach (var row in flagged)
                {
                    var email = row.CandidateEmail?.Trim();
                    row.CandidateCodedName = !string.IsNullOrEmpty(email) && pseudonyms != null && pseudonyms.TryGetValue(email, out var nick) ? nick : null;
                    row.CandidateEmail = null;
                    row.AIReasoning = CleanReviewText(row.AIReasoning);
                    row.QCComment = CleanReviewText(row.QCComment);
                }

                model.FlaggedInterviews = flagged;
                model.FlaggedInterviewsLoaded = true;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                model.FlaggedInterviews = new List<AlignmentFlaggedInterview>();
                model.FlaggedInterviewsLoaded = false;
            }
        }

        /// <summary>
        /// Review text as plain, short text: markdown bold markers removed, any email address masked (the interviewer
        /// sees candidates by coded identity only), whitespace collapsed and cut to a readable length.
        /// The view HTML-encodes it as well.
        /// </summary>
        internal static string CleanReviewText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var cleaned = EmailPattern.Replace(text.Replace("**", ""), "[email]");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ").Trim();
            return cleaned.Length > MaxReasoningCharacters ? cleaned.Substring(0, MaxReasoningCharacters).TrimEnd() + "…" : cleaned;
        }

        public async Task<bool> AcknowledgeTrialOnboardingAsync(string interviewerEmail)
        {
            if (string.IsNullOrWhiteSpace(interviewerEmail))
            {
                return false;
            }

            return await _repository.AcknowledgeTrialOnboardingAsync(interviewerEmail.Trim(), TrialPolicyVersion, DateTime.Now);
        }

        public async Task<bool> SaveJoinGateOverrideAsync(int interviewId, string interviewerEmail, IEnumerable<string> outstandingRequirements)
        {
            // Only known requirement keys are stored, whatever the browser sent, in the gate's own order.
            var sent = new HashSet<string>(
                (outstandingRequirements ?? Enumerable.Empty<string>())
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .Select(r => r.Trim().ToLowerInvariant()));
            var outstanding = JoinGateRequirementKeys.All.Where(sent.Contains).ToList();

            return await _repository.SaveJoinGateOverrideAsync(interviewId, interviewerEmail.Trim(), string.Join(",", outstanding), DateTime.Now);
        }

        public async Task<(IList<InterviewerCodeQuestion> Questions, IList<InterviewCodeSession> Sessions)> GetCodeQuestionsAsync(int interviewId, string interviewerEmail)
        {
            var questions = await _repository.GetInterviewerCodeQuestionsAsync(interviewId, interviewerEmail) ?? new List<InterviewerCodeQuestion>();
            var sessions = await _repository.GetCodeSessionsAsync(interviewId) ?? new List<InterviewCodeSession>();
            return (questions, sessions);
        }

        public async Task<CreateInterviewCodeSessionResult> CreateCodeSessionAsync(string interviewerEmail, CreateCodeSessionRequestVM request)
        {
            return await _repository.CreateCodeSessionAsync(
                request.InterviewId,
                interviewerEmail.Trim(),
                request.QuestionId,
                request.QuestionText,
                string.IsNullOrWhiteSpace(request.SkillName) ? null : request.SkillName.Trim(),
                request.TimeLimitInMinutes,
                Guid.NewGuid(),
                DateTime.Now);
        }

        public async Task<CodeSessionPageViewModel> GetCandidatePageAsync(Guid token)
        {
            var session = await _repository.GetCodeSessionByTokenAsync(token);
            if (session == null)
            {
                return null;
            }

            var now = DateTime.Now;
            var live = IsSessionUsable(session, now);
            if (live && !session.OpenedOn.HasValue)
            {
                session.OpenedOn = await _repository.MarkCodeSessionOpenedAsync(token, now) ?? now;
            }

            return new CodeSessionPageViewModel
            {
                Token = session.Token,
                QuestionText = session.QuestionText,
                CodingTimeLimitInMinutes = session.CodingTimeLimitInMinutes,
                OpenedOn = session.OpenedOn,
                ServerNow = now,
                LanguageId = session.LanguageId,
                Source = session.SubmittedSource ?? session.LastRunSource,
                LastRunOutput = session.LastRunOutput,
                SubmittedOn = session.SubmittedOn,
                RunEnabled = _codeExecutionService.IsEnabled,
                IsExpired = !live,
                Languages = CodeLanguageCatalog.Languages.ToList()
            };
        }

        public async Task<(bool Accepted, string Message, CodeExecutionResult Result)> RunAsync(CodeSessionRunRequestVM request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Code))
            {
                return (false, "Write some code before running it.", null);
            }

            if (!_codeExecutionService.IsEnabled)
            {
                return (false, "Running code is not available in this environment. You can still write and submit your answer.", null);
            }

            if (request.Code.Length > MaxSourceCharacters || (request.Stdin?.Length ?? 0) > MaxStdinCharacters)
            {
                return (false, "Your code is too long to run.", null);
            }

            var language = CodeLanguageCatalog.Find(request.Language);
            if (language == null)
            {
                return (false, "Choose a language before running your code.", null);
            }

            var session = await _repository.GetCodeSessionByTokenAsync(request.Token);
            if (!IsSessionUsable(session, DateTime.Now))
            {
                return (false, "This code session has ended or the link is not valid.", null);
            }

            // A submitted answer is final; nothing more runs against it.
            if (session.SubmittedOn.HasValue)
            {
                return (false, SubmittedLockedMessage(session.SubmittedOn.Value), null);
            }

            var result = await _codeExecutionService.ExecuteAsync(new CodeExecutionRequest
            {
                LanguageId = language.Id,
                SourceCode = request.Code,
                Stdin = request.Stdin
            });

            // The runner never persists anything; the session keeps its own record of what came back.
            await _repository.SaveCodeSessionRunAsync(request.Token, language.Id, request.Code,
                result?.CombinedOutput, result?.Status ?? (result?.Success == true ? "Success" : "Error"), DateTime.Now);

            return (true, null, result);
        }

        public async Task<(bool Accepted, string Message, DateTime? SubmittedOn)> SubmitAsync(CodeSessionSubmitRequestVM request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Code))
            {
                return (false, "Write some code before submitting it.", null);
            }

            if (request.Code.Length > MaxSourceCharacters)
            {
                return (false, "Your code is too long to submit.", null);
            }

            var language = CodeLanguageCatalog.Find(request.Language);
            var session = await _repository.GetCodeSessionByTokenAsync(request.Token);
            if (!IsSessionUsable(session, DateTime.Now))
            {
                return (false, "This code session has ended or the link is not valid.", null);
            }

            // The candidate's code is final once submitted: it cannot be replaced, by the candidate or by anyone else who
            // opens the same link. usp_SubmitInterviewCodeSession enforces this too.
            if (session.SubmittedOn.HasValue)
            {
                return (false, SubmittedLockedMessage(session.SubmittedOn.Value), session.SubmittedOn);
            }

            var now = DateTime.Now;
            var saved = await _repository.SubmitCodeSessionAsync(request.Token, language?.Id ?? session.LanguageId, request.Code, now);
            return saved
                ? (true, null, now)
                : (false, "Your answer could not be saved, or it had already been submitted. Reload the page to see it.", null);
        }

        public async Task<IList<InterviewCodeSession>> GetSessionsForReportAsync(int interviewId)
        {
            if (interviewId <= 0)
            {
                return new List<InterviewCodeSession>();
            }

            return await _repository.GetCodeSessionsAsync(interviewId) ?? new List<InterviewCodeSession>();
        }

        private static string SubmittedLockedMessage(DateTime submittedOn) =>
            $"This answer was submitted on {submittedOn:dd MMM yyyy, HH:mm} and can no longer be changed.";

        /// <summary>
        /// A token answers only while its interview is live and its channel still has the alignment switch on:
        /// like every other alignment endpoint, nothing answers for a channel that has been switched off.
        /// </summary>
        private bool IsSessionUsable(InterviewCodeSession session, DateTime now) =>
            session != null && session.IsInterviewerAlignmentRequired && IsSessionLive(session, now);

        public bool IsSessionLive(InterviewCodeSession session, DateTime now)
        {
            if (session == null)
            {
                return false;
            }

            DateTime expiresAt;
            if (session.InterviewDate.HasValue)
            {
                var plannedMinutes = session.InterviewDurationMinutes.GetValueOrDefault() > 0
                    ? session.InterviewDurationMinutes.Value
                    : FallbackInterviewMinutes;
                expiresAt = session.InterviewDate.Value.AddMinutes(plannedMinutes + TokenGraceMinutes);
            }
            else
            {
                expiresAt = session.CreatedOn.AddHours(UndatedTokenLifetimeHours);
            }

            return now <= expiresAt;
        }
    }
}
