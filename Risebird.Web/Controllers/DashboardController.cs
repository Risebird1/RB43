
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OfficeOpenXml;
using Risebird.Business;

//using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
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
        private readonly ICandidateService _candidateService;
        private readonly IAssistantServiceHelper _assistantServiceHelper;
        private readonly IIRFRepository _irfRepository;
        private readonly ITwilioHelper _twilioHelper;

        public DashboardController(IDashboardService dashboardService, IApplicationErrorLogger applicationErrorLogger,
            IAzureStorageHelper azureStorageHelper, INotificationHelper notificationHelper,
            IUserService userService, IConfiguration configuration, IMeetingService meetingService,
            IFileMimeTypeHelper fileMimeTypeHelper, IUserRepository userRepository, IInterviewerService interviewerService,
            IMeetingRespository meetingRespository, IZoomHelper zoomHelper, ICandidateService candidateService,
            IAssistantServiceHelper assistantServiceHelper, IIRFRepository irfRepository, ITwilioHelper twilioHelper)
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
            _candidateService = candidateService;
            _assistantServiceHelper = assistantServiceHelper;
            _irfRepository = irfRepository;
            _twilioHelper = twilioHelper;
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
                    ViewBag.ProfileName = profileName;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "TechSupport")
                    {
                        //await CandidateIVRCall();
                        //try
                        //{
                        //    //get transinterviewers for whom screening needs to be done
                        //    var interviewers = await _irfRepository.GetTransInterviewersForOpenAIScreening();



                        //    //Parallel.ForEach(interviewers, item =>
                        //    //{
                        //    //    if (!string.IsNullOrEmpty(item.ResumeLink))
                        //    //    {
                        //    //        var response = _assistantServiceHelper.ScreenAsync(new ScreeningRequest { JDTitle = item.JDTitle, ResumeLink = item.ResumeLink, Type = ProfileNames.Interviewer }).Result;

                        //    //        //save the response to db

                        //    //        var responseInJson = JsonConvert.SerializeObject(response);
                        //    //        bool saveOperationResult = true;
                        //    //        if (!string.IsNullOrEmpty(responseInJson) && responseInJson != "null")
                        //    //        {
                        //    //            saveOperationResult = _irfRepository.SaveTransInterviewerOpenAIScreeningResults(item.id, responseInJson).Result;
                        //    //        }
                        //    //    }
                        //    //});
                        //    foreach (var item in interviewers)
                        //    {


                        //        //call screening function
                        //        if (!string.IsNullOrEmpty(item.ResumeLink))
                        //        {
                        //            if ((await _azureStorageHelper.IsDocFileAsync(item.ResumeLink)) && string.IsNullOrEmpty(item.ConvertedFromDocResumeLink))
                        //            {
                        //                var uploadedResumeUri = await _azureStorageHelper.DownloadAndConvertFromDocToPdfAsync(item.ResumeLink);

                        //                if (!string.IsNullOrEmpty(uploadedResumeUri))
                        //                {
                        //                    item.ResumeLink = uploadedResumeUri;
                        //                    //update the interviewers' uri
                        //                    await _irfRepository.SaveInterviewerConvertedFromDocResumeLink(item.id, uploadedResumeUri);
                        //                }
                        //            }

                        //            var resumeStringContent = await ReadPdfFromUrlAsync(item.ResumeLink);
                        //            var response = await _assistantServiceHelper.GenerateResumeAnalysis(resumeStringContent, item.JDTitle);

                        //            //var response = await _assistantServiceHelper.ScreenAsync(new ScreeningRequest { JDTitle = item.JDTitle, ResumeLink = (item.ResumeLink.EndsWith(".doc") ? item.ConvertedFromDocResumeLink : item.ResumeLink), Type = ProfileNames.Interviewer });

                        //            //save the response to db
                        //            if (response != null)
                        //            {
                        //                //var responseInJson = JsonConvert.SerializeObject(response);
                        //                var aiAnalysisModel = JsonConvert.DeserializeObject<InterviewerResumeFitmentAnalysisSchema>(response.Data?.First()?.Content?.FirstOrDefault()?.Text?.Value);

                        //                bool saveOperationResult = true;
                        //                if (aiAnalysisModel != null)
                        //                {
                        //                    //saveOperationResult = await _irfRepository.SaveTransInterviewerOpenAIScreeningResults(item.id, responseInJson);
                        //                    saveOperationResult = await _irfRepository.SaveInterviewerOpenAIScreeningResultsV2(item.IRFId, item.UserId, aiAnalysisModel);//responseInJson.Replace("'", "")

                        //                }
                        //            }
                        //            //else
                        //            //{
                        //            //    //check if the resume is a doc file
                        //            //    if (await _azureStorageHelper.IsDocFileAsync(item.ResumeLink))
                        //            //    {
                        //            //        var uploadedResumeUri = await _azureStorageHelper.DownloadAndConvertFromDocToPdfAsync(item.ResumeLink);

                        //            //        if (!string.IsNullOrEmpty(uploadedResumeUri))
                        //            //        {
                        //            //            //update the interviewers' uri
                        //            //        }
                        //            //    }
                        //            //}


                        //        }

                        //    }
                        //}
                        //catch (Exception ex)
                        //{
                        //    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        //}




                        var model = new DashboardVM();
                        //await _assistantServiceHelper.DeleteFiles();
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
                        model.ProgressOptions = new SelectList(await _dashboardService.GetProgressOptions(), "id", "Name");
                        var Assassination = from AssassinationStatus s in Enum.GetValues(typeof(AssassinationStatus))
                                            select new { ID = (int)s, Name = s.GetDescription() };
                        model.AssassinationList = new SelectList(Assassination, "ID", "Name");
                        //need to Update
                        //var channelrequest = await _dashboardService.FetchChannelRequests();
                        //model.ChannelRequest = channelrequest;
                        var interviewmanagement = (IList<InterviewManagementVM>)await _dashboardService.GetInterviewManagement(Emailid);
                        interviewmanagement = interviewmanagement?.OrderByDescending(x => x.interviewDate).ToList();
                        if (interviewmanagement != null)
                        {
                            foreach (var item in interviewmanagement)
                            {
                                item.interviewDate = item.interviewDate.Date;
                            }
                            model.InterviewManagement = interviewmanagement;
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
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        //private async Task CandidateIVRCall()
        //{
        //    //var candidatemeetinglist = await _meetingService.FetchCandidateMeetingsForReminder();
        //    string timeZoneValue = "IST";
        //    //foreach (var item in candidatemeetinglist)
        //    //{
        //    //    try
        //    //    {
        //    //        timeZoneValue = "IST";

        //    //        var primaryskill = item.PrimarySkillInvite;
        //    //        string yesbutton = "https://portal.risebird.io/Interview/CandidateMeetingsStatus?meetingid=" + item.Interviewid + "&status=1&uniqueid=" + item.uniqueid + "&Irfid=" + item.Irfid + "&candidateId=" + item.candidateid;
        //    //        string nobutton = "https://portal.risebird.io/Interview/CandidateMeetingsStatus?meetingid=" + item.Interviewid + "&status=2&uniqueid=" + item.uniqueid + "&Irfid=" + item.Irfid + "&candidateId=" + item.candidateid;
        //    //        string rescheulebutton = "https://portal.risebird.io/Interview/CandidateRescheduleMeetingsStatus?meetingid=" + item.Interviewid + "&status=3&uniqueid=" + item.uniqueid + "&Irfid=" + item.Irfid + "&candidateId=" + item.candidateid;
        //    //        string needcleritybutton = "https://portal.risebird.io/Interview/CandidateMeetingsStatus?meetingid=" + item.Interviewid + "&status=4&uniqueid=" + item.uniqueid + "&Irfid=" + item.Irfid + "&candidateId=" + item.candidateid;

        //    //        if (!string.IsNullOrWhiteSpace(item.TimeZonesValue)
        //    //            && item.TimeZoneoffset.HasValue
        //    //            && !item.TimeZonesValue.Contains("Chennai"))
        //    //        {
        //    //            timeZoneValue = item.TimeZonesValue.Split(")").ElementAtOrDefault(1);

        //    //            item.interviewDate = item.interviewDate.AddMinutes(-(330 + item.TimeZoneoffset.Value));

        //    //        }

        //    //        await _twilioHelper.MakeIVRCallForCandidateMeetingReminder(item.MobileNumber, item.FirstName, item.uniqueid, item.Irfid, item.PrimarySkillInvite, item.Interviewid, item.candidateid, item.interviewDate, item.TimeZonesValue);

        //    //    }
        //    //    catch (Exception ex)
        //    //    {
        //    //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //    //    }
        //    //}
        //    await _twilioHelper.MakeIVRCallForCandidateMeetingReminder("+918888571923", "Harshit", string.Empty, 16323, "expertise", 427827, 0, DateTime.Now, timeZoneValue);
        //}
        public async Task<string> ReadPdfFromUrlAsync(string pdfUrl)
        {

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    byte[] pdfBytes = await client.GetByteArrayAsync(pdfUrl);

                    using (MemoryStream stream = new MemoryStream(pdfBytes))
                    {
                        using (var pdfDocument = UglyToad.PdfPig.PdfDocument.Open(stream))
                        {
                            string text = "";
                            foreach (var page in pdfDocument.GetPages())
                            {
                                text += page.Text + "\n";
                            }
                            return text;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //var resultDe = _activityTrackerService.DebugLogger($"AI Resume Screener function error : {ex.Message}, {ex.StackTrace}", "aiScreener", string.Empty).Result;

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
                model.ProgressOptions = new SelectList(await _dashboardService.GetProgressOptions(), "id", "Name");
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
                        var Whatsapp = _userService.GetWhatsappno(emailid);
                        if (Whatsapp == null)
                        {
                            ViewBag.Whatsappflag = true;
                        }
                        else
                        {
                            ViewBag.Whatsappflag = false;
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
        public async Task<ActionResult> AddRequirement()
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
                var menuData = await _userService.GetMenuBasedOnUserEmailld("Admin");
                var menus = menuData.GroupBy(p => p.functionName).Select(g => g.First()).ToList();
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
                    var resultset = await _notificationHelper.ChannelRequestNotification(_mail, model.Client, model.PrimarySkill, model.Requester);
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
                    var menuData = await _userService.GetMenuBasedOnUserEmailld("Admin");
                    var menus = menuData.GroupBy(p => p.functionName).Select(g => g.First()).ToList();
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
                if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement")
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
                if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement")
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
                if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller")
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller")
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller")
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
        public async Task<IActionResult> UnassignedInterviews(string actionstatus, int actiontype, int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.emailid = emailid;
                    ViewBag.PageFrom = "Completed";
                    ViewBag.ReturnBack = returnBack;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Interviewer" || profileName == "Quality Controller" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "TechSupport" || profileName == "Finance")
                    {
                        if (HttpContext.Session.GetString("UnassignedInterviewsFilters") != null)
                            HttpContext.Session.Remove("UnassignedInterviewsFilters");
                        if (actionstatus == RibbonActionType.DeletedZoomMeetings)
                        {
                            return RedirectToAction("DeletedZoomMeetings", new { /*actionStatus = actionstatus, */actionType = actiontype, returnBack = returnBack });
                        }
                        var modelList = new List<MeetingVM>();
                        if (actiontype == 1 && actionstatus == RibbonActionType.UnAssignedCoordianter)
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
                        ViewBag.ProfileName = profileName;
                        if (actionstatus == RibbonActionType.InterviewCount)
                        {
                            ViewBag.PageHeader = "Unassigned Interviews";
                        }
                        else if (actionstatus == RibbonActionType.MeetingStatus)
                        {
                            ViewBag.PageHeader = "Status Pending";
                        }
                        else if (actionstatus == RibbonActionType.QcPending)
                        {
                            ViewBag.PageHeader = "Qc Pending";
                        }
                        else if (actionstatus == RibbonActionType.UnAssignedCoordianter && actiontype == 0)
                        {
                            ViewBag.PageHeader = "Auto Cancelled Interview";
                        }
                        else if (actionstatus == RibbonActionType.UnAssignedCoordianter && actiontype == 1)
                        {
                            ViewBag.PageHeader = "Auto Cancelled Interview";
                        }
                        else if (actionstatus == RibbonActionType.ZoomLink)
                        {
                            ViewBag.PageHeader = "Missing Zoom Link";
                        }
                        else if (actionstatus == RibbonActionType.UnAttendedZoomLink)
                        {
                            ViewBag.PageHeader = "Participant Waiting";
                        }
                        else if (actionstatus == RibbonActionType.VideoLink)
                        {
                            ViewBag.PageHeader = "Missing Video Link";
                        }
                        else if (actionstatus == RibbonActionType.QCPendingReport || actionstatus == RibbonActionType.QCReport)
                        {
                            ViewBag.PageHeader = "QC Report";
                        }
                        else if (actionstatus == RibbonActionType.PotentialRiskByClient)
                        {
                            ViewBag.PageHeader = "Potential Risk By Client";
                        }
                        else if (actionstatus == RibbonActionType.PotentialRiskInterviewCount)
                        {
                            ViewBag.PageHeader = "Potential Risk By Candidate";
                        }
                        else if (actionstatus == RibbonActionType.NeedClarity)
                        {
                            ViewBag.PageHeader = "Need Clarity";
                        }
                        else if (actionstatus == RibbonActionType.ReScheduleCount)
                        {
                            ViewBag.PageHeader = "Reschedule Interview";
                        }
                        else if (actionstatus == RibbonActionType.PotentialRiskInterviewCount)
                        {
                            ViewBag.PageHeader = "Potential Risk By Candidate Interview";
                        }
                        else if (actionstatus == RibbonActionType.SuspeciousInterviewerCount)
                        {
                            ViewBag.PageHeader = "Suspicious Interviewer";
                        }

                        else if (actionstatus == RibbonActionType.CancelledInterview)
                        {
                            ViewBag.PageHeader = "Cancelled Interviwes";
                        }

                        else if (actionstatus == RibbonActionType.RedoInterview)
                        {
                            ViewBag.PageHeader = "Redo Interviwes";
                        }
                        else if (actionstatus == RibbonActionType.PotentialRiskByClient)
                        {
                            ViewBag.PageHeader = "Potential Risk By Client Interviwes";
                        }
                        else if (actionstatus == RibbonActionType.InterviewerChurn)
                        {
                            ViewBag.PageHeader = "Interviewer Churn Data";
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
        public async Task<IActionResult> UnassignedInterviews(UnassignedInterviewsFilter filter, string actionstatus, int actiontype, int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.emailid = emailid;
                    ViewBag.ProfileName = profileName;
                    ViewBag.ReturnBack = returnBack;
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "Quality Controller" || profileName == "Finance" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SupportAdmin" || profileName == "HR" || profileName == "Interviewer" || profileName == "TechSupport")
                    {
                        if (filter != null)
                        {
                            HttpContext.Session.SetString("UnassignedInterviewsFilters", JsonConvert.SerializeObject(filter));
                        }
                        ViewBag.PageHeader = "";
                        ViewBag.Actiontype = actiontype;
                        ViewBag.actionstatus = actionstatus;
                        if (actionstatus == RibbonActionType.InterviewCount)
                        {
                            ViewBag.PageHeader = "Unassigned Interviews";
                        }
                        else if (actionstatus == RibbonActionType.MeetingStatus)
                        {
                            ViewBag.PageHeader = "Status Pending";
                        }
                        else if (actionstatus == RibbonActionType.QcPending)
                        {
                            ViewBag.PageHeader = "Qc Pending";
                        }
                        else if (actionstatus == RibbonActionType.PotentialRiskByClient)
                        {
                            ViewBag.PageHeader = "Potential Risk By Client";
                        }
                        else if (actionstatus == RibbonActionType.PotentialRiskInterviewCount)
                        {
                            ViewBag.PageHeader = "Potential Risk By Candidate";
                        }
                        else if (actionstatus == RibbonActionType.UnAssignedCoordianter && actiontype == 0)
                        {
                            ViewBag.PageHeader = "Unassigned RB Coordinator";
                        }
                        else if (actionstatus == RibbonActionType.UnAssignedCoordianter && actiontype == 1)
                        {
                            ViewBag.PageHeader = "PENDING CANDIDATE FEEDBACK";
                        }
                        else if (actionstatus == RibbonActionType.ZoomLink)
                        {
                            ViewBag.PageHeader = "Missing Zoom Link";
                        }
                        else if (actionstatus == RibbonActionType.UnAttendedZoomLink)
                        {
                            ViewBag.PageHeader = "Participant Waiting";
                        }
                        else if (actionstatus == RibbonActionType.VideoLink)
                        {
                            ViewBag.PageHeader = "Missing Video Link";
                        }
                        else if (actionstatus == RibbonActionType.QCPendingReport || actionstatus == RibbonActionType.QCReport)
                        {
                            ViewBag.PageHeader = "QC Report";
                        }
                        else if (actionstatus == RibbonActionType.ReScheduleCount)
                        {
                            ViewBag.PageHeader = "Reschedule Interview";
                        }
                        else if (actionstatus == RibbonActionType.PotentialRiskInterviewCount)
                        {
                            ViewBag.PageHeader = "Potential Risk By Candidate Interview";
                        }
                        else if (actionstatus == RibbonActionType.SuspeciousInterviewerCount)
                        {
                            ViewBag.PageHeader = "Suspicious Interviewer";
                        }

                        else if (actionstatus == RibbonActionType.CancelledInterview)
                        {
                            ViewBag.PageHeader = "Cancelled Interviwes";
                        }

                        else if (actionstatus == RibbonActionType.RedoInterview)
                        {
                            ViewBag.PageHeader = "Redo Interviwes";
                        }
                        else if (actionstatus == RibbonActionType.PotentialRiskByClient)
                        {
                            ViewBag.PageHeader = "Potential Risk By Client Interviwes";
                        }
                        else if (actionstatus == RibbonActionType.InterviewerChurn)
                        {
                            ViewBag.PageHeader = "Interviewer Churn Data";
                        }

                        var modelList = new List<MeetingVM>();
                        if (actiontype == 1 && actionstatus == RibbonActionType.UnAssignedCoordianter)
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
                            modelList = await _dashboardService.FetchRibbonDetailsValuesbydate(actionstatus, emailid, actiontype, profileName, filter);
                        }
                        if (modelList == null)
                        {
                            modelList = new List<MeetingVM>();
                        }
                        ViewBag.actionstatus = actionstatus;
                        ViewBag.Actiontype = actiontype;
                        if (filter.StartDate != null && filter.EndDate != null)
                        {
                            if (actionstatus == RibbonActionType.UnAttendedZoomLink)
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
                        if (actionstatus == RibbonActionType.InterviewCount)
                        {
                            modelList = modelList.OrderBy(x => x.IRFId).ToList();
                        }
                        else
                        {
                            modelList = modelList.OrderBy(x => x.interviewDate).ToList();
                        }
                        //modelList = modelList.OrderBy(x => x.interviewDate).ToList();
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
        /// <summary>
        /// List of all unassigned interviews clubbed together by Skill Channel, with action to send mail notification
        /// </summary>
        /// <param name="actiontype"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SendOpenInterviewsNotification(int actiontype)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SupportAdmin" || profileName == "SeniorInterviewerManagement" || profileName == "InterviewerManagement")
                    {
                        var modelList = new List<MeetingVM>();
                        modelList = await _dashboardService.FetchRibbonDetailsValues(RibbonActionType.InterviewCount, emailid, actiontype, profileName);
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
                return RedirectToAction("UnassignedInterviews");
            }

        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ResetUnassignedInterviewsFilter(string actionstatus, int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("UnassignedInterviewsFilters");
                    return RedirectToAction("UnassignedInterviews", new { actionstatus = actionstatus, returnBack = returnBack });
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
                var modelList = await _dashboardService.FetchParticipantwaitingDetails(email);
                Ribbonmodel.CandidatewaitingCount = modelList.Where(x => x.PerformedOn != null).Count();
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
                var modelList = await _dashboardService.FetchParticipantwaitingDetails(Emailid);
                Ribbonmodel.CandidatewaitingCount = modelList.Where(x => x.PerformedOn != null).Count();
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
        public async Task<IActionResult> DeletedZoomMeetings(int actionType, int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    var model = await _dashboardService.GetUpcomingMeetingsDeletedFromZoom(emailId);
                    ViewBag.Actiontype = actionType;
                    ViewBag.PageHeader = "";
                    ViewBag.PageHeader = "Deleted Zoom Meetings";
                    ViewBag.ReturnBack = returnBack;
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
                        //var codedname = await _meetingService.FetchInterviewerCodedName(emailid);
                        if (HttpContext.Session.GetString("InterviewerCodedName") != null)
                        {
                            model.CodedName = HttpContext.Session.GetString("InterviewerCodedName")?.Trim().Replace("\"", "");
                        }
                        //var isCodedResumePresent = _meetingService.CheckInterviewerCodedResume(emailid);
                        ViewBag.isCodedResumePresent = false;
                        ViewBag.TimeExpire = _configuration["InterviewerReport:ExpireTime"];
                        ViewBag.isFirstTimeLogin = model.FirstTimePopupDisplayed;
                        ViewBag.PaymentSLAConfirmation = model.PaymentSLAConfirmation;
                        ViewBag.InterviewerRedMarkerReport = model.RedmarkerAcknowledge;
                        ViewBag.VideoKYC = model.VideoKYC;
                        if (model.VideoConfirmation == 0)
                        {
                            ViewBag.VideoConfirmationPopup = true;

                        }
                        else
                        {
                            ViewBag.VideoConfirmationPopup = false;

                        }
                        if (model.AcknowledgementStatus.ToUpper() != "YES" && model.AcknowledgementStatus.ToUpper() != "NO")
                        {
                            return RedirectToAction("InterviewersOnboarding", "Interviewer");
                        }
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
                Candidatemodel = await _interviewerService.fetchClientFeedbackbyInterviewer(emailid, "Candidate");
                if (Candidatemodel.Count() != 0)
                {
                    candidaterating = Math.Round(Candidatemodel.Select(x => x.Question2).Average(), 2);
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
                var results = await _interviewerService.fetchClientFeedbackbyInterviewer(emailid, "Client");
                if (results != null)
                    Clientmodel = results;

                if (Clientmodel.Count() != 0)
                {
                    clientrating = Math.Round(Clientmodel.Select(x => x.Rating).Average(), 2);
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
        public async Task<IActionResult> UnProcessedResume(int actiontype, int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.ReturnBack = returnBack;
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "Quality Controller" || profileName == "Finance" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SupportAdmin" || profileName == "HR" || profileName == "Interviewer" || profileName == "TechSupport")
                    {
                        var modelList = new List<MeetingVM>();
                        modelList = await _dashboardService.FetchRibbonDetailsValues("UnProcessedResume", emailid, actiontype, profileName);
                        if (modelList == null)
                        {
                            modelList = new List<MeetingVM>();
                        }
                        ViewBag.PageHeader = "";
                        ViewBag.Actiontype = actiontype;
                        ViewBag.PageHeader = "Unprocessed Resume";
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement")
                    {
                        var modelList = new List<MeetingVM>();
                        var filter = new UnassignedInterviewsFilter();
                        if (HttpContext.Session.GetString("UnassignedInterviewsFilters") != null)
                        {
                            filter = JsonConvert.DeserializeObject<UnassignedInterviewsFilter>(HttpContext.Session.GetString("UnassignedInterviewsFilters"));
                            modelList = await _dashboardService.FetchRibbonDetailsValuesbydate(actionstatus, emailid, actiontype, profileName, filter);
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
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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
            for (int i = 0; i < modelList.Count; i++)
            {
                modelList[i] = System.Web.HttpUtility.HtmlDecode(modelList[i]);
            }
            return Json(modelList);
        }

        [HttpPost]
        [Authorize]
        public async Task<JsonResult> SaveWhatsappNumber(string whatsappnumber)
        {

            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {

                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");

                //var result = _userService.GetWhatsappno(model.EmailId);
                _ = await _userService.UpdateWhatsappno(emailid, whatsappnumber);
                return Json("Whatsapp added Successfully.");
            }
            else
            {

                return Json("Failed to update Whatsapp number!");
            }

        }
        [Authorize]
        [HttpPost]
        public async Task<JsonResult> FetchClientIRFFeedbackList()
        {
            var results = await _interviewerService.FetchClientIRFFeedbackList();
            if (results == null)
            {
                var Clientmodel = new List<ClientIRFsFeedbackVM>();
                return Json(Clientmodel);
            }
            return Json(results);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Dashboard911()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var Emailid = HttpContext.Session.GetString("email")?.Trim();
                    Emailid = Emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "TechSupport")
                    {
                        var model = new DashboardVM();
                        model.NotUpdatedMeetingsObj = new NotUpdatedMeetings();
                        //model.InterviewerCountObj = await _dashboardService.GetInterviewerCount();
                        model.NotUpdatedMeetingsObj = await _dashboardService.GetNotUpdatedMeetingsCount(Emailid);
                        var modelList = await _dashboardService.FetchParticipantwaitingDetails(Emailid);
                        model.NotUpdatedMeetingsObj.CandidatewaitingCount = modelList.Where(x => x.PerformedOn != null).Count();
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
        public async Task<IActionResult> BlackListCompany(int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    ViewBag.ReturnBack = returnBack;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "SeniorInterviewerManagement")
                    {
                        var model = await _dashboardService.GetBlackkListComapany();

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
                return RedirectToAction("SignOut", "Session");
            }
        }
        [HttpPost]
        public async Task<JsonResult> UndoBlackListCompany(string CompanyName)
        {
            try
            {

                var emailId = HttpContext.Session.GetString("email")?.Trim();
                emailId = emailId.Replace("\"", "");
                BlackListCompanyVM model = new BlackListCompanyVM();
                model.CompanyName = CompanyName;
                model.ModifyDate = DateTime.Now;
                model.ModifyBy = emailId;
                var data = await _dashboardService.UndoBlackListCompanyService(model);
                if (data == true)
                {
                    return Json("Undo blacklisted company marked Successfully");
                }
                else
                {
                    return Json("Failed to mark whether undo blacklisted company.");
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        [HttpPost]
        public async Task<JsonResult> ApprovedBlackListCompany(string CompanyName)
        {
            try
            {

                var emailId = HttpContext.Session.GetString("email")?.Trim();
                emailId = emailId.Replace("\"", "");
                BlackListCompanyVM model = new BlackListCompanyVM();
                model.CompanyName = CompanyName;
                model.ModifyDate = DateTime.Now;
                model.ModifyBy = emailId;
                var data = await _dashboardService.ApprovedBlackListCompanyService(model);
                if (data == true)
                {
                    return Json("Approved blacklisted company has been marked successfully");
                }
                else
                {
                    return Json("Failed to mark whether approved blacklisted company.");
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRedFlagsQCReport(int Interviewid)
        {
            try
            {
                var data = await _dashboardService.GetRedFlagsQCReportService(Interviewid);
                return PartialView("_GetRedFlagsQCReportPartial", data);
            }
            catch (Exception ex)
            {
                var data = new QCFormVM();
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return PartialView("_GetRedFlagsQCReportPartial", data);
            }
        }

        [HttpPost]
        public async Task<JsonResult> UndoSuspiciousInterviewer(int UserId)
        {
            try
            {

                var emailId = HttpContext.Session.GetString("email")?.Trim();
                emailId = emailId.Replace("\"", "");
                // int interviewerProfileid = InterviewerProfileId;
                var data = await _dashboardService.UndoSuspiciousInterviewerService(UserId);
                if (data == true)
                {
                    return Json("Undo suspicious interviewer marked Successfully");
                }
                else
                {
                    return Json("Failed to mark whether undo suspicious interviewer.");
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [HttpPost]
        public async Task<JsonResult> ApprovedRedoInterviews(int InterviewId)
        {
            try
            {
                var emailId = HttpContext.Session.GetString("email")?.Trim();
                emailId = emailId.Replace("\"", "");
                var modifyDate = DateTime.Now;
                var modifyBy = emailId;
                var data = await _dashboardService.ApprovedRedoInterviewsService(InterviewId, modifyBy, modifyDate);
                if (data == true)
                {
                    return Json("Approved Redo Interview has been marked successfully");
                }
                else
                {
                    return Json("Failed to mark whether approved Redo Interview.");
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
        public async Task<IActionResult> RedoRequest(int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    ViewBag.ReturnBack = returnBack;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "SeniorInterviewerManagement")
                    {
                        var model = await _dashboardService.GetRedoRequestService(emailid);

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
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> HighRiskInterviewers(int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    ViewBag.ReturnBack = returnBack;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "SeniorInterviewerManagement")
                    {
                        var model = await _dashboardService.GetHighRiskInterviewersService();

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
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<JsonResult> MoveToUnassign(int meetingId)
        {
            try
            {
                var data = await _dashboardService.MoveToUnassignService(meetingId);
                if (data == true)
                {
                    return Json("Move to Unassign Successfully");
                }

                else
                {
                    return Json("Fail to marked Move To Unassign, Try again after sometime.");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        [HttpPost]
        public async Task<JsonResult> AddRedoInterviews(int InterviewId)
        {
            try
            {
                string Type = "Candidate";
                var data = await _dashboardService.AddRedoInterviewsService(InterviewId, Type);
                if (data == true)
                {
                    return Json(" Redo Interview has been marked successfully");
                }
                else
                {
                    return Json("Failed to mark whether  Redo Interview.");
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
        public async Task<IActionResult> PotentialRiskByCandidate(int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    ViewBag.ReturnBack = returnBack;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "SeniorInterviewerManagement")
                    {
                        var model = await _dashboardService.GetPotentialRiskByCandidateService(emailid);
                        foreach (var item in model)
                        {
                            if (item.InterviewStatus.HasValue)
                            {
                                item.intervieweStatusStatDesc = ((InterviewStatus)(item.InterviewStatus)).GetDescription();
                            }
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
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }
        [Authorize]
        [HttpGet]
        public async Task<FileContentResult> ExportPotentialRiskByCandidate()
        {
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Potential Risk By Candidate");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_Potential Risk By Candidate\r\n" + ".xlsx";
            try
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var modelList = await _dashboardService.GetPotentialRiskByCandidateService(emailid);
                foreach (var item in modelList)
                {

                    if (item.InterviewStatus.HasValue)
                    {
                        item.intervieweStatusStatDesc = ((InterviewStatus)(item.InterviewStatus)).GetDescription();
                    }
                }
                Sheet.Cells["A1:AI1"].Style.Font.Bold = true;
                Sheet.Cells["A1:AI1"].Style.Font.Size = 12;
                Sheet.Cells["A1:AI1"].Style.WrapText = true;
                int startColumn = 1; int endColumn = 12;
                for (int a = startColumn; a <= endColumn; a++)
                {
                    Sheet.Column(a).Width = 30;
                }
                Sheet.Cells[1, 1].Value = "Skill Channel ID";
                Sheet.Cells[1, 2].Value = "Interview Date";
                Sheet.Cells[1, 3].Value = "Interviewer";
                Sheet.Cells[1, 4].Value = "Candidate Name";
                Sheet.Cells[1, 5].Value = "Candidate Email";
                Sheet.Cells[1, 6].Value = "Candidate Contact";
                Sheet.Cells[1, 7].Value = "Interview Status";
                Sheet.Cells[1, 8].Value = "Client Name";
                Sheet.Cells[1, 9].Value = "Channel Owner";
                Sheet.Cells[1, 10].Value = "Red Marker";
                Sheet.Cells[1, 11].Value = "Rating";
                Sheet.Cells[1, 12].Value = "Rated On";
                Sheet.Cells[1, 13].Value = "Payment SLA Status";

                int i = 2;
                if (modelList.Count() > 0)
                {
                    foreach (var item in modelList)
                    {
                        Sheet.Cells[i, 1].Value = item.IRFId;
                        Sheet.Cells[i, 2].Value = item.interviewDate.ToString("MM/dd/yyyy HH:mm");
                        Sheet.Cells[i, 3].Value = item.interviewer;
                        Sheet.Cells[i, 4].Value = item.candidateName;
                        Sheet.Cells[i, 5].Value = item.candidateEmail;

                        Sheet.Cells[i, 6].Value = item.MobileNumber;
                        Sheet.Cells[i, 7].Value = item.intervieweStatusStatDesc;
                        Sheet.Cells[i, 8].Value = item.Name;
                        Sheet.Cells[i, 9].Value = item.ChannelOwner;
                        Sheet.Cells[i, 10].Value = item.RedMarker;
                        Sheet.Cells[i, 11].Value = item.Rating;
                        Sheet.Cells[i, 12].Value = item.RatingAddedOn;
                        if (item.IsApproved == 1)
                        {
                            Sheet.Cells[i, 13].Value = "Approved";
                        }
                        else if (item.IsApproved == 2)
                        {
                            Sheet.Cells[i, 13].Value = "Not Approved";
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



        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Participantwaiting(string type, int returnBack)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                ViewBag.ReturnBack = returnBack;
                var modelList = await _dashboardService.FetchParticipantwaitingDetails(emailid);
                if (type == "Interviewer")
                {
                    ViewBag.Type = "Interviewer";
                    modelList = modelList.Where(x => x.Joinedon != null).ToList();
                }
                else
                {
                    ViewBag.Type = "Candidate";
                    modelList = modelList.Where(x => x.PerformedOn != null).ToList();
                }
                return View(modelList);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> InterviewerVideoKYC(int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    ViewBag.ReturnBack = returnBack;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "SeniorInterviewerManagement")
                    {
                        var model = await _dashboardService.GeInterviewerVideoKYC();

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
                return RedirectToAction("SignOut", "Session");
            }
        }

        [HttpPost]
        public async Task<JsonResult> ApprovedInterviewerVideoKYC(int id, string type)
        {
            try
            {

                var emailId = HttpContext.Session.GetString("email")?.Trim();
                emailId = emailId.Replace("\"", "");
                var data = await _dashboardService.UpdateInterviewerVideoKYC(id, type);
                if (type == "Approve")
                {
                    if (data == true)
                    {
                        return Json("Approved Video KYC has been marked successfully");
                    }
                    else
                    {
                        return Json("Failed to mark whether approved Video KYC.");
                    }
                }
                else
                {
                    if (data == true)
                    {
                        return Json("Re-Record has been marked successfully");
                    }
                    else
                    {
                        return Json("Failed to mark whether Re-Record.");
                    }
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
        public async Task<IActionResult> ProxyReport(int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    ViewBag.ReturnBack = returnBack;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "SeniorInterviewerManagement")
                    {
                        var model = await _dashboardService.GetProxyReport();
                        model = model.Select(x =>
                        {
                            x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription();
                            x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty;
                            x.intervieweStatusStatDesc = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription();
                            return x;
                        }).ToList();
                        foreach (var item in model)
                        {

                            if (item.confirmationStatus.Trim() != ReceiveMeetingStatus.Cancelled.GetDescription())
                            {
                                if (item.interviewDate < DateTime.Now && item.MeetingStatusStatusDesc == Risebird.Entities.Enum.MeetingStatus.Scheduled.GetDescription())
                                {
                                    item.MeetingStatusStatusDesc = "Status Pending";
                                }
                            }
                            else
                            {
                                item.MeetingStatusStatusDesc = string.Empty;

                            }
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
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetProxyReportCountDetails(int id)
        {
            try
            {
                var model = await _dashboardService.GetProxyReportCountDetails(id);

                return PartialView(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        #region Missed Candidate
        [HttpGet]
        public async Task<IActionResult> InterviewManagementDetails(string type, string pagetype, int returnBack)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var Emailid = HttpContext.Session.GetString("email")?.Trim();
                Emailid = Emailid.Replace("\"", "");
                ViewBag.ReturnBack = returnBack;
                DateTime startdate;
                DateTime enddate;
                startdate = DateTime.Now.Date.AddDays(-7);
                enddate = DateTime.Now.Date.AddDays(1);
                if (HttpContext.Session.GetString("InterviewManagementDetailsfilters") != null)
                {
                    var filter = JsonConvert.DeserializeObject<MyInterviewFilter>(HttpContext.Session.GetString("InterviewManagementDetailsfilters"));
                    startdate = Convert.ToDateTime(filter.StartDate);
                    enddate = Convert.ToDateTime(filter.EndDate).AddDays(1);
                    ViewBag.ReturnBack = filter.HdnReturnBack;
                }
                var interviewmanagement = await _dashboardService.GetInterviewManagementDetails(startdate, enddate, Emailid);
                interviewmanagement = interviewmanagement.Where(x => (x.IsRescheduleNotificationSent == 1 && x.MeetingStatus != 3 && x.MeetingStatus != 4 && x.MeetingStatus != 6 && x.MeetingStatus != 7 && x.MeetingStatus != 8) || (x.PerformedOn != null && (x.MeetingStatus == 1 || x.MeetingStatus == 5 || x.MeetingStatus == 9))).ToList();
                interviewmanagement = interviewmanagement.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.MeetingStatusStatusDesc = ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription(); ; x.LastMeetingStatusStatusDesc = ((MeetingStatus)Convert.ToInt32(x.LastMeetingStatus)).GetDescription(); return x; }).ToList();
                return View(interviewmanagement);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }
        [HttpPost]
        public async Task<IActionResult> InterviewManagementDetails(MyInterviewFilter filter)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("InterviewManagementDetailsfilters", JsonConvert.SerializeObject(filter));
                    }
                    return RedirectToAction("InterviewManagementDetails");
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
        [HttpGet]
        public async Task<IActionResult> ResetInterviewManagementDetailsfilters(int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("InterviewManagementDetailsfilters");
                    return RedirectToAction("InterviewManagementDetails", new { returnBack = returnBack });
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("InterviewManagementDetails", new { returnBack = returnBack });
            }
        }
        #endregion
        #region Interview Management
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> EmergencyInterviewDetails(string interviewdate, string type, string pagetype)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                ViewBag.Pagetype = pagetype;
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var Emailid = HttpContext.Session.GetString("email")?.Trim();
                Emailid = Emailid.Replace("\"", "");
                DateTime startdate;
                DateTime enddate;
                if (string.IsNullOrEmpty(interviewdate))
                {
                    startdate = DateTime.Now.Date;
                }
                else
                {
                    startdate = Convert.ToDateTime(interviewdate);
                }
                enddate = startdate.AddDays(1);
                ViewBag.interviewdate = interviewdate;
                if (HttpContext.Session.GetString("EmergencyInterviewDetailsfilters") != null)
                {
                    var filter = JsonConvert.DeserializeObject<MyInterviewFilter>(HttpContext.Session.GetString("EmergencyInterviewDetailsfilters"));
                    startdate = Convert.ToDateTime(filter.StartDate);
                    enddate = Convert.ToDateTime(filter.EndDate).AddDays(1);
                    //filter.ClientName = filter.ClientName == null ? filter.ClientName = modelList.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;
                    //interviewmanagement = interviewmanagement.Where(c => filter.ClientName != null && filter.ClientName.Count() > 0) ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null).ToList();
                }
                var interviewmanagement = await _dashboardService.GetInterviewManagementDetails(startdate, enddate, Emailid);
                if (type == "InterviewM")
                {
                    ViewBag.Type = "InterviewM";
                    interviewmanagement = interviewmanagement.Where(x => x.IsMeetingNotificationSent == 1 && x.PerformedOn != null).ToList();
                }
                else if (type == "OnTimeIM")
                {
                    ViewBag.Type = "OnTimeIM";
                    interviewmanagement = interviewmanagement.Where(x => x.IsMeetingNotificationSent == 0 && x.PerformedOn != null && x.Joinedon != null).ToList();
                }
                else
                {
                    ViewBag.Type = "RescheduleIM";
                    interviewmanagement = interviewmanagement.Where(x => (x.IsRescheduleNotificationSent == 1 && x.MeetingStatus != 3 && x.MeetingStatus != 4 && x.MeetingStatus != 6 && x.MeetingStatus != 7 && x.MeetingStatus != 8) || (x.PerformedOn != null && (x.MeetingStatus == 1 || x.MeetingStatus == 5 || x.MeetingStatus == 9))).ToList();
                    interviewmanagement = interviewmanagement.Select(x => { x.LastMeetingStatusStatusDesc = ((MeetingStatus)Convert.ToInt32(x.LastMeetingStatus)).GetDescription(); return x; }).ToList();

                }
                interviewmanagement = interviewmanagement.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.MeetingStatusStatusDesc = ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription(); return x; })?.OrderByDescending(x => x.interviewDate).ToList();
                return View(interviewmanagement);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> EmergencyInterviewDetails(MyInterviewFilter filter, string type, string pagetype)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("EmergencyInterviewDetailsfilters", JsonConvert.SerializeObject(filter));
                    }
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    return RedirectToAction("EmergencyInterviewDetails", new { interviewdate = DateTime.Now.Date.ToString(), type = type, pagetype = pagetype });
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
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ResetEmergencyInterviewDetailsfilters(string pagetype)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("EmergencyInterviewDetailsfilters");
                    return RedirectToAction("EmergencyInterviewDetails", new { interviewdate = DateTime.Now.Date.ToString(), pagetype = pagetype });
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("EmergencyInterviewDetails", new { interviewdate = DateTime.Now.Date.ToString() });
            }
        }
        #endregion
        [HttpGet]
        public async Task<IActionResult> GetEmailRecipientsBounceAndDropped(string recipientType, int returnBack, string pagetype)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    ViewBag.ReturnBack = returnBack;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Client")
                    {
                        IEnumerable<MailRecipientActivityDetailsVM> recipients = null;

                        if (HttpContext.Session.GetString("MailActivityFilters") != null && string.IsNullOrEmpty(recipientType))
                        {
                            var filter = JsonConvert.DeserializeObject<MailActivityFilter>(HttpContext.Session.GetString("MailActivityFilters"));

                            recipientType = filter.RecipientType;
                            ViewBag.ReturnBack = filter.returnBack;
                            recipients = await _dashboardService.GetEmailRecipientsDetails(recipientType, filter.StartDate, filter.EndDate, emailid);

                            filter.SkillChannel = filter.SkillChannel == null ? filter.SkillChannel = recipients.Select(x => x.SkillChannelName).ToArray() : filter.SkillChannel = filter.SkillChannel;
                            filter.SentBy = filter.SentBy == null ? filter.SentBy = recipients.Select(x => x.SentBy).ToArray() : filter.SentBy = filter.SentBy;
                            filter.Recipient = filter.Recipient == null ? filter.Recipient = recipients.Select(x => x.email).ToArray() : filter.Recipient = filter.Recipient;
                            filter.Activity = filter.Activity == null ? filter.Activity = recipients.Select(x => x.latestEvent).ToArray() : filter.Activity = filter.Activity;

                            recipients = recipients.Where(c => filter.SkillChannel != null && filter.SkillChannel.Contains(c.SkillChannelName)
                                                    && ((filter.Activity != null && filter.Activity.Count() > 0) ? filter.Activity.Contains(c.latestEvent) : c.latestEvent == null)
                                                    && ((filter.SentBy != null && filter.SentBy.Count() > 0) ? filter.SentBy.Contains(c.SentBy) : c.SentBy == null)
                                                    && ((filter.Recipient != null && filter.Recipient.Count() > 0) ? filter.Recipient.Contains(c.email) : c.email == null)).ToList();
                        }
                        else
                        {
                            HttpContext.Session.Remove("MailActivityFilters");
                            if (string.IsNullOrEmpty(recipientType))
                            {
                                recipientType = "Candidate";
                            }

                            recipients = await _dashboardService.GetEmailRecipientsDetails(recipientType, DateTime.Now.AddDays(-2), DateTime.Now, emailid);
                        }
                        if (recipients == null)
                        {
                            recipients = new List<MailRecipientActivityDetailsVM>();
                        }
                        ViewBag.recipientType = recipientType;

                        ViewBag.UserProfile = profileName;
                        ViewBag.pagetype = pagetype;

                        return View(recipients);
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
        public async Task<IActionResult> GetEmailRecipientsBounceAndDropped(MailActivityFilter filter, string recipientType, int returnBack, string pagetype)
        {
            try
            {
                if (filter != null)
                {
                    filter.RecipientType = recipientType;
                    filter.returnBack = returnBack;
                    HttpContext.Session.SetString("MailActivityFilters", JsonConvert.SerializeObject(filter));
                }
                return RedirectToAction("GetEmailRecipientsBounceAndDropped", new { pagetype = pagetype });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }
        [HttpGet]
        public async Task<IActionResult> ResetRecipientActivityDetails(int returnBack, string pagetype)
        {
            try
            {
                HttpContext.Session.Remove("MailActivityFilters");
                return RedirectToAction("GetEmailRecipientsBounceAndDropped", new { returnBack = returnBack, pagetype = pagetype });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetDMApproveDetails(int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    ViewBag.ReturnBack = returnBack;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "SeniorInterviewerManagement")
                    {
                        var model = await _dashboardService.GetDMApproveDetails(emailid);


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
                return RedirectToAction("SignOut", "Session");
            }
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetOpenClickCampaignMailDetails(int campaignId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "SeniorInterviewerManagement" || profileName == "InterviewerManagement")
                    {
                        IEnumerable<MailRecipientActivityDetailsVM> recipients = null;


                        recipients = await _dashboardService.GetOpenClickCampaignMailDetails(campaignId: campaignId);

                        // ViewBag.campaignName = campaignName;

                        ViewBag.UserProfile = profileName;

                        return View(recipients);
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

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetColdCandidatesList(int ColdCandidateValue, int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    ViewBag.ReturnBack = returnBack;
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        var data = await _dashboardService.GetColdCandidatesList(ColdCandidateValue, emailid);
                        //data = data.Select(x =>
                        //{
                        //    x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty;
                        //    return x;
                        //}).ToList();
                        ViewBag.ColdCandidateValue = ColdCandidateValue;
                        return View(data);
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
        public async Task<JsonResult> MarkedColdCandidate(int CandidateId, string IRFId, string CandidateEmail, int InterviewId, int irfIntId)
        {
            try
            {
                var emailId = HttpContext.Session.GetString("email")?.Trim();
                emailId = emailId.Replace("\"", "");
                //  var result = await _dashboardService.GetFutureDateInterviewSchedule(InterviewId);
                var irfOwnersAndRestrictedIrfOwner = await _candidateService.GetCandidateStaffingAgencyAndClientHRByInterviewId(InterviewId);

                var data = await _dashboardService.MarkedColdCandidate(CandidateId, emailId, InterviewId, 1);

                if (data != null)
                {

                    foreach (var item in data)
                    {
                        if (item.interviewDate > DateTime.Now)
                        {
                            List<string> interviewers = new List<string>();
                            if (!string.IsNullOrEmpty(item.interviewer))
                            {
                                interviewers.Add(item.interviewer);
                            }
                            var cancellationsource = string.Empty;
                            cancellationsource = await _meetingService.GetInterviewCancellationSource(InterviewId);

                            var emailresult = await _notificationHelper.SendInterviewCancelledMail(interviewers, item.interviewDate.ToString(), irfOwnersAndRestrictedIrfOwner?.ToList(), irfIntId, InterviewId);
                            var emailresult1 = await _notificationHelper.SendInterviewCancelledMailClient(item.interviewDate.ToString(), item.CandidateName, irfOwnersAndRestrictedIrfOwner?.ToList(), irfIntId, InterviewId, cancellationsource);
                            await _notificationHelper.SendInterviewCancelledMailCandidate(item.candidateEmail, item.interviewDate.ToString(), item.CandidateName, irfIntId, InterviewId);
                        }
                    }
                    return Json("Candidate " + "(" + CandidateEmail + ")" + " for " + IRFId + " has been marked as cold successfully");
                }
                else
                {
                    return Json("Failed to mark candidate as Cold.");
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        [HttpPost]
        public async Task<JsonResult> UndoMarkedColdCandidate(int CandidateId, string IRFId, string CandidateEmail, int irfIntId)
        {
            try
            {
                var emailId = HttpContext.Session.GetString("email")?.Trim();
                emailId = emailId.Replace("\"", "");


                var data = await _dashboardService.MarkedColdCandidate(CandidateId, emailId, 0, 2);

                if (data == null || data.Count == 0)
                {
                    return Json("Candidate " + "(" + CandidateEmail + ")" + " for " + IRFId + " has been undo as cold successfully");
                }
                else
                {
                    return Json("Failed to undo cold candidate .");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        #region PQI
        public async Task<IActionResult> InterviewerIRFDetails(string type, int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    ViewBag.ReturnBack = returnBack;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "SeniorInterviewerManagement")
                    {
                        if (type == "Rejection")
                        {
                            ViewBag.Type = "Rejection";
                        }
                        else if (type == "NegativeCandidate")
                        {
                            type = "NegativeCandidate";
                            ViewBag.Type = "NegativeCandidate";
                        }
                        else
                        {
                            type = "Selection";
                            ViewBag.Type = "Selection";
                        }
                        var model = await _dashboardService.GetInterviewerIRFDetails(type, null, emailid);
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
                return RedirectToAction("SignOut", "Session");
            }
        }
        public async Task<IActionResult> InterviewerIRFLogisticDetails(string type, int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    ViewBag.ReturnBack = returnBack;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "SeniorInterviewerManagement")
                    {
                        if (type == "Shortselectcomment")
                        {
                            type = "Shortselectcomment";
                            ViewBag.Type = "Shortselectcomment";
                        }
                        else if (type == "Shortrejectcomment")
                        {
                            type = "Shortrejectcomment";
                            ViewBag.Type = "Shortrejectcomment";
                        }
                        else if (type == "Lessquestion")
                        {
                            type = "Lessquestion";
                            ViewBag.Type = "Lessquestion";
                        }
                        else if (type == "Lessduration")
                        {
                            type = "Lessduration";
                            ViewBag.Type = "Lessduration";
                        }
                        else
                        {
                            type = "Latejoin";
                            ViewBag.Type = "Latejoin";
                        }
                        var model = await _dashboardService.InterviewerIRFLogisticDetails(type, emailid);
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
                return RedirectToAction("SignOut", "Session");
            }
        }
        public async Task<IActionResult> InterviewerRiskIRFDetails(string type, string uniqueid)
        {
            var emailid = HttpContext.Session.GetString("email")?.Trim();
            emailid = emailid?.Replace("\"", "");
            if (string.IsNullOrEmpty(emailid))
            {
                return RedirectToAction("PublicAllMeetings", "Candidate", new { id = uniqueid });
            }
            ViewBag.Profile = "Client";
            ViewBag.Emailid = emailid;
            ViewBag.PageStatus = "Public";
            ViewBag.uniqueid = uniqueid;
            if (type == "Rejection")
            {
                ViewBag.Type = "Rejection";
            }
            else if (type == "NegativeCandidate")
            {
                type = "NegativeCandidate";
                ViewBag.Type = "NegativeCandidate";
            }
            else
            {
                type = "Selection";
                ViewBag.Type = "Selection";
            }
            var model = await _dashboardService.GetInterviewerIRFDetails(type, uniqueid);
            return View("InterviewerIRFDetails", model);
        }
        public async Task<IActionResult> TotalInterviewerRiskIRFDetails(string uniqueid, string type)
        {
            var emailid = HttpContext.Session.GetString("email")?.Trim();
            emailid = emailid?.Replace("\"", "");
            if (string.IsNullOrEmpty(emailid))
            {
                return RedirectToAction("PublicAllMeetings", "Candidate", new { id = uniqueid });
            }
            ViewBag.Profile = "Client";
            ViewBag.Emailid = emailid;
            ViewBag.PageStatus = "Public";
            ViewBag.uniqueid = uniqueid;
            ViewBag.InterviewerStatus = "TotalInterviewer";
            var model = await _dashboardService.GetTotalInterviewerRiskIRFDetails(uniqueid);
            if (model == null)
            {
                model = new List<PQIDetails>();
            }
            if (type == "ShortList")
            {
                model = model.Where(x => x.IsActive == true).ToList();
                model = model.Where(x => x.ShortListstatus == "Shortlisted").ToList();
            }
            else if (type == "Removed")
            {
                model = model.Where(x => x.ShortListstatus == "Rejected").ToList();
            }
            else
            {
                model = model.Where(x => x.IsActive == true).ToList();
                model = model.Where(x => string.IsNullOrWhiteSpace(x.ShortListstatus)).ToList();
            }
            ViewBag.Type = type;
            return View("InterviewerIRFDetails", model);
        }
        public async Task<IActionResult> TotalRedMarkeredInterviewers()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "TechSupport" || profileName == "Interviewer")
                    {
                        var model = await _dashboardService.GetTotalRedMarkeredInterviewers();
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
                return RedirectToAction("SignOut", "Session");
            }
        }
        #endregion
        public async Task<IActionResult> TechQCDetails(string type, int ReturnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    ViewBag.ReturnBack = ReturnBack;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "SeniorInterviewerManagement")
                    {
                        var modelList = await _dashboardService.FetchTechQCDetails(emailid);
                        if (modelList == null)
                        {
                            modelList = new List<MeetingVM>();
                        }
                        modelList = modelList.Select(x => { x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; return x; }).ToList();
                        if (type == "Completed")
                        {
                            ViewBag.Type = "Completed";
                            modelList = modelList.Where(x => x.IsQC == true).ToList();
                        }
                        else
                        {
                            ViewBag.Type = "Pending";
                            modelList = modelList.Where(x => x.IsQC == false).ToList();
                        }
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
                return RedirectToAction("SignOut", "Session");
            }
        }

        public async Task<IActionResult> PotentialPausedDetails(int type, int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    ViewBag.ReturnBack = returnBack;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "SeniorInterviewerManagement")
                    {
                        var modelList = await _dashboardService.PotentialPausedDetails(emailid);
                        if (modelList == null)
                        {
                            modelList = new List<MeetingVM>();
                        }
                        if (type == 0)
                        {
                            modelList = modelList.Where(x => !x.IsPaused).ToList();
                            ViewBag.ColdCandidateValue = 0;
                        }
                        else
                        {
                            modelList = modelList.Where(x => x.IsPaused).ToList();
                            ViewBag.ColdCandidateValue = 1;
                        }
                        // modelList = modelList.Select(x => { x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; return x; }).ToList();
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
                return RedirectToAction("SignOut", "Session");
            }
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetPotentialColdCandidatesByClient(string Email)
        {
            try
            {
                var data = await _dashboardService.GetColdCandidatesByClient(Email);
                ViewBag.ClientEmail = Email;
                return View("_GetPotentialColdCandidatesByClient", data.ToList());
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EmergencyDashboard()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var Emailid = HttpContext.Session.GetString("email")?.Trim();
                    Emailid = Emailid.Replace("\"", "");
                    ViewBag.ProfileName = profileName;
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "Quality Controller" || profileName == "Finance" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SupportAdmin" || profileName == "HR" || profileName == "Interviewer")
                    {
                        var model = new DashboardVM();
                        model.NotUpdatedMeetingsObj = new NotUpdatedMeetings();
                        model.NotUpdatedMeetingsObj = await _dashboardService.GetNotUpdatedMeetingsCount(Emailid);
                        var modelList = await _dashboardService.FetchParticipantwaitingDetails(Emailid);
                        model.NotUpdatedMeetingsObj.CandidatewaitingCount = modelList.Where(x => x.PerformedOn != null).Count();
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
        public async Task<IActionResult> NotScheduledMeetingsCO(int? Page, string searchValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    ViewBag.profile = profileName;
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    int ClientId = 0;
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "Quality Controller" || profileName == "Finance" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SupportAdmin" || profileName == "HR" || profileName == "Interviewer")
                    {
                        if (TempData["SuccessErrorMessageCandidate"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageCandidate"].ToString();
                            TempData.Remove("SuccessErrorMessageCandidate");
                        }
                        if (profileName == "Client")
                        {
                            var client = await _userRepository.GetClientUserDetails(emailid);
                            ClientId = client.ClientId;
                            ViewBag.ClientRole = client?.RoleId != 0 ? ((ClientRoleEnum)Convert.ToInt32(client.RoleId)).GetDescription() : string.Empty;
                        }
                        List<MeetingVM> meetings = new List<MeetingVM>();
                        if (profileName == "SeniorHR")
                        {
                            meetings = await _dashboardService.GetNotSecheduleSeniorHRList(emailid);
                        }
                        else
                        {
                            meetings = await _meetingService.GetClientNotScheduledMeetingList(ClientId, emailid);
                        }
                        if (HttpContext.Session.GetString("NotScheduledMeetingsfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("NotScheduledMeetingsfilters"));

                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                filter.EndDate = Convert.ToDateTime(filter.EndDate).AddDays(1);
                                meetings = meetings.Where(t => t.dtCreatedOn >= filter.StartDate && t.dtCreatedOn <= filter.EndDate).ToList();
                            }
                            else if (filter.StartDate != null && filter.EndDate == null)
                            {
                                meetings = meetings.Where(t => t.dtCreatedOn >= filter.StartDate).ToList();
                            }
                            else if (filter.StartDate == null && filter.EndDate != null)
                            {
                                filter.EndDate = Convert.ToDateTime(filter.EndDate).AddDays(1);
                                meetings = meetings.Where(t => t.dtCreatedOn <= filter.EndDate).ToList();
                            }

                            filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = meetings.Select(x => x.JDTiltle).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                            filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = meetings.Select(x => x.ClientHrContact).ToArray() : filter.ClientHR = filter.ClientHR;
                            filter.RFID = filter.RFID == null ? filter.RFID = meetings.Select(x => x.IRFId).ToArray() : filter.RFID = filter.RFID;
                            filter.ClientName = filter.ClientName == null ? filter.ClientName = meetings.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;

                            meetings = meetings.Where(c => filter.PrimarySkill != null && filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                                                    && ((filter.ClientHR != null && filter.ClientHR.Count() > 0) ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                                                    && ((filter.RFID != null && filter.RFID.Count() > 0) ? filter.RFID.Contains(c.IRFId) : c.IRFId == null)
                                                    && ((filter.ClientName != null && filter.ClientName.Count() > 0) ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)).ToList();
                        }
                        else
                        {
                            DateTime StartDate = DateTime.Now.AddMonths(-1);
                            DateTime EndDate = DateTime.Now.AddDays(1);
                            meetings = meetings.Where(t => t.dtCreatedOn >= StartDate && t.dtCreatedOn <= EndDate).ToList();
                        }
                        return View(meetings);
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

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> NotScheduledMeetingsCO(InterviewFilter filter, int? Page, string searchValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("NotScheduledMeetingsfilters", JsonConvert.SerializeObject(filter));
                    }
                    return RedirectToAction("NotScheduledMeetingsCO", new { Page = Page, searchValue = searchValue });
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

        [HttpGet]
        public async Task<IActionResult> ResetNotScheduleMeetingsFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("NotScheduledMeetingsfilters");
                    return RedirectToAction("NotScheduledMeetingsCO");
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
        public async Task<FileContentResult> NotScheduledMeetingstoExcel()
        {
            string userEmailId = string.Empty;
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Interviews");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_Interviews" + ".xlsx";
            try
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                int ClientId = 0;
                if (profileName == "Client")
                {
                    var client = await _userRepository.GetClientUserDetails(emailid);
                    ClientId = client.ClientId;
                    ViewBag.ClientRole = client?.RoleId != 0 ? ((ClientRoleEnum)Convert.ToInt32(client?.RoleId)).GetDescription() : string.Empty;
                }
                List<MeetingVM> meetings = new List<MeetingVM>();
                if (profileName == "SeniorHR")
                {
                    meetings = await _dashboardService.GetNotSecheduleSeniorHRList(emailid);
                }
                else
                {
                    meetings = await _meetingService.GetClientNotScheduledMeetingList(ClientId, emailid);
                }
                if (HttpContext.Session.GetString("NotScheduledMeetingsfilters") != null)
                {
                    var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("NotScheduledMeetingsfilters"));

                    if (filter.StartDate != null && filter.EndDate != null)
                    {
                        filter.EndDate = Convert.ToDateTime(filter.EndDate).AddDays(1);
                        meetings = meetings.Where(t => t.dtCreatedOn >= filter.StartDate && t.dtCreatedOn <= filter.EndDate).ToList();
                    }
                    else if (filter.StartDate != null && filter.EndDate == null)
                    {
                        meetings = meetings.Where(t => t.dtCreatedOn >= filter.StartDate).ToList();
                    }
                    else if (filter.StartDate == null && filter.EndDate != null)
                    {
                        filter.EndDate = Convert.ToDateTime(filter.EndDate).AddDays(1);
                        meetings = meetings.Where(t => t.dtCreatedOn <= filter.EndDate).ToList();
                    }

                    filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = meetings.Select(x => x.JDTiltle).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                    filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = meetings.Select(x => x.ClientHrContact).ToArray() : filter.ClientHR = filter.ClientHR;
                    filter.RFID = filter.RFID == null ? filter.RFID = meetings.Select(x => x.IRFId).ToArray() : filter.RFID = filter.RFID;
                    filter.ClientName = filter.ClientName == null ? filter.ClientName = meetings.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;

                    meetings = meetings.Where(c => filter.PrimarySkill != null && filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                                            && ((filter.ClientHR != null && filter.ClientHR.Count() > 0) ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                                            && ((filter.RFID != null && filter.RFID.Count() > 0) ? filter.RFID.Contains(c.IRFId) : c.IRFId == null)
                                            && ((filter.ClientName != null && filter.ClientName.Count() > 0) ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)).ToList();
                }
                else
                {
                    DateTime StartDate = DateTime.Now.AddMonths(-1);
                    DateTime EndDate = DateTime.Now.AddDays(1);
                    meetings = meetings.Where(t => t.dtCreatedOn >= StartDate && t.dtCreatedOn <= EndDate).ToList();
                }

                Sheet.Cells["A1:AI1"].Style.Font.Bold = true;
                Sheet.Cells["A1:AI1"].Style.Font.Size = 12;
                Sheet.Cells["A1:AI1"].Style.WrapText = true;
                int startColumn = 1; int endColumn = 10;
                for (int a = startColumn; a <= endColumn; a++)
                {
                    Sheet.Column(a).Width = 40;
                }
                Sheet.Cells[1, 1].Value = "Added Date";
                Sheet.Cells[1, 2].Value = "Client Name";
                Sheet.Cells[1, 3].Value = "JD Title";
                Sheet.Cells[1, 4].Value = "Candidate Name";
                Sheet.Cells[1, 5].Value = "Candidate Email";
                Sheet.Cells[1, 6].Value = "Candidate Mobile Number";
                Sheet.Cells[1, 7].Value = "IRF ID";
                Sheet.Cells[1, 8].Value = "Resume";
                Sheet.Cells[1, 9].Value = "Client HR";
                Sheet.Cells[1, 10].Value = "Channel Owner";
                Sheet.Cells[1, 11].Value = "Latest Status";
                int i = 2;
                if (meetings.Count() > 0)
                {
                    foreach (var item in meetings)
                    {
                        Sheet.Cells[i, 1].Value = item.dtCreatedOn.ToString("MM/dd/yyyy HH:mm");
                        Sheet.Cells[i, 2].Value = item.ClientName;
                        Sheet.Cells[i, 3].Value = item.JDTiltle;
                        Sheet.Cells[i, 4].Value = item.candidateName;
                        Sheet.Cells[i, 5].Value = item.candidateEmail;
                        Sheet.Cells[i, 6].Value = item.MobileNumber;
                        Sheet.Cells[i, 7].Value = item.IRFId;
                        if (!string.IsNullOrEmpty(item.resumeLink))
                        {
                            try
                            {
                                Sheet.Cells[i, 8].Hyperlink = new Uri(item.resumeLink, UriKind.Absolute);
                                Sheet.Cells[i, 8].Value = "Resume";
                                Sheet.Cells[i, 8].Style.Font.UnderLine = true;
                                Sheet.Cells[i, 8].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                            }
                            catch (Exception)
                            {
                                Sheet.Cells[i, 8].Value = "No Resume";
                            }
                        }
                        else
                        {
                            Sheet.Cells[i, 8].Value = "No Resume";
                        }
                        Sheet.Cells[i, 9].Value = item.ClientHrContact;
                        Sheet.Cells[i, 10].Value = item.ChannelOwner;
                        Sheet.Cells[i, 11].Value = item.StatusValue;
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

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> AllEmergencyInterviewDetails()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var Emailid = HttpContext.Session.GetString("email")?.Trim();
                    Emailid = Emailid.Replace("\"", "");
                    DateTime startdate = DateTime.Now.AddMonths(-1);
                    DateTime enddate = DateTime.Now;

                    if (HttpContext.Session.GetString("EmergencyInterviewDetailsfilters") != null)
                    {
                        var filter = JsonConvert.DeserializeObject<MyInterviewFilter>(HttpContext.Session.GetString("EmergencyInterviewDetailsfilters"));
                        startdate = Convert.ToDateTime(filter.StartDate);
                        enddate = Convert.ToDateTime(filter.EndDate).AddDays(1);
                    }
                    var interviewmanagement = await _dashboardService.GetAllInterviewManagementDetails(startdate, enddate);

                    interviewmanagement = interviewmanagement.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.MeetingStatusStatusDesc = ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription(); return x; }).ToList();
                    return View(interviewmanagement);
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
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> AllEmergencyInterviewDetails(MyInterviewFilter filter)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("EmergencyInterviewDetailsfilters", JsonConvert.SerializeObject(filter));
                    }
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    return RedirectToAction("AllEmergencyInterviewDetails");
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
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> AllResetEmergencyInterviewDetailsfilters(string pagetype)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("EmergencyInterviewDetailsfilters");
                    return RedirectToAction("AllEmergencyInterviewDetails");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("AllEmergencyInterviewDetails");
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetSurchargesDetails(int actionType)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    ViewBag.Type = actionType;
                    DateTime? StartDate;
                    DateTime? EndDate;
                    if (HttpContext.Session.GetString("SurchargeFilters") != null)
                    {
                        var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("SurchargeFilters"));
                        StartDate = filter.StartDate;
                        EndDate = filter.EndDate;
                    }
                    else
                    {
                        StartDate = DateTime.Now.AddMonths(-1);
                        EndDate = DateTime.Now;
                    }
                    var surchaes = await _dashboardService.GetSurcharesDetails(actionType, StartDate, EndDate);
                    surchaes = surchaes.Select(x =>
                    {
                        x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty;
                        return x;
                    }).ToList();
                    return View(surchaes);
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

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> GetSurchargesDetails(SurchargeFilter filter, int actionType)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("SurchargeFilters", JsonConvert.SerializeObject(filter));
                    }
                    return RedirectToAction("GetSurchargesDetails", "Dashboard", new { actionType });
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

        [HttpGet]
        public async Task<IActionResult> ResetGetSurchargesDetailsFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("SurchargeFilters");
                    return RedirectToAction("GetSurchargesDetails");
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

        [HttpPost]
        public async Task<JsonResult> ApproveSurcharge(int InterviewId, int Surcharge, string SurchargeComment, string Type, int IRFId, int actionType, int UserId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {

                    var Emailid = HttpContext.Session.GetString("email")?.Trim();
                    Emailid = Emailid.Replace("\"", "");
                    var data = await _dashboardService.ApproveSurcharge(InterviewId, Surcharge, SurchargeComment, Type, Emailid, IRFId, actionType, UserId);

                    if (data)
                    {
                        return Json("Successfully");

                    }
                    else
                    {
                        return Json("UnSuccessfully");
                    }

                }
                else
                {
                    return Json(new { success = false, redirectUrl = Url.Action("SignOut", "Session") });
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [HttpPost]
        public async Task<JsonResult> UpdateSurchargeDetails(int InterviewId, int IRFId, string Type, int UserId, string SurchargeComment, int Surchargeamt)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    var model = await _dashboardService.UpdateSurchargeDetails(InterviewId, IRFId, Type, UserId, SurchargeComment, Surchargeamt, emailId);
                    if (model)
                    {
                        return Json("Surcharge details are updated successfully.");
                    }
                    else
                    {
                        return Json("Failed to update Surcharge details, please try again.");
                    }
                }
                else
                {
                    return Json("Failed to update Surcharge details, please try again.");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetSurchargesActivityTracker(int InterviewId, int IRFId, string Type, int UserId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var surchaes = await _dashboardService.GetSurcharesActivityTraker(InterviewId, IRFId, Type, UserId);
                    return PartialView("_GetSurchargesActivityTracker", surchaes);
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
        //Interviewer rate and Plan type
        [HttpPost]
        public async Task<IActionResult> GetPerSuccessRateDetails(int InterviewId, int IRFId, string Type, int UserId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var surchaes = await _dashboardService.GetPerSuccessRateDetails(InterviewId, IRFId, Type, UserId);
                    foreach (var item in surchaes)
                    {
                        return Json(new { Heuristicsscore = item.Heuristicsscore, PerSuccessRate = item.PerSuccessRate });
                    }
                    return View();
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


        /// <summary>
        /// Gets active skills channel Interviewers whose AI verdict is negative i.e, not suitable.
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> InterviewersWithNegativeAIVerdict()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");

                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "Quality Controller" || profileName == "Finance" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SupportAdmin" || profileName == "HR" || profileName == "Interviewer")
                    {
                        ViewBag.Profile = profileName;
                        var model = await _dashboardService.GetTransInterviewersWithNegativeAIVerdict();
                        return View("InterviewersWithNegativeAIVerdict", model);
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

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> AIQCNegative()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "Quality Controller" || profileName == "Finance" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SupportAdmin" || profileName == "HR" || profileName == "Interviewer")
                    {
                        var model = await _dashboardService.GetAIQCNegativeVerdictReports();
                        model = model.Select(x =>
                        {
                            x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription();
                            x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty;
                            x.intervieweStatusStatDesc = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription();
                            return x;
                        }).ToList();
                        foreach (var item in model)
                        {

                            if (item.confirmationStatus.Trim() != ReceiveMeetingStatus.Cancelled.GetDescription())
                            {
                                if (item.interviewDate < DateTime.Now && item.MeetingStatusStatusDesc == Risebird.Entities.Enum.MeetingStatus.Scheduled.GetDescription())
                                {
                                    item.MeetingStatusStatusDesc = "Status Pending";
                                }
                            }
                            else
                            {
                                item.MeetingStatusStatusDesc = string.Empty;

                            }
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
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> UnconfirmedCandidates()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "Quality Controller" || profileName == "Finance" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SupportAdmin" || profileName == "HR" || profileName == "Interviewer")
                    {
                        var model = await _dashboardService.GetCandidatesWhoHaventConfirmed();
                        model = model.Select(x =>
                        {
                            x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription();
                            x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty;
                            //x.intervieweStatusStatDesc = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription();
                            return x;
                        }).ToList();

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
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SkillChannelInterviewerBilling()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    if (profileName == "Admin" || profileName == "SupportAdmin" || profileName == "Finance")
                    {
                        var model = await _dashboardService.GetAllSkillChannelsList();
                        if (model == null)
                        {
                            model = new List<IRFDetailsList>();
                        }
                        model = model.Select(x =>
                        {
                            x.Heuristicsscore = !string.IsNullOrWhiteSpace(x.Heuristicsscore) ? ((Heuristicsscorelevel)Convert.ToInt32(x.Heuristicsscore)).GetDescription() : string.Empty;
                            x.CompanyType = !string.IsNullOrWhiteSpace(x.CompanyType) ? ((CompanyType)Convert.ToInt32(x.CompanyType)).GetDescription() : string.Empty;
                            x.JobCountry = x.JobCountry == "2" ? "Onsite" : "Offshore";
                            return x;
                        }).ToList();
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
                return RedirectToAction("SignOut", "Session");
            }
        }
        #region Client Action Data
        public async Task<IActionResult> ClientRemovedInterviewers()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    if (profileName == "Admin" || profileName == "SupportAdmin" || profileName == "SeniorHr")
                    {
                        var model = await _dashboardService.GetClientRemovedInterviewers();
                        if (model == null)
                        {
                            model = new List<PQIDetails>();
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
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }
        #endregion

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> AITranscriptVerdicts()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "SeniorInterviewerManagement" || profileName == "Interviewer")
                    {
                        var model = await _dashboardService.GetAITranscriptReportsWithVerdict(DateTime.Now.AddDays(-30), DateTime.Now);
                        model = model.Select(x =>
                        {
                            x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription();
                            x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty;
                            x.intervieweStatusStatDesc = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription();
                            return x;
                        }).ToList();
                        foreach (var item in model)
                        {

                            if (item.confirmationStatus.Trim() != ReceiveMeetingStatus.Cancelled.GetDescription())
                            {
                                if (item.interviewDate < DateTime.Now && item.MeetingStatusStatusDesc == Risebird.Entities.Enum.MeetingStatus.Scheduled.GetDescription())
                                {
                                    item.MeetingStatusStatusDesc = "Status Pending";
                                }
                            }
                            else
                            {
                                item.MeetingStatusStatusDesc = string.Empty;

                            }
                        }
                        ViewBag.Pagetype = "";
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
                return RedirectToAction("SignOut", "Session");
            }
        }
        [HttpGet]
        public async Task<IActionResult> AITranscriptVerdictsInterviewer(string interviewer, string Irfid)
        {
            try
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName?.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid?.Replace("\"", "");
                ViewBag.Profile = profileName;
                var model = await _dashboardService.GetAITranscriptReportsWithVerdictInterviewer(interviewer, Irfid);
                model = model.Select(x =>
                {
                    x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription();
                    x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty;
                    x.intervieweStatusStatDesc = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription();
                    return x;
                }).ToList();
                foreach (var item in model)
                {
                    if (item.confirmationStatus.Trim() != ReceiveMeetingStatus.Cancelled.GetDescription())
                    {
                        if (item.interviewDate < DateTime.Now && item.MeetingStatusStatusDesc == Risebird.Entities.Enum.MeetingStatus.Scheduled.GetDescription())
                        {
                            item.MeetingStatusStatusDesc = "Status Pending";
                        }
                    }
                    else
                    {
                        item.MeetingStatusStatusDesc = string.Empty;
                    }
                }
                ViewBag.Pagetype = "Public";
                return View("AITranscriptVerdicts", model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> TotalInterviews()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");

                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "Quality Controller" || profileName == "Finance" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SupportAdmin" || profileName == "HR" || profileName == "Interviewer")
                    {
                        ViewBag.Profile = profileName;
                        DateTime startdate =DateTime.Now.AddMonths(-1);
                        DateTime enddate = DateTime.Now.AddDays(1);
                        var model = await _dashboardService.GetTotalInterviews(startdate, enddate);
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
                return RedirectToAction("SignOut", "Session");
            }
        }

    }
}
