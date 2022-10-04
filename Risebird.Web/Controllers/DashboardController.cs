using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OfficeOpenXml;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{

    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IUserService _userService;
        private IConfiguration _configuration;
        private readonly IMeetingService _meetingService;
        private readonly IFileMimeTypeHelper _fileMimeTypeHelper;
        private readonly IUserRepository _userRepository;
        private readonly IInterviewerService _interviewerService;
        private readonly IMeetingRespository _meetingRespository;
        private readonly IZoomHelper _zoomHelper;

        public DashboardController(IDashboardService dashboardService, IApplicationErrorLogger applicationErrorLogger,
            IAzureStorageHelper azureStorageHelper, INotificationHelper notificationHelper,
            IUserService userService, IConfiguration configuration, IMeetingService meetingService,
            IFileMimeTypeHelper fileMimeTypeHelper, IUserRepository userRepository, IInterviewerService interviewerService,
            IMeetingRespository meetingRespository, IZoomHelper zoomHelper)
        {
            _dashboardService = dashboardService;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            _azureStorageHelper = azureStorageHelper;
            _userService = userService;
            _configuration = configuration;
            _meetingService = meetingService;
            _fileMimeTypeHelper = fileMimeTypeHelper;
            _userRepository = userRepository;
            _interviewerService = interviewerService;
            _meetingRespository = meetingRespository;
            _zoomHelper = zoomHelper;
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
                    var Emailid = HttpContext.Session.GetString("email")?.Trim();
                    Emailid = Emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        var model = new DashboardVM();

                        //daily interview tracking first chart
                        //model = GetDateWiseInterviews(DateTime.Now);
                        //daily interview tracking slots
                        //model.DailyInterviewSlots = await _dashboardService.GetDateWiseMeetingsSlots(DateTime.Now, Emailid);
                        model.InterviewerCountObj = await _dashboardService.GetInterviewerCount();
                        model.NotUpdatedMeetingsObj = await _dashboardService.GetNotUpdatedMeetingsCount(Emailid);
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
                        //model.IrfProgressList = (IList<IRF>)(await _dashboardService.GetIRFProgress(model.ProgressDate, Emailid));
                        //model.MeetingResultCountObj = await _dashboardService.GetMeetingResultCount(Emailid);
                        //CandidatesCountObj model.CandidatesCountObj = await _dashboardService.GetCandidatesCount(Emailid);
                        model.TotalCompletedMeetings = model.Meetings != null ? model.Meetings.Where(x => x.InterviewStatus.HasValue).Count() : 0;
                        model.ProgressOptions = new SelectList(_dashboardService.GetProgressOptions(), "id", "Name");
                        var Assassination = from AssassinationStatus s in Enum.GetValues(typeof(AssassinationStatus))
                                            select new { ID = (int)s, Name = s.GetDescription() };
                        model.AssassinationList = new SelectList(Assassination, "ID", "Name");
                        //need to Update
                        //var channelrequest = await _dashboardService.FetchChannelRequests();
                        //model.ChannelRequest = channelrequest;
                        var interviewmanagement = (IList<InterviewManagementVM>)await _dashboardService.GetInterviewManagement(Emailid);
                        interviewmanagement = interviewmanagement.OrderByDescending(x => x.interviewDate).ToList();
                        foreach (var item in interviewmanagement)
                        {
                            item.interviewDate = item.interviewDate.Date;
                        }
                        model.InterviewManagement = interviewmanagement;
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
        public async Task<IActionResult> GetMeetingResultCount()
        {
            try
            {
                var emailId = string.Empty;
                if (HttpContext.Session.GetString("email") != null)
                {
                    emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                }
                var model = new MeetingResultCount();
                model = await _dashboardService.GetMeetingResultCount(emailId);
                return Json(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [Authorize]
        [HttpGet]
        public static void AddProperty(ExpandoObject expando, string propertyName, object propertyValue)
        {
            // ExpandoObject supports IDictionary so we can extend it like this
            var expandoDict = expando as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
                expandoDict[propertyName] = propertyValue;
            else
                expandoDict.Add(propertyName, propertyValue);
        }

        [Authorize]
        [HttpGet]
        public DashboardVM GetDateWiseInterviews(DateTime date)
        {
            try
            {
                var Emailid = string.Empty;
                if (HttpContext.Session.GetString("email") != null)
                {
                    Emailid = HttpContext.Session.GetString("email")?.Trim();
                    Emailid = Emailid.Replace("\"", "");
                }
                var model = new DashboardVM();
                model.Meetings = _dashboardService.GetDateWiseMeetings(date, Emailid).Result;
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

        [Authorize]
        [HttpGet]
        public IActionResult GetDateWiseInterviewStatus(DateTime date)
        {
            try
            {
                var Emailid = string.Empty;
                if (HttpContext.Session.GetString("email") != null)
                {
                    Emailid = HttpContext.Session.GetString("email")?.Trim();
                    Emailid = Emailid.Replace("\"", "");
                }
                var model = new DashboardVM();
                model.Meetings = _dashboardService.GetDateWiseMeetings(date, Emailid).Result;
                model.TotalCompletedMeetings = model.Meetings != null ? model.Meetings.Where(x => x.InterviewStatus.HasValue).Count() : 0;
                return PartialView("InterviewStatusPartial", model);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult GetDateWiseMeetingsSlots(DateTime date)
        {
            try
            {
                var Emailid = string.Empty;
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    Emailid = HttpContext.Session.GetString("email")?.Trim();
                    Emailid = Emailid.Replace("\"", "");
                }
                var model = new DashboardVM();
                model.DailyInterviewSlots = _dashboardService.GetDateWiseMeetingsSlots(date, Emailid).Result;
                return PartialView("InterviewSlotsPartial", model);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [Authorize]
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

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetDateWiseIRFProgress(DateTime date)
        {
            try
            {
                var Emailid = string.Empty;
                if (HttpContext.Session.GetString("email") != null)
                {
                    Emailid = HttpContext.Session.GetString("email")?.Trim();
                    Emailid = Emailid.Replace("\"", "");
                }
                var model = new DashboardVM();
                model.IrfProgressList = (IList<IRF>)(await _dashboardService.GetIRFProgress(date, Emailid));
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

        [Authorize]
        [HttpGet]
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
                        //var funnelData = await _dashboardService.GetClientFunnelData(emailid);
                        //var slotsDetails = await _dashboardService.GetDateWiseClientMeetingsSlots(DateTime.Now.Date, emailid);
                        //model = GetDateWiseClientInterviews(DateTime.Now.Date, emailid);
                        //model.CandidatesCountObj = await _dashboardService.GetClientCandidatesCurrentStatusCount(emailid);
                        model.EmailId = emailid;
                        //model.FunnelData = JsonConvert.SerializeObject(funnelData);
                        //model.DailyMeetingResultsSlots = slotsDetails;
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
        public async Task<string> GetClientFunnelData()
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
                        var funnelData = await _dashboardService.GetClientFunnelData(emailid);
                        return JsonConvert.SerializeObject(funnelData);
                    }
                    else
                        return string.Empty;
                }
                else
                {
                    return null;
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
        public ClientDashboardVM GetDateWiseClientInterviews(DateTime dateValue, string emailId)
        {
            try
            {
                var model = new ClientDashboardVM();
                model.Meetings = _dashboardService.GetDateWiseClientMeetings(dateValue, emailId).Result;
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

        [Authorize]
        [HttpGet]
        public IActionResult GetDateWiseClientMeetingsSlots(DateTime dateValue, string emailId)
        {
            try
            {
                var model = new ClientDashboardVM();
                model.DailyMeetingResultsSlots = _dashboardService.GetDateWiseClientMeetingsSlots(dateValue, emailId).Result;
                return PartialView("ClientSlotsStatusPartial", model);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [Authorize]
        [HttpGet]
        public JsonResult FetchActivityDetails(int id, int idprogess)
        {
            var result = _dashboardService.FetchActivityDetails(id, idprogess);
            return Json(result);
        }


        #region ChannelManagement
        [HttpGet]
        public async Task<IActionResult> RequirementManagement()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var model = await _dashboardService.FetchChannelRequests();
                ViewBag.Client = "false";
                if (profileName == "Client")
                {
                    var Emailaddress = emailid.ToUpper();
                    model = model.Where(x => x.RequesterEmailid.ToUpper() == Emailaddress).ToList();
                    ViewBag.Client = "true";
                }
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

        [Authorize]
        [HttpGet]
        public ActionResult AddRequirement()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                ViewBag.type = "back";
            }
            else
            {
                ViewBag.type = "no";
                var menus = _userService.GetMenuBasedOnUserEmailld("Admin").GroupBy(p => p.functionName).Select(g => g.First()).ToList();
                if (menus != null)
                {
                    menus = menus.Where(x => x.functionName == "Support").ToList();
                    HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
                }
            }
            ViewBag.Client = "false";
            var model = new ChannelVM();
            model.isEditable = false;
            return View(model);
        }

        [Authorize]
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
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            if (model.Jdfile == null && (model.JdLink == null))
            {
                ModelState.AddModelError(string.Empty, "Please upload a Jd.");
                return View(model);
            }
            if (model.Jdfile != null)
            {
                var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.Jdfile);

                if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                || fileMimeType == "application/pdf")
                {
                    await _azureStorageHelper.InsertBlob(model.Jdfile.FileName, "resumes", model.Jdfile);
                    var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                    var blob = containerRef.GetBlockBlobReference(model.Jdfile.FileName);
                    model.JdLink = blob.Uri.ToString();
                }

            }

            if (model.isEditable)
            {
                var result = await _dashboardService.AddChannelRequest(model);
                if (result == "Success")
                {
                    TempData["SuccessErrorMessageInterview"] = "Channel Request has been updated Successfully!";
                }
                else
                {
                    TempData["SuccessErrorMessageInterview"] = "Failed to update channel request!";
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
                    var resultset = _notificationHelper.ChannelRequestNotification(_mail, model.Client, model.PrimarySkill, model.Requester);
                    TempData["SuccessErrorMessageInterview"] = "Channel Request has been added Successfully!";
                    ViewBag.Message = "Channel Request has been updated Successfully!";
                }
                else
                {
                    TempData["SuccessErrorMessageInterview"] = "Failed to add channel request!";
                    ViewBag.Message = "Failed to add channel request.";
                }
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                {
                    var menus = _userService.GetMenuBasedOnUserEmailld("Admin").GroupBy(p => p.functionName).Select(g => g.First()).ToList();
                    if (menus != null)
                    {
                        menus = menus.Where(x => x.functionName == "Support").ToList();
                        HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
                    }
                    return View();
                }
                return RedirectToAction("RequirementManagement");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EditRequirement(int id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                {
                    ViewBag.type = "back";
                    ViewBag.Client = "false";
                    if (profileName == "Client")
                    {
                        ViewBag.Client = "true";
                    }
                    var model = await _dashboardService.FetchChannelRequestsByid(id);
                    model.isEditable = true;
                    var Priority = from Prioritylevels s in Enum.GetValues(typeof(Prioritylevels))
                                   select new { ID = (int)s, Name = s.GetDescription() };
                    model.Prioritylist = new SelectList(Priority, "ID", "Name");
                    var Origin = from Originlevel s in Enum.GetValues(typeof(Originlevel))
                                 select new { ID = (int)s, Name = s.GetDescription() };
                    model.Originlist = new SelectList(Origin, "ID", "Name");
                    var Engagementtype = from Engagementtypelevel s in Enum.GetValues(typeof(Engagementtypelevel))
                                         select new { ID = (int)s, Name = s.GetDescription() };
                    model.EngagementTypelist = new SelectList(Engagementtype, "ID", "Name");
                    var stage = from stagelevel s in Enum.GetValues(typeof(stagelevel))
                                select new { ID = (int)s, Name = s.GetDescription() };
                    model.stagelist = new SelectList(stage, "ID", "Name");
                    var paymentstatus = from paymentstatuslevel s in Enum.GetValues(typeof(paymentstatuslevel))
                                        select new { ID = (int)s, Name = s.GetDescription() };
                    model.PaymentStatuslist = new SelectList(paymentstatus, "ID", "Name");
                    var confirmationstatus = from ConfirmationStatuslevel s in Enum.GetValues(typeof(ConfirmationStatuslevel))
                                             select new { ID = (int)s, Name = s.GetDescription() };
                    model.ConfirmationStatuslist = new SelectList(confirmationstatus, "ID", "Name");
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

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Interviewer(int id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
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

        [Authorize]
        [HttpGet]
        public ActionResult AddInterviewer(int id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                {
                    var model = new ChannelInterviewerVM();
                    model.Channelid = id;
                    var Priority = from Prioritylevels s in Enum.GetValues(typeof(Prioritylevels))
                                   select new { ID = (int)s, Name = s.GetDescription() };
                    model.Prioritylist = new SelectList(Priority, "ID", "Name");
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

        [Authorize]
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
                if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                {
                    if (!ModelState.IsValid)
                    {
                        return View(model);
                    }
                    if (model.CV == null && (model.CVLink == null))
                    {
                        ModelState.AddModelError(string.Empty, "Please upload a CV.");
                        var Priority = from Prioritylevels s in Enum.GetValues(typeof(Prioritylevels))
                                       select new { ID = (int)s, Name = s.GetDescription() };
                        model.Prioritylist = new SelectList(Priority, "ID", "Name");
                        return View(model);
                    }
                    if (model.CV != null)
                    {
                        var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.CV);

                        if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                        || fileMimeType == "application/pdf")
                        {
                            await _azureStorageHelper.InsertBlob(model.CV.FileName, "resumes", model.CV);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                            var blob = containerRef.GetBlockBlobReference(model.CV.FileName);
                            model.CVLink = blob.Uri.ToString();
                        }

                    }
                    if (model.CodedCV != null)
                    {
                        var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.CodedCV);

                        if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                        || fileMimeType == "application/pdf")
                        {
                            await _azureStorageHelper.InsertBlob(model.CodedCV.FileName, "resumes", model.CodedCV);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                            var blob = containerRef.GetBlockBlobReference(model.CodedCV.FileName);
                            model.CodedCVLink = blob.Uri.ToString();
                        }

                    }
                    var result = await _dashboardService.AddChannelInterviewer(model);
                    if (result == "Success")
                    {
                        if (model.isEditable)
                        {
                            TempData["SuccessErrorMessageInterview"] = "Interviewer has been updated Successfully!";
                        }
                        else
                        {
                            TempData["SuccessErrorMessageInterview"] = "Interviewer has been added Successfully!";
                        }
                    }
                    else
                    {
                        if (model.isEditable)
                        {
                            TempData["SuccessErrorMessageInterview"] = "Failed to update interviewer!";
                        }
                        else
                        {
                            TempData["SuccessErrorMessageInterview"] = "Failed to add interviewer!";
                        }
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

        [Authorize]
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

        [Authorize]
        [HttpGet]
        public async Task<ActionResult> EditInterviewer(int id, int channelid)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var model = await _dashboardService.FecthInterviewerById(id);
                model.Channelid = channelid;
                var Priority = from Prioritylevels s in Enum.GetValues(typeof(Prioritylevels))
                               select new { ID = (int)s, Name = s.GetDescription() };
                model.Prioritylist = new SelectList(Priority, "ID", "Name");
                model.isEditable = true;
                return View("AddInterviewer", model);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [HttpPost]
        public async Task<JsonResult> FetchHR(string term)
        {
            var model = await _dashboardService.FetchHRList(term);
            return Json(model);
        }

        [HttpGet]
        public IActionResult AddInterviewerList(int id, string channelname)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        if (profileName == "Client")
                        {
                            ViewBag.IsClient = "true";
                        }
                        ViewBag.ChannelName = channelname;
                        ViewBag.ISCId = id;
                        ViewBag.profile = profileName;
                        var interviewerList = new List<ChannelApprovedInterviewer>();
                        interviewerList = _dashboardService.GetChannelInterviewers(id)?.ToList();


                        if (TempData["SuccessErrorMessageISCInterviewerList"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageISCInterviewerList"].ToString();
                            TempData.Remove("SuccessErrorMessageISCInterviewerList");
                        }
                        return View(interviewerList);
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public IActionResult SelectChannelInterviewers(int cmId, string cmTitle)
        {

            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                {
                    var model = new AddChannelInterviewersVM();
                    model.ChannelInterviewersCollection = (IList<ChannelApprovedInterviewer>)_dashboardService.GetChannelApprovedInterviewer(cmId);
                    model.ChannelId = cmId;
                    model.ChannelTitle = cmTitle;
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
        public async Task<IActionResult> SelectChannelInterviewers(AddChannelInterviewersVM model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        if (_dashboardService.AddChannelApprovedInterviewer(model.ChannelInterviewersCollection, model.ChannelId))
                        {
                            TempData["SuccessErrorMessageISCInterviewerList"] = "Interviewer has been added Successfully!";
                            return RedirectToAction("AddInterviewerList", new { id = model.ChannelId, channelname = model.ChannelTitle });
                        }
                        else
                        {
                            TempData["SuccessErrorMessageISCInterviewerList"] = "Interviewer could not be added.";
                            return RedirectToAction("AddInterviewerList", new { id = model.ChannelId, channelname = model.ChannelTitle });
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
                TempData["SuccessErrorMessageISCInterviewerList"] = "Interviewer could not be added.";
                return RedirectToAction("AddInterviewerList", new { id = model.ChannelId, channelname = model.ChannelTitle });
            }
        }

        [HttpGet]
        public IActionResult DeleteChannelInterviewer(int id, int iscId, string iscName)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        if (_dashboardService.DeleteChannelInterviewer(id).Result)
                        {
                            //TempData["SuccessErrorMessageISCInterviewer"] = "Interviewer removed Successfully.";
                            return RedirectToAction("AddInterviewerList", new { id = iscId, channelname = iscName });
                        }
                        else
                        {
                            //TempData["SuccessErrorMessageISCInterviewer"] = "Could not remove Interviewer.";
                            return RedirectToAction("AddInterviewerList", new { id = iscId, channelname = iscName });
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageISCInterviewer"] = "Could not remove Interviewer.";
                return RedirectToAction("GetISCInterviewers", new { id = iscId, InterviewerSkillidName = iscName });
            }

        }

        [HttpGet]
        public IActionResult ChannelInterviewerAction(int id, string channelName, int channelid, string status)
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
                        if (_dashboardService.ChannelInterviewerAction(id, status).Result)
                        {
                            if (status == "Approved")
                            {
                                TempData["SuccessErrorMessageISCInterviewer"] = "Interviewer has been approved Successfully!";
                            }
                            else
                            {
                                TempData["SuccessErrorMessageISCInterviewer"] = "Interviewer has been unapproved Successfully!";
                            }

                            return RedirectToAction("AddInterviewerList", new { id = channelid, channelname = channelName });
                        }
                        else
                        {
                            if (status == "Approved")
                            {
                                TempData["SuccessErrorMessageISCInterviewer"] = "Could not approved Interviewer.";
                            }
                            else
                            {
                                TempData["SuccessErrorMessageISCInterviewer"] = "Could not unapproved Interviewer.";
                            }
                            return RedirectToAction("AddInterviewerList", new { id = channelid, channelname = channelName });
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageISCInterviewer"] = "Could not remove Interviewer.";
                return RedirectToAction("GetISCInterviewers", new { id = channelid, channelname = channelName });
            }
        }

        #endregion

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> UnassignedInterviews(string actionstatus, int actiontype)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        if (HttpContext.Session.GetString("UnassignedInterviewsFilters") != null)
                            HttpContext.Session.Remove("UnassignedInterviewsFilters");
                        if (actionstatus == "DeletedZoomMeetings")
                        {
                            return RedirectToAction("DeletedZoomMeetings", new { /*actionStatus = actionstatus, */actionType = actiontype });
                        }
                        var modelList = new List<MeetingVM>();
                        if (actiontype == 1 && actionstatus == "UnAssignedCoordianter")
                        {
                            AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
                            var result = _azureTableStorageService.GetAllEntities<SurveyVM>("RisebirdSurvey");

                            var myInterview = _meetingService.GetHRAssignedInterviews(emailid);
                            var WeekDate = DateTime.Now.AddDays(-30);
                            var interviewerids = myInterview.Where(x => x.interviewDate >= WeekDate).Select(x => x.id).Distinct().ToList();
                            if (interviewerids != null)
                            {
                                foreach (var item in myInterview)
                                {
                                    if (result.Where(x => x.interviewid == item.id).Count() == 0)
                                    {
                                        modelList.Add(new MeetingVM
                                        {
                                            IRFId = item.IRFID,
                                            interviewDate = item.interviewDate,
                                            candidateName = item.candidateName,
                                            candidateEmail = item.candidateEmail,
                                            imid = item.id,
                                            interviewer = item.interviewer,
                                            confirmationStatus = "1",
                                            HRCoordinatorEmailId = emailid,
                                            MeetingStatus = item.MeetingStatus,
                                            InterviewStatus = item.InterviewStatus,
                                        });
                                    }
                                }
                            }
                        }
                        else
                        {
                            modelList = await _dashboardService.FetchRibbonDetailsValues(actionstatus, emailid, actiontype, profileName);
                        }
                        if (modelList == null)
                        {
                            modelList = new List<MeetingVM>();
                        }
                        ViewBag.PageHeader = "";
                        ViewBag.Actiontype = actiontype;
                        ViewBag.actionstatus = actionstatus;
                        if (actionstatus == "InterviewCount")
                        {
                            ViewBag.PageHeader = "911 Dashboard / Unassigned Interviews";
                        }
                        else if (actionstatus == "MeetingStatus")
                        {
                            ViewBag.PageHeader = "911 Dashboard / Status Pending";
                        }
                        else if (actionstatus == "QcPending")
                        {
                            ViewBag.PageHeader = "911 Dashboard / Qc Pending";
                        }
                        else if (actionstatus == "UnAssignedCoordianter" && actiontype == 0)
                        {
                            ViewBag.PageHeader = "911 Dashboard / Auto Cancelled Interview";
                        }
                        else if (actionstatus == "UnAssignedCoordianter" && actiontype == 1)
                        {
                            ViewBag.PageHeader = "911 Dashboard / Auto Cancelled Interview";
                        }
                        else if (actionstatus == "ZoomLink")
                        {
                            ViewBag.PageHeader = "911 Dashboard / Missing Zoom Link";
                        }
                        else if (actionstatus == "UnAttendedZoomLink")
                        {
                            ViewBag.PageHeader = "911 Dashboard / Participant Waiting";
                        }
                        else if (actionstatus == "VideoLink")
                        {
                            ViewBag.PageHeader = "911 Dashboard / Missing Video Link";
                        }
                        else if (actionstatus == "QCPendingReport" || actionstatus == "QCReport")
                        {
                            ViewBag.PageHeader = "911 Dashboard / QC Report";
                        }
                        modelList = modelList.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; x.QCStatusStatusDesc = x.QCStatus.HasValue ? ((QCStatus)x.QCStatus).GetDescription() : string.Empty; return x; }).ToList();
                        modelList = modelList.OrderBy(x => x.interviewDate).ToList();
                        return View(modelList);
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
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UnassignedInterviews(UnassignedInterviewsFilter filter, string actionstatus, int actiontype)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        if (filter != null)
                        {
                            HttpContext.Session.SetString("UnassignedInterviewsFilters", JsonConvert.SerializeObject(filter));
                        }
                        ViewBag.PageHeader = "";
                        ViewBag.Actiontype = actiontype;
                        ViewBag.actionstatus = actionstatus;
                        if (actionstatus == "InterviewCount")
                        {
                            ViewBag.PageHeader = "911 Dashboard / Unassigned Interviews";
                        }
                        else if (actionstatus == "MeetingStatus")
                        {
                            ViewBag.PageHeader = "911 Dashboard / Status Pending";
                        }
                        else if (actionstatus == "QcPending")
                        {
                            ViewBag.PageHeader = "911 Dashboard / Qc Pending";
                        }
                        else if (actionstatus == "UnAssignedCoordianter" && actiontype == 0)
                        {
                            ViewBag.PageHeader = "911 Dashboard / Unassigned RB Coordinator";
                        }
                        else if (actionstatus == "UnAssignedCoordianter" && actiontype == 1)
                        {
                            ViewBag.PageHeader = "911 Dashboard / PENDING CANDIDATE FEEDBACK";
                        }
                        else if (actionstatus == "ZoomLink")
                        {
                            ViewBag.PageHeader = "911 Dashboard / Missing Zoom Link";
                        }
                        else if (actionstatus == "UnAttendedZoomLink")
                        {
                            ViewBag.PageHeader = "911 Dashboard / Participant Waiting";
                        }
                        else if (actionstatus == "VideoLink")
                        {
                            ViewBag.PageHeader = "911 Dashboard / Missing Video Link";
                        }
                        else if (actionstatus == "QCPendingReport" || actionstatus == "QCReport")
                        {
                            ViewBag.PageHeader = "911 Dashboard / QC Report";
                        }
                        var modelList = new List<MeetingVM>();
                        if (actiontype == 1 && actionstatus == "UnAssignedCoordianter")
                        {
                            AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
                            var result = _azureTableStorageService.GetAllEntities<SurveyVM>("RisebirdSurvey");
                            string email = string.Empty;
                            if (HttpContext.Session.GetString("email") != null)
                            {
                                email = HttpContext.Session.GetString("email")?.Trim();
                                email = email.Replace("\"", "");
                            }
                            var myInterview = _meetingService.GetHRAssignedInterviews(email);
                            var interviewerids = myInterview.Select(x => x.id).Distinct().ToList();
                            if (interviewerids != null)
                            {
                                foreach (var item in myInterview)
                                {
                                    if (result.Where(x => x.interviewid == item.id).Count() == 0)
                                    {
                                        modelList.Add(new MeetingVM
                                        {
                                            IRFId = item.IRFID,
                                            interviewDate = item.interviewDate,
                                            candidateName = item.candidateName,
                                            candidateEmail = item.candidateEmail,
                                            imid = item.id,
                                            interviewer = item.interviewer,
                                            confirmationStatus = "1",
                                            HRCoordinatorEmailId = email,
                                            MeetingStatus = item.MeetingStatus,
                                            InterviewStatus = item.InterviewStatus,
                                        });
                                    }
                                }
                            }
                        }
                        else
                        {
                            modelList = await _dashboardService.FetchRibbonDetailsValuesbydate(actionstatus, emailid, actiontype, profileName);
                        }
                        if (modelList == null)
                        {
                            modelList = new List<MeetingVM>();
                        }
                        ViewBag.actionstatus = actionstatus;
                        ViewBag.Actiontype = actiontype;
                        if (filter.StartDate != null && filter.EndDate != null)
                        {
                            if (actionstatus == "UnAttendedZoomLink")
                            {
                                filter.EndDate = Convert.ToDateTime(filter.EndDate).AddDays(1);
                            }
                            modelList = modelList.Where(x => x.interviewDate >= filter.StartDate && x.interviewDate <= filter.EndDate).ToList();
                        }
                        filter.Client = filter.Client == null ? filter.Client = modelList.Select(x => x.Name).ToArray() : filter.Client = filter.Client;
                        filter.ClientHREmail = filter.ClientHREmail == null ? filter.ClientHREmail = modelList.Select(x => x.ClientHrContact).ToArray() : filter.ClientHREmail = filter.ClientHREmail;
                        modelList = modelList.Where(c => (filter.Client.Count() > 0 ? filter.Client.Contains(c.Name) : c.Name == null)
                        && (filter.ClientHREmail.Count() > 0 ? filter.ClientHREmail.Contains(c.ClientHrContact) : c.ClientHrContact == null)).ToList();

                        modelList = modelList.Select(x =>
                        {
                            x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription();
                            return x;
                        }).ToList();
                        modelList = modelList.Select(x =>
                        {
                            x.MeetingStatusStatusDesc = ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription();
                            return x;
                        }).ToList();
                        modelList = modelList.Select(x =>
                        {
                            x.intervieweStatusStatDesc = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription();
                            return x;
                        }).ToList();
                        modelList = modelList.OrderBy(x => x.interviewDate).ToList();
                        return View(modelList);
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
        public async Task<IActionResult> ResetUnassignedInterviewsFilter(string actionstatus)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("UnassignedInterviewsFilters");
                    return RedirectToAction("UnassignedInterviews", new { actionstatus = actionstatus });
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("UnassignedInterviews");
            }

        }

        [HttpGet]
        public async Task<JsonResult> FetchRibbonValues(int section, bool getDeletedZoomMeetingsCount = false)
        {
            if (section == 1)
            {
                var modelData = new List<SurveyVM>();
                AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
                var result = _azureTableStorageService.GetAllEntities<SurveyVM>("RisebirdSurvey");
                string email = string.Empty;
                if (HttpContext.Session.GetString("email") != null)
                {
                    email = HttpContext.Session.GetString("email")?.Trim();
                    email = email.Replace("\"", "");
                }
                var Ribbonmodel = await _dashboardService.GetNotUpdatedMeetingsCountbyHr(email, getDeletedZoomMeetingsCount);
                var myInterview = _meetingService.GetHRAssignedInterviews(email);
                var WeekDate = DateTime.Now.AddDays(-7);
                var interviewerids = myInterview.Where(x => x.interviewDate >= WeekDate).Select(x => x.id).Distinct().ToList();
                int countvalue = 0;
                if (interviewerids != null)
                {
                    foreach (var item in interviewerids)
                    {
                        if (result.Where(x => x.interviewid == item).Count() == 0)
                        {
                            countvalue++;
                        }
                    }
                }
                Ribbonmodel.UnfilledCommentsCount = countvalue;
                return Json(Ribbonmodel);
            }
            else
            {
                var Emailid = string.Empty;
                if (HttpContext.Session.GetString("email") != null)
                {
                    Emailid = HttpContext.Session.GetString("email")?.Trim();
                    Emailid = Emailid.Replace("\"", "");
                }
                var Ribbonmodel = await _dashboardService.GetNotUpdatedMeetingsCount(Emailid, getDeletedZoomMeetingsCount);
                return Json(Ribbonmodel);
            }
        }

        [HttpPost]
        public JsonResult SendNotoificationForParticipantWaiting()
        {
            //Send mail only one time no duplicate mail
            return Json("Success");
        }

        [HttpPost]
        public async Task<JsonResult> FetchZoomMeetingHistory(string meetingid)
        {
            var modelList = await _dashboardService.FetchZoomMeetingHistory(meetingid);
            return Json(modelList);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> DeletedZoomMeetings(int actionType)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    var model = await _dashboardService.GetUpcomingMeetingsDeletedFromZoom(emailId);
                    ViewBag.Actiontype = actionType;
                    return View(model);
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
        #region Interviewer Dashboard
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> InterviewerIndex()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        var model = new InterviewerDashboardVM();
                        model = await _dashboardService.GetInterviewDetailsCount(emailid);
                        var codedname = _meetingService.FetchInterviewerCodedName(emailid);
                        model.CodedName = codedname;
                        //var isCodedResumePresent = _meetingService.CheckInterviewerCodedResume(emailid);
                        ViewBag.isCodedResumePresent = false;
                        ViewBag.TimeExpire = _configuration["InterviewerReport:ExpireTime"];
                        //Boradcastmessage
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
        public async Task<JsonResult> InterviewerCandidateRatingDetails()
        {
            var candidaterating = 0.0;
            if (HttpContext.Session.GetString("email") != null)
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                int interviewerid = 0;
                var Candidatemodel = new List<InterviewerRating>();
                AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
                var result = _azureTableStorageService.GetAllEntities<SurveyVM>("RisebirdSurvey");
                interviewerid = _userRepository.GetInterviewerid(emailid);
                result = result.Where(x => x.Interviewerid == interviewerid).ToList();
                foreach (var item in result)
                {
                    if (Candidatemodel.Where(x => x.Interviewid == item.interviewid).Count() == 0)
                    {
                        var rating = !string.IsNullOrEmpty(item.Question2) ? item.Question2 : "0";
                        Candidatemodel.Add(new InterviewerRating
                        {
                            Interviewid = item.interviewid,
                            StarRating = Convert.ToInt32(rating),
                        });
                    }
                }
                if (Candidatemodel.Count() != 0)
                {
                    candidaterating = Math.Round(Candidatemodel.Select(x => x.StarRating).Average(), 2);
                }
                return Json(new { candidateRating = candidaterating });
            }
            else
            {
                return Json(new { candidateRating = candidaterating });
            }
        }
        [Authorize]
        [HttpPost]
        public async Task<JsonResult> InterviewerClientRatingDetails()
        {
            var clientrating = 0.0;
            if (HttpContext.Session.GetString("email") != null)
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var Clientmodel = new List<InterviewerRating>();
                var results = await _interviewerService.fetchClientFeedbackbyInterviewer(emailid);
                if (results != null)
                    Clientmodel = results;

                if (Clientmodel.Count() != 0)
                {
                    clientrating = Math.Round(Clientmodel.Select(x => x.StarRating).Average(), 2);
                }
                return Json(new { clientRating = clientrating });
            }
            else
            {
                return Json(new { clientRating = clientrating });
            }
        }
        [Authorize]
        [HttpPost]
        public async Task<JsonResult> InterviewerPendingPaymentDetails()
        {
            var Payment = 0;
            var PaidPayment = 0;
            if (HttpContext.Session.GetString("email") != null)
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var model = await _meetingService.RBDetails(emailid);
                model = model.Select(x => { x.InterviewStatusDetails = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription(); x.MeetingStatusDetails = ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription(); return x; }).ToList();
                Payment = model.Where(x => x.Status == "Submitted" || x.Status == "Pending").Sum(x => x.Points) - model.Where(x => x.Status == "Advance").Sum(x => x.Points);
                PaidPayment = model.Where(x => x.Status == "Paid").Sum(x => x.Points);
                return Json(new { payment = Payment, paidpayment = PaidPayment });
            }
            else
            {
                return Json(new { payment = Payment, paidpayment = PaidPayment });
            }
        }
        #endregion
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> UnProcessedResume(int actiontype)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        var modelList = new List<MeetingVM>();
                        modelList = await _dashboardService.FetchRibbonDetailsValues("UnProcessedResume", emailid, actiontype, profileName);
                        if (modelList == null)
                        {
                            modelList = new List<MeetingVM>();
                        }
                        ViewBag.PageHeader = "";
                        ViewBag.Actiontype = actiontype;
                        ViewBag.PageHeader = "911 Dashboard / Unprocessed Resume";
                        modelList = modelList.OrderBy(x => x.interviewDate).ToList();
                        return View(modelList);
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
                return RedirectToAction("Index");
            }
        }
        [Authorize]
        [HttpGet]
        public async Task<FileContentResult> UnassignedInterviewsExportToExcel(string actionstatus, int actiontype)
        {
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("QC Report Details");
            string fileName = "Risebird_" + DateTime.Now.ToString("dd/MM/yyyy") + "_QCReport" + ".xlsx";
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        var modelList = new List<MeetingVM>();
                        var filter = new UnassignedInterviewsFilter();
                        if (HttpContext.Session.GetString("UnassignedInterviewsFilters") != null)
                        {
                            filter = JsonConvert.DeserializeObject<UnassignedInterviewsFilter>(HttpContext.Session.GetString("UnassignedInterviewsFilters"));
                            modelList = await _dashboardService.FetchRibbonDetailsValuesbydate(actionstatus, emailid, actiontype, profileName);
                        }
                        else
                        {
                            modelList = await _dashboardService.FetchRibbonDetailsValues(actionstatus, emailid, actiontype, profileName);
                        }
                        if (modelList == null)
                        {
                            modelList = new List<MeetingVM>();
                        }
                        if (filter != null)
                        {
                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                if (actionstatus == "UnAttendedZoomLink")
                                {
                                    filter.EndDate = Convert.ToDateTime(filter.EndDate).AddDays(1);
                                }
                                modelList = modelList.Where(x => x.interviewDate >= filter.StartDate && x.interviewDate <= filter.EndDate).ToList();
                            }
                            filter.Client = filter.Client == null ? filter.Client = modelList.Select(x => x.Name).ToArray() : filter.Client = filter.Client;
                            filter.ClientHREmail = filter.ClientHREmail == null ? filter.ClientHREmail = modelList.Select(x => x.ClientHrContact).ToArray() : filter.ClientHREmail = filter.ClientHREmail;
                            modelList = modelList.Where(c => (filter.Client.Count() > 0 ? filter.Client.Contains(c.Name) : c.Name == null)
                            && (filter.ClientHREmail.Count() > 0 ? filter.ClientHREmail.Contains(c.ClientHrContact) : c.ClientHrContact == null)).ToList();
                        }
                        modelList = modelList.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; x.QCStatusStatusDesc = x.QCStatus.HasValue ? ((QCStatus)x.QCStatus).GetDescription() : string.Empty; return x; }).ToList();
                        modelList = modelList.OrderBy(x => x.interviewDate).ToList();
                        if (modelList != null && modelList.Count() > 0)
                        {
                            Sheet.Cells["A1:AI1"].Style.Font.Bold = true;
                            Sheet.Cells["A1:AI1"].Style.Font.Size = 12;
                            Sheet.Cells["A1:AI1"].Style.WrapText = true;
                            int startColumn = 1; int endColumn = 12;
                            for (int a = startColumn; a <= endColumn; a++)
                            {
                                Sheet.Column(a).Width = 40;
                            }
                            Sheet.Cells[1, 1].Value = "Skill Channel";
                            Sheet.Cells[1, 2].Value = "Interview Date";
                            Sheet.Cells[1, 3].Value = "Candidate Name";
                            Sheet.Cells[1, 4].Value = "Candidate Email";
                            Sheet.Cells[1, 5].Value = "Interviewer";
                            Sheet.Cells[1, 6].Value = "Meeting Status";
                            Sheet.Cells[1, 7].Value = "Interview Status";
                            Sheet.Cells[1, 8].Value = "Client";
                            if (actionstatus == "QCReport")
                            {
                                Sheet.Cells[1, 9].Value = "Red Flags";
                            }
                            int i = 2;
                            foreach (var item in modelList)
                            {
                                Sheet.Cells[i, 1].Value = item.IRFId;
                                Sheet.Cells[i, 2].Value = item.interviewDate.ToString("MM/dd/yyyy HH:mm");
                                Sheet.Cells[i, 3].Value = item.candidateName;
                                Sheet.Cells[i, 4].Value = item.candidateEmail;
                                Sheet.Cells[i, 5].Value = item.interviewer;
                                Sheet.Cells[i, 6].Value = item.MeetingStatusStatusDesc;
                                Sheet.Cells[i, 7].Value = item.intervieweStatusStatDesc;
                                Sheet.Cells[i, 8].Value = item.Name;
                                if (actionstatus == "QCReport")
                                {
                                    Sheet.Cells[i, 9].Value = item.Redflags;
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
                    else
                    {
                        ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                        Rng.Value = "No data available in table !!!";
                        return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                }
                else
                {
                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                    Rng.Value = "No data available in table !!!";
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "No data available in table !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
        [Authorize]
        [HttpPost]
        public async Task<JsonResult> FetchAlertMessage()
        {
            var modelList = await _dashboardService.FetchAlertMessage();

            return Json(modelList);

        }
    }
}