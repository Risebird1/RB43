using Google.Apis.Drive.v3.Data;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OfficeOpenXml;
using RestSharp;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using Risebird.Web.Controllers.AzureMediaservice;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VirusTotalNet;
using VirusTotalNet.ResponseCodes;
using X.PagedList;

namespace Risebird.Web.Controllers
{

    public class IRFController : Controller
    {
        private readonly IIRFRepository _irfRepository;
        private readonly IUserService _userService;
        private readonly IIRFService _irfService;
        private readonly IEvaluatorService _evaluatorService;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IMeetingService _meetingService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ISkillService _skillService;
        private static List<Candidate> _modelDetails;
        private static List<MeetingVM> _modelDetail;
        private static List<MeetingVM> _modelDetle;
        private IConfiguration _configuration;
        private IInterviewerSkillsService _iInterviewerSkillsService;
        private readonly ICandidateService _candidateService;
        private IInterviewerService _interviewerService;
        private readonly INotificationHelper _notificationHelper;
        private readonly ITemplateService _templateService;
        private readonly IClientService _clientService;
        private readonly IFileMimeTypeHelper _fileMimeTypeHelper;
        private readonly IInterviewSchedulerService _interviewSchedulerservice;
        private readonly IClonableSkillChannelTemplateService _clonableSkillChannelTemplateService;
        private readonly ICalendlyHelper _calendlyHelper;
        private readonly IMCQService _mCQService;
        private readonly string codeapiendpoint;
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly IWhatsAppHelper _whatsAppHelper;
        private readonly HttpClient _httpClient;
        private readonly CancellationToken _cancellationToken;

