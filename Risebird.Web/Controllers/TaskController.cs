using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
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
    public class TaskController : Controller
    {
        private readonly ITaskService _taskService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public TaskController(ITaskService taskService, IApplicationErrorLogger applicationErrorLogger)
        {
            _taskService = taskService;
            _applicationErrorLogger = applicationErrorLogger;
        }
        public async Task<IActionResult> SkillChannelCollectiveDetails(int irfId, string type)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter" || profileName == "CuratedUser" || profileName == "Finance" || profileName == "Finance Support" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "SeniorInterviewerManagement")
                    {
                        var model = await _taskService.SkillChannelCollectiveDetails(irfId, type);
                        if (model == null)
                        {
                            model = new SkillChannelCollectiveDetailsVM();
                        }
                        model.IRFId = irfId;
                        model.type = type;
                        return PartialView(model);
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
                return RedirectToAction("SignOut", "Session");
            }
        }
        [HttpPost]
        public async Task<IActionResult> SkillChannelAccuracyDetails(int irfId, string type)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter" || profileName == "CuratedUser" || profileName == "Finance" || profileName == "Finance Support" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "SeniorInterviewerManagement")
                    {
                        if (type == "RedoRequest")
                        {
                            var model = await _taskService.GetSkillChannelAccuracyRedoDetails(irfId, type);
                            if (model == null)
                            {
                                model = new List<SkillChannelTaskCompletionDetailsVM>();
                            }
                            ViewBag.AccuracyType = type;
                            return PartialView("_SkillChannelAccuracyRedoDetails", model);
                        }
                        else
                        {
                            var model = await _taskService.GetSkillChannelAccuracyDetails(irfId, type);
                            if (model == null)
                            {
                                model = new List<PQIDetails>();
                            }
                            ViewBag.AccuracyType = type;
                            return PartialView(model);
                        }
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
                return RedirectToAction("SignOut", "Session");
            }
        }
        [HttpPost]
        public async Task<IActionResult> SkillChannelSpeedDetails(int irfId, string type)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter" || profileName == "CuratedUser" || profileName == "Finance" || profileName == "Finance Support" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "SeniorInterviewerManagement")
                    {
                        var model = await _taskService.SkillChannelSpeedDetails(irfId, type);
                        if (model == null)
                        {
                            model = new List<SkillChannelTaskSpeedDetailsVM>();
                        }

                        ViewBag.AccuracyType = type;
                        return PartialView(model);
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
                return RedirectToAction("SignOut", "Session");
            }
        }
        [HttpPost]
        public async Task<IActionResult> SkillChannelCompletionDetails(int irfId, string type)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter" || profileName == "CuratedUser" || profileName == "Finance" || profileName == "Finance Support" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "SeniorInterviewerManagement")
                    {
                        var model = await _taskService.SkillChannelCompletionDetails(irfId, type);
                        if (model == null)
                        {
                            model = new List<SkillChannelTaskCompletionDetailsVM>();
                        }
                        model = model.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.MeetingStatusStatusDesc = ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription(); ; x.LastMeetingStatusStatusDesc = ((MeetingStatus)Convert.ToInt32(x.LastMeetingStatus)).GetDescription(); return x; }).ToList();
                        ViewBag.AccuracyType = type;
                        return PartialView(model);
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
                return RedirectToAction("SignOut", "Session");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddStatus()
        {
            SkillChannelTasks model = new SkillChannelTasks();
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var nvc = Request.Form;
                    string comment = string.Empty;
                    string StatusTabName = string.Empty;
                    string StatusMainTaskName = string.Empty;
                    int irfId = 0;
                    //int candidateId = 0;
                    int status = 0;
                    if (!string.IsNullOrEmpty(nvc["irfId"]))
                    {
                        irfId = Convert.ToInt32(nvc["irfId"]);
                    }
                    if (!string.IsNullOrEmpty(nvc["status"]))
                    {
                        status = Convert.ToInt32(nvc["status"]);
                    }
                    if (!string.IsNullOrEmpty(nvc["comment"]))
                    {
                        comment = nvc["comment"];
                    }
                    if (!string.IsNullOrEmpty(nvc["comment"]))
                    {
                        StatusTabName = nvc["StatusTabName"];
                    }
                    if (!string.IsNullOrEmpty(nvc["comment"]))
                    {
                        StatusMainTaskName = nvc["StatusMainTaskName"];
                    }

                    model.IRFId = irfId;
                    model.PerformedBy = emailid;
                    model.Status = status;
                    model.Comments = comment;
                    model.TaskName = StatusTabName;
                    model.TaskValue = StatusMainTaskName;

                    if (profileName != "Client")
                    {
                        if (irfId != 0 && status != 0)
                        {
                            bool result = await _taskService.AddTaskStatus(model);
                            return Json(result);
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
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                //TempData["SuccessErrorMessageCandidate"] = "Error saving candidate status.";
                return Json(false);
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetTaskStatusList(int irfId, string tabName, string taskValue, string irfName = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");

                    var model = new List<SkillChannelTasks>();
                    model = (await _taskService.GetTaskStatusList(irfId, tabName, taskValue)).ToList();
                    ViewBag.IRFName = irfName;
                    ViewBag.TaskValue = taskValue;
                    ViewBag.Profile = profileName;
                    return PartialView("TaskStatusListPartial", model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(false);//RedirectToAction("GetCandidates", new { id = irfId, irfName = irfName });
            }
        }
    }
}
