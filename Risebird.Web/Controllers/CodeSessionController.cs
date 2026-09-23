using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;

namespace Risebird.Web.Controllers
{
    /// <summary>
    /// The candidate's side of a per-question code session.
    ///
    /// Every action here is unauthenticated by design - the candidate has no account and
    /// never signs in. The SESSION TOKEN is the entire security boundary, so each action
    /// resolves exactly one session by token and exposes nothing else: not the candidate,
    /// not the channel, not the sibling sessions in the same interview. There is no action
    /// on this controller that takes an interview id or lists anything.
    ///
    /// A token stops working when the interview ends; the stored procedures enforce that,
    /// not this controller.
    /// </summary>
    [AllowAnonymous]
    public class CodeSessionController : Controller
    {
        private readonly IMeetingService _meetingService;
        private readonly ICodeExecutionService _codeExecutionService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public CodeSessionController(IMeetingService meetingService,
            ICodeExecutionService codeExecutionService,
            IApplicationErrorLogger applicationErrorLogger)
        {
            _meetingService = meetingService;
            _codeExecutionService = codeExecutionService;
            _applicationErrorLogger = applicationErrorLogger;
        }

        /// <summary>
        /// The candidate's page: one question and an editor. GET /CodeSession/Index?t={token}
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string t)
        {
            try
            {
                var model = await _meetingService.GetCodeSessionByToken(t);

                // Whether a token is unknown, expired or malformed, the page says the same
                // neutral thing. Distinguishing them would let someone probe for valid tokens.
                ViewBag.RunnerAvailable = _codeExecutionService != null && _codeExecutionService.IsEnabled;
                ViewBag.Languages = CodeLanguageCatalog.Languages;
                return View(model ?? new CodeSessionCandidateVM { IsValid = false });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                ViewBag.RunnerAvailable = false;
                ViewBag.Languages = CodeLanguageCatalog.Languages;
                return View(new CodeSessionCandidateVM { IsValid = false });
            }
        }

        /// <summary>
        /// Debounced autosave from the editor. Capture must survive a closed tab, a refresh
        /// or lost connectivity, so this writes the whole buffer every time rather than
        /// trying to be clever about deltas.
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> Save([FromBody] CodeSessionSaveRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Token))
                {
                    return Json(new { saved = false });
                }

                var saved = await _meetingService.SaveCodeSessionProgress(
                    request.Token, request.Code, request.Language, request.PasteEvents);

                return Json(new { saved });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { saved = false });
            }
        }

        /// <summary>
        /// Runs the code and stores the result. A failure to reach the runner is reported
        /// honestly rather than as an empty result - the candidate can still submit.
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> Run([FromBody] CodeSessionSaveRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Token))
                {
                    return Json(new { ran = false, output = "This session is no longer available." });
                }

                var result = await _meetingService.RunCodeSession(
                    request.Token, request.Code, request.Language, request.PasteEvents);

                // DidNotRun is checked as well as the status string: the execution service
                // reports an unreachable, refusing or rate-limited runner with Status = "Error",
                // and the candidate should be told the run did not happen rather than shown an
                // empty output panel that reads as their program having printed nothing.
                if (result == null
                    || result.DidNotRun
                    || string.Equals(result.Status, "NotExecuted", StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new
                    {
                        ran = false,
                        notExecuted = true,
                        // The service's own message says which of the failure modes it was, which
                        // is more use than a blanket line. The generic wording is the fallback.
                        output = result != null && !string.IsNullOrWhiteSpace(result.Message)
                            ? result.Message
                            : "Running code is not available right now. You can still write and submit your answer - it will be recorded either way."
                    });
                }

                return Json(new
                {
                    ran = true,
                    success = result.Success,
                    status = result.Status,
                    stdout = result.Stdout,
                    stderr = string.IsNullOrWhiteSpace(result.Stderr) ? result.CompileOutput : result.Stderr,
                    output = result.CombinedOutput
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new
                {
                    ran = false,
                    notExecuted = true,
                    output = "The code runner is temporarily unavailable. Your code is safe - you can still submit it."
                });
            }
        }

        /// <summary>
        /// Submits. The service re-runs once here so the stored output always matches the
        /// submitted code, then the session freezes.
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> Submit([FromBody] CodeSessionSaveRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Token))
                {
                    return Json(new { submitted = false });
                }

                var submitted = await _meetingService.SubmitCodeSession(
                    request.Token, request.Code, request.Language, request.PasteEvents);

                return Json(new { submitted });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { submitted = false });
            }
        }
    }

    /// <summary>The candidate posts only these. No interview id, no session id.</summary>
    public class CodeSessionSaveRequest
    {
        public string Token { get; set; }
        public string Code { get; set; }
        public string Language { get; set; }
        public int PasteEvents { get; set; }
    }
}
