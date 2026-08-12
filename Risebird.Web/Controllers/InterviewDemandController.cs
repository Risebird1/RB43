using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Constants;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class InterviewDemandController : Controller
    {
        private readonly IIRFService _irfService;
        private readonly IDashboardService _dashboardService;
        private readonly IInterviewerService _interviewerService;
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IInterviewerEmailAvailabilityRepository _eaRepository;

        public InterviewDemandController(
            IIRFService irfService,
            IDashboardService dashboardService,
            IInterviewerService interviewerService,
            IEncryptionHelper encryptionHelper,
            IConfiguration configuration,
            IApplicationErrorLogger applicationErrorLogger,
            IInterviewerEmailAvailabilityRepository eaRepository)
        {
            _irfService = irfService;
            _dashboardService = dashboardService;
            _interviewerService = interviewerService;
            _encryptionHelper = encryptionHelper;
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _eaRepository = eaRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!TryGetSessionUser(out _))
            {
                return RedirectToAction("SignOut", "Session");
            }

            ViewData["Title"] = "Weekend Drive";
            ViewBag.PageHeader = "Skills Channel / Weekend Drive";
            try
            {
                var data = await BuildDriveWeekendModelAsync("This");
                return View(data);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                var (sat, _) = GetWeekendRange("This");
                return View(new DriveWeekendDataVM { WeekendStart = sat });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DriveWeekendData(string weekend)
        {
            try
            {
                if (!TryGetSessionUser(out _))
                {
                    return PartialView("_DriveWeekendData", new DriveWeekendDataVM());
                }

                var data = await BuildDriveWeekendModelAsync(weekend);
                return PartialView("_DriveWeekendData", data);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                var (sat, _) = GetWeekendRange(weekend);
                return PartialView("_DriveWeekendData", new DriveWeekendDataVM { WeekendStart = sat });
            }
        }

        private async Task<DriveWeekendDataVM> BuildDriveWeekendModelAsync(string weekend)
        {
            var (sat, end) = GetWeekendRange(weekend);
            int threshold = _configuration.GetValue<int>("DriveThreshold", 5);
            var bucket = string.Equals(weekend, "Next", StringComparison.OrdinalIgnoreCase) ? "Next" : "This";

            var interviews = _irfService.GetWeekendInterviewList(sat, end) ?? new List<InterviewVM>();
            var stats = new DriveWeekendLoadStats
            {
                Threshold = threshold,
                TotalInterviews = interviews.Count,
                TotalIrfGroups = interviews
                    .Where(x => !string.IsNullOrWhiteSpace(x.IRFID))
                    .Select(x => x.IRFID.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count()
            };

            var interviewGroups = interviews
                .Where(x => !string.IsNullOrWhiteSpace(x.IRFID))
                .GroupBy(x => x.IRFID.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToList();

            var irfIdStrings = interviewGroups
                .Select(g => g.Key)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var metadataMap = _irfService.GetDriveIRFMetadata(irfIdStrings)
                ?? new Dictionary<string, DriveIRFMetadataRecord>(StringComparer.OrdinalIgnoreCase);

            var allRows = interviewGroups
                .Select(g => BuildDriveRow(g, metadataMap, sat))
                .OrderByDescending(m => m.ExpectedTotal)
                .ToList();

            EnrichMissingMetadata(allRows);

            var spotlight = (await _dashboardService.GetWeekendDriveSpotlight(bucket))?.ToList()
                ?? new List<WeekendDriveSpotlightVM>();
            var spotlightIds = spotlight.Select(s => s.IRFId).Where(id => id > 0).Distinct().ToList();
            var spotlightActions = spotlight
                .Where(s => s.IRFId > 0)
                .GroupBy(s => s.IRFId)
                .ToDictionary(g => g.Key, g => g.First().ActionsTaken);

            // Spotlight picker options: weekend IRFs only (avoid loading every skill channel on page load).
            var optionMap = new Dictionary<int, DriveSpotlightOptionVM>();
            foreach (var row in allRows.Where(r => r.IrfIntId > 0))
            {
                optionMap[row.IrfIntId] = new DriveSpotlightOptionVM
                {
                    IrfIntId = row.IrfIntId,
                    IRFId = row.IRFId,
                    Demand = row.ExpectedTotal,
                    BelowThreshold = row.ExpectedTotal < threshold,
                    Label = BuildSpotlightLabel(row.IRFId, row.PrimarySkill, row.ExpectedTotal, row.ExpectedTotal < threshold)
                };
            }

            // Ensure currently spotlighted IRFs appear in the picker even if missing from weekend demand
            foreach (var sid in spotlightIds.Where(id => !optionMap.ContainsKey(id)))
            {
                optionMap[sid] = new DriveSpotlightOptionVM
                {
                    IrfIntId = sid,
                    IRFId = sid.ToString(),
                    Demand = 0,
                    BelowThreshold = true,
                    Label = "IRF #" + sid + " (spotlight)"
                };
            }

            var spotlightOptions = optionMap.Values
                .OrderByDescending(o => o.Demand)
                .ThenBy(o => o.Label, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Spotlight curated → those rows are the planner.
            // No spotlight yet → show threshold-met IRFs (matches ref Weekend Drive UI).
            var spotlightIdSet = new HashSet<int>(spotlightIds);
            var byIntId = allRows.Where(r => r.IrfIntId > 0).GroupBy(r => r.IrfIntId).ToDictionary(g => g.Key, g => g.First());
            var hasSpotlight = spotlightIdSet.Count > 0;
            var driveRows = new List<DriveIRFVM>();

            if (hasSpotlight)
            {
                foreach (var sid in spotlightIds)
                {
                    if (byIntId.TryGetValue(sid, out var existing))
                    {
                        driveRows.Add(existing);
                    }
                    else
                    {
                        var opt = optionMap.TryGetValue(sid, out var o) ? o : null;
                        driveRows.Add(new DriveIRFVM
                        {
                            IrfIntId = sid,
                            IRFId = opt?.IRFId ?? sid.ToString(),
                            PrimarySkill = opt?.Label,
                            WeekendStart = sat,
                            WeekendBucket = bucket,
                            ExpectedMorning = 0,
                            ExpectedEvening = 0,
                            UnassignedMorning = 0,
                            UnassignedEvening = 0
                        });
                    }
                }
            }
            else
            {
                driveRows = allRows
                    .Where(r => r.ExpectedTotal >= threshold)
                    .OrderByDescending(r => r.ExpectedTotal)
                    .ToList();
            }

            EnrichMissingMetadata(driveRows);

            // Suggestions: only when spotlight is curated (otherwise threshold rows already fill the planner)
            var suggestedThresholdRows = hasSpotlight
                ? allRows
                    .Where(r => r.IrfIntId > 0 && !spotlightIdSet.Contains(r.IrfIntId) && r.ExpectedTotal >= threshold)
                    .OrderByDescending(r => r.ExpectedTotal)
                    .ToList()
                : new List<DriveIRFVM>();

            // Cap below-threshold list — do not run supply enrichment on it (that was the main slowdown)
            const int belowCap = 25;
            var belowThresholdRows = allRows
                .Where(r => r.IrfIntId > 0 && !spotlightIdSet.Contains(r.IrfIntId) && r.ExpectedTotal < threshold)
                .OrderByDescending(r => r.ExpectedTotal)
                .Take(belowCap)
                .ToList();

            stats.QualifyingIrfCount = hasSpotlight
                ? suggestedThresholdRows.Count + driveRows.Count(r => r.ExpectedTotal >= threshold)
                : driveRows.Count;
            stats.BelowThresholdIrfCount = belowThresholdRows.Count;

            var actionRows = driveRows.Concat(suggestedThresholdRows).ToList();
            var actionRefIds = actionRows
                .Where(m => !string.IsNullOrWhiteSpace(m.IRFId))
                .Select(m => DriveWeekendActionRefs.BuildRefId(m.IRFId, sat))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var actionsTakenMap = _dashboardService.GetRowActions(DriveWeekendActionRefs.RefType, actionRefIds)
                ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in actionRows)
            {
                if (row.IrfIntId > 0 && spotlightActions.TryGetValue(row.IrfIntId, out var spotTaken)
                    && !string.IsNullOrWhiteSpace(spotTaken))
                {
                    row.ActionsTaken = spotTaken;
                }
                else if (!string.IsNullOrWhiteSpace(row.IRFId))
                {
                    var refId = DriveWeekendActionRefs.BuildRefId(row.IRFId, sat);
                    if (actionsTakenMap.TryGetValue(refId, out var actionsTaken))
                        row.ActionsTaken = actionsTaken;
                }
            }

            // Supply counts only for planner (+ small suggested list) — parallel + MAG cache
            await EnrichSupplyCountsAsync(actionRows);

            foreach (var row in driveRows.Concat(suggestedThresholdRows).Concat(belowThresholdRows))
            {
                row.WeekendBucket = bucket;
                row.WeekendStart = sat;
            }

            var page = new DriveWeekendDataVM
            {
                Rows = driveRows.OrderByDescending(r => r.ExpectedTotal).ToList(),
                SuggestedThresholdRows = suggestedThresholdRows,
                BelowThresholdRows = belowThresholdRows,
                WeekendStart = sat,
                Stats = stats,
                WeekendBucket = bucket,
                SelectedSpotlightIds = spotlightIds,
                SpotlightOptions = spotlightOptions
            };

            await AttachDemandEnhancementsAsync(page);
            return page;
        }

        /// <summary>
        /// D2 overlay (all-future counts) + D3/D4/D6 volume section.
        /// Does not change how the attention/planner view selects IRFs.
        /// </summary>
        private async Task AttachDemandEnhancementsAsync(DriveWeekendDataVM page)
        {
            if (page == null)
                return;

            page.Rows ??= new List<DriveIRFVM>();
            page.SuggestedThresholdRows ??= new List<DriveIRFVM>();
            page.BelowThresholdRows ??= new List<DriveIRFVM>();
            page.HighPrioritySpotlight ??= new List<IRFAttentionRowVM>();
            page.HighVolumeRows ??= new List<IRFAttentionRowVM>();
            page.MagList ??= new List<AssignmentVM>();

            try
            {
                var volumeRows = (await _dashboardService.GetIRFAttentionVolumeRows() ?? Enumerable.Empty<IRFAttentionRowVM>()).ToList();
                var volumeByPk = volumeRows
                    .Where(x => x != null && x.IrfPk > 0)
                    .GroupBy(x => x.IrfPk)
                    .ToDictionary(g => g.Key, g => g.First());

                ApplyAllFutureCounts(page.Rows, volumeByPk);
                ApplyAllFutureCounts(page.SuggestedThresholdRows, volumeByPk);
                ApplyAllFutureCounts(page.BelowThresholdRows, volumeByPk);

                var spotlights = (await _dashboardService.GetActiveIRFAttentionSpotlights() ?? Enumerable.Empty<IRFAttentionSpotlightVM>()).ToList();
                var spotlightIds = spotlights.Where(s => s != null && s.IrfId > 0).Select(x => x.IrfId).Distinct().ToList();

                var missingMetaIds = spotlightIds.Where(id => !volumeByPk.ContainsKey(id)).ToList();
                if (missingMetaIds.Count > 0)
                {
                    var meta = await _dashboardService.GetIRFAttentionMetaByIds(missingMetaIds) ?? Enumerable.Empty<IRFAttentionRowVM>();
                    foreach (var m in meta)
                    {
                        if (m != null && m.IrfPk > 0 && !volumeByPk.ContainsKey(m.IrfPk))
                            volumeByPk[m.IrfPk] = m;
                    }
                }

                var spotlightRows = new List<IRFAttentionRowVM>();
                foreach (var pin in spotlights.Where(s => s != null && s.IrfId > 0))
                {
                    if (!volumeByPk.TryGetValue(pin.IrfId, out var row))
                    {
                        row = new IRFAttentionRowVM { IrfPk = pin.IrfId, IrfCode = "#" + pin.IrfId };
                    }
                    else
                    {
                        row = CloneAttentionRow(row);
                    }
                    row.IsSpotlight = true;
                    row.PinnedBy = pin.PinnedBy;
                    row.PinnedAt = pin.PinnedAt;
                    spotlightRows.Add(row);
                }

                var spotlightSet = spotlightIds.ToHashSet();
                page.HighPrioritySpotlight = spotlightRows;
                page.HighVolumeRows = volumeRows
                    .Where(x => x != null && x.IrfPk > 0 && !spotlightSet.Contains(x.IrfPk))
                    .OrderByDescending(x => x.Volume)
                    .ThenBy(x => x.IrfCode, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                page.MagList = (_interviewerService.FetchAssessmentGroups() ?? Enumerable.Empty<AssignmentVM>())
                    .Where(x => x != null && x.id > 0 && !string.IsNullOrWhiteSpace(x.AssessmentGroup))
                    .GroupBy(x => x.id)
                    .Select(g => g.First())
                    .OrderBy(x => x.AssessmentGroup)
                    .ToList();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                page.HighPrioritySpotlight ??= new List<IRFAttentionRowVM>();
                page.HighVolumeRows ??= new List<IRFAttentionRowVM>();
                page.MagList ??= new List<AssignmentVM>();
            }
        }

        private static IRFAttentionRowVM CloneAttentionRow(IRFAttentionRowVM row)
        {
            return new IRFAttentionRowVM
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

        private static void ApplyAllFutureCounts(IEnumerable<DriveIRFVM> rows, Dictionary<int, IRFAttentionRowVM> volumeByPk)
        {
            if (rows == null || volumeByPk == null)
                return;

            foreach (var row in rows)
            {
                if (row == null)
                    continue;
                if (row.IrfIntId > 0 && volumeByPk.TryGetValue(row.IrfIntId, out var v) && v != null)
                {
                    row.AllFutureVolume = v.Volume;
                    row.AllFutureUnassigned = v.UnassignedCount;
                    row.HasAllFutureCounts = true;
                }
            }
        }

        private async Task EnrichSupplyCountsAsync(List<DriveIRFVM> rows)
        {
            if (rows == null || rows.Count == 0)
                return;

            // List load must stay fast for This/Next tab switches.
            // IRF + Rel. counts open live via modal; only MAG uses a lightweight count here.
            var magCountCache = new System.Collections.Concurrent.ConcurrentDictionary<int, int>();
            var magIds = rows.Where(r => r.MAGId > 0).Select(r => r.MAGId).Distinct().ToList();
            using var gate = new System.Threading.SemaphoreSlim(8);

            var tasks = magIds.Select(async magId =>
            {
                await gate.WaitAsync();
                try
                {
                    var magMembers = await _irfService.GetAssessmentGroupInterviewerMembers(magId, isMinimalData: true);
                    magCountCache[magId] = magMembers?.Count() ?? 0;
                }
                catch (Exception ex)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    magCountCache[magId] = 0;
                }
                finally
                {
                    gate.Release();
                }
            });

            await Task.WhenAll(tasks);

            foreach (var row in rows)
            {
                row.InterviewersInIRF = 0;
                row.InterviewersInRelatedMAGs = 0;
                row.InterviewersInMAG = row.MAGId > 0 && magCountCache.TryGetValue(row.MAGId, out var c) ? c : 0;
            }
        }

        private static string BuildSpotlightLabel(string irfId, string skill, int demand, bool below)
        {
            var title = string.IsNullOrWhiteSpace(irfId) ? "IRF" : irfId.Trim();
            if (!string.IsNullOrWhiteSpace(skill))
                title += " — " + skill.Trim();
            title += " (" + demand + ")";
            if (below)
                title += " · below threshold";
            return title;
        }

        private DriveIRFVM BuildDriveRow(
            IGrouping<string, InterviewVM> group,
            Dictionary<string, DriveIRFMetadataRecord> metadataMap,
            DateTime weekendStart)
        {
            var irfKey = group.Key;
            TryGetDriveMetadata(metadataMap, irfKey, out var irf);
            var irfIntId = irf?.id ?? 0;
            if (irfIntId <= 0)
            {
                irfIntId = _irfService.GetIRFIdByIRFName(irfKey);
            }

            return new DriveIRFVM
            {
                IRFId = irf?.IRFId ?? irfKey,
                IrfIntId = irfIntId,
                PrimarySkill = irf?.JDTiltle,
                AssessmentGroupName = irf?.AssessmentGroupName,
                ClientName = irf?.ClientName,
                EndClientName = irf?.ClientCompName,
                MAGId = ParseMagId(irf?.AssessmentGroupId),
                WeekendStart = weekendStart,
                WeekendBucket = null,
                ExpectedMorning = group.Count(x => IsMorningSlot(x)),
                ExpectedEvening = group.Count(x => IsEveningSlot(x)),
                UnassignedMorning = group.Count(x => IsMorningSlot(x) && x.interviewer == null),
                UnassignedEvening = group.Count(x => IsEveningSlot(x) && x.interviewer == null),
                ConcurrentInterview = string.Join(" ",
                    group.GroupBy(x => x.interviewDate)
                        .Where(d => d.Count() > 2)
                        .Select(d => d.Key.ToString("dd-MM HH:mm") + ":" + d.Count()))
            };
        }

        private void EnrichMissingMetadata(List<DriveIRFVM> rows)
        {
            if (rows == null || rows.Count == 0)
                return;

            var unresolvedIntIds = rows
                .Where(m => m != null && m.IrfIntId > 0 && string.IsNullOrWhiteSpace(m.ClientName))
                .Select(m => m.IrfIntId.ToString())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (unresolvedIntIds.Count == 0)
            {
                return;
            }

            var metadataByIntId = (_irfService.GetDriveIRFMetadata(unresolvedIntIds)
                    ?? new Dictionary<string, DriveIRFMetadataRecord>(StringComparer.OrdinalIgnoreCase))
                .Values
                .Where(x => x != null && x.id > 0)
                .GroupBy(x => x.id)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var row in rows)
            {
                if (row.IrfIntId > 0
                    && string.IsNullOrWhiteSpace(row.ClientName)
                    && metadataByIntId.TryGetValue(row.IrfIntId, out var irfMeta))
                {
                    ApplyDriveMetadata(row, irfMeta);
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRelatedMagInterviewerMembers(int magId, int irfIntId, string irfName)
        {
            try
            {
                if (!TryGetSessionUser(out _))
                {
                    return Content(string.Empty);
                }

                if (magId <= 0)
                {
                    return Content("No related MAGs for this row.");
                }

                var secondaryGroups = await _irfService.GetMAGSecondaryAssessmentGroupsById(magId);
                if (secondaryGroups == null || !secondaryGroups.Any())
                {
                    return Content("No related MAGs configured for this assessment group.");
                }

                var members = new List<IRFInterviewerMembers>();
                foreach (var secondaryGroup in secondaryGroups)
                {
                    if (secondaryGroup.AssessmentGroupId <= 0)
                    {
                        continue;
                    }

                    var groupMembers = await _irfService.GetAssessmentGroupInterviewerMembers(secondaryGroup.AssessmentGroupId);
                    if (groupMembers != null)
                    {
                        members.AddRange(groupMembers);
                    }
                }

                var model = members
                    .GroupBy(x => x.UserId)
                    .Select(g => g.First())
                    .Select(x =>
                    {
                        x.AvailableStatus = ((InterviewerAvailabilityStatus)Convert.ToInt32(x.AvailableStatus)).GetDescription();
                        return x;
                    })
                    .ToList();

                ViewBag.Type = "ALL";
                ViewBag.IRFId = irfIntId;
                ViewBag.IRFName = irfName;
                return PartialView("~/Views/IRF/IRFInterviewerMembersPartial.cshtml", model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Content("Unable to load related MAG interviewers.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> PlivoBatchCall(int magId, string scope)
        {
            try
            {
                if (!TryGetSessionUser(out _))
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                if (magId <= 0)
                {
                    return Json(new { success = false, message = "Invalid MAG." });
                }

                var urls = new List<string>();
                var magIds = new List<int> { magId };

                if (string.Equals(scope, "Campaign", StringComparison.OrdinalIgnoreCase))
                {
                    var secondaryGroups = await _irfService.GetMAGSecondaryAssessmentGroupsById(magId);
                    if (secondaryGroups != null)
                    {
                        magIds.AddRange(secondaryGroups.Select(x => x.AssessmentGroupId).Where(x => x > 0));
                    }
                }

                foreach (var agId in magIds.Distinct())
                {
                    var magName = await _eaRepository.GetMAGNameByAssessmentGroupIdAsync(agId);
                    if (!string.IsNullOrWhiteSpace(magName))
                    {
                        urls.Add(Url.Action("BatchCall", "InterviewerAvailability", new { search = magName }));
                    }
                }

                if (!urls.Any())
                {
                    return Json(new { success = false, message = "No Plivo batch-call route found for this MAG." });
                }

                if (urls.Count == 1)
                {
                    return Json(new { success = true, url = urls[0] });
                }

                return Json(new { success = true, urls = urls });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Unable to resolve batch call route." });
            }
        }

        private static (DateTime saturday, DateTime end) GetWeekendRange(string weekend)
        {
            var today = DateTime.Now.Date;
            var sat = GetThisWeekendSaturday(today);
            if (string.Equals(weekend, "Next", StringComparison.OrdinalIgnoreCase))
            {
                sat = sat.AddDays(7);
            }

            return (sat, sat.AddDays(1));
        }

        private static bool IsMorningSlot(InterviewVM interview)
        {
            return interview != null && interview.interviewSlot == 1;
        }

        private static bool IsEveningSlot(InterviewVM interview)
        {
            return interview != null && interview.interviewSlot == 2;
        }

        private static bool TryGetDriveMetadata(
            Dictionary<string, DriveIRFMetadataRecord> metadataMap,
            string irfKey,
            out DriveIRFMetadataRecord irf)
        {
            irf = null;
            if (metadataMap == null || string.IsNullOrWhiteSpace(irfKey))
            {
                return false;
            }

            return metadataMap.TryGetValue(irfKey.Trim(), out irf);
        }

        private static void ApplyDriveMetadata(DriveIRFVM row, DriveIRFMetadataRecord irf)
        {
            if (row == null || irf == null)
            {
                return;
            }

            row.IRFId = irf.IRFId ?? row.IRFId;
            row.IrfIntId = irf.id > 0 ? irf.id : row.IrfIntId;
            row.PrimarySkill = irf.JDTiltle;
            row.AssessmentGroupName = irf.AssessmentGroupName;
            row.ClientName = irf.ClientName;
            row.EndClientName = irf.ClientCompName;
            row.MAGId = ParseMagId(irf.AssessmentGroupId);
        }

        private static int ParseMagId(string assessmentGroupId)
        {
            if (string.IsNullOrWhiteSpace(assessmentGroupId))
            {
                return 0;
            }

            var first = assessmentGroupId.Split(',')[0].Trim();
            return int.TryParse(first, out var magId) ? magId : 0;
        }

        private static DateTime GetThisWeekendSaturday(DateTime today)
        {
            if (today.DayOfWeek == DayOfWeek.Sunday)
            {
                return today.AddDays(-1);
            }

            int toSat = ((int)DayOfWeek.Saturday - (int)today.DayOfWeek + 7) % 7;
            return today.AddDays(toSat);
        }

        private bool TryGetSessionUser(out string email)
        {
            email = HttpContext.Session.GetString("email")?.Trim()?.Replace("\"", "");
            var profile = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "");
            return !string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(profile);
        }

        [HttpGet]
        public async Task<JsonResult> GetIRFsByMAG(int magId)
        {
            try
            {
                if (!TryGetSessionUser(out _))
                    return Json(new { success = false, data = Array.Empty<object>(), message = "Unauthorized." });
                if (magId <= 0)
                    return Json(new { success = true, data = Array.Empty<object>() });

                var irfs = await _irfService.GetAssessmentGroupIRFList(magId) ?? Enumerable.Empty<AssessmentIRFListVM>();
                var data = irfs
                    .Where(x => x != null && x.id > 0 && !string.IsNullOrWhiteSpace(x.IRFId))
                    .Select(x => new
                    {
                        id = x.id,
                        irfId = x.IRFId,
                        label = string.IsNullOrWhiteSpace(x.PrimarySkillInvite)
                            ? x.IRFId
                            : (x.IRFId + " — " + x.PrimarySkillInvite)
                    })
                    .ToList();
                return Json(new { success = true, data });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, data = Array.Empty<object>(), message = "Failed to load IRFs." });
            }
        }

        [HttpPost]
        public async Task<JsonResult> PinHighPriority(int irfId)
        {
            try
            {
                if (!TryGetSessionUser(out var email))
                    return Json(new { ok = false, message = "Unauthorized." });
                if (irfId <= 0)
                    return Json(new { ok = false, message = "Invalid IRF." });

                var ok = await _dashboardService.PinIRFAttentionSpotlight(irfId, email);
                return Json(new { ok, success = ok, message = ok ? "Pinned." : "Could not pin." });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, message = "Could not pin." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DriveVolumeSection()
        {
            try
            {
                if (!TryGetSessionUser(out _))
                    return Content(string.Empty);

                var page = new DriveWeekendDataVM();
                await AttachDemandEnhancementsAsync(page);
                return PartialView("_DemandVolumeSection", page);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return PartialView("_DemandVolumeSection", new DriveWeekendDataVM());
            }
        }

        [HttpPost]
        public async Task<JsonResult> UnpinHighPriority(int irfId)
        {
            try
            {
                if (!TryGetSessionUser(out var email))
                    return Json(new { ok = false, success = false, message = "Unauthorized." });
                if (irfId <= 0)
                    return Json(new { ok = false, success = false, message = "Invalid IRF." });

                var ok = await _dashboardService.UnpinIRFAttentionSpotlight(irfId, email);
                return Json(new { ok, success = ok, message = ok ? "Unpinned." : "Could not unpin." });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, success = false, message = "Could not unpin." });
            }
        }

        /// <summary>D5 — standing cross-MAG entry link for a target MAG (does not expire).</summary>
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
                var url = "https://portal.risebird.io/Interviewer/MAGApply?id=" + System.Net.WebUtility.UrlEncode(encryptedId);
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
