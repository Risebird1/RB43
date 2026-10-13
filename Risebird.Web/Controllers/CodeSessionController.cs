using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    /// <summary>
    /// The candidate's side of a per-question code session in a human-led interview. The candidate is
    /// not signed in: the token in the link is the credential, valid for one interview and one question,
    /// and it expires with the interview.
    ///
    /// Run is parallel to AIInterviewController.RunCode - same ICodeExecutionService, same IsEnabled gate,
    /// same timeout and output cap - but validates the code-session token instead of an AI interview
    /// session, and persists what comes back. RunCode and its auth are untouched.
    /// </summary>
    [AllowAnonymous]
    public class CodeSessionController : Controller
    {
        private readonly IInterviewAlignmentService _alignmentService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public CodeSessionController(IInterviewAlignmentService alignmentService, IApplicationErrorLogger applicationErrorLogger)
        {
            _alignmentService = alignmentService;
            _applicationErrorLogger = applicationErrorLogger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(Guid token)
        {
            // The token is in the URL: keep it out of Referer headers and search indexes.
            Response.Headers["Referrer-Policy"] = "no-referrer";
            Response.Headers["X-Robots-Tag"] = "noindex, nofollow";

            CodeSessionPageViewModel model = null;
            try
            {
                if (token != Guid.Empty)
                {
                    model = await _alignmentService.GetCandidatePageAsync(token);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            return View(model);
        }

        [HttpPost]
        [EnableRateLimiting("CodeSession")]
        public async Task<JsonResult> Run([FromBody] CodeSessionRunRequestVM model)
        {
            try
            {
                var (accepted, message, result) = await _alignmentService.RunAsync(model);
                if (!accepted)
                {
                    return Json(new { success = false, output = message });
                }

                return Json(new
                {
                    success = result.Success,
                    status = result.Status,
                    output = result.CombinedOutput,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, output = "The code runner is temporarily unavailable. Your code is safe — you can still submit it." });
            }
        }

        [HttpPost]
        [EnableRateLimiting("CodeSession")]
        public async Task<JsonResult> Submit([FromBody] CodeSessionSubmitRequestVM model)
        {
            try
            {
                var (accepted, message, submittedOn) = await _alignmentService.SubmitAsync(model);
                return Json(new
                {
                    success = accepted,
                    message,
                    submittedOn = submittedOn?.ToString("dd MMM yyyy, HH:mm")
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Your answer could not be saved. Please try again." });
            }
        }
    }
}
