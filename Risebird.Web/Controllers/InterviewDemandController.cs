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

            var driveRows = allRows
                .Where(m => m.ExpectedTotal >= threshold)
                .ToList();

            var belowThresholdRows = allRows
                .Where(m => m.ExpectedTotal < threshold)
                .ToList();

            stats.QualifyingIrfCount = driveRows.Count;
            stats.BelowThresholdIrfCount = belowThresholdRows.Count;

            var actionRefIds = driveRows
                .Where(m => !string.IsNullOrWhiteSpace(m.IRFId))
                .Select(m => DriveWeekendActionRefs.BuildRefId(m.IRFId, sat))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var actionsTakenMap = _dashboardService.GetRowActions(DriveWeekendActionRefs.RefType, actionRefIds);
            foreach (var row in driveRows)
            {
                if (!string.IsNullOrWhiteSpace(row.IRFId))
                {
                    var refId = DriveWeekendActionRefs.BuildRefId(row.IRFId, sat);
                    if (actionsTakenMap.TryGetValue(refId, out var actionsTaken))
                    {
                        row.ActionsTaken = actionsTaken;
                    }
                }
            }

            foreach (var row in driveRows)
            {
                if (row.IrfIntId > 0)
                {
                    var irfMembers = await _irfService.GetIRFInterviewerMembers(row.IrfIntId);
                    row.InterviewersInIRF = irfMembers?.Count() ?? 0;
                }

                if (row.MAGId > 0)
                {
                    var magMembers = await _irfService.GetAssessmentGroupInterviewerMembers(row.MAGId);
                    row.InterviewersInMAG = magMembers?.Count() ?? 0;

                    var secondaryGroups = await _irfService.GetMAGSecondaryAssessmentGroupsById(row.MAGId);
                    var relatedCount = 0;
                    if (secondaryGroups != null)
                    {
                        foreach (var secondaryGroup in secondaryGroups)
                        {
                            var secondaryMembers = await _irfService.GetAssessmentGroupInterviewerMembers(secondaryGroup.AssessmentGroupId);
                            relatedCount += secondaryMembers?.Count() ?? 0;
                        }
                    }

                    row.InterviewersInRelatedMAGs = relatedCount;
                }
            }

            return new DriveWeekendDataVM
            {
                Rows = driveRows,
                BelowThresholdRows = belowThresholdRows,
                WeekendStart = sat,
                Stats = stats
            };
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
