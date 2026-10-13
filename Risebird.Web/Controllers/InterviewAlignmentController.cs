using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    /// <summary>
    /// AJAX endpoints for the interview alignment additions to Interview/InterviewDetails: the trial
    /// onboarding acknowledgement, the recorded "join without confirming" override, and minting
    /// per-question code sessions.
    ///
    /// The page itself is gated by IsInterviewerForInterview, but that does not protect a new route, so
    /// every action here resolves the interviewer from session and calls it again.
    /// </summary>
    public class InterviewAlignmentController : Controller
    {
        private readonly IInterviewAlignmentService _alignmentService;
        private readonly IMeetingService _meetingService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public InterviewAlignmentController(IInterviewAlignmentService alignmentService, IMeetingService meetingService,
            IApplicationErrorLogger applicationErrorLogger)
        {
            _alignmentService = alignmentService;
            _meetingService = meetingService;
            _applicationErrorLogger = applicationErrorLogger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> AcknowledgeTrialOnboarding(int interviewId)
        {
            try
            {
                var email = await GetAuthorizedInterviewerAsync(interviewId);
                if (email == null)
                {
                    return Json(new { success = false, message = "This email is not assigned as the interviewer for this interview." });
                }

                var saved = await _alignmentService.AcknowledgeTrialOnboardingAsync(email);
                return Json(new { success = saved });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> JoinGateOverride(int interviewId, List<string> outstanding)
        {
            try
            {
                var email = await GetAuthorizedInterviewerAsync(interviewId);
                if (email == null)
                {
                    return Json(new { success = false });
                }

                var saved = await _alignmentService.SaveJoinGateOverrideAsync(interviewId, email, outstanding);
                return Json(new { success = saved });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false });
            }
        }

        [HttpGet]
        public async Task<JsonResult> CodeQuestions(int interviewId)
        {
            try
            {
                var email = await GetAuthorizedInterviewerAsync(interviewId);
                if (email == null)
                {
                    return Json(new { success = false, message = "This email is not assigned as the interviewer for this interview." });
                }

                var (questions, sessions) = await _alignmentService.GetCodeQuestionsAsync(interviewId, email);
                return Json(new { success = true, questions = ToQuestionRows(questions), sessions = ToSessionRows(sessions) });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Could not load your questions." });
            }
        }

        /// <summary>
        /// One action, not a wizard: writes the question to the interviewer's own list when it was typed
        /// live (QuestionId = 0), marks it as needing code, mints the session and returns the link.
        /// No question gist is required - that exists only because the AI avatar narrates questions.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> CreateCodeSession(CreateCodeSessionRequestVM request)
        {
            try
            {
                if (request == null || request.InterviewId <= 0)
                {
                    return Json(new { success = false, message = "Interview not found." });
                }

                var email = await GetAuthorizedInterviewerAsync(request.InterviewId);
                if (email == null)
                {
                    return Json(new { success = false, message = "This email is not assigned as the interviewer for this interview." });
                }

                request.QuestionText = request.QuestionText?.Trim();
                if (request.QuestionId <= 0)
                {
                    request.QuestionId = 0;
                    if (string.IsNullOrWhiteSpace(request.QuestionText))
                    {
                        return Json(new { success = false, message = "Type the coding question first." });
                    }
                    if (request.QuestionText.Length > IRFController.MaxSampleQuestionLength)
                    {
                        return Json(new { success = false, message = $"Question must not exceed {IRFController.MaxSampleQuestionLength} characters." });
                    }
                }
                else
                {
                    // An existing row is minted as it stands; its text is never replaced from the request.
                    request.QuestionText = null;
                }

                if (!request.TimeLimitInMinutes.HasValue)
                {
                    request.TimeLimitInMinutes = IRFController.DefaultCodingTimeLimitInMinutes;
                }
                if (request.TimeLimitInMinutes.Value < 1 || request.TimeLimitInMinutes.Value > 180)
                {
                    return Json(new { success = false, message = "Time limit must be between 1 and 180 minutes." });
                }

                if (!string.IsNullOrWhiteSpace(request.SkillName) && request.SkillName.Trim().Length > 500)
                {
                    request.SkillName = request.SkillName.Trim().Substring(0, 500);
                }

                var result = await _alignmentService.CreateCodeSessionAsync(email, request);
                if (result == null)
                {
                    return Json(new { success = false, message = "The code session could not be created. Please try again." });
                }

                switch (result.Status)
                {
                    case "ok":
                        break;
                    case "not_owner":
                        return Json(new { success = false, message = "You can only create a code session for one of your own questions on this channel." });
                    case "empty_question":
                        return Json(new { success = false, message = "Type the coding question first." });
                    default:
                        return Json(new { success = false, message = "This email is not assigned as the interviewer for this interview." });
                }

                var (questions, sessions) = await _alignmentService.GetCodeQuestionsAsync(request.InterviewId, email);
                return Json(new
                {
                    success = true,
                    link = CandidateLink(result.Session.Token),
                    questionId = result.Session.QuestionId,
                    questions = ToQuestionRows(questions),
                    sessions = ToSessionRows(sessions)
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "The code session could not be created. Please try again." });
            }
        }

        private async Task<string> GetAuthorizedInterviewerAsync(int interviewId)
        {
            var email = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
            if (string.IsNullOrWhiteSpace(email) || interviewId <= 0)
            {
                return null;
            }

            return await _meetingService.IsInterviewerForInterview(email, interviewId) ? email : null;
        }

        private string CandidateLink(Guid token) =>
            $"{Request.Scheme}://{Request.Host}{Url.Action("Index", "CodeSession", new { token })}";

        private IEnumerable<object> ToQuestionRows(IEnumerable<InterviewerCodeQuestion> questions) =>
            (questions ?? Enumerable.Empty<InterviewerCodeQuestion>()).Select(q => new
            {
                questionId = q.QuestionId,
                comment = q.Comment,
                skillName = q.SkillName,
                isCodingQuestion = q.IsCodingQuestion,
                codingTimeLimitInMinutes = q.CodingTimeLimitInMinutes,
                hasSession = q.Token.HasValue,
                link = q.Token.HasValue ? CandidateLink(q.Token.Value) : null,
                runCount = q.RunCount ?? 0,
                opened = q.OpenedOn.HasValue,
                submitted = q.SubmittedOn.HasValue
            });

        private IEnumerable<object> ToSessionRows(IEnumerable<InterviewCodeSession> sessions) =>
            (sessions ?? Enumerable.Empty<InterviewCodeSession>()).Select(s => new
            {
                questionId = s.QuestionId,
                questionText = s.QuestionText,
                codingTimeLimitInMinutes = s.CodingTimeLimitInMinutes,
                link = CandidateLink(s.Token),
                runCount = s.RunCount,
                lastRunStatus = s.LastRunStatus,
                opened = s.OpenedOn.HasValue,
                submitted = s.SubmittedOn.HasValue
            });
    }
}
