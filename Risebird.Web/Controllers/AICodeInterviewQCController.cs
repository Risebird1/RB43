using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    /// <summary>
    /// Expert review of AI code interview reports. Items reach this controller from the Open QC
    /// screen, which is the delivery vehicle for tech QC generally.
    /// </summary>
    [Authorize]
    public class AICodeInterviewQCController : Controller
    {
        /// <summary>
        /// Profiles that may assign an audit to any expert and read a submitted audit. Skill channel
        /// SMEs reach the same audit by assigning it to themselves from the Open QC screen.
        /// </summary>
        private static readonly string[] QCAdminProfiles =
        {
            ProfileNames.Admin,
            ProfileNames.SeniorHR,
            ProfileNames.SupportAdmin,
            ProfileNames.QualityController
        };

        private readonly IAICodeInterviewService _aiCodeInterviewService;
        private readonly IAIInterviewerService _aiInterviewerService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public AICodeInterviewQCController(IAICodeInterviewService aiCodeInterviewService,
            IAIInterviewerService aiInterviewerService,
            IApplicationErrorLogger applicationErrorLogger)
        {
            _aiCodeInterviewService = aiCodeInterviewService;
            _aiInterviewerService = aiInterviewerService;
            _applicationErrorLogger = applicationErrorLogger;
        }

        /// <summary>
        /// Assigns an AI code interview audit. QC admin profiles may name the expert; an SME
        /// self-assigning from the Open QC screen may only assign themselves.
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> AssignAICodeInterviewExpert(Guid sid, string smeEmail = null)
        {
            try
            {
                var currentUser = GetSessionValue("email");
                var profileName = GetSessionValue("profileName");
                if (string.IsNullOrWhiteSpace(currentUser) || string.IsNullOrWhiteSpace(profileName))
                {
                    return Json(new { success = false, message = "Your session has expired. Please sign in again." });
                }

                var isQCAdmin = QCAdminProfiles.Contains(profileName);
                string expertEmail;

                if (isQCAdmin)
                {
                    expertEmail = string.IsNullOrWhiteSpace(smeEmail) ? currentUser : smeEmail.Trim();
                }
                else if (profileName == ProfileNames.Interviewer)
                {
                    // Self-assignment only: an SME cannot hand the audit to somebody else.
                    expertEmail = currentUser;
                }
                else
                {
                    return Json(new { success = false, message = "You are not allowed to assign this review." });
                }

                var result = await _aiCodeInterviewService.AssignExpert(sid, expertEmail, currentUser);

                switch (result.Status)
                {
                    case 1:
                    case 2:
                        return Json(new
                        {
                            success = true,
                            message = result.Status == 1 ? "Assigned Successfully." : "This review is already assigned to you.",
                            redirectUrl = Url.Action("ExpertReport", "AICodeInterviewQC", new { seId = sid })
                        });
                    case 3:
                        return Json(new { success = false, message = "This review has already been picked up by another expert." });
                    default:
                        return Json(new { success = false, message = "This interview is not available for expert review." });
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Sorry! Could not assign." });
            }
        }

        /// <summary>
        /// The expert copy of the AI report: an editable form pre-filled with the AI's values, with
        /// every changed field marked against the AI baseline.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExpertReport(Guid seId)
        {
            try
            {
                var currentUser = GetSessionValue("email");
                var profileName = GetSessionValue("profileName");
                if (string.IsNullOrWhiteSpace(currentUser) || string.IsNullOrWhiteSpace(profileName))
                {
                    return RedirectToAction("SignOut", "Session");
                }

                var audit = await _aiCodeInterviewService.GetAuditReport(seId);
                if (audit?.AIReport == null)
                {
                    ViewBag.Message = "The AI report for this interview is not available for expert review.";
                    return View("ExpertReportUnavailable");
                }

                var isAssignedExpert = audit.Assignment != null
                    && string.Equals(audit.Assignment.ExpertEmail, currentUser, StringComparison.OrdinalIgnoreCase);

                if (!isAssignedExpert && !QCAdminProfiles.Contains(profileName))
                {
                    return RedirectToAction("QCOpenInterviews", "Interview");
                }

                // Only the assigned expert can edit; resubmission stays open so they can revise.
                audit.CanEdit = isAssignedExpert;

                await AttachRecordedAnswerMedia(seId, audit);
                return View(audit);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("QCOpenInterviews", "Interview");
            }
        }

        /// <summary>
        /// Stores the expert copy and recomputes the discrepancies. ReportSource is forced to the
        /// expert copy inside the service, so a posted value cannot overwrite the AI baseline.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAICodeInterviewExpertAudit(AICodeInterviewReportVM model)
        {
            try
            {
                var currentUser = GetSessionValue("email");
                if (string.IsNullOrWhiteSpace(currentUser))
                {
                    return RedirectToAction("SignOut", "Session");
                }

                if (model == null || !Guid.TryParse(model.SessionId, out var sessionId))
                {
                    TempData["SuccessErrorMessage"] = "The expert review could not be submitted.";
                    return RedirectToAction("QCOpenInterviews", "Interview");
                }

                var audit = await _aiCodeInterviewService.GetAuditReport(sessionId);
                var isAssignedExpert = audit?.Assignment != null
                    && string.Equals(audit.Assignment.ExpertEmail, currentUser, StringComparison.OrdinalIgnoreCase);

                if (!isAssignedExpert)
                {
                    TempData["SuccessErrorMessage"] = "This review is not assigned to you.";
                    return RedirectToAction("QCOpenInterviews", "Interview");
                }

                var result = await _aiCodeInterviewService.SubmitExpertAudit(model, currentUser);
                TempData["SuccessErrorMessage"] = result.Message;

                if (!result.Success)
                {
                    return RedirectToAction("ExpertReport", new { seId = sessionId });
                }

                return RedirectToAction("ExpertReport", new { seId = sessionId });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "The expert review could not be submitted.";
                return RedirectToAction("QCOpenInterviews", "Interview");
            }
        }

        /// <summary>
        /// Fills in the answer and question video URLs for each question row so the expert edits the
        /// transcript next to the recording it came from.
        /// </summary>
        private async Task AttachRecordedAnswerMedia(Guid sessionId, AICodeInterviewAuditReportVM audit)
        {
            var answers = await _aiInterviewerService.GetBotInterviewVideoDetails(sessionId)
                          ?? new List<BotInterviewQuestionVideos>();
            if (answers.Count == 0) return;

            AttachMedia(audit.AIReport, answers);
            AttachMedia(audit.ExpertReport, answers);
        }

        private static void AttachMedia(AICodeInterviewReportVM report, List<BotInterviewQuestionVideos> answers)
        {
            foreach (var row in report?.QuestionRatings ?? new List<AICodeInterviewReportQuestionRatingVM>())
            {
                var answer = answers.FirstOrDefault(a => a.id == row.BotInterviewQuestionVideoId)
                             ?? (row.DisplayOrder < answers.Count ? answers[row.DisplayOrder] : null);
                if (answer == null) continue;

                row.VideoLink = answer.VideoLink;
                row.QuestionVideoLink = string.IsNullOrWhiteSpace(answer.AIQuestionVideoLink)
                    ? answer.BotSynthesizedVideoLink
                    : answer.AIQuestionVideoLink;
            }
        }

        private string GetSessionValue(string key)
        {
            var value = HttpContext.Session.GetString(key);
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim().Replace("\"", string.Empty);
        }
    }
}
