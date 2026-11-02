using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    /// <summary>
    /// WhatsApp opt-outs (dbo.WhatsAppOptOut): find who unsubscribed from WhatsApp, which message they unsubscribed
    /// from and why, and re-enable them - the hand-off's "Manual re-enable" admin action. A re-enable is stamped on
    /// the row (kept as history) and also written to the application log. Allowed: Admin, SupportAdmin.
    /// </summary>
    [Authorize]
    [Route("WhatsAppOptOut")]
    public class WhatsAppOptOutController : Controller
    {
        private static readonly string[] AllowedProfiles = { "Admin", "SupportAdmin" };

        private readonly IWhatsappRepository     _whatsappRepository;
        private readonly IApplicationErrorLogger _logger;

        public WhatsAppOptOutController(IWhatsappRepository whatsappRepository, IApplicationErrorLogger logger)
        {
            _whatsappRepository = whatsappRepository;
            _logger             = logger;
        }

        private string CurrentUser()
        {
            var email = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
            var profile = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
            if (string.IsNullOrWhiteSpace(email) || Array.IndexOf(AllowedProfiles, profile) < 0) return null;
            return email;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        /// <param name="history">Also show opt-outs that were re-enabled.</param>
        public async Task<IActionResult> Index(string search = null, bool history = false)
        {
            if (CurrentUser() == null) return RedirectToAction("SignOut", "Session");
            ViewBag.Search = search;
            ViewBag.History = history;
            return View(await _whatsappRepository.SearchOptOutsAsync(search, history));
        }

        /// <summary>
        /// Re-enables WhatsApp for one opt-out: the row is stamped ReEnabledAt / ReEnabledBy and kept as history, and
        /// a cold mark the unsubscribe made is undone. Emails were never affected.
        /// </summary>
        [HttpPost("ReEnable")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReEnable(int id)
        {
            var user = CurrentUser();
            if (user == null) return Json(new { ok = false, message = "You do not have access to WhatsApp opt-outs." });

            var row = await _whatsappRepository.GetOptOutAsync(id);
            if (row == null) return Json(new { ok = false, message = "That opt-out no longer exists." });
            if (!row.IsActive) return Json(new { ok = false, message = "Already re-enabled by " + row.ReEnabledBy + "." });
            if (!await _whatsappRepository.ReEnableOptOutAsync(id, user)) return Json(new { ok = false, message = "The opt-out could not be re-enabled." });

            bool candidate = row.Audience == WhatsAppHelper.AudienceCandidate;
            if (candidate)
                await _whatsappRepository.UndoCandidateColdAsync(row.EmailId, row.IRFId, user);
            else if (row.Audience == WhatsAppHelper.AudienceInterviewer)
                await _whatsappRepository.UndoInterviewerChurnAsync(row.EmailId);

            var who = row.Audience + " " + (row.EmailId ?? ("prospect " + row.ProspectId)) + (row.IRFId.HasValue ? " for IRF " + (row.IRFName ?? row.IRFId.ToString()) : " (all roles)");
            await _logger.LogMessage(LogLevelInfo.Info,
                "WhatsApp re-enabled (manual): opt-out #" + row.Id + " of " + who + ", source " + row.Source + ", opted out " + row.CreatedAt.ToString("u") + "; by " + user,
                user);
            return Json(new
            {
                ok = true,
                message = "WhatsApp re-enabled for " + who + "." + (candidate
                    ? " A cold mark from the unsubscribe was undone; a cold mark set by a person is left as it is."
                    : row.Audience == WhatsAppHelper.AudienceInterviewer ? " Removed from Interviewer Churn Data (their availability was never changed)." : "")
            });
        }
    }
}
