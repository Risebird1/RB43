using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Risebird.CommonHelper;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private readonly IAzureStorageHelper _azureStorageHelper;
        public DashboardController(IDashboardService dashboardService, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, INotificationHelper notificationHelper)
        {
            _dashboardService = dashboardService;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            _azureStorageHelper = azureStorageHelper;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");

                    if (profileName == "Admin")
                    {
                        var model = new DashboardVM();

                        //daily interview tracking first chart
                        model = GetDateWiseInterviews(DateTime.Now);
                        //daily interview tracking slots
                        model.DailyInterviewSlots = await _dashboardService.GetDateWiseMeetingsSlots(DateTime.Now);
                        model.InterviewerCountObj = await _dashboardService.GetInterviewerCount();
                        var progressDate = TempData.Peek("ProgressDate")?.ToString();
                        if (!string.IsNullOrEmpty(progressDate))
                        {
                            model.ProgressDate = Convert.ToDateTime(progressDate);

                            TempData.Keep("ProgressDate");
                        }
                        else
                        {
                            model.ProgressDate = DateTime.Now;
                        }


                        model.IrfProgressList = (IList<IRF>)(await _dashboardService.GetIRFProgress(model.ProgressDate));

                        model.MeetingResultCountObj = await _dashboardService.GetMeetingResultCount();
                        model.CandidatesCountObj = await _dashboardService.GetCandidatesCount();

                        model.TotalCompletedMeetings = model.Meetings != null ? model.Meetings.Where(x => x.InterviewStatus.HasValue).Count() : 0;
                        model.ProgressOptions = new SelectList(_dashboardService.GetProgressOptions(), "id", "Name");
                        var Assassination = from AssassinationStatus s in Enum.GetValues(typeof(AssassinationStatus))
                                            select new { ID = (int)s, Name = s.GetDescription() };
                        model.AssassinationList = new SelectList(Assassination, "ID", "Name");
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

        public static void AddProperty(ExpandoObject expando, string propertyName, object propertyValue)
        {
            // ExpandoObject supports IDictionary so we can extend it like this
            var expandoDict = expando as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
                expandoDict[propertyName] = propertyValue;
            else
                expandoDict.Add(propertyName, propertyValue);
        }

        public DashboardVM GetDateWiseInterviews(DateTime date)
        {
            try
            {
                var model = new DashboardVM();
                model.Meetings = _dashboardService.GetDateWiseMeetings(date).Result;
                var meetingTimes = model.Meetings.Select(x => x.interviewDate).Distinct();
                //var irfList = model.Meetings.Select(x => x.IRFID).Distinct();
                var irfList = model.Meetings.Select(x => x.IRFnSkill).Distinct();
                List<object> iData = new List<object>();

                foreach (var item in meetingTimes)
                {
                    dynamic expando = new ExpandoObject();
                    expando.Time = item;
                    foreach (var irf in irfList)
                    {
                        AddProperty(expando, irf, model.Meetings.Where(x => x.IRFID == irf.Split("(")[0] && x.interviewDate == item).Count());
                    }
                    iData.Add(expando);
                }

                model.MeetingsData = JsonConvert.SerializeObject(iData);
                model.MeetingsDataLabels = JsonConvert.SerializeObject(irfList);
                return model;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public IActionResult GetDateWiseInterviewStatus(DateTime date)
        {
            try
            {
                var model = new DashboardVM();
                model.Meetings = _dashboardService.GetDateWiseMeetings(date).Result;
                model.TotalCompletedMeetings = model.Meetings != null ? model.Meetings.Where(x => x.InterviewStatus.HasValue).Count() : 0;
                return PartialView("InterviewStatusPartial", model);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public IActionResult GetDateWiseMeetingsSlots(DateTime date)
        {
            try
            {
                var model = new DashboardVM();
                model.DailyInterviewSlots = _dashboardService.GetDateWiseMeetingsSlots(date).Result;
                return PartialView("InterviewSlotsPartial", model);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateIRFProgress(DashboardVM model)
        {
            try
            {
                await _dashboardService.UpdateIRFProgress(model.IrfProgressList);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<IActionResult> GetDateWiseIRFProgress(DateTime date)
        {
            try
            {
                var model = new DashboardVM();
                model.IrfProgressList = (IList<IRF>)(await _dashboardService.GetIRFProgress(date));
                model.ProgressOptions = new SelectList(_dashboardService.GetProgressOptions(), "id", "Name");
                model.ProgressDate = date;
                TempData["ProgressDate"] = date;
                return PartialView("IRFProgressPartial", model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<IActionResult> ClientIndex()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Client")
                    {
                        var model = new ClientDashboardVM();
                        var funnelData = await _dashboardService.GetClientFunnelData(emailid);
                        var slotsDetails = await _dashboardService.GetDateWiseClientMeetingsSlots(DateTime.Now.Date, emailid);
                        model = GetDateWiseClientInterviews(DateTime.Now.Date, emailid);
                        model.MeetingResultCountObj = await _dashboardService.GetClientMeetingsResults(emailid);
                        model.CandidatesCountObj = await _dashboardService.GetClientCandidatesCount(emailid);
                        model.EmailId = emailid;
                        model.FunnelData = JsonConvert.SerializeObject(funnelData);
                        model.DailyMeetingResultsSlots = slotsDetails;
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

        public ClientDashboardVM GetDateWiseClientInterviews(DateTime date, string emailId)
        {
            try
            {
                var model = new ClientDashboardVM();
                model.Meetings = _dashboardService.GetDateWiseClientMeetings(date, emailId).Result;
                var meetingTimes = model.Meetings.Select(x => x.interviewDate).Distinct();

                var irfList = model.Meetings.Select(x => x.IRFnSkill).Distinct();
                List<object> iData = new List<object>();

                foreach (var item in meetingTimes)
                {
                    dynamic expando = new ExpandoObject();
                    expando.Time = item;
                    foreach (var irf in irfList)
                    {
                        AddProperty(expando, irf, model.Meetings.Where(x => x.IRFID == irf.Split("(")[0] && x.interviewDate == item).Count());
                    }
                    iData.Add(expando);
                }

                model.MeetingsData = JsonConvert.SerializeObject(iData);
                model.MeetingsDataLabels = JsonConvert.SerializeObject(irfList);
                return model;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public IActionResult GetDateWiseClientMeetingsSlots(DateTime date, string emailId)
        {
            try
            {
                var model = new ClientDashboardVM();
                model.DailyMeetingResultsSlots = _dashboardService.GetDateWiseClientMeetingsSlots(date, emailId).Result;
                return PartialView("ClientSlotsStatusPartial", model);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public JsonResult FetchActivityDetails(int id, int idprogess)
        {
            var result = _dashboardService.FetchActivityDetails(id, idprogess);
            return Json(result);
        }

        #region ChannelManagement
        public async Task<IActionResult> RequirementManagement()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");

                if (profileName == "Admin")
                {
                    var model = await _dashboardService.FetchChannelRequests();
                    if (TempData["SuccessErrorMessageInterview"] != null)
                    {
                        ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                        TempData.Remove("SuccessErrorMessageInterview");
                    }
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
        public ActionResult AddRequirement()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");

                if (profileName == "Admin")
                {
                    var model = new ChannelVM();
                    model.isEditable = false;
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
        [HttpPost]
        public async Task<ActionResult> AddRequirement(ChannelVM model)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                model.CreatedBy = emailid;
                if (profileName == "Admin")
                {
                    if (!ModelState.IsValid)
                    {
                        return View(model);
                    }
                    if (model.Jdfile == null && (model.JdLink == null))
                    {
                        ModelState.AddModelError(String.Empty, "Please upload a Jd.");
                        return View(model);
                    }
                    if (model.Jdfile != null)
                    {
                        await _azureStorageHelper.InsertBlob(model.Jdfile.FileName, "resumes", model.Jdfile);
                        var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                        var blob = containerRef.GetBlockBlobReference(model.Jdfile.FileName);
                        model.JdLink = blob.Uri.ToString();
                    }

                    if (model.isEditable)
                    {
                        var result = await _dashboardService.AddChannelRequest(model);
                        if (result == "Success")
                        {
                            TempData["SuccessErrorMessageInterview"] = "Channel Request updated Successfully.";
                        }
                        else
                        {
                            TempData["SuccessErrorMessageInterview"] = "Fail to update channel request.";
                        }
                        return RedirectToAction("RequirementManagement");
                    }
                    else
                    {
                        var result = await _dashboardService.AddChannelRequest(model);
                        if (result == "Success")
                        {
                            var Emailds = await _dashboardService.FetchAdminandHRList();
                            string Emailaddress = string.Join(";", Emailds);
                            Mail _mail = new Mail
                            {
                                TO = Emailaddress,
                                Subject = $"Risebird: a new Channel request is created for " + model.Client
                            };
                            var resultset = _notificationHelper.ChannelRequestNotification(_mail, model.Client, model.PrimarySkill, model.Recruiter);
                            TempData["SuccessErrorMessageInterview"] = "Channel Request added Successfully.";
                        }
                        else
                        {
                            TempData["SuccessErrorMessageInterview"] = "Fail to add channel request.";
                        }
                        return RedirectToAction("RequirementManagement");
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
        public async Task<IActionResult> EditRequirement(int id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin")
                {
                    var model = await _dashboardService.FetchChannelRequestsByid(id);
                    model.isEditable = true;
                    var Priority = from Prioritylevels s in Enum.GetValues(typeof(Prioritylevels))
                                   select new { ID = (int)s, Name = s.GetDescription() };
                    model.Prioritylist = new SelectList(Priority, "ID", "Name");
                    return View("AddRequirement", model);
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
        public async Task<IActionResult> Interviewer(int id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin")
                {
                    var model = await _dashboardService.FetchInterviewerBychannelid(id);
                    ViewBag.channelid = id;
                    if (TempData["SuccessErrorMessageInterview"] != null)
                    {
                        ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                        TempData.Remove("SuccessErrorMessageInterview");
                    }
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
        public ActionResult AddInterviewer(int id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin")
                {
                    var model = new ChannelInterviewerVM();
                    model.Channelid = id;
                    var Priority = from Prioritylevels s in Enum.GetValues(typeof(Prioritylevels))
                                   select new { ID = (int)s, Name = s.GetDescription() };
                    model.Prioritylist = new SelectList(Priority, "ID", "Name");
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
        [HttpPost]
        public async Task<IActionResult> AddInterviewer(ChannelInterviewerVM model)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                model.CreatedBy = emailid;
                if (profileName == "Admin")
                {
                    if (!ModelState.IsValid)
                    {
                        return View(model);
                    }
                    if (model.CV == null && (model.CVLink == null))
                    {
                        ModelState.AddModelError(String.Empty, "Please upload a CV.");
                        return View(model);
                    }
                    if (model.CV != null)
                    {
                        await _azureStorageHelper.InsertBlob(model.CV.FileName, "resumes", model.CV);
                        var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                        var blob = containerRef.GetBlockBlobReference(model.CV.FileName);
                        model.CVLink = blob.Uri.ToString();
                    }
                    if (model.CodedCV != null)
                    {
                        await _azureStorageHelper.InsertBlob(model.CodedCV.FileName, "resumes", model.CodedCV);
                        var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                        var blob = containerRef.GetBlockBlobReference(model.CodedCV.FileName);
                        model.CodedCVLink = blob.Uri.ToString();
                    }
                    var result = await _dashboardService.AddChannelInterviewer(model);
                    if (result == "Success")
                    {
                        TempData["SuccessErrorMessageInterview"] = "Interviewer added Successfully.";
                    }
                    else
                    {
                        TempData["SuccessErrorMessageInterview"] = "Fail to add interviewer.";
                    }
                    return RedirectToAction("Interviewer", "DashBoard", new { id = model.Channelid });
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
        [HttpPost]
        public async Task<JsonResult> DeleteInterviewer(int id, int channelid)
        {
            var result = await _dashboardService.DeleteChannelInterviewerByid(id, channelid);
            if (result == "Success")
            {
                return Json("Success");
            }
            else
            {
                return Json("fail");
            }
        }
        #endregion
    }
}