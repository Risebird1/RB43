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
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IInterviewerEmailAvailabilityRepository _eaRepository;

        public InterviewDemandController(
            IIRFService irfService,
            IDashboardService dashboardService,
            IConfiguration configuration,
            IApplicationErrorLogger applicationErrorLogger,
            IInterviewerEmailAvailabilityRepository eaRepository)
        {
            _irfService = irfService;
            _dashboardService = dashboardService;
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _eaRepository = eaRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!IsAdminOrHr(out _))
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
                if (!IsAdminOrHr(out _))
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

            var metadataMap = _irfService.GetDriveIRFMetadata(irfIdStrings);

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

            var actionsTakenMap = _dashboardService.GetRowActions(DriveWeekendActionRefs.RefType, actionRefIds);
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

            return new DriveWeekendDataVM
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
            var unresolvedIntIds = rows
                .Where(m => m.IrfIntId > 0 && string.IsNullOrWhiteSpace(m.ClientName))
                .Select(m => m.IrfIntId.ToString())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (unresolvedIntIds.Count == 0)
            {
                return;
            }

            var metadataByIntId = _irfService.GetDriveIRFMetadata(unresolvedIntIds)
                .Values
                .Where(x => x.id > 0)
                .ToDictionary(x => x.id, x => x);

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
                if (!IsAdminOrHr(out _))
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
                if (!IsAdminOrHr(out _))
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

        private bool IsAdminOrHr(out string profileName)
        {
            profileName = null;
            if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
            {
                return false;
            }

            profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
            return profileName == "Admin" || profileName == "HR";
        }
    }
}
