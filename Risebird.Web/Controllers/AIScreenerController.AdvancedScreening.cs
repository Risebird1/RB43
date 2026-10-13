using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel.AdvancedScreening;
using System;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    /// <summary>
    /// Advanced Resume Screening at IRF level: the Screening Rubric page (recruiter instructions,
    /// rubric generation, editing and approval), the Screen All dialog, re-screening, and the
    /// candidate report. Scoring itself runs in the background (AdvancedScreeningWorker here, and
    /// AdvancedResumeScreeningJob in the functions host) - no action below calls the model for a
    /// candidate.
    ///
    /// The controller has [Authorize] commented out at class level, so every action here carries
    /// its own, plus the session-profile check. Allowed: Admin, HR, SeniorHR, SupportAdmin, and a
    /// Client only for an IRF it is attached to.
    /// </summary>
    public partial class AIScreenerController
    {
        private static readonly string[] AdvancedScreeningInternalProfiles = { "Admin", "HR", "SeniorHR", "SupportAdmin" };

        private (string Email, string Profile) SessionUser()
        {
            var email = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
            var profile = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
            return (email, profile);
        }

        /// <summary>The signed-in user, when they may work with this IRF's advanced screening; otherwise null.</summary>
        private async Task<string> AuthorizeAdvancedScreeningAsync(int irfId)
        {
            var (email, profile) = SessionUser();
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(profile))
            {
                return null;
            }
            if (Array.IndexOf(AdvancedScreeningInternalProfiles, profile) >= 0)
            {
                return email;
            }
            if (profile == ProfileNames.Client && irfId > 0)
            {
                return await _userRepository.GetClientUserDetailsByIRF(email, irfId) != null ? email : null;
            }
            return null;
        }

        private IActionResult Denied(bool json)
        {
            return json
                ? Json(new { success = false, message = "You do not have access to this skill channel." })
                : RedirectToAction("SignOut", "Session");
        }

        // ------------------------------------------------------------ rubric page

        /// <summary>The Screening Rubric page: recruiter instructions card, points summary and the 27-criterion editor.</summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ScreeningCriteria(int irfId)
        {
            if (await AuthorizeAdvancedScreeningAsync(irfId) == null)
            {
                return Denied(json: false);
            }
            try
            {
                var rubric = await _advancedScreeningService.GetRubricAsync(irfId);
                if (rubric == null)
                {
                    TempData["AdvScreeningError"] = "That skill channel could not be found.";
                    return RedirectToAction("Index", "IRF");
                }
                ViewBag.Counts = await _advancedScreeningService.GetCountsAsync(irfId) ?? new AdvancedScreeningCounts();
                return View(rubric);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index", "IRF");
            }
        }

        /// <summary>Saves the instructions; returns the new version and how many results they made stale.</summary>
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveScreeningInstructions(int irfId, string instructions, bool applyToRubric, bool applyToScoring)
        {
            var user = await AuthorizeAdvancedScreeningAsync(irfId);
            if (user == null)
            {
                return Denied(json: true);
            }
            var result = await _advancedScreeningService.SaveInstructionsAsync(irfId, instructions, applyToRubric, applyToScoring, user);
            return Json(new
            {
                success = result.Success,
                message = result.Error,
                instructionsVersion = result.InstructionsVersion,
                staleCount = result.StaleCount,
                editedBy = user,
                editedOn = DateTime.UtcNow.ToString("dd MMM yyyy, HH:mm") + " UTC"
            });
        }

        /// <summary>Generates (or regenerates) the rubric and returns the editor partial.</summary>
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateScreeningRubric(int irfId, string jdText)
        {
            var user = await AuthorizeAdvancedScreeningAsync(irfId);
            if (user == null)
            {
                return Denied(json: true);
            }

            // A JD the recruiter corrected on the page is saved first, so generation reads it.
            if (jdText != null)
            {
                var current = await _advancedScreeningService.GetRubricAsync(irfId);
                if (current != null && !string.Equals(current.JDText ?? string.Empty, jdText.Trim(), StringComparison.Ordinal))
                {
                    var draft = current.Rubric ?? RubricCriteria.DefaultRubric(current.JDTiltle);
                    await _advancedScreeningService.SaveRubricAsync(irfId, draft, approve: false, user, jdText.Trim());
                }
            }

            var result = await _advancedScreeningService.GenerateRubricAsync(irfId, user);
            ViewBag.GenerationError = result.Success ? null : result.Error;
            return PartialView("_ScreeningRubricEditor", result.Rubric);
        }

        /// <summary>Saves the edited rubric, or approves it (which requires 27 criteria totalling 300).</summary>
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveScreeningRubric(ScreeningRubricForm form)
        {
            var user = await AuthorizeAdvancedScreeningAsync(form?.IRFId ?? 0);
            if (user == null)
            {
                return Denied(json: false);
            }
            var result = await _advancedScreeningService.SaveRubricAsync(form.IRFId, form.ToRubric(), form.Approve, user, form.JDText);
            if (result.Success)
            {
                TempData["AdvScreeningMessage"] = form.Approve
                    ? $"Rubric approved (v{result.Rubric?.RubricVersion}). New candidates are scored against it automatically; use Screen All Resumes to score existing ones."
                    : "Rubric saved as a draft. Approve it to start screening against it.";
            }
            else
            {
                TempData["AdvScreeningError"] = result.Error;
            }
            return RedirectToAction(nameof(ScreeningCriteria), new { irfId = form.IRFId });
        }

        // -------------------------------------------------------------- screening

        /// <summary>The Screen All Resumes dialog: counts and the current instructions.</summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ScreenAllSummary(int irfId)
        {
            if (await AuthorizeAdvancedScreeningAsync(irfId) == null)
            {
                return Denied(json: true);
            }
            var rubric = await _advancedScreeningService.GetRubricAsync(irfId);
            if (rubric == null)
            {
                return Json(new { success = false, message = "That skill channel could not be found." });
            }
            return PartialView("_ScreenAllDialog", new ScreenAllDialogVM
            {
                IRFId = irfId,
                IRFName = rubric.IRFName,
                IsAdvancedResumeScreening = rubric.IsAdvancedResumeScreening,
                IsRubricApproved = rubric.IsApproved,
                RubricVersion = rubric.RubricVersion,
                InstructionsVersion = rubric.InstructionsVersion,
                Instructions = rubric.Instructions,
                ApplyInstructionsToScoring = rubric.ApplyInstructionsToScoring,
                Counts = await _advancedScreeningService.GetCountsAsync(irfId) ?? new AdvancedScreeningCounts()
            });
        }

        /// <summary>Queues candidates; the background worker and the job do the screening.</summary>
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScreenAllResumes(int irfId, ScreenAllScope scope)
        {
            var user = await AuthorizeAdvancedScreeningAsync(irfId);
            if (user == null)
            {
                return Denied(json: true);
            }
            var problem = await ScreeningBlockerAsync(irfId);
            if (problem != null)
            {
                return Json(new { success = false, message = problem });
            }

            var queued = await _advancedScreeningService.QueueAsync(irfId, scope, null, user);
            if (queued < 0)
            {
                return Json(new { success = false, message = "The candidates could not be queued. Try again in a moment." });
            }
            if (queued > 0)
            {
                _advancedScreeningSignal.Kick();
            }
            return Json(new
            {
                success = true,
                queued,
                message = queued == 0
                    ? "There is nobody to screen for that choice."
                    : $"Screening started for {queued} candidate(s). It runs in the background; results appear in the AI Verdict column as they finish."
            });
        }

        /// <summary>Queues one candidate.</summary>
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RescreenCandidate(int candidateId)
        {
            var candidate = await _advancedScreeningService.GetCandidateResultAsync(candidateId);
            if (candidate == null)
            {
                return Json(new { success = false, message = "That candidate could not be found." });
            }
            var user = await AuthorizeAdvancedScreeningAsync(candidate.IRFId);
            if (user == null)
            {
                return Denied(json: true);
            }
            var problem = await ScreeningBlockerAsync(candidate.IRFId);
            if (problem != null)
            {
                return Json(new { success = false, message = problem });
            }
            if (string.IsNullOrWhiteSpace(candidate.ResumeLink))
            {
                return Json(new { success = false, message = "This candidate has no resume on file." });
            }

            var queued = await _advancedScreeningService.QueueAsync(candidate.IRFId, ScreenAllScope.All, candidateId, user);
            if (queued < 0)
            {
                return Json(new { success = false, message = "The candidate could not be queued. Try again in a moment." });
            }
            _advancedScreeningSignal.Kick();
            return Json(new
            {
                success = true,
                message = queued == 0 ? "This candidate is already being screened." : "Re-screening started. The result appears in a minute or two."
            });
        }

        /// <summary>Why screening cannot run on this IRF yet, or null when it can.</summary>
        private async Task<string> ScreeningBlockerAsync(int irfId)
        {
            var rubric = await _advancedScreeningService.GetRubricAsync(irfId);
            if (rubric == null)
            {
                return "That skill channel could not be found.";
            }
            if (!rubric.IsAdvancedResumeScreening)
            {
                return "Advanced Resume Screening is not switched on for this skill channel. Turn it on in Edit IRF.";
            }
            if (!rubric.IsApproved)
            {
                return "Approve the screening rubric first (Screening Rubric page).";
            }
            return null;
        }

        // ----------------------------------------------------------------- report

        /// <summary>The screening report for one candidate.</summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> AdvancedScreenCandidate(int candidateId)
        {
            var candidate = await _advancedScreeningService.GetCandidateResultAsync(candidateId);
            if (candidate == null)
            {
                return NotFound();
            }
            if (await AuthorizeAdvancedScreeningAsync(candidate.IRFId) == null)
            {
                return Denied(json: false);
            }
            return View(candidate);
        }
    }
}