        public IRFController(IIRFRepository iRFRepository, IApplicationErrorLogger applicationErrorLogger, IIRFService irfService, IMeetingService meetingService, IAzureStorageHelper azureStorageHelper, IEvaluatorService evaluatorService, IUserService userService, ISkillService skillService, IConfiguration configuration, ICandidateService candidateService, IInterviewerSkillsService iInterviewerSkillsService, IInterviewerService interviewerService, INotificationHelper notificationHelper, ITemplateService templateService, IClientService clientService, IFileMimeTypeHelper fileMimeTypeHelper, IInterviewSchedulerService interviewSchedulerservice,
            IClonableSkillChannelTemplateService clonableSkillChannelTemplateService, ICalendlyHelper calendlyHelper,
            IMCQService mCQService, IEncryptionHelper encryptionHelper, IWhatsAppHelper whatsAppHelper,
            HttpClientHelper factory)
        {
            _userService = userService;
            _irfRepository = iRFRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _irfService = irfService;
            _meetingService = meetingService;
            _azureStorageHelper = azureStorageHelper;
            _evaluatorService = evaluatorService;
            _skillService = skillService;
            _configuration = configuration;
            _iInterviewerSkillsService = iInterviewerSkillsService;
            _candidateService = candidateService;
            _interviewerService = interviewerService;
            _notificationHelper = notificationHelper;
            _templateService = templateService;
            _clientService = clientService;
            _fileMimeTypeHelper = fileMimeTypeHelper;
            _interviewSchedulerservice = interviewSchedulerservice;
            _clonableSkillChannelTemplateService = clonableSkillChannelTemplateService;
            _calendlyHelper = calendlyHelper;
            _mCQService = mCQService;
            _encryptionHelper = encryptionHelper;
            _whatsAppHelper = whatsAppHelper;
            codeapiendpoint = _configuration["CodeApiEndPoint"];
            _httpClient = factory.GetHttpClient();
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index(string searchValue = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter" || profileName == "CuratedUser" || profileName == "Finance" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        var model = new IRFViewModel();
                        IRFFilter filter = null;
                        if (HttpContext.Session.GetString("IRFfilters") != null)
                        {
                            filter = JsonConvert.DeserializeObject<IRFFilter>(HttpContext.Session.GetString("IRFfilters"));

                            //IRFilterdata  new call according to the filter applied
                            model.IRFFilterData = await _irfService.GetIRFFilterData(emailid, profileName, filter);

                        }
                        else
                        {
                            model.IRFFilterData = await _irfService.GetIRFFilterData(emailid, profileName);
                        }
                        var irfList = await _irfService.GetAllIRFs(emailid, profileName, null, searchValue, filter);
                        irfList?.ToList().ForEach(x => { x.ArchiveStatus = ((ArchiveStatus)Convert.ToInt32(x.IsDeleted)).GetDescription(); x.SubscriptionStatus = ((SubscriptionStatus)Convert.ToInt32(x.IsSubscribed)).GetDescription(); });
                        model.IRFList = irfList;

                        if (model.IRFList != null)
                        {
                            foreach (var item in model.IRFList)
                            {
                                if (item.IsPaused)
                                {
                                    item.ImageUrl = "/img/Cards_Png/01_Grey-White.png";
                                    item.IRFStatus = "Paused";
                                }

                                else if (item.IsDeleted)
                                {
                                    item.ImageUrl = "/img/Cards_Png/01_Black-White.png";
                                    item.IRFStatus = "Archived";
                                }
                                else if (item.EndDate >= DateTime.Now)
                                {
                                    item.ImageUrl = "/img/Cards_Png/01_Green-White.png";
                                    item.IRFStatus = "Active";
                                }
                                else
                                {
                                    item.ImageUrl = "/img/Cards_Png/01_Black-White.png";
                                    item.IRFStatus = "Archived";
                                }
                            }
                        }

                        if (profileName != null && profileName.Contains("Client"))
                        {
                            model.IsClientUser = true;
                        }
                        if (TempData["SuccessErrorMessage"] != null)
                        {
                            model.Message = TempData["SuccessErrorMessage"].ToString();
                            if (TempData["SuccessErrorMessage2"] != null)
                            {
                                model.Message2 = TempData["SuccessErrorMessage2"].ToString();
                                TempData.Remove("SuccessErrorMessage2");
                            }
                            TempData.Remove("SuccessErrorMessage");
                        }
                        HttpContext.Session.SetString("IRFSearchValue", searchValue ?? string.Empty);
                        model.Profile = profileName;
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
        public async Task<IActionResult> Index(IRFFilter filter)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("IRFfilters", JsonConvert.SerializeObject(filter));

                    }
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter" || profileName == "CuratedUser" || profileName == "Finance" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {

                        var model = new IRFViewModel();

                        var irfList = await _irfService.GetAllIRFs(emailid, profileName, null, null, filter);

                        irfList?.ToList().ForEach(x => { x.ArchiveStatus = ((ArchiveStatus)Convert.ToInt32(x.IsDeleted)).GetDescription(); x.SubscriptionStatus = ((SubscriptionStatus)Convert.ToInt32(x.IsSubscribed)).GetDescription(); });
                        model.IRFFilterData = await _irfService.GetIRFFilterData(emailid, profileName, filter);

                        if (profileName != null && profileName.Contains("Client"))
                        {
                            model.IsClientUser = true;
                        }

                        foreach (var item in irfList)
                        {
                            if (item.IsPaused)
                            {
                                item.ImageUrl = "/img/Cards_Png/01_Grey-White.png";
                                item.IRFStatus = "Paused";
                            }

                            else if (item.IsDeleted)
                            {
                                item.ImageUrl = "/img/Cards_Png/01_Black-White.png";
                                item.IRFStatus = "Archived";
                            }
                            else if (item.EndDate >= DateTime.Now)
                            {
                                item.ImageUrl = "/img/Cards_Png/01_Green-White.png";
                                item.IRFStatus = "Active";
                            }
                            else
                            {
                                item.ImageUrl = "/img/Cards_Png/01_Black-White.png";
                                item.IRFStatus = "Archived";
                            }
                        }


                        model.IRFList = irfList;

                        model.Profile = profileName;

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
        public async Task<IActionResult> IndexPartial(int? pageNumber, string searchValue = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter" || profileName == "CuratedUser" || profileName == "Finance" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        var model = new IRFViewModel();
                        IRFFilter filter = null;
                        if (HttpContext.Session.GetString("IRFfilters") != null)
                        {
                            filter = JsonConvert.DeserializeObject<IRFFilter>(HttpContext.Session.GetString("IRFfilters"));
                        }

                        var irfList = await _irfService.GetAllIRFs(emailid, profileName, pageNumber, searchValue, filter);
                        irfList.ToList().ForEach(x => { x.ArchiveStatus = ((ArchiveStatus)Convert.ToInt32(x.IsDeleted)).GetDescription(); x.SubscriptionStatus = ((SubscriptionStatus)Convert.ToInt32(x.IsSubscribed)).GetDescription(); });
                        model.IRFList = irfList;

                        if (model.IRFList != null)
                        {
                            foreach (var item in model.IRFList)
                            {
                                if (item.IsPaused)
                                {
                                    item.ImageUrl = "/img/Cards_Png/01_Grey-White.png";
                                    item.IRFStatus = "Paused";
                                }

                                else if (item.IsDeleted)
                                {
                                    item.ImageUrl = "/img/Cards_Png/01_Black-White.png";
                                    item.IRFStatus = "Archived";
                                }
                                else if (item.EndDate >= DateTime.Now)
                                {
                                    item.ImageUrl = "/img/Cards_Png/01_Green-White.png";
                                    item.IRFStatus = "Active";
                                }
                                else
                                {
                                    item.ImageUrl = "/img/Cards_Png/01_Black-White.png";
                                    item.IRFStatus = "Archived";
                                }
                            }
                        }


                        if (profileName != null && profileName.Contains("Client"))
                        {
                            model.IsClientUser = true;
                        }

                        if (TempData["SuccessErrorMessage"] != null)
                        {
                            model.Message = TempData["SuccessErrorMessage"].ToString();
                            TempData.Remove("SuccessErrorMessage");
                        }

                        model.Profile = profileName;

                        if (model.IRFList == null || model.IRFList.Count() == 0)
                        {
                            return Json(null);
                        }
                        else
                        {
                            return PartialView("IRFIndexPartial", model);
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
                return Json(false);
            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult InterviewDashboard()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        var iRFVms = _irfService.GetUpComingIRFList();
                        var interviewVMs = _irfService.GetUpComingInterviewList();
                        var datamodel = new List<IRFInterviewVM>();
                        DateTime startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 00, 00, 00);
                        DateTime endDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59);
                        foreach (var item in iRFVms)
                        {
                            // var lastDay = DateTime.Now.AddDays(-1);

                            var interviews = interviewVMs.Where(x => x.IRFID == item.IRFId && (x.interviewDate > startDate && x.interviewDate < endDate));
                            if (interviews != null && interviews.Count() > 0)
                            {

                                var data = new IRFInterviewVM
                                {
                                    IRFId = item.IRFId,
                                    PrimarySkill = item.JDTiltle,
                                    ClientName = item.ClientName,
                                    EndClientName = item.ClientCompName
                                };
                                var mrngInterview = interviews.Where(x => Convert.ToInt32(x.interviewSlot) == 1).Count();
                                var eveningInterview = interviews.Where(x => Convert.ToInt32(x.interviewSlot) == 2).Count();
                                data.InterviewCount = "Morning: " + mrngInterview + " " + "Evening: " + eveningInterview;
                                var mrngunInterview = interviews.Where(x => Convert.ToInt32(x.interviewSlot) == 1 && x.interviewer == null).Count();
                                var eveningunInterview = interviews.Where(x => Convert.ToInt32(x.interviewSlot) == 2 && x.interviewer == null).Count();
                                data.UnassignedInterviewCount = "Morning: " + mrngunInterview + " " + "Evening: " + eveningunInterview;
                                var selectedInterview = interviews.Where(x => x.interviewStatus == 2).Count();
                                var rejectedInterview = interviews.Where(x => x.interviewStatus == 3).Count();
                                var borderLineInterview = interviews.Where(x => x.interviewStatus == 4).Count();
                                var proxyInterview = interviews.Where(x => x.interviewStatus == 1).Count();
                                data.SelectionCount = "Selected: " + selectedInterview + " " + "Borderline: " + borderLineInterview + " " + "Rejected: " + rejectedInterview + " " + "Proxy: " + proxyInterview;
                                data.InterviewsReportCount = interviews.Where(x => x.InterviewerComment == null).Count().ToString();
                                data.QualityCheck = interviews.Where(x => x.QCInterview == 0).Count().ToString();
                                var concurrentCollection = interviews.GroupBy(x => x.interviewDate)
                                       .Select(g => new { Value = g.Key, Count = g.Count() })
                                      .OrderByDescending(x => x.Count);
                                var concurrentInterview = concurrentCollection.Where(x => x.Count > 2).ToList();
                                if (concurrentInterview.Count() > 0)
                                {
                                    foreach (var conInterview in concurrentInterview)
                                    {
                                        data.ConcurrentInterview += conInterview.Value + " : " + conInterview.Count + " ";
                                    }
                                }
                                datamodel.Add(data);
                            }
                        }
                        var dayDate = DateTime.Now;
                        var list = new SelectList(new[]
                        {
                            new { ID = "Yesterday", Name = dayDate.AddDays(-1).DayOfWeek.ToString()+"( "+dayDate.AddDays(-1).ToString("dd-MM-yyyy") +" )" },
                            new { ID = "Today", Name = dayDate.DayOfWeek.ToString()+"( "+dayDate.ToString("dd-MM-yyyy") +" )" },
                            new { ID = "Tomorrow", Name = dayDate.AddDays(1).DayOfWeek.ToString() +"( "+dayDate.AddDays(1).ToString("dd-MM-yyyy") +" )" },
                            new { ID = "1DayTomorrow", Name = dayDate.AddDays(2).DayOfWeek.ToString()+"( "+dayDate.AddDays(2).ToString("dd-MM-yyyy") +" )" },
                            new { ID = "2DayTomorrow", Name = dayDate.AddDays(3).DayOfWeek.ToString()+"( "+dayDate.AddDays(3).ToString("dd-MM-yyyy") +" )" },
                            new { ID = "3DayTomorrow", Name = dayDate.AddDays(4).DayOfWeek.ToString()+"( "+dayDate.AddDays(4).ToString("dd-MM-yyyy") +" )" },
                             new { ID = "3DayTomorrow", Name = dayDate.AddDays(5).DayOfWeek.ToString() +"( "+dayDate.AddDays(5).ToString("dd-MM-yyyy") +" )" },
                        },
                        "ID", "Name", "Today");

                        ViewData["list"] = list;

                        return View(datamodel);
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

        [Authorize]
        [HttpGet]
        public IActionResult FilteredInterviewDashboard(string filterValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        var iRFVms = _irfService.GetUpComingIRFList();
                        var interviewVMs = _irfService.GetUpComingInterviewList();
                        var datamodel = new List<IRFInterviewVM>();



                        if (filterValue == "Today")
                        {
                            DateTime startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 00, 00, 00);
                            DateTime endDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59);
                            foreach (var item in iRFVms)
                            {
                                var interviews = interviewVMs.Where(x => x.IRFID == item.IRFId && (x.interviewDate > startDate && x.interviewDate < endDate));
                                if (interviews != null && interviews.Count() > 0)
                                {
                                    var data = new IRFInterviewVM
                                    {
                                        IRFId = item.IRFId,
                                        PrimarySkill = item.JDTiltle,
                                        ClientName = item.ClientName,
                                        EndClientName = item.ClientCompName
                                    };
                                    var mrngInterview = interviews.Where(x => Convert.ToInt32(x.interviewSlot) == 1).Count();
                                    var eveningInterview = interviews.Where(x => Convert.ToInt32(x.interviewSlot) == 2).Count();
                                    data.InterviewCount = "Morning: " + mrngInterview + " " + "Evening: " + eveningInterview;
                                    var mrngunInterview = interviews.Where(x => Convert.ToInt32(x.interviewSlot) == 1 && x.interviewer == null).Count();
                                    var eveningunInterview = interviews.Where(x => Convert.ToInt32(x.interviewSlot) == 2 && x.interviewer == null).Count();
                                    data.UnassignedInterviewCount = "Morning: " + mrngunInterview + " " + "Evening: " + eveningunInterview;
                                    var selectedInterview = interviews.Where(x => x.interviewStatus == 2).Count();
                                    var rejectedInterview = interviews.Where(x => x.interviewStatus == 3).Count();
                                    var borderLineInterview = interviews.Where(x => x.interviewStatus == 4).Count();
                                    var proxyInterview = interviews.Where(x => x.interviewStatus == 1).Count();
                                    data.SelectionCount = "Selected: " + selectedInterview + " " + "Borderline: " + borderLineInterview + " " + "Rejected: " + rejectedInterview + " " + "Proxy: " + proxyInterview;
                                    data.InterviewsReportCount = interviews.Where(x => x.InterviewerComment == null).Count().ToString();
                                    data.QualityCheck = interviews.Where(x => x.QCInterview == 0).Count().ToString();
                                    var concurrentCollection = interviews.GroupBy(x => x.interviewDate)
                                      .Select(g => new { Value = g.Key, Count = g.Count() })
                                     .OrderByDescending(x => x.Count);
                                    var concurrentInterview = concurrentCollection.Where(x => x.Count > 2).ToList();
                                    if (concurrentInterview.Count() > 0)
                                    {
                                        foreach (var conInterview in concurrentInterview)
                                        {
                                            data.ConcurrentInterview += conInterview.Value + " : " + conInterview.Count + " ";
                                        }
                                    }
                                    datamodel.Add(data);
                                }
                            }

                        }
                        else if (filterValue == "Yesterday")
                        {
                            DateTime startDate = new DateTime(DateTime.Now.AddDays(-1).Year, DateTime.Now.AddDays(-1).Month, DateTime.Now.AddDays(-1).Day, 00, 00, 00);
                            DateTime endDate = new DateTime(DateTime.Now.AddDays(-1).Year, DateTime.Now.AddDays(-1).Month, DateTime.Now.AddDays(-1).Day, 23, 59, 59);
                            foreach (var item in iRFVms)
                            {
                                var interviews = interviewVMs.Where(x => x.IRFID == item.IRFId && (x.interviewDate > startDate && x.interviewDate < endDate));
                                if (interviews != null && interviews.Count() > 0)
                                {
                                    var data = new IRFInterviewVM
                                    {
                                        IRFId = item.IRFId,
                                        PrimarySkill = item.JDTiltle,
                                        ClientName = item.ClientName,
                                        EndClientName = item.ClientCompName
                                    };
                                    var mrngInterview = interviews.Where(x => Convert.ToInt32(x.interviewSlot) == 1).Count();
                                    var eveningInterview = interviews.Where(x => Convert.ToInt32(x.interviewSlot) == 2).Count();
                                    data.InterviewCount = "Morning: " + mrngInterview + " " + "Evening: " + eveningInterview;
                                    var mrngunInterview = interviews.Where(x => Convert.ToInt32(x.interviewSlot) == 1 && x.interviewer == null).Count();
                                    var eveningunInterview = interviews.Where(x => Convert.ToInt32(x.interviewSlot) == 2 && x.interviewer == null).Count();
                                    data.UnassignedInterviewCount = "Morning: " + mrngunInterview + " " + "Evening: " + eveningunInterview;
                                    var selectedInterview = interviews.Where(x => x.interviewStatus == 2).Count();
                                    var rejectedInterview = interviews.Where(x => x.interviewStatus == 3).Count();
                                    var borderLineInterview = interviews.Where(x => x.interviewStatus == 4).Count();
                                    var proxyInterview = interviews.Where(x => x.interviewStatus == 1).Count();
                                    data.SelectionCount = "Selected: " + selectedInterview + " " + "Borderline: " + borderLineInterview + " " + "Rejected: " + rejectedInterview + " " + "Proxy: " + proxyInterview;
                                    data.InterviewsReportCount = interviews.Where(x => x.InterviewerComment == null).Count().ToString();
                                    data.QualityCheck = interviews.Where(x => x.QCInterview == 0).Count().ToString();
                                    var concurrentCollection = interviews.GroupBy(x => x.interviewDate)
                                      .Select(g => new { Value = g.Key, Count = g.Count() })
                                     .OrderByDescending(x => x.Count);
                                    var concurrentInterview = concurrentCollection.Where(x => x.Count > 2).ToList();
                                    if (concurrentInterview.Count() > 0)
                                    {
                                        foreach (var conInterview in concurrentInterview)
                                        {
                                            data.ConcurrentInterview += conInterview.Value + " : " + conInterview.Count + " ";
                                        }
                                    }
                                    datamodel.Add(data);
                                }
                            }

                        }
                        else if (filterValue == "Tomorrow")
                        {
                            DateTime startDate = new DateTime(DateTime.Now.AddDays(1).Year, DateTime.Now.AddDays(1).Month, DateTime.Now.AddDays(1).Day, 00, 00, 00);
                            DateTime endDate = new DateTime(DateTime.Now.AddDays(1).Year, DateTime.Now.AddDays(1).Month, DateTime.Now.AddDays(1).Day, 23, 59, 59);

                            foreach (var item in iRFVms)
                            {
                                var interviews = interviewVMs.Where(x => x.IRFID == item.IRFId && (x.interviewDate > startDate && x.interviewDate < endDate));
                                if (interviews != null && interviews.Count() > 0)
                                {
                                    var data = new IRFInterviewVM
                                    {
                                        IRFId = item.IRFId,
                                        PrimarySkill = item.JDTiltle,
                                        ClientName = item.ClientName,
                                        EndClientName = item.ClientCompName
                                    };
                                    var mrngInterview = interviews.Where(x => Convert.ToInt32(x.interviewSlot) == 1).Count();
                                    var eveningInterview = interviews.Where(x => Convert.ToInt32(x.interviewSlot) == 2).Count();
                                    data.InterviewCount = "Morning: " + mrngInterview + " " + "Evening: " + eveningInterview;
                                    var mrngunInterview = interviews.Where(x => Convert.ToInt32(x.interviewSlot) == 1 && x.interviewer == null).Count();
                                    var eveningunInterview = interviews.Where(x => Convert.ToInt32(x.interviewSlot) == 2 && x.interviewer == null).Count();
                                    data.UnassignedInterviewCount = "Morning: " + mrngunInterview + " " + "Evening: " + eveningunInterview;
                                    var concurrentCollection = interviews.GroupBy(x => x.interviewDate)
                                      .Select(g => new { Value = g.Key, Count = g.Count() })
                                     .OrderByDescending(x => x.Count);
                                    var concurrentInterview = concurrentCollection.Where(x => x.Count > 2).ToList();
                                    if (concurrentInterview.Count() > 0)
                                    {
                                        foreach (var conInterview in concurrentInterview)
                                        {
                                            data.ConcurrentInterview += conInterview.Value + " : " + conInterview.Count + " ";
                                        }
                                    }
                                    datamodel.Add(data);
                                }
                            }

                        }
                        else if (filterValue == "1DayTomorrow")
                        {
                            DateTime startDate = new DateTime(DateTime.Now.AddDays(2).Year, DateTime.Now.AddDays(2).Month, DateTime.Now.AddDays(2).Day, 00, 00, 00);
                            DateTime endDate = new DateTime(DateTime.Now.AddDays(2).Year, DateTime.Now.AddDays(2).Month, DateTime.Now.AddDays(2).Day, 23, 59, 59);

                            foreach (var item in iRFVms)
                            {
                                var interviews = interviewVMs.Where(x => x.IRFID == item.IRFId && (x.interviewDate > startDate && x.interviewDate < endDate));
                                if (interviews != null && interviews.Count() > 0)
                                {
                                    var data = new IRFInterviewVM
                                    {
                                        IRFId = item.IRFId,
                                        PrimarySkill = item.JDTiltle,
                                        ClientName = item.ClientName,
                                        EndClientName = item.ClientCompName
                                    };
                                    var mrngInterview = interviews.Where(x => Convert.ToInt32(x.interviewSlot) == 1).Count();
                                    var eveningInterview = interviews.Where(x => Convert.ToInt32(x.interviewSlot) == 2).Count();
                                    data.InterviewCount = "Morning: " + mrngInterview + " " + "Evening: " + eveningInterview;
                                    var mrngunInterview = interviews.Where(x => Convert.ToInt32(x.interviewSlot) == 1 && x.interviewer == null).Count();
                                    var eveningunInterview = interviews.Where(x => Convert.ToInt32(x.interviewSlot) == 2 && x.interviewer == null).Count();
                                    data.UnassignedInterviewCount = "Morning: " + mrngunInterview + " " + "Evening: " + eveningunInterview;
                                    var concurrentCollection = interviews.GroupBy(x => x.interviewDate)
                                       .Select(g => new { Value = g.Key, Count = g.Count() })
                                      .OrderByDescending(x => x.Count);
                                    var concurrentInterview = concurrentCollection.Where(x => x.Count > 2).ToList();
                                    if (concurrentInterview.Count() > 0)
                                    {
                                        foreach (var conInterview in concurrentInterview)
                                        {
                                            data.ConcurrentInterview += conInterview.Value + " : " + conInterview.Count + " ";
                                        }
                                    }
                                    datamodel.Add(data);
                                }
                            }

                        }
                        else if (filterValue == "2DayTomorrow")
                        {
                            DateTime startDate = new DateTime(DateTime.Now.AddDays(3).Year, DateTime.Now.AddDays(3).Month, DateTime.Now.AddDays(3).Day, 00, 00, 00);
                            DateTime endDate = new DateTime(DateTime.Now.AddDays(3).Year, DateTime.Now.AddDays(3).Month, DateTime.Now.AddDays(3).Day, 23, 59, 59);

                            foreach (var item in iRFVms)
                            {
                                var interviews = interviewVMs.Where(x => x.IRFID == item.IRFId && (x.interviewDate > startDate && x.interviewDate < endDate));
                                if (interviews != null && interviews.Count() > 0)
                                {
                                    var data = new IRFInterviewVM
                                    {
                                        IRFId = item.IRFId,
                                        PrimarySkill = item.JDTiltle,
                                        ClientName = item.ClientName,
                                        EndClientName = item.ClientCompName
                                    };
                                    var mrngInterview = interviews.Where(x => Convert.ToInt32(x.interviewSlot) == 1).Count();
                                    var eveningInterview = interviews.Where(x => Convert.ToInt32(x.interviewSlot) == 2).Count();
                                    data.InterviewCount = "Morning: " + mrngInterview + " " + "Evening: " + eveningInterview;
                                    var mrngunInterview = interviews.Where(x => Convert.ToInt32(x.interviewSlot) == 1 && x.interviewer == null).Count();
                                    var eveningunInterview = interviews.Where(x => Convert.ToInt32(x.interviewSlot) == 2 && x.interviewer == null).Count();
                                    data.UnassignedInterviewCount = "Morning: " + mrngunInterview + " " + "Evening: " + eveningunInterview;
                                    var concurrentCollection = interviews.GroupBy(x => x.interviewDate)
                                      .Select(g => new { Value = g.Key, Count = g.Count() })
                                     .OrderByDescending(x => x.Count);
                                    var concurrentInterview = concurrentCollection.Where(x => x.Count > 2).ToList();
                                    if (concurrentInterview.Count() > 0)
                                    {
                                        foreach (var conInterview in concurrentInterview)
                                        {
                                            data.ConcurrentInterview += conInterview.Value + " : " + conInterview.Count + " ";
                                        }
                                    }
                                    datamodel.Add(data);
                                }
                            }

                        }
                        else if (filterValue == "3DayTomorrow")
                        {
                            DateTime startDate = new DateTime(DateTime.Now.AddDays(4).Year, DateTime.Now.AddDays(4).Month, DateTime.Now.AddDays(4).Day, 00, 00, 00);
                            DateTime endDate = new DateTime(DateTime.Now.AddDays(4).Year, DateTime.Now.AddDays(4).Month, DateTime.Now.AddDays(4).Day, 23, 59, 59);
                            foreach (var item in iRFVms)
                            {
                                var interviews = interviewVMs.Where(x => x.IRFID == item.IRFId && (x.interviewDate > startDate && x.interviewDate < endDate));
                                if (interviews != null && interviews.Count() > 0)
                                {
                                    var data = new IRFInterviewVM
                                    {
                                        IRFId = item.IRFId,
                                        PrimarySkill = item.JDTiltle,
                                        ClientName = item.ClientName,
                                        EndClientName = item.ClientCompName
                                    };
                                    var mrngInterview = interviews.Where(x => Convert.ToInt32(x.interviewSlot) == 1).Count();
                                    var eveningInterview = interviews.Where(x => Convert.ToInt32(x.interviewSlot) == 2).Count();
                                    data.InterviewCount = "Morning: " + mrngInterview + " " + "Evening: " + eveningInterview;
                                    var mrngunInterview = interviews.Where(x => Convert.ToInt32(x.interviewSlot) == 1 && x.interviewer == null).Count();
                                    var eveningunInterview = interviews.Where(x => Convert.ToInt32(x.interviewSlot) == 2 && x.interviewer == null).Count();
                                    data.UnassignedInterviewCount = "Morning: " + mrngunInterview + " " + "Evening: " + eveningunInterview;
                                    var concurrentCollection = interviews.GroupBy(x => x.interviewDate)
                                      .Select(g => new { Value = g.Key, Count = g.Count() })
                                     .OrderByDescending(x => x.Count);
                                    var concurrentInterview = concurrentCollection.Where(x => x.Count > 2).ToList();
                                    if (concurrentInterview.Count() > 0)
                                    {
                                        foreach (var conInterview in concurrentInterview)
                                        {
                                            data.ConcurrentInterview += conInterview.Value + " : " + conInterview.Count + " ";
                                        }
                                    }
                                    datamodel.Add(data);
                                }
                            }

                        }
                        return PartialView("FilteredInterviewDashboard", datamodel);
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
        public async Task<IActionResult> ResetIRFFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("IRFfilters");
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
                return null;
            }

        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Add(string IntrviewerId, int? templateId = null)
        {
            if (HttpContext.Session.GetString("email") != null || HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var model = new IRFVm();
                if (!string.IsNullOrEmpty(IntrviewerId))
                {
                    model.InterviewerId = IntrviewerId;
                }
                if (templateId != null && templateId != 0)
                {
                    var templateDetails = await _templateService.GetTemplateById(templateId.Value);
                    model.TemplateId = templateId.Value;
                    model.AssessmentGroupId = templateDetails.AssessmentGroupId.Value.ToString();
                    foreach (var templateInterviewer in templateDetails.InterviewerCollection?.Where(x => x.IsSelected))
                    {
                        model.InterviewerCollection.Add(new TransInterviewer { UserId = templateInterviewer.UserId.Value, InterviewerId = templateInterviewer.InterviewerId, PrimarySkill = templateInterviewer.PrimarySkill });
                        model.EmergencyInterviewerCollection.Add(new TransEmergencyInterviewer { UserId = templateInterviewer.UserId.Value, InterviewerId = templateInterviewer.InterviewerId, PrimarySkill = templateInterviewer.PrimarySkill });
                        model.QualityController.Add(new TransQualityController { UserId = templateInterviewer.UserId.Value, InterviewerId = templateInterviewer.InterviewerId, PrimarySkill = templateInterviewer.PrimarySkill });
                    }
                    foreach (var techSkill in templateDetails.TechSkillCollection?.Where(x => x.IsSelected))
                    {
                        model.SkillCollection.Add(new TransSkill { LevelId = techSkill.LevelId.Value, SkillId = techSkill.SkillId.Value, Weightage = techSkill.Weightage.Value });
                    }
                }
                await SetAddIRFProperties(model);
                model.PerMintuteRate = 0;
                model.TotalBudget = 0;
                model.NoOfPosition = 0;
                model.Type = "B2B";
                model.Profile = profileName;
                return View(model);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddIRF(IRFVm irfDetails)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    irfDetails.Profile = profileName;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        string uploadedFileURL = string.Empty;
                        if (irfDetails.JDFile == null && (irfDetails.JDLink == null))
                        {
                            TempData["SuccessErrorMessage"] = "Adding Skills Channel has Failed. JDLink not found.";
                            return RedirectToAction("Index");
                        }
                        if (irfDetails.JDFile != null)
                        {
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(irfDetails.JDFile);
                            if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                                || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                            {
                                await _azureStorageHelper.InsertBlob(irfDetails.JDFile.FileName, "resumes", irfDetails.JDFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                var blob = containerRef.GetBlockBlobReference(irfDetails.JDFile.FileName);
                                uploadedFileURL = blob.Uri.ToString();
                                irfDetails.JDLink = uploadedFileURL;
                            }

                        }
                        if (irfDetails.CompanyDetailFile != null)
                        {
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(irfDetails.CompanyDetailFile);
                            if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                                || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                            {
                                await _azureStorageHelper.InsertBlob(irfDetails.CompanyDetailFile.FileName, "resumes", irfDetails.CompanyDetailFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                var blob = containerRef.GetBlockBlobReference(irfDetails.CompanyDetailFile.FileName);
                                uploadedFileURL = blob.Uri.ToString();
                                irfDetails.CompanyDetails = uploadedFileURL;
                            }

                        }
                        var domain = Request.Scheme + "://" + Request.Host.Value;
                        //Start: Auto Generate Scheduling Link 

                        var _schedulinglink = AutoGenerateSchedulingLink(irfDetails.StartDate, irfDetails.IRFId, irfDetails.JDTiltle + "_Scheduling");
                        irfDetails.CalendlyLink = _schedulinglink;

                        //End: Auto Generate Scheduling Link
                        if (await _irfService.AddIRFDetails(irfDetails, emailId, domain))
                        {
                            //call fire and forget method here to send emails and whatsapp for availability
                            FireAndForgetRequestToSendAvailabilityMails(irfDetails.AssessmentGroupId, irfDetails.id, irfDetails.FlatRate, irfDetails.PrimarySkillInvite);
                            TempData["SuccessErrorMessage"] = "Skills Channel " + irfDetails.IRFId + " has been added Successfully!";
                            return RedirectToAction("Index");
                        }
                        else if (irfDetails.IRFId != null)
                        {
                            TempData["SuccessErrorMessage"] = "Failed to add " + irfDetails.IRFId + " Skills Channel!";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Failed to add Skills Channel!";
                            return RedirectToAction("Index");
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
                TempData["SuccessErrorMessage"] = "Adding IRF has failed";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        private async Task SetAddIRFProperties(IRFVm model)
        {
            var skills = await _irfRepository.GetSkills();
            var AssessmentsGroups = _interviewerService.FetchAssessmentGroups();
            var interviewers = new List<Interviewer>();
            if (string.IsNullOrEmpty(model.AssessmentGroupId))
            {
                interviewers = await _irfService.GetInterviewersForIRF(AssessmentsGroups.FirstOrDefault()?.id.ToString());
                AssessmentsGroups = AssessmentsGroups.Where(x => x.IsRelevant == true).ToList();
            }
            else
            {
                interviewers = await _irfService.GetInterviewersForIRF(model.AssessmentGroupId);
                var _assessmentid = Convert.ToInt32(model.AssessmentGroupId);
                var _listassessment = AssessmentsGroups.Where(x => x.id == _assessmentid).ToList();
                AssessmentsGroups = AssessmentsGroups.Where(x => x.IsRelevant == true).ToList();
                if (AssessmentsGroups.Where(x => x.id == _assessmentid).Count() == 0)
                {
                    AssessmentsGroups = AssessmentsGroups.Union(_listassessment);
                }
            }
            model.JoinabilityTypeList = new SelectList(_irfService.GetJoinabilityMatrix(), "id", "Name");
            model.SkillGeneralTypeList = new SelectList(_irfService.GetGeneralSkillMatrix(), "id", "Name");
            model.SelectabilityTypeList = new SelectList(_irfService.GetSelectabilityMatrix(), "id", "Name");
            var priorityTypes = from PriorityLevel s in Enum.GetValues(typeof(PriorityLevel))
                                select new { ID = (int)s, Name = s.GetDescription() };
            model.PriorityLevelList = new SelectList(priorityTypes, "ID", "Name");
            var clients = await _irfRepository.GetClients();
            ViewBag.Clients = new SelectList(clients, "id", "Name");
            if (HttpContext.Session.GetString("email") != null || HttpContext.Session.GetString("profileName") != null)
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                model.LoggedEmail = emailid;
                if (profileName == "SeniorHR" || profileName == "SupportAdmin")
                    ViewBag.Clients = new SelectList(await _clientService.GetClients(emailid, profileName), "id", "Name");
            }
            ViewBag.Level = new SelectList(await _irfRepository.GetLevels(), "id", "Name");
            ViewBag.LevelTech = new SelectList(await _irfRepository.GetLevelTech(), "id", "Name");
            var Skillset = skills;
            if (Skillset != null)
            {
                Skillset = Skillset.OrderBy(x => x.Name.Trim()).ToList();
            }
            ViewBag.Skill = new SelectList(Skillset, "id", "Name");
            //ViewBag.ClientUser = new SelectList(_irfRepository.GetCientUsers(), "id", "EmailId");
            ViewBag.ClientUser = new SelectList(_irfService.GetClientUser(clients.FirstOrDefault().id), "id", "EmailId");
            ViewBag.ClientRole = new SelectList(_irfRepository.GetClientRoles(), "id", "Name");
            ViewBag.Interviewers = new SelectList(interviewers, "UserId", "EmailId");
            ViewBag.Recruiters = new SelectList(_irfRepository.GetRecruiters(), "UserId", "EmailId");
            ViewBag.ResumeEvaluators = new SelectList(_irfRepository.GetResumeEvaluators(), "UserId", "EmailId");
            ViewBag.HRs = new SelectList(_irfRepository.GetHRs(), "UserId", "EmailId");
            ViewBag.QualityController = new SelectList(interviewers, "UserId", "EmailId");
            ViewBag.EmergencyInterviewers = new SelectList(interviewers, "UserId", "EmailId");
            //ViewBag.RiseBirdHR = new SelectList(_irfService.GetAllHRs(), "id", "EmailId");
            ViewBag.ChannelOwnerOptions = new SelectList(_irfService.GetEligibleChannelOwners(), "id", "EmailId");

            model.AssessmentGroupList = new SelectList(AssessmentsGroups, "id", "AssessmentGroup");
            var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                             select new { ID = (int)s, Name = s.GetDescription() };
            ViewBag.Heuristicsscorelist = new SelectList(Heuristics, "ID", "Name");
            var jobLevels = from JobLevels s in Enum.GetValues(typeof(JobLevels))
                            select new { ID = (int)s, Name = s.GetDescription() };
            ViewBag.JobLevelList = new SelectList(jobLevels, "ID", "Name");

            var companyTypes = from CompanyType s in Enum.GetValues(typeof(CompanyType))
                               select new { ID = (int)s, Name = s.GetDescription() };
            model.CompanyTypeList = new SelectList(companyTypes, "ID", "Name");
            if (!string.IsNullOrEmpty(model.InterviewerId))
            {
                List<InterviewerIds> _string = new List<InterviewerIds>();
                ViewBag.InterviewerFromSearch = model.InterviewerId;
                var interviewer = model.InterviewerId.Split(',');
                var resultset = _irfRepository.pickInterviewerSills(model.InterviewerId);
                for (int i = 0; i < interviewer.Count(); i++)
                {
                    _string.Add(new InterviewerIds { index = i, Id = Convert.ToInt32(interviewer[i]) });
                }
                model.InterviewerIds = _string;
                ViewBag.InterviewerSkills = resultset;
            }
            else
            {
                ViewBag.InterviewerFromSearch = "";
                ViewBag.InterviewerSkills = "";
            }
            if (string.IsNullOrEmpty(model.Type))
            {
                model.Type = "B2B";
            }
            //model.IRFRecommendations = new IRFRecommendations();
            //model.IRFRecommendations.CategoryList = new SelectList(_skillService.GetAssessmentGroupCategory(), "id", "Name");
            var interviewduration = from InterviewDurationEnum s in Enum.GetValues(typeof(InterviewDurationEnum))
                                    select new { ID = (int)s, Name = s.GetDescription() };
            model.InterviewDurationList = new SelectList(interviewduration, "ID", "Name");
            return;
        }
        //    catch (Exception ex)
        //    {
        //        _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //    }
        //}


        private async Task FireAndForgetRequestToSendAvailabilityMails(string assessmentGroupId, int irfId, int flatRate, string primarySkillInvite)
        {
            var dto = new AvailabilityCheckDTO{ id = irfId, AssessmentGroupId = assessmentGroupId, FlatRate = flatRate, PrimarySkillInvite = primarySkillInvite };
            var path = $"https://risebirdmicroservicededicated.azurewebsites.net/api/CheckIRFAvailability";
            //var path = $"http://localhost:7169/api/UploadAndVerifyCandidateImage";
            var functionKey = "ok-GN-EaOPcWFO2ft8_m_eIaLSxMgT_0a19YdeFmBSPCAzFuuzQdQg==";
            await _httpClient.SendRequestAsync(HttpMethod.Post, path, dto, functionKey, "", _cancellationToken);
        }


        [HttpGet]
        public string GetInterviewerSkills(int id)
        {
            var name = _irfService.GetInterviewerSkills(id);
            return name;
        }

        [HttpGet]
        public JsonResult GetInterviewerSkillsAndResume(int id)
        {
            return Json(_irfService.GetInterviewerSkillsAndResume(id));
        }

        public async Task<int> GetInterviewerRate(int userId, int? companyType, int? countryType = null)
        {

            try
            {
                var rate = await _irfService.GetInterviewerRate(userId, companyType, countryType);
                return rate;

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<int> GetRenewedInterviewerRate(int? planType, int? countryType = null, int? companyType = null)
        {
            try
            {
                var rate = await _irfService.GetRenewedInterviewerRate(planType, countryType, companyType);
                return rate;

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        [Authorize]
        public async Task<int> GetInterviewerQCRate(int userId)
        {

            try
            {
                var rate = await _irfService.GetInterviewerQCRate(userId);
                return rate;

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        [Authorize]
        [HttpGet]
        public string GetInterviewerSkillsFromProfile(int id)
        {
            var name = _iInterviewerSkillsService.GetInterviewerSkills(id);
            return name;
        }

        [Authorize]
        [HttpGet]
        public string GetRecruiterDomain(string userId)
        {
            return _irfService.GetRecruiterDomain(Convert.ToInt32(userId));

        }

        //[Authorize]
        //[HttpGet]
        //public async Task<SelectList> GetInterviewers(string assessmentgroupid)
        //{

        //    var list = new List<SelectListItem>();
        //    var interviewers = await _irfService.GetInterviewers(assessmentgroupid);

        //    if (interviewers != null && interviewers.Count() > 0)
        //    {
        //        return new SelectList(interviewers, "UserId", "EmailId");
        //    }
        //    else
        //        list.Add(new SelectListItem { Text = "No Interviewer", Value = "" });
        //    //clientUsers.ToList().Add(new Users { id = 0, FirstName = "No Client User" });
        //    return new SelectList(list, "Value", "Text");
        //}

        [Authorize]
        [HttpGet]
        public string GetQCSkills(string id)
        {
            return _irfService.GetQCSkills(Convert.ToInt32(id));
        }

        //GetClientUserRole
        [HttpGet]
        public string GetClientUserRole(int id)
        {
            return _irfService.GetClientUserRole(id);
        }

        [Authorize]
        [HttpGet]
        public string GetQuestionSkills(string id)
        {
            return _irfService.GetQuestionSkills(id);
        }

        [Authorize]
        [HttpGet]
        public async Task<string> GetTechSkillofSkillChannelByQuestion(string question, int irfId)
        {
            return await _irfService.GetTechSkillofSkillChannelByQuestion(question, irfId);
        }

        [Authorize]
        [HttpGet]
        public bool CheckUniqueness(string value, bool isCloneTemplate = false)
        {
            return _irfService.CheckUniqueness(value, isCloneTemplate);
        }

        //[Authorize]
        //public IActionResult SampleQuestions(int id)
        //{
        //    if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
        //    {
        //        var profileName = HttpContext.Session.GetString("profileName")?.Trim();
        //        profileName = profileName.Replace("\"", "");
        //        if (profileName == "Admin" || profileName == "HR")
        //        {
        //            TechSkillComments model = new TechSkillComments();
        //            var result = _irfService.GetTechSkillComments(id);
        //            //if (result.Count() > 0)
        //            model.SkillCollection = result;
        //            model.Skills = new SelectList(_irfService.GetTechSkills(id), "id", "Name");
        //            model.IRFId = id;
        //            model.Profile = profileName;
        //            return View(model);
        //        }
        //        else
        //        {
        //            return RedirectToAction("SignOut", "Session");
        //        }

        //    }
        //    else
        //    {
        //        return RedirectToAction("SignOut", "Session");
        //    }
        //}

        //[Authorize]
        //[HttpPost]
        //public IActionResult SampleQuestions(TechSkillComments model)
        //{
        //    try
        //    {
        //        if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
        //        {
        //            var profileName = HttpContext.Session.GetString("profileName")?.Trim();
        //            profileName = profileName.Replace("\"", "");
        //            if (profileName == "Admin" || profileName == "HR")
        //            {
        //                if (_irfService.AddSkillComments(model))
        //                {
        //                    //TempData["SuccessErrorMessage"] = "Questions saved successfully";
        //                    model.Message = "Questions saved successfully";
        //                    model.Skills = new SelectList(_irfService.GetTechSkills(Convert.ToInt32(model.IRFId)), "id", "Name");
        //                    return View(model);
        //                    //return RedirectToAction("Index");

        //                }
        //                else
        //                {
        //                    //TempData["SuccessErrorMessage"] = "Questions were not saved.";
        //                    model.Message = "Questions were not saved.";
        //                    model.Skills = new SelectList(_irfService.GetTechSkills(Convert.ToInt32(model.IRFId)), "id", "Name");
        //                    return View(model);
        //                    //return RedirectToAction("Index");

        //                }
        //            }
        //            else
        //            {
        //                return RedirectToAction("SignOut", "Session");
        //            }

        //        }
        //        else
        //        {
        //            return RedirectToAction("SignOut", "Session");
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        TempData["SuccessErrorMessage"] = "Questions were not saved";
        //        return RedirectToAction("Index");
        //    }
        //}


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SampleQuestions(int id, string irf = null)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Interviewer" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                {
                    TechSkillComments model = new TechSkillComments();
                    var result = _irfService.GetTechSkillComments(id, emailid);
                    //if (result.Count() > 0)
                    model.SkillCollection = result;
                    //var techSkills = new List<Skill>();
                    var skills = await _irfService.GetTechSkills(id);

                    //foreach (var skill in skills)
                    //{
                    //    techSkills.Add(new Skill { id = skill.id, Name = skill.Name?.Replace("\t", "").Replace("\n", "").Replace("\r", ""), Description = skill.Description?.Replace("\t", "").Replace("\n", "").Replace("\r", "") });
                    //}
                    ViewBag.Skills = new SelectList(skills, "id", "Name");
                    //model.SkillsOptionsData = JsonConvert.SerializeObject(skills);
                    //model.SkillsOptionsData = model.SkillsOptionsData?.Replace("\t", "").Replace("\n", "").Replace("\r", "");
                    //ViewBag.Skills = new SelectList(skills, "id", "Name");
                    //model.SkillsOptionsData = JsonConvert.SerializeObject(techSkills);
                    model.IRFId = id;
                    model.Profile = profileName;
                    model.UserId = _irfService.GetUserId(emailid);
                    if (!string.IsNullOrEmpty(irf))
                    {
                        model.IrfName = irf;
                    }
                    if (TempData["SuccessErrorMessageQuestions"] != null)
                    {
                        model.Message = TempData["SuccessErrorMessageQuestions"].ToString();
                        TempData.Remove("SuccessErrorMessageQuestions");
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
        [HttpPost]
        [DisableRequestSizeLimit]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SampleQuestions(TechSkillComments model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Interviewer" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        if (ModelState.IsValid)
                        {
                            var questionsCollection = model.SkillCollection.Select(x => (x.Comment, x.Skill, x.UserId)).Where(x => x.UserId == model.UserId);

                            if (questionsCollection.Count() != questionsCollection.Distinct().Count())
                            {
                                //duplicates exist
                                ModelState.AddModelError("", "The skill-question combination added by you should contain unique entries.");
                                var skills = await _irfService.GetTechSkills(model.IRFId);
                                ViewBag.Skills = new SelectList(skills, "id", "Name");
                                //model.SkillsOptionsData = JsonConvert.SerializeObject(skills);
                                model.Profile = profileName;
                                return View(model);
                            }

                            if (await _irfService.AddSkillComments(model))
                            {
                                TempData["SuccessErrorMessageQuestions"] = "Question(s) saved Successfully!";
                                return RedirectToAction("SampleQuestions", new { id = model.IRFId, irf = model.IrfName });
                            }
                            else
                            {
                                TempData["SuccessErrorMessageQuestions"] = "Failed to save Question(s)!";
                                return RedirectToAction("SampleQuestions", new { id = model.IRFId, irf = model.IrfName });
                            }
                        }
                        else
                        {
                            var skills = await _irfService.GetTechSkills(model.IRFId);
                            ViewBag.Skills = new SelectList(skills, "id", "Name");
                            //model.SkillsOptionsData = JsonConvert.SerializeObject(skills);

                            model.Profile = profileName;
                            //ModelState.AddModelError
                            return View(model);
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
                TempData["SuccessErrorMessageQuestions"] = "Questions were not saved";
                return RedirectToAction("SampleQuestions", new { id = model.IRFId, irf = model.IrfName });
            }
        }

        [HttpPost]
        public JsonResult IsQuestionUsedInReport(string question)
        {
            try
            {
                bool isQuestionUsed = _irfService.IsQuestionUsedInReport(question/*.Replace("\n", "").Replace("\r", "")*/).Result;
                return Json(isQuestionUsed);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(false);
            }
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ArchiveIRF(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        var result = await _irfService.ArchiveIRF(id);
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
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(false);
            }

        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> UnArchiveIRF(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        var result = await _irfService.UnArchiveIRF(id);
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
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(false);
            }

        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SelectedInterviewMeetings(string id, string irfId, int? Page, string searchValue)
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
                        var selectedMeeting = new List<MeetingVM>();
                        var meetings = new List<MeetingVM>();
                        if (profileName == "Client")
                        {
                            //meetings = (await _irfService.GetIRFInterviewMeetingsForClient(Convert.ToInt32(id), profileName, emailid)).Where(x => x.QCInterview > 0 && x.QCStatus == 1).ToList();
                            meetings = (await _irfService.GetIRFInterviewMeetingsForClient(Convert.ToInt32(id), profileName, emailid)).Where(x => !x.IsQCRequired || (x.QCInterview > 0 && x.QCStatus == 1 && x.IsQCRequired)).ToList();
                        }
                        else
                        {
                            //meetings = (await _irfService.GetIRFInterviewMeetings(Convert.ToInt32(id), profileName, emailid)).Where(x => x.QCInterview > 0 && x.QCStatus == 1).ToList();
                            meetings = (await _irfService.GetIRFInterviewMeetings(Convert.ToInt32(id), profileName, emailid)).Where(x => !x.IsQCRequired || (x.QCInterview > 0 && x.QCStatus == 1 && x.IsQCRequired)).ToList();
                        }
                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((ReceiveMeetingStatus)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                        ViewBag.IRF = irfId;
                        ViewBag.IRFintId = id;

                        foreach (var m in meetings)
                        {
                            if (m.InterviewStatus.HasValue && (m.InterviewStatus.Value == 2 || m.InterviewStatus.Value == 4))
                            {
                                m.intervieweStatusStatDesc = ((InterviewStatus)(m.InterviewStatus)).GetDescription();
                            }
                            else
                            {
                                continue;
                            }

                            if (m.L2InterviewStatus.HasValue)
                            {
                                m.L2InterviewStatDesc = ((L2InterviewStatus)(m.L2InterviewStatus)).GetDescription();
                            }
                            if (m.L2InterviewStatus.HasValue)
                            {
                                m.ClientInterviewStatDesc = ((ClientInterviewStatus)(m.ClientInterviewStatus)).GetDescription();
                            }
                            if (m.JoiningStatus.HasValue)
                            {
                                m.JoiningStatusStatDesc = ((JoiningStatus)(m.JoiningStatus)).GetDescription();
                            }
                            if (m.PreparationStatus.HasValue)
                            {
                                m.PreparationStatusDesc = ((PreparationStatus)(m.PreparationStatus)).GetDescription();
                            }
                            if (m.OfferStatus.HasValue)
                            {
                                m.OfferStatusStatDesc = ((PreparationStatus)(m.OfferStatus)).GetDescription();
                            }
                            if (m.MeetingStatus.HasValue)
                            {
                                m.MeetingStatusStatusDesc = ((MeetingStatus)(m.MeetingStatus)).GetDescription();
                            }
                            if (m.QCStatus.HasValue)
                            {
                                m.QCStatusStatusDesc = ((QCStatus)(m.QCStatus)).GetDescription();
                            }
                            if (m.IsApproved.HasValue)
                            {
                                m.NextLevelApprovalStat = ((NextLevelApprovalStatus)(m.IsApproved)).GetDescription();
                            }
                            if (m.IsSelected.HasValue)
                            {
                                m.FinalSelectionStat = ((FinalSelectionStatus)(m.IsSelected)).GetDescription();
                            }
                            selectedMeeting.Add(m);
                        }
                        //                        var list = new SelectList(new[]
                        //{
                        //    new { ID = "Today", Name = "Today"},
                        //    new { ID = "Tomorrow", Name = "Tomorrow"  },
                        //    new { ID = "Yesterday", Name = "Yesterday"  },
                        //    new { ID = "Next Seven Days", Name = "Next Seven Days" },
                        //    new { ID = "All", Name = "All"},
                        //},
                        //"ID", "Name", "All");

                        //                        ViewData["list"] = list;
                        ViewBag.profileName = profileName.ToLower();
                        if (!string.IsNullOrEmpty(searchValue))
                        {
                            searchValue = searchValue.ToUpper().Trim();
                            selectedMeeting = selectedMeeting.Where(x => (!string.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == "") || (!string.IsNullOrEmpty(x.ClientHrContact) ? x.ClientHrContact.ToUpper().Contains(searchValue) : x.ClientHrContact == "") || (!string.IsNullOrEmpty(x.confirmationStatus) ? x.confirmationStatus.ToUpper().Contains(searchValue) : x.confirmationStatus == "") || (!string.IsNullOrEmpty(x.MeetingStatusStatusDesc) ? x.MeetingStatusStatusDesc.ToUpper().Contains(searchValue) : x.MeetingStatusStatusDesc == "") || (!string.IsNullOrEmpty(x.intervieweStatusStatDesc) ? x.intervieweStatusStatDesc.ToUpper().Contains(searchValue) : x.intervieweStatusStatDesc == "")).ToList();

                        }
                        ViewBag.TotalCount = selectedMeeting.Count;

                        ViewBag.PageStartCount = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                        int a = selectedMeeting.Count / 10;
                        if (selectedMeeting.Count % 10 == 0)
                        {
                            ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                        }
                        else
                        {
                            if (Page == null)
                            {
                                Page = 1;
                            }
                            a = a + 1;
                            if (a == Page)
                            {
                                ViewBag.PageEndCount = selectedMeeting.Count;
                            }
                            else
                            {
                                ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));

                            }

                        }
                        if (selectedMeeting.Count == 0)
                        {
                            ViewBag.PageStartCount = 0;
                            ViewBag.PageEndCount = 0;
                        }
                        _modelDetail = selectedMeeting;
                        ViewBag.IRFId = id;
                        return View(selectedMeeting.ToPagedList(Page ?? 1, 10));


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
        [ActionName("MultipleCandidateInvites")]
        public async Task<IActionResult> MultipleCandidateInvites(string irfId, int? Page, string searchValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {

                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {

                        var modelList = _meetingService.IRFMultipleCandidateInvites(irfId);
                        if (profileName == "Admin")
                        {
                            modelList.ForEach(x => x.IsAdmin = true);
                        }
                        modelList = modelList.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                        if (!string.IsNullOrEmpty(searchValue))
                        {
                            searchValue = searchValue.ToUpper().Trim();
                            modelList = modelList.Where(x => (!string.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == "") || (!string.IsNullOrEmpty(x.JDTiltle) ? x.JDTiltle.Split("|")[0].ToUpper().Contains(searchValue) : x.JDTiltle == "") || (!string.IsNullOrEmpty(x.confirmationStatus) ? x.confirmationStatus.ToUpper().Contains(searchValue) : x.confirmationStatus == "") || (!string.IsNullOrEmpty(x.MeetingStatusStatusDesc) ? x.MeetingStatusStatusDesc.ToUpper().Contains(searchValue) : x.MeetingStatusStatusDesc == "") || (!string.IsNullOrEmpty(x.intervieweStatusStatDesc) ? x.intervieweStatusStatDesc.ToUpper().Contains(searchValue) : x.intervieweStatusStatDesc == "")).ToList();

                        }
                        ViewBag.TotalCount = modelList.Count;

                        ViewBag.PageStartCount = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                        int a = modelList.Count / 10;
                        if (modelList.Count % 10 == 0)
                        {
                            ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                        }
                        else
                        {
                            a = a + 1;
                            if (a == Page)
                            {
                                ViewBag.PageEndCount = modelList.Count;
                            }
                            else
                            {
                                ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));

                            }

                        }
                        if (modelList.Count == 0)
                        {
                            ViewBag.PageStartCount = 0;
                            ViewBag.PageEndCount = 0;
                        }
                        _modelDetle = modelList;
                        ViewBag.IRFID = irfId;
                        return View(modelList.ToPagedList(Page ?? 1, 10));

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
        [ActionName("InterviewMeetings")]
        public async Task<IActionResult> InterviewMeetings(string id, string irfId, int? Page, string searchValue)
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
                        var meetings = new List<MeetingVM>();

                        if (profileName == "Client")
                        {
                            meetings = (await _irfService.GetIRFInterviewMeetingsForClient(Convert.ToInt32(id), profileName, emailid)).ToList();

                        }
                        else
                        {
                            meetings = (await _irfService.GetIRFInterviewMeetings(Convert.ToInt32(id), profileName, emailid)).ToList();

                        }

                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((ReceiveMeetingStatus)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                        ViewBag.IRF = irfId;
                        ViewBag.IRFintId = id;

                        foreach (var m in meetings)
                        {
                            if (m.InterviewStatus.HasValue)
                            {
                                m.intervieweStatusStatDesc = ((InterviewStatus)(m.InterviewStatus)).GetDescription();
                            }
                            if (m.L2InterviewStatus.HasValue)
                            {
                                m.L2InterviewStatDesc = ((L2InterviewStatus)(m.L2InterviewStatus)).GetDescription();
                            }
                            if (m.L2InterviewStatus.HasValue)
                            {
                                m.ClientInterviewStatDesc = ((ClientInterviewStatus)(m.ClientInterviewStatus)).GetDescription();
                            }
                            if (m.JoiningStatus.HasValue)
                            {
                                m.JoiningStatusStatDesc = ((JoiningStatus)(m.JoiningStatus)).GetDescription();
                            }
                            if (m.PreparationStatus.HasValue)
                            {
                                m.PreparationStatusDesc = ((PreparationStatus)(m.PreparationStatus)).GetDescription();
                            }
                            if (m.OfferStatus.HasValue)
                            {
                                m.OfferStatusStatDesc = ((PreparationStatus)(m.OfferStatus)).GetDescription();
                            }
                            if (m.MeetingStatus.HasValue)
                            {
                                m.MeetingStatusStatusDesc = ((MeetingStatus)(m.MeetingStatus)).GetDescription();
                            }
                            if (m.QCStatus.HasValue)
                            {
                                m.QCStatusStatusDesc = ((QCStatus)(m.QCStatus)).GetDescription();
                            }

                        }

                        if (HttpContext.Session.GetString("IRFInterviewFilter") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<IRFInterviewFilter>(HttpContext.Session.GetString("IRFInterviewFilter"));
                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                meetings = meetings.Where(m => m.interviewDate >= filter.StartDate && m.interviewDate <= filter.EndDate).ToList();
                            }

                            filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = meetings.Select(x => x.ClientHrContact).ToArray() : filter.ClientHR = filter.ClientHR;
                            filter.ConfirmationStatus = filter.ConfirmationStatus == null ? filter.ConfirmationStatus = meetings.Select(x => x.confirmationStatus).ToArray() : filter.ConfirmationStatus = filter.ConfirmationStatus;
                            filter.MeetingStatus = filter.MeetingStatus == null ? filter.MeetingStatus = meetings.Select(x => x.MeetingStatusStatusDesc).ToArray() : filter.MeetingStatus = filter.MeetingStatus;
                            filter.InterviewStatus = filter.InterviewStatus == null ? filter.InterviewStatus = meetings.Select(x => x.intervieweStatusStatDesc).ToArray() : filter.InterviewStatus = filter.InterviewStatus;
                            //filter.Interviewer = filter.Interviewer == null ? filter.Interviewer = meetings.Select(x => x.interviewer).ToArray() : filter.Interviewer = filter.Interviewer;
                            filter.CandidateName = filter.CandidateName == null ? filter.CandidateName = meetings.Select(x => x.candidateName).ToArray() : filter.CandidateName = filter.CandidateName;
                            filter.irfId = Convert.ToInt32(id);
                            filter.IRFName = irfId;

                            meetings = meetings.Where(c => ((filter.ClientHR != null && filter.ClientHR.Count() > 0) ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                            && ((filter.ConfirmationStatus != null && filter.ConfirmationStatus.Count() > 0) ? filter.ConfirmationStatus.Contains(c.confirmationStatus) : c.confirmationStatus == null)
                            && ((filter.MeetingStatus != null && filter.MeetingStatus.Count() > 0) ? filter.MeetingStatus.Contains(c.MeetingStatusStatusDesc) : c.MeetingStatusStatusDesc == string.Empty)
                            && ((filter.InterviewStatus != null && filter.InterviewStatus.Count() > 0) ? filter.InterviewStatus.Contains(c.intervieweStatusStatDesc) : c.intervieweStatusStatDesc == string.Empty)
                            //&& ((filter.Interviewer != null && filter.Interviewer.Count() > 0) ? filter.Interviewer.Contains(c.interviewer) : c.interviewer == null)
                            && ((filter.CandidateName != null && filter.CandidateName.Count() > 0) ? filter.CandidateName.Contains(c.candidateName) : c.candidateName == null)).ToList();

                        }


                        meetings = meetings.OrderByDescending(x => x.interviewDate).ToList();

                        if (!string.IsNullOrEmpty(searchValue))
                        {
                            searchValue = searchValue.ToUpper().Trim();
                            meetings = meetings.Where(x => (!string.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == "") || (!string.IsNullOrEmpty(x.ClientHrContact) ? x.ClientHrContact.ToUpper().Contains(searchValue) : x.ClientHrContact == "")
                            || (!string.IsNullOrEmpty(x.confirmationStatus) ? x.confirmationStatus.ToUpper().Contains(searchValue) : x.confirmationStatus == "") || (!string.IsNullOrEmpty(x.MeetingStatusStatusDesc) ? x.MeetingStatusStatusDesc.ToUpper().Contains(searchValue) : x.MeetingStatusStatusDesc == "")
                            || (!string.IsNullOrEmpty(x.intervieweStatusStatDesc) ? x.intervieweStatusStatDesc.ToUpper().Contains(searchValue) : x.intervieweStatusStatDesc == "")).ToList();

                        }
                        ViewBag.TotalCount = meetings.Count;

                        ViewBag.PageStartCount = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                        int a = meetings.Count / 10;
                        if (meetings.Count % 10 == 0)
                        {
                            ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                        }
                        else
                        {
                            a = a + 1;
                            if (a == Page)
                            {
                                ViewBag.PageEndCount = meetings.Count;
                            }
                            else
                            {
                                ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));

                            }

                        }
                        if (meetings.Count == 0)
                        {
                            ViewBag.PageStartCount = 0;
                            ViewBag.PageEndCount = 0;
                        }
                        _modelDetail = meetings;
                        //ViewBag.IRFId =id;
                        return View(meetings.ToPagedList(Page ?? 1, 10));

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
        public async Task<IActionResult> InterviewMeetings(IRFInterviewFilter filter)
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
                        if (filter != null)
                        {
                            HttpContext.Session.SetString("IRFInterviewFilter", JsonConvert.SerializeObject(filter));
                        }

                        return RedirectToAction("InterviewMeetings", new { id = filter.irfId.ToString(), irfId = filter.IRFName });

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
        public async Task<IActionResult> ResetIRFInterviewFilter()
        {
            IRFInterviewFilter filter = new IRFInterviewFilter();
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    filter = JsonConvert.DeserializeObject<IRFInterviewFilter>(HttpContext.Session.GetString("IRFInterviewFilter"));

                    HttpContext.Session.Remove("IRFInterviewFilter");
                    return RedirectToAction("InterviewMeetings", new { id = filter.irfId.ToString(), irfId = filter.IRFName });
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("InterviewMeetings", new { id = filter.irfId.ToString(), irfId = filter.IRFName });
            }

        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EditIRF(int id, int clonevalue, int? templateId, bool isClone = false)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");

                    IRFVm model = new IRFVm();
                    //model.UseAsCloneTemplate = useAsCloneTemplate;
                    model = await _irfService.GetEditIRF(id, templateId, isClone);
                    if (model == null)
                        return RedirectToAction("Index");
                    model.IsClone = isClone;

                    int abc;
                    if (!int.TryParse(model.JobCountry, out abc))
                    {
                        if (model.JobCountry?.ToUpper() == "INDIA" || model.JobCountry?.ToUpper() == "OFFSHORE")
                        {
                            model.JobCountry = "1";
                        }
                        else
                        {
                            model.JobCountry = "2";
                        }
                    }

                    model.OldJDLink = model.JDLink;
                    model.OldCompanyDetails = model.CompanyDetails;
                    model.OldJDTitle = model.JDTiltle;
                    ViewBag.clonevalue = clonevalue;
                    model.clonevalue = clonevalue;
                    model.Profile = profileName;
                    if (profileName == "Client" && templateId != null)
                    {
                        await _clonableSkillChannelTemplateService.PopulateDropDownsForSkillChannel(model, emailid);
                        return View("Edit", model);
                    }
                    else
                    {
                        await SetEditIRFProperties(model);
                        return View(model);
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
        public async Task<IActionResult> EditIRF(IRFVm model)
        {
            try
            {
                if (model != null && HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    ViewBag.clonevalue = model.clonevalue;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        //check duplicates for interviewer, emergency interviewer and quality controllers
                        bool isDuplicate = false;
                        var interviewerCollection = model.InterviewerCollection.Select(x => (x.UserId));
                        if (interviewerCollection.Count() != interviewerCollection.Distinct().Count())
                        {
                            //duplicates exist
                            isDuplicate = true;
                            ModelState.AddModelError("", "Please remove duplicate entries from Interviewers' list.");
                        }

                        var emergencyInterviewerCollection = model.EmergencyInterviewerCollection.Select(x => (x.UserId));
                        if (emergencyInterviewerCollection.Count() != emergencyInterviewerCollection.Distinct().Count())
                        {
                            //duplicates exist
                            isDuplicate = true;
                            ModelState.AddModelError("", "Please remove duplicate entries from Emergency Interviewers' list.");
                        }

                        //var qualityControllerCollection = model.QualityController.Select(x => (x.UserId));
                        //if (qualityControllerCollection.Count() != qualityControllerCollection.Distinct().Count())
                        //{
                        //    //duplicates exist
                        //    isDuplicate = true;
                        //    ModelState.AddModelError("", "Please remove duplicate entries from Quality Controllers' list.");
                        //}

                        if (isDuplicate)
                        {
                            model.isDuplicateEntries = isDuplicate;

                            await SetEditIRFProperties(model);
                            return View("EditIRF", model);
                        }

                        if (!model.IsClone)
                        {
                            model.ResourceUrl = Request.Scheme + "://" + Request.Host.Value + Request.Path + Request.QueryString;
                        }

                        if (string.IsNullOrEmpty(model.MeetingPlatform))
                        {
                            model.MeetingPlatform = MeetingPlatforms.Zoom;
                        }
                        //else
                        //{
                        //    model.id = 0;
                        //}
                        if (model.JDFile != null)
                        {
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.JDFile);
                            if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                                || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                            {
                                await _azureStorageHelper.InsertBlob(model.JDFile.FileName, "resumes", model.JDFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                var blob = containerRef.GetBlockBlobReference(model.JDFile.FileName);
                                string uploadedFileURL = blob.Uri.ToString();
                                if (uploadedFileURL == null)
                                {
                                    TempData["SuccessErrorMessage"] = "Skills Channel edits were not saved.";
                                    return RedirectToAction("Index");

                                }
                                model.JDLink = uploadedFileURL;
                            }
                        }
                        else if (model.JDLink == null || model.JDLink == "")
                        {
                            model.JDLink = model.OldJDLink;
                        }
                        if (model.CompanyDetailFile != null)
                        {
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.CompanyDetailFile);
                            if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                                || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                            {
                                await _azureStorageHelper.InsertBlob(model.CompanyDetailFile.FileName.Replace(" ", "_"), "resumes", model.CompanyDetailFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                var blob = containerRef.GetBlockBlobReference(model.CompanyDetailFile.FileName.Replace(" ", "_"));
                                string uploadedFileURL = blob.Uri.ToString();
                                if (uploadedFileURL == null)
                                {
                                    TempData["SuccessErrorMessage"] = "Skills Channel edits were not saved.";
                                    return RedirectToAction("Index");

                                }
                                model.CompanyDetails = uploadedFileURL;
                            }
                        }
                        else if (model.CompanyDetails == null || model.CompanyDetails == "")
                        {
                            model.CompanyDetails = model.OldCompanyDetails;
                        }

                        if (model.IsClone && !string.IsNullOrEmpty(model.IRFId))
                        {
                            if (!(_irfService.CheckUniqueness(model.IRFId)))
                            {
                                TempData["SuccessErrorMessage"] = "Sorry! Skills Channel with this name already exists!";
                                return RedirectToAction("Index");
                            }
                            var domain = Request.Scheme + "://" + Request.Host.Value;
                            //Start: Auto Generate Scheduling Link 
                            if (string.IsNullOrEmpty(model.CalendlyLink))
                            {
                                var _schedulinglink = AutoGenerateSchedulingLink(model.StartDate, model.IRFId, model.JDTiltle + "_Scheduling");
                                model.CalendlyLink = _schedulinglink;
                            }
                            //End: Auto Generate Scheduling Link
                            model.id = 0;
                            if (_irfService.AddIRFDetails(model, emailId, domain).Result)
                            {
                                TempData["SuccessErrorMessage"] = "Skills Channel " + model.IRFId + " has been saved Successfully!";
                                return RedirectToAction("Index");
                            }
                            else if (model.IRFId != null)
                            {
                                TempData["SuccessErrorMessage"] = "Skills Channel " + model.IRFId + " edits were not saved.";
                                return RedirectToAction("Index");
                            }
                            else
                            {
                                TempData["SuccessErrorMessage"] = "Skills Channel edits were not saved.";
                                return RedirectToAction("Index");
                            }
                        }
                        else
                        {
                            if (await _irfService.UpdateIRF(model, emailId))
                            {
                                TempData["SuccessErrorMessage"] = "Skills Channel " + model.IRFId + " has been saved Successfully!";
                                return RedirectToAction("Index");
                            }
                            else if (model.IRFId != null)
                            {
                                TempData["SuccessErrorMessage"] = "Skills Channel " + model.IRFId + " edits were not saved.";
                                return RedirectToAction("Index");
                            }
                            else
                            {
                                TempData["SuccessErrorMessage"] = "Skills Channel edits were not saved.";
                                return RedirectToAction("Index");
                            }
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
                TempData["SuccessErrorMessage"] = "Skills Channel edits were not saved";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        private async Task SetEditIRFProperties(IRFVm model)
        {
            string prof = HttpContext.Session.GetString("profileName");
            if (prof != null && prof.Contains("Client"))
            {
                ViewBag.IsClientUser = true;
            }
            else
            {
                ViewBag.IsClientUser = false;
            }
            if (model.InterviewerCollection != null)
            {
                foreach (var item in model.InterviewerCollection)
                {
                    if (item.PerSuccessRate == 0)
                    {
                        item.PerSuccessRate = model.PerMintuteRate != 0.0 ? Convert.ToInt32(model.PerMintuteRate) : 1000;
                    }

                }
            }

            var AssessmentsGroups = _interviewerService.FetchAssessmentGroups();
            var interviewers = await _irfService.GetInterviewersForIRF(model.AssessmentGroupId, model.id);
            //var interviewers = await _irfService.GetAvailableAssessmentGroupInterviewerMembers(model.AssessmentGroupId);

            var _assessmentid = Convert.ToInt32(model.AssessmentGroupId);
            var _listassessment = AssessmentsGroups.Where(x => x.id == _assessmentid).ToList();
            AssessmentsGroups = AssessmentsGroups.Where(x => x.IsRelevant == true).ToList();
            if (AssessmentsGroups.Where(x => x.id == _assessmentid).Count() == 0)
            {
                AssessmentsGroups = AssessmentsGroups.Union(_listassessment);
            }

            ViewBag.Clients = new SelectList(await _irfRepository.GetClients(), "id", "Name");
            ViewBag.Level = new SelectList(await _irfRepository.GetLevels(), "id", "Name");
            ViewBag.LevelTech = new SelectList(await _irfRepository.GetLevelTech(), "id", "Name");

            var skillset = await _irfRepository.GetSkills();
            if (skillset != null)
            {
                skillset = skillset.OrderBy(x => !string.IsNullOrEmpty(x.Name) ? x.Name.Trim() : "").ToList();
            }
            ViewBag.Skill = new SelectList(skillset, "id", "Name");
            if (!model.IsClone)
            {
                ViewBag.ClientUser = new SelectList(_irfService.GetClientUser(model.ClientId), "id", "EmailId");
            }
            else
            {
                model.ClientCollection = null;
                model.ClientId = 0;
            }

            ViewBag.ClientRole = new SelectList(_irfRepository.GetClientRoles(), "id", "Name");
            ViewBag.Recruiters = new SelectList(_irfRepository.GetRecruiters(), "UserId", "EmailId");
            ViewBag.Interviewers = new SelectList(interviewers, "UserId", "EmailId");
            ViewBag.QualityController = new SelectList(interviewers, "UserId", "EmailId");
            ViewBag.EmergencyInterviewers = new SelectList(interviewers, "UserId", "EmailId");
            ViewBag.HRs = new SelectList(_irfRepository.GetHRs(), "UserId", "EmailId");
            ViewBag.ChannelOwnerOptions = new SelectList(_irfService.GetEligibleChannelOwners(), "id", "EmailId");
            ViewBag.ResumeEvaluators = new SelectList(_irfRepository.GetResumeEvaluators(), "UserId", "EmailId");


            //ViewBag.RiseBirdHR = new SelectList(_irfService.GetRiseBirdHR(), "id", "EmailId");
            //ViewBag.RiseBirdHR = new SelectList(_irfService.GetAllHRs(), "id", "EmailId");
            var jobLevels = from JobLevels s in Enum.GetValues(typeof(JobLevels))
                            select new { ID = (int)s, Name = s.GetDescription() };
            ViewBag.JobLevelList = new SelectList(jobLevels, "ID", "Name");
            var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                             select new { ID = (int)s, Name = s.GetDescription() };
            ViewBag.Heuristicsscorelist = new SelectList(Heuristics, "ID", "Name");
            model.AssessmentGroupList = new SelectList(AssessmentsGroups, "id", "AssessmentGroup");
            model.JoinabilityTypeList = new SelectList(_irfService.GetJoinabilityMatrix(), "id", "Name");
            model.SkillGeneralTypeList = new SelectList(_irfService.GetGeneralSkillMatrix(), "id", "Name");
            model.SelectabilityTypeList = new SelectList(_irfService.GetSelectabilityMatrix(), "id", "Name");

            var companyTypes = from CompanyType s in Enum.GetValues(typeof(CompanyType))
                               select new { ID = (int)s, Name = s.GetDescription() };
            model.CompanyTypeList = new SelectList(companyTypes, "ID", "Name");
            var priorityTypes = from PriorityLevel s in Enum.GetValues(typeof(PriorityLevel))
                                select new { ID = (int)s, Name = s.GetDescription() };
            model.PriorityLevelList = new SelectList(priorityTypes, "ID", "Name");

            var emailid = HttpContext.Session.GetString("email")?.Trim();
            emailid = emailid.Replace("\"", "");
            model.LoggedEmail = emailid;
            model.UserId = _irfService.GetUserId(emailid);
            if (string.IsNullOrEmpty(model.Type))
            {
                model.Type = "B2B";
            }
            if (string.IsNullOrEmpty(model.AssessmentGroupId))
            {
                model.InterviewerMembers = await _irfService.GetIRFInterviewerMembers(model.id);
                model.InterviewerCollection = new List<TransInterviewer>();
                model.EmergencyInterviewerCollection = new List<TransEmergencyInterviewer>();
                model.QualityController = new List<TransQualityController>();
            }
            else
            {
                model.InterviewerMembers = new List<IRFInterviewerMembers>();
            }

            //TempData["EditIRFId"] = model.IRFId;
            //model.IsClone = isClone;
            var interviewduration = from InterviewDurationEnum s in Enum.GetValues(typeof(InterviewDurationEnum))
                                    select new { ID = (int)s, Name = s.GetDescription() };
            model.InterviewDurationList = new SelectList(interviewduration, "ID", "Name");
            return;
        }

        [Authorize]
        [HttpGet]
        public IActionResult Stats(string id, string irfId)
        {
            TempData["id"] = id;
            TempData["irfId"] = irfId;
            TempData.Keep();
            ViewBag.IRF = irfId;
            return View();

        }

        [HttpGet]
        public async Task<IActionResult> AddReport(int interviewId, int irfId, bool IsSelected = false)
        {
            try
            {
                string userMenus = HttpContext.Session.GetString("userMenus");

                if (userMenus == null)
                {
                    return RedirectToAction("Index", "Home", new { url = "AddReport/IRF/" + interviewId + "/" + irfId });
                }

                string prof = HttpContext.Session.GetString("profileName");
                if (prof != null)
                {
                    prof = prof.Replace("\"", "");
                }

                var model = await _meetingService.GetInteviewReport(interviewId, irfId);

                if (model == null)
                {
                    return RedirectToAction("Index");
                }
                ViewBag.IsSelected = IsSelected;
                if (model.QuestionsCollection == null)
                {
                    ViewBag.QuestionsDiscussedCount = 0;
                }
                else
                {
                    ViewBag.QuestionsDiscussedCount = model.QuestionsCollection?.Count();
                }
                ViewBag.profileName = prof.ToLower();

                await GetStatusList(model, irfId);
                ViewBag.PageStatus = "Mainurl";
                if (model.IsProctor)
                {
                    return PartialView("ProctorReportPartial", model);
                }
                else
                {
                    return PartialView("AddReportPartial", model);
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
        [DisableRequestSizeLimit]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReport(InterviewReport model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.PageStatus = "Mainurl";
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        if (model.AudioFile != null)
                        {
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.AudioFile);
                            if (fileMimeType == "audio/basic" || fileMimeType == "audio/mpeg"
                            || fileMimeType == "audio/x-wav")
                            {
                                await _azureStorageHelper.InsertBlob(model.AudioFile.FileName, "audios", model.AudioFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("audios");
                                var blob = containerRef.GetBlockBlobReference(model.AudioFile.FileName);
                                string uploadedaudioFileURL = blob.Uri.ToString();
                                if (uploadedaudioFileURL == null)
                                {
                                    ModelState.AddModelError(string.Empty, "Audio Upload is failed.");
                                    await GetStatusList(model);
                                    return View("AddReport", model);
                                }
                                else
                                {
                                    model.AudioLink = uploadedaudioFileURL;
                                }
                            }


                        }
                        if (model.VideoFile == null && model.PageVideoLink == null && model.VideoLink == null)
                        {
                            ModelState.AddModelError(string.Empty, "Video file/link not found.");
                            await GetStatusList(model);
                            return View("AddReport", model);
                        }

                        if (model.VideoFile != null)
                        {
                            // byte[] videofileData = new byte[model.VideoFile.Length];
                            // string uploadedvideoFileURL = _azureStorageHelper.InsertBlob(model.VideoFile.FileName, "videos", videofileData);
                            //await _azureStorageHelper.InsertBlob(model.VideoFile.FileName, "videos", model.VideoFile);
                            //var containerRef = _azureStorageHelper.GetCloudBlobContainer("videos");
                            //var blob = containerRef.GetBlockBlobReference(model.VideoFile.FileName);
                            //string uploadedvideoFileURL = blob.Uri.ToString();
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.VideoFile);
                            if (fileMimeType == "video/mpeg" || fileMimeType == "video/mp4"
                            || fileMimeType == "video/quicktime")
                            {
                                AzureCall _call = new AzureCall(_configuration);
                                //var resulttype = _call.CallApi(model.VideoFile, model.VideoFile.FileName, model.VideoFile.Length);
                                var resulttype = _call.CallApiUpload(model.VideoFile);
                                resulttype = resulttype.Replace("\"", "");
                                var uploadedvideoFileURL = resulttype;
                                if (uploadedvideoFileURL == null)
                                {
                                    ModelState.AddModelError(string.Empty, "Video Upload is failed.");
                                    await GetStatusList(model);
                                    return View("AddReport", model);
                                }
                                else
                                {
                                    model.VideoLink = uploadedvideoFileURL;
                                }
                            }
                        }


                        else if (model.PageVideoLink != null)
                        {
                            model.VideoLink = model.PageVideoLink;
                        }
                        if (_meetingService.AddInterviewReport(model, emailid).Result)
                        {
                            TempData["SuccessErrorMessage"] = "Report has been saved Successfully!";
                            return RedirectToAction("AddReport", new { interviewId = model.InterviewId, irfId = model.IRFId });
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Report was not saved.";
                            return RedirectToAction("Index");
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
                TempData["SuccessErrorMessage"] = "Report was not saved.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        private async Task GetStatusList(InterviewReport model, int irfId = 0)
        {
            var interviewStatuses = from InterviewStatus s in Enum.GetValues(typeof(InterviewStatus))
                                    select new { ID = (int)s, Name = s.GetDescription() };
            model.InterviewStatusList = new SelectList(interviewStatuses, "ID", "Name");

            var meetingStatuses = from MeetingStatus s in Enum.GetValues(typeof(MeetingStatus))
                                  select new { ID = (int)s, Name = s.GetDescription() };

            model.MeetingStatusList = new SelectList(meetingStatuses, "ID", "Name");

            model.Level = new SelectList(await _irfRepository.GetLevels(), "id", "Name");
            model.LevelTech = new SelectList(await _irfRepository.GetLevelTech(), "id", "Name");
            var questionsLevel = from QuestionsLevel s in Enum.GetValues(typeof(QuestionsLevel))
                                 select new { ID = (int)s, Name = s.GetDescription() };
            model.QuestionsLevelList = new SelectList(questionsLevel, "ID", "Name");
            var confirmationTypes = from ConfirmationStatus s in Enum.GetValues(typeof(ConfirmationStatus))
                                    select new { ID = (int)s, Name = s.GetDescription() };
            model.ConfirmationTypeList = new SelectList(confirmationTypes, "ID", "Name");
            model.QuestionsList = new SelectList(await _meetingService.GetTechSkillComments(irfId), "Comment", "Comment");

        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> NewChart()
        {
            try
            {
                string id = TempData.Peek("id").ToString();
                string irfId = TempData.Peek("irfId").ToString();
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        var meetings = await _irfService.GetIRFInterviewMeetings(Convert.ToInt32(id), profileName, emailid);
                        var noShowCount = meetings.Where(x => x.MeetingStatus.HasValue && x.MeetingStatus.Value == 1).Count();
                        var ScheduledCount = meetings.Where(x => !x.MeetingStatus.HasValue && x.interviewDate >= DateTime.Now && x.confirmationStatus == "1").Count();
                        var DroppedCount = meetings.Where(x => x.MeetingStatus.HasValue && x.MeetingStatus.Value == 3).Count();
                        var CompletedCount = meetings.Where(x => x.MeetingStatus.HasValue && x.MeetingStatus.Value == 4).Count();
                        ViewBag.IRF = irfId;
                        ViewBag.IRFintId = id;

                        List<object> iData = new List<object>();

                        DataTable dt = new DataTable();
                        dt.Columns.Add("Meeting Status", System.Type.GetType("System.string"));
                        dt.Columns.Add("Count", System.Type.GetType("System.Int32"));

                        DataRow dr = dt.NewRow();
                        dr["Meeting Status"] = "No Show";
                        dr["Count"] = noShowCount;
                        dt.Rows.Add(dr);

                        dr = dt.NewRow();
                        dr["Meeting Status"] = "Scheduled";
                        dr["Count"] = ScheduledCount;
                        dt.Rows.Add(dr);

                        dr = dt.NewRow();
                        dr["Meeting Status"] = "Dropped";
                        dr["Count"] = DroppedCount;
                        dt.Rows.Add(dr);

                        dr = dt.NewRow();
                        dr["Meeting Status"] = "Completed";
                        dr["Count"] = CompletedCount;
                        dt.Rows.Add(dr);
                        //Looping and extracting each DataColumn to List<Object>  
                        foreach (DataColumn dc in dt.Columns)
                        {
                            List<object> x = new List<object>();
                            x = (from DataRow drr in dt.Rows select drr[dc.ColumnName]).ToList();
                            iData.Add(x);
                        }
                        //Source data returned as JSON  
                        return Json(iData);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Account");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Account");
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
        public async Task<IActionResult> NewChartSelected()
        {
            try
            {
                string id = TempData.Peek("id").ToString();
                string irfId = TempData.Peek("irfId").ToString();
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        var selectedMeeting = new List<MeetingVM>();
                        var meetings = await _irfService.GetIRFInterviewMeetings(Convert.ToInt32(id), profileName, emailid);

                        var ProxyCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 1).Count();
                        var SelectedCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 2).Count();
                        var RejectedCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 3).Count();
                        var BorderLineCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 4).Count();



                        ViewBag.IRF = irfId;
                        ViewBag.IRFintId = id;
                        List<object> iData = new List<object>();
                        //Creating sample data  
                        DataTable dt = new DataTable();
                        dt.Columns.Add("Interview Status", System.Type.GetType("System.string"));
                        dt.Columns.Add("Count", System.Type.GetType("System.Int32"));

                        DataRow dr = dt.NewRow();
                        dr["Interview Status"] = "Proxy";
                        dr["Count"] = ProxyCount;
                        dt.Rows.Add(dr);

                        dr = dt.NewRow();
                        dr["Interview Status"] = "Selected";
                        dr["Count"] = SelectedCount;
                        dt.Rows.Add(dr);

                        dr = dt.NewRow();
                        dr["Interview Status"] = "Rejected";
                        dr["Count"] = RejectedCount;
                        dt.Rows.Add(dr);

                        dr = dt.NewRow();
                        dr["Interview Status"] = "BorderLine";
                        dr["Count"] = BorderLineCount;
                        dt.Rows.Add(dr);
                        //Looping and extracting each DataColumn to List<Object>  
                        foreach (DataColumn dc in dt.Columns)
                        {
                            List<object> x = new List<object>();
                            x = (from DataRow drr in dt.Rows select drr[dc.ColumnName]).ToList();
                            iData.Add(x);
                        }
                        //Source data returned as JSON  
                        return Json(iData);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Account");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Account");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public SelectList GetClientUser(int clientId)
        {
            var list = new List<SelectListItem>();
            var clientUsers = _irfService.GetClientUser(clientId);
            if (clientUsers != null && clientUsers.Count() > 0)
            {
                return new SelectList(clientUsers, "id", "EmailId");
            }
            else
                list.Add(new SelectListItem { Text = "No Client User", Value = "0" });
            //clientUsers.ToList().Add(new Users { id = 0, FirstName = "No Client User" });
            return new SelectList(list, "Value", "Text");
        }


        //NewChartClientsData
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> NewChartClientMeetingData()
        {
            try
            {
                string id = TempData.Peek("id").ToString();
                string irfId = TempData.Peek("irfId").ToString();
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        var selectedMeeting = new List<MeetingVM>();
                        var meetings = await _irfService.GetIRFInterviewMeetings(Convert.ToInt32(id), profileName, emailid);
                        var clientList = new List<string>();
                        var chartData = new List<ClientChartData>();
                        var clientUserList = _irfRepository.GetTransClientUser(Convert.ToInt32(id));
                        foreach (var item in clientUserList)
                        {
                            if (item.ClientRoleId == 1 || item.ClientRoleId == 4)
                            {
                                clientList.Add(item.EmailId);

                            }

                        }
                        foreach (var item in clientList)
                        {
                            var obj = new ClientChartData
                            {
                                Name = item,
                                NoShowCount = meetings.Where(x => x.MeetingStatus.HasValue && x.MeetingStatus.Value == 1 && (x.ClientHrContact?.Trim() == item || x.staffingAgency?.Trim() == item)).Count(),
                                ScheduledCount = meetings.Where(x => !x.MeetingStatus.HasValue && x.interviewDate >= DateTime.Now && x.confirmationStatus == "1" && (x.ClientHrContact?.Trim() == item || x.staffingAgency?.Trim() == item)).Count(),
                                CompletedCount = meetings.Where(x => x.MeetingStatus.HasValue && x.MeetingStatus.Value == 4 && (x.ClientHrContact?.Trim() == item || x.staffingAgency?.Trim() == item)).Count(),
                                DroppedCount = meetings.Where(x => x.MeetingStatus.HasValue && x.MeetingStatus.Value == 3 && (x.ClientHrContact?.Trim() == item || x.staffingAgency?.Trim() == item)).Count()
                            };
                            chartData.Add(obj);
                        }


                        ViewBag.IRF = irfId;
                        ViewBag.IRFintId = id;

                        return Json(chartData);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Account");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Account");
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
        public async Task<IActionResult> NewChartClientFeedbackData()
        {
            try
            {
                string id = TempData.Peek("id").ToString();
                string irfId = TempData.Peek("irfId").ToString();
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        var selectedMeeting = new List<MeetingVM>();
                        var meetings = await _irfService.GetIRFInterviewMeetings(Convert.ToInt32(id), profileName, emailid);
                        var clientList = new List<string>();
                        var chartData = new List<ClientChartFeedbackData>();
                        var clientUserList = _irfRepository.GetTransClientUser(Convert.ToInt32(id));
                        foreach (var item in clientUserList)
                        {
                            if (item.ClientRoleId == 1 || item.ClientRoleId == 4)
                            {
                                clientList.Add(item.EmailId);

                            }

                        }
                        foreach (var item in clientList)
                        {
                            var obj = new ClientChartFeedbackData
                            {
                                Name = item,
                                ProxyCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 1 && (x.ClientHrContact?.Trim() == item || x.staffingAgency?.Trim() == item)).Count(),
                                SelectedCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 2 && (x.ClientHrContact?.Trim() == item || x.staffingAgency?.Trim() == item)).Count()
                            };
                            ;
                            obj.RejectedCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 3 && (x.ClientHrContact?.Trim() == item || x.staffingAgency?.Trim() == item)).Count();
                            obj.BorderLineCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 4 && (x.ClientHrContact?.Trim() == item || x.staffingAgency?.Trim() == item)).Count();
                            chartData.Add(obj);
                        }


                        ViewBag.IRF = irfId;
                        ViewBag.IRFintId = id;

                        return Json(chartData);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Account");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Account");
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
        public async Task<IActionResult> BudgetConsumedChart()
        {
            try
            {
                string id = TempData.Peek("id").ToString();
                string irfId = TempData.Peek("irfId").ToString();
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        var selectedMeeting = new List<MeetingVM>();
                        var meetings = await _irfService.GetIRFInterviewMeetings(Convert.ToInt32(id), profileName, emailid);
                        meetings = meetings.Where(x => x.MeetingStatus != null && x.MeetingStatus == 4).ToList();
                        var chartData = new List<MeetingBudgetChart>();

                        foreach (var item in meetings)
                        {
                            if (chartData != null && chartData.Where(x => x.Date == item.interviewDate).Count() > 0)
                            {
                                chartData.Where(x => x.Date == item.interviewDate).FirstOrDefault().Cost += item.PerMintuteRate;
                            }
                            else
                            {
                                var obj = new MeetingBudgetChart
                                {
                                    Date = item.interviewDate,
                                    Cost = item.PerMintuteRate
                                };
                                chartData.Add(obj);
                            }

                        }


                        return Json(chartData);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Account");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Account");
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
        public async Task<IActionResult> FunnelChart()
        {
            try
            {
                string irfId = TempData.Peek("id").ToString();
                string irfName = TempData.Peek("irfId").ToString();
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {

                        var chartData = await _irfService.GetFunnelData(Convert.ToInt32(irfId), irfName);
                        return Json(chartData);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Account");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Account");
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
        public async Task<IActionResult> Candidates(string id, string irfName, int? Page, string searchValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");

                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        ViewBag.IRFName = irfName;
                        ViewBag.IRFId = id;
                        ViewBag.profile = profileName;

                        var meetings = (await _irfService.GetIRFInterviewMeetings(Convert.ToInt32(id), profileName, emailid))?.ToList();
                        var candidateList = (await _irfService.GetIRFCandidates(Convert.ToInt32(id)))?.ToList();
                        if (profileName == "Recruiter")
                        {
                            var userId = _irfService.GetUserId(emailid, profileName);
                            candidateList = candidateList.Where(x => x.AddedByUserId == userId).ToList();
                        }

                        if (candidateList != null && candidateList.Count > 0)
                        {

                            candidateList.ForEach(x => x.InviteConfirmation = meetings.Where(y => Convert.ToInt32(y.confirmationStatus) == 1 && y.candidateEmail == x.EmailId).ToList().Count);
                            candidateList.Where(x => x.Status != null).ToList().ForEach(x => x.StatusValue = ((CandidateStatusOptions)x.Status).GetDescription());
                            candidateList.ForEach(x => { x.confirmationStatusDesc = ((ReceiveMeetingStatus)x.confirmationStatus).GetDescription(); x.MeetingStatusDesc = ((MeetingStatus)x.MeetingStatus).GetDescription(); x.InterviewStatusDesc = ((InterviewStatus)x.InterviewStatus).GetDescription(); x.HeuristicsscoreDesc = ((Heuristicsscorelevel)x.Heuristicsscore).GetDescription(); });
                        }

                        if (TempData["SuccessErrorMessageCandidate"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageCandidate"].ToString();
                            TempData.Remove("SuccessErrorMessageCandidate");
                        }

                        if (!string.IsNullOrEmpty(searchValue))
                        {
                            searchValue = searchValue.ToUpper().Trim();
                            candidateList = candidateList.Where(x => (!string.IsNullOrEmpty(x.Name) ? x.Name.ToUpper().Contains(searchValue) : x.Name == "") || (!string.IsNullOrEmpty(x.EmailId) ? x.EmailId.ToUpper().Contains(searchValue) : x.EmailId == "")).ToList();

                        }
                        ViewBag.TotalCount = candidateList.Count;

                        ViewBag.PageStartCount = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                        int a = candidateList.Count / 10;
                        if (candidateList.Count % 10 == 0)
                        {
                            ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                        }
                        else
                        {
                            a = a + 1;
                            if (a == Page)
                            {
                                ViewBag.PageEndCount = candidateList.Count;
                            }
                            else
                            {
                                ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));

                            }

                        }
                        if (candidateList.Count == 0)
                        {
                            ViewBag.PageStartCount = 0;
                            ViewBag.PageEndCount = 0;
                        }
                        _modelDetails = candidateList;
                        return View(candidateList.ToPagedList(Page ?? 1, 10));



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

        //[Authorize]
        //[HttpGet]
        //public async Task<FileContentResult> ExportCandidateList(string irfId, string irfName)
        //{
        //    ExcelPackage Ep = new ExcelPackage();
        //    ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Candidate Report");
        //    string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_" + irfName + "_Candidate_Report" + ".xlsx";
        //    try
        //    {
        //        if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
        //        {
        //            var profileName = HttpContext.Session.GetString("profileName")?.Trim();
        //            profileName = profileName.Replace("\"", "");
        //            var emailid = HttpContext.Session.GetString("email")?.Trim();
        //            emailid = emailid.Replace("\"", "");
        //            if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
        //            {
        //                var meetings = (await _irfService.GetIRFInterviewMeetings(Convert.ToInt32(irfId), profileName, emailid))?.ToList();
        //                var candidateList = (await _irfService.GetIRFCandidates(Convert.ToInt32(irfId)))?.ToList();
        //                if (candidateList != null && candidateList.Count > 0)
        //                {
        //                    candidateList.Where(x => x.Status != null).ToList().ForEach(x => x.StatusValue = ((CandidateStatusOptions)x.Status).GetDescription());
        //                    candidateList.ForEach(x => x.InviteConfirmation = meetings.Where(y => Convert.ToInt32(y.confirmationStatus) == 1 && y.candidateEmail == x.EmailId).ToList().Count);
        //                    candidateList.ForEach(x => { x.confirmationStatusDesc = ((ReceiveMeetingStatus)x.confirmationStatus).GetDescription(); x.MeetingStatusDesc = ((MeetingStatus)x.MeetingStatus).GetDescription(); x.InterviewStatusDesc = ((InterviewStatus)x.InterviewStatus).GetDescription(); });

        //                }

        //                string userEmailId = string.Empty;

        //                Sheet.Cells["A1:Z1"].Style.Font.Bold = true;
        //                Sheet.Cells["A1:Z1"].Style.Font.Size = 12;
        //                Sheet.Cells["A1:Z1"].Style.WrapText = true;
        //                //Sheet.Column(1).Width = 30;
        //                //Sheet.Column(2).Width = 30;
        //                int startColumn = 1; int endColumn = 25;
        //                for (int a = startColumn; a <= endColumn; a++)
        //                {
        //                    Sheet.Column(a).Width = 30;
        //                }
        //                Sheet.Cells[1, 1].Value = "Candidate Name";
        //                Sheet.Cells[1, 2].Value = "Candidate Email";
        //                Sheet.Cells[1, 3].Value = "Resume";
        //                Sheet.Cells[1, 4].Value = "Type";
        //                Sheet.Cells[1, 5].Value = "Location";
        //                Sheet.Cells[1, 6].Value = "Skill";
        //                Sheet.Cells[1, 7].Value = "Work Auth";
        //                Sheet.Cells[1, 8].Value = "Current Location";
        //                Sheet.Cells[1, 9].Value = "Current Employer";
        //                Sheet.Cells[1, 10].Value = "Phone";
        //                Sheet.Cells[1, 11].Value = "Total Experience";
        //                Sheet.Cells[1, 12].Value = "Current Salary";
        //                Sheet.Cells[1, 13].Value = "Expected Salary";
        //                Sheet.Cells[1, 14].Value = "Notice";
        //                Sheet.Cells[1, 15].Value = "Invites Sent";
        //                Sheet.Cells[1, 16].Value = "Invite Confirmation";
        //                Sheet.Cells[1, 17].Value = "Schedule Status";
        //                Sheet.Cells[1, 18].Value = "Confirmation Status";
        //                Sheet.Cells[1, 19].Value = "Meeting Status";
        //                Sheet.Cells[1, 20].Value = "Interview Status";
        //                Sheet.Cells[1, 21].Value = "Interview Date";
        //                Sheet.Cells[1, 22].Value = "Video Link";

        //                int i = 2;
        //                //string LastAction = null; ;

        //                if (candidateList.Count() > 0)
        //                {
        //                    foreach (var item in candidateList)
        //                    {
        //                        Sheet.Cells[i, 1].Value = item.Name;
        //                        Sheet.Cells[i, 2].Value = item.EmailId;
        //                        if (!string.IsNullOrEmpty(item.ResumeLink))
        //                        {
        //                            try
        //                            {
        //                                Sheet.Cells[i, 3].Hyperlink = new Uri(item.ResumeLink, UriKind.Absolute);
        //                                Sheet.Cells[i, 3].Value = "Resume Link";
        //                                Sheet.Cells[i, 3].Style.Font.UnderLine = true;
        //                                Sheet.Cells[i, 3].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
        //                            }
        //                            catch (Exception)
        //                            {
        //                                Sheet.Cells[i, 3].Value = "No Resume";
        //                            }
        //                        }
        //                        else
        //                        {
        //                            Sheet.Cells[i, 3].Value = "No Resume";
        //                        }
        //                        Sheet.Cells[i, 4].Value = item.Type ?? "NA";
        //                        Sheet.Cells[i, 5].Value = item.Location ?? "NA";

        //                        Sheet.Cells[i, 6].Value = item.Skill ?? "NA";

        //                        Sheet.Cells[i, 7].Value = item.WorkAuth ?? "NA";

        //                        Sheet.Cells[i, 8].Value = item.CurrentLocation ?? "NA";

        //                        Sheet.Cells[i, 9].Value = item.CurrentEmployer ?? "NA";

        //                        Sheet.Cells[i, 10].Value = item.Phone ?? "NA";

        //                        Sheet.Cells[i, 11].Value = Convert.ToString(item.TotalExperience);//item.TotalExperience != 0 ? Convert.ToString(item.TotalExperience) : "NA";
        //                        Sheet.Cells[i, 12].Value = Convert.ToString(item.CurrentSalary);//item.CurrentSalary != 0 ? Convert.ToString(item.CurrentSalary) : "NA";

        //                        Sheet.Cells[i, 13].Value = Convert.ToString(item.ExpectedSalary);// item.ExpectedSalary != 0 ? Convert.ToString(item.ExpectedSalary) : "NA";
        //                        Sheet.Cells[i, 14].Value = Convert.ToString(item.Notice);// item.Notice != 0 ? Convert.ToString(item.Notice) : "NA";

        //                        Sheet.Cells[i, 15].Value = item.TotalInvites;
        //                        Sheet.Cells[i, 16].Value = item.InviteConfirmation;
        //                        Sheet.Cells[i, 17].Value = item.InviteConfirmation > 0 ? "Scheduled" : item.StatusValue;
        //                        Sheet.Cells[i, 18].Value = item.confirmationStatusDesc != "0" ? item.confirmationStatusDesc : "NA";
        //                        Sheet.Cells[i, 19].Value = item.MeetingStatusDesc != "0" ? item.MeetingStatusDesc : "NA";
        //                        Sheet.Cells[i, 20].Value = item.InterviewStatusDesc != "0" ? item.InterviewStatusDesc : "NA";
        //                        Sheet.Cells[i, 21].Value = item.interviewDate != null ? item.interviewDate?.ToString("dd MMMM yyyy") : "NA";
        //                        if (!string.IsNullOrEmpty(item.VideoLink))
        //                        {
        //                            try
        //                            {
        //                                Sheet.Cells[i, 22].Hyperlink = new Uri(item.VideoLink, UriKind.Absolute);
        //                                Sheet.Cells[i, 22].Value = "Video Link";
        //                                Sheet.Cells[i, 22].Style.Font.UnderLine = true;
        //                                Sheet.Cells[i, 22].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
        //                            }
        //                            catch (Exception)
        //                            {
        //                                Sheet.Cells[i, 22].Value = "No Video";
        //                            }
        //                        }
        //                        else
        //                        {
        //                            Sheet.Cells[i, 22].Value = "No Video";
        //                        }
        //                        i++;
        //                    }
        //                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        //                }
        //                else
        //                {
        //                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
        //                    Rng.Value = "No data available in table !!!";
        //                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        //                }

        //            }
        //            else
        //            {
        //                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
        //                Rng.Value = "Data export failed !!!";
        //                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        //            }
        //        }
        //        else
        //        {
        //            ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
        //            Rng.Value = "Data export failed !!!";
        //            return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
        //        Rng.Value = "Data export failed !!!";
        //        return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        //    }

        //}

        //[Authorize]
        //[HttpGet]
        //public IActionResult AddCandidate(string irfId, string irfName)
        //{
        //    if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
        //    {
        //        var profileName = HttpContext.Session.GetString("profileName")?.Trim();
        //        profileName = profileName.Replace("\"", "");
        //        var emailid = HttpContext.Session.GetString("email")?.Trim();
        //        emailid = emailid.Replace("\"", "");
        //        if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter")
        //        {
        //            var model = new Candidate
        //            {
        //                IRFId = Convert.ToInt32(irfId),
        //                IRFName = irfName
        //            };
        //            var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
        //                             select new { ID = (int)s, Name = s.GetDescription() };
        //            model.Heuristicsscorelist = new SelectList(Heuristics, "ID", "Name");
        //            return View(model);
        //        }
        //        else
        //        {
        //            return RedirectToAction("SignOut", "Session");

        //        }
        //    }
        //    else
        //    {
        //        return RedirectToAction("SignOut", "Session");
        //    }
        //}

        //[Authorize]
        //[HttpPost]
        //public async Task<IActionResult> AddCandidate(Candidate model)
        //{
        //    try
        //    {
        //        if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
        //        {
        //            var profileName = HttpContext.Session.GetString("profileName")?.Trim();
        //            profileName = profileName.Replace("\"", "");
        //            var emailid = HttpContext.Session.GetString("email")?.Trim();
        //            emailid = emailid.Replace("\"", "");
        //            if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter")
        //            {
        //                var result = _irfService.AddCandidate(model, emailid, profileName).Result;
        //                TempData["SuccessErrorMessageCandidate"] = result;
        //                return RedirectToAction("Candidates", new { id = model.IRFId, irfName = model.IRFName });

        //            }
        //            else
        //            {
        //                return RedirectToAction("SignOut", "Session");
        //            }


        //        }
        //        else
        //        {
        //            return RedirectToAction("SignOut", "Session");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        TempData["SuccessErrorMessage"] = "Adding candidate failed";
        //        return RedirectToAction("Index");
        //    }
        //}

        //public ActionResult ImportCandidates(IFormCollection formCollection, string irfId, string IRFName)
        //{

        //    try
        //    {
        //        if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
        //        {
        //            var profileName = HttpContext.Session.GetString("profileName")?.Trim();
        //            profileName = profileName.Replace("\"", "");
        //            var emailid = HttpContext.Session.GetString("email")?.Trim();
        //            emailid = emailid.Replace("\"", "");

        //            if (Request != null)
        //            {
        //                var file = formCollection.Files[0];
        //                var IRFId = Convert.ToInt32((formCollection["IRFId"].ToString()));
        //                if ((file != null) && (file.Length > 0) && !string.IsNullOrEmpty(file.FileName))
        //                {

        //                    string fileName = file.FileName;
        //                    string fileContentType = file.ContentType;
        //                    byte[] fileBytes = new byte[file.Length];
        //                    var data = file.OpenReadStream();
        //                    var candidateList = new List<Candidate>();
        //                    using (var package = new ExcelPackage(file.OpenReadStream()))
        //                    {
        //                        var currentSheet = package.Workbook.Worksheets;
        //                        var workSheet = currentSheet.First();
        //                        var noOfCol = workSheet.Dimension.End.Column;
        //                        var noOfRow = workSheet.Dimension.End.Row;
        //                        for (int rowIterator = 2; rowIterator <= noOfRow; rowIterator++)
        //                        {
        //                            var candidate = new Candidate
        //                            {
        //                                Name = (workSheet.Cells[rowIterator, 1] != null) ? workSheet.Cells[rowIterator, 1].Value.ToString() : "",
        //                                EmailId = workSheet.Cells[rowIterator, 2].Value.ToString(),
        //                                ResumeLink = (workSheet.Cells[rowIterator, 3].Value != null) ? workSheet.Cells[rowIterator, 3].Value.ToString() : "",         //workSheet.Cells[rowIterator, 3].Value.ToString();
        //                                Type = (workSheet.Cells[rowIterator, 4].Value != null) ? workSheet.Cells[rowIterator, 4].Value.ToString() : "", //workSheet.Cells[rowIterator, 4].Value.ToString();
        //                                Location = (workSheet.Cells[rowIterator, 5].Value != null) ? workSheet.Cells[rowIterator, 5].Value.ToString() : "",            //workSheet.Cells[rowIterator, 5].Value.ToString();
        //                                Skill = (workSheet.Cells[rowIterator, 6].Value != null) ? workSheet.Cells[rowIterator, 6].Value.ToString() : "",            // workSheet.Cells[rowIterator, 6].Value.ToString();
        //                                WorkAuth = (workSheet.Cells[rowIterator, 7].Value != null) ? workSheet.Cells[rowIterator, 7].Value.ToString() : "",               //workSheet.Cells[rowIterator, 7].Value.ToString();
        //                                CurrentLocation = (workSheet.Cells[rowIterator, 8].Value != null) ? workSheet.Cells[rowIterator, 8].Value.ToString() : "",                 //workSheet.Cells[rowIterator, 8].Value.ToString();
        //                                CurrentEmployer = (workSheet.Cells[rowIterator, 9].Value != null) ? workSheet.Cells[rowIterator, 9].Value.ToString() : "",                 //workSheet.Cells[rowIterator, 9].Value.ToString();
        //                                Phone = (workSheet.Cells[rowIterator, 10].Value != null) ? ("+" + workSheet.Cells[rowIterator, 10].Value.ToString()) : "",                  // "+" + workSheet.Cells[rowIterator, 10].Value.ToString();
        //                                TotalExperience = (workSheet.Cells[rowIterator, 11].Value != null) ? Convert.ToDecimal(workSheet.Cells[rowIterator, 11].Value) : 0,                // Convert.ToDecimal(workSheet.Cells[rowIterator, 11].Value);
        //                                CurrentSalary = (workSheet.Cells[rowIterator, 12].Value != null) ? Convert.ToDecimal(workSheet.Cells[rowIterator, 12].Value) : 0,                  // Convert.ToDecimal(workSheet.Cells[rowIterator, 12].Value);
        //                                ExpectedSalary = (workSheet.Cells[rowIterator, 13].Value != null) ? Convert.ToDecimal(workSheet.Cells[rowIterator, 13].Value) : 0,                //Convert.ToDecimal(workSheet.Cells[rowIterator, 13].Value);
        //                                Notice = (workSheet.Cells[rowIterator, 14].Value != null) ? Convert.ToDecimal(workSheet.Cells[rowIterator, 14].Value) : 0,                   //Convert.ToDecimal(workSheet.Cells[rowIterator, 14].Value);
        //                                //Add template column EncodedResumeLink
        //                                IRFId = IRFId
        //                            };
        //                            candidateList.Add(candidate);
        //                        }

        //                    }
        //                    var result = _irfService.AddMultipleCandidates(candidateList, emailid, profileName).Result;
        //                    TempData["SuccessErrorMessageCandidate"] = result;
        //                    return RedirectToAction("Candidates", new { id = IRFId, irfName = IRFName });

        //                }

        //                else
        //                {
        //                    TempData["SuccessErrorMessageCandidate"] = "Importing candidates failed!";
        //                    return RedirectToAction("Candidates", new { id = IRFId, irfName = IRFName });
        //                }
        //            }
        //        }


        //    }
        //    catch (Exception ex)
        //    {
        //        _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        TempData["SuccessErrorMessageCandidate"] = "Importing candidates failed!";
        //        return RedirectToAction("Candidates", new { id = Convert.ToInt32(irfId), irfName = IRFName });
        //    }
        //    return null;
        //}

        [HttpGet]
        public IActionResult ShowCalendly(string irfName, string calendlyLink, int type)
        {
            ViewBag.IRFName = irfName;
            ViewBag.CalendlyLink = calendlyLink;
            ViewBag.Pagetype = type;
            return View();
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SendInvite(string irfId, string IRFName, string emailId, DateTime endDate)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var loggedInUserEmail = HttpContext.Session.GetString("email")?.Trim();
                    loggedInUserEmail = loggedInUserEmail.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        var linkurl = "https://" + Request.Host.ToString();
                        if (endDate < DateTime.Now)
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Sorry! Could not send invite! The requested IRF positions have been filled.";
                            return RedirectToAction("Candidates", new { id = Convert.ToInt32(irfId), irfName = IRFName });
                        }
                        if (await _irfService.SendInvite(Convert.ToInt32(irfId), emailId, loggedInUserEmail, linkurl))
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Invitation has been sent Successfully!";
                            return RedirectToAction("Candidates", new { id = Convert.ToInt32(irfId), irfName = IRFName });
                        }
                        else
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Sorry! Could not send invite!";
                            return RedirectToAction("Candidates", new { id = Convert.ToInt32(irfId), irfName = IRFName });
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
                TempData["SuccessErrorMessageCandidate"] = "Sorry! Could not send invite!";
                return RedirectToAction("Candidates", new { id = Convert.ToInt32(irfId), irfName = IRFName });
            }

        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CancelInvite(string irfId, string IRFName, string emailId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {

                        if (await _irfService.CancelInvite(Convert.ToInt32(irfId), emailId))
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Invite has been cancelled Successfully!";
                            return RedirectToAction("Candidates", new { id = Convert.ToInt32(irfId), irfName = IRFName });
                        }
                        else
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Sorry! Could not cancel invite!";
                            return RedirectToAction("Candidates", new { id = Convert.ToInt32(irfId), irfName = IRFName });
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
                TempData["SuccessErrorMessageCandidate"] = "Sorry! Could not send invite!";
                return RedirectToAction("Candidates", new { id = Convert.ToInt32(irfId), irfName = IRFName });
            }

        }

        [Authorize]
        [HttpGet]
        public FileContentResult DownloadTemplate()
        {
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Import Template");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_Import_Template" + ".xlsx";
            try
            {

                Sheet.Cells["A1:P1"].Style.Font.Bold = true;
                Sheet.Cells["A1:P1"].Style.Font.Size = 12;
                Sheet.Cells["A1:P1"].Style.WrapText = true;
                //Sheet.Column(1).Width = 30;
                //Sheet.Column(2).Width = 30;
                int startColumn = 1; int endColumn = 14;
                for (int a = startColumn; a <= endColumn; a++)
                {
                    Sheet.Column(a).Width = 35;
                }
                Sheet.Cells[1, 1].Value = "Name";
                Sheet.Cells[1, 2].Value = "EmailId";
                Sheet.Cells[1, 3].Value = "Resume Link";
                Sheet.Cells[1, 4].Value = "Type(FTE/Contract)";
                Sheet.Cells[1, 5].Value = "Location Submitted For";
                Sheet.Cells[1, 6].Value = "Skill";
                Sheet.Cells[1, 7].Value = "Work Authorization";
                Sheet.Cells[1, 8].Value = "Current Location";
                Sheet.Cells[1, 9].Value = "Current Employer";
                Sheet.Cells[1, 10].Value = "Phone";
                Sheet.Cells[1, 11].Value = "Total Experience";
                Sheet.Cells[1, 12].Value = "Current Salary";
                Sheet.Cells[1, 13].Value = "Expected Salary";
                Sheet.Cells[1, 14].Value = "Notice";


                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);

            }
            catch (Exception)
            {
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Download failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }

        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> InterviewPlanner(int irfId, string irfName)
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
                        var planner = new InterviewPlanner();
                        var meetings = await _irfService.GetIRFInterviewMeetings(Convert.ToInt32(irfId), profileName, emailid);
                        if (meetings != null)
                        {
                            planner.Overall = (meetings != null ? meetings.Count() : 0);
                            planner.RisebirdSelectionTotal = meetings.Where(x => x.InterviewStatus.HasValue && (x.InterviewStatus.Value == 2 || x.InterviewStatus.Value == 4)).Count();//new List<MeetingVM>();
                            planner.TotalInvites = _irfRepository.GetAllIRF(emailid, profileName).Where(x => x.id == irfId).FirstOrDefault().TotalInvites;
                            planner.ConfirmationTotal = meetings.Where(x => x.confirmationStatus != null && x.confirmationStatus == "1").Count();
                            planner.CompletionTotal = meetings.Where(x => x.MeetingStatus.HasValue && x.MeetingStatus == 4).Count();
                            planner.ApprovalTotal = meetings.Where(x => x.IsApproved.HasValue && x.IsApproved == 1).Count();
                            planner.FinalSelectionTotal = meetings.Where(x => x.IsSelected.HasValue && x.IsSelected == 1).Count();
                            planner.OfferAcceptanceTotal = meetings.Where(x => x.OfferStatus.HasValue && x.OfferStatus == 2).Count();
                            planner.JoiningTotal = meetings.Where(x => x.JoiningStatus.HasValue && x.JoiningStatus == 1).Count();

                            if (planner.Overall != 0)
                            {
                                planner.InvitePercentage = ((planner.TotalInvites * 100) / planner.Overall);
                            }
                            if (planner.TotalInvites != 0)
                            {
                                planner.ConfirmationPercentage = ((planner.ConfirmationTotal * 100) / planner.TotalInvites);

                            }

                            if (planner.ConfirmationTotal != 0)
                            {
                                planner.CompletionPercentage = ((planner.CompletionTotal * 100) / planner.ConfirmationTotal);

                            }
                            if (planner.CompletionTotal != 0)
                            {
                                planner.RisebirdSelectionPercentage = ((planner.RisebirdSelectionTotal * 100) / planner.CompletionTotal);

                            }
                            if (planner.RisebirdSelectionTotal != 0)
                            {
                                planner.ApprovalPercentage = ((planner.ApprovalTotal * 100) / planner.RisebirdSelectionTotal);

                            }
                            if (planner.ApprovalTotal != 0)
                            {
                                planner.FinalSelectionPercentage = ((planner.FinalSelectionTotal * 100) / planner.ApprovalTotal);

                            }
                            if (planner.FinalSelectionTotal != 0)
                            {
                                planner.OfferAcceptancePercentage = ((planner.OfferAcceptanceTotal * 100) / planner.FinalSelectionTotal);

                            }
                            if (planner.OfferAcceptanceTotal != 0)
                            {
                                planner.JoiningPercentage = ((planner.JoiningTotal * 100) / planner.OfferAcceptanceTotal);

                            }
                            if (planner.Overall != 0)
                            {
                                planner.OverallPercentage = ((planner.JoiningTotal * 100) / planner.Overall);

                            }
                        }

                        planner = await _irfService.GetInterviewPlanner(irfId);
                        if (planner.ApprovalOwnedBy == null)
                        {
                            planner.JoiningOwnedBy = "Staffing agency";
                            planner.OfferOwnedBy = "Staffing agency";
                            planner.FinalSelectionOwnedBy = "Risebird Interviewer and Delivery Team";
                            planner.ApprovalOwnedBy = "Risebird Interviewer and Delivery Team";
                            planner.RisebirdSelectionOwnedBy = "Staffing agency";
                            planner.CompletionOwnedBy = "Staffing agency and Risebird";
                            planner.ConfirmationOwnedBy = "Staffing agency";
                            planner.InviteOwnedBy = "Staffing agency";
                            planner.JoiningProcessingTime = 14;
                            planner.OfferProcessingTime = 2;
                            planner.FinalSelectionProcessingTime = 4;
                            planner.ApprovalProcessingTime = 1;
                            planner.RisebirdSelectionProcessingTime = 1;
                            planner.CompletionProcessingTime = 2;
                            planner.ConfirmationProcessingTime = 2;
                            planner.InviteProcessingTime = 2;
                        }

                        planner.IRFId = irfId;
                        planner.IRFName = irfName;
                        planner.OverAllProcessingTime = (planner.JoiningProcessingTime + planner.OfferProcessingTime + planner.FinalSelectionProcessingTime + planner.ApprovalProcessingTime
                            + planner.RisebirdSelectionProcessingTime + planner.CompletionProcessingTime + planner.ConfirmationProcessingTime + planner.InviteProcessingTime);



                        return View(planner);
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
        public async Task<IActionResult> Invoice(int irfId, string irfName, DateTime startDate, DateTime endDate, string srNumber, string poNumber, string reqNumber, string currencyCode, string projectCode, bool isGST, string discount, string clientAddress)
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
                        var model = new InvoiceModel();
                        model = await _irfService.GetInvoiceModel(irfId, startDate, endDate, srNumber, poNumber, reqNumber, currencyCode, projectCode, isGST, discount, clientAddress);

                        return PartialView("InvoicePartial", model);
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditInterviewPlanner(InterviewPlanner model)
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

                        if (_irfService.AddUpdateInterviewPlanner(model).Result)
                        {
                            TempData["SuccessErrorMessage"] = "Interview Planner has been updated Successfully!";
                            return RedirectToAction("Index");

                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Sorry! Interview Planner update failed";
                            return RedirectToAction("Index");
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
                TempData["SuccessErrorMessage"] = "Adding IRF has failed";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public IActionResult EnrollVoiceItUser(int candidateId, string candidateEmail, int irfId, string irfName)
        {
            try
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Recruiter")
                {
                    var datamodel = new CandidateAudioVM
                    {
                        IRFName = irfName,
                        IRFId = irfId,
                        CandidateEmail = candidateEmail
                    };
                    var model = (IList<CandidateAudio>)_evaluatorService.GetCandidateAudios(candidateId, irfId).Result.Where(x => x.IsVoiceBiometric == true).ToList();
                    datamodel.CandidateAudios = model.ToList();
                    if (TempData["SuccessErrorMessageEnroll"] != null)
                    {
                        ViewBag.Message = TempData["SuccessErrorMessageEnroll"].ToString();
                        TempData.Remove("SuccessErrorMessageEnroll");
                    }
                    return View(datamodel);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }



            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Candidates", new { id = irfId, irfName = irfName });
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnrollVoiceItUser(IFormCollection model)
        {
            CandidateAudio canObj = new CandidateAudio();
            try
            {
                if (HttpContext.Session.GetString("email") != null &&
                    HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Recruiter")
                    {
                        var count = model.Count();
                        int imax = (count - 1) / 8;
                        var objList = new List<CandidateAudio>();
                        for (int i = 0; i < imax; i++)
                        {
                            var obj = new CandidateAudio
                            {
                                id = Convert.ToInt32(model["CandidateAudios[" + i + "].id"]),
                                IRFId = Convert.ToInt32(model["CandidateAudios[" + i + "].IRFId"]),
                                IRFName = model["CandidateAudios[" + i + "].IRFName"],
                                CandidateId = Convert.ToInt32(model["CandidateAudios[" + i + "].CandidateId"]),
                                IsEnrolled = Convert.ToBoolean(model["CandidateAudios[" + i + "].IsEnrolled"]),
                                AudioFileLink = model["CandidateAudios[" + i + "].AudioFileLink"],
                                PhraseDetail = model["CandidateAudios[" + i + "].PhraseDetail"],
                                //VoiceItUserId
                                VoiceItUserId = model["CandidateAudios[" + i + "].VoiceItUserId"]
                            };
                            objList.Add(obj);
                        }
                        var result = _evaluatorService.EnrollCandidate(objList);
                        canObj = objList.First();
                        TempData["SuccessErrorMessageUploadAudio"] = "Kindly check the table below for enrollment status of each audio.";
                        return RedirectToAction("UploadAudio", new { candidateId = canObj.CandidateId, irfId = canObj.IRFId, rfName = canObj.IRFName });

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
                TempData["SuccessErrorMessageUploadAudio"] = "Enrollment Failed!";
                return RedirectToAction("UploadAudio", new { candidateId = canObj.CandidateId, irfId = canObj.IRFId, rfName = canObj.IRFName });

            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> PauseIRF(int irfId, string irfName)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        if (await _irfService.PauseIRF(irfId, irfName))
                        {
                            TempData["SuccessErrorMessage"] = "IRF has been paused Successfully!";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Failed to pause IRF!";
                            return RedirectToAction("Index");
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
                TempData["SuccessErrorMessage"] = "IRF pause Failed";
                return RedirectToAction("Index");
            }

        }

        [Authorize]
        [HttpGet]
        public IActionResult PlayIRF(int irfId, string irfName)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        if (_irfService.PlayIRF(irfId, irfName))
                        {
                            TempData["SuccessErrorMessage"] = "IRF has been enabled Successfully!";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Failed to enable IRF!";
                            return RedirectToAction("Index");
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
                TempData["SuccessErrorMessage"] = "IRF play Failed";
                return RedirectToAction("Index");
            }

        }

        [HttpGet]
        public async Task<IActionResult> InterviewerDetail(int? Page, string Searchvalue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        if (TempData["SuccessErrorMessageInterviewer"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterviewer"].ToString();
                            TempData.Remove("SuccessErrorMessageInterviewer");

                        }
                        var model = await _userService.GetInterviewersDetails();
                        if (!string.IsNullOrEmpty(Searchvalue))
                        {
                            model = model.Where(x => x.FirstName.ToUpper().Contains(Searchvalue.ToUpper()) || (x.PrimarySkill != null && x.PrimarySkill.ToUpper().Contains(Searchvalue.ToUpper()))).ToList();
                            ViewBag.searchvalue = Searchvalue;
                        }
                        else
                        {
                            ViewBag.searchvalue = "";
                        }
                        foreach (var item in model)
                        {
                            var PrimarySkillSet = item.PrimarySkill?.Split(',');
                            if (PrimarySkillSet == null)
                                continue;
                            string PrimaryTopSkill = string.Empty;
                            int ForLoopCount = 5;
                            if (PrimarySkillSet.Count() < 5)
                            {
                                ForLoopCount = PrimarySkillSet.Count();
                            }
                            for (int i = 0; i < ForLoopCount; i++)
                            {
                                if (string.IsNullOrEmpty(PrimaryTopSkill))
                                {
                                    PrimaryTopSkill = PrimarySkillSet[i];
                                }
                                else
                                {
                                    PrimaryTopSkill = PrimaryTopSkill + "," + PrimarySkillSet[i];
                                }
                            }
                            item.PrimarySkill = PrimaryTopSkill;
                        }

                        return View(model.ToPagedList(Page ?? 1, 12));
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

        [HttpGet]
        public async Task<IActionResult> InterviewerDetailById(string Id, string Videoid)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        // code for fetch data or any thing else
                        InterviewerVM interviewerVM = await _userService.GetInterviewer(Convert.ToInt32(Id));
                        interviewerVM.id = Convert.ToInt32(Id);
                        if (!string.IsNullOrEmpty(Videoid))
                        {
                            //fetch vediolink wth details
                            var model = await _userService.GetMeetingbyid(Convert.ToInt32(Videoid));
                            interviewerVM.VideoLink = model.VideoLink;
                        }
                        interviewerVM.SubVideo_List = await _userService.GetInterviewerVideos(interviewerVM.EmailId);
                        if (interviewerVM.SubVideo_List == null || interviewerVM.SubVideo_List.Count == 0)
                        {
                            interviewerVM.SubVideo_List = new List<InterviewerVM>();
                        }
                        return View(interviewerVM);
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
                return RedirectToAction("InterviewerDetail");
            }
        }

        [Authorize]
        [HttpGet]
        public bool Subscribe(int id)
        {
            try
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                return _irfService.SubscribeCuratedGroup(id, emailid, profileName).Result;

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }

        }

        [Authorize]
        [HttpGet]
        public bool UnSubscribe(int id)
        {
            try
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                return _irfService.UnSubscribeCuratedGroup(id, emailid, profileName).Result;

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }

        }

        //private SelectList GetEnrollmentList()
        //{
        //    var listItems = new List<SelectListItem>();
        //    foreach (var item in Enum.GetValues(typeof(VoiceEnrollment)))
        //    {
        //        listItems.Add(new SelectListItem { Text = item.ToString(), Value = item.ToString() });
        //    }

        //    return new SelectList(listItems, "Value", "Text", listItems.First().Value);
        //}

        [HttpGet]
        public bool SaveCandidateStatus(int candidateId, int statusValue)
        {
            var result = _irfService.SaveCandidateStatus(candidateId, statusValue);
            if (result)
            {
                TempData["SuccessErrorMessageCandidate"] = "Candidate status has been saved Successfully!";

            }
            else
                TempData["SuccessErrorMessageCandidate"] = "Failed to save candidate status!";

            return result;

        }


        [Authorize]
        [HttpGet]
        public IActionResult CandidateCuratedScore(string id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                CandidateCuratedScoreVM model = new CandidateCuratedScoreVM();
                var result = _irfService.GetCandidateCuratedScore(Convert.ToInt32(id));
                model.CandidateCuratedScores = result;
                model.IRFId = Convert.ToInt32(id);
                return View(model);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpPost]
        public IActionResult CandidateCuratedScore(CandidateCuratedScoreVM model)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                _irfService.AddCandidateCuratedScore(model.IRFId, model.CandidateCuratedScores.ToList());
                return RedirectToAction("Index", "IRF");
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult CuratedGroups()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {

                        var curatedGroups = _irfService.GetAllCuratedGroups();
                        if (TempData["SuccessErrorMessageCG"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageCG"].ToString();
                            TempData.Remove("SuccessErrorMessageCG");
                        }
                        return View(curatedGroups);

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

        [Authorize]
        [HttpGet]
        public IActionResult AddCuratedGroup()
        {
            if (HttpContext.Session.GetString("email") != null)
            {

                var curatedGroupModel = new CuratedGroup
                {
                    CuratedIRFMappings = (IList<CuratedIRFMapping>)(_irfService.GetIRFListForCuratedMapping())
                };
                return View(curatedGroupModel);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddCuratedGroup(int[] irfList, string cgName)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    //var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    //profileName = profileName.Replace("\"", "");
                    if (_irfService.AddCuratedGroup(irfList, cgName).Result)
                    {
                        TempData["SuccessErrorMessageCG"] = "Curated Group has been added Successfully!";
                        return Json(true);
                    }
                    else
                    {
                        TempData["SuccessErrorMessageCG"] = "Failed to add Curated Group!";
                        return Json(false);
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
                TempData["SuccessErrorMessageCG"] = "Adding Curated Group has failed";
                return Json(false);
            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult EditCuratedGroup(int id)
        {
            if (HttpContext.Session.GetString("email") != null)
            {

                var cgModel = _irfService.GetCuratedGroupMappingsById(id);
                return View(cgModel);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> EditCuratedGroup(int[] irfList, string cgName, int cgId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (_irfService.UpdateCuratedGroup(irfList, cgName, cgId))
                    {
                        TempData["SuccessErrorMessageCG"] = "Curated Group has been edited Successfully!";
                        return Json(true);
                    }
                    else
                    {
                        TempData["SuccessErrorMessageCG"] = "Failed to save changes for Curated Group!";
                        return Json(false);
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
                TempData["SuccessErrorMessageCG"] = "Saving changes for Curated Group failed!";
                return Json(false);
            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult AddUpdateCuratedGroupUsers(int id)
        {
            if (HttpContext.Session.GetString("email") != null)
            {

                var model = _irfService.GetCuratedGroupUsers(id);
                return View(model);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddUpdateCuratedGroupUsers(int[] users, int curatedGroupId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (_irfService.AddUpdateCuratedGroupUsers(users, curatedGroupId))
                    {
                        TempData["SuccessErrorMessageCG"] = "Curated Group users has been updated Successfully!";
                        return Json(true);
                    }
                    else
                    {
                        TempData["SuccessErrorMessageCG"] = "Sorry! Curated Group users could not be updated.";
                        return Json(false);
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
                TempData["SuccessErrorMessageCG"] = "Sorry! Curated Group users could not be updated.";
                return Json(false);
            }
        }

        [HttpGet]
        public IActionResult CuratedIndex()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "CuratedUser" || profileName == "Client")
                {
                    var model = new CuratedIndexVM
                    {
                        CuratedData = _irfService.GetCuratedGroupsForCuratedUser(emailid)
                    };
                    foreach (var item in model.CuratedData)
                    {
                        if (item.IsDeleted)
                        {
                            item.ImageUrl = "/img/Cards_Png/01_Black-White.png";
                            item.Status = "Archived";
                        }
                        else
                        {
                            item.ImageUrl = "/img/Cards_Png/01_Green-White.png";
                            item.Status = "Active";
                        }
                        if (item.SubId != null)
                        {
                            item.SubscriptionStatus = SubscriptionStatus.Subscribed.GetDescription();
                        }
                        else
                        {
                            item.SubscriptionStatus = SubscriptionStatus.UnSubscribed.GetDescription();
                        }
                    }


                    if (TempData["SuccessErrorMessageCuratedIndex"] != null)
                    {
                        model.Message = TempData["SuccessErrorMessageCuratedIndex"].ToString();
                        TempData.Remove("SuccessErrorMessageCuratedIndex");
                    }

                    if (HttpContext.Session.GetString("CuratedIndexFilters") != null)
                    {
                        var filter = JsonConvert.DeserializeObject<CuratedIndexFilter>(HttpContext.Session.GetString("CuratedIndexFilters"));

                        filter.SubscriptionStatus = filter.SubscriptionStatus ?? (filter.SubscriptionStatus = model.CuratedData.Select(x => x.SubscriptionStatus).ToArray());
                        filter.CuratedGroupName = filter.CuratedGroupName == null ? filter.CuratedGroupName = model.CuratedData.Select(x => x.CuratedGroupName).ToArray() : filter.CuratedGroupName = filter.CuratedGroupName;

                        model.CuratedData = model.CuratedData.Where(c => (filter.SubscriptionStatus.Count() > 0 ? filter.SubscriptionStatus.Contains(c.SubscriptionStatus) : c.SubscriptionStatus == null)
                        && (filter.CuratedGroupName.Count() > 0 ? filter.CuratedGroupName.Contains(c.CuratedGroupName) : c.CuratedGroupName == null)).ToList();

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
        [HttpPost]
        public IActionResult CuratedIndex(CuratedIndexFilter filter)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                if (filter != null)
                {
                    HttpContext.Session.SetString("CuratedIndexFilters", JsonConvert.SerializeObject(filter));

                }
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "CuratedUser" || profileName == "Client")
                {

                    var model = new CuratedIndexVM
                    {
                        CuratedData = _irfService.GetCuratedGroupsForCuratedUser(emailid)
                    };
                    foreach (var item in model.CuratedData)
                    {
                        if (item.IsDeleted)
                        {
                            item.ImageUrl = "/img/Cards_Png/01_Black-White.png";
                            item.Status = "Archived";
                        }
                        else
                        {
                            item.ImageUrl = "/img/Cards_Png/01_Green-White.png";
                            item.Status = "Active";
                        }
                        if (item.SubId != null)
                        {
                            item.SubscriptionStatus = SubscriptionStatus.Subscribed.GetDescription();
                        }
                        else
                        {
                            item.SubscriptionStatus = SubscriptionStatus.UnSubscribed.GetDescription();
                        }
                    }

                    filter.SubscriptionStatus = filter.SubscriptionStatus ?? (filter.SubscriptionStatus = model.CuratedData.Select(x => x.SubscriptionStatus).ToArray());
                    filter.CuratedGroupName = filter.CuratedGroupName == null ? filter.CuratedGroupName = model.CuratedData.Select(x => x.CuratedGroupName).ToArray() : filter.CuratedGroupName = filter.CuratedGroupName;

                    model.CuratedData = model.CuratedData.Where(c => (filter.SubscriptionStatus.Count() > 0 ? filter.SubscriptionStatus.Contains(c.SubscriptionStatus) : c.SubscriptionStatus == null)
                    && (filter.CuratedGroupName.Count() > 0 ? filter.CuratedGroupName.Contains(c.CuratedGroupName) : c.CuratedGroupName == null)).ToList();




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

        [HttpGet]
        public async Task<IActionResult> ResetCuratedIndexFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("CuratedIndexFilters");
                    return RedirectToAction("CuratedIndex");
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
        public async Task<IActionResult> GetIRFInterviewerMembers(int irfId)
        {
            try
            {
                ViewBag.Type = "IRF";
                var model = await _irfService.GetIRFInterviewerMembers(irfId);
                return PartialView("IRFInterviewerMembersPartial", model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        /// <summary>
        /// Fetch All assessment groups
        /// </summary>
        /// <param name="Page"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> InterviewerEntitle(int? Page)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SeniorInterviewerManagement" || profileName == "InterviewerManagement" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "MappingAdmin")
                    {
                        if (TempData["SuccessErrorMessageInterviewer"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterviewer"].ToString();
                            TempData.Remove("SuccessErrorMessageInterviewer");
                        }
                        var model = new Assessment();
                        var modellist = await _userService.GetInterviewerEntitle();
                        if (modellist == null)
                        {
                            modellist = new List<AssignmentVM>();
                        }
                        model._assessment = modellist;
                        var skillset = await _skillService.GetSkills();
                        model._skilldetails.PrimarySkills = new SelectList(skillset, "id", "Name");

                        //    return View("AddInterviewer", model);

                        //var skilllist = _irfService.GetSkillDetails();
                        //ViewBag.skilllist = new SelectList(skilllist, "id", "PrimarySkill");
                        //if (!string.IsNullOrEmpty(Searchvalue))
                        //{
                        //    model = model.Where(x => x.FirstName.ToUpper().Contains(Searchvalue.ToUpper()) || (x.PrimarySkill != null && x.PrimarySkill.ToUpper().Contains(Searchvalue.ToUpper()))).ToList();
                        //    ViewBag.searchvalue = Searchvalue;
                        //}
                        //else
                        //{
                        //    ViewBag.searchvalue = "";
                        //}
                        foreach (var item in model._assessment)
                        {
                            var PrimarySkillSet = item.PrimarySkills?.Split(',');
                            if (PrimarySkillSet == null)
                                continue;
                            string PrimaryTopSkill = string.Empty;
                            int ForLoopCount = 5;
                            if (PrimarySkillSet.Count() < 5)
                            {
                                ForLoopCount = PrimarySkillSet.Count();
                            }
                            for (int i = 0; i < ForLoopCount; i++)
                            {
                                if (string.IsNullOrEmpty(PrimaryTopSkill))
                                {
                                    PrimaryTopSkill = PrimarySkillSet[i];
                                }
                                else
                                {
                                    PrimaryTopSkill = PrimaryTopSkill + "," + PrimarySkillSet[i];
                                }
                            }
                            item.PrimarySkills = PrimaryTopSkill;
                        }
                        return View("InterviewerEntitle", model);
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

        [HttpGet]
        public async Task<ActionResult> EditAssessment(int assessmentid)
        {
            var model = new Assessment();
            var modellist = await _userService.GetInterviewerEntitleById(assessmentid);
            if (modellist == null)
            {

                modellist = new AssignmentVM();
            }
            //var modeldata = modellist.Where(x => x.id == assessmentid).FirstOrDefault();
            var modeldata = modellist;
            //model._assessment = modellist.Where(x => x.id == assessmentid).ToList();
            var resultData = await _skillService.GetAssessmentGroupCategory();
            var _Category = resultData.OrderBy(x => x.Name);
            model._skilldetails.CategoryNameList = new SelectList(_Category, "id", "Name");

            var skillset = await _skillService.GetSkills();
            var primaryskilllist = new List<SelectListItem>();
            model._skilldetails.PrimarySkills = new SelectList(skillset, "id", "Name");
            foreach (var item in skillset)
            {
                bool selected = false;
                if (modeldata != null)
                {
                    if (modeldata.PrimarySkills != null && modeldata.PrimarySkills.Contains(item.Name))
                    {
                        selected = true;
                    }
                }
                var selectItem = new SelectListItem(item.Name, item.id.ToString(), selected);
                primaryskilllist.Add(selectItem);
            }
            model._skilldetails.PrimarySkillsList = primaryskilllist;
            if (modeldata != null)
            {
                model._skilldetails.id = modeldata.id;
                model._skilldetails.AssessmentGroupName = modeldata.AssessmentGroup;
                model._skilldetails.Isinstaavailable = modeldata.Isinstaavailable;
                model._skilldetails.Category = _Category.Where(x => x.Name == modeldata.CategoryName).Select(x => x.id).FirstOrDefault();
                model._skilldetails.CompanyType = modeldata.CompanyType;
                model._skilldetails.Country = modeldata.Country;
                model._skilldetails.IsRelevant = modeldata.IsRelevant;
                model._skilldetails.ProfileType = modeldata.ProfileType;
                model._skilldetails.CompetencyLevel = modeldata.CompetencyLevel;
                model._skilldetails.PerHourRate = modeldata.PerHourRate;
                model._skilldetails.SeniorityLevel = modeldata.SeniorityLevel;
                model._skilldetails.EmploymentType = modeldata.EmploymentType;
                model._skilldetails.AssessmentVerificationType = modeldata.AssessmentVerificationType;
                model._skilldetails.MCQSetsValue = modeldata.MCQSetsValue;
                model._skilldetails.CodeLanguage = modeldata.CodeLanguage;
            }
            else
            {
                model._skilldetails.id = 0;
                model._skilldetails.AssessmentGroupName = "";
                model._skilldetails.Isinstaavailable = false;
            }
            if (string.IsNullOrWhiteSpace(model._skilldetails.AssessmentVerificationType))
            {
                model._skilldetails.AssessmentVerificationType = "MCQ";
            }
            var companyTypes = from CompanyType s in Enum.GetValues(typeof(CompanyType))
                               select new { ID = (int)s, Name = s.GetDescription() };
            model._skilldetails.CompanyTypeList = new SelectList(companyTypes, "ID", "Name");
            //var profiletype = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
            //                  select new { ID = (int)s, Name = s.GetDescription() };
            //ViewBag.ProfileTypeList = new SelectList(profiletype, "ID", "Name");
            //var CompetencyLevel = from CompetencyLevel s in Enum.GetValues(typeof(CompetencyLevel))
            //                      select new { ID = (int)s, Name = s.GetDescription() };
            //ViewBag.CompetencyLevellist = new SelectList(CompetencyLevel, "ID", "Name");
            //var seniorityLevels = from JobLevels s in Enum.GetValues(typeof(JobLevels))
            //                      select new { ID = (int)s, Name = s.GetDescription() };
            //ViewBag.SeniorityLevelList = new SelectList(seniorityLevels, "ID", "Name");


            if (!string.IsNullOrEmpty(model._skilldetails.ProfileType))
            {
                model._skilldetails.ProfileTypeArray = model._skilldetails.ProfileType.Split(',');
            }
            var DifficultyLevelTypes = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                                       select new { ID = (int)s, Name = s.GetDescription() };
            var selectListforDifficultyLevel = new List<SelectListItem>();
            foreach (var item in DifficultyLevelTypes)
            {
                bool selected = false;
                if (model._skilldetails.ProfileTypeArray != null && model._skilldetails.ProfileTypeArray.Contains(Convert.ToString(item.ID)))
                {
                    selected = true;
                }
                var selectItem = new SelectListItem(item.Name, Convert.ToString(item.ID), selected);
                selectListforDifficultyLevel.Add(selectItem);
            }
            model._skilldetails.ProfileTypeList = selectListforDifficultyLevel;

            if (!string.IsNullOrEmpty(model._skilldetails.SeniorityLevel))
            {
                model._skilldetails.SeniorityLevelArray = model._skilldetails.SeniorityLevel.Split(',');
            }
            var seniorityLevelTypes = from JobLevels s in Enum.GetValues(typeof(JobLevels))
                                      select new { ID = (int)s, Name = s.GetDescription() };
            var selectListforSeniorityLevel = new List<SelectListItem>();
            foreach (var item in seniorityLevelTypes)
            {
                bool selected = false;
                if (model._skilldetails.SeniorityLevelArray != null && model._skilldetails.SeniorityLevelArray.Contains(Convert.ToString(item.ID)))
                {
                    selected = true;
                }
                var selectItem = new SelectListItem(item.Name, Convert.ToString(item.ID), selected);
                selectListforSeniorityLevel.Add(selectItem);
            }
            model._skilldetails.SeniorityLevelList = selectListforSeniorityLevel;

            if (!string.IsNullOrEmpty(model._skilldetails.EmploymentType))
            {
                model._skilldetails.EmploymentTypeArray = model._skilldetails.EmploymentType.Split(',');
            }
            var employementTypes = from EmploymentTypeOptions s in Enum.GetValues(typeof(EmploymentTypeOptions))
                                   select new { ID = (int)s, Name = s.GetDescription() };
            var selectListforEmployementTypes = new List<SelectListItem>();
            foreach (var item in employementTypes)
            {
                bool selected = false;
                if (model._skilldetails.EmploymentTypeArray != null && model._skilldetails.EmploymentTypeArray.Contains(Convert.ToString(item.ID)))
                {
                    selected = true;
                }
                var selectItem = new SelectListItem(item.Name, Convert.ToString(item.ID), selected);
                selectListforEmployementTypes.Add(selectItem);
            }
            model._skilldetails.EmploymentTypeList = selectListforEmployementTypes;
            var mcqsets = await _skillService.GetMCQSetsDetails();
            model._skilldetails.MCQSetsList = new SelectList(mcqsets, "id", "Name");
            var programmingLanguage = from ProgrammingLanguageenum s in Enum.GetValues(typeof(ProgrammingLanguageenum))
                                      select new { ID = s.GetDescription(), Name = s.GetDescription() };
            model._skilldetails.ProgrammingLanguageList = new SelectList(programmingLanguage, "ID", "Name");
            return PartialView(model);
        }

        /// <summary>
        /// Fetch Assessment Related to Assessment Group
        /// </summary>
        /// <param name="Page"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> InterviewerEntitleDetails(int? Page, int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorInterviewerManagement" || profileName == "InterviewerManagement" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "MappingAdmin")
                    {
                        if (TempData["SuccessErrorMessageInterviewer"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterviewer"].ToString();
                            TempData.Remove("SuccessErrorMessageInterviewer");
                        }
                        var model = await _userService.InterviewerEntitleDetails(id);

                        //if (!string.IsNullOrEmpty(Searchvalue))
                        //{
                        //    model = model.Where(x => x.FirstName.ToUpper().Contains(Searchvalue.ToUpper()) || (x.PrimarySkill != null && x.PrimarySkill.ToUpper().Contains(Searchvalue.ToUpper()))).ToList();
                        //    ViewBag.searchvalue = Searchvalue;
                        //}
                        //else
                        //{
                        //    ViewBag.searchvalue = "";
                        //}
                        foreach (var item in model)
                        {
                            item.Seniority = ((Senioritylevel)item.Senioritylevel).GetDescription();
                            var PrimarySkillSet = item.PrimarySkills?.Split(',');
                            if (PrimarySkillSet == null)
                                continue;
                            string PrimaryTopSkill = string.Empty;
                            int ForLoopCount = 5;
                            if (PrimarySkillSet.Count() < 5)
                            {
                                ForLoopCount = PrimarySkillSet.Count();
                            }
                            for (int i = 0; i < ForLoopCount; i++)
                            {
                                if (string.IsNullOrEmpty(PrimaryTopSkill))
                                {
                                    PrimaryTopSkill = PrimarySkillSet[i];
                                }
                                else
                                {
                                    PrimaryTopSkill = PrimaryTopSkill + "," + PrimarySkillSet[i];
                                }
                            }
                            item.PrimarySkills = PrimaryTopSkill;
                        }
                        return View("InterviewerEntitleDetails", model.ToPagedList(Page ?? 1, 12));
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
        /// <summary>
        /// Craete Assessment Group
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> InterviewerEntitle(Assessment model)
        {
            List<Skill> skills = await _skillService.GetSkills();
            List<string> selectedSkill = skills.Where(x => model._skilldetails.SelectedSkillIds.Contains(x.id)).Select(x => x.Name).ToList();
            model._skilldetails.PrimarySkill = string.Join(",", selectedSkill);
            var result = _irfService.Addassignmentgroup(model._skilldetails);
            if (result == "Success")
            {
                if (model._skilldetails.id > 0)
                {
                    TempData["SuccessErrorMessageInterviewer"] = "Edited Successfully.";
                }
                else
                {
                    TempData["SuccessErrorMessageInterviewer"] = "Added Successfully.";
                }
            }
            else
            {
                if (model._skilldetails.id > 0)
                {
                    TempData["SuccessErrorMessageInterviewer"] = "Failed to edit";
                }
                else
                {
                    TempData["SuccessErrorMessageInterviewer"] = "Failed to add";
                }
            }
            return RedirectToAction("InterviewerEntitle");
        }

        /// <summary>
        /// Assessment Detailed page
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult AssessmentDetails(int id)
        {
            var result = _irfService.FetchAssessmentdetails(id);
            var model = result.FirstOrDefault();
            model.Seniority = ((Senioritylevel)model.Senioritylevel).GetDescription();
            model.YearsOfExperience = ((AssessmentYearsOfExperience)model.YearsofExprience).GetDescription();
            model.AssessmentTypes = ((AssessmentType)model.AssessmentType).GetDescription();
            model.Evaluations = ((Evaluation)model.Evaluation).GetDescription();
            model.EmployerStages = ((EmployerStage)model.EmployerStage).GetDescription();
            model.EmployerSizes = ((EmployerSize)model.EmployerSize).GetDescription();
            model.backid = id;
            foreach (var item in model.Questionslist)
            {
                item.Levels = ((AssessmentLevel)item.Level).GetDescription();
            }
            return View(model);
        }

        [HttpPost]
        public JsonResult AssessmentLike(int id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var result = _irfService.AssessmentLike(id, emailid);
                return Json("Success");
            }
            else
            {
                return Json("Fail");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CandidateStatusList(int irfId, int candidateId, string irfName, string candidateName)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");

                    var model = new CandidateStatusListVM();
                    model.CandidateStatusList = await _irfService.GetCandidatesStatusList(irfId, candidateId);
                    model.IRFName = irfName;
                    model.CandidateName = candidateName;
                    model.CandidateId = candidateId;
                    model.IRFId = irfId;
                    model.Profile = profileName;

                    if (TempData["SuccessErrorCandidateStatus"] != null)
                    {
                        ViewBag.Message = TempData["SuccessErrorCandidateStatus"].ToString();
                        TempData.Remove("SuccessErrorCandidateStatus");
                    }
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

        [HttpGet]
        public IActionResult AddCandidateStatus(int candidateId, int irfId, string rfName, string candidateName)
        {
            try
            {

                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName != "Client")
                {
                    var model = new CandidateStatus();
                    model.IRFName = rfName;
                    model.CandidateName = candidateName;
                    model.CandidateId = candidateId;
                    model.IRFId = irfId;

                    return View(model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }



            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Candidates", new { id = irfId.ToString(), irfName = rfName });
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> AddCandidateStatus(CandidateStatus model)
        {

            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName != "Client")
                    {
                        model.PerformedBy = emailid;
                        if (model.AudioFile != null)
                        {
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.AudioFile);
                            if (fileMimeType == "audio/basic" || fileMimeType == "audio/mpeg"
                            || fileMimeType == "audio/x-wav")
                            {
                                await _azureStorageHelper.InsertBlob(model.AudioFile.FileName, "audios", model.AudioFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("audios");
                                var blob = containerRef.GetBlockBlobReference(model.AudioFile.FileName);
                                string uploadedaudioFileURL = blob.Uri.ToString();
                                if (uploadedaudioFileURL == null)
                                {
                                    TempData["SuccessErrorCandidateStatus"] = "Audio Upload failed.";
                                    return RedirectToAction("CandidateStatusList", new { irfId = model.IRFId, candidateId = model.CandidateId, irfName = model.IRFName, candidateName = model.CandidateName });

                                }
                                else
                                {
                                    model.AudioFileLink = uploadedaudioFileURL;
                                }

                            }

                        }

                        if (await _irfService.AddCandidateStatus(model))
                        {
                            TempData["SuccessErrorCandidateStatus"] = "Candidate status has been saved Successfully!";
                            return RedirectToAction("CandidateStatusList", new { irfId = model.IRFId, candidateId = model.CandidateId, irfName = model.IRFName, candidateName = model.CandidateName });
                        }
                        else
                        {
                            TempData["SuccessErrorCandidateStatus"] = "Error saving candidate status.";
                            return RedirectToAction("CandidateStatusList", new { irfId = model.IRFId, candidateId = model.CandidateId, irfName = model.IRFName, candidateName = model.CandidateName });
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
                TempData["SuccessErrorCandidateStatus"] = "Error saving candidate status.";
                return RedirectToAction("CandidateStatusList", new { irfId = model.IRFId, candidateId = model.CandidateId, irfName = model.IRFName, candidateName = model.CandidateName });
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditCandidateStatus(int id, int candidateId, int irfId, string rfName, string candidateName)
        {
            try
            {

                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName != "Client")
                {
                    var model = new CandidateStatus();
                    model = await _irfService.GetCandidateStatusById(id);
                    model.IRFName = rfName;
                    model.CandidateName = candidateName;
                    model.id = id;

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
                return RedirectToAction("CandidateStatusList", new { irfId = irfId, candidateId = candidateId, irfName = rfName, candidateName = candidateName });
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> EditCandidateStatus(CandidateStatus model)
        {

            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        model.PerformedBy = emailid;
                        if (model.AudioFile != null)
                        {
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.AudioFile);
                            if (fileMimeType == "audio/basic" || fileMimeType == "audio/mpeg"
                            || fileMimeType == "audio/x-wav")
                            {
                                await _azureStorageHelper.InsertBlob(model.AudioFile.FileName, "audios", model.AudioFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("audios");
                                var blob = containerRef.GetBlockBlobReference(model.AudioFile.FileName);
                                string uploadedaudioFileURL = blob.Uri.ToString();
                                if (uploadedaudioFileURL == null)
                                {
                                    TempData["SuccessErrorCandidateStatus"] = "Audio Upload failed.";
                                    return RedirectToAction("CandidateStatusList", new { irfId = model.IRFId, candidateId = model.CandidateId, irfName = model.IRFName, candidateName = model.CandidateName });

                                }
                                else
                                {
                                    model.AudioFileLink = uploadedaudioFileURL;
                                }
                            }

                        }

                        if (await _irfService.EditCandidateStatus(model))
                        {
                            TempData["SuccessErrorCandidateStatus"] = "Candidate status has been updated Successfully!";
                            return RedirectToAction("CandidateStatusList", new { irfId = model.IRFId, candidateId = model.CandidateId, irfName = model.IRFName, candidateName = model.CandidateName });
                        }
                        else
                        {
                            TempData["SuccessErrorCandidateStatus"] = "Error updating candidate status.";
                            return RedirectToAction("CandidateStatusList", new { irfId = model.IRFId, candidateId = model.CandidateId, irfName = model.IRFName, candidateName = model.CandidateName });
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
                TempData["SuccessErrorCandidateStatus"] = "Error updating candidate status.";
                return RedirectToAction("CandidateStatusList", new { irfId = model.IRFId, candidateId = model.CandidateId, irfName = model.IRFName, candidateName = model.CandidateName });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> DeleteCandidateStatus(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName != "Client")
                    {
                        var result = await _irfService.DeleteCandidateStatus(id);
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
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(false);
            }

        }

        [HttpPost]
        public JsonResult CandidatesJson(string searchValue)
        {
            int Page = 1;
            var candidateList = _modelDetails;
            if (!string.IsNullOrEmpty(searchValue))
            {
                searchValue = searchValue.ToUpper().Trim();
                candidateList = candidateList.Where(x => (!string.IsNullOrEmpty(x.Name) ? x.Name.ToUpper().Contains(searchValue) : x.Name == "") || (!string.IsNullOrEmpty(x.EmailId) ? x.EmailId.ToUpper().Contains(searchValue) : x.EmailId == "")).ToList();

            }
            int totalcount = candidateList.Count;
            int Pagestart = 0;
            int Pageend = 0;
            ViewBag.TotalCount = candidateList.Count;

            Pagestart = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
            int a = candidateList.Count / 10;
            if (candidateList.Count % 10 == 0)
            {
                Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
            }
            else
            {
                a = a + 1;
                if (a == Page)
                {
                    Pageend = candidateList.Count;
                }
                else
                {
                    Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));

                }

            }
            if (candidateList.Count == 0)
            {
                Pagestart = 0;
                Pageend = 0;
            }

            return Json(new { result = candidateList.Take(10), startPage = Pagestart, endPage = Pageend, totalPage = a, totalentry = candidateList.Count });
        }

        [HttpPost]
        public JsonResult InterviewMeetingsJson(string searchValue)
        {
            int Page = 1;
            var meetings = _modelDetail;
            if (!string.IsNullOrEmpty(searchValue))
            {
                searchValue = searchValue.ToUpper().Trim();
                meetings = meetings.Where(x => (!string.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == "") || (!string.IsNullOrEmpty(x.ClientHrContact) ? x.ClientHrContact.ToUpper().Contains(searchValue) : x.ClientHrContact == "")
                            || (!string.IsNullOrEmpty(x.confirmationStatus) ? x.confirmationStatus.ToUpper().Contains(searchValue) : x.confirmationStatus == "") || (!string.IsNullOrEmpty(x.MeetingStatusStatusDesc) ? x.MeetingStatusStatusDesc.ToUpper().Contains(searchValue) : x.MeetingStatusStatusDesc == "")
                            || (!string.IsNullOrEmpty(x.intervieweStatusStatDesc) ? x.intervieweStatusStatDesc.ToUpper().Contains(searchValue) : x.intervieweStatusStatDesc == "")).ToList();

            }
            int totalcount = meetings.Count;
            int Pagestart = 0;
            int Pageend = 0;
            ViewBag.TotalCount = meetings.Count;

            Pagestart = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
            int a = meetings.Count / 10;
            if (meetings.Count % 10 == 0)
            {
                Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
            }
            else
            {
                a = a + 1;
                if (a == Page)
                {
                    Pageend = meetings.Count;
                }
                else
                {
                    Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));

                }

            }
            if (meetings.Count == 0)
            {
                Pagestart = 0;
                Pageend = 0;
            }

            return Json(new { result = meetings.Take(10), startPage = Pagestart, endPage = Pageend, totalPage = a, totalentry = meetings.Count });
        }

        [HttpPost]
        public JsonResult SelectedInterviewMeetingsJson(string searchValue)
        {
            int Page = 1;
            var selectedMeeting = _modelDetail;
            if (!string.IsNullOrEmpty(searchValue))
            {
                searchValue = searchValue.ToUpper().Trim();
                selectedMeeting = selectedMeeting.Where(x => (!string.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == "") || (!string.IsNullOrEmpty(x.ClientHrContact) ? x.ClientHrContact.ToUpper().Contains(searchValue) : x.ClientHrContact == "") || (!string.IsNullOrEmpty(x.confirmationStatus) ? x.confirmationStatus.ToUpper().Contains(searchValue) : x.confirmationStatus == "") || (!string.IsNullOrEmpty(x.MeetingStatusStatusDesc) ? x.MeetingStatusStatusDesc.ToUpper().Contains(searchValue) : x.MeetingStatusStatusDesc == "") || (!string.IsNullOrEmpty(x.intervieweStatusStatDesc) ? x.intervieweStatusStatDesc.ToUpper().Contains(searchValue) : x.intervieweStatusStatDesc == "") || (!string.IsNullOrEmpty(x.confirmationStatus) ? x.confirmationStatus.ToUpper().Contains(searchValue) : x.confirmationStatus == "") || (!string.IsNullOrEmpty(x.MeetingStatusStatusDesc) ? x.MeetingStatusStatusDesc.ToUpper().Contains(searchValue) : x.MeetingStatusStatusDesc == "") || (!string.IsNullOrEmpty(x.intervieweStatusStatDesc) ? x.intervieweStatusStatDesc.ToUpper().Contains(searchValue) : x.intervieweStatusStatDesc == "")).ToList();

            }
            int totalcount = selectedMeeting.Count;
            int Pagestart = 0;
            int Pageend = 0;
            ViewBag.TotalCount = selectedMeeting.Count;

            Pagestart = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
            int a = selectedMeeting.Count / 10;
            if (selectedMeeting.Count % 10 == 0)
            {
                Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
            }
            else
            {
                a = a + 1;
                if (a == Page)
                {
                    Pageend = selectedMeeting.Count;
                }
                else
                {
                    Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));

                }

            }
            if (selectedMeeting.Count == 0)
            {
                Pagestart = 0;
                Pageend = 0;
            }

            return Json(new { result = selectedMeeting.Take(10), startPage = Pagestart, endPage = Pageend, totalPage = a, totalentry = selectedMeeting.Count });
        }

        [HttpPost]
        public JsonResult MultipleCandidateInvitesJson(string searchValue)
        {
            int Page = 1;
            var modelList = _modelDetle;
            if (!string.IsNullOrEmpty(searchValue))
            {
                searchValue = searchValue.ToUpper().Trim();
                modelList = modelList.Where(x => (!string.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == "") ||
                (!string.IsNullOrEmpty(x.JDTiltle) ? x.JDTiltle.Split("|")[0].ToUpper().Contains(searchValue) : x.JDTiltle == string.Empty) || (!string.IsNullOrEmpty(x.confirmationStatus) ? x.confirmationStatus.ToUpper().Contains(searchValue) : x.confirmationStatus == string.Empty) || (!string.IsNullOrEmpty(x.MeetingStatusStatusDesc) ? x.MeetingStatusStatusDesc.ToUpper().Contains(searchValue) : x.MeetingStatusStatusDesc == null) || (!string.IsNullOrEmpty(x.intervieweStatusStatDesc) ? x.intervieweStatusStatDesc.ToUpper().Contains(searchValue) : x.intervieweStatusStatDesc == null)).ToList();

            }
            int totalcount = modelList.Count;
            int Pagestart = 0;
            int Pageend = 0;
            ViewBag.TotalCount = modelList.Count;

            Pagestart = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
            int a = modelList.Count / 10;
            if (modelList.Count % 10 == 0)
            {
                Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
            }
            else
            {
                a = a + 1;
                if (a == Page)
                {
                    Pageend = modelList.Count;
                }
                else
                {
                    Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));

                }

            }
            if (modelList.Count == 0)
            {
                Pagestart = 0;
                Pageend = 0;
            }

            return Json(new { result = modelList.Take(10), startPage = Pagestart, endPage = Pageend, totalPage = a, totalentry = modelList.Count });
        }

        [HttpGet]
        public async Task<IActionResult> GetCandidateStatus(int id, string irfId)
        {
            var profileName = HttpContext.Session.GetString("profileName")?.Trim();
            profileName = profileName.Replace("\"", "");
            var emailid = HttpContext.Session.GetString("email")?.Trim();
            emailid = emailid.Replace("\"", "");
            if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
            {
                if (HttpContext.Session.GetString("IRFCandidateStatusfilters") != null)
                {
                    HttpContext.Session.Remove("IRFCandidateStatusfilters");
                }

                var model = new List<CandidateTimelineVM>();
                var candidateData = await _irfService.GetCandidateTimeline(id, emailid);
                var distinctCandidates = candidateData.Select(x => (x.IRFId, x.CandidateId, x.Name)).Distinct().OrderBy(x => x.Name);
                foreach (var candidate in distinctCandidates)
                {
                    var firstData = new CandidateTimelineData();
                    var data = candidateData.Where(x => (x.CandidateId == candidate.CandidateId && x.IRFId == candidate.IRFId));
                    if (data.Where(x => x.confirmationStatus == 1).Any() && data.Where(x => x.confirmationStatus == 2).Any())
                    {
                        //data = data.Where(x => x.confirmationStatus != 2);
                        //data = data.OrderByDescending(x => x.InterviewId);
                        firstData = data.Where(x => x.confirmationStatus == 1).OrderByDescending(x => x.interviewDate).FirstOrDefault();
                    }
                    else
                    {
                        firstData = data.FirstOrDefault();
                    }
                    var candidateTimeline = new CandidateTimelineVM();

                    candidateTimeline.HREmail = firstData.HREmail;
                    candidateTimeline.CandidateEmailId = firstData.EmailId;
                    candidateTimeline.CandidateName = firstData.Name;
                    candidateTimeline.IRFId = firstData.IRFId;
                    candidateTimeline.InterviewId = firstData.InterviewId;
                    candidateTimeline.CurrentStatus = firstData.CurrentStatus != null ? firstData.CurrentStatus : string.Empty;
                    candidateTimeline.CandidateMasterId = firstData.CandidateMasterId;
                    candidateTimeline.CurrentInterviewStatus = firstData.CurrentInterviewStatus;
                    //candidateTimeline.QCStatus = firstData.QCStatus;
                    candidateTimeline.CurrentMeetingStatus = firstData.CurrentMeetingStatus;
                    candidateTimeline.confirmationStatus = firstData.confirmationStatus;
                    candidateTimeline.PrimarySkill = firstData.PrimarySkill;
                    candidateTimeline.ScheduleStatus = new List<TimelineStatus>();

                    //schedule status
                    var scheduleStatusData = data.Select(x => (x.ScheduleStatusDate, x.ScheduleStatusValue, x.ScheduleStatusComments)).Distinct();
                    if (scheduleStatusData.FirstOrDefault().ScheduleStatusDate != DateTime.MinValue)
                    {
                        scheduleStatusData = scheduleStatusData.OrderBy(x => x.ScheduleStatusDate);
                        var scheduleStatusObjList = new List<TimelineStatus>();
                        foreach (var item in scheduleStatusData.Where(x => x.ScheduleStatusValue != "Meeting Status" && x.ScheduleStatusValue != "Interview Status" && x.ScheduleStatusComments != CandidateStatusOptions.QCPending.GetDescription()))
                        {
                            var scheduleStatusObj = new TimelineStatus();
                            scheduleStatusObj.Date = item.ScheduleStatusDate.ToString("dd");
                            scheduleStatusObj.Day = item.ScheduleStatusDate.Date.ToString("dddd");
                            scheduleStatusObj.Time = item.ScheduleStatusDate.ToString("hh:mm tt");
                            scheduleStatusObj.Status = item.ScheduleStatusValue;

                            scheduleStatusObj.StatusDate = item.ScheduleStatusDate.Date.ToString("MMMM yyyy");
                            scheduleStatusObjList.Add(scheduleStatusObj);
                        }
                        candidateTimeline.ScheduleStatus = scheduleStatusObjList;
                        var lastStatus = scheduleStatusData.LastOrDefault();
                        if (lastStatus.ScheduleStatusValue == "Meeting Status" || lastStatus.ScheduleStatusValue == "Interview Status")
                        {
                            candidateTimeline.CurrentStatus = lastStatus.ScheduleStatusComments;
                        }
                        else
                        {
                            candidateTimeline.CurrentStatus = lastStatus.ScheduleStatusValue;

                        }

                    }

                    //meeting status 
                    var meetingStatusData = data.Select(x => (x.MeetingStatusDate, x.MeetingStatus)).Distinct();

                    if (meetingStatusData.FirstOrDefault().MeetingStatusDate != DateTime.MinValue)
                    {
                        meetingStatusData = meetingStatusData.OrderBy(x => x.MeetingStatusDate);
                        var meetingStatusObjList = new List<TimelineStatus>();

                        foreach (var item in meetingStatusData)
                        {
                            if (item.MeetingStatus != MeetingStatus.Scheduled.GetDescription())
                            {
                                var meetingStatusObj = new TimelineStatus();
                                meetingStatusObj.Date = item.MeetingStatusDate.ToString("dd");
                                meetingStatusObj.Day = item.MeetingStatusDate.Date.ToString("dddd");
                                meetingStatusObj.Status = item.MeetingStatus;
                                meetingStatusObj.StatusDate = item.MeetingStatusDate.Date.ToString("MMMM yyyy");
                                meetingStatusObj.Time = item.MeetingStatusDate.ToString("hh:mm tt");

                                meetingStatusObjList.Add(meetingStatusObj);
                            }

                        }
                        candidateTimeline.MeetingStatus = meetingStatusObjList;
                    }
                    //interview status
                    var interviewStatusData = data.Select(x => (x.InterviewStatusDate, x.InterviewStatus)).Distinct();
                    if (interviewStatusData.FirstOrDefault().InterviewStatusDate != DateTime.MinValue)
                    {
                        interviewStatusData = interviewStatusData.OrderBy(x => x.InterviewStatusDate);
                        var interviewStatusObjList = new List<TimelineStatus>();

                        foreach (var item in interviewStatusData)
                        {
                            var interviewStatusObj = new TimelineStatus();

                            interviewStatusObj.Date = item.InterviewStatusDate.ToString("dd");
                            interviewStatusObj.Day = item.InterviewStatusDate.Date.ToString("dddd");
                            interviewStatusObj.Status = item.InterviewStatus;
                            interviewStatusObj.StatusDate = item.InterviewStatusDate.Date.ToString("MMMM yyyy");
                            interviewStatusObj.Time = item.InterviewStatusDate.ToString("hh:mm tt");

                            interviewStatusObjList.Add(interviewStatusObj);
                        }
                        candidateTimeline.InterviewStatus = interviewStatusObjList;
                    }
                    //add candidate to final model
                    if (candidateTimeline.InterviewStatus != null || candidateTimeline.MeetingStatus != null || (candidateTimeline.ScheduleStatus != null && candidateTimeline.ScheduleStatus.Count() != 0) || !(string.IsNullOrEmpty(candidateTimeline.CurrentStatus)))
                    {
                        model.Add(candidateTimeline);

                    }
                }

                ViewBag.id = id;
                return View(model);

            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetCandidateStatus(int id, string irfId, CandidateTimelineFilter filter)
        {
            var profileName = HttpContext.Session.GetString("profileName")?.Trim();
            profileName = profileName.Replace("\"", "");
            var emailid = HttpContext.Session.GetString("email")?.Trim();
            emailid = emailid.Replace("\"", "");
            if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
            {
                var model = new List<CandidateTimelineVM>();
                var candidateData = await _irfService.GetCandidateTimeline(id, emailid);
                var distinctCandidates = candidateData.Select(x => (x.IRFId, x.CandidateId, x.Name)).Distinct().OrderBy(x => x.Name);
                foreach (var candidate in distinctCandidates)
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("IRFCandidateStatusfilters", JsonConvert.SerializeObject(filter));
                    }
                    var firstData = new CandidateTimelineData();
                    var data = candidateData.Where(x => (x.CandidateId == candidate.CandidateId && x.IRFId == candidate.IRFId));
                    if (data.Where(x => x.confirmationStatus == 1).Any() && data.Where(x => x.confirmationStatus == 2).Any())
                    {
                        //data = data.Where(x => x.confirmationStatus != 2);
                        //data = data.OrderByDescending(x => x.InterviewId);
                        firstData = data.Where(x => x.confirmationStatus == 1).OrderByDescending(x => x.interviewDate).FirstOrDefault();
                    }
                    else
                    {
                        firstData = data.FirstOrDefault();
                    }

                    var candidateTimeline = new CandidateTimelineVM();

                    candidateTimeline.HREmail = firstData.HREmail;
                    candidateTimeline.CandidateEmailId = firstData.EmailId;
                    candidateTimeline.CandidateName = firstData.Name;
                    candidateTimeline.IRFId = firstData.IRFId;
                    candidateTimeline.InterviewId = firstData.InterviewId;
                    candidateTimeline.CurrentStatus = firstData.CurrentStatus != null ? firstData.CurrentStatus : string.Empty;
                    candidateTimeline.CandidateMasterId = firstData.CandidateMasterId;
                    candidateTimeline.CurrentInterviewStatus = firstData.CurrentInterviewStatus;
                    //candidateTimeline.QCStatus = firstData.QCStatus;
                    candidateTimeline.CurrentMeetingStatus = firstData.CurrentMeetingStatus;
                    candidateTimeline.confirmationStatus = firstData.confirmationStatus;
                    candidateTimeline.PrimarySkill = firstData.PrimarySkill;
                    candidateTimeline.ScheduleStatus = new List<TimelineStatus>();

                    //schedule status
                    var scheduleStatusData = data.Select(x => (x.ScheduleStatusDate, x.ScheduleStatusValue, x.ScheduleStatusComments)).Distinct();
                    if (scheduleStatusData.FirstOrDefault().ScheduleStatusDate != DateTime.MinValue)
                    {
                        scheduleStatusData = scheduleStatusData.OrderBy(x => x.ScheduleStatusDate);
                        var scheduleStatusObjList = new List<TimelineStatus>();
                        foreach (var item in scheduleStatusData.Where(x => x.ScheduleStatusValue != "Meeting Status" && x.ScheduleStatusValue != "Interview Status" && x.ScheduleStatusComments != CandidateStatusOptions.QCPending.GetDescription()))
                        {
                            var scheduleStatusObj = new TimelineStatus();
                            scheduleStatusObj.Date = item.ScheduleStatusDate.ToString("dd");
                            scheduleStatusObj.Day = item.ScheduleStatusDate.Date.ToString("dddd");
                            scheduleStatusObj.Time = item.ScheduleStatusDate.ToString("hh:mm tt");
                            scheduleStatusObj.Status = item.ScheduleStatusValue;

                            scheduleStatusObj.StatusDate = item.ScheduleStatusDate.Date.ToString("MMMM yyyy");
                            scheduleStatusObjList.Add(scheduleStatusObj);
                        }
                        candidateTimeline.ScheduleStatus = scheduleStatusObjList;
                        var lastStatus = scheduleStatusData.LastOrDefault();
                        if (lastStatus.ScheduleStatusValue == "Meeting Status" || lastStatus.ScheduleStatusValue == "Interview Status")
                        {
                            if ((lastStatus.ScheduleStatusComments == InterviewStatus.Selected.GetDescription() || lastStatus.ScheduleStatusComments == InterviewStatus.BorderLine.GetDescription()) /*&& candidateTimeline.QCStatus != 1*/)
                            {
                                candidateTimeline.CurrentStatus = string.Empty;
                            }
                            else
                            {
                                candidateTimeline.CurrentStatus = lastStatus.ScheduleStatusComments;
                            }

                        }
                        else
                        {
                            candidateTimeline.CurrentStatus = lastStatus.ScheduleStatusValue;

                        }

                    }

                    //meeting status 
                    var meetingStatusData = data.Select(x => (x.MeetingStatusDate, x.MeetingStatus)).Distinct();

                    if (meetingStatusData.FirstOrDefault().MeetingStatusDate != DateTime.MinValue)
                    {
                        meetingStatusData = meetingStatusData.OrderBy(x => x.MeetingStatusDate);
                        var meetingStatusObjList = new List<TimelineStatus>();

                        foreach (var item in meetingStatusData)
                        {
                            if (item.MeetingStatus != MeetingStatus.Scheduled.GetDescription())
                            {
                                var meetingStatusObj = new TimelineStatus();
                                meetingStatusObj.Date = item.MeetingStatusDate.ToString("dd");
                                meetingStatusObj.Day = item.MeetingStatusDate.Date.ToString("dddd");
                                meetingStatusObj.Status = item.MeetingStatus;
                                meetingStatusObj.StatusDate = item.MeetingStatusDate.Date.ToString("MMMM yyyy");
                                meetingStatusObj.Time = item.MeetingStatusDate.ToString("hh:mm tt");

                                meetingStatusObjList.Add(meetingStatusObj);
                            }

                        }
                        candidateTimeline.MeetingStatus = meetingStatusObjList;
                    }
                    //interview status
                    var interviewStatusData = data.Select(x => (x.InterviewStatusDate, x.InterviewStatus)).Distinct();
                    if (interviewStatusData.FirstOrDefault().InterviewStatusDate != DateTime.MinValue)
                    {
                        interviewStatusData = interviewStatusData.OrderBy(x => x.InterviewStatusDate);
                        var interviewStatusObjList = new List<TimelineStatus>();

                        foreach (var item in interviewStatusData)
                        {
                            if ((item.InterviewStatus == InterviewStatus.Selected.GetDescription() || item.InterviewStatus == InterviewStatus.BorderLine.GetDescription()) /*&& candidateTimeline.QCStatus != 1*/)
                            {
                                continue;
                            }
                            var interviewStatusObj = new TimelineStatus();

                            interviewStatusObj.Date = item.InterviewStatusDate.ToString("dd");
                            interviewStatusObj.Day = item.InterviewStatusDate.Date.ToString("dddd");
                            interviewStatusObj.Status = item.InterviewStatus;
                            interviewStatusObj.StatusDate = item.InterviewStatusDate.Date.ToString("MMMM yyyy");
                            interviewStatusObj.Time = item.InterviewStatusDate.ToString("hh:mm tt");

                            interviewStatusObjList.Add(interviewStatusObj);
                        }
                        candidateTimeline.InterviewStatus = interviewStatusObjList;
                    }
                    //add candidate to final model
                    if (candidateTimeline.InterviewStatus != null || candidateTimeline.MeetingStatus != null || (candidateTimeline.ScheduleStatus != null && candidateTimeline.ScheduleStatus.Count() != 0) || !(string.IsNullOrEmpty(candidateTimeline.CurrentStatus)))
                    {
                        model.Add(candidateTimeline);

                    }
                }
                if (filter.StartDate != null && filter.EndDate != null)
                {
                    model = model.Where(t => t.CurrentStatusDate >= filter.StartDate && t.CurrentStatusDate <= filter.EndDate).ToList();
                }
                filter.HREmail = filter.HREmail == null ? filter.HREmail = model.Select(x => x.HREmail).ToArray() : filter.HREmail = filter.HREmail;

                if (filter.ScheduleStatus != null)
                {
                    if (filter.ScheduleStatus.Length == 1)
                    {
                        if (filter.ScheduleStatus[0] == ScheduleStatusTypes.Scheduled.GetDescription())
                        {
                            model = model.Where(x => x.ScheduleStatus != null && x.ScheduleStatus.Count() > 0 && x.ScheduleStatus.Any(y => y.Status == ScheduleStatusTypes.Scheduled.GetDescription())).ToList();
                        }
                        else
                        {
                            model = model.Where(x => x.ScheduleStatus != null && x.ScheduleStatus.Count() > 0 && !x.ScheduleStatus.Any(y => y.Status == ScheduleStatusTypes.Scheduled.GetDescription())).ToList();
                        }
                    }
                }
                if (filter.MeetingStatus != null)
                {
                    model = model.Where(x => x.MeetingStatus != null && x.MeetingStatus.Count() > 0 && x.MeetingStatus.Any(c => filter.MeetingStatus.Contains(c.Status))).ToList();

                }
                if (filter.InterviewStatus != null)
                {
                    model = model.Where(x => x.InterviewStatus != null && x.InterviewStatus.Count() > 0 && x.InterviewStatus.Any(c => filter.InterviewStatus.Contains(c.Status))).ToList();

                }
                if (filter.HREmail != null)
                {
                    model = model.Where(x => filter.HREmail.Contains(x.HREmail)).ToList();
                }
                ViewBag.id = id;
                return View(model);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ResetCandidateTimelineFilter(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("IRFCandidateStatusfilters");
                    return RedirectToAction("GetCandidateStatus", new { id = id });
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
        public async Task<FileContentResult> GetCandidateStatusExportToExcel(int id, string searchValue)
        {
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("CandidateStatus");
            string fileName = "CandidateStatus_" + DateTime.Now.ToString("dd/MM/yyyy") + ".xlsx";
            var profileName = HttpContext.Session.GetString("profileName")?.Trim();
            profileName = profileName.Replace("\"", "");
            var emailid = HttpContext.Session.GetString("email")?.Trim();
            emailid = emailid.Replace("\"", "");
            if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SupportAdmin")
            {
                var model = new List<CandidateTimelineVM>();
                var candidateData = await _irfService.GetCandidateTimeline(id, emailid);
                var distinctCandidates = candidateData.Select(x => (x.IRFId, x.CandidateId, x.Name)).Distinct().OrderBy(x => x.Name);
                foreach (var candidate in distinctCandidates)
                {
                    var firstData = new CandidateTimelineData();
                    var data = candidateData.Where(x => (x.CandidateId == candidate.CandidateId && x.IRFId == candidate.IRFId));
                    if (data.Where(x => x.confirmationStatus == 1).Any() && data.Where(x => x.confirmationStatus == 2).Any())
                    {
                        //data = data.Where(x => x.confirmationStatus != 2);
                        //data = data.OrderByDescending(x => x.InterviewId);
                        firstData = data.Where(x => x.confirmationStatus == 1).OrderByDescending(x => x.interviewDate).FirstOrDefault();
                    }
                    else
                    {
                        firstData = data.FirstOrDefault();
                    }
                    var candidateTimeline = new CandidateTimelineVM();
                    candidateTimeline.HREmail = firstData.HREmail;
                    candidateTimeline.CandidateEmailId = firstData.EmailId;
                    candidateTimeline.CandidateName = firstData.Name;
                    candidateTimeline.IRFId = firstData.IRFId;
                    candidateTimeline.InterviewId = firstData.InterviewId;
                    candidateTimeline.CurrentStatus = firstData.CurrentStatus != null ? firstData.CurrentStatus : string.Empty;
                    candidateTimeline.CandidateMasterId = firstData.CandidateMasterId;
                    candidateTimeline.CurrentInterviewStatus = firstData.CurrentInterviewStatus;
                    //candidateTimeline.QCStatus = firstData.QCStatus;
                    candidateTimeline.CurrentMeetingStatus = firstData.CurrentMeetingStatus;
                    candidateTimeline.confirmationStatus = firstData.confirmationStatus;
                    candidateTimeline.PrimarySkill = firstData.PrimarySkill;
                    candidateTimeline.ScheduleStatus = new List<TimelineStatus>();
                    //schedule status
                    var scheduleStatusData = data.Select(x => (x.ScheduleStatusDate, x.ScheduleStatusValue, x.ScheduleStatusComments)).Distinct();
                    if (scheduleStatusData.FirstOrDefault().ScheduleStatusDate != DateTime.MinValue)
                    {
                        scheduleStatusData = scheduleStatusData.OrderBy(x => x.ScheduleStatusDate);
                        var scheduleStatusObjList = new List<TimelineStatus>();
                        foreach (var item in scheduleStatusData.Where(x => x.ScheduleStatusValue != "Meeting Status" && x.ScheduleStatusValue != "Interview Status" && x.ScheduleStatusComments != CandidateStatusOptions.QCPending.GetDescription()))
                        {
                            var scheduleStatusObj = new TimelineStatus();
                            scheduleStatusObj.Date = item.ScheduleStatusDate.ToString("dd");
                            scheduleStatusObj.Day = item.ScheduleStatusDate.Date.ToString("dddd");
                            scheduleStatusObj.Time = item.ScheduleStatusDate.ToString("hh:mm tt");
                            scheduleStatusObj.Status = item.ScheduleStatusValue;
                            scheduleStatusObj.ActualDate = item.ScheduleStatusDate;
                            scheduleStatusObj.StatusDate = item.ScheduleStatusDate.Date.ToString("MMMM yyyy");
                            scheduleStatusObjList.Add(scheduleStatusObj);
                        }
                        candidateTimeline.ScheduleStatus = scheduleStatusObjList;
                        var lastStatus = scheduleStatusData.LastOrDefault();
                        if (lastStatus.ScheduleStatusValue == "Meeting Status" || lastStatus.ScheduleStatusValue == "Interview Status")
                        {
                            if ((lastStatus.ScheduleStatusComments == InterviewStatus.Selected.GetDescription() || lastStatus.ScheduleStatusComments == InterviewStatus.BorderLine.GetDescription()) /*&& candidateTimeline.QCStatus != 1*/)
                            {
                                candidateTimeline.CurrentStatus = string.Empty;
                            }
                            else
                            {
                                candidateTimeline.CurrentStatus = lastStatus.ScheduleStatusComments;
                            }
                        }
                        else
                        {
                            candidateTimeline.CurrentStatus = lastStatus.ScheduleStatusValue;

                        }
                    }
                    //meeting status 
                    var meetingStatusData = data.Select(x => (x.MeetingStatusDate, x.MeetingStatus)).Distinct();

                    if (meetingStatusData.FirstOrDefault().MeetingStatusDate != DateTime.MinValue)
                    {
                        meetingStatusData = meetingStatusData.OrderBy(x => x.MeetingStatusDate);
                        var meetingStatusObjList = new List<TimelineStatus>();

                        foreach (var item in meetingStatusData)
                        {
                            if (item.MeetingStatus != MeetingStatus.Scheduled.GetDescription())
                            {
                                var meetingStatusObj = new TimelineStatus();
                                meetingStatusObj.Date = item.MeetingStatusDate.ToString("dd");
                                meetingStatusObj.Day = item.MeetingStatusDate.Date.ToString("dddd");
                                meetingStatusObj.Status = item.MeetingStatus;
                                meetingStatusObj.StatusDate = item.MeetingStatusDate.Date.ToString("MMMM yyyy");
                                meetingStatusObj.Time = item.MeetingStatusDate.ToString("hh:mm tt");
                                meetingStatusObj.ActualDate = item.MeetingStatusDate;
                                meetingStatusObjList.Add(meetingStatusObj);
                            }

                        }
                        candidateTimeline.MeetingStatus = meetingStatusObjList;
                    }
                    //interview status
                    var interviewStatusData = data.Select(x => (x.InterviewStatusDate, x.InterviewStatus)).Distinct();
                    if (interviewStatusData.FirstOrDefault().InterviewStatusDate != DateTime.MinValue)
                    {
                        interviewStatusData = interviewStatusData.OrderBy(x => x.InterviewStatusDate);
                        var interviewStatusObjList = new List<TimelineStatus>();

                        foreach (var item in interviewStatusData)
                        {
                            if ((item.InterviewStatus == InterviewStatus.Selected.GetDescription() || item.InterviewStatus == InterviewStatus.BorderLine.GetDescription()) /*&& candidateTimeline.QCStatus != 1*/)
                            {
                                continue;
                            }
                            var interviewStatusObj = new TimelineStatus();

                            interviewStatusObj.Date = item.InterviewStatusDate.ToString("dd");
                            interviewStatusObj.Day = item.InterviewStatusDate.Date.ToString("dddd");
                            interviewStatusObj.Status = item.InterviewStatus;
                            interviewStatusObj.StatusDate = item.InterviewStatusDate.Date.ToString("MMMM yyyy");
                            interviewStatusObj.Time = item.InterviewStatusDate.ToString("hh:mm tt");
                            interviewStatusObj.ActualDate = item.InterviewStatusDate;
                            interviewStatusObjList.Add(interviewStatusObj);
                        }
                        candidateTimeline.InterviewStatus = interviewStatusObjList;
                    }
                    //add candidate to final model
                    if (candidateTimeline.InterviewStatus != null || candidateTimeline.MeetingStatus != null || (candidateTimeline.ScheduleStatus != null && candidateTimeline.ScheduleStatus.Count() != 0) || !(string.IsNullOrEmpty(candidateTimeline.CurrentStatus)))
                    {
                        model.Add(candidateTimeline);

                    }
                }
                if (HttpContext.Session.GetString("IRFCandidateStatusfilters") != null)
                {
                    var filter = JsonConvert.DeserializeObject<CandidateTimelineFilter>(HttpContext.Session.GetString("IRFCandidateStatusfilters"));
                    if (filter.StartDate != null && filter.EndDate != null)
                    {
                        model = model.Where(t => t.CurrentStatusDate >= filter.StartDate && t.CurrentStatusDate <= filter.EndDate).ToList();
                    }
                    filter.HREmail = filter.HREmail == null ? filter.HREmail = model.Select(x => x.HREmail).ToArray() : filter.HREmail = filter.HREmail;
                    if (filter.ScheduleStatus != null)
                    {
                        if (filter.ScheduleStatus.Length == 1)
                        {
                            if (filter.ScheduleStatus[0] == ScheduleStatusTypes.Scheduled.GetDescription())
                            {
                                model = model.Where(x => x.ScheduleStatus != null && x.ScheduleStatus.Count() > 0 && x.ScheduleStatus.Any(y => y.Status == ScheduleStatusTypes.Scheduled.GetDescription())).ToList();
                            }
                            else
                            {
                                model = model.Where(x => x.ScheduleStatus != null && x.ScheduleStatus.Count() > 0 && !x.ScheduleStatus.Any(y => y.Status == ScheduleStatusTypes.Scheduled.GetDescription())).ToList();
                            }
                        }
                    }
                    if (filter.MeetingStatus != null)
                    {
                        model = model.Where(x => x.MeetingStatus != null && x.MeetingStatus.Count() > 0 && x.MeetingStatus.Any(c => filter.MeetingStatus.Contains(c.Status))).ToList();
                    }
                    if (filter.InterviewStatus != null)
                    {
                        model = model.Where(x => x.InterviewStatus != null && x.InterviewStatus.Count() > 0 && x.InterviewStatus.Any(c => filter.InterviewStatus.Contains(c.Status))).ToList();
                    }
                    if (filter.HREmail != null)
                    {
                        model = model.Where(x => filter.HREmail.Contains(x.HREmail)).ToList();
                    }
                }
                Sheet.Cells["A1:Z1"].Style.Font.Bold = true;
                Sheet.Cells["A1:Z1"].Style.Font.Size = 12;
                Sheet.Cells["A1:Z1"].Style.WrapText = true;
                int startColumn = 1; int endColumn = 10;
                for (int a = startColumn; a <= endColumn; a++)
                {
                    Sheet.Column(a).Width = 40;
                }
                Sheet.Cells[1, 1].Value = "Candidate Added Date";
                Sheet.Cells[1, 2].Value = "Candidate Name";
                Sheet.Cells[1, 3].Value = "Candidate Email";
                Sheet.Cells[1, 4].Value = "Primary Skill";
                Sheet.Cells[1, 5].Value = "Client HR Email";
                Sheet.Cells[1, 6].Value = "Schedule Status";
                Sheet.Cells[1, 7].Value = "Meeting Status";
                Sheet.Cells[1, 8].Value = "Interview Status";
                Sheet.Cells[1, 9].Value = "Current Status";
                Sheet.Cells[1, 10].Value = "Last Status Date";
                int i = 2;
                if (model.Count() > 0)
                {
                    foreach (var item in model)
                    {
                        Sheet.Cells[i, 1].Value = item.ScheduleStatus.Where(x => x.Status.ToUpper() == "ADDED").FirstOrDefault() != null ? item.ScheduleStatus.Where(x => x.Status.ToUpper() == "ADDED").FirstOrDefault().ActualDate.ToString("MM/dd/yyyy HH:mm") : "";
                        Sheet.Cells[i, 2].Value = item.CandidateName;
                        Sheet.Cells[i, 3].Value = item.CandidateEmailId;
                        Sheet.Cells[i, 4].Value = item.PrimarySkill;
                        Sheet.Cells[i, 5].Value = item.HREmail;
                        if (item.ScheduleStatus.Where(x => x.Status.ToUpper() == "SCHEDULED").Count() > 0)
                        {
                            Sheet.Cells[i, 6].Value = "Scheduled";
                        }
                        else
                        {
                            Sheet.Cells[i, 6].Value = "Yet To Scheduled";
                        }
                        if (item.MeetingStatus != null && item.MeetingStatus.Count != 0)
                        {
                            Sheet.Cells[i, 7].Value = item.MeetingStatus.OrderByDescending(x => x.ActualDate).FirstOrDefault().Status;
                        }
                        if (item.InterviewStatus != null && item.InterviewStatus.Count != 0)
                        {
                            Sheet.Cells[i, 8].Value = item.InterviewStatus.OrderByDescending(x => x.ActualDate).FirstOrDefault().Status;
                        }
                        Sheet.Cells[i, 9].Value = item.CurrentStatus;

                        if (item.ScheduleStatus != null)
                        {
                            if (item.MeetingStatus != null && item.MeetingStatus.Count != 0)
                            {
                                if (item.InterviewStatus != null && item.InterviewStatus.Count != 0)
                                {
                                    if (item.MeetingStatus.OrderByDescending(x => x.ActualDate).FirstOrDefault().ActualDate > item.InterviewStatus.OrderByDescending(x => x.ActualDate).FirstOrDefault().ActualDate)
                                    {
                                        if (item.MeetingStatus.OrderByDescending(x => x.ActualDate).FirstOrDefault().ActualDate > item.ScheduleStatus.OrderByDescending(x => x.ActualDate).FirstOrDefault().ActualDate)
                                        {
                                            Sheet.Cells[i, 10].Value = item.MeetingStatus.OrderByDescending(x => x.ActualDate).FirstOrDefault().ActualDate.ToString("MM/dd/yyyy HH:mm");
                                        }
                                        else
                                        {
                                            Sheet.Cells[i, 10].Value = item.ScheduleStatus.OrderByDescending(x => x.ActualDate).FirstOrDefault().ActualDate.ToString("MM/dd/yyyy HH:mm");
                                        }
                                    }
                                    else
                                    {
                                        if (item.InterviewStatus.OrderByDescending(x => x.ActualDate).FirstOrDefault().ActualDate > item.ScheduleStatus.OrderByDescending(x => x.ActualDate).FirstOrDefault().ActualDate)
                                        {
                                            Sheet.Cells[i, 10].Value = item.InterviewStatus.OrderByDescending(x => x.ActualDate).FirstOrDefault().ActualDate.ToString("MM/dd/yyyy HH:mm");
                                        }
                                        else
                                        {
                                            Sheet.Cells[i, 10].Value = item.ScheduleStatus.OrderByDescending(x => x.ActualDate).FirstOrDefault().ActualDate.ToString("MM/dd/yyyy HH:mm");
                                        }
                                    }
                                }
                                else
                                {
                                    if (item.MeetingStatus.OrderByDescending(x => x.ActualDate).FirstOrDefault().ActualDate > item.ScheduleStatus.OrderByDescending(x => x.ActualDate).FirstOrDefault().ActualDate)
                                    {
                                        Sheet.Cells[i, 10].Value = item.MeetingStatus.OrderByDescending(x => x.ActualDate).FirstOrDefault().ActualDate.ToString("MM/dd/yyyy HH:mm");
                                    }
                                    else
                                    {
                                        Sheet.Cells[i, 10].Value = item.ScheduleStatus.OrderByDescending(x => x.ActualDate).FirstOrDefault().ActualDate.ToString("MM/dd/yyyy HH:mm");
                                    }
                                }
                            }
                            else
                            {
                                if (item.InterviewStatus != null && item.InterviewStatus.Count != 0)
                                {
                                    if (item.InterviewStatus.OrderByDescending(x => x.ActualDate).FirstOrDefault().ActualDate > item.ScheduleStatus.OrderByDescending(x => x.ActualDate).FirstOrDefault().ActualDate)
                                    {
                                        Sheet.Cells[i, 10].Value = item.InterviewStatus.OrderByDescending(x => x.ActualDate).FirstOrDefault().ActualDate.ToString("MM/dd/yyyy HH:mm");
                                    }
                                    else
                                    {
                                        Sheet.Cells[i, 10].Value = item.ScheduleStatus.OrderByDescending(x => x.ActualDate).FirstOrDefault().ActualDate.ToString("MM/dd/yyyy HH:mm");
                                    }
                                }
                                else
                                {
                                    Sheet.Cells[i, 10].Value = item.ScheduleStatus.OrderByDescending(x => x.ActualDate).FirstOrDefault().ActualDate.ToString("MM/dd/yyyy HH:mm");
                                }
                            }
                        }
                        else
                        {
                            if (item.MeetingStatus != null && item.MeetingStatus.Count != 0)
                            {
                                if (item.InterviewStatus != null && item.InterviewStatus.Count != 0)
                                {
                                    if (item.InterviewStatus.OrderByDescending(x => x.ActualDate).FirstOrDefault().ActualDate > item.MeetingStatus.OrderByDescending(x => x.ActualDate).FirstOrDefault().ActualDate)
                                    {
                                        Sheet.Cells[i, 10].Value = item.InterviewStatus.OrderByDescending(x => x.ActualDate).FirstOrDefault().ActualDate.ToString("MM/dd/yyyy HH:mm");
                                    }
                                    else
                                    {
                                        Sheet.Cells[i, 10].Value = item.MeetingStatus.OrderByDescending(x => x.ActualDate).FirstOrDefault().ActualDate.ToString("MM/dd/yyyy HH:mm");
                                    }
                                }
                                else
                                {
                                    Sheet.Cells[i, 10].Value = item.MeetingStatus.OrderByDescending(x => x.ActualDate).FirstOrDefault().ActualDate.ToString("MM/dd/yyyy HH:mm");
                                }
                            }
                            else
                            {
                                if (item.InterviewStatus != null && item.InterviewStatus.Count != 0)
                                {
                                    Sheet.Cells[i, 10].Value = item.InterviewStatus.OrderByDescending(x => x.ActualDate).FirstOrDefault().ActualDate.ToString("MM/dd/yyyy HH:mm");
                                }
                            }
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

        [Authorize]
        [HttpGet]
        public async Task<JsonResult> SendEmailForCandidateFeedbackByIRF(string IRFId)
        {
            var modelData = new List<SurveyVM>();
            AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
            var result = _azureTableStorageService.GetAllEntities<SurveyVM>("RisebirdSurvey");
            var interviewerids = await _meetingService.FetchAllInterviewidfromIrf(IRFId);
            if (interviewerids != null)
            {
                var _resultlist = (from data in interviewerids
                                   select new SurveyDetails { interviewid = data }).ToList();
                _resultlist = _resultlist.Distinct().ToList();
                var resultset = _meetingService.FetchSurveyCandidateEmails(_resultlist);
                bool flag = false;
                foreach (var item in interviewerids)
                {
                    if (result.Where(x => x.interviewid == item).Count() == 0)
                    {
                        var CandidateEmail = resultset.Where(x => x.interviewid == item).Select(x => x.Intervieweremail).FirstOrDefault();
                        string currenturl = "https://" + Request.Host.ToString() + "/Survey/Index?id=" + item;
                        Mail _mail = new Mail
                        {
                            TO = CandidateEmail,
                            Subject = "Risebird: Feedback form"
                        };
                        var emailsend = await _notificationHelper.SurveyNotification(_mail, currenturl);
                        if (emailsend == "Mail sent successfully.")
                        {
                            flag = true;
                        }
                    }
                }
                if (flag == true)
                {
                    return Json("Success");
                }
                else
                {
                    return Json("fail");
                }
            }
            return Json("Success");
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> PublicAddReport(int interviewId, int irfId, bool IsSelected = false)
        {
            try
            {
                var model = await _meetingService.GetInteviewReport(interviewId, irfId);
                ViewBag.IsSelected = IsSelected;
                if (model.QuestionsCollection == null)
                {
                    ViewBag.QuestionsDiscussedCount = 0;
                }
                else
                {
                    ViewBag.QuestionsDiscussedCount = model.QuestionsCollection?.Count();
                }
                ViewBag.profileName = "";
                ViewBag.PageStatus = "Public";
                await GetStatusList(model, irfId);
                var menuData = await _userService.GetMenuBasedOnUserEmailld("ExternalUser");
                var menus = menuData.GroupBy(p => p.functionName).Select(g => g.First()).ToList();
                if (menus != null)
                {
                    menus = menus.Where(x => x.functionName != "Support" && x.functionName != "Common Pool").ToList();
                    HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
                }
                if (model.IsProctor)
                {
                    return PartialView("ProctorReportPartial", model);
                }
                else
                {
                    return PartialView("AddReportPartial", model);
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
        public async Task<IActionResult> ExecutiveReport(int interviewId, int irfId, bool IsSelected = false)
        {
            try
            {
                var model = _meetingService.GetInteviewReport(interviewId, irfId).Result;
                ViewBag.IsSelected = IsSelected;
                if (model.QuestionsCollection == null)
                {
                    ViewBag.QuestionsDiscussedCount = 0;
                }
                else
                {
                    ViewBag.QuestionsDiscussedCount = model.QuestionsCollection?.Count();
                }
                ViewBag.profileName = "";
                ViewBag.PageStatus = "PublicExternal";
                ViewBag.PageReport = "";
                await GetStatusList(model, irfId);
                var menuData = await _userService.GetMenuBasedOnUserEmailld("ExternalUser");
                var menus = menuData.GroupBy(p => p.functionName).Select(g => g.First()).ToList();
                if (menus != null)
                {
                    menus = menus.Where(x => x.functionName != "Support" && x.functionName != "Common Pool" && x.functionName != "Risetube").ToList();
                    HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
                }
                if (model.IsProctor)
                {
                    return PartialView("ProctorReportPartial", model);
                }
                else
                {
                    return PartialView("AddReportPartial", model);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }

        }

        [HttpPost]
        public JsonResult CheckEditCondition(string IRFId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        return Json("Success");
                    }
                    else
                    {
                        if (_irfService.CheckChannelOwnerByIrfid(IRFId, emailid))
                        {
                            return Json("Success");
                        }
                        else
                        {
                            return Json("Fail");
                        }
                    }
                }
                else
                {
                    return Json("Fail");
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Fail");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> NotScheduledCandidate(string IRFId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Client" || profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        if (TempData["SuccessErrorMessageCandidate"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageCandidate"].ToString();
                            TempData.Remove("SuccessErrorMessageCandidate");
                        }
                        var meetings = _meetingService.GetClientNotScheduledMeetingListByIRFId(IRFId, emailid);
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

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> InstaAddReport(int interviewId, int irfId, bool IsSelected = false)
        {
            try
            {
                var model = await _meetingService.GetInteviewReport(interviewId, irfId);
                ViewBag.IsSelected = IsSelected;
                if (model.QuestionsCollection == null)
                {
                    ViewBag.QuestionsDiscussedCount = 0;
                }
                else
                {
                    ViewBag.QuestionsDiscussedCount = model.QuestionsCollection?.Count();
                }
                ViewBag.profileName = "";
                ViewBag.PageStatus = "PublicExternal";
                ViewBag.PageReport = "Insta";
                await GetStatusList(model, irfId);
                var menuData = await _userService.GetMenuBasedOnUserEmailld("ExternalUser");
                var menus = menuData.GroupBy(p => p.functionName).Select(g => g.First()).ToList();
                if (menus != null)
                {
                    menus = menus.Where(x => x.functionName != "Support" && x.functionName != "Common Pool" && x.functionName != "Risetube").ToList();
                    HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
                }
                if (model.IsProctor)
                {
                    return PartialView("ProctorReportPartial", model);
                }
                else
                {
                    return PartialView("AddReportPartial", model);
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
        public async Task<IActionResult> SkillChannelDetails(string type)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                ViewBag.profile = profileName;
                ViewBag.PageStatus = "Main";
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                ViewBag.Type = "ChannelOwner";
                if (profileName == "Client")
                {
                    type = "IRF";
                }
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter" || profileName == "CuratedUser" || profileName == "Finance" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                {
                    var irfList = new List<IRF>();
                    if (HttpContext.Session.GetString("SkillChannelDetailsFilters") == null)
                    {
                        if (type == "IRF")
                        {
                            ViewBag.Type = "IRF";
                            irfList = (await _irfService.GetAllIRFDetailsIRF(DateTime.Now.AddDays(-30), DateTime.Now.AddDays(7), profileName, emailid))?.ToList();
                            //only Active IRFs
                        }
                        else
                        {
                            irfList = (await _irfService.GetAllIRFDetails(DateTime.Now.AddDays(-30), DateTime.Now.AddDays(7)))?.ToList();
                        }
                    }
                    else
                    {
                        var filter = JsonConvert.DeserializeObject<SkillChannelDetailsFilter>(HttpContext.Session.GetString("SkillChannelDetailsFilters"));
                        if (filter.StartDate != null && filter.EndDate != null)
                        {
                            if (type == "IRF")
                            {
                                ViewBag.Type = "IRF";
                                irfList = (await _irfService.GetAllIRFDetailsIRF(filter.StartDate.Value, filter.EndDate.Value, profileName, emailid))?.ToList();
                                //Add Active and Archive filter
                            }
                            else
                            {
                                irfList = (await _irfService.GetAllIRFDetails(filter.StartDate.Value, filter.EndDate.Value))?.ToList();
                            }
                        }
                        else
                        {
                            if (type == "IRF")
                            {
                                ViewBag.Type = "IRF";
                                irfList = (await _irfService.GetAllIRFDetailsIRF(DateTime.Now.AddDays(-30), DateTime.Now.AddDays(7), profileName, emailid))?.ToList();
                                //Add Active and Archive filter
                            }
                            else
                            {
                                irfList = (await _irfService.GetAllIRFDetails(DateTime.Now.AddDays(-30), DateTime.Now.AddDays(7)))?.ToList();
                            }
                        }
                    }
                    return View(irfList);
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
        public async Task<IActionResult> SkillChannelDetails(SkillChannelDetailsFilter filter, string type)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                if (filter != null)
                {
                    HttpContext.Session.SetString("SkillChannelDetailsFilters", JsonConvert.SerializeObject(filter));
                }
                return RedirectToAction("SkillChannelDetails", new { type = type });
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResetSkillChannelDetailsFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("SkillChannelDetailsFilters");
                    return RedirectToAction("SkillChannelDetails");
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
        public IActionResult CloneSkillChannnel()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "Admin" || profileName == "SupportAdmin" || profileName == "SeniorInterviewerManagement")
                {
                    var irfList = _irfService.GetAllSkillChannelDetails()?.Where(x => !string.IsNullOrWhiteSpace(x.AssessmentGroup) && (x.AssessmentGroupId != 0) && (x.CreatedFromTemplateId == null || x.CreatedFromTemplateId == 0));
                    if (HttpContext.Session.GetString("CloneSkillChannelfilters") != null)
                    {
                        var filter = JsonConvert.DeserializeObject<IRFFilter>(HttpContext.Session.GetString("CloneSkillChannelfilters"));
                        filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = irfList.Select(x => x.AssessmentGroup).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                        irfList = irfList.Where(c => (filter.PrimarySkill.Count() > 0 ? filter.PrimarySkill.Contains(c.AssessmentGroup) : c.AssessmentGroup == null)).ToList();
                    }
                    if (TempData["SuccessErrorCloneSkillChannnelMessage"] != null)
                    {
                        ViewBag.Message = TempData["SuccessErrorCloneSkillChannnelMessage"].ToString();
                        TempData.Remove("SuccessErrorCloneSkillChannnelMessage");
                    }
                    return View(irfList);
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
        public IActionResult CloneSkillChannnel(InterviewFilter filter)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "SeniorInterviewerManagement")
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("CloneSkillChannelfilters", JsonConvert.SerializeObject(filter));
                    }
                    var irfList = _irfService.GetAllSkillChannelDetails()?.Where(x => !string.IsNullOrEmpty(x.AssessmentGroup));
                    filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = irfList.Select(x => x.AssessmentGroup).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                    irfList = irfList.Where(c => (filter.PrimarySkill.Count() > 0 ? filter.PrimarySkill.Contains(c.AssessmentGroup) : c.AssessmentGroup == null)).ToList();
                    return View(irfList);
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

        [HttpGet]
        public async Task<IActionResult> ResetCloneSkillChannnelFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("CloneSkillChannelfilters");
                    return RedirectToAction("CloneSkillChannnel");
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

        [HttpPost]
        public async Task<JsonResult> FetchSkillDetails(int id)
        {
            var transTechSkills = await _irfRepository.GetSkillsbyIRFid(id);
            var level = await _irfRepository.GetLevelTech();
            var Skillset = await _irfRepository.GetSkills();
            if (Skillset != null)
            {
                Skillset = Skillset.OrderBy(x => !string.IsNullOrEmpty(x.Name) ? x.Name.Trim() : "").ToList();
            }
            foreach (var item in transTechSkills)
            {
                item.LevelName = level.Where(x => x.id == item.LevelId).Select(x => x.Name).FirstOrDefault();
                item.SkillName = Skillset.Where(x => x.id == item.SkillId).Select(x => x.Name).FirstOrDefault();
            }
            return Json(transTechSkills);
        }
        [Authorize]
        [HttpGet]
        public async Task<FileContentResult> SkillChannelDetailstoExcel(string type)
        {
            string userEmailId = string.Empty;
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("SkillChannelDetails");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_Interviews" + ".xlsx";
            try
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                ViewBag.profile = profileName;
                var irfList = new List<IRF>();
                if (HttpContext.Session.GetString("SkillChannelDetailsFilters") == null)
                {
                    if (type == "IRF")
                    {
                        irfList = (await _irfService.GetAllIRFDetailsIRF(DateTime.Now.AddDays(-30), DateTime.Now.AddDays(7), profileName, emailid))?.ToList();
                    }
                    else
                    {
                        irfList = (await _irfService.GetAllIRFDetails(DateTime.Now.AddDays(-30), DateTime.Now.AddDays(7)))?.ToList();
                    }
                }
                else
                {
                    var filter = JsonConvert.DeserializeObject<SkillChannelDetailsFilter>(HttpContext.Session.GetString("SkillChannelDetailsFilters"));
                    if (filter.StartDate != null && filter.EndDate != null)
                    {
                        if (type == "IRF")
                        {
                            irfList = (await _irfService.GetAllIRFDetailsIRF(filter.StartDate.Value, filter.EndDate.Value, profileName, emailid))?.ToList();
                        }
                        else
                        {
                            irfList = (await _irfService.GetAllIRFDetails(filter.StartDate.Value, filter.EndDate.Value))?.ToList();
                        }
                    }
                    else
                    {
                        if (type == "IRF")
                        {
                            irfList = (await _irfService.GetAllIRFDetailsIRF(DateTime.Now.AddDays(-30), DateTime.Now.AddDays(7), profileName, emailid))?.ToList();
                        }
                        else
                        {
                            irfList = (await _irfService.GetAllIRFDetails(DateTime.Now.AddDays(-30), DateTime.Now.AddDays(7)))?.ToList();
                        }
                    }
                }

                Sheet.Cells["A1:AI1"].Style.Font.Bold = true;
                Sheet.Cells["A1:AI1"].Style.Font.Size = 12;
                Sheet.Cells["A1:AI1"].Style.WrapText = true;
                int startColumn = 1; int endColumn = 15;
                for (int a = startColumn; a <= endColumn; a++)
                {
                    Sheet.Column(a).Width = 40;
                }
                Sheet.Cells[1, 1].Value = "Channel Owner";
                Sheet.Cells[1, 2].Value = "Skill Channels";
                Sheet.Cells[1, 3].Value = "Total Candidates";
                Sheet.Cells[1, 4].Value = "Completed Interviews";
                Sheet.Cells[1, 5].Value = "Selected Interviews";
                Sheet.Cells[1, 6].Value = "Rejected Interviews";
                Sheet.Cells[1, 7].Value = "Not Scheduled Count";
                Sheet.Cells[1, 8].Value = "Rescheduled Count";
                Sheet.Cells[1, 9].Value = "Future Scheduled Count";
                if (type == "IRF")
                {
                    Sheet.Cells[1, 10].Value = "Client";
                }
                int i = 2;
                if (irfList.Count() > 0)
                {
                    foreach (var item in irfList)
                    {
                        Sheet.Cells[i, 1].Value = item.RisebirdHREmail;
                        Sheet.Cells[i, 2].Value = item.IRFId;
                        Sheet.Cells[i, 3].Value = item.CandidateCount;
                        Sheet.Cells[i, 4].Value = item.CompletedInterviews;
                        Sheet.Cells[i, 5].Value = item.SelectedMeetings;
                        Sheet.Cells[i, 6].Value = item.RejectedMeetings;
                        Sheet.Cells[i, 7].Value = item.NotScheduledCount;
                        Sheet.Cells[i, 8].Value = item.ReScheduledCount;
                        Sheet.Cells[i, 9].Value = item.FutureScheduledMeetings;
                        if (type == "IRF")
                        {
                            Sheet.Cells[i, 10].Value = item.ClientCompName;
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

        public string AutoGenerateSchedulingLink(DateTime eventStartdate, string SkillChannel, string EventName, string meetingPlatform = null)
        {
            var model_Second = new List<daytimes_second>();
            var sun = new List<perdaytime>();
            var mon = new List<perdaytime>();
            sun.Add(new perdaytime { Start = "8", End = "23" });
            mon.Add(new perdaytime { Start = "6", End = "9" });
            mon.Add(new perdaytime { Start = "18", End = "23" });
            model_Second.Add(new daytimes_second { Daytype = "sunday", dayCheck = true, dayvalue = sun });
            model_Second.Add(new daytimes_second { Daytype = "monday", dayCheck = true, dayvalue = mon });
            model_Second.Add(new daytimes_second { Daytype = "tuesday", dayCheck = true, dayvalue = mon });
            model_Second.Add(new daytimes_second { Daytype = "wednesday", dayCheck = true, dayvalue = mon });
            model_Second.Add(new daytimes_second { Daytype = "thursday", dayCheck = true, dayvalue = mon });
            model_Second.Add(new daytimes_second { Daytype = "friday", dayCheck = true, dayvalue = mon });
            model_Second.Add(new daytimes_second { Daytype = "saturday", dayCheck = true, dayvalue = sun });
            var model_report = new daytimes_report();
            model_report.setvalue = model_Second;
            model_report.StartDate = eventStartdate.AddHours(12);
            model_report.EndDate = eventStartdate.AddDays(60);
            model_report.EventName = EventName;
            model_report.SkillChannel = SkillChannel;
            model_report.duration = 60;
            model_report.Concurrency = 10;
            model_report.BufferTime = 18;
            model_report.TimeSlotDays = 6;
            model_report.MeetingPlatform = meetingPlatform;
            var eventid = _interviewSchedulerservice.AutoCreateEvent(model_report).Result;
            var model = _interviewSchedulerservice.FetchEventById(eventid).Result;
            if (model != null)
            {
                var eventurl = "https://" + Request.Host.ToString() + "/InterviewScheduler/ScheduleEvent?id=" + model.Uniqueid;
                return eventurl;
            }
            return "";
        }
        [HttpGet]
        public async Task<JsonResult> CheckIntervieweravailbility(int irfId, string IRFName, int assessmentGroupId)
        {
            try
            {
                //var model = await _irfService.GetAllIRFAssessmentInterviewerMembers(irfId);
                var model = await _irfService.GetAvailableAssessmentGroupInterviewerMembers(assessmentGroupId, irfId);
                //var interviewers = await _irfRepository.GetAvailableAssessmentGroupInterviewerMembers(Convert.ToInt16(irf.AssessmentGroupId));

                //model = model.Where(x => x.Type == "Interviewer").ToList();
                if (model !=null && model.Count() > 0)
                {
                    if(model.FirstOrDefault().AvailbilityHour >= 3600 || model.FirstOrDefault().AvailbilityHour == 0)
                    {
                        await _irfService.UpdateIntervieweravailbilityDetails(irfId);
                        var skills = await _irfService.GetTechSkillswithWeightage(irfId);
                        StringBuilder sb = new StringBuilder();
                        sb.Append("<table style='width: 100%; text-align: center;border-collapse:collapse'>");
                        sb.Append("<tr><td style='border: 1px solid black'>Skills</td><td style='border: 1px solid black'>Weightage</td></tr>");
                        foreach (var item in skills)
                        {
                            sb.Append("<tr><td style='border: 1px solid black'>");
                            sb.Append(item.SkillName);
                            sb.Append("</td>");
                            sb.Append("<td style='border: 1px solid black'>");
                            sb.Append(item.Weightage);
                            sb.Append("</td></tr>");
                        }
                        sb.Append("</table>");
                        foreach (var item in model)
                        {
                            if (!string.IsNullOrEmpty(item.EmailId))
                            {
                                //string yesbutton = "https://portal.risebird.io/IRF/UpdateCheckIntervieweravailbilityStatus?status=1&uniqueid=" + item.uniqueid + "&Irfid=" + irfId;
                                //string nobutton = "https://portal.risebird.io/IRF/UpdateCheckIntervieweravailbilityStatus?status=2&uniqueid=" + item.uniqueid + "&Irfid=" + irfId;

                                var encryptedDate = await _encryptionHelper.EncryptDateTime(DateTime.Now);
                                string yesbutton = "https://portal.risebird.io/IRF/UpdateInterviewerAvailbilityStatusForIRF?status=1&uniqueid=" + item.uniqueid + "&Irfid=" + irfId + "&dec=" + encryptedDate;
                                string nobutton = "https://portal.risebird.io/IRF/UpdateInterviewerAvailbilityStatusForIRF?status=2&uniqueid=" + item.uniqueid + "&Irfid=" + irfId + "&dec=" + encryptedDate;
                                string dropMyProfileButton = "https://portal.risebird.io/IRF/UpdateInterviewerAvailbilityStatusForIRF?status=4&uniqueid=" + item.uniqueid + "&Irfid=" + irfId + "&dec=" + encryptedDate;

                                Mail _mail = new Mail
                                {
                                    TO = item.EmailId,
                                    Subject = "Current Availability (2 days) check for  " + item.BasicSkill + " Risebird Interviews"
                                };
                                var emailresult = await _notificationHelper.SendCheckIntervieweravailbility(_mail, item.InterviewerName, yesbutton, nobutton, sb.ToString(), item.FlatRate, dropMyProfileButton, item.BasicSkill, irfId);

                                WhatsAppVM _whatsAppVM = new WhatsAppVM();
                                _whatsAppVM.Emailid = item.EmailId;
                                _whatsAppVM.PrimarySkill = item.BasicSkill;
                                //_whatsAppVM.MessageValue = "";
                                _whatsAppVM.PerSuccessRate = item.FlatRate.ToString();//interviewer.PerSuccessRate.ToString();
                                //_whatsAppVM.Interviewduration = "";
                                //_whatsAppVM.buttonlink = yesbutton.Replace("https://portal.risebird.io/IRF/UpdateCheckIntervieweravailbilityStatus?status=1&uniqueid=", "");
                                _whatsAppVM.buttonlink = yesbutton.Replace("https://portal.risebird.io/IRF/", "");
                                _whatsAppVM.Event = "SkillChannel Interviewer Availbility";
                                await _whatsAppHelper.WhatsAppSkillChannelInterviewerAvailbility(_whatsAppVM);
                            }
                        }
                        return Json("Emails sent successfully.");
                    }
                    else
                    {
                        return Json("Emails have already been sent for the Assessment Group Interviewers mapped to this skill channel. Please try notifying(email) at least 1 hour after the last successful notification attempt.");

                    }
                }
                else
                {
                    return Json("No globally interviewer available in the assessment group. Emails not sent.");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Fail");
            }
        }
        [HttpGet]
        public async Task<JsonResult> UpdateCheckIntervieweravailbilityStatus(int status, string uniqueid, int Irfid)
        {
            if (await _meetingService.UpdateCheckIntervieweravailbilityStatus(status, uniqueid, Irfid))
            {
                return Json("Status updated successfully");
            }
            else
            {
                return Json("Failed to update status, Please try again");
            }
        }

        [HttpGet]
        public async Task<JsonResult> UpdateInterviewerAvailbilityStatusForIRF(int status, string uniqueid, int Irfid, string dec)
        {
            //check link expiry befire hitting db

            try
           {
                dec = dec.Replace(" ", "+");
                var timeOfCommunication = await _encryptionHelper.DecryptDateTime(dec);
                if (timeOfCommunication != null && (DateTime.Now - timeOfCommunication.Value).TotalMinutes <= 60)
                {
                    if (await _meetingService.UpdateInterviewerAvailbilityStatusForIRF(status, uniqueid, Irfid))
                    {
                        return Json("Status updated successfully.");
                    }
                    else
                    {
                        return Json("Failed to update status, Please try again");
                    }
                }
                else
                {
                    return Json("Link Expired.");
                }

                //if (await _meetingService.UpdateInterviewerAvailbilityStatusForIRF(status, uniqueid, Irfid))
                //{
                //    return Json("Status updated successfully.");
                //}
                //else
                //{
                //    return Json("Failed to update status, Please try again");
                //}
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Failed to update status, Please try again");
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<JsonResult> StatsFlagForIRF(int IRFId)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                ViewBag.profile = profileName;
                var irfList = new List<IRF>();
                irfList = (await _irfService.GetAllIRFDetailsByIRFId(IRFId))?.ToList();
                return Json(irfList.FirstOrDefault());
            }
            else
            {
                var irfList = new IRF();
                return Json(irfList);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetAssessmentGroupInterviewerMembers(int Assessmentid)
        {
            try
            {
                ViewBag.Type = "ALL";
                var model = await _irfService.GetAssessmentGroupInterviewerMembers(Assessmentid);
                model = model.Select(x => { x.AvailableStatus = ((InterviewerAvailabilityStatus)Convert.ToInt32(x.AvailableStatus)).GetDescription(); return x; }).ToList();
                return PartialView("IRFInterviewerMembersPartial", model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetAssessmentGroupIRFList(int Assessmentid)
        {
            try
            {
                var model = await _irfService.GetAssessmentGroupIRFList(Assessmentid);
                return PartialView("GetAssessmentGroupIRFList", model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [HttpPost]
        public JsonResult BroadcastIRFmembers(string message, string subject, int irfid, int interviewId = 0, bool shouldSendEmail = true)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");


                    if (profileName == "Admin" || profileName == "Client" || profileName == "SupportAdmin" || profileName == "SeniorHR")
                    {
                        var emailId = HttpContext.Session.GetString("email")?.Trim();
                        emailId = emailId.Replace("\"", "");
                        var model = new BroadcastModel
                        {
                            Message = message,
                            Subject = subject
                        };
                        model.IRFId = irfid;
                        model.SentBy = emailId;
                        model.Profile = profileName;
                        string result = string.Empty;
                        if (interviewId > 0 && !shouldSendEmail)
                        {
                            model.InterviewId = interviewId;
                            result = _meetingService.StoreClientBroadcastMessage(model).Result;

                        }
                        else
                        {
                            result = _meetingService.BroadcastIRFmembers(model).Result;

                        }
                        return Json(result);
                    }
                    else
                    {
                        return Json("You are not authorized to broadcast.");
                    }
                }
                else
                {
                    return Json("Broadcast failed, Please try again.");
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Broadcast failed, Please try again.");
            }
        }
        [HttpPost]
        public JsonResult FetchIRFAudioLink(int irfid)
        {
            var audiofilelink = _irfService.FetchIRFAudioLink(irfid);
            return Json(audiofilelink);
        }
        [HttpPost]
        public async Task<string> UploadAudio()
        {
            var file = Request.Form.Files;

            var fileName = ContentDispositionHeaderValue.Parse(file[0].ContentDisposition).FileName.Trim('"');
            var Irfid = fileName.Split("_")[2];
            string uploadedfbaudioFileURL = string.Empty;
            if (file != null)
            {
                var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(file[0]);
                if (fileMimeType == "audio/basic" || fileMimeType == "audio/mpeg" || fileMimeType == "audio/x-wav" || fileMimeType == "video/x-msvideo")
                {
                    await _azureStorageHelper.InsertBlob(fileName, "audios", file[0]);
                    var containerRef = _azureStorageHelper.GetCloudBlobContainer("audios");
                    var blob = containerRef.GetBlockBlobReference(fileName);
                    uploadedfbaudioFileURL = blob.Uri.ToString();
                }
                else
                {
                    return null;
                }
            }
            _irfService.SaveIRFAudioLink(Convert.ToInt32(Irfid), uploadedfbaudioFileURL);
            return uploadedfbaudioFileURL;
        }
        [HttpPost]
        public async Task<JsonResult> GetAssessmentGroupQuestionList(int Assessmentid)
        {
            try
            {
                var model = await _irfService.GetAssessmentGroupQuestionList(Assessmentid);
                return Json(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                var model = new List<AssessmentIRFListVM>();
                return Json(model);
            }
        }
        //    }
        //}

        [HttpGet]
        public async Task<SelectList> GetAssessmentGroupsByCategory(int categoryId)
        {
            var list = new List<SelectListItem>();
            var assessmentGroups = await _skillService.GetAssessmentGroupsByCategory(categoryId);
            if (assessmentGroups != null && assessmentGroups.Count() > 0)
            {
                return new SelectList(assessmentGroups, "id", "AssessmentGroup");
            }
            else
            {
                list.Add(new SelectListItem { Text = "No Assessment Groups for this Category", Value = "" });

            }
            return new SelectList(list, "Value", "Text");
        }

        [AcceptVerbs("Get", "Post")]
        [AllowAnonymous]
        public async Task<IActionResult> IsClonableTemplateNameInUse(string name)
        {
            bool inUse = await _irfService.IsClonableTemplateNameInUse(name.Replace("\r", "").Replace("\n", ""));

            if (!inUse)
            {
                return Json(true);
            }
            else
            {
                return Json($"Template Name '{name}' is already in use.");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Create(int templateId, string templateName)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null || HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        var model = new IRFVm();
                        if (profileName == "Client")
                        {
                            model = await _clonableSkillChannelTemplateService.GetTemplateDataForPopulatingSkillChannel(templateId, emailid);
                            model.IRFId = GenerateSkillChannelName(model.AssessmentGroupName, model.Clients.Where(x => x.Value == model.ClientId.ToString())?.FirstOrDefault()?.Text);
                        }
                        else
                        {
                            model = await _clonableSkillChannelTemplateService.GetTemplateDataForPopulatingSkillChannel(templateId);
                        }
                        model.Profile = profileName;
                        model.TemplateName = templateName;
                        var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                                         select new { ID = (int)s, Name = s.GetDescription() };
                        ViewBag.Heuristicsscorelist = new SelectList(Heuristics, "ID", "Name");
                        var companyTypes = from CompanyType s in Enum.GetValues(typeof(CompanyType))
                                           select new { ID = (int)s, Name = s.GetDescription() };
                        model.CompanyTypeList = new SelectList(companyTypes, "ID", "Name");
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
                TempData["SuccessErrorCloneTemplateMessage"] = "Failed to redirect! Kindly contact the Coordinator.";
                return RedirectToAction("Index", "Template");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(IRFVm model)
        {
            string emailId = string.Empty;
            try
            {
                var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                                 select new { ID = (int)s, Name = s.GetDescription() };
                ViewBag.Heuristicsscorelist = new SelectList(Heuristics, "ID", "Name");
                var companyTypes = from CompanyType s in Enum.GetValues(typeof(CompanyType))
                                   select new { ID = (int)s, Name = s.GetDescription() };
                model.CompanyTypeList = new SelectList(companyTypes, "ID", "Name");
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {


                        //check duplicates for client
                        bool isValid = true;
                        var clientCollection = model.ClientCollection.Select(x => (x.ClientUserId));
                        if (clientCollection.Count() != clientCollection.Distinct().Count())
                        {
                            //duplicates exist
                            isValid = false;
                            ModelState.AddModelError("", "Please remove duplicate entries from Client's list.");
                        }
                        if (!model.InterviewerCollection.Any(x => x.IsActive))
                        {
                            isValid = false;
                            ModelState.AddModelError("", "Please select atleast one Interviewer from the list.");
                        }

                        if (!isValid)
                        {
                            if (profileName == "Client")
                            {
                                await _clonableSkillChannelTemplateService.PopulateDropDownsForSkillChannel(model, emailId);

                            }
                            else
                            {
                                await _clonableSkillChannelTemplateService.PopulateDropDownsForSkillChannel(model);

                            }
                            model.IsValidIRFFromTemplate = false;
                            return View(model);
                        }
                        string uploadedFileURL = string.Empty;

                        if (model.JDFile != null)
                        {
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.JDFile);
                            if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                                || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                            {
                                await _azureStorageHelper.InsertBlob(model.JDFile.FileName, "resumes", model.JDFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                var blob = containerRef.GetBlockBlobReference(model.JDFile.FileName);
                                uploadedFileURL = blob.Uri.ToString();
                                model.JDLink = uploadedFileURL;
                            }

                        }

                        //Start: Auto Generate Scheduling Link 
                        model.StartDate = DateTime.Now;
                        model.EndDate = DateTime.Now.AddMonths(3);

                        string _schedulinglink = AutoGenerateSchedulingLink(model.StartDate, model.IRFId, model.JDTiltle + "_Scheduling", model.MeetingPlatform);

                        model.CalendlyLink = _schedulinglink;
                        //End: Auto Generate Scheduling Link
                        if (await _irfService.CreateSkillChannelFromTemplate(model, emailId, profileName))
                        {
                            TempData["SuccessErrorMessage"] = $"Skills Channel '{model.IRFId}' created Successfully!";
                            TempData["SuccessErrorMessage2"] = model.CalendlyLink;
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorCloneTemplateMessage"] = $"Failed to create Skills Channel from {model.TemplateName}!";
                            return RedirectToAction("Index", "Template");
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
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, $"Message : {ex.Message}; StackTrace : {ex.StackTrace};  Source : {ex.Source}; TemplateName : {model.TemplateName}; userEmailId : {emailId}");
                TempData["SuccessErrorCloneTemplateMessage"] = "Failed to create Skills Channel!";
                return RedirectToAction("Index", "Template");
            }
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> CreateInterviewLink(IRFVm model)
        {
            string emailId = string.Empty;
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    if (/*profileName == "Admin" || profileName == "HR" ||*/ profileName == "Client" /*|| profileName == "Recruiter" || profileName == "CuratedUser" || profileName == "Finance" || profileName == "SeniorHR"*/)
                    {

                        string uploadedFileURL = string.Empty;
                        if (model.ClientId == 0)
                            model.ClientId = await _clientService.GetClientByEmailId(emailId);
                        if (model.JDFile != null)
                        {


                            int virusScanResult = 0;
                            if (profileName == "Client")
                            {
                                using (var ms = new MemoryStream())
                                {
                                    model.JDFile.CopyTo(ms);

                                    //VirusTotal virusTotal = new VirusTotal("30368672cc1d7fa0497a0d2c640544ade03ad455600d24c220bfd41c83b5e6d5");
                                    VirusTotal virusTotal = new VirusTotal(_configuration["VirusTotal:ApiKey"]);

                                    //Use HTTPS instead of HTTP
                                    virusTotal.UseTLS = true;

                                    //byte[] eicar = Encoding.ASCII.GetBytes(@"X5O!P%@AP[4\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*");
                                    //VirusTotalNet.Results.FileReport fileReport = await virusTotal.GetFileReportAsync(eicar);//ms

                                    VirusTotalNet.Results.FileReport fileReport = await virusTotal.GetFileReportAsync(ms);
                                    bool hasFileBeenScannedBefore = fileReport.ResponseCode == FileReportResponseCode.Present;

                                    if (hasFileBeenScannedBefore)
                                    {
                                        virusScanResult = fileReport.Positives;
                                    }
                                    else
                                    {
                                        //var fileResults = await virusTotal.ScanFileAsync(eicar, fileName);//ms
                                        var fileResults = await virusTotal.ScanFileAsync(ms, model.JDFile.FileName);
                                        virusScanResult = fileReport.Positives;
                                    }

                                    if (virusScanResult > 0)
                                    {
                                        TempData["SuccessErrorCloneTemplateMessage"] = $"File upload failed. Virus Detected! Failed to create Skills Channel from {model.TemplateName}!";
                                        return RedirectToAction("Index", "Template");
                                    }

                                }
                            }

                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.JDFile);
                            if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                                || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint" || fileMimeType == "text/plain")
                            {
                                await _azureStorageHelper.InsertBlob(model.JDFile.FileName, "resumes", model.JDFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                var blob = containerRef.GetBlockBlobReference(model.JDFile.FileName);
                                uploadedFileURL = blob.Uri.ToString();
                                model.JDLink = uploadedFileURL;
                            }

                        }

                        //Start: Auto Generate Scheduling Link 
                        model.StartDate = DateTime.Now;
                        model.EndDate = DateTime.Now.AddMonths(3);

                        //model.MeetingPlatform = await _clonableSkillChannelTemplateService.GetClonableSkillChannelMeetingPlatformById(model.CreatedFromTemplateId);

                        string _schedulinglink = AutoGenerateSchedulingLink(model.StartDate, model.IRFId, model.JDTiltle + "_Scheduling", model.MeetingPlatform);

                        model.CalendlyLink = _schedulinglink;
                        //End: Auto Generate Scheduling Link
                        if (await _irfService.CreateSkillChannelUsingTemplateByInterviewLinkPopUp(model, emailId, profileName))
                        {
                            TempData["SuccessErrorMessage"] = $"Skills Channel '{model.IRFId}' created Successfully!";
                            TempData["SuccessErrorMessage2"] = model.CalendlyLink;
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorCloneTemplateMessage"] = $"Failed to create Skills Channel from {model.TemplateName}!";
                            return RedirectToAction("Index", "Template");
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
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, $"Message : {ex.Message}; StackTrace : {ex.StackTrace};  Source : {ex.Source}; TemplateName : {model.TemplateName}; userEmailId : {emailId}");
                //await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorCloneTemplateMessage"] = "Failed to create Skills Channel!";
                return RedirectToAction("Index", "Template");
            }
        }
        public string GenerateRandomstring()
        {
            //4char date or month and 1char secondUnique
            DateTime _dt = DateTime.Now;
            var month = _dt.Month.ToString();
            if (month.Length == 1)
            {
                month = "0" + month;
            }
            var _date = _dt.Day.ToString();
            if (_date.Length == 1)
            {
                _date = "0" + _date;
            }
            var sec = digSum(_dt.Second);
            return _date + month + sec.ToString();
        }

        public int digSum(int n)
        {
            int sum = 0;
            while (n > 0 || sum > 9)
            {
                if (n == 0)
                {
                    n = sum;
                    sum = 0;
                }
                sum += n % 10;
                n /= 10;
            }
            return sum;
        }
        //public string GenerateRandomstring()
        //{
        //    var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        //    var stringChars = new char[5];
        //    var random = new Random();
        //    for (int i = 0; i < stringChars.Length; i++)
        //    {
        //        stringChars[i] = chars[random.Next(chars.Length)];
        //    }
        //    var finalString = new string(stringChars);
        //    return finalString;
        //}
        public string GenerateSkillChannelName(string assessmentGroupName, string clientCompanyName)
        {
            bool flag = false;
            string skillChannelName = string.Empty;
            while (!flag)
            {
                if (!string.IsNullOrEmpty(clientCompanyName))
                {
                    skillChannelName = clientCompanyName.Substring(0, 3);
                    skillChannelName = skillChannelName + assessmentGroupName.Replace(" ", "").Substring(0, 2) + GenerateRandomstring();
                }
                //flag = CheckUniqueness(skillChannelName, false);
                flag = _irfService.CheckUniqueness(skillChannelName, false);
            }
            skillChannelName = skillChannelName.Replace(".", "a").Replace(" ", "-").Replace("+", "s").Replace("&", "o");
            return skillChannelName;
        }

        /// <summary>
        /// Edit Skill Channel created from Template exclusively for Client.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Edit(IRFVm model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    if (profileName == "Client")
                    {


                        //check duplicates for client
                        bool isValid = true;
                        var clientCollection = model.ClientCollection.Select(x => (x.ClientUserId));
                        if (clientCollection.Count() != clientCollection.Distinct().Count())
                        {
                            //duplicates exist
                            isValid = false;
                            ModelState.AddModelError("", "Please remove duplicate entries from Client's list.");
                        }
                        if (!model.InterviewerCollection.Any(x => x.IsActive))
                        {
                            isValid = false;
                            ModelState.AddModelError("", "Please select atleast one Interviewer from the list.");
                        }
                        if (!isValid)
                        {
                            await _clonableSkillChannelTemplateService.PopulateDropDownsForSkillChannel(model, emailId);
                            return View(model);
                        }
                        string uploadedFileURL = string.Empty;

                        if (model.JDFile != null)
                        {
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.JDFile);
                            if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                                || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                            {
                                await _azureStorageHelper.InsertBlob(model.JDFile.FileName, "resumes", model.JDFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                var blob = containerRef.GetBlockBlobReference(model.JDFile.FileName);
                                uploadedFileURL = blob.Uri.ToString();
                                model.JDLink = uploadedFileURL;
                            }

                        }

                        if (string.IsNullOrEmpty(model.JDLink))
                        {
                            model.JDLink = model.OldJDLink;
                        }

                        //Start: Auto Generate Scheduling Link 

                        if (model.JDTiltle != model.OldJDTitle)
                        {
                            var _schedulinglink = AutoGenerateSchedulingLink(model.StartDate, model.IRFId, model.JDTiltle + "_Scheduling");
                            model.CalendlyLink = _schedulinglink;
                        }

                        //End: Auto Generate Scheduling Link
                        if (await _irfService.EditSkillChannelCreatedFromTemplate(model, emailId))
                        {
                            TempData["SuccessErrorMessage"] = $"Skills Channel '{model.IRFId}' created Successfully!";
                            TempData["SuccessErrorMessage2"] = model.CalendlyLink;
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = $"Failed to update Skills Channel '{model.IRFId}'!";
                            return RedirectToAction("Index");
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
                TempData["SuccessErrorMessage"] = $"Failed to update Skills Channel '{model.IRFId}'!";
                return RedirectToAction("Index");
            }
        }
        //[Authorize]
        //[HttpPost]
        //public async Task<JsonResult> SkillChannelDetailsIRFCount(string HrEmail)
        //{
        //    var irfList = new List<IRF>();
        //    if (HttpContext.Session.GetString("SkillChannelDetailsFilters") == null)
        //    {
        //        irfList = (await _irfService.GetAllIRFDetailsIRFCount(DateTime.Now.AddDays(-30), DateTime.Now.AddDays(7), HrEmail))?.ToList();
        //    }
        //    else
        //    {
        //        var filter = JsonConvert.DeserializeObject<SkillChannelDetailsFilter>(HttpContext.Session.GetString("SkillChannelDetailsFilters"));
        //        if (filter.StartDate != null && filter.EndDate != null)
        //        {
        //            irfList = (await _irfService.GetAllIRFDetailsIRFCount(filter.StartDate.Value, filter.EndDate.Value, HrEmail))?.ToList();
        //        }
        //        else
        //        {
        //            irfList = (await _irfService.GetAllIRFDetailsIRFCount(DateTime.Now.AddDays(-30), DateTime.Now.AddDays(7), HrEmail))?.ToList();
        //        }
        //    }
        //    return Json(irfList);
        //}

        [HttpGet]
        public async Task<IActionResult> PublicSkillChannelDetails(string emailid, bool isClientLogin = false, string uniqueid = null)
        {
            ViewBag.emailid = emailid;
            //if(!string.IsNullOrEmpty(uniqueid))
            //{
            //    ViewBag.uniqueid = uniqueid;
            //}
            ViewBag.uniqueid = uniqueid;

            ViewBag.IsClientLogin = isClientLogin;
            ViewBag.Type = "IRF";
            if (!isClientLogin)
            {
                ViewBag.PageStatus = "Public";
            }
            HttpContext.Session.SetString("email", JsonConvert.SerializeObject(emailid));
            var profileName = "Client";
            ViewBag.profile = profileName;
            var irfList = new List<IRF>();
            if (HttpContext.Session.GetString("SkillChannelDetailsFilters") == null)
            {
                irfList = (await _irfService.GetAllIRFDetailsIRF(DateTime.Now.AddDays(-30), DateTime.Now.AddDays(7), profileName, emailid))?.ToList();
            }
            else
            {
                var filter = JsonConvert.DeserializeObject<SkillChannelDetailsFilter>(HttpContext.Session.GetString("SkillChannelDetailsFilters"));
                if (filter.StartDate != null && filter.EndDate != null)
                {
                    irfList = (await _irfService.GetAllIRFDetailsIRF(filter.StartDate.Value, filter.EndDate.Value, profileName, emailid))?.ToList();
                }
                else
                {
                    irfList = (await _irfService.GetAllIRFDetailsIRF(DateTime.Now.AddDays(-30), DateTime.Now.AddDays(7), profileName, emailid))?.ToList();
                }
            }
            return View("SkillChannelDetails", irfList);
        }
        [HttpPost]
        public async Task<IActionResult> PublicSkillChannelDetails(SkillChannelDetailsFilter filter, string emailid, bool isClientLogin = false, string uniqueid = null)
        {
            if (filter != null)
            {
                HttpContext.Session.SetString("SkillChannelDetailsFilters", JsonConvert.SerializeObject(filter));
            }
            ViewBag.emailid = emailid;
            ViewBag.uniqueid = uniqueid;
            ViewBag.IsClientLogin = isClientLogin;
            var profileName = "Client";
            ViewBag.profile = profileName;
            ViewBag.Type = "IRF";
            ViewBag.PageStatus = "Public";
            var irfList = new List<IRF>();
            if (filter.StartDate != null && filter.EndDate != null)
            {
                irfList = (await _irfService.GetAllIRFDetailsIRF(filter.StartDate.Value, filter.EndDate.Value, profileName, emailid))?.ToList();
            }
            else
            {
                irfList = (await _irfService.GetAllIRFDetailsIRF(DateTime.Now.AddDays(-30), DateTime.Now.AddDays(7), profileName, emailid))?.ToList();
            }
            return View("SkillChannelDetails", irfList);
        }
        [HttpGet]
        public async Task<IActionResult> ResetPublicSkillChannelDetailsFilter(string emailid, bool isClientLogin = false, string uniqueid = null)
        {
            try
            {
                HttpContext.Session.Remove("SkillChannelDetailsFilters");
                return RedirectToAction("PublicSkillChannelDetails", new { emailid = emailid, uniqueid = uniqueid, isClientLogin = isClientLogin });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        [HttpPost]
        public async Task<JsonResult> GetClientDetails(int clientid)
        {
            var _clientdetails = await _clientService.GetClient(clientid);
            return Json(_clientdetails);
        }
        [HttpPost]
        public async Task<JsonResult> ShareVerificationToInterviewer(int Assessmentid)
        {
            try
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var model = await _irfService.GetAssessmentGroupInterviewerMembers(Assessmentid);
                var Uniquedate = model?.FirstOrDefault();
                MCQVMShare _list = new MCQVMShare();
                _list.status = 0;
                _list.MCQSet = Uniquedate.Title;
                _list.ClientHR = emailid;
                _list.ApplyingCompany = "";
                _list.WhoShared = emailid;
                if (Uniquedate.AssessmentVerificationType == "MCQ")
                {
                    var Emailids = string.Join(",", model.Select(x => x.EmailId).Distinct());
                    string currenturl = "https://" + Request.Host.ToString() + "/MCQ/SkillTest?Id=" + Uniquedate.MCQSetsValue;
                    var botlinkslist = new MCQBotResponse();
                    currenturl = currenturl + "&uniqueid=322";
                    var aadharverificationflag = "Y";
                    var _interviewerflag = "N";
                    botlinkslist = await GenerateBotLink(Emailids.Replace(";", ","), aadharverificationflag, currenturl, _interviewerflag);
                    foreach (var item in Emailids.Split(","))
                    {
                        var date1 = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");
                        var _testurl = botlinkslist.emailBotUrls.Where(x => x.email == item).FirstOrDefault().botTestLink;
                        var roomId = _testurl.Split("/").LastOrDefault();
                        //Send Mail to Interviewer
                        Mail _mail = new Mail
                        {
                            TO = item,
                            Subject = "Invitation for " + Uniquedate.Title + " : " + "Risebird"
                        };
                        var result = await _notificationHelper.SharedBotTestLink(_mail, Uniquedate.Title, "15", _testurl, "Risebird");

                        _list.PartitionKey = date1;
                        _list.MCQId = Uniquedate.MCQSetsValue;
                        _list.EmailAddress = item.Trim();
                        _list.roomId = roomId;
                        _list.Expiretime = 48;
                        _list.ShareType = true;
                        _list.botactive = true;
                        await _mCQService.AddMCQSetShareList(_list);
                        await _interviewerService.AddInterviewerAssesssmentDetails(Assessmentid, emailid);
                    }
                }
                else if (Uniquedate.AssessmentVerificationType == "Code")
                {
                    var Emailids = string.Join(",", model.Select(x => x.EmailId).Distinct());
                    foreach (var item in Emailids.Split(","))
                    {
                        var codeInviteVM = new CodeInviteVM();
                        codeInviteVM.userName = emailid;
                        codeInviteVM.candidateEmail = item;
                        codeInviteVM.programmingLanguage = Uniquedate.CodeLanguage;
                        codeInviteVM.experience = 3;
                        codeInviteVM.inviteStartTime = DateTime.Now;
                        codeInviteVM.inviteEndTime = DateTime.Now.AddDays(2);
                        codeInviteVM.difficultyLevel = "standard";
                        await GenerateCodeInviteURL(codeInviteVM);
                        await _interviewerService.SendInviteCode(codeInviteVM, emailid);
                        await _interviewerService.AddInterviewerAssesssmentDetails(Assessmentid, emailid);
                        //Send mail to interviewer
                        Mail _mail = new Mail
                        {
                            TO = item,
                            Subject = "Invitation for " + Uniquedate.CodeLanguage + " : " + "Risebird"
                        };
                        var result = await _notificationHelper.SharedBotTestLink(_mail, Uniquedate.CodeLanguage, "15", codeInviteVM.botURL, "Risebird");
                    }
                }
                return Json("Success");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Fail");
            }
        }
        public async Task<MCQBotResponse> GenerateBotLink(string emailds, string AadhaarVerificationRequired, string MCQTestlink, string _interviewerflag)
        {
            var model = new MCQBotRequest();
            model.tenantUuid = "e5f37005-42a3-4510-b211-0a3c0ee675b7";
            model.email = emailds;
            model.aadharVerification = AadhaarVerificationRequired;
            model.faceVerification = "Y";
            model.interviewerTraining = _interviewerflag;
            model.companyEmailVerification = "N";
            model.videoBio = "N";
            model.careerQuestions = "N";
            model.testLink.mcq = MCQTestlink;
            model.testLink.code = "";
            var client = new RestClient("https://bot.risebird.io/api/v1/bot/invite");
            var request = new RestRequest("https://bot.risebird.io/api/v1/bot/invite", Method.Post);
            request.AddHeader("content-type", "application/json");
            request.AddJsonBody(model);
            RestResponse response = await client.ExecuteAsync(request);
            var convertedResponse = JsonConvert.DeserializeObject<MCQBotResponse>(response.Content);
            return convertedResponse;
        }
        public async Task GenerateCodeInviteURL(CodeInviteVM _codeinvite)
        {
            var model = new CodeInviteURLRequest();
            model.userName = _codeinvite.userName;
            model.candidateEmail = _codeinvite.candidateEmail;
            model.programmingLanguage = _codeinvite.programmingLanguage;
            model.experience = _codeinvite.experience;
            model.inviteStartTime = _codeinvite.inviteStartTime.ToString("yyyy-MM-dd HH:mm:ss");
            model.inviteEndTime = _codeinvite.inviteEndTime.ToString("yyyy-MM-dd HH:mm:ss");
            model.zone = "IST";
            model.difficultyLevel = _codeinvite.difficultyLevel.ToLower();
            model.assignmentType = "QUICK";
            var client = new RestClient(codeapiendpoint + "invited-assignment");
            var request = new RestRequest(codeapiendpoint + "invited-assignment", Method.Post);
            request.AddHeader("content-type", "application/json");
            request.AddJsonBody(model);
            RestResponse response = await client.ExecuteAsync(request);
            var convertedResponse = JsonConvert.DeserializeObject<CodeInviteURLResponse>(response.Content);
            _codeinvite.invite_UrL = convertedResponse.inviteUrl;
            _codeinvite.inviteId = convertedResponse.inviteUrl.Split("/").LastOrDefault();
            var botcodeurl = await GenerateBotCodeLink(_codeinvite.candidateEmail, _codeinvite.invite_UrL);
            if (botcodeurl != null)
                _codeinvite.botURL = botcodeurl.emailBotUrls.FirstOrDefault().botTestLink;
        }
        public async Task<MCQBotResponse> GenerateBotCodeLink(string emailds, string CodeTestLink)
        {
            var model = new MCQBotRequest();
            model.tenantUuid = "e5f37005-42a3-4510-b211-0a3c0ee675b7";
            model.email = emailds;
            model.aadharVerification = "Y";
            model.faceVerification = "Y";
            model.interviewerTraining = "N";
            model.companyEmailVerification = "N";
            model.videoBio = "N";
            model.careerQuestions = "N";
            model.testLink.mcq = "";
            model.testLink.code = CodeTestLink;
            var client = new RestClient("https://bot.risebird.io/api/v1/bot/invite");
            var request = new RestRequest("https://bot.risebird.io/api/v1/bot/invite", Method.Post);
            request.AddHeader("content-type", "application/json");
            request.AddJsonBody(model);
            RestResponse response = await client.ExecuteAsync(request);
            var convertedResponse = JsonConvert.DeserializeObject<MCQBotResponse>(response.Content);
            return convertedResponse;
        }
        [HttpGet]
        public async Task<IActionResult> GetInterviewerAssessmentGroup(string InterviewerEmailid)
        {
            try
            {
                var model = await _irfService.GetInterviewerAssessmentGroups(InterviewerEmailid);
                return PartialView(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        [HttpPost]
        public async Task<JsonResult> ShareAssessmentVerificationToInterviewer(int Assessmentid, string interieweremailid, string Type)
        {
            try
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var model = await _irfService.GetAssessmentGroupInterviewerMembers(Assessmentid);
                var Uniquedate = model?.Where(x => x.EmailId == interieweremailid).FirstOrDefault();
                if (Uniquedate.AssessmentVerificationType == "MCQ")
                {
                    MCQVMShare _list = new MCQVMShare();
                    _list.status = 0;
                    _list.MCQSet = Uniquedate.Title;
                    _list.ClientHR = emailid;
                    _list.ApplyingCompany = "";
                    _list.WhoShared = emailid;
                    var Emailids = string.Join(",", model.Select(x => x.EmailId).Distinct());
                    string currenturl = "https://" + Request.Host.ToString() + "/MCQ/SkillTest?Id=" + Uniquedate.MCQSetsValue;
                    var botlinkslist = new MCQBotResponse();
                    if (Type == "manual")
                    {
                        currenturl = currenturl + "&uniqueid=448";
                    }
                    else
                    {
                        currenturl = currenturl + "&uniqueid=322";
                        var aadharverificationflag = "Y";
                        var _interviewerflag = "N";
                        botlinkslist = await GenerateBotLink(Emailids.Replace(";", ","), aadharverificationflag, currenturl, _interviewerflag);
                    }

                    foreach (var item in Emailids.Split(","))
                    {
                        var date1 = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");
                        var _meetinglinks = new ShareMCQRisebirdMeetingResponse();
                        if (Type == "manual")
                        {
                            var MeetingDate = DateTime.Now;
                            _meetinglinks = ShareRisebirdMeeting("Risebird", item.Trim(), MeetingDate);
                            if (string.IsNullOrWhiteSpace(_meetinglinks.roomId))
                            {
                                _meetinglinks = ShareRisebirdMeeting("Risebird", item.Trim(), MeetingDate);
                            }
                            Mail _mail = new Mail
                            {
                                TO = item,
                                Subject = "Invitation for " + Uniquedate.Title + " : " + "Risebird"
                            };
                            var result = await _notificationHelper.SharedTestLink(_mail, Uniquedate.Title, "15", currenturl, "Risebird", _meetinglinks.meetingAttendee, MeetingDate, -330);
                            string Reporturl = "https://" + Request.Host.ToString() + "/MCQ/CandidateMCQReport?MCQId=" + Uniquedate.MCQSetsValue + "&SMCQId=" + date1;
                            Mail _mail1 = new Mail
                            {
                                TO = emailid,
                                Subject = "Invitation for " + Uniquedate.Title + " : " + item
                            };
                            await _notificationHelper.SharedTestLinkClient(_mail1, Uniquedate.Title, "15", currenturl, item, _meetinglinks.meetingAgent, MeetingDate, Reporturl, -330, Uniquedate.logolink);
                        }
                        else
                        {
                            var _testurl = botlinkslist.emailBotUrls.Where(x => x.email == item).FirstOrDefault().botTestLink;
                            _meetinglinks.roomId = _testurl.Split("/").LastOrDefault();
                            Mail _mail = new Mail
                            {
                                TO = item,
                                Subject = "Invitation for " + Uniquedate.Title + " : " + "Risebird"
                            };
                            var result = await _notificationHelper.SharedBotTestLink(_mail, Uniquedate.Title, "15", _testurl, "Risebird");
                        }
                        _list.PartitionKey = date1;
                        _list.MCQId = Uniquedate.MCQSetsValue;
                        _list.EmailAddress = item.Trim();
                        _list.roomId = _meetinglinks.roomId;
                        _list.Expiretime = 48;
                        _list.ShareType = true;
                        _list.botactive = true;
                        await _mCQService.AddMCQSetShareList(_list);
                        await _interviewerService.AddInterviewerAssesssmentDetails(Assessmentid, emailid);
                    }
                }
                else if (Uniquedate.AssessmentVerificationType == "Code")
                {
                    var Emailids = string.Join(",", model.Select(x => x.EmailId).Distinct());
                    foreach (var item in Emailids.Split(","))
                    {
                        var codeInviteVM = new CodeInviteVM();
                        codeInviteVM.userName = emailid;
                        codeInviteVM.candidateEmail = item;
                        codeInviteVM.programmingLanguage = Uniquedate.CodeLanguage;
                        codeInviteVM.experience = 3;
                        codeInviteVM.inviteStartTime = DateTime.Now;
                        codeInviteVM.inviteEndTime = DateTime.Now.AddDays(2);
                        codeInviteVM.difficultyLevel = "standard";
                        await GenerateCodeInviteURL(codeInviteVM);
                        await _interviewerService.SendInviteCode(codeInviteVM, emailid);
                        await _interviewerService.AddInterviewerAssesssmentDetails(Assessmentid, emailid);
                        //Send mail to interviewer
                        Mail _mail = new Mail
                        {
                            TO = item,
                            Subject = "Invitation for " + Uniquedate.CodeLanguage + " : " + "Risebird"
                        };
                        var result = await _notificationHelper.SharedBotTestLink(_mail, Uniquedate.CodeLanguage, "15", codeInviteVM.botURL, "Risebird");
                    }
                }
                return Json("Success");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Fail");
            }
        }
        public ShareMCQRisebirdMeetingResponse ShareRisebirdMeeting(string agentname, string visitorName, DateTime datetime)
        {
            var _model = new ShareMCQRisebirdMeeting();
            _model.agentname = agentname;
            _model.visitorName = visitorName;
            DateTime currentTime = TimeZoneInfo.ConvertTime(datetime, TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"));
            var datetime1 = currentTime.ToString("yyyy-MM-dd'T'HH:mm:ss.000Z");
            _model.datetime = datetime1;
            var _meeting = new RestClient("https://meet.risebird.io/api/v1/room");
            var request = new RestRequest("https://meet.risebird.io/api/v1/room", Method.Post);
            request.AddHeader("authorization", "v7sy3J8cAFdEJp5S");
            request.AddBody(_model);
            RestResponse response = _meeting.Execute(request);
            var content = response.Content;
            var convertedResponse = JsonConvert.DeserializeObject<ShareMCQRisebirdMeetingResponse>(content);

            if (convertedResponse != null)
            {
                return convertedResponse;
            }
            return new ShareMCQRisebirdMeetingResponse();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllActiveAssessmentGroupInterviewersForSME(int agId, string agName)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "SeniorInterviewerManagement")

                    {
                        var interviewingMembers = (IList<AssessmentGroupInterviewers>)(await _irfService.GetAssessmentGroupInterviewerMembersForSME(agId));
                        ViewBag.AssessmentGroupName = agName;
                        return PartialView("GetAllActiveAssessmentGroupInterviewersForSME", interviewingMembers);

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
                return Json("Error retrieving data! Please try again.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddUpdateAssessmentGroupSME(IList<AssessmentGroupInterviewers> model, string assessmentGroupName)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SupportAdmin" || profileName == "SeniorInterviewerManagement")
                    {
                        var emailId = HttpContext.Session.GetString("email")?.Trim();
                        emailId = emailId.Replace("\"", "");
                        ViewBag.IsValid = "True";
                        if (ModelState.IsValid)
                        {
                            if (model != null)
                            {
                                if (model.Where(x => x.IsSME)?.Count() > 5)
                                {
                                    ModelState.AddModelError("", "Cannot add more than 5 Subject Matter Experts for an Assessment Group!");
                                    ViewBag.AssessmentGroupName = assessmentGroupName;
                                    ViewBag.IsValid = "False";
                                    return PartialView("GetAllActiveAssessmentGroupInterviewersForSME", model);

                                }
                            }


                        }
                        else
                        {
                            ViewBag.IsValid = "False";
                            ViewBag.AssessmentGroupName = assessmentGroupName;
                            return PartialView("GetAllActiveAssessmentGroupInterviewersForSME", model);

                        }
                        var result = await _irfService.AddUpdateAssessmentGroupSMEInterviewer(model);
                        ViewBag.Result = result.ToString();
                        return PartialView("GetAllActiveAssessmentGroupInterviewersForSME", model);


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
                return Json(false);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetIRFClientReview(int irfId)
        {
            try
            {
                var model = await _irfService.GetIRFClientReviews(irfId);
                if (model == null)
                {
                    model = new List<IRFClientReviewVM>();
                }
                ViewBag.IRFId = irfId;
                return PartialView(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        [HttpGet]
        public async Task<JsonResult> ClientFeedbackNotification(int irfId)
        {
            var transClientUser = _irfRepository.GetTransClientUser(irfId);
            var billingowner = await _irfService.GetIRFBillingOwnerById(irfId);
            var irfList = (await _irfService.GetAllIRFDetailsByIRFId(irfId))?.ToList();
            var _irfitem = irfList.FirstOrDefault();
            string clientlogolink = string.Empty;
            string JDTitle = string.Empty;
            DataTable dt = new DataTable();
            if (_irfitem != null)
            {
                clientlogolink = _irfitem.LogoLink;
                JDTitle = _irfitem.JDTiltle;
                dt.Columns.AddRange(new DataColumn[5] { new DataColumn("Skill Channel", typeof(string)),
                            new DataColumn("Candidates", typeof(int)),
                            new DataColumn("Completed",typeof(int)),
                            new DataColumn("Selected",typeof(int)),
                            new DataColumn("Rejected",typeof(int))});
                dt.Rows.Add(_irfitem.IRFId, _irfitem.CandidateCount, _irfitem.CompletedInterviews, _irfitem.SelectedMeetings, _irfitem.RejectedMeetings);
            }
            foreach (var item in transClientUser)
            {
                try
                {
                    var ReviewURL = "https://portal.risebird.io/Survey/ClientIRFReview?IRFId=" + item.IRFName + "&uniqueid=" + item.uniqueid;
                    Mail _mail1 = new Mail
                    {
                        TO = item.EmailId,
                        Subject = "Skill Channel " + item.IRFName + " Feedback"
                    };
                    var emailsend = await _notificationHelper.SendSkillChannelFeedbackNotification(_mail1, item.FirstName, dt, JDTitle, ReviewURL, clientlogolink);
                }
                catch (Exception e)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                }
            }
            try
            {
                var ReviewURL = "https://portal.risebird.io/Survey/ClientIRFReview?IRFId=" + billingowner.IRFId + "&uniqueid=" + billingowner.uniqueid;
                Mail _mail1 = new Mail
                {
                    TO = billingowner.EmailId,
                    Subject = "Skill Channel " + billingowner.IRFId + " Feedback"
                };
                var emailsend = await _notificationHelper.SendSkillChannelFeedbackNotification(_mail1, billingowner.Name, dt, JDTitle, ReviewURL, clientlogolink);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            return Json("Notification sent successfully.");
        }

        [HttpGet]
        public async Task<IActionResult> GetAssessmentGroupSMEs(int agId, string agName, int? irfId, bool isLocked = false)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Client")

                    {
                        var interviewingSMEMembers = (IList<SkillChannelSME>)(await _irfService.GetEligibleSMEForSkillChannel(agId, irfId));
                        ViewBag.AssessmentGroupName = agName;
                        ViewBag.IsLocked = isLocked;
                        return PartialView("SMEListPartialView", interviewingSMEMembers);

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
                return Json("Error retrieving data! Please try again.");
            }
        }
        public async Task<IActionResult> SkillChannelDetailedView(string type, int irfid)
        {
            ViewBag.type = type;
            var interviewlist = (await _irfService.GetSkillChannelStatsDetails(irfid, type))?.ToList();
            return View(interviewlist);
        }



    }
}