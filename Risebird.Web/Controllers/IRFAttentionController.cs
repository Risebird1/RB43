using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Risebird.Web.Controllers
{
    /// <summary>
    /// IRF Attention dashboard — any authenticated Risebird user (D1). No role gate.
    /// </summary>
    [Authorize]
    public class IRFAttentionController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly IInterviewerService _interviewerService;
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IConfiguration _configuration;

        public IRFAttentionController(
            IDashboardService dashboardService,
            IInterviewerService interviewerService,
            IEncryptionHelper encryptionHelper,
            IApplicationErrorLogger applicationErrorLogger,
            IConfiguration configuration)
        {
            _dashboardService = dashboardService;
            _interviewerService = interviewerService;
            _encryptionHelper = encryptionHelper;
            _applicationErrorLogger = applicationErrorLogger;
            _configuration = configuration;
        }

        private bool TryGetSessionUser(out string email)
        {
            email = HttpContext.Session.GetString("email")?.Trim()?.Replace("\"", "");
            var profile = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "");
            return !string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(profile);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                if (!TryGetSessionUser(out _))
                    return RedirectToAction("SignOut", "Session");

                var page = await BuildPageAsync();
                return View(page);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return View(new IRFAttentionPageVM());
            }
        }

        private async Task<IRFAttentionPageVM> BuildPageAsync()
        {
            int? threshold = null;
            var thresholdRaw = _configuration["AttentionThreshold"];
            if (!string.IsNullOrWhiteSpace(thresholdRaw) && int.TryParse(thresholdRaw, out var t) && t > 0)
                threshold = t;

            var volumeRows = (await _dashboardService.GetIRFAttentionVolumeRows() ?? Enumerable.Empty<IRFAttentionRowVM>()).ToList();
            var volumeById = volumeRows.ToDictionary(x => x.IrfPk, x => x);

            var spotlights = (await _dashboardService.GetActiveIRFAttentionSpotlights() ?? Enumerable.Empty<IRFAttentionSpotlightVM>()).ToList();
            var spotlightIds = spotlights.Select(x => x.IrfId).Where(id => id > 0).Distinct().ToList();

            var missingMetaIds = spotlightIds.Where(id => !volumeById.ContainsKey(id)).ToList();
            if (missingMetaIds.Count > 0)
            {
                var meta = await _dashboardService.GetIRFAttentionMetaByIds(missingMetaIds) ?? Enumerable.Empty<IRFAttentionRowVM>();
                foreach (var m in meta)
                {
                    if (m != null && m.IrfPk > 0 && !volumeById.ContainsKey(m.IrfPk))
                        volumeById[m.IrfPk] = m;
                }
            }

            var spotlightRows = new List<IRFAttentionRowVM>();
            foreach (var pin in spotlights)
            {
                if (!volumeById.TryGetValue(pin.IrfId, out var row))
                {
                    row = new IRFAttentionRowVM { IrfPk = pin.IrfId, IrfCode = "#" + pin.IrfId };
                }
                else
                {
                    row = new IRFAttentionRowVM
                    {
                        IrfPk = row.IrfPk,
                        IrfCode = row.IrfCode,
                        PrimarySkill = row.PrimarySkill,
                        MagId = row.MagId,
                        MagName = row.MagName,
                        Volume = row.Volume,
                        UnassignedCount = row.UnassignedCount
                    };
                }
                row.IsSpotlight = true;
                row.PinnedBy = pin.PinnedBy;
                row.PinnedAt = pin.PinnedAt;
                spotlightRows.Add(row);
            }

            var spotlightSet = spotlightIds.ToHashSet();
            var attention = volumeRows
                .Where(x => !spotlightSet.Contains(x.IrfPk))
                .Where(x => !threshold.HasValue || x.Volume >= threshold.Value)
                .OrderByDescending(x => x.Volume)
                .ToList();

            var mags = (_interviewerService.FetchAssessmentGroups() ?? Enumerable.Empty<AssignmentVM>())
                .Where(x => x != null && x.id > 0 && !string.IsNullOrWhiteSpace(x.AssessmentGroup))
                .OrderBy(x => x.AssessmentGroup)
                .ToList();

            return new IRFAttentionPageVM
            {
                Spotlight = spotlightRows,
                Attention = attention,
                AttentionThreshold = threshold,
                MagList = mags
            };
        }

        [HttpPost]
        public async Task<JsonResult> Pin(int irfId)
        {
            try
            {
                if (!TryGetSessionUser(out var email))
                    return Json(new { ok = false, message = "Unauthorized." });
                if (irfId <= 0)
                    return Json(new { ok = false, message = "Invalid IRF." });

                var ok = await _dashboardService.PinIRFAttentionSpotlight(irfId, email);
                return Json(new { ok, message = ok ? "Pinned." : "Could not pin." });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, message = "Could not pin." });
            }
        }

        [HttpPost]
        public async Task<JsonResult> Unpin(int irfId)
        {
            try
            {
                if (!TryGetSessionUser(out var email))
                    return Json(new { ok = false, message = "Unauthorized." });
                if (irfId <= 0)
                    return Json(new { ok = false, message = "Invalid IRF." });

                var ok = await _dashboardService.UnpinIRFAttentionSpotlight(irfId, email);
                return Json(new { ok, message = ok ? "Unpinned." : "Could not unpin." });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, message = "Could not unpin." });
            }
        }

        /// <summary>Cross-MAG entry link for a target MAG — non-expiring (D5).</summary>
        [HttpPost]
        public async Task<JsonResult> GenerateMAGApplicationLink(int targetMagId)
        {
            try
            {
                if (!TryGetSessionUser(out _))
                    return Json(new { ok = false, message = "Unauthorized." });
                if (targetMagId <= 0)
                    return Json(new { ok = false, message = "Select a target MAG." });

                var encryptedId = await _encryptionHelper.EncryptStringValue(targetMagId.ToString());
                var url = "https://portal.risebird.io/Interviewer/MAGApply?id=" + HttpUtility.UrlEncode(encryptedId);
                return Json(new { ok = true, url });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, message = "Could not generate link." });
            }
        }
    }
}
