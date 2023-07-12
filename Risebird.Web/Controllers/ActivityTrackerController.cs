using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OfficeOpenXml;
using Org.BouncyCastle.Asn1;
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
    public class ActivityTrackerController : Controller
    {
        private readonly IActivityTrackerService _activityTrackerService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        public ActivityTrackerController(IActivityTrackerService activityTrackerService, IApplicationErrorLogger applicationErrorLogger)
        {
            _activityTrackerService = activityTrackerService;
            _applicationErrorLogger = applicationErrorLogger;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");

                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        var model = new List<ActivityTrackerVM>();
                        if (HttpContext.Session.GetString("ActivityTrackerFilters") == null)
                        {
                            model = (await _activityTrackerService.GetAllActivities(DateTime.Now, DateTime.Now)).ToList();
                        }
                        else
                        {
                            var filter = JsonConvert.DeserializeObject<ActivityTrackerFilter>(HttpContext.Session.GetString("ActivityTrackerFilters"));

                            model = (await _activityTrackerService.GetAllActivities(filter.StartDate ?? DateTime.Now, filter.EndDate ?? DateTime.Now)).ToList();

                            filter.PerformedBy = filter.PerformedBy == null ? model.Select(x => x.PerformedBy).ToArray() : filter.PerformedBy;
                            filter.ActivityType = filter.ActivityType == null ? model.Select(x => x.ActivityTypeDesc).ToArray() : filter.ActivityType;

                            model = model.Where(c => ((filter.PerformedBy != null && filter.PerformedBy.Count() > 0) ? filter.PerformedBy.Contains(c.PerformedBy) : c.PerformedBy == null)
                            && ((filter.ActivityType != null && filter.ActivityType.Count() > 0) ? filter.ActivityType.Contains(c.ActivityTypeDesc) : c.ActivityTypeDesc == null)).ToList();
                        }
                        ViewBag.profile = profileName;
                        return View(model);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }

                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Index(ActivityTrackerFilter filter)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {

                        if (filter != null)
                        {
                            HttpContext.Session.SetString("ActivityTrackerFilters", JsonConvert.SerializeObject(filter));
                        }
                        var model = (await _activityTrackerService.GetAllActivities(filter.StartDate ?? DateTime.Now, filter.EndDate ?? DateTime.Now)).ToList();

                        filter.PerformedBy = filter.PerformedBy == null ? model.Select(x => x.PerformedBy).ToArray() : filter.PerformedBy;
                        filter.ActivityType = filter.ActivityType == null ? model.Select(x => x.ActivityTypeDesc).ToArray() : filter.ActivityType;

                        model = model.Where(c => ((filter.PerformedBy != null && filter.PerformedBy.Count() > 0) ? filter.PerformedBy.Contains(c.PerformedBy) : c.PerformedBy == null)
                        && ((filter.ActivityType != null && filter.ActivityType.Count() > 0) ? filter.ActivityType.Contains(c.ActivityTypeDesc) : c.ActivityTypeDesc == null)).ToList();

                        return View(model);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ResetActivityTrackerFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("ActivityTrackerFilters");
                    return RedirectToAction("Index");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<FileContentResult> ActivitiesExporttoExcel()
        {
            string userEmailId = string.Empty;
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Interviews");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_Interviews" + ".xlsx";
            try
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");

                var model = new List<ActivityTrackerVM>();
                if (HttpContext.Session.GetString("ActivityTrackerFilters") == null)
                {
                    model = (await _activityTrackerService.GetAllActivities(DateTime.Now, DateTime.Now)).ToList();
                }
                else
                {
                    var filter = JsonConvert.DeserializeObject<ActivityTrackerFilter>(HttpContext.Session.GetString("ActivityTrackerFilters"));

                    model = (await _activityTrackerService.GetAllActivities(filter.StartDate ?? DateTime.Now, filter.EndDate ?? DateTime.Now)).ToList();
                    filter.PerformedBy = filter.PerformedBy == null ? model.Select(x => x.PerformedBy).ToArray() : filter.PerformedBy;
                    filter.ActivityType = filter.ActivityType == null ? model.Select(x => x.ActivityTypeDesc).ToArray() : filter.ActivityType;

                    model = model.Where(c => ((filter.PerformedBy != null && filter.PerformedBy.Count() > 0) ? filter.PerformedBy.Contains(c.PerformedBy) : c.PerformedBy == null)
                    && ((filter.ActivityType != null && filter.ActivityType.Count() > 0) ? filter.ActivityType.Contains(c.ActivityTypeDesc) : c.ActivityTypeDesc == null)).ToList();
                }

                Sheet.Cells["A1:Z1"].Style.Font.Bold = true;
                Sheet.Cells["A1:Z1"].Style.Font.Size = 12;
                Sheet.Cells["A1:Z1"].Style.WrapText = true;
                //Sheet.Column(1).Width = 30;
                //Sheet.Column(2).Width = 30;
                int startColumn = 1; int endColumn = 10;
                for (int a = startColumn; a <= endColumn; a++)
                {
                    Sheet.Column(a).Width = 40;
                }
                Sheet.Cells[1, 1].Value = "Activity Type";
                Sheet.Cells[1, 2].Value = "Particulars";
                Sheet.Cells[1, 3].Value = "Summary";
                Sheet.Cells[1, 4].Value = "Performed At";
                Sheet.Cells[1, 5].Value = "Performed By";
                Sheet.Cells[1, 6].Value = "Uploaded File";

                int i = 2;

                if (model.Count() > 0)
                {
                    foreach (var item in model)
                    {
                        Sheet.Cells[i, 1].Value = item.ActivityTypeDesc;
                        Sheet.Cells[i, 2].Value = item.Particulars;

                        if (!string.IsNullOrEmpty(item.ResourceUrl))
                        {
                            Sheet.Cells[i, 2].Hyperlink = new Uri(item.FileUrl, UriKind.Absolute);
                            Sheet.Cells[i, 2].Style.Font.UnderLine = true;
                            Sheet.Cells[i, 2].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                        }
                        Sheet.Cells[i, 3].Value = item.Summary;
                        Sheet.Cells[i, 4].Value = item.PerformedAt.ToString("MM/dd/yyyy HH:mm");
                        Sheet.Cells[i, 5].Value = item.PerformedBy;
                        if (!string.IsNullOrEmpty(item.FileUrl))
                        {
                            try
                            {
                                Sheet.Cells[i, 6].Hyperlink = new Uri(item.FileUrl, UriKind.Absolute);
                                Sheet.Cells[i, 6].Value = "Uploaded File";
                                Sheet.Cells[i, 6].Style.Font.UnderLine = true;
                                Sheet.Cells[i, 6].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                            }
                            catch (Exception)
                            {
                                Sheet.Cells[i, 6].Value = "No file uploaded";
                            }
                        }
                        else
                        {
                            Sheet.Cells[i, 6].Value = "No file uploaded";
                        }
                        i++;
                    }
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else
                {
                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                    Rng.Value = "No data available in table !!!";
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception)
            {
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Data export failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        [HttpPost]
        public async Task<JsonResult> CapturePageClick(string type, string currenturl, string buttonaction)
        {
            string emailid = string.Empty;
            if (HttpContext.Session.GetString("email") != null)
            {
                emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
            }
            var model = new CapturePageDM();
            model.ButtonAction = buttonaction;
            model.currenturl = currenturl;
            model.Functiontype = type;
            model.PerformedBy = emailid;
            await _activityTrackerService.SaveCaptureClickDetails(model);
            return Json("");
        }
    }
}
