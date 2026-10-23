using ExcelDataReader.Log;
using iTextSharp.text.pdf.qrcode;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using OfficeOpenXml;
using RestSharp;
using Risebird.Business;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.OpenAIModels;
using Risebird.Entities.Data.PlivoCXModels;
using Risebird.Entities.Data.VapiModels;
using Risebird.Entities.Enum;
using Risebird.Entities.Helpers;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using Risebird.Web.Controllers.AzureMediaservice;
using Risebird.Web.Helpers;
using Syncfusion.DocIO.DLS;
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
using System.Web;
using VirusTotalNet;
using VirusTotalNet.ResponseCodes;
using X.PagedList.Extensions;
using static iTextSharp.text.pdf.AcroFields;
using static SkiaSharp.HarfBuzz.SKShaper;
using Method = RestSharp.Method;

namespace Risebird.Web.Controllers
{

    public class IRFController : Controller
    {
        private const int MaxSampleQuestionLength = 5000;
        private const int DefaultCodingTimeLimitInMinutes = 15;

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
        private readonly IInterviewerService _interviewerService;
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
        private readonly IAffIndaResumeParserHelper _affIndaResumeParserHelper;
        private readonly ISASUrlHelper _sasUrlHelper;
        private readonly ITwilioHelper _twilioHelper;
        private readonly IActivityTrackerService _activityTrackerService;
        private readonly IAssistantServiceHelper _assistantServiceHelper;
        private readonly IOpenAISpeechToTextService _speechToTextService;
        private readonly IPlivoCXService _plivoCXService;
        private readonly IInterviewerEmailAvailabilityRepository _eaRepository;
        private readonly ITransInterviewerAuditRepository _transInterviewerAuditRepository;
        private readonly IPhoneScreenRepository _phoneScreenRepository;
        private readonly IMcqAssessmentService _mcqAssessmentService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ICodeProjectQuestionService _codeProjectQuestionService;
        private readonly Risebird.Web.Services.CodeProjectBackgroundQueue _codeProjectQueue;
        private readonly IOpenAIResponsesHelper _openAIResponsesHelper;


        private readonly IHttpClientFactory _httpClientFactory;

        public IRFController(IIRFRepository iRFRepository, IApplicationErrorLogger applicationErrorLogger, IIRFService irfService, IMeetingService meetingService, IAzureStorageHelper azureStorageHelper, IEvaluatorService evaluatorService, IUserService userService, ISkillService skillService, IConfiguration configuration, ICandidateService candidateService, IInterviewerSkillsService iInterviewerSkillsService, IInterviewerService interviewerService, INotificationHelper notificationHelper, ITemplateService templateService, IClientService clientService, IFileMimeTypeHelper fileMimeTypeHelper, IInterviewSchedulerService interviewSchedulerservice,
            IClonableSkillChannelTemplateService clonableSkillChannelTemplateService, ICalendlyHelper calendlyHelper,
            IMCQService mCQService, IEncryptionHelper encryptionHelper, IWhatsAppHelper whatsAppHelper,
            HttpClientHelper factory, IAffIndaResumeParserHelper affIndaResumeParserHelper,
            ISASUrlHelper sasUrlHelper, ITwilioHelper twilioHelper, IActivityTrackerService activityTrackerService, IAssistantServiceHelper assistantServiceHelper,
            IOpenAISpeechToTextService speechToTextService,
            IPlivoCXService plivoCXService,
            IInterviewerEmailAvailabilityRepository eaRepository,
            ITransInterviewerAuditRepository transInterviewerAuditRepository,
            IPhoneScreenRepository phoneScreenRepository,
            IMcqAssessmentService mcqAssessmentService,
            IServiceScopeFactory scopeFactory,
            ICodeProjectQuestionService codeProjectQuestionService,
            Risebird.Web.Services.CodeProjectBackgroundQueue codeProjectQueue,
            IOpenAIResponsesHelper openAIResponsesHelper, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _scopeFactory = scopeFactory;
            _codeProjectQuestionService = codeProjectQuestionService;
            _codeProjectQueue = codeProjectQueue;
            _openAIResponsesHelper = openAIResponsesHelper;
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
            _affIndaResumeParserHelper = affIndaResumeParserHelper;
            _sasUrlHelper = sasUrlHelper;
            _twilioHelper = twilioHelper;
            _activityTrackerService = activityTrackerService;
            _assistantServiceHelper = assistantServiceHelper;
            _speechToTextService = speechToTextService;
            _plivoCXService = plivoCXService;
            _eaRepository = eaRepository;
            _transInterviewerAuditRepository = transInterviewerAuditRepository;
            _phoneScreenRepository = phoneScreenRepository;
            _mcqAssessmentService = mcqAssessmentService;
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter" || profileName == "CuratedUser" || profileName == "Finance" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "SeniorInterviewerManagement")
                    {
                        //await _twilioHelper.MakeIVRCall("+918888571923", "Harshit", "Dated", string.Empty);
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter" || profileName == "CuratedUser" || profileName == "Finance" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "SeniorInterviewerManagement")
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter" || profileName == "CuratedUser" || profileName == "Finance" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "SeniorInterviewerManagement")
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


        /// <summary>
        /// Primary Prompt Id to stamp on an AI interview skill channel when the box is left empty.
        /// It is deliberately the same prompt the runtime already falls back to for a channel whose
        /// column is NULL, so filling the blank in cannot change how any existing channel behaves -
        /// it only stops the save being refused:
        ///
        ///   Adaptive - the primary conducts the interview. GenerateAIInterviewerMessage falls back
        ///              to OpenAI:AIInterviewerPromptId, so that is the value. AI screening uses its
        ///              own AIScreening:PrimaryPromptId when one is configured, and this follows it
        ///              so channels made by hand and channels provisioned by the intake agree.
        ///   Preset   - questions come from the channel's question bank, never from a prompt, so the
        ///              primary is only ever read as the report prompt (ResolvePromptIds returns
        ///              evaluator ?? primary). The report falls back to OpenAI:AIInterviewPromptMap,
        ///              so that is the value. Stamping the INTERVIEWER prompt here instead would hand
        ///              every preset channel's report to the question generator.
        ///
        /// The Interview Evaluator Prompt Id is deliberately left alone: blank means "the primary
        /// writes the report too", which with the values above is exactly today's behaviour.
        /// Returns null only when nothing is configured - the single case where a save is still
        /// refused, and the message then names the setting to fill in.
        /// </summary>
        private string GetDefaultPrimaryPromptId(int aiInterviewType)
        {
            string promptId;
            if (aiInterviewType == (int)AIInterviewTypeEnum.Preset)
            {
                promptId = _configuration["OpenAI:AIInterviewPromptMap"];
            }
            else
            {
                promptId = _configuration["AIScreening:PrimaryPromptId"];
                if (string.IsNullOrWhiteSpace(promptId))
                {
                    promptId = _configuration["OpenAI:AIInterviewerPromptId"];
                }
            }
            return string.IsNullOrWhiteSpace(promptId) ? null : promptId.Trim();
        }

        /// <summary>
        /// Hands both defaults to the skill-channel form so the Primary Prompt Id box can fill itself
        /// when the AI interview type is picked or changed, without a round trip.
        /// </summary>
        private void SetDefaultPromptIdViewData()
        {
            ViewBag.DefaultAdaptivePromptId = GetDefaultPrimaryPromptId((int)AIInterviewTypeEnum.Adaptive) ?? string.Empty;
            ViewBag.DefaultPresetPromptId = GetDefaultPrimaryPromptId((int)AIInterviewTypeEnum.Preset) ?? string.Empty;
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
                    irfDetails.Heuristicsscore = irfDetails.DisHeuristicsscore;
                    irfDetails.JobCountry = irfDetails.DisJobCountry;
                    irfDetails.CompanyType = irfDetails.DisCompanyType;
                    irfDetails.SkillSetsevaluate = irfDetails.DisSkillSetsevaluate;
                    irfDetails.TechRubricType = TechRubricTypes.Advanced;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        string uploadedFileURL = string.Empty;
                        if (irfDetails.JDFile == null && (irfDetails.JDLink == null))
                        {
                            TempData["SuccessErrorMessage"] = "Adding Skills Channel has Failed. JDLink not found.";
                            return RedirectToAction("Index");
                        }
                        var isEmptyskillName = irfDetails?.SkillCollection?.Any(x => string.IsNullOrEmpty(x.SkillName) && x.LevelId > 0);
                        if (isEmptyskillName.HasValue && isEmptyskillName.Value == true)
                        {
                            //ModelState.AddModelError("", "The Skill Name column cant be empty in the Tech Rubric.");
                            //return View("EditIRF", irfDetails);
                            TempData["SuccessErrorMessage"] = "Adding Skills Channel has Failed. Skill Name cannot be left empty.";
                            return RedirectToAction("Index");
                        }
                        // The Primary Prompt Id conducts the interview, so an AI interview channel cannot be
                        // created without one. The Interview Evaluator Prompt Id stays optional: left blank,
                        // the primary prompt generates the candidate's report as well.
                        // A blank box is filled with the prompt this channel's type already falls back
                        // to at runtime rather than refused, so a channel is never lost over a field the
                        // person had no value to type; the save fails only when nothing is configured.
                        if (irfDetails.IsAIInterview && string.IsNullOrWhiteSpace(irfDetails.OpenAIPromptId))
                        {
                            irfDetails.OpenAIPromptId = GetDefaultPrimaryPromptId(irfDetails.AIInterviewType);
                        }
                        if (irfDetails.IsAIInterview && string.IsNullOrWhiteSpace(irfDetails.OpenAIPromptId))
                        {
                            TempData["SuccessErrorMessage"] = "Adding Skills Channel has Failed. Primary Prompt Id is mandatory for an AI interview skill channel, and no default is configured for this interview type (OpenAI:AIInterviewerPromptId for Adaptive, OpenAI:AIInterviewPromptMap for Preset).";
                            return RedirectToAction("Index");
                        }
                        // AI Interview Type decides how the channel is conducted and is read by roughly
                        // 15 procs (revenue reporting, QC, redo, MyInterviews) plus the IsAdaptive flag the
                        // interview loop branches on. An unset value reaches SQL as 0, which matches neither
                        // Adaptive nor Preset and leaves the channel in a state nothing handles.
                        if (irfDetails.IsAIInterview && !System.Enum.IsDefined(typeof(AIInterviewTypeEnum), irfDetails.AIInterviewType))
                        {
                            TempData["SuccessErrorMessage"] = "Adding Skills Channel has Failed. AI Interview Type is mandatory for an AI interview skill channel.";
                            return RedirectToAction("Index");
                        }
                        if (irfDetails.JDFile != null)
                        {
                            var filename = irfDetails.JDFile.FileName.Replace(" ", "_");
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(irfDetails.JDFile);
                            if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                                || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                            {
                                await _azureStorageHelper.InsertBlob(filename, "resumes", irfDetails.JDFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                var blob = containerRef.GetBlockBlobReference(filename);
                                uploadedFileURL = blob.Uri.ToString();
                                irfDetails.JDLink = uploadedFileURL;
                            }

                        }
                        if (irfDetails.CompanyDetailFile != null)
                        {
                            var filename = irfDetails.CompanyDetailFile.FileName.Replace(" ", "_");
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(irfDetails.CompanyDetailFile);
                            if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                                || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                            {
                                await _azureStorageHelper.InsertBlob(filename, "resumes", irfDetails.CompanyDetailFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                var blob = containerRef.GetBlockBlobReference(filename);
                                uploadedFileURL = blob.Uri.ToString();
                                irfDetails.CompanyDetails = uploadedFileURL;
                            }

                        }

                        if (string.IsNullOrEmpty(irfDetails.MeetingPlatform))
                        {
                            irfDetails.MeetingPlatform = MeetingPlatforms.Zoho;
                        }

                        var domain = Request.Scheme + "://" + Request.Host.Value;

                        irfDetails.StartDate = DateTime.Now;
                        irfDetails.EndDate = DateTime.Now.AddMonths(3);

                        //Start: Auto Generate Scheduling Link 
                        var _schedulinglink = await AutoGenerateSchedulingLink(irfDetails.StartDate, irfDetails.IRFId, irfDetails.JDTiltle + "_Scheduling", "AddIRF", isAIInterview: irfDetails.IsAIInterview);
                        irfDetails.CalendlyLink = _schedulinglink;
                        //End: Auto Generate Scheduling Link
                        await checkSkillSetandCombine(irfDetails);
                        if (await _irfService.AddIRFDetails(irfDetails, emailId, domain))
                        {
                            await _phoneScreenRepository.SetIRFPlivoAssistantUrlAsync(irfDetails.id > 0 ? irfDetails.id : (int?)null, irfDetails.IRFId, irfDetails.PlivoAssistantUrl);
                            await _phoneScreenRepository.SetIRFPlivoWhatsAppAssistantUrlAsync(irfDetails.id > 0 ? irfDetails.id : (int?)null, irfDetails.IRFId, irfDetails.PlivoWhatsAppAssistantUrl);
                            await _phoneScreenRepository.SetIRFElevenLabsAgentIdAsync(irfDetails.id > 0 ? irfDetails.id : (int?)null, irfDetails.IRFId, irfDetails.ElevenLabsAgentId);
                            //create an index with the same name as that of this skill channel in affinda for using the search and match tool

                            //var isIndexCreated = await _affIndaResumeParserHelper.CreateIndex(irfDetails.IRFId);

                            //await _irfService.UpdateAffindaIndexFlag(irfDetails.IRFId, isIndexCreated);


                            //call fire and forget method here to send emails and whatsapp for availability
                            await FireAndForgetRequestToSendAvailabilityMails(irfDetails.AssessmentGroupId, irfDetails.id, irfDetails.FlatRate, irfDetails.PrimarySkillInvite, irfDetails.IRFId, irfDetails.JDTiltle);
                            TempData["SuccessErrorMessage"] = "Skills Channel " + irfDetails.IRFId + " has been added Successfully!"
                                + await EnsureMcqChannelLinkAsync(irfDetails);
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
            SetDefaultPromptIdViewData();
            //var AssessmentsGroups = _interviewerService.FetchAssessmentGroups();
            var AssessmentsGroups = await _irfService.GetAllMAGs();
            //var interviewers = new List<Interviewer>();
            if (string.IsNullOrEmpty(model.AssessmentGroupId))
            {
                //interviewers = await _irfService.GetInterviewersForIRF(AssessmentsGroups.FirstOrDefault()?.id.ToString());
                AssessmentsGroups = AssessmentsGroups.Where(x => x.IsRelevant == true).ToList();
            }
            else
            {
                //interviewers = await _irfService.GetInterviewersForIRF(model.AssessmentGroupId);
                var _assessmentid = Convert.ToInt32(model.AssessmentGroupId);
                var _listassessment = AssessmentsGroups.Where(x => x.id == _assessmentid).ToList();
                AssessmentsGroups = AssessmentsGroups.Where(x => x.IsRelevant == true).ToList();
                if (AssessmentsGroups.Where(x => x.id == _assessmentid).Count() == 0)
                {
                    AssessmentsGroups = AssessmentsGroups.Union(_listassessment);
                }
            }
            //model.JoinabilityTypeList = new SelectList(_irfService.GetJoinabilityMatrix(), "id", "Name");
            //model.SkillGeneralTypeList = new SelectList(_irfService.GetGeneralSkillMatrix(), "id", "Name");
            //model.SelectabilityTypeList = new SelectList(_irfService.GetSelectabilityMatrix(), "id", "Name");
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
            //var level = await _irfRepository.GetLevelTech();
            //level = level.Where(x => x.id != 1 && x.id != 7).ToList();
            //ViewBag.LevelTech = new SelectList(level, "id", "Name");
            //var Skillset = await _irfRepository.GetSkills();
            ////if (Skillset != null)
            ////{
            ////    Skillset = Skillset.OrderBy(x => x.Name.Trim()).ToList();
            ////}
            //ViewBag.Skill = new SelectList(Skillset.ToList(), "id", "Name");
            //ViewBag.ClientUser = new SelectList(_irfRepository.GetCientUsers(), "id", "EmailId");
            ViewBag.ClientUser = new SelectList(_irfService.GetClientUser(clients.FirstOrDefault().id), "id", "EmailId");
            //ViewBag.ClientRole = new SelectList(_irfRepository.GetClientRoles(), "id", "Name");
            //ViewBag.Interviewers = new SelectList(interviewers, "UserId", "EmailId");
            //ViewBag.Recruiters = new SelectList(_irfRepository.GetRecruiters(), "UserId", "EmailId");
            //ViewBag.ResumeEvaluators = new SelectList(_irfRepository.GetResumeEvaluators(), "UserId", "EmailId");
            ViewBag.HRs = new SelectList(_irfRepository.GetHRs(), "UserId", "EmailId");
            //ViewBag.QualityController = new SelectList(interviewers, "UserId", "EmailId");
            //ViewBag.EmergencyInterviewers = new SelectList(interviewers, "UserId", "EmailId");
            //ViewBag.RiseBirdHR = new SelectList(_irfService.GetAllHRs(), "id", "EmailId");
            ViewBag.ChannelOwnerOptions = new SelectList(_irfService.GetEligibleChannelOwners(), "id", "EmailId");

            model.AssessmentGroupList = new SelectList(AssessmentsGroups, "id", "AssessmentGroup");
            var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                             select new { ID = (int)s, Name = s.GetDescription() };
            ViewBag.Heuristicsscorelist = new SelectList(Heuristics, "ID", "Name");
            var jobLevels = JobLevelHelper.SelectListOptions().Select(x => new { x.ID, x.Name });
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
            if (string.IsNullOrEmpty(model.TechRubricType))
            {
                model.TechRubricType = TechRubricTypes.Advanced;
            }
            return;
        }
        //    catch (Exception ex)
        //    {
        //        _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //    }
        //}


        private async Task FireAndForgetRequestToSendAvailabilityMails(string assessmentGroupId, int irfId, int flatRate, string primarySkillInvite, string irfName, string jdTitle)
        {
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    if (!int.TryParse(assessmentGroupId, out int agId))
                        return;
                    string magName = await _eaRepository.GetMAGNameByAssessmentGroupIdAsync(agId);
                    if (string.IsNullOrWhiteSpace(magName))
                        return;

                    var interviewers = await _eaRepository.GetCallableInterviewersForMAGAsync(magName, onlyUncalled: true);
                    if (interviewers == null || !interviewers.Any())
                        return;

                    string callerId = _configuration["PlivoCX:AvailabilityCallerId"]
                                   ?? _configuration["PlivoCX:OnboardingCallerId"]
                                   ?? _configuration["Plivo:FromNumber"]
                                   ?? "+918031136547";

                    int batchLogId = 0;
                    string initiatedBy = "IRF AutoCall";
                    try
                    {
                        batchLogId = await _eaRepository.InsertBatchCallLogAsync(magName, initiatedBy, interviewers.Count);
                    }
                    catch { }

                    int succeeded = 0, failed = 0;

                    foreach (var interviewer in interviewers)
                    {
                        try
                        {
                            // Profile interviewers (not yet in InterviewerEmailAvailability) are NOT
                            // inserted up-front. We pass a self-contained token as log_id; the hangup
                            // webhook creates the row only if the call confirms availability.
                            bool deferred = interviewer.Source == "Profile" && interviewer.InterviewerProfileId.HasValue;
                            int eaId = interviewer.Id;

                            string logId = deferred
                                ? AvailabilityCallToken.Build(interviewer.InterviewerProfileId.Value, magName, "IRFAutoCall")
                                : eaId.ToString();

                            string phone = interviewer.Phone?.Trim() ?? "";
                            if (phone.Length == 10 && !phone.StartsWith("+")) phone = "+91" + phone;
                            else if (phone.Length > 10 && !phone.StartsWith("+")) phone = "+" + phone;

                            if (!deferred)
                                await _eaRepository.UpdatePlivoCallStatusAsync(eaId, "initiated", null,
                                    DateTime.UtcNow, null, null, null, null, null, null, null, null, initiatedBy);

                            var triggerRequest = new PlivoCXAvailabilityTriggerRequest
                            {
                                CallerId        = callerId,
                                PhoneNumber     = phone,
                                InterviewerName = interviewer.InterviewerName ?? "there",
                                InterviewerEmail = interviewer.SenderEmail,
                                MagName         = magName,
                                LogId           = logId,
                                TodayDate       = DateTime.Now.ToString("dddd, dd MMMM yyyy")
                            };

                            var response = await _plivoCXService.TriggerAvailabilityCallAsync(triggerRequest);

                            if (!string.IsNullOrEmpty(response?.Error))
                            {
                                if (!deferred)
                                    await _eaRepository.UpdatePlivoCallStatusAsync(eaId, "trigger-failed", null,
                                        null, null, null, response.Error, null, null, null, null, null, null);
                                failed++;
                            }
                            else
                            {
                                if (!deferred)
                                {
                                    await _eaRepository.UpdatePlivoCallStatusAsync(eaId, "triggered", response?.ConversationId,
                                        null, null, null, null, null, null, null, null, null, null);
                                    await _eaRepository.InsertHistoryAsync(eaId, "IRFAutoCall", "—",
                                        $"Availability call triggered for IRF '{irfName}'. ConvId: {response?.ConversationId ?? "N/A"}", initiatedBy);
                                }
                                succeeded++;
                            }

                            await System.Threading.Tasks.Task.Delay(1200);
                        }
                        catch
                        {
                            failed++;
                        }
                    }

                    if (batchLogId > 0)
                    {
                        try { await _eaRepository.CompleteBatchCallLogAsync(batchLogId, succeeded, failed, 0, "Completed"); }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    try { await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex); } catch { }
                }
            });
        }
        private async Task MakeIVRCallForAvailability(AvailabilityCheckDTO dto)
        {
            try
            {
                await _activityTrackerService.DebugLogger("MakeIVRCallForAvailability function successfully invoked", "ivr", string.Empty);
                //var apiurl = "https://" + Request.Host.ToString() + "/api/CheckIRFAvailability/InterviewerAvailabilityCheck";
                var apiurl = "https://api.portal.risebird.io/api/CheckIRFAvailability/InterviewerAvailabilityCheck";
                //var apiurl = "https://" + "localhost:50323" + "/api/CheckIRFAvailability/InterviewerAvailabilityCheck";
                var client = new RestClient(apiurl);
                var request = new RestRequest(apiurl, Method.Post);
                request.AddHeader("content-type", "application/json");
                var body = JsonConvert.SerializeObject(dto);
                request.AddParameter("application/json", body, ParameterType.RequestBody);
                RestResponse response = await client.ExecuteAsync(request);
            }
            catch (Exception ex)
            {

                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                await _activityTrackerService.DebugLogger($"MakeIVRCallForAvailability function error : {ex.Message}, {ex.StackTrace}", "ivr", string.Empty);

            }
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

        [Authorize]
        [HttpGet]
        public bool CheckCampaignUniqueness(string value)
        {
            return _irfService.CheckCampaignUniqueness(value);
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
                    //var skills = await _irfService.GetTechSkills(id);
                    var skills = await _irfService.GetIRFTechSkills(id);

                    //foreach (var skill in skills)
                    //{
                    //    techSkills.Add(new Skill { id = skill.id, Name = skill.Name?.Replace("\t", "").Replace("\n", "").Replace("\r", ""), Description = skill.Description?.Replace("\t", "").Replace("\n", "").Replace("\r", "") });
                    //}
                    //ViewBag.Skills = new SelectList(skills, "id", "Name");
                    //model.SkillsOptionsData = JsonConvert.SerializeObject(skills);
                    //model.SkillsOptionsData = model.SkillsOptionsData?.Replace("\t", "").Replace("\n", "").Replace("\r", "");
                    //ViewBag.Skills = new SelectList(skills, "id", "Name");
                    ViewBag.Skills = new SelectList(skills);
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
                            var questionsCollection = model.SkillCollection.Select(x => (x.Comment, x.SkillName, x.UserId)).Where(x => x.UserId == model.UserId);
                            //questionsCollection = questionsCollection.Distinct();
                            var currentUserSkills = model.SkillCollection
                            .Where(x => x.UserId == model.UserId)
                            .DistinctBy(x => new { x.Comment, x.SkillName, x.UserId });

                            var otherUserSkills = model.SkillCollection
                                .Where(x => x.UserId != model.UserId);

                            model.SkillCollection = currentUserSkills
                                .Concat(otherUserSkills)
                                .ToList();


                            //if (questionsCollection.Count() != questionsCollection.Distinct().Count())
                            //{
                            //    //duplicates exist
                            //    ModelState.AddModelError("", "The skill-question combination added by you should contain unique entries.");
                            //    //var skills = await _irfService.GetTechSkills(model.IRFId);
                            //    //ViewBag.Skills = new SelectList(skills, "id", "Name");
                            //    var skills = await _irfService.GetIRFTechSkills(model.IRFId);
                            //    ViewBag.Skills = new SelectList(skills);
                            //    //model.SkillsOptionsData = JsonConvert.SerializeObject(skills);
                            //    model.Profile = profileName;
                            //    return View(model);
                            //}
                            //foreach (var skill in model.SkillCollection)
                            //{
                            //    if (!string.IsNullOrEmpty(skill.Comment))
                            //    {
                            //        skill.Comment = skill.Comment
                            //            .Replace("\r", "")
                            //            .Replace("\n", "")
                            //            .Replace("\t", "")
                            //            .Trim();
                            //    }
                            //}
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
                            //var skills = await _irfService.GetTechSkills(model.IRFId);
                            //ViewBag.Skills = new SelectList(skills, "id", "Name");
                            var skills = await _irfService.GetIRFTechSkills(model.IRFId);
                            ViewBag.Skills = new SelectList(skills);
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
        public async Task<JsonResult> IsQuestionUsedInReport(string question)
        {
            try
            {
                bool isQuestionUsed = await _irfService.IsQuestionUsedInReport(question/*.Replace("\n", "").Replace("\r", "")*/);
                return Json(isQuestionUsed);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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
                    model.PlivoAssistantUrl = await _phoneScreenRepository.GetIRFPlivoAssistantUrlAsync(id);
                    model.PlivoWhatsAppAssistantUrl = await _phoneScreenRepository.GetIRFPlivoWhatsAppAssistantUrlAsync(id);
                    model.ElevenLabsAgentId = await _phoneScreenRepository.GetIRFElevenLabsAgentIdAsync(id);
                    model.IsClone = isClone;
                    model.LoggedEmail = emailid;
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
                    await checkSkillSetandSeperate(model);
                    model.OldJDLink = model.JDLink;
                    model.OldCompanyDetails = model.CompanyDetails;
                    model.OldJDTitle = model.JDTiltle;
                    ViewBag.clonevalue = clonevalue;
                    model.clonevalue = clonevalue;
                    model.Profile = profileName;
                    if (string.IsNullOrEmpty(model.TechRubricType))
                    {
                        model.TechRubricType = TechRubricTypes.Basic;
                    }
                    // Channels created before the Primary Prompt Id became mandatory come back with an
                    // empty box, and every later edit to them would be refused for it. Open with the
                    // prompt this channel's type already falls back to at runtime filled in instead -
                    // still editable, and saving it changes nothing about how the channel behaves.
                    // The filled-in value is handed to the view as well, so that changing the interview
                    // type on the form can correct it; a prompt the channel carries of its own is not
                    // passed on and so is never replaced.
                    if (string.IsNullOrWhiteSpace(model.OpenAIPromptId))
                    {
                        model.OpenAIPromptId = GetDefaultPrimaryPromptId(model.AIInterviewType);
                        ViewBag.AutoFilledPromptId = model.OpenAIPromptId;
                    }
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

        /// <summary>
        /// Makes sure the MCQ system has a skill channel linked to this one, creating it if not,
        /// and returns a sentence to append to the save confirmation.
        ///
        /// The assessment section is provisioned from screen.risebird.io by matching
        /// MCQIRF.PortalIRFId to this channel's IRF Id. That mirror channel used to be created by
        /// hand over there, which meant switching the section on here produced a channel that
        /// looked configured and failed at the first candidate. This closes that gap: ticking the
        /// box is now sufficient.
        ///
        /// The MCQ endpoint is find-or-create and idempotent on the portal channel id, so this is
        /// safe to call on every save and cannot produce duplicate channels.
        ///
        /// Returns an empty string when the section is off or the channel was already linked -
        /// there is nothing the person needs to know in either case.
        /// </summary>
        private async Task<string> EnsureMcqChannelLinkAsync(IRFVm model)
        {
            if (model == null || !model.HasMcqAssessmentSection || string.IsNullOrWhiteSpace(model.IRFId))
                return string.Empty;

            try
            {
                var result = await _mcqAssessmentService.EnsureChannelLinkAsync(new McqEnsureChannelLinkRequest
                {
                    SkillChannelId = model.IRFId,
                    RequestedBy = (await _userService.GetUser(model.BillingOwner)).EmailId,
                    Title = model.JDTiltle,
                    PrimarySkills =  model.McqAssessmentSections ?? model.JDTiltle,
                    ProfileType = model.Heuristicsscore > 0 ? model.Heuristicsscore.ToString() : null,
                    SeniorityLevel = model.JobLevel > 0 ? model.JobLevel.ToString() : null,
                    // No job function on the portal channel, and the MCQ side treats it as optional.
                    PortalIRFTitle = model.JDTiltle
                });

                if (result == null)
                {
                    // The failure itself is logged with its reason by the service. What the person
                    // in front of the screen needs is the manual fallback, not the cause.
                    return $" The skill channel was saved, but the matching channel on the MCQ system could not be"
                         + $" created automatically. Create one there with AI Interview ticked and Portal IRF Id set to"
                         + $" '{model.IRFId}', or candidates will be told the assessment section is unavailable.";
                }

                if (result.Created)
                {
                    return $" A matching skill channel ({result.McqIrfId}) was also created on the MCQ system for the"
                         + $" assessment section.";
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Primary skills for the mirrored MCQ channel. The MCQ side needs at least one skill: it
        /// builds the question rubric from this, and rejects an empty one. PrimarySkillInvite is
        /// the channel's own headline skill; the tech rubric is the fallback when it is blank, and
        /// carries more of what the channel actually tests anyway.
        /// </summary>
        private static string ResolveMcqPrimarySkills(IRFVm model)
        {
            if (!string.IsNullOrWhiteSpace(model.PrimarySkillInvite))
                return model.PrimarySkillInvite.Trim();

            var rubricSkills = (model.SkillCollection ?? new List<TransSkill>())
                .Select(s => s?.SkillName?.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return rubricSkills.Count > 0 ? string.Join(", ", rubricSkills) : model.JDTiltle;
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement")
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
                        var isEmptyskillName = model?.SkillCollection?.Any(x => string.IsNullOrEmpty(x.SkillName) && x.LevelId > 0);
                        if (isEmptyskillName.HasValue && isEmptyskillName.Value == true)
                        {
                            ModelState.AddModelError("", "The Skill Name column cant be empty in the Tech Rubric.");
                            return View("EditIRF", model);

                        }
                        // The Primary Prompt Id conducts the interview, so an AI interview channel cannot be
                        // saved without one. The Interview Evaluator Prompt Id stays optional: left blank,
                        // the primary prompt generates the candidate's report as well.
                        // A blank box is filled with the prompt this channel's type already falls back
                        // to at runtime rather than refused. Channels created before the column existed
                        // carry NULL, so without this every edit to one of them is turned away over a
                        // field that did not exist when it was made, and the recruiter has no value to
                        // type. The save fails only when nothing is configured.
                        if (model.IsAIInterview && string.IsNullOrWhiteSpace(model.OpenAIPromptId))
                        {
                            model.OpenAIPromptId = GetDefaultPrimaryPromptId(model.AIInterviewType);
                        }
                        if (model.IsAIInterview && string.IsNullOrWhiteSpace(model.OpenAIPromptId))
                        {
                            ModelState.AddModelError("", "Primary Prompt Id is mandatory for an AI interview skill channel, and no default is configured for this interview type (OpenAI:AIInterviewerPromptId for Adaptive, OpenAI:AIInterviewPromptMap for Preset).");
                            await SetEditIRFProperties(model);
                            return View("EditIRF", model);
                        }
                        // AI Interview Type decides how the channel is conducted and is read by roughly
                        // 15 procs (revenue reporting, QC, redo, MyInterviews) plus the IsAdaptive flag the
                        // interview loop branches on. An unset value reaches SQL as 0, which matches neither
                        // Adaptive nor Preset and leaves the channel in a state nothing handles.
                        if (model.IsAIInterview && !System.Enum.IsDefined(typeof(AIInterviewTypeEnum), model.AIInterviewType))
                        {
                            ModelState.AddModelError("", "AI Interview Type is mandatory for an AI interview skill channel.");
                            await SetEditIRFProperties(model);
                            return View("EditIRF", model);
                        }
                        // IRF.RisebirdHr is NOT NULL and foreign-keyed to UserList.id, so an unset Channel
                        // Owner reaches SQL as 0 and the save dies with an FK_IRF_User violation that the
                        // person only sees as "edits were not saved". The dropdown can post empty for
                        // reasons that are not the recruiter's doing - it is disabled by the interviewer
                        // table's change handler, or it had no option for an owner who is no longer
                        // eligible - so this is checked here rather than trusted to the client.
                        if (model.RisebirdHr <= 0)
                        {
                            ModelState.AddModelError("", "Channel Owner is mandatory. Please pick one on the Event Details tab and save again.");
                            await SetEditIRFProperties(model);
                            return View("EditIRF", model);
                        }
                        if (!model.IsClone)
                        {
                            model.ResourceUrl = Request.Scheme + "://" + Request.Host.Value + Request.Path + Request.QueryString;
                        }

                        if (string.IsNullOrEmpty(model.MeetingPlatform))
                        {
                            model.MeetingPlatform = MeetingPlatforms.Zoho;
                        }
                        //else
                        //{
                        //    model.id = 0;
                        //}
                        if (model.JDFile != null)
                        {
                            var filename = model.JDFile.FileName.Replace(" ", "_");
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.JDFile);
                            if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                                || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                            {
                                await _azureStorageHelper.InsertBlob(filename, "resumes", model.JDFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                var blob = containerRef.GetBlockBlobReference(filename);
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
                        await checkSkillSetandCombine(model);
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
                                var _schedulinglink = await AutoGenerateSchedulingLink(model.StartDate, model.IRFId, model.JDTiltle + "_Scheduling", "EditIRF");
                                model.CalendlyLink = _schedulinglink;
                            }
                            //End: Auto Generate Scheduling Link
                            model.id = 0;
                            if (await _irfService.AddIRFDetails(model, emailId, domain))
                            {
                                var isIndexCreated = await _affIndaResumeParserHelper.CreateIndex(model.IRFId);

                                await _irfService.UpdateAffindaIndexFlag(model.IRFId, isIndexCreated);

                                TempData["SuccessErrorMessage"] = "Skills Channel " + model.IRFId + " has been saved Successfully!"
                                    + await EnsureMcqChannelLinkAsync(model);
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
                                await _phoneScreenRepository.SetIRFPlivoAssistantUrlAsync(model.id, model.IRFId, model.PlivoAssistantUrl);
                                await _phoneScreenRepository.SetIRFPlivoWhatsAppAssistantUrlAsync(model.id, model.IRFId, model.PlivoWhatsAppAssistantUrl);
                                await _phoneScreenRepository.SetIRFElevenLabsAgentIdAsync(model.id, model.IRFId, model.ElevenLabsAgentId);
                                TempData["SuccessErrorMessage"] = "Skills Channel " + model.IRFId + " has been saved Successfully!"
                                    + await EnsureMcqChannelLinkAsync(model);
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
            SetDefaultPromptIdViewData();
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
            DateTime absoluteTime = new DateTime(2024, 12, 2, 16, 30, 0);
            var AssessmentsGroups = new List<AssignmentVM>();
            //var interviewers = new List<Interviewer>();
            var interviewerOptions = new List<SelectListItem>();
            // UserIds of interviewers that currently belong to this channel's MAG (i.e. are eligible to
            // be ACTIVE on the channel). Interviewers attached to the channel but not in this set have
            // been removed from the MAG and must not be (re)activated.
            var magMemberUserIds = new List<int>();
            if (model.CreatedOn < absoluteTime)
            {
                AssessmentsGroups = _interviewerService.FetchAssessmentGroups().ToList();
                var interviewers = await _irfService.GetInterviewersForIRF(model.AssessmentGroupId, model.id);
                if (interviewers != null)
                {
                    interviewerOptions = interviewers
                        .Select(x => new SelectListItem { Value = x.UserId.ToString(), Text = x.EmailId })
                        .ToList();
                    magMemberUserIds = interviewers.Select(x => x.UserId).Distinct().ToList();
                }
            }
            else
            {
                AssessmentsGroups = (await _irfService.GetAllMAGs()).ToList();
                var interviewers = await _irfService.GetAssessmentGroupInterviewerMembers(Convert.ToInt16(model.AssessmentGroupId), true);
                if (interviewers != null)
                {
                    interviewerOptions = interviewers
                        .Select(x => new SelectListItem { Value = x.UserId.ToString(), Text = x.EmailId })
                        .ToList();
                    magMemberUserIds = interviewers.Select(x => x.UserId).Distinct().ToList();
                }
            }
            ViewBag.MagMemberUserIds = magMemberUserIds;

            // Interviewers already attached to this skill channel must always remain selectable,
            // even if they've since been removed from the MAG (or deactivated). Otherwise their
            // dropdown renders only "Select" with an empty value, which trips the client-side
            // "There should be atleast one Interviewer." validation and makes the IRF uneditable.
            if (model.InterviewerCollection != null)
            {
                var existingOptionValues = new HashSet<string>(interviewerOptions.Select(o => o.Value));
                foreach (var member in model.InterviewerCollection)
                {
                    if (member.UserId == 0)
                    {
                        continue;
                    }
                    var userIdValue = member.UserId.ToString();
                    if (existingOptionValues.Add(userIdValue))
                    {
                        interviewerOptions.Add(new SelectListItem
                        {
                            Value = userIdValue,
                            Text = !string.IsNullOrEmpty(member.EmailId) ? member.EmailId : userIdValue
                        });
                    }
                }
            }

            ViewBag.Interviewers = new SelectList(interviewerOptions, "Value", "Text");
            //var interviewers = await _irfService.GetInterviewersForIRF(model.AssessmentGroupId, model.id);
            //var interviewers = await _irfService.GetAssessmentGroupInterviewerMembers(Convert.ToInt16(model.AssessmentGroupId), true);
            //var interviewers = await _irfService.GetAvailableAssessmentGroupInterviewerMembers(model.AssessmentGroupId);

            var _assessmentid = Convert.ToInt32(model.AssessmentGroupId);
            var _listassessment = AssessmentsGroups.Where(x => x.id == _assessmentid).ToList();
            AssessmentsGroups = AssessmentsGroups.Where(x => x.IsRelevant == true).ToList();
            if (AssessmentsGroups.Where(x => x.id == _assessmentid).Count() == 0)
            {
                AssessmentsGroups = AssessmentsGroups.Union(_listassessment).ToList();
            }

            ViewBag.Clients = new SelectList(await _irfRepository.GetClients(), "id", "Name");
            ViewBag.Level = new SelectList(await _irfRepository.GetLevels(), "id", "Name");
            //var level = await _irfRepository.GetLevelTech();
            //level = level.Where(x => x.id != 1 && x.id != 7).ToList();
            //ViewBag.LevelTech = new SelectList(level, "id", "Name");

            //var skillset = await _irfRepository.GetSkills();
            //if (skillset != null)
            //{
            //    skillset = skillset.OrderBy(x => !string.IsNullOrEmpty(x.Name) ? x.Name.Trim() : "").ToList();
            //}
            //ViewBag.Skill = new SelectList(skillset, "id", "Name");
            if (!model.IsClone)
            {
                ViewBag.ClientUser = new SelectList(_irfService.GetClientUser(model.ClientId), "id", "EmailId");
            }
            else
            {
                model.ClientCollection = null;
                model.ClientId = 0;
            }

            //ViewBag.ClientRole = new SelectList(_irfRepository.GetClientRoles(), "id", "Name");
            //ViewBag.Recruiters = new SelectList(_irfRepository.GetRecruiters(), "UserId", "EmailId");
            //ViewBag.Interviewers = new SelectList(interviewers, "UserId", "EmailId");
            //ViewBag.QualityController = new SelectList(interviewers, "UserId", "EmailId");
            //ViewBag.EmergencyInterviewers = new SelectList(interviewers, "UserId", "EmailId");
            ViewBag.HRs = new SelectList(_irfRepository.GetHRs(), "UserId", "EmailId");
            // The channel's current owner is passed in so they stay in the list even after they stop
            // being an eligible owner (deactivated, or moved off an owner profile). Otherwise the
            // dropdown has nothing matching RisebirdHr, renders as "Select", and the next save posts
            // an empty owner - which fails on FK_IRF_User (IRF.RisebirdHr -> UserList.id) and makes
            // the channel uneditable. Same reasoning as the interviewer options built above.
            ViewBag.ChannelOwnerOptions = new SelectList(_irfService.GetEligibleChannelOwners(model.RisebirdHr), "id", "EmailId");
            //ViewBag.ResumeEvaluators = new SelectList(_irfRepository.GetResumeEvaluators(), "UserId", "EmailId");


            //ViewBag.RiseBirdHR = new SelectList(_irfService.GetRiseBirdHR(), "id", "EmailId");
            //ViewBag.RiseBirdHR = new SelectList(_irfService.GetAllHRs(), "id", "EmailId");
            var jobLevels = JobLevelHelper.SelectListOptions().Select(x => new { x.ID, x.Name });
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
                var interviewersmembers = await _irfService.GetIRFInterviewerMembers(model.id);
                model.InterviewerMembers = interviewersmembers?.Where(x => x.IsActive).ToList();
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
                        if (await _meetingService.AddInterviewReport(model, emailid))
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

        [HttpGet]
        public async Task<SelectList> GetAllClients()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "SupportAdmin" || profileName == "SeniorHR")
                {
                    var list = new List<SelectListItem>();
                    var clients = await _clientService.GetClients(emailid, profileName);
                    if (clients != null && clients.Count() > 0)
                    {
                        return new SelectList(clients, "id", "Name");
                    }
                    else
                        list.Add(new SelectListItem { Text = "No Clients Found", Value = "0" });
                    return new SelectList(list, "Value", "Text");
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
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

                        if (await _irfService.AddUpdateInterviewPlanner(model))
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
        public async Task<IActionResult> EnrollVoiceItUser(int candidateId, string candidateEmail, int irfId, string irfName)
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
                    var model = (IList<CandidateAudio>)(await _evaluatorService.GetCandidateAudios(candidateId, irfId)).Where(x => x.IsVoiceBiometric == true).ToList();
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
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Client")
                    {
                        if (await _irfService.PauseIRF(irfId, irfName, emailid))
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Client")
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
        public async Task<bool> Subscribe(int id)
        {
            try
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                return await _irfService.SubscribeCuratedGroup(id, emailid, profileName);

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }

        }

        [Authorize]
        [HttpGet]
        public async Task<bool> UnSubscribe(int id)
        {
            try
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                return await _irfService.UnSubscribeCuratedGroup(id, emailid, profileName);

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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
                    if (await _irfService.AddCuratedGroup(irfList, cgName))
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
                ViewBag.Profile = "";
                if (HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    ViewBag.Profile = profileName;
                }
                ViewBag.Type = "IRF";
                var interviewersmembers = await _irfService.GetIRFInterviewerMembers(irfId);
                var model = interviewersmembers?.Where(x => x.IsActive).ToList();
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
                        ViewBag.ProfileName = profileName;
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

            //var skillset = await _skillService.GetSkills();
            //var primaryskilllist = new List<SelectListItem>();
            //model._skilldetails.PrimarySkills = new SelectList(skillset, "id", "Name");
            //foreach (var item in skillset)
            //{
            //    bool selected = false;
            //    if (modeldata != null)
            //    {
            //        if (modeldata.PrimarySkills != null && modeldata.PrimarySkills.Contains(item.Name))
            //        {
            //            selected = true;
            //        }
            //    }
            //    var selectItem = new SelectListItem(item.Name, item.id.ToString(), selected);
            //    primaryskilllist.Add(selectItem);
            //}
            //model._skilldetails.PrimarySkillsList = primaryskilllist;
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
            var seniorityLevelTypes = JobLevelHelper.SelectListOptions().Select(x => new { x.ID, x.Name });
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
            //List<Skill> skills = await _skillService.GetSkills();
            //List<string> selectedSkill = skills.Where(x => model._skilldetails.SelectedSkillIds.Contains(x.id)).Select(x => x.Name).ToList();
            //model._skilldetails.PrimarySkill = string.Join(",", selectedSkill);
            if (string.IsNullOrEmpty(model._skilldetails.PrimarySkill))
            {
                model._skilldetails.PrimarySkill = "misc";

            }
            var CheckAssesmentGroupNameExistOrNot = await _irfRepository.CheckAssessmentGroupAlraedyExist(model._skilldetails.NewAssessmentGroupName);
            if (CheckAssesmentGroupNameExistOrNot)
            {
                TempData["SuccessErrorMessageInterviewer"] = model._skilldetails.NewAssessmentGroupName + " " + "Already Exists";
            }
            else
            {
                model._skilldetails.AssessmentGroupName = model._skilldetails.AssessmentGroupName.Trim();
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
            AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration, _httpClientFactory);
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
                        //string currenturl = "https://" + Request.Host.ToString() + "/Survey/Index?id=" + item;
                        string currenturl = "https://" + Request.Host.ToString() + "/Survey/CandidateFeedbackForm?id=" + item;
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
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "SeniorInterviewerManagement")
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
                //profileName = "Client";
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
                    var priorityTypes = from PriorityLevel s in Enum.GetValues(typeof(PriorityLevel))
                                        select new { ID = (int)s, Name = s.GetDescription() };
                    ViewBag.PriorityLevelList = new SelectList(priorityTypes, "ID", "Name");
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
            //var level = await _irfRepository.GetLevelTech();
            //var Skillset = await _irfRepository.GetSkills();
            //if (Skillset != null)
            //{
            //    Skillset = Skillset.OrderBy(x => !string.IsNullOrEmpty(x.Name) ? x.Name.Trim() : "").ToList();
            //}
            foreach (var item in transTechSkills)
            {
                //item.LevelName = level.Where(x => x.id == item.LevelId).Select(x => x.Name).FirstOrDefault();

                item.LevelName = ((LevelTechEnum)(item.LevelId)).GetDescription();
                //  item.SkillName = Skillset.Where(x => x.id == item.SkillId).Select(x => x.Name).FirstOrDefault();
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
                    Sheet.Cells[1, 11].Value = "Status";
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
                            Sheet.Cells[i, 11].Value = item.IRFStatus;
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

        public async Task<string> AutoGenerateSchedulingLink(DateTime eventStartdate, string SkillChannel, string EventName, string source, string meetingPlatform = null, bool? isAIInterview = false)
        {
            var model_Second = new List<daytimes_second>();
            var sun = new List<perdaytime>();
            var mon = new List<perdaytime>();
            sun.Add(new perdaytime { Start = "8", End = "23" });
            mon.Add(new perdaytime { Start = "10", End = "23" });
            //mon.Add(new perdaytime { Start = "18", End = "23" });
            model_Second.Add(new daytimes_second { Daytype = "sunday", dayCheck = true, dayvalue = sun });
            model_Second.Add(new daytimes_second { Daytype = "monday", dayCheck = true, dayvalue = mon });
            model_Second.Add(new daytimes_second { Daytype = "tuesday", dayCheck = true, dayvalue = mon });
            model_Second.Add(new daytimes_second { Daytype = "wednesday", dayCheck = true, dayvalue = mon });
            model_Second.Add(new daytimes_second { Daytype = "thursday", dayCheck = true, dayvalue = mon });
            model_Second.Add(new daytimes_second { Daytype = "friday", dayCheck = true, dayvalue = mon });
            model_Second.Add(new daytimes_second { Daytype = "saturday", dayCheck = true, dayvalue = sun });
            var model_report = new daytimes_report();
            model_report.setvalue = model_Second;
            if (isAIInterview.HasValue && isAIInterview.Value)
            {
                model_report.StartDate = eventStartdate;
                model_report.duration = 15;
                model_report.BufferTime = 0;

            }
            else
            {
                model_report.StartDate = eventStartdate.AddHours(12);
                model_report.duration = 60;
                model_report.BufferTime = 18;

            }
            model_report.EndDate = eventStartdate.AddDays(60);
            model_report.EventName = EventName;
            model_report.SkillChannel = SkillChannel;

            model_report.Concurrency = 10;
            model_report.TimeSlotDays = 6;
            model_report.MeetingPlatform = meetingPlatform;
            model_report.sourcevalue = source;
            var eventid = await _interviewSchedulerservice.AutoCreateEvent(model_report);
            var model = await _interviewSchedulerservice.FetchEventById(eventid);
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
                if (model != null && model.Count() > 0)
                {
                    if (model.FirstOrDefault().AvailbilityHour >= 3600 || model.FirstOrDefault().AvailbilityHour == 0)
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

                                string source = "ml";

                                var encryptedDate = await _encryptionHelper.EncryptDateTime(DateTime.Now);
                                string yesbutton = "https://portal.risebird.io/IRF/UpdateInterviewerAvailbilityStatusForIRF?status=1&uniqueid=" + item.uniqueid + "&Irfid=" + irfId + "&dec=" + encryptedDate;
                                string nobutton = "https://portal.risebird.io/IRF/UpdateInterviewerAvailbilityStatusForIRF?status=2&uniqueid=" + item.uniqueid + "&Irfid=" + irfId + "&dec=" + encryptedDate;
                                string dropMyProfileButton = "https://portal.risebird.io/IRF/UpdateInterviewerAvailbilityStatusForIRF?status=4&uniqueid=" + item.uniqueid + "&Irfid=" + irfId + "&dec=" + encryptedDate;
                                if (item.IsVideoRequired)
                                {
                                    Mail _mail = new Mail
                                    {
                                        TO = item.EmailId,
                                        Subject = "Mandatory Interviewer Video On : Current Availability (2 days) check for  " + item.JDTitle + " Risebird Interviews"
                                    };
                                    var emailresult = await _notificationHelper.SendCheckIntervieweravailbilitywithVideo(_mail, item.InterviewerName, yesbutton + "&rsc=" + source, nobutton + "&rsc=" + source, sb.ToString(), item.FlatRate, dropMyProfileButton + "&rsc=" + source, item.BasicSkill, irfId, item.CompanyTypeDesc, item.CountryTypeDesc, item.PlanTypeDesc, item.SelectedDurationMin, item.RejectedDurationMin);
                                }
                                else
                                {
                                    Mail _mail = new Mail
                                    {
                                        TO = item.EmailId,
                                        Subject = "Current Availability (2 days) check for  " + item.JDTitle + " Risebird Interviews"
                                    };
                                    var emailresult = await _notificationHelper.SendCheckIntervieweravailbility(_mail, item.InterviewerName, yesbutton + "&rsc=" + source, nobutton + "&rsc=" + source, sb.ToString(), item.FlatRate, dropMyProfileButton + "&rsc=" + source, item.BasicSkill, irfId, item.CompanyTypeDesc, item.CountryTypeDesc, item.PlanTypeDesc, item.SelectedDurationMin, item.RejectedDurationMin);
                                }

                                source = "wp";

                                yesbutton = yesbutton + "&rsc=" + source;
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
        public async Task<JsonResult> UpdateInterviewerAvailbilityStatusForIRF(int status, string uniqueid, int Irfid, string dec, string rsc = null)
        {
            //check link expiry befire hitting db

            try
            {
                //dec = dec.Replace(" ", "+");
                //var timeOfCommunication = await _encryptionHelper.DecryptDateTime(dec);
                //if (timeOfCommunication != null && (DateTime.Now - timeOfCommunication.Value).TotalMinutes <= 60)
                //{
                //    if (await _meetingService.UpdateInterviewerAvailbilityStatusForIRF(status, uniqueid, Irfid, rsc))
                //    {
                //        return Json("Status updated successfully.");
                //    }
                //    else
                //    {
                //        return Json("Failed to update status, Please try again");
                //    }
                //}
                //else
                //{
                //    return Json("Link Expired.");
                //}
                var result = await _meetingService.UpdateInterviewerAvailbilityStatusForIRF(status, uniqueid, Irfid, rsc);
                if (result.Item1)
                {
                    //if status = 1, send open interviews notification to the interviewer
                    //if (status == 1)
                    //{
                    //    var unassignedInterviewMeetings = await _meetingService.GetUnassignedOpenInterviews(Irfid);
                    //    List<string> interviewers = new List<string>
                    //    {
                    //        result.Item2
                    //    };
                    //    if (unassignedInterviewMeetings != null && unassignedInterviewMeetings.Count() > 0)
                    //    {
                    //        foreach (var meeting in unassignedInterviewMeetings)
                    //        {
                    //            await _notificationHelper.SendIRFBasedInterviewerMail(interviewers, meeting.interviewDate.ToString(), meeting.irfIntId, meeting.imid);

                    //        }

                    //    }
                    //}

                    return Json("Status updated successfully.");
                }
                else
                {
                    return Json("Failed to update status, Please try again");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Failed to update status, Please try again");
            }

        }
        public async Task<JsonResult> CheckAGLevelInterviewerAvailability(int assessmentGroupId)
        {
            try
            {
                var model = await _irfService.GetAvailableAssessmentGroupInterviewerMembers(assessmentGroupId);

                if (model != null && model.Count() > 0)
                {
                    if (model.FirstOrDefault().AGAvailabilityCommunicationTimeElapsed >= 3600 || model.FirstOrDefault().AGAvailabilityCommunicationTimeElapsed == 0)
                    {

                        foreach (var item in model)
                        {
                            if (!string.IsNullOrEmpty(item.EmailId))
                            {

                                string source = "ml";

                                var encryptedDate = await _encryptionHelper.EncryptDateTime(DateTime.Now);
                                string yesbutton = "https://portal.risebird.io/IRF/UpdateInterviewerAvailbilityStatusForAssessmentGroup?isAv=true&uniqueid=" + item.uniqueid + "&agId=" + assessmentGroupId + "&dec=" + encryptedDate;
                                string nobutton = "https://portal.risebird.io/IRF/UpdateInterviewerAvailbilityStatusForAssessmentGroup?isAv=false&uniqueid=" + item.uniqueid + "&agId=" + assessmentGroupId + "&dec=" + encryptedDate;
                                //string dropMyProfileButton = "https://portal.risebird.io/IRF/UpdateInterviewerAvailbilityStatusForAssessmentGroup?isAv=4&uniqueid=" + item.uniqueid + "&agId=" + irfId + "&dec=" + encryptedDate;

                                Mail _mail = new Mail
                                {
                                    TO = item.EmailId,
                                    Subject = "Current Availability (2 days) check for skills based on: " + item.AGName + " Risebird Interviews"
                                };
                                var emailresult = await _notificationHelper.CheckAGLevelInterviewerAvailability(_mail, item.InterviewerName, yesbutton + "&rsc=" + source, nobutton + "&rsc=" + source, item.AGName, assessmentGroupId, item.uniqueid);

                                source = "wp";

                                yesbutton = yesbutton + "&rsc=" + source;
                                WhatsAppVM _whatsAppVM = new WhatsAppVM();
                                _whatsAppVM.Emailid = item.EmailId;
                                _whatsAppVM.PrimarySkill = item.AGName;

                                _whatsAppVM.buttonlink = yesbutton.Replace("https://portal.risebird.io/IRF/", "");
                                _whatsAppVM.Event = "SkillChannel Interviewer Availbility";
                            }
                        }

                        //fire and forget method to make an ivr call to interviewers?
                        MakeIVRCallForAvailability(new AvailabilityCheckDTO { AssessmentGroupId = assessmentGroupId.ToString() });

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
                await _activityTrackerService.DebugLogger($"CheckAGLevelInterviewerAvailability function error : {ex.Message}, {ex.StackTrace}", "ivr", string.Empty);
                return Json("Fail");
            }
        }
        [HttpGet]
        public async Task<JsonResult> UpdateInterviewerAvailbilityStatusForAssessmentGroup(bool isAv, string uniqueid, int agId, string dec, string rsc = null)
        {
            //check link expiry befire hitting db

            try
            {
                //dec = dec.Replace(" ", "+");
                //var timeOfCommunication = await _encryptionHelper.DecryptDateTime(dec);
                //if (timeOfCommunication != null && (DateTime.Now - timeOfCommunication.Value).TotalMinutes <= 60)
                //{
                //    if (await _meetingService.UpdateInterviewerAvailbilityStatusForIRF(status, uniqueid, Irfid, rsc))
                //    {
                //        return Json("Status updated successfully.");
                //    }
                //    else
                //    {
                //        return Json("Failed to update status, Please try again");
                //    }
                //}
                //else
                //{
                //    return Json("Link Expired.");
                //}

                if (await _meetingService.UpdateInterviewerAvailbilityStatusForAssessmentGroup(isAv, uniqueid, agId, rsc))
                {
                    return Json("Status updated successfully.");
                }
                else
                {
                    return Json("Failed to update status, Please try again");
                }
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
        public async Task<IActionResult> GetAssessmentGroupInterviewerMembers(int Assessmentid, int irfId, string irfName)
        {
            try
            {
                ViewBag.Type = "ALL";
                var model = await _irfService.GetAssessmentGroupInterviewerMembers(Assessmentid);
                model = model.Select(x => { x.AvailableStatus = ((InterviewerAvailabilityStatus)Convert.ToInt32(x.AvailableStatus)).GetDescription(); return x; }).ToList();
                ViewBag.IRFId = irfId;
                ViewBag.IRFName = irfName;
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
        public async Task<JsonResult> BroadcastIRFmembers(string message, string subject, int irfid, int interviewId = 0, bool shouldSendEmail = true)
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
                            result = await _meetingService.StoreClientBroadcastMessage(model);

                        }
                        else
                        {
                            result = await _meetingService.BroadcastIRFmembers(model);

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
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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

        /// <summary>
        /// Transcribes the HM voice brief already stored on IRF.AudioComment using OpenAI Whisper (same path as AI Interview STT).
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> TranscribeVoiceInstruction(int irfId, bool isMultiLingual = false)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null)
                    return Json(new { success = false, message = "Unauthorized" });
                if (!await AuthorizeDhiMutatingAccessAsync(irfId))
                    return Json(new { success = false, message = "Forbidden" });

                var audioUrl = _irfService.FetchIRFAudioLink(irfId);
                if (string.IsNullOrWhiteSpace(audioUrl))
                    return Json(new { success = false, message = "No voice recording found. Record and upload audio first." });

                using var downloadClient = _httpClientFactory.CreateClient();
                await using var remoteStream = await downloadClient.GetStreamAsync(audioUrl);
                using var audioBuffer = new MemoryStream();
                await remoteStream.CopyToAsync(audioBuffer);
                audioBuffer.Position = 0;

                var transcript = await _speechToTextService.TranscribeAsync(audioBuffer, "voice_instruction.wav", isMultiLingual);
                if (!IsVoiceInstructionTranscriptionValid(transcript, isMultiLingual))
                    return Json(new { success = false, message = "Could not produce a clear transcript. Try re-recording in a quiet environment." });

                var emailid = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
                _irfService.SaveVoiceInstructionTranscript(irfId, transcript, emailid);
                var brief = _irfService.FetchVoiceInstructionBrief(irfId);
                return Json(new
                {
                    success = true,
                    transcript,
                    audioUrl = brief?.AudioUrl,
                    transcriptUpdatedOn = IndiaTimeHelper.ToUtcIso8601(brief?.TranscriptUpdatedOn)
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Transcription failed: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult FetchVoiceInstructionBrief(int irfid)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null)
                    return Json(new { success = false, message = "Unauthorized" });

                var brief = _irfService.FetchVoiceInstructionBrief(irfid);
                return Json(new
                {
                    success = true,
                    audioUrl = brief?.AudioUrl,
                    transcript = brief?.Transcript,
                    transcriptUpdatedOn = IndiaTimeHelper.ToUtcIso8601(brief?.TranscriptUpdatedOn),
                    transcriptUpdatedBy = brief?.TranscriptUpdatedBy
                });
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> UpdateVoiceInstructionTranscript(int irfId, string transcript, bool regenerateRubric = false)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null)
                    return Json(new { success = false, message = "Unauthorized" });
                if (!await AuthorizeDhiMutatingAccessAsync(irfId))
                    return Json(new { success = false, message = "Forbidden" });

                if (string.IsNullOrWhiteSpace(transcript))
                    return Json(new { success = false, message = "Transcript cannot be empty." });

                var emailid = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
                var saved = _irfService.SaveVoiceInstructionTranscript(irfId, transcript.Trim(), emailid);
                if (!saved)
                    return Json(new { success = false, message = "Could not save transcript." });

                var brief = _irfService.FetchVoiceInstructionBrief(irfId);
                return Json(new
                {
                    success = true,
                    transcript = brief?.Transcript,
                    transcriptUpdatedOn = IndiaTimeHelper.ToUtcIso8601(brief?.TranscriptUpdatedOn),
                    regenerateRubric
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> GenerateRubricFromVoice(int irfId, string transcript = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null)
                    return Json(new { success = false, message = "Unauthorized" });
                if (!await AuthorizeDhiMutatingAccessAsync(irfId))
                    return Json(new { success = false, message = "Forbidden" });

                var brief = _irfService.FetchVoiceInstructionBrief(irfId);
                var voiceText = !string.IsNullOrWhiteSpace(transcript)
                    ? transcript.Trim()
                    : brief?.Transcript?.Trim();
                if (string.IsNullOrWhiteSpace(voiceText))
                    return Json(new { success = false, message = "Save a voice transcript first (record audio and transcribe, or enter text manually)." });

                if (!string.IsNullOrWhiteSpace(transcript)
                    && !string.Equals(transcript.Trim(), brief?.Transcript?.Trim(), StringComparison.Ordinal))
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
                    _irfService.SaveVoiceInstructionTranscript(irfId, transcript.Trim(), emailid);
                }

                var irfVm = await _irfService.GetEditIRF(irfId, 0, false);
                if (irfVm == null)
                    return Json(new { success = false, message = "Skill channel not found." });

                var currentSkills = DmVoiceRubricRevisionHelper.GetScoreRubricSkills(irfVm.SkillCollection).ToList();
                AssignmentVM magDetails = null;
                if (int.TryParse(irfVm.AssessmentGroupId, out var assessmentGroupId) && assessmentGroupId > 0)
                    magDetails = await _irfRepository.GetAssessmentGroupDetails(assessmentGroupId);

                var revisionPrompt = DmVoiceRubricRevisionHelper.BuildRevisionUserMessage(
                    irfVm, currentSkills, magDetails, voiceText);

                var result = await _assistantServiceHelper.GenerateTechRubricRevision(revisionPrompt);
                if (result == null)
                    return Json(new { success = false, message = "Rubric revision returned no result." });

                var rawJson = result.Data?.FirstOrDefault()?.Content?.FirstOrDefault()?.Text?.Value;
                if (string.IsNullOrWhiteSpace(rawJson))
                    return Json(new { success = false, message = "Rubric revision returned empty content." });

                rawJson = DmVoiceRubricRevisionHelper.ExtractJsonFromAssistantText(rawJson);
                var model = DmVoiceRubricRevisionHelper.DeserializeRubricSchema(rawJson);
                var revisedSkills = DmVoiceRubricRevisionHelper.ToTransSkills(model, irfId);

                if (!revisedSkills.Any() && currentSkills.Any())
                    return Json(new { success = false, message = "No rubric skills could be parsed from the assistant response." });

                var magSkillNames = DmVoiceRubricRevisionHelper.BuildMagSkillNameSet(magDetails);
                var weightNormalization = DmVoiceRubricRevisionHelper.NormalizeWeightageTo100(revisedSkills);
                revisedSkills = weightNormalization.Skills;
                var diff = DmVoiceRubricRevisionHelper.CompareRubrics(currentSkills, revisedSkills, magSkillNames);
                var outcome = DmVoiceRubricRevisionHelper.BuildOutcome(
                    currentSkills, revisedSkills, diff, voiceText, weightNormalization);

                return Json(new
                {
                    success = true,
                    skillCount = outcome.RevisedSkills.Count,
                    changed = outcome.Changed,
                    removed = diff.Removed,
                    added = diff.Added,
                    modified = diff.Modified,
                    magWarnings = diff.MagWarnings,
                    message = outcome.Message,
                    weightsAdjusted = outcome.WeightsAdjusted,
                    totalWeightBefore = weightNormalization.TotalBefore,
                    pendingSave = outcome.Changed,
                    reloadRequired = false,
                    revisedSkills = DmVoiceRubricRevisionHelper.ToClientSkillPayload(outcome.RevisedSkills)
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        private static bool IsVoiceInstructionTranscriptionValid(string text, bool isMultilingual)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;
            if (!isMultilingual)
            {
                var cleaned = new string(text.Where(c => char.IsLetter(c) || char.IsDigit(c)).ToArray());
                if (text.Length == 0) return false;
                double validRatio = (double)cleaned.Length / text.Length;
                if (validRatio < 0.5)
                    return false;
            }
            return true;
        }

        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 1048576000)]
        [RequestSizeLimit(1048576000)]
        public async Task<JsonResult> UploadVideoInstructionFile()
        {
            if (Request.Form.Files.Count() > 0)
            {
                var irfid = Convert.ToInt32(Request.Form["irfid"].ToString());
                string uploadedVideoUrl = string.Empty;
                var model = Request.Form.Files[0];
                string outputFileName = model.FileName.Replace(" ", "").Replace("+", "").Replace("_", "");
                var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model);
                FileInfo fi = new FileInfo(outputFileName);
                if (fileMimeType == "video/mpeg" || fileMimeType == "video/mp4" || fileMimeType == "video/quicktime")
                {
                    await _azureStorageHelper.InsertImageBlob(outputFileName, "videoinstructions", model, fileMimeType);
                    var containerRef = _azureStorageHelper.GetCloudBlobContainer("videoinstructions");
                    var blob = containerRef.GetBlockBlobReference(outputFileName);
                    uploadedVideoUrl = blob.Uri.ToString();
                    var sas = _sasUrlHelper.CreateSasUrl("videoinstructions");
                    if (!string.IsNullOrEmpty(uploadedVideoUrl) && !string.IsNullOrEmpty(sas))
                    {
                        uploadedVideoUrl = uploadedVideoUrl + "?" + sas;
                    }
                    var result = _irfService.SaveIRFAudioLink(Convert.ToInt32(irfid), uploadedVideoUrl);
                    return Json("Upload completed successfully.");
                }
                else
                {
                    return Json("Only PDF file is allowed.");
                }
            }
            else
            {
                return Json("");
            }
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
                return RedirectToAction("List", "Template");
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

                        string _schedulinglink = await AutoGenerateSchedulingLink(model.StartDate, model.IRFId, model.JDTiltle + "_Scheduling", "Create", model.MeetingPlatform);

                        model.CalendlyLink = _schedulinglink;
                        var result = await _irfService.CreateSkillChannelFromTemplate(model, emailId, profileName);
                        //var result = await _irfService.CreateSkillChannelFromTemplate(model, emailId, profileName);
                        //End: Auto Generate Scheduling Link
                        if (result.Item1)
                        {

                            var irfDetails = await _irfService.GetSkillChannelDetailsByName(model.IRFId);

                            await _phoneScreenRepository.SetIRFPlivoAssistantUrlAsync(irfDetails?.id, model.IRFId, model.PlivoAssistantUrl);
                            await _phoneScreenRepository.SetIRFPlivoWhatsAppAssistantUrlAsync(irfDetails?.id, model.IRFId, model.PlivoWhatsAppAssistantUrl);
                            await _phoneScreenRepository.SetIRFElevenLabsAgentIdAsync(irfDetails?.id, model.IRFId, model.ElevenLabsAgentId);

                            FireAndForgetRequestToSendAvailabilityMails(model.AssessmentGroupId, irfDetails.id, result.Item2, irfDetails.PrimarySkillInvite, model.IRFId, model.JDTiltle);

                            TempData["SuccessErrorMessage"] = $"Skills Channel '{model.IRFId}' created Successfully!";
                            TempData["SuccessErrorMessage2"] = model.CalendlyLink;
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorCloneTemplateMessage"] = $"Failed to create Skills Channel from {model.TemplateName}!";
                            return RedirectToAction("List", "Template");
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
                return RedirectToAction("List", "Template");
            }
        }

        /// <summary>
        /// Public (email + uniqueId) interview-link requests: the link must belong to a client user, and the
        /// client and plan rate are resolved server-side so posted ClientId / RateComputedForClient are ignored.
        /// </summary>
        private async Task<bool> PreparePublicInterviewLinkModelAsync(IRFVm model, string emailId)
        {
            var clientId = await _clientService.GetClientByEmailId(emailId);
            if (clientId <= 0 || await _meetingService.GetUniqueUserByEmailId(emailId, model.UniqueId) <= 0)
                return false;
            await ApplyClientAndPlanRateAsync(model, clientId);
            return true;
        }

        /// <summary>Overwrites posted ClientId and RateComputedForClient with server-resolved values.</summary>
        private async Task ApplyClientAndPlanRateAsync(IRFVm model, int clientId)
        {
            model.ClientId = clientId;
            model.RateComputedForClient = 0;
            if (clientId > 0 && model.MarketPlacePlanType > 0)
            {
                var rates = await _clientService.GenerateDynamicRateByCurrentTimeOfWeek(clientId);
                model.RateComputedForClient = model.MarketPlacePlanType switch
                {
                    2 => rates?.StandardPlanRate ?? 0,
                    4 => rates?.ProPlanRate ?? 0,
                    5 => rates?.AdvancedPlanRate ?? 0,
                    _ => 0
                };
            }
            // Marketplace channel only when created from Template/Marketplace (its popup posts IsMarketPlaceChannel)
            // and a plan price applies; Template/Index creates normal channels.
            model.IsMarketPlaceChannel = model.IsMarketPlaceChannel && model.RateComputedForClient > 0;
        }

        //[Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> CreateInterviewLink(IRFVm model)
        {
            string emailId = string.Empty;
            // The popup posts the template name as JDTiltle; use it in messages. Failures return to the page used.
            if (string.IsNullOrEmpty(model.TemplateName)) model.TemplateName = model.JDTiltle;
            var templatePage = model.IsMarketPlaceChannel ? "Marketplace" : "Index";
            try
            {
                if (!string.IsNullOrEmpty(model.UniqueId) && !string.IsNullOrEmpty(model.PublicUserEmailId) && !string.IsNullOrEmpty(model.AccessType) && model.AccessType == "Public")
                {
                    emailId = model.PublicUserEmailId.Trim();
                    if (!await PreparePublicInterviewLinkModelAsync(model, emailId))
                        return RedirectToAction("SignIn", "Session");
                    string uploadedFileURL = string.Empty;
                    //if (model.ClientId == 0)
                    //    model.ClientId = await _clientService.GetClientByEmailId(emailId);
                    if (model.JDFile != null)
                    {


                        int virusScanResult = 0;
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
                                return RedirectToAction(templatePage, "Template");
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

                    string _schedulinglink = await AutoGenerateSchedulingLink(model.StartDate, model.IRFId, model.JDTiltle + "_Scheduling", "CreateInterviewLink", model.MeetingPlatform);

                    model.CalendlyLink = _schedulinglink;
                    var result = await _irfService.CreateSkillChannelUsingTemplateByInterviewLinkPopUp(model, emailId, "Client");
                    //End: Auto Generate Scheduling Link
                    if (result.Item1)
                    {

                        //get irfDetails by irfName
                        var irfDetails = await _irfService.GetSkillChannelDetailsByName(model.IRFId);

                        FireAndForgetRequestToSendAvailabilityMails(model.AssessmentGroupId, irfDetails.id, result.Item2, irfDetails.PrimarySkillInvite, model.IRFId, model.JDTiltle);


                        TempData["SuccessErrorMessageMarketPlace1"] = $"Skills Channel '{model.IRFId}' created Successfully!";
                        TempData["SuccessErrorMessageMarketPlace2"] = model.CalendlyLink;
                        //return RedirectToAction("Index");
                        return RedirectToAction("FetchInterviewerCommunicator", "Candidate", new { emailid = model.PublicUserEmailId, id = model.UniqueId });
                    }
                    else
                    {
                        TempData["SuccessErrorMessageMarketPlace1"] = $"Failed to create Skills Channel from {model.TemplateName}!";
                        return RedirectToAction("FetchInterviewerCommunicator", "Candidate", new { emailid = model.PublicUserEmailId, id = model.UniqueId });

                    }
                }

                else if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    if (/*profileName == "Admin" || profileName == "HR" ||*/ profileName == "Client" /*|| profileName == "Recruiter" || profileName == "CuratedUser" || profileName == "Finance" || profileName == "SeniorHR"*/)
                    {

                        string uploadedFileURL = string.Empty;
                        // Client users can only create for their own client; staff keep the selected client.
                        if (profileName == "Client" || model.ClientId == 0)
                            model.ClientId = await _clientService.GetClientByEmailId(emailId);
                        await ApplyClientAndPlanRateAsync(model, model.ClientId);
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
                                        return RedirectToAction(templatePage, "Template");
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

                        string _schedulinglink = await AutoGenerateSchedulingLink(model.StartDate, model.IRFId, model.JDTiltle + "_Scheduling", "CreateInterviewLinkelse", model.MeetingPlatform);

                        model.CalendlyLink = _schedulinglink;
                        var result = await _irfService.CreateSkillChannelUsingTemplateByInterviewLinkPopUp(model, emailId, profileName);
                        //End: Auto Generate Scheduling Link
                        if (result.Item1)
                        {

                            //get irfDetails by irfName
                            var irfDetails = await _irfService.GetSkillChannelDetailsByName(model.IRFId);

                            FireAndForgetRequestToSendAvailabilityMails(model.AssessmentGroupId, irfDetails.id, result.Item2, irfDetails.PrimarySkillInvite, model.IRFId, model.JDTiltle);


                            TempData["SuccessErrorMessage"] = $"Skills Channel '{model.IRFId}' created Successfully!";
                            TempData["SuccessErrorMessage2"] = model.CalendlyLink;
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorCloneTemplateMessage"] = $"Failed to create Skills Channel from {model.TemplateName}!";
                            return RedirectToAction(templatePage, "Template");
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
                return RedirectToAction(templatePage, "Template");
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

        //[Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> CreateInterviewLinkV1(IRFVm model)
        {
            string emailId = string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(model.UniqueId) && !string.IsNullOrEmpty(model.PublicUserEmailId) && !string.IsNullOrEmpty(model.AccessType) && model.AccessType == "Public")
                {
                    emailId = model.PublicUserEmailId.Trim();
                    if (!await PreparePublicInterviewLinkModelAsync(model, emailId))
                        return RedirectToAction("SignIn", "Session");
                    string uploadedFileURL = string.Empty;
                    //if (model.ClientId == 0)
                    //    model.ClientId = await _clientService.GetClientByEmailId(emailId);
                    if (model.JDFile != null)
                    {


                        int virusScanResult = 0;
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
                                TempData["SuccessErrorPublicCloneTemplateMessage"] = $"File upload failed. Virus Detected! Failed to create Skills Channel from {model.TemplateName}!";
                                return RedirectToAction("ListPb", "Template", new { emailId = model.PublicUserEmailId, uniqueId = model.UniqueId, type = "pbc", profile = "Client" });

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

                    string _schedulinglink = await AutoGenerateSchedulingLink(model.StartDate, model.IRFId, model.JDTiltle + "_Scheduling", "CreateInterviewLinkV1", model.MeetingPlatform, isAIInterview: model.IsAIInterview);

                    model.CalendlyLink = _schedulinglink;
                    var result = await _irfService.CreateSkillChannelUsingTemplateByInterviewLinkPopUpV1(model, emailId, "Client");
                    //End: Auto Generate Scheduling Link
                    if (result.Item1)
                    {
                        var irfDetails = await _irfService.GetSkillChannelDetailsByName(model.IRFId);

                        if (!model.IsAIInterview)
                        {
                            FireAndForgetRequestToSendAvailabilityMails(model.AssessmentGroupId, irfDetails.id, result.Item2, irfDetails.PrimarySkillInvite, model.IRFId, model.JDTiltle);

                        }

                        TempData["SuccessErrorPublicCloneTemplateMessage"] = $"Skills Channel '{model.IRFId}' created Successfully!";
                        TempData["SuccessErrorPublicCloneTemplateMessage2"] = model.CalendlyLink;
                        //TempData["SuccessErrorCloneTemplateMessage3"] = "https://portal.risebird.io/IRF/Index";
                        return RedirectToAction("ListPb", "Template", new { emailId = model.PublicUserEmailId, uniqueId = model.UniqueId, type = "pbc", profile = "Client" });

                        //return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["SuccessErrorCloneTemplateMessage"] = $"Failed to create Skills Channel from {model.TemplateName}!";
                        return RedirectToAction("ListPb", "Template", new { emailId = model.PublicUserEmailId, uniqueId = model.UniqueId, type = "pbc", profile = "Client" });
                    }

                }
                else if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin"/*|| profileName == "Recruiter" || profileName == "CuratedUser" || profileName == "Finance" || profileName == "SeniorHR"*/)
                    {

                        string uploadedFileURL = string.Empty;
                        // Client users can only create for their own client; staff keep the selected client.
                        if (profileName == "Client" || model.ClientId == 0)
                            model.ClientId = await _clientService.GetClientByEmailId(emailId);
                        await ApplyClientAndPlanRateAsync(model, model.ClientId);
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

                        string _schedulinglink = await AutoGenerateSchedulingLink(model.StartDate, model.IRFId, model.JDTiltle + "_Scheduling", "CreateInterviewLinkV1", model.MeetingPlatform);

                        model.CalendlyLink = _schedulinglink;
                        var result = await _irfService.CreateSkillChannelUsingTemplateByInterviewLinkPopUpV1(model, emailId, profileName);
                        //End: Auto Generate Scheduling Link
                        if (result.Item1)
                        {
                            var irfDetails = await _irfService.GetSkillChannelDetailsByName(model.IRFId);

                            if (!model.IsAIInterview)
                            {
                                FireAndForgetRequestToSendAvailabilityMails(model.AssessmentGroupId, irfDetails.id, result.Item2, irfDetails.PrimarySkillInvite, model.IRFId, model.JDTiltle);
                            }
                            //else
                            //{
                            //    //proctor: same mail only to interviewers with IsProctor set to true
                            //}

                            TempData["SuccessErrorCloneTemplateMessage"] = $"Skills Channel '{model.IRFId}' created Successfully!";
                            TempData["SuccessErrorCloneTemplateMessage2"] = model.CalendlyLink;
                            //TempData["SuccessErrorCloneTemplateMessage3"] = "https://portal.risebird.io/IRF/Index";
                            //return RedirectToAction("List", "Template");
                            return RedirectToAction("GetCandidates", "Candidate", new { id = irfDetails.id, irfName = model.IRFId });
                            //return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorCloneTemplateMessage"] = $"Failed to create Skills Channel from {model.TemplateName}!";
                            return RedirectToAction("List", "Template");
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
                return RedirectToAction("List", "Template");
            }
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
                            var _schedulinglink = await AutoGenerateSchedulingLink(model.StartDate, model.IRFId, model.JDTiltle + "_Scheduling", "Edit");
                            model.CalendlyLink = _schedulinglink;
                        }

                        //End: Auto Generate Scheduling Link
                        if (await _irfService.EditSkillChannelCreatedFromTemplate(model, emailId))
                        {
                            await _phoneScreenRepository.SetIRFPlivoAssistantUrlAsync(model.id, model.IRFId, model.PlivoAssistantUrl);
                            await _phoneScreenRepository.SetIRFPlivoWhatsAppAssistantUrlAsync(model.id, model.IRFId, model.PlivoWhatsAppAssistantUrl);
                            await _phoneScreenRepository.SetIRFElevenLabsAgentIdAsync(model.id, model.IRFId, model.ElevenLabsAgentId);
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
                                if (model.Where(x => x.IsSME)?.Count() > 25)
                                {
                                    ModelState.AddModelError("", "Cannot add more than 25 Subject Matter Experts for an Assessment Group!");
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
                        Subject = "We Value Your Feedback on the " + JDTitle + "(" + item.IRFName + ") Interview Drive with Risebird"
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Client" || profileName == "SeniorInterviewerManagement")

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
        #region Delivery Head Instruction

        private async Task<bool> AuthorizeClientShareAccessAsync(int irfId, string type = null)
        {
            if (irfId <= 0) return false;
            var email = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
            var profile = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
            if (string.IsNullOrEmpty(email) || !DeliveryHeadInstructionViewMode.IsClientRoleProfile(profile))
                return false;
            return await _clientService.VerifyClientAuthenticity(irfId, email);
        }

        /// <summary>DM/SME full access; client role limited to associated IRF.</summary>
        private async Task<bool> AuthorizeDhiMutatingAccessAsync(int irfId, string type = null)
        {
            if (irfId <= 0) return false;
            var email = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
            if (string.IsNullOrEmpty(email)) return false;
            var profile = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
            if (DeliveryHeadInstructionViewMode.IsOperationalProfile(profile)) return true;
            if (DeliveryHeadInstructionViewMode.IsClientRoleProfile(profile))
                return await AuthorizeClientShareAccessAsync(irfId, type);
            if (string.Equals(profile, "Interviewer", StringComparison.OrdinalIgnoreCase))
                return await _interviewerService.IsSmeAssignedToIrfAsync(email, irfId);
            return false;
        }

        private bool IsClientShareRequest(int irfId, string type = null)
        {
            var profile = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
            if (DeliveryHeadInstructionViewMode.IsOperationalProfile(profile))
                return false;
            if (DeliveryHeadInstructionViewMode.IsClientRoleProfile(profile))
                return true;
            return DeliveryHeadInstructionViewMode.IsClientShareForIrf(HttpContext, irfId)
                || DeliveryHeadInstructionViewMode.IsClientShareType(type);
        }

        [HttpGet]
        public async Task<JsonResult> GetDmInterviewerPanelMetrics(int irfId, string Type = null)
        {
            try
            {
                var isClientShare = IsClientShareRequest(irfId, Type);
                var isSmeView = DeliveryHeadInstructionViewMode.IsSmeViewForIrf(HttpContext, irfId);
                if (isClientShare)
                {
                    if (!await AuthorizeClientShareAccessAsync(irfId, Type))
                        return Json(new { ok = false });
                }
                else if (!isSmeView && HttpContext.Session.GetString("email") == null)
                {
                    return Json(new { ok = false });
                }

                if (isSmeView)
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
                    if (!await _interviewerService.IsSmeAssignedToIrfAsync(emailid, irfId))
                    {
                        return Json(new { ok = false });
                    }
                }

                var model = await _irfService.GetEditIRF(irfId, 0, false);
                var source = model?.InterviewerCollection?.AsEnumerable();
                if (isClientShare || isSmeView)
                {
                    source = source?.Where(x => x != null && x.IsActive);
                }

                if (isClientShare)
                {
                    var interviewers = source?
                        .Select(iv =>
                        {
                            DeliveryHeadInstructionViewMode.StripInterviewerForClientView(iv);
                            return DeliveryHeadInstructionViewMode.BuildClientPanelMetricsEntry(iv);
                        })
                        .ToList();
                    return Json(new { ok = true, interviewers });
                }

                var interviewersInternal = source?
                    .Select(iv =>
                    {
                        if (isSmeView)
                        {
                            DeliveryHeadInstructionViewMode.StripInterviewerForSmeView(iv);
                        }

                        return new
                        {
                            userId = iv.UserId,
                            email = (isClientShare || isSmeView) ? "" : (iv.EmailId ?? ""),
                            companyName = !string.IsNullOrWhiteSpace(iv.CompanyName)
                                ? iv.CompanyName.Trim()
                                : (!string.IsNullOrWhiteSpace(iv.EmploymentComment) ? iv.EmploymentComment.Trim() : null),
                            futureAssignedCount = iv.TotalFuture,
                            lastLoginAt = iv.LastLoginAt.HasValue ? iv.LastLoginAt.Value.ToString("o") : (string)null,
                            lastInterviewConductedAt = iv.LastInterviewConductedAt.HasValue
                                ? iv.LastInterviewConductedAt.Value.ToString("o") : (string)null,
                            perSuccessRate = (isClientShare || isSmeView) ? 0 : iv.PerSuccessRate,
                            surcharge = (isClientShare || isSmeView) ? 0 : iv.Surcharge,
                            clientAction = (isClientShare || isSmeView) ? null : iv.ClientAction
                        };
                    })
                    .ToList();

                return Json(new { ok = true, interviewers = interviewersInternal });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetDmInterviewerCvState(int irfId, int userId, string Type = null)
        {
            try
            {
                var isClientShare = IsClientShareRequest(irfId, Type);
                var isSmeView = DeliveryHeadInstructionViewMode.IsSmeViewForIrf(HttpContext, irfId);
                if (isClientShare)
                {
                    if (!await AuthorizeClientShareAccessAsync(irfId, Type))
                        return Json(new { ok = false });
                }
                else if (!isSmeView && HttpContext.Session.GetString("email") == null)
                {
                    return Json(new { ok = false });
                }

                if (isSmeView)
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
                    if (!await _interviewerService.IsSmeAssignedToIrfAsync(emailid, irfId))
                    {
                        return Json(new { ok = false });
                    }
                }

                var model = await _irfService.GetEditIRF(irfId, 0, false);
                var iv = model?.InterviewerCollection?
                    .Where(x => x.IsActive)
                    .FirstOrDefault(x => x.UserId == userId);
                if (iv == null)
                {
                    return Json(new { ok = false });
                }

                if (isClientShare)
                {
                    DeliveryHeadInstructionViewMode.StripInterviewerForClientView(iv);
                    return Json(DeliveryHeadInstructionViewMode.BuildClientCvStatePayload(iv));
                }

                if (isSmeView)
                {
                    DeliveryHeadInstructionViewMode.StripInterviewerForSmeView(iv);
                }

                return Json(new
                {
                    ok = true,
                    resumeLink = isClientShare ? "" : (iv.ResumeLink ?? ""),
                    codedCvLink = DmIvCodedCvHelper.GetDisplayCodedCvLink(iv.CodedResumeLink) ?? "",
                    cvCode = DmIvCodedCvHelper.HasDisplayCodedCv(iv.CodedResumeLink) ? (iv.NickName ?? "CV") : "",
                    isResumeRedactAttempted = iv.IsResumeRedactAttempted,
                    aiVerdict = iv.AIVerdict ?? "",
                    aiScore = iv.AIScreeningScore,
                    aiReasonFull = iv.OpenAIScreeningResponse ?? "",
                    futureAssignedCount = iv.TotalFuture,
                    perSuccessRate = (isClientShare || isSmeView) ? 0 : iv.PerSuccessRate,
                    surcharge = (isClientShare || isSmeView) ? 0 : iv.Surcharge
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false });
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetClientShareInterviewerInsights(int irfId, int userId, string Type = null)
        {
            var isClientShare = IsClientShareRequest(irfId, Type);
            var isSmeView = DeliveryHeadInstructionViewMode.IsSmeViewForIrf(HttpContext, irfId);

            if (isClientShare || isSmeView)
            {
                return StatusCode(403);
            }

            return Unauthorized();
        }

        [HttpGet]
        public async Task<IActionResult> DeliveryHeadInstruction(string irfid, string Type = null)//Type = "ublc" stands for client share link
        {
            var resolvedIrfId = await _irfService.ResolveSkillChannelNumericIdAsync(irfid);
            ViewBag.PageType = "";
            ViewBag.IsClientShareView = false;
            ViewBag.IsSmeView = false;

            var hasSession = HttpContext.Session.GetString("email") != null
                || HttpContext.Session.GetString("profileName") != null;
            var clientEntryRequested = DeliveryHeadInstructionViewMode.WantsClientShareEntry(HttpContext, Type);

            if (!hasSession)
            {
                if (clientEntryRequested)
                    return StatusCode(403, "You do not have access to this skill channel.");
                if (User.Identity?.IsAuthenticated == true)
                    return RedirectToAction("Index", "Home");
                return RedirectToAction("SignIn", "Session");
            }

            var emailid = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
            var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
            ViewBag.profileName = profileName;
            var jobLevels = JobLevelHelper.SelectListOptions().Select(x => new { x.ID, x.Name });
            ViewBag.JobLevelList = new SelectList(jobLevels, "ID", "Name");

            if (resolvedIrfId <= 0)
            {
                if (DeliveryHeadInstructionViewMode.IsClientRoleProfile(profileName))
                    return StatusCode(403, "You do not have access to this skill channel.");
                ViewBag.Message = "Skill Channel not found.";
                return View(new IRFVm { id = 0 });
            }

            if (string.Equals(profileName, "Interviewer", StringComparison.OrdinalIgnoreCase))
            {
                var isSmeForIrf = await _interviewerService.IsSmeAssignedToIrfAsync(emailid, resolvedIrfId);
                if (!isSmeForIrf)
                {
                    ViewBag.Message = "You do not have access to this skill channel.";
                    return View(new IRFVm { id = resolvedIrfId });
                }

                ViewBag.IsSmeView = true;
                DeliveryHeadInstructionViewMode.SetSmeSession(HttpContext, resolvedIrfId);
                var smeModel = await _irfService.GetEditIRF(resolvedIrfId, 0, false);
                if (smeModel == null)
                {
                    ViewBag.Message = "Skill Channel not found.";
                    return View(new IRFVm { id = resolvedIrfId });
                }

                DeliveryHeadInstructionViewMode.ApplySmeViewFilter(smeModel);
                smeModel.DeliveryHeadSampleQuestions = await _clonableSkillChannelTemplateService.GetDeliveryHeadSampleQuestions(resolvedIrfId);
                smeModel.AudioComment = _irfService.FetchIRFAudioLink(resolvedIrfId);
                var smeVoiceBrief = _irfService.FetchVoiceInstructionBrief(resolvedIrfId);
                smeModel.VoiceInstructionTranscript = smeVoiceBrief?.Transcript;
                smeModel.VoiceInstructionTranscriptUpdatedOn = smeVoiceBrief?.TranscriptUpdatedOn;
                smeModel.CandidateVideo = (await _irfService.GetVideoConsentForm(resolvedIrfId));
                await checkSkillSetandSeperate(smeModel);
                return View(smeModel);
            }

            if (DeliveryHeadInstructionViewMode.IsClientRoleProfile(profileName))
            {
                if (!await AuthorizeClientShareAccessAsync(resolvedIrfId, Type))
                    return StatusCode(403, "You do not have access to this skill channel.");

                ViewBag.PageType = "Public";
                ViewBag.IsClientShareView = true;
                DeliveryHeadInstructionViewMode.SetClientShareSession(HttpContext, resolvedIrfId);

                var clientModel = await _irfService.GetEditIRF(resolvedIrfId, 0, false);
                if (clientModel == null)
                    return StatusCode(403, "You do not have access to this skill channel.");

                DeliveryHeadInstructionViewMode.ApplyClientViewFilter(clientModel);
                clientModel.DeliveryHeadSampleQuestions = await _clonableSkillChannelTemplateService.GetDeliveryHeadSampleQuestions(resolvedIrfId);
                clientModel.AudioComment = _irfService.FetchIRFAudioLink(resolvedIrfId);
                var clientVoiceBrief = _irfService.FetchVoiceInstructionBrief(resolvedIrfId);
                clientModel.VoiceInstructionTranscript = clientVoiceBrief?.Transcript;
                clientModel.VoiceInstructionTranscriptUpdatedOn = clientVoiceBrief?.TranscriptUpdatedOn;
                await checkSkillSetandSeperate(clientModel);
                return View(clientModel);
            }

            var model = await _irfService.GetEditIRF(resolvedIrfId, 0, false);
            if (model == null)
            {
                ViewBag.Message = "Skill Channel not found.";
                return View(new IRFVm { id = resolvedIrfId });
            }

            model.DeliveryHeadSampleQuestions = await _clonableSkillChannelTemplateService.GetDeliveryHeadSampleQuestions(resolvedIrfId);
            model.AudioComment = _irfService.FetchIRFAudioLink(resolvedIrfId);
            var voiceBrief = _irfService.FetchVoiceInstructionBrief(resolvedIrfId);
            model.VoiceInstructionTranscript = voiceBrief?.Transcript;
            model.VoiceInstructionTranscriptUpdatedOn = voiceBrief?.TranscriptUpdatedOn;
            model.CandidateVideo = (await _irfService.GetVideoConsentForm(resolvedIrfId));
            await checkSkillSetandSeperate(model);
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeliveryHeadInstruction(IRFVm model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (!await AuthorizeDhiMutatingAccessAsync(model?.id ?? 0))
                        return StatusCode(403, "You do not have access to this skill channel.");
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var existingSkills = (await _irfRepository.GetSkillsbyIRFid(model.id))?.ToList() ?? new List<TransSkill>();
                    var skillsToSave = BuildDeliveryHeadRubricSkillsForSave(model, existingSkills);
                    var formPostedRubric = Request?.Form?.Keys?.Any(k =>
                        k != null && k.StartsWith("SkillCollection[", StringComparison.OrdinalIgnoreCase)) == true;
                    if (formPostedRubric
                        && !skillsToSave.Any(x => x.SkillSet == 1)
                        && existingSkills.Any(x => x.SkillSet == 1))
                    {
                        ModelState.AddModelError(string.Empty, "Score rubric could not be saved. Please refresh the page and try again.");
                        await checkSkillSetandSeperate(model);
                        var jobLevelsErr = JobLevelHelper.SelectListOptions().Select(x => new { x.ID, x.Name });
                        ViewBag.JobLevelList = new SelectList(jobLevelsErr, "ID", "Name");
                        if (DeliveryHeadInstructionViewMode.IsClientRoleProfile(profileName))
                        {
                            ViewBag.IsClientShareView = true;
                            ViewBag.PageType = "Public";
                            DeliveryHeadInstructionViewMode.ApplyClientViewFilter(model);
                        }
                        return View(model);
                    }
                    foreach (var item in skillsToSave)
                    {
                        item.IRFId = model.id;
                    }
                    var result = await _irfService.UpdateTechSkills(skillsToSave);
                    if (result)
                    {
                        return RedirectToAction("DeliveryHeadInstruction", new { irfid = model.id });
                    }
                    var level = await _irfRepository.GetLevelTech();
                    level = level.Where(x => x.id != 1 && x.id != 7).ToList();
                    ViewBag.LevelTech = new SelectList(level, "id", "Name");
                    var Skillset = await _irfRepository.GetSkills();
                    ViewBag.Skill = new SelectList(Skillset.ToList(), "id", "Name");
                    var jobLevels = JobLevelHelper.SelectListOptions().Select(x => new { x.ID, x.Name });
                    ViewBag.JobLevelList = new SelectList(jobLevels, "ID", "Name");
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
                return null;
            }
        }

        [HttpPost]
        public async Task<JsonResult> VerifyClientAuthenticityForDHI(int irfId, string emailId) // DHI stands for Delivery Head Instructions
        {
            //HttpContext.Session.SetString("PublicUrlAccessor", JsonConvert.SerializeObject(Emailid));
            //var currentUrl = Request.Scheme + "://" + Request.Host.Value + Request.Path;
            //_trackPublicUrlAccessor.SendTelemetry(JsonConvert.SerializeObject(Emailid), currentUrl);
            //bool result = await _clientService.VerifyClientAuthenticity(irfId, emailId);
            //var logovalue = await _clientService.Fetchlogoforclient(Emailid, id);
            //if (string.IsNullOrEmpty(logovalue))
            //{
            //    HttpContext.Session.SetString("Logo", "");
            //}
            //else
            //    HttpContext.Session.SetString("Logo", logovalue);
            if (await _clientService.VerifyClientAuthenticity(irfId, emailId))
            {
                // Only start a session once the email is verified for this IRF.
                HttpContext.Session.SetString("email", JsonConvert.SerializeObject(emailId));
                HttpContext.Session.SetString("profileName", "Public");
                return Json("success");
            }
            else
            {
                return Json("This User does not have access");
            }
        }

        [HttpPost]
        public async Task<JsonResult> UpdateJobLevel(int irfid, int joblevel)
        {
            var result = await _irfService.UpdateJobLevel(irfid, joblevel);
            return Json("Successfully");
        }
        [HttpPost]
        public async Task<JsonResult> UpdateDeficultyLevel(int irfid, int deficultylevel)
        {
            var result = await _irfService.UpdateDeficultyLevel(irfid, deficultylevel);
            return Json("Successfully");
        }
        [HttpPost]
        public async Task<JsonResult> UpdateFormat(int irfid, string format)
        {
            var result = await _irfService.UpdateFormat(irfid, format);
            return Json("Successfully");
        }
        [HttpPost]
        public async Task<JsonResult> GetFormatanddifficultylevel(int irfid)
        {
            var result = await _irfService.GetFormatanddifficultylevel(irfid);
            if (result == null)
            {
                result = new IRFInstructionDetailsVM();
            }
            return Json(result);
        }
        [HttpPost]
        public JsonResult TextInstruction(int irfId, string Comment)
        {
            try
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                BroadcastModel model = new BroadcastModel();
                model.IRFId = irfId;
                model.Message = Comment;
                model.SentBy = emailid;
                var result = _meetingService.StoreClientBroadcastMessage(model);
                return Json("Successfully");
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Text Instruction failed, Please try again.");
            }
        }
        [HttpPost]
        public async Task<JsonResult> GetTextInstruction(int irfid)
        {
            var textInstruction = await _irfService.GetTextInstructionService(irfid);
            return Json(textInstruction);
        }

        [HttpPost]
        public async Task<JsonResult> GenerateEmbeddableResumeSearchUrl(string irfId)
        {
            try
            {
                string[] indices = new string[] { irfId };
                var result = await _affIndaResumeParserHelper.GenerateEmbeddableResumeSearchUrl(indices);
                if (!string.IsNullOrEmpty(result))
                {
                    await _irfService.UpdateSkillChannelEmbeddableUrl(irfId, result);
                }
                return Json(result);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        /// <summary>
        /// these questions are for the interviewers to refer to while taking an interview. This is not the same as the sample questions that the interviewer adds to the skill channel. Names are same and hence confusing. These samples questions are saved in a different table altogerther and are not visible in the Report drop-down. Purely for reference purposes.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> SkillChannelSampleQuestions(int id)
        {
            if (HttpContext.Session.GetString("email") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                TechSkillComments model = new TechSkillComments();
                //var skills = await _irfService.GetTechSkills(id);
                var skills = await _irfService.GetIRFTechSkills(id);
                //ViewBag.Skills = new SelectList(skills, "id", "Name");
                ViewBag.Skills = new SelectList(skills);
                model.IRFId = id;
                model.Profile = profileName;
                model.CreatedBy = emailid;
                // Add question on the HM page loads this via AJAX into the old popup.
                // A normal browser navigation (the "Open AI Suggested Questions" link) gets the full AI page.
                if (string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
                {
                    return PartialView("_SkillChannelSampleQuestionsModal", model);
                }
                return View(model);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [HttpGet]
        public async Task<IActionResult> SkillChannelSampleQuestionsModal(int id)
        {
            if (HttpContext.Session.GetString("email") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                TechSkillComments model = new TechSkillComments();
                var skills = await _irfService.GetIRFTechSkills(id);
                ViewBag.Skills = new SelectList(skills);
                model.IRFId = id;
                model.Profile = profileName;
                model.CreatedBy = emailid;
                return PartialView("_SkillChannelSampleQuestionsModal", model);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [HttpPost]
        [DisableRequestSizeLimit]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SkillChannelSampleQuestions(TechSkillComments model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var questionsCollection = model.SkillCollection.Select(x => (x.Comment, x.SkillName, x.CreatedBy)).Where(x => x.CreatedBy == model.CreatedBy);

                    if (questionsCollection.Count() != questionsCollection.Distinct().Count())
                    {
                        //duplicates exist
                        TempData["SuccessErrorMessageQuestions"] = "The skill-question combination added by you should contain unique entries.";
                        return RedirectToAction("DeliveryHeadInstruction", new { irfid = model.IRFId });
                    }


                    foreach (var question in model.SkillCollection ?? new List<SkillComment>())
                    {

                        if ((question.Comment?.Length ?? 0) > MaxSampleQuestionLength
                            || (question.ExpectedAnswer?.Length ?? 0) > MaxSampleQuestionLength)
                        {
                            TempData["SuccessErrorMessageQuestions"] = $"Questions and expected answers must not exceed {MaxSampleQuestionLength} characters.";
                            return RedirectToAction("DeliveryHeadInstruction", new { irfid = model.IRFId });
                        }

                        if (!string.IsNullOrEmpty(question.QuestionGist) && question.QuestionGist.Length > MaxSampleQuestionLength)
                        {
                            TempData["SuccessErrorMessageQuestions"] = $"The question gist must not exceed {MaxSampleQuestionLength} characters.";
                            return RedirectToAction("DeliveryHeadInstruction", new { irfid = model.IRFId });
                        }

                        if (!question.IsCodingQuestion)
                        {
                            question.CodingTimeLimitInMinutes = null;
                            continue;
                        }

                        if (!question.CodingTimeLimitInMinutes.HasValue
                            || question.CodingTimeLimitInMinutes.Value < 1
                            || question.CodingTimeLimitInMinutes.Value > 180)
                        {
                            TempData["SuccessErrorMessageQuestions"] = "A coding question needs a time limit between 1 and 180 minutes.";
                            return RedirectToAction("DeliveryHeadInstruction", new { irfid = model.IRFId });
                        }

                        if (string.IsNullOrWhiteSpace(question.QuestionGist))
                        {
                            TempData["SuccessErrorMessageQuestions"] = "A coding question needs a gist. The avatar narrates the gist because code cannot be read aloud.";
                            return RedirectToAction("DeliveryHeadInstruction", new { irfid = model.IRFId });
                        }
                    }

                    if (await _irfService.AddIRFSampleQuestions(model))
                    {
                        TempData["SuccessErrorMessageQuestions"] = "Question(s) saved Successfully!";
                    }
                    else
                    {
                        TempData["SuccessErrorMessageQuestions"] = "Failed to save Question(s)!";
                    }
                    return Redirect($"/IRF/DeliveryHeadInstruction?irfid={model.IRFId}#sample-questions");
                }
                else
                {
                    TempData["SuccessErrorMessageQuestions"] = "Questions were not saved";
                    return RedirectToAction("DeliveryHeadInstruction", new { irfid = model.IRFId });
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageQuestions"] = "Questions were not saved";
                return RedirectToAction("DeliveryHeadInstruction", new { irfid = model.IRFId });
            }
        }
        [HttpPost]
        public async Task<JsonResult> GetSkillChannelSampleQuestions(int irfid)
        {
            var questions = await _irfService.GetSkillSampleQuestions(irfid);
            if (questions == null)
            {
                questions = new List<SkillComment>();
            }
            return Json(questions);
        }
        [HttpPost]
        public async Task<JsonResult> DeleteSkillChannelSampleQuestions(int questionid)
        {
            var result = await _irfService.DeleteSkillChannelSampleQuestions(questionid);
            return Json(new
            {
                success = result?.Success ?? false,
                message = result?.Message,
                setsDiscarded = result?.SetsDiscarded ?? 0
            });
        }

        [HttpPost]
        public async Task<JsonResult> GetAIQuestionSuggestions(int irfid, int topCount = 10)
        {
            try
            {
                var emailId = GetSessionEmail();
                if (string.IsNullOrEmpty(emailId))
                    return Json(new { success = false, message = "Your session has expired. Please sign in again." });

                if (topCount <= 0)
                    topCount = 10;

                var bank = (await _irfService.GetQuestionBankForJD(irfid))?.ToList() ?? new List<AISuggestedQuestion>();
                if (bank.Count == 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "No questions found in the question database for this channel's rubric skills."
                    });
                }

                var jdContext = await BuildJDContextForAI(irfid);
                var suggestions = await _assistantServiceHelper.GenerateAIQuestionSuggestions(jdContext, bank, topCount);
                if (suggestions == null)
                    return Json(new { success = false, message = "Unable to generate AI suggestions. Please try again." });

                return Json(new { success = true, questions = suggestions });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Unable to generate AI suggestions. Please try again." });
            }
        }

        [HttpPost]
        public async Task<JsonResult> GenerateAIIdealAnswers([FromBody] AIQuestionAnswerRequest request)
        {
            try
            {
                var emailId = GetSessionEmail();
                if (string.IsNullOrEmpty(emailId))
                    return Json(new { success = false, message = "Your session has expired. Please sign in again." });

                if (request == null || request.IrfId <= 0 || request.Questions == null || request.Questions.Count == 0)
                    return Json(new { success = false, message = "No questions were provided." });

                var jdContext = await BuildJDContextForAI(request.IrfId);
                var answers = await _assistantServiceHelper.GenerateAIIdealAnswers(jdContext, request.Questions);
                if (answers == null)
                    return Json(new { success = false, message = "Unable to generate ideal answers. Please try again." });

                return Json(new { success = true, answers });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Unable to generate ideal answers. Please try again." });
            }
        }

        [HttpPost]
        public async Task<JsonResult> SaveAIQuestions([FromBody] AIQuestionAnswerRequest request)
        {
            try
            {
                var emailId = GetSessionEmail();
                if (string.IsNullOrEmpty(emailId))
                    return Json(new { success = false, message = "Your session has expired. Please sign in again." });

                if (request == null || request.IrfId <= 0 || request.Questions == null || request.Questions.Count == 0)
                    return Json(new { success = false, message = "Select at least one question to save." });

                var channelSkills = (await _irfService.GetIRFTechSkills(request.IrfId) ?? Enumerable.Empty<string>())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .ToList();
                foreach (var q in request.Questions.Where(x => x != null && !string.IsNullOrWhiteSpace(x.SkillName)))
                {
                    var match = channelSkills.FirstOrDefault(s =>
                        string.Equals(s, q.SkillName.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                        q.SkillName = match;
                }

                var requested = request.Questions.Count(q => q != null && !string.IsNullOrWhiteSpace(q.Question));
                var inserted = await _irfService.AddAIQuestionsWithAnswers(request.IrfId, request.Questions, emailId);
                if (inserted < 0)
                {
                    // AI columns/proc may not be on this database yet — save through the same
                    // path the HM manual form uses so questions still appear on the HM page.
                    var fallback = new TechSkillComments
                    {
                        IRFId = request.IrfId,
                        CreatedBy = emailId,
                        SkillCollection = request.Questions
                            .Where(q => q != null && !string.IsNullOrWhiteSpace(q.Question))
                            .Select(q => new SkillComment
                            {
                                Comment = q.Question,
                                SkillName = q.SkillName,
                                ExpectedAnswer = q.IdealAnswer,
                                IdealAnswer = q.IdealAnswer,
                                QuestionGist = DmIvQuestionRubricHelper.BuildSpokenQuestionGist(q.Question, q.QuestionGist),
                                CreatedBy = emailId,
                                IsAIGenerated = true
                            })
                            .ToList()
                    };
                    var saved = await _irfService.AddIRFSampleQuestions(fallback);
                    if (!saved)
                        return Json(new { success = false, message = "AI question(s) could not be saved. Confirm the database scripts for IdealAnswer / IsAIGenerated have been run." });
                    inserted = fallback.SkillCollection.Count;
                }

                var skipped = Math.Max(0, requested - inserted);
                string message;
                if (inserted == 0)
                    message = "No new questions were saved. They are already on this skill channel.";
                else if (skipped > 0)
                    message = inserted + " question(s) saved. " + skipped + " already existed on this channel.";
                else
                    message = inserted + " question(s) saved to this skill channel.";

                return Json(new { success = true, inserted, skipped, message });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "AI question(s) could not be saved." });
            }
        }

        private string GetSessionEmail()
        {
            var emailId = HttpContext.Session.GetString("email")?.Trim();
            if (string.IsNullOrEmpty(emailId))
                return null;
            return emailId.Replace("\"", "");
        }

        private async Task<string> BuildJDContextForAI(int irfId)
        {
            var jdTitle = _irfRepository.GetIRFJDTitleBasedOnId(irfId);
            if (string.IsNullOrWhiteSpace(jdTitle))
            {
                var irf = await _irfRepository.GetIRFForCall(irfId);
                jdTitle = !string.IsNullOrWhiteSpace(irf?.JDTiltle)
                    ? irf.JDTiltle
                    : (irf?.IRFId ?? "Skill Channel");
            }

            var sb = new StringBuilder();
            sb.AppendLine("Job Title: " + jdTitle);
            sb.AppendLine("Score Rubric:");

            var skills = await _irfRepository.GetSkillsbyIRFid(irfId);
            if (skills != null)
            {
                foreach (var skill in skills.Where(s => s != null && !string.IsNullOrWhiteSpace(s.SkillName)))
                {
                    sb.AppendLine(
                        "- Skill: " + skill.SkillName.Trim() +
                        " | Concepts: " + (skill.Concepts ?? string.Empty) +
                        " | Nature of questions: " + (skill.Natureofquestion ?? string.Empty));
                }
            }

            return sb.ToString();
        }

        [HttpPost]
        public async Task<JsonResult> GetDmInterviewerEtiquetteJson(int irfId, string Type = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null)
                    return Json(new { success = false });
                if (!await AuthorizeDhiMutatingAccessAsync(irfId, Type))
                    return Json(new { success = false });
                var etiquette = await _irfService.GetDmInterviewerEtiquette(irfId);
                return Json(new { success = etiquette != null, etiquette });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false });
            }
        }

        [HttpPost]
        public async Task<JsonResult> UpdateDmInterviewerEtiquette(int irfId, string dressCode, bool cameraRequired, bool noEatingRule, string Type = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null)
                    return Json(new { success = false });
                if (!await AuthorizeDhiMutatingAccessAsync(irfId, Type))
                    return Json(new { success = false });
                var result = await _irfService.UpdateDmInterviewerEtiquette(irfId, dressCode, cameraRequired, noEatingRule);
                return Json(new { success = result });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false });
            }
        }

        [HttpPost]
        public async Task<JsonResult> GetDeliveryHeadSampleQuestionsJson(int irfId, string Type = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null)
                {
                    return Json(new { success = false, questions = new List<SkillComment>() });
                }
                if (!await AuthorizeDhiMutatingAccessAsync(irfId, Type))
                {
                    return Json(new { success = false, questions = new List<SkillComment>() });
                }
                // DHI sample questions are a shared IRF pool for internal DM and client share (ublc).
                // Do not filter by CreatedBy — otherwise AI/manual questions from one side are hidden on the other.
                var questions = await _clonableSkillChannelTemplateService.GetDeliveryHeadSampleQuestions(irfId, emailId: null);
                return Json(new { success = true, questions = questions ?? new List<SkillComment>() });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, questions = new List<SkillComment>() });
            }
        }

        [HttpPost]
        public async Task<JsonResult> GenerateQuestionsFromRubric(int irfId, bool useVoiceTranscript = false, string Type = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null)
                    return Json(new { success = false, message = "Unauthorized" });
                if (!await AuthorizeDhiMutatingAccessAsync(irfId, Type))
                    return Json(new { success = false, message = "Forbidden" });

                var emailid = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");

                var irfVm = await _irfService.GetEditIRF(irfId, 0, false);
                if (irfVm == null || irfVm.SkillCollection == null || !irfVm.SkillCollection.Any())
                    return Json(new { success = false, message = "No rubric skills found for this channel." });

                var rubricPrompt = DmIvQuestionRubricHelper.BuildRubricPromptSection(irfVm.SkillCollection);
                var rubricInput = $"Role: {irfVm.IRFId}\nInterview Type: Technical\n\n{rubricPrompt}\n\n{DmIvQuestionRubricHelper.QuestionGistInstruction}";

                if (useVoiceTranscript)
                {
                    var voiceBrief = _irfService.FetchVoiceInstructionBrief(irfId);
                    if (!string.IsNullOrWhiteSpace(voiceBrief?.Transcript))
                    {
                        rubricInput = $"Hiring manager voice brief (canonical):\n{voiceBrief.Transcript.Trim()}\n\n{rubricInput}";
                    }
                }

                var validSkillNames = DmIvQuestionRubricHelper.BuildValidSkillNameSet(irfVm.SkillCollection);

                var assistantId = _configuration["OpenAI:RubricQuestionAssistantId"];
                if (string.IsNullOrEmpty(assistantId))
                    return Json(new { success = false, message = "Rubric question assistant not configured." });

                var apiKey = _configuration["OpenAI:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                    return Json(new { success = false, message = "OpenAI API key not configured." });

                var generationResult = await RunRubricQuestionGenerationAsync(assistantId, apiKey, rubricInput, irfId, emailid, validSkillNames);
                if (generationResult.RetryAttempted)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Info,
                        new Exception($"GenerateQuestionsFromRubric: retried after >50% skill mismatches for IRF {irfId}."));
                }
                return Json(generationResult.Response);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }


        #region Code project questions

        /// <summary>Active stack images for the author's picker. Only these codes enter the generator's allowlist.</summary>
        [HttpPost]
        public async Task<JsonResult> GetCodeProjectStackImages(int irfId, string Type = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null)
                    return Json(new { success = false, message = "Unauthorized" });
                if (!await AuthorizeDhiMutatingAccessAsync(irfId, Type))
                    return Json(new { success = false, message = "Forbidden" });

                var images = await _codeProjectQuestionService.GetActiveStackImages();
                return Json(new
                {
                    success = true,
                    images = images.Select(i => new { code = i.Code, displayName = i.DisplayName, toolchain = i.Toolchain }),
                    timeLimitMinutes = CodeProjectQuestionHelper.DefaultTimeLimitMinutes
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Could not load stack images." });
            }
        }

        /// <summary>
        /// Suggests the author brief from the chosen stack, rubric skill and difficulty, so the author can
        /// start from a feasible idea instead of a blank box. Inline instructions on the Responses API
        /// (CodeProjectQuestionHelper.BriefSuggestionInstructions): no stored OpenAI prompt is needed.
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> SuggestCodeProjectBrief(int irfId, string stackImageCode, string skillName = null,
            string difficulty = null, string previousBrief = null, string Type = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null)
                    return Json(new { success = false, message = "Unauthorized" });
                if (!await AuthorizeDhiMutatingAccessAsync(irfId, Type))
                    return Json(new { success = false, message = "Forbidden" });
                if (string.IsNullOrEmpty(_configuration["OpenAI:ApiKey"]) || string.IsNullOrEmpty(_configuration["OpenAI:ResponsesModel"]))
                    return Json(new { success = false, message = "OpenAI:ApiKey and OpenAI:ResponsesModel must be configured." });

                var irfVm = await _irfService.GetEditIRF(irfId, 0, false);
                if (irfVm == null || irfVm.SkillCollection == null || !irfVm.SkillCollection.Any())
                    return Json(new { success = false, message = "No rubric skills found for this channel." });

                var images = await _codeProjectQuestionService.GetActiveStackImages();
                var validStackCodes = DmIvQuestionRubricHelper.BuildValidStackImageSet(images);
                if (!DmIvQuestionRubricHelper.TryMatchStackImage(stackImageCode, validStackCodes, out var stack))
                    return Json(new { success = false, message = "Choose one of the listed stack images." });
                var image = images.First(i => string.Equals(i.Code, stack, StringComparison.OrdinalIgnoreCase));

                var validSkillNames = DmIvQuestionRubricHelper.BuildValidSkillNameSet(irfVm.SkillCollection);
                string preferredSkill = null;
                if (!string.IsNullOrWhiteSpace(skillName) && !DmIvQuestionRubricHelper.TryMatchSkill(skillName, validSkillNames, out preferredSkill))
                    return Json(new { success = false, message = "The skill must be one of this channel's rubric skills." });

                var input = new StringBuilder();
                input.AppendLine("Interview: technical, code project question (the candidate works on a real machine with an IDE).");
                input.AppendLine($"Chosen stack: {image.Code} ({image.DisplayName}). Toolchain and the ONLY available dependencies: {image.Toolchain}");
                input.AppendLine($"Difficulty: {(string.IsNullOrWhiteSpace(difficulty) ? "Medium" : difficulty.Trim())}");
                input.AppendLine(preferredSkill != null ? $"Rubric skill to exercise: {preferredSkill}" : "Rubric skill: choose the one that best fits the chosen stack.");
                if (!string.IsNullOrWhiteSpace(previousBrief))
                    input.AppendLine("Previous brief (suggest something different): " + (previousBrief.Length > 1000 ? previousBrief.Substring(0, 1000) : previousBrief.Trim()));
                input.AppendLine();
                input.AppendLine(DmIvQuestionRubricHelper.BuildRubricPromptSection(irfVm.SkillCollection));
                input.AppendLine("Answer in JSON.");

                var raw = await _openAIResponsesHelper.CreateJsonWithInstructionsAsync(
                    CodeProjectQuestionHelper.BriefSuggestionInstructions, input.ToString(), "SuggestCodeProjectBrief", maxOutputTokens: 600);
                if (string.IsNullOrWhiteSpace(raw))
                    return Json(new { success = false, message = "Could not suggest a brief right now. Write one, or try again." });

                string brief = null, suggestedSkill = null;
                try
                {
                    var json = Newtonsoft.Json.Linq.JObject.Parse(raw.Substring(Math.Max(0, raw.IndexOf('{'))));
                    brief = json["brief"]?.ToString()?.Trim();
                    suggestedSkill = json["skill"]?.ToString();
                }
                catch (Newtonsoft.Json.JsonException) { }
                if (string.IsNullOrWhiteSpace(brief))
                    return Json(new { success = false, message = "Could not suggest a brief right now. Write one, or try again." });
                if (brief.Length > CodeProjectQuestionHelper.MaxSuggestedBriefLength)
                    brief = brief.Substring(0, CodeProjectQuestionHelper.MaxSuggestedBriefLength).TrimEnd() + "…";

                // The skill is shown to the author and sent back with Generate, so only an exact rubric skill is returned.
                var skill = preferredSkill;
                if (skill == null && DmIvQuestionRubricHelper.TryMatchSkill(suggestedSkill, validSkillNames, out var matched)) skill = matched;

                return Json(new { success = true, brief, skill, stack = image.Code });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Could not suggest a brief right now. Write one, or try again." });
            }
        }

        /// <summary>
        /// Generates a code project question from the rubric. Mirrors GenerateQuestionsFromRubric with
        /// one structural difference: building and test-running the project takes minutes, so this
        /// persists the question as 'validating', enqueues the validation job and returns at once.
        /// The panel polls CodeProjectValidationStatus.
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> GenerateCodeProjectQuestion(int irfId, string stackImageCode, string brief,
            string difficulty = null, string skillName = null, string language = null, string framework = null,
            int timeLimitMinutes = CodeProjectQuestionHelper.DefaultTimeLimitMinutes, string Type = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null)
                    return Json(new { success = false, message = "Unauthorized" });
                if (!await AuthorizeDhiMutatingAccessAsync(irfId, Type))
                    return Json(new { success = false, message = "Forbidden" });
                if (string.IsNullOrWhiteSpace(brief))
                    return Json(new { success = false, message = "Describe what the candidate should build or fix." });
                if (brief.Length > MaxSampleQuestionLength)
                    return Json(new { success = false, message = $"The brief must not exceed {MaxSampleQuestionLength} characters." });
                if (timeLimitMinutes < 10 || timeLimitMinutes > 60)
                    return Json(new { success = false, message = "Time limit must be between 10 and 60 minutes." });

                var emailid = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");

                var irfVm = await _irfService.GetEditIRF(irfId, 0, false);
                if (irfVm == null || irfVm.SkillCollection == null || !irfVm.SkillCollection.Any())
                    return Json(new { success = false, message = "No rubric skills found for this channel." });

                var images = await _codeProjectQuestionService.GetActiveStackImages();
                var validStackCodes = DmIvQuestionRubricHelper.BuildValidStackImageSet(images);
                if (validStackCodes.Count == 0)
                    return Json(new { success = false, message = "No stack images are available yet. A VM image has to be baked and registered before code project questions can be generated." });
                if (!DmIvQuestionRubricHelper.TryMatchStackImage(stackImageCode, validStackCodes, out var requestedStack))
                    return Json(new { success = false, message = "Choose one of the listed stack images." });

                var validSkillNames = DmIvQuestionRubricHelper.BuildValidSkillNameSet(irfVm.SkillCollection);
                string preferredSkill = null;
                if (!string.IsNullOrWhiteSpace(skillName) && !DmIvQuestionRubricHelper.TryMatchSkill(skillName, validSkillNames, out preferredSkill))
                    return Json(new { success = false, message = "The skill must be one of this channel's rubric skills." });

                // Responses API with the instructions inline: OpenAI retired the Assistants API on
                // 26 Aug 2026, and inline instructions need no stored prompt or assistant to be set up.
                if (string.IsNullOrEmpty(_configuration["OpenAI:ApiKey"]) || string.IsNullOrEmpty(_configuration["OpenAI:ResponsesModel"]))
                    return Json(new { success = false, message = "OpenAI:ApiKey and OpenAI:ResponsesModel must be configured." });

                var input = BuildCodeProjectGenerationInput(irfVm, images, requestedStack, brief.Trim(), difficulty, preferredSkill,
                    language, framework, timeLimitMinutes);

                // One retry on any mismatch, same policy as the rubric generator.
                CodeProjectGeneration generation = null;
                string matchedSkill = null;
                List<string> errors = null;
                for (var attempt = 0; attempt < 3; attempt++)
                {
                    var raw = await _openAIResponsesHelper.CreateJsonWithInstructionsAsync(
                        CodeProjectQuestionHelper.GeneratorInstructions, input, "GenerateCodeProjectQuestion");
                    generation = CodeProjectQuestionHelper.Parse(raw, out var parseError);
                    // A hidden test also listed as a project file would leak the answer key; drop the visible copy.
                    CodeProjectQuestionHelper.StripHiddenTestsFromProject(generation);
                    errors = generation == null
                        ? new List<string> { parseError }
                        : CheckCodeProjectGeneration(generation, validSkillNames, validStackCodes, requestedStack, preferredSkill, timeLimitMinutes, out matchedSkill);
                    if (errors.Count == 0) break;

                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Info,
                        new Exception($"GenerateCodeProjectQuestion IRF {irfId} attempt {attempt + 1} rejected: {string.Join(" | ", errors)}"));
                    input += "\n\nREMINDER — your previous answer was rejected for these reasons. Fix every one:\n- " + string.Join("\n- ", errors);
                }
                if (errors.Count > 0)
                    return Json(new { success = false, message = "The generated project did not meet the rules.", errors });

                // dotnet build/test in the workspace root need a solution there; add one when the projects sit in sub-folders.
                CodeProjectQuestionHelper.EnsureDotnetSolution(generation);
                // No internet in the runner or on the candidate's machine: restore must use only pre-loaded packages.
                CodeProjectQuestionHelper.EnsureDotnetOffline(generation);
                CodeProjectQuestionHelper.EnsureProgramVisibleToTests(generation);
                generation.TimeLimitMinutes = timeLimitMinutes;
                generation.Language = string.IsNullOrWhiteSpace(generation.Language) ? (language ?? requestedStack) : generation.Language;
                generation.QuestionGist = DmIvQuestionRubricHelper.BuildSpokenQuestionGist(generation.QuestionText, generation.QuestionGist);

                var (questionId, storeError) = await _codeProjectQuestionService.CreateQuestionAsync(irfId, matchedSkill, generation, timeLimitMinutes, emailid);
                if (questionId <= 0)
                    return Json(new { success = false, message = storeError ?? "Could not save the generated question." });

                if (storeError == null)
                    _codeProjectQueue.Enqueue(new Risebird.Web.Services.CodeProjectBackgroundQueue.Validate(questionId));

                return Json(new
                {
                    success = storeError == null,
                    questionId,
                    status = storeError == null ? Risebird.Entities.ViewModel.CodeProjectValidationStatus.Validating : Risebird.Entities.ViewModel.CodeProjectValidationStatus.Failed,
                    message = storeError
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        /// <summary>Polled by the panel while the validation job runs.</summary>
        [HttpGet]
        public async Task<JsonResult> CodeProjectValidationStatus(int irfId, int questionId, string Type = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null)
                    return Json(new { success = false, message = "Unauthorized" });
                if (!await AuthorizeDhiMutatingAccessAsync(irfId, Type))
                    return Json(new { success = false, message = "Forbidden" });

                var details = await _codeProjectQuestionService.GetDetails(questionId);
                if (details == null || details.IRFId != irfId)
                    return Json(new { success = false, message = "Question not found." });

                CodeProjectValidationReport report = null;
                if (!string.IsNullOrWhiteSpace(details.ValidationJson))
                {
                    try { report = System.Text.Json.JsonSerializer.Deserialize<CodeProjectValidationReport>(details.ValidationJson, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)); }
                    catch (System.Text.Json.JsonException) { }
                }

                return Json(new
                {
                    success = true,
                    status = details.ValidationStatus,
                    detail = report,
                    question = BuildCodeProjectPreview(details)
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Could not read the validation status." });
            }
        }

        /// <summary>Validation status for every code project question on the channel, for the list badges.</summary>
        [HttpPost]
        public async Task<JsonResult> GetCodeProjectStatuses(int irfId, string Type = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null)
                    return Json(new { success = false });
                if (!await AuthorizeDhiMutatingAccessAsync(irfId, Type))
                    return Json(new { success = false });
                var rows = await _codeProjectQuestionService.GetStatusesByIRF(irfId);
                return Json(new { success = true, statuses = rows.Select(r => new { questionId = r.QuestionId, status = r.ValidationStatus, stackImageCode = r.StackImageCode }) });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false });
            }
        }

        private const string CodeProjectNotEditableMessage =
            "This is a code project question. Its text, gist and time limit were validated together with the project, so edit the brief and regenerate instead.";

        /// <summary>
        /// Code project questions share the coding columns (CodingTimeLimitInMinutes is their slot), so the
        /// coding/listening toggles and in-place edits would silently clear them. They are refused instead.
        /// </summary>
        private async Task<bool> IsCodeProjectQuestionAsync(int questionId) =>
            questionId > 0 && (await _irfService.GetDeliveryHeadSampleQuestionById(questionId))?.IsCodeProjectQuestion == true;

        private static string BuildCodeProjectGenerationInput(IRFVm irfVm, List<StackImage> images, string stackImageCode, string brief,
            string difficulty, string preferredSkill, string language, string framework, int timeLimitMinutes)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Role: {irfVm.IRFId}");
            sb.AppendLine("Interview Type: Technical — code project question (the candidate works on a real machine)");
            sb.AppendLine();
            sb.AppendLine("Author brief:");
            sb.AppendLine(brief);
            sb.AppendLine();
            if (!string.IsNullOrWhiteSpace(difficulty)) sb.AppendLine($"Difficulty: {difficulty.Trim()}");
            if (!string.IsNullOrWhiteSpace(preferredSkill)) sb.AppendLine($"Primary skill (skill_or_competency must be exactly this): {preferredSkill}");
            if (!string.IsNullOrWhiteSpace(language)) sb.AppendLine($"Language: {language.Trim()}");
            if (!string.IsNullOrWhiteSpace(framework)) sb.AppendLine($"Framework: {framework.Trim()}");
            sb.AppendLine($"stack_image_code must be: {stackImageCode}");
            sb.AppendLine($"time_limit_minutes must be: {timeLimitMinutes}");
            sb.AppendLine();
            sb.AppendLine(DmIvQuestionRubricHelper.BuildRubricPromptSection(irfVm.SkillCollection));
            sb.AppendLine("secondary_skills is optional; every entry must also be copied exactly from the Score Rubric list.");
            sb.AppendLine();
            // Rules and the response contract travel as the call's instructions (CodeProjectQuestionHelper.GeneratorInstructions).
            sb.AppendLine(DmIvQuestionRubricHelper.BuildStackImagePromptSection(images));
            return sb.ToString();
        }

        /// <summary>Rubric match, stack allowlist match, then the static scope rules.</summary>
        private static List<string> CheckCodeProjectGeneration(CodeProjectGeneration g, HashSet<string> validSkillNames,
            HashSet<string> validStackCodes, string requestedStack, string preferredSkill, int timeLimitMinutes, out string matchedSkill)
        {
            var errors = new List<string>();
            if (!DmIvQuestionRubricHelper.TryMatchSkill(g.SkillOrCompetency, validSkillNames, out matchedSkill))
                errors.Add($"skill_or_competency '{g.SkillOrCompetency}' is not one of the rubric skills: {string.Join(", ", validSkillNames)}.");
            else if (preferredSkill != null && !string.Equals(matchedSkill, preferredSkill, StringComparison.OrdinalIgnoreCase))
                errors.Add($"skill_or_competency must be '{preferredSkill}'.");

            var secondaries = new List<string>();
            foreach (var s in g.SecondarySkills)
            {
                if (DmIvQuestionRubricHelper.TryMatchSkill(s, validSkillNames, out var m)) secondaries.Add(m);
                else errors.Add($"secondary skill '{s}' is not one of the rubric skills.");
            }
            g.SecondarySkills = secondaries;

            if (!DmIvQuestionRubricHelper.TryMatchStackImage(g.StackImageCode, validStackCodes, out var matchedStack))
                errors.Add($"stack_image_code '{g.StackImageCode}' is not in the allowlist.");
            else if (!string.Equals(matchedStack, requestedStack, StringComparison.OrdinalIgnoreCase))
                errors.Add($"stack_image_code must be '{requestedStack}'.");
            else
                g.StackImageCode = matchedStack;

            errors.AddRange(CodeProjectQuestionHelper.ValidateScope(g, timeLimitMinutes));
            return errors;
        }

        /// <summary>What the author sees: statement, gist, requirements and the file tree. Never tier 3.</summary>
        private static object BuildCodeProjectPreview(CodeProjectDetails details)
        {
            List<string> files = new List<string>(), editable = new List<string>(), readOnly = new List<string>();
            List<object> requirements = new List<object>();
            if (!string.IsNullOrWhiteSpace(details.GeneratedPayload))
            {
                try
                {
                    var p = Newtonsoft.Json.Linq.JObject.Parse(details.GeneratedPayload);
                    files = p["project_files"]?.ToObject<List<string>>() ?? files;
                    editable = p["editable_files"]?.ToObject<List<string>>() ?? editable;
                    readOnly = p["read_only_paths"]?.ToObject<List<string>>() ?? readOnly;
                    requirements = (p["requirements"] as Newtonsoft.Json.Linq.JArray)?
                        .Select(r => (object)new { id = r["id"]?.ToString(), text = r["text"]?.ToString() }).ToList() ?? requirements;
                }
                catch (Newtonsoft.Json.JsonException) { }
            }
            return new
            {
                id = details.QuestionId,
                skillName = details.SkillName,
                questionText = details.Comment,
                questionGist = details.QuestionGist,
                stackImageCode = details.StackImageCode,
                framework = details.Framework,
                language = details.Language,
                timeLimitMinutes = details.CodingTimeLimitInMinutes,
                estimatedMinutes = details.EstimatedMinutes,
                requirements,
                files = files.Select(f => new
                {
                    path = f,
                    editable = editable.Contains(f, StringComparer.OrdinalIgnoreCase),
                    readOnly = CodeProjectQuestionHelper.IsReadOnly(f, readOnly)
                })
            };
        }

        #endregion

        /// <summary>
        /// Turns coding mode on or off for a question that already exists, so questions added before
        /// the feature shipped (or generated from the rubric) can be converted without retyping them.
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> SetQuestionCodingMode(int irfId, int id, bool isCodingQuestion, int? codingTimeLimitInMinutes = null, string questionGist = null, string Type = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null)
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }
                if (!await AuthorizeDhiMutatingAccessAsync(irfId, Type))
                {
                    return Json(new { success = false, message = "Forbidden" });
                }
                if (id <= 0)
                {
                    return Json(new { success = false, message = "Question not found." });
                }
                if (await IsCodeProjectQuestionAsync(id))
                {
                    return Json(new { success = false, message = CodeProjectNotEditableMessage });
                }

                if (!isCodingQuestion)
                {
                    codingTimeLimitInMinutes = null;
                }
                else
                {
                    if (!codingTimeLimitInMinutes.HasValue)
                    {
                        codingTimeLimitInMinutes = DefaultCodingTimeLimitInMinutes;
                    }
                    if (codingTimeLimitInMinutes.Value < 1 || codingTimeLimitInMinutes.Value > 180)
                    {
                        return Json(new { success = false, message = "Time limit must be between 1 and 180 minutes." });
                    }

                    // The avatar narrates the gist for coding questions, so one has to exist before
                    // the question can be marked as coding. An existing gist satisfies this.
                    if (string.IsNullOrWhiteSpace(questionGist))
                    {
                        var existing = await _irfService.GetDeliveryHeadSampleQuestionById(id);
                        if (existing == null || string.IsNullOrWhiteSpace(existing.QuestionGist))
                        {
                            return Json(new { success = false, message = "Write a question gist in plain language before marking this as a coding question. It is what the AI avatar reads out." });
                        }
                    }
                    else if (questionGist.Length > MaxSampleQuestionLength)
                    {
                        return Json(new { success = false, message = $"The question gist must not exceed {MaxSampleQuestionLength} characters." });
                    }
                }

                var saved = await _irfService.SetSampleQuestionCodingMode(id, isCodingQuestion, codingTimeLimitInMinutes, questionGist?.Trim());
                return saved
                    ? Json(new { success = true, isCodingQuestion, codingTimeLimitInMinutes })
                    : Json(new { success = false, message = "Could not update the question." });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Could not update the question." });
            }
        }

        /// <summary>
        /// Turns listening comprehension mode on or off for an existing question. The candidate hears
        /// the uploaded clip (at most twice) and types a summary within three minutes.
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> SetQuestionListeningMode(int irfId, int id, bool isListeningComprehension, string listeningAudioTranscription = null, string questionGist = null, string Type = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null)
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }
                if (!await AuthorizeDhiMutatingAccessAsync(irfId))
                {
                    return Json(new { success = false, message = "Forbidden" });
                }
                if (id <= 0)
                {
                    return Json(new { success = false, message = "Question not found." });
                }
                if (await IsCodeProjectQuestionAsync(id))
                {
                    return Json(new { success = false, message = CodeProjectNotEditableMessage });
                }

                if (isListeningComprehension)
                {
                    var existing = await _irfService.GetDeliveryHeadSampleQuestionById(id);

                    // The candidate cannot do the exercise without the clip, so require it up front
                    // rather than letting a half-configured question reach an interview.
                    if (existing == null || string.IsNullOrWhiteSpace(existing.ListeningAudioUrl))
                    {
                        return Json(new { success = false, message = "Upload the listening audio clip before marking this as a listening comprehension question." });
                    }

                    // The avatar introduces the exercise from the gist. It must not read the question
                    // text, which describes what the candidate is supposed to listen for.
                    if (string.IsNullOrWhiteSpace(questionGist) && string.IsNullOrWhiteSpace(existing.QuestionGist))
                    {
                        return Json(new { success = false, message = "Write a question gist in plain language before marking this as a listening comprehension question. It is what the AI avatar reads out to introduce the exercise." });
                    }

                    if (!string.IsNullOrEmpty(questionGist) && questionGist.Length > MaxSampleQuestionLength)
                    {
                        return Json(new { success = false, message = $"The question gist must not exceed {MaxSampleQuestionLength} characters." });
                    }

                    // The transcript is the scoring baseline, so it has to be there too.
                    if (string.IsNullOrWhiteSpace(listeningAudioTranscription)
                        && string.IsNullOrWhiteSpace(existing.ListeningAudioTranscription))
                    {
                        return Json(new { success = false, message = "Add the audio transcript before marking this as a listening comprehension question. It is what the candidate's summary is scored against." });
                    }
                }

                var saved = await _irfService.SetSampleQuestionListeningMode(
                    id,
                    isListeningComprehension,
                    string.IsNullOrWhiteSpace(listeningAudioTranscription) ? null : listeningAudioTranscription.Trim(),
                    string.IsNullOrWhiteSpace(questionGist) ? null : questionGist.Trim());

                return saved
                    ? Json(new { success = true, isListeningComprehension })
                    : Json(new { success = false, message = "Could not update the question." });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Could not update the question." });
            }
        }

        /// <summary>
        /// Uploads the audio clip a listening comprehension question plays to the candidate and
        /// stores its url against the question.
        /// </summary>
        [HttpPost]
        [RequestSizeLimit(26214400)] // 25 MB - clips are short by design
        public async Task<JsonResult> UploadListeningAudio(int irfId, int id, IFormFile audioFile)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null)
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }
                if (!await AuthorizeDhiMutatingAccessAsync(irfId))
                {
                    return Json(new { success = false, message = "Forbidden" });
                }
                if (id <= 0)
                {
                    return Json(new { success = false, message = "Save the question before uploading its audio." });
                }
                if (audioFile == null || audioFile.Length == 0)
                {
                    return Json(new { success = false, message = "Choose an audio file to upload." });
                }
                if (audioFile.Length > 25 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "The audio file must be 25 MB or smaller." });
                }

                var extension = Path.GetExtension(audioFile.FileName)?.ToLowerInvariant();
                var allowedExtensions = new[] { ".mp3", ".wav", ".m4a", ".ogg", ".aac" };
                if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
                {
                    return Json(new { success = false, message = "Upload an mp3, wav, m4a, ogg or aac file." });
                }

                var contentType = extension switch
                {
                    ".mp3" => "audio/mpeg",
                    ".wav" => "audio/wav",
                    ".m4a" => "audio/mp4",
                    ".ogg" => "audio/ogg",
                    ".aac" => "audio/aac",
                    _ => "application/octet-stream"
                };

                // Question id plus a timestamp: re-uploading a clip writes a new blob rather than
                // overwriting the old one, so an interview already in flight keeps working.
                var filename = $"listening_{id}_{DateTime.Now:yyyyMMddHHmmssffff}{extension}";

                string uploadedUrl;
                using (var stream = new MemoryStream())
                {
                    await audioFile.CopyToAsync(stream);
                    stream.Position = 0;
                    uploadedUrl = await _azureStorageHelper.InsertBlobAIInterviewerAudio(
                        filename, "botintervieweraudio", stream, contentType: contentType);
                }

                if (string.IsNullOrWhiteSpace(uploadedUrl))
                {
                    return Json(new { success = false, message = "The audio file could not be stored. Try again." });
                }

                // The container is not public, so the candidate's browser needs the SAS token.
                var sas = _sasUrlHelper.CreateSasUrl("botintervieweraudio");
                var audioUrl = string.IsNullOrEmpty(sas) ? uploadedUrl : $"{uploadedUrl}?{sas}";

                var saved = await _irfService.UpdateSampleQuestionListeningAudio(id, audioUrl);
                return saved
                    ? Json(new { success = true, audioUrl, message = "Audio uploaded." })
                    : Json(new { success = false, message = "Uploaded the file but could not save it against the question." });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Could not upload the audio file." });
            }
        }

        [HttpPost]
        public async Task<JsonResult> SaveDeliveryHeadSampleQuestion(int irfId, string skillName, string comment, string expectedAnswer, int? id = null, bool isCodingQuestion = false, int? codingTimeLimitInMinutes = null, string questionGist = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null)
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }
                if (!await AuthorizeDhiMutatingAccessAsync(irfId))
                    return Json(new { success = false, message = "Forbidden" });
                if (string.IsNullOrWhiteSpace(skillName) || string.IsNullOrWhiteSpace(comment))
                {
                    return Json(new { success = false, message = "Skill and question are required." });
                }
                if (comment.Length > MaxSampleQuestionLength)
                {
                    return Json(new { success = false, message = $"Question must not exceed {MaxSampleQuestionLength} characters." });
                }
                if (!string.IsNullOrEmpty(expectedAnswer) && expectedAnswer.Length > MaxSampleQuestionLength)
                {
                    return Json(new { success = false, message = $"Expected answer must not exceed {MaxSampleQuestionLength} characters." });
                }
                if (isCodingQuestion
                    && (!codingTimeLimitInMinutes.HasValue || codingTimeLimitInMinutes.Value < 1 || codingTimeLimitInMinutes.Value > 180))
                {
                    return Json(new { success = false, message = "A coding question needs a time limit between 1 and 180 minutes." });
                }
                if (isCodingQuestion && string.IsNullOrWhiteSpace(questionGist))
                {
                    return Json(new { success = false, message = "A coding question needs a gist. The avatar narrates the gist because code cannot be read aloud." });
                }
                if (!string.IsNullOrEmpty(questionGist) && questionGist.Length > MaxSampleQuestionLength)
                {
                    return Json(new { success = false, message = $"The question gist must not exceed {MaxSampleQuestionLength} characters." });
                }
                if (!isCodingQuestion)
                {
                    codingTimeLimitInMinutes = null;
                }
                var emailid = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
                if (id.HasValue && id.Value > 0)
                {
                    if (await IsCodeProjectQuestionAsync(id.Value))
                    {
                        return Json(new { success = false, message = CodeProjectNotEditableMessage });
                    }
                    var ok = await _irfService.UpdateSkillChannelSampleQuestion(id.Value, comment.Trim(), expectedAnswer?.Trim(), isCodingQuestion, codingTimeLimitInMinutes, questionGist?.Trim());
                    return Json(new { success = ok, id = id.Value });
                }
                var model = new TechSkillComments
                {
                    IRFId = irfId,
                    CreatedBy = emailid,
                    SkillCollection = new List<SkillComment>
                    {
                        new SkillComment
                        {
                            SkillName = skillName.Trim(),
                            Comment = comment.Trim(),
                            ExpectedAnswer = expectedAnswer?.Trim(),
                            CreatedBy = emailid,
                            IsCodingQuestion = isCodingQuestion,
                            CodingTimeLimitInMinutes = codingTimeLimitInMinutes,
                            QuestionGist = questionGist?.Trim()
                        }
                    }
                };
                var added = await _irfService.AddIRFSampleQuestions(model);
                if (!added)
                {
                    return Json(new { success = false, message = "Failed to save question." });
                }
                var questions = await _clonableSkillChannelTemplateService.GetDeliveryHeadSampleQuestions(irfId, emailid);
                var created = questions?.OrderByDescending(q => q.id)
                    .FirstOrDefault(q => string.Equals(q.SkillName, skillName, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(q.Comment, comment.Trim(), StringComparison.Ordinal));
                return Json(new { success = true, id = created?.id ?? 0 });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Save failed." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeliveryHeadSkillDetails(int id)
        {
            var transTechSkills = await _irfRepository.GetSkillsbyIRFid(id);
            //var level = await _irfRepository.GetLevelTech();
            //var level = from TechSkillLevels s in Enum.GetValues(typeof(TechSkillLevels))
            //            select new { ID = (int)s, Name = s.GetDescription() };
            //var Skillset = await _irfRepository.GetSkills();
            //if (Skillset != null)
            //{
            //    Skillset = Skillset.OrderBy(x => !string.IsNullOrEmpty(x.Name) ? x.Name.Trim() : "").ToList();
            //}
            foreach (var item in transTechSkills)
            {
                item.LevelName = ((LevelTechEnum)(item.LevelId)).GetDescription();
                //item.SkillName = Skillset.Where(x => x.id == item.SkillId).Select(x => x.Name).FirstOrDefault();
            }
            var colectionone = transTechSkills.Where(x => x.SkillSet != 2).ToList();
            var colectiontwo = transTechSkills.Where(x => x.SkillSet == 2).ToList(); //Other Skill
            //return Json(new { colectionone = colectionone, colectiontwo = colectiontwo });
            var model = new TechSkillsCollectionsWrapper();
            model.colectionone = colectionone;
            model.colectiontwo = colectiontwo;
            if (transTechSkills != null)
            {
                ViewBag.TechRubricType = transTechSkills.FirstOrDefault().TechRubricType;
            }
            else
            {
                ViewBag.TechRubricType = TechRubricTypes.Basic;
            }
            return PartialView("DeliveryHeadSkillDetailsPartial", model);
        }
        [HttpPost]
        public async Task<JsonResult> ApproveDM(int id)
        {
            if (HttpContext.Session.GetString("email") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var result = await _irfService.ApproveDM(id, emailid);
                if (result)
                {
                    return Json("Approved Successfully");
                }
                else
                {
                    return Json("Fail to approve, Please try after some time.");
                }
            }
            else
            {
                return Json("Fail to approve, Please try after some time.");
            }
        }
        #endregion

        #region Assessment Group Campaigns

        [Authorize]
        public async Task<IActionResult> CreateCampaign(int assessmentGroupId, string assessmentGroupName, bool isAlreadyMasterAG, bool isAlreadySecondary)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");

                    if (profileName == "Admin" || profileName == "HR" || profileName == "InterviewerManagement" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "SeniorInterviewerManagement")
                    {
                        //get all assessment groups related to this assessment group and pass it to the form.
                        var model = new AGCampaignModel();
                        model.AssessmentGroupId = assessmentGroupId;
                        model.AssessmentGroupName = assessmentGroupName;
                        model.CurrentForm = "form_1";
                        model.IsAlreadyMasterAG = isAlreadyMasterAG;
                        //model.IsAlreadySecondary = isAlreadySecondary;
                        //var searchValue = assessmentGroupName.Trim()?.Split(" ")[0];
                        var allAGs = (await _irfService.GetSimilarAssessmentGroups(assessmentGroupName.Trim(), isAlreadyMasterAG));//call db and search for assessmentGroups that carry a similar name.         /// the functionality of this function was later changed to get both similar and dissimilar assessment groups, differentiated from each other by a flag

                        model.AssessmentGroups = (IList<AssignmentVM>)(allAGs.Where(x => x.isDirectlyTargeted).ToList());
                        var asg = model.AssessmentGroups.Where(x => x.AssessmentGroup == assessmentGroupName)?.FirstOrDefault();
                        if (asg != null)
                        {
                            model.AssessmentGroups.Where(x => x == asg).FirstOrDefault().IsSelectedForCampaign = true;

                        }
                        model.RemainingAssessmentGroups = allAGs.Where(x => !x.isDirectlyTargeted);

                        model.SerializedRemainingAGList = JsonConvert.SerializeObject(model.RemainingAssessmentGroups);

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

        [HttpPost]
        [Authorize]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> CreateCampaignS1(AGCampaignModel model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "InterviewerManagement" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "SeniorInterviewerManagement")
                    {
                        //create a campaign and save all the selected assessment groups
                        if (!model.CreateMAGOnly)
                        {
                            model = _irfService.CreateCampaign(model, emailid);
                        }
                        else
                        {
                            // create mag only
                            await _irfService.CreateMAG(model, emailid);
                            //add something in tempData here
                            TempData["SuccessErrorMessageInterviewer"] = $"{model.AssessmentGroupName} successfully flagged as Master Assessment Group";
                            return RedirectToAction("InterviewerEntitle");
                        }

                        if (model.id > 0)
                        {
                            model.CurrentForm = "form_2";
                            var listAssessmentGroupsClubbed = new List<int>();
                            if (model.AssessmentGroups != null)
                            {
                                listAssessmentGroupsClubbed.AddRange(model.AssessmentGroups.Where(x => x.IsSelectedForCampaign).Select(x => x.id));

                            }
                            if (model.SelectedRemainingAssessmentGroups != null)
                            {
                                listAssessmentGroupsClubbed.AddRange(model.SelectedRemainingAssessmentGroups);

                            }
                            model.SkillChannels = (IList<IRFVm>)(await _irfService.GetSkillChannelsForAssessmentGroups(listAssessmentGroupsClubbed));//call db and search for assessmentGroups that carry a similar name

                        }
                        else
                        {
                            model.CurrentForm = "form_1";
                        }

                        if (!string.IsNullOrEmpty(model.SerializedRemainingAGList))
                        {
                            model.RemainingAssessmentGroups = JsonConvert.DeserializeObject<IEnumerable<AssignmentVM>>(model.SerializedRemainingAGList);

                        }

                        if (model.SelectedRemainingAssessmentGroups != null)
                        {
                            model.RemainingAssessmentGroupsForSummary = model.RemainingAssessmentGroups.Where(obj => model.SelectedRemainingAssessmentGroups.Contains(obj.id));
                        }
                        if (model.RemainingAssessmentGroupsForSummary != null)
                        {
                            model.SerializedRemainingAssessmentGroupsForSummary = JsonConvert.SerializeObject(model.RemainingAssessmentGroupsForSummary);

                        }

                        return View("CreateCampaign", model);
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

        [HttpPost]
        [Authorize]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> CreateCampaignS2(AGCampaignModel model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "InterviewerManagement" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "SeniorInterviewerManagement")
                    {
                        //create a campaign and save all the selected assessment groups
                        var result = _irfService.AddSkillChannelsForCampaign(model, emailid);
                        if (result)
                        {
                            //get all concerned interviewers
                            var interviewers = await _irfService.GetCampaignInterviewingMembers(model.id.HasValue ? model.id.Value : 0);
                            model.CurrentForm = "id_Summary";
                            model.Interviewers = interviewers;

                            //send mails to all concerned interviewers
                            SendCampaignLevelMailsToInterviewers(interviewers, model.AssessmentGroupName, model.id.Value);//fire and forget method, keeps sending emails to the interviewers in the background

                            if (!string.IsNullOrEmpty(model.SerializedRemainingAssessmentGroupsForSummary))
                            {
                                model.RemainingAssessmentGroupsForSummary = JsonConvert.DeserializeObject<IEnumerable<AssignmentVM>>(model.SerializedRemainingAssessmentGroupsForSummary);
                            }

                            model.OutcomeMessage = $"Successfully created campaign named {model.CampaignName} for assessment Group : {model.AssessmentGroupName}";
                        }
                        else
                        {
                            model.CurrentForm = "form_2";
                            model.OutcomeMessage = $"Failed to create campaign for assessment Group : {model.AssessmentGroupName}";

                        }
                        return View("CreateCampaign", model);
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

        private async Task SendCampaignLevelMailsToInterviewers(IEnumerable<AssessmentGroupInterviewerDetails> interviewers, string assessmentGroupName, int campaignId)
        {
            //string source = "ml";

            //var encryptedDate = await _encryptionHelper.EncryptDateTime(DateTime.Now);
            //foreach (var item in interviewers)
            //{
            //    string yesbutton = "https://portal.risebird.io/IRF/CampaignUpdateInterviewerAvailbilityStatusForAssessmentGroup?isAv=true&uniqueid=" + item.uniqueid + "&cmpId=" + campaignId + "&dec=" + encryptedDate;
            //    string nobutton = "https://portal.risebird.io/IRF/CampaignUpdateInterviewerAvailbilityStatusForAssessmentGroup?isAv=false&uniqueid=" + item.uniqueid + "&cmpId=" + campaignId + "&dec=" + encryptedDate;
            //    //string dropMyProfileButton = "https://portal.risebird.io/IRF/CampaignUpdateInterviewerAvailbilityStatusForAssessmentGroup?isAv=4&uniqueid=" + item.uniqueid + "&agId=" + irfId + "&dec=" + encryptedDate;
            //    Mail _mail = new Mail
            //    {
            //        TO = item.EmailId,
            //        Subject = "Current Availability (2 days) check for skills based on: " + assessmentGroupName + " Risebird Interviews"
            //    };

            //    var emailresult = await _notificationHelper.SendCampaignLevelInterviewerAvailabilityMails(item.EmailId, yesbutton + "&rsc=" + source, nobutton + "&rsc=" + source, assessmentGroupName, campaignId);

            //}

            //source = "wp";

            //yesbutton = yesbutton + "&rsc=" + source;
            //WhatsAppVM _whatsAppVM = new WhatsAppVM();
            //_whatsAppVM.Emailid = item.EmailId;
            //_whatsAppVM.PrimarySkill = item.AGName;

            //_whatsAppVM.buttonlink = yesbutton.Replace("https://portal.risebird.io/IRF/", "");
            //_whatsAppVM.Event = "SkillChannel Interviewer Availbility";

            var dto = new CampaignMailDTO { interviewers = interviewers, assessentGroupName = assessmentGroupName, campagignId = campaignId };
            var path = $"https://risebirdazuretimerfunctions.azurewebsites.net/api/SendCampaignMails";
            //var path = $"http://localhost:7071/api/SendCampaignMails";
            var functionKey = "eAasAihxsI0qTnAwlv-ZZIWiGZaZ0swoJS150jxIqU4XAzFuLF0xnA==";
            await _httpClient.SendRequestAsync(HttpMethod.Post, path, dto, functionKey, "", _cancellationToken);
        }

        [HttpGet]
        public async Task<JsonResult> CampaignUpdateInterviewerAvailbilityStatusForAssessmentGroup(bool isAv, string uniqueid, int cmpId, string dec, string rsc = null)
        {
            //check link expiry befire hitting db

            try
            {
                if (await _meetingService.UpdateInterviewerAvailbilityStatusCampaignLevel(isAv, uniqueid, cmpId, rsc))
                {
                    return Json("Status updated successfully.");
                }
                else
                {
                    return Json("Failed to update status, Please try again");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Failed to update status, Please try again");
            }

        }

        [HttpGet]
        public async Task<IActionResult> Campaigns(int returnBack)
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Client" || profileName == "SeniorInterviewerManagement" || profileName == "InterviewerManagement")
                    {
                        var model = await _irfService.GetCampaignList();
                        ViewBag.Profile = profileName;
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TriggerCampaignBatchCall(int campaignId)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                string initiatedBy = HttpContext.Session.GetString("email")?.Trim('"') ?? "Manual";
                var dto = new CampaignBatchCallRequestDTO
                {
                    CampaignId  = campaignId,
                    InitiatedBy = initiatedBy,
                    Source      = "Campaign"
                };

                var path = $"https://risebirdazuretimerfunctions.azurewebsites.net/api/CampaignBatchCall";
                //var path = $"http://localhost:7071/api/CampaignBatchCall";
                var functionKey = "8zKgFQa2S5HddU1aSqRIPVeEhuRHHGDwHoajMMSLS0H0AzFuQV1OPw==";

                HttpResponseMessage response;
                try
                {
                    response = await _batchCallHttpClient.SendRequestAsync(
                        HttpMethod.Post, path, dto, functionKey, "", System.Threading.CancellationToken.None);
                }
                catch (TaskCanceledException)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                        new Exception($"TriggerCampaignBatchCall: HTTP request to Azure Function timed out after 230s for campaign #{campaignId}"));
                    return Json(new { success = false, error = "The batch call service did not respond in time. The service may still be processing — check back shortly." });
                }
                catch (HttpRequestException httpEx)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                        new Exception($"TriggerCampaignBatchCall: HTTP request to Azure Function failed for campaign #{campaignId}. Status={httpEx.StatusCode}", httpEx));
                    return Json(new { success = false, error = $"Could not reach the batch call service: {httpEx.Message}" });
                }

                var body = await response.Content.ReadAsStringAsync();
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "";

                if (!response.IsSuccessStatusCode)
                {
                    var truncated = body?.Length > 500 ? body.Substring(0, 500) : body;
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                        new Exception($"TriggerCampaignBatchCall: Azure Function returned HTTP {(int)response.StatusCode} for campaign #{campaignId}. Body: {truncated}"));

                    return Json(new
                    {
                        success = false,
                        error = $"The batch call service returned HTTP {(int)response.StatusCode}. " +
                                (contentType.Contains("json") && !string.IsNullOrWhiteSpace(body)
                                    ? body
                                    : "Please check Application Insights for details.")
                    });
                }

                if (!string.IsNullOrWhiteSpace(body) && contentType.Contains("json"))
                    return Content(body, "application/json");

                return Json(new
                {
                    success = true,
                    started = true,
                    message = "Batch call started. Refresh this page in a few minutes to see the outcome."
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                    new Exception($"TriggerCampaignBatchCall: Unexpected exception for campaign #{campaignId}", ex));
                return Json(new { success = false, error = $"Unexpected error: {ex.Message}" });
            }
        }

        // Dedicated client for the batch-call trigger. The function now opens the attempt and returns
        // promptly (calls run in a background task inside the function), so a bounded timeout is plenty
        // and we avoid mutating the shared 100s-timeout portal client.
        private static readonly HttpClient _batchCallHttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(230)
        };

        // Returns the batch-call attempt history (and optionally per-call details) for a campaign.
        [HttpGet]
        public async Task<IActionResult> GetCampaignBatchCallAttempts(int campaignId)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                var attempts = await _eaRepository.GetCampaignBatchCallAttemptsAsync(campaignId);
                return Json(new
                {
                    success = true,
                    attempts = attempts.Select(a => new
                    {
                        id = a.Id,
                        attemptNumber = a.AttemptNumber,
                        attemptName = a.AttemptName,
                        initiatedBy = a.InitiatedBy,
                        initiatedAt = a.InitiatedAt.ToString("dd MMM yyyy, HH:mm"),
                        attempted = a.InterviewersAttempted,
                        succeeded = a.InterviewersSucceeded,
                        failed = a.InterviewersFailed,
                        skipped = a.InterviewersSkipped,
                        completedAt = a.CompletedAt?.ToString("dd MMM yyyy, HH:mm"),
                        status = a.Status
                    })
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCampaignBatchCallAttemptDetails(int batchCallLogId)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                var details = await _eaRepository.GetCampaignBatchCallAttemptDetailsAsync(batchCallLogId);
                return Json(new
                {
                    success = true,
                    details = details.Select(d => new
                    {
                        name = d.InterviewerName,
                        email = d.InterviewerEmail,
                        phone = d.Phone,
                        outcome = d.Outcome,
                        error = d.Error,
                        calledAt = d.CalledAt.ToString("dd MMM yyyy, HH:mm")
                    })
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CampaignDetails(int campaignId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Client" || profileName == "SeniorInterviewerManagement")
                    {
                        var model = await _irfService.GetCampaignDetailsById(campaignId);
                        // The details could not be resolved (campaign missing, or its detail query failed).
                        // Don't render the view with a null model — that NREs on @Model.CampaignName.
                        if (model == null || (model.id == 0 && string.IsNullOrEmpty(model.CampaignName)))
                        {
                            TempData["SuccessErrorMessage"] = "Campaign details could not be loaded for this campaign.";
                            return RedirectToAction("Campaigns", new { returnBack = 1 });
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
        ///option to add assessment groups to a MAG                

        public async Task<IActionResult> EditMAG(int assessmentGroupId, string assessmentGroupName, bool isAlreadyMasterAG)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");

                    if (profileName == "Admin" || profileName == "SupportAdmin")
                    {
                        //get all assessment groups related to this assessment group and pass it to the form.
                        //var model = new AGCampaignModel();
                        //var model = await _irfService.GetCampaignDetailsById(campaignId);

                        var model = new AGCampaignModel();
                        model.AssessmentGroupName = assessmentGroupName;
                        model.AssessmentGroupId = assessmentGroupId;
                        model.IsAlreadyMasterAG = isAlreadyMasterAG;
                        var secondaryAssessmentGroupsForMag = await _irfService.GetMAGSecondaryAssessmentGroupsById(assessmentGroupId);
                        model.SecondaryAGListForMAG = secondaryAssessmentGroupsForMag;
                        //var searchValue = assessmentGroupName.Trim()?.Split(" ")[0];
                        var allAGs = (await _irfService.GetSimilarAssessmentGroups(model.AssessmentGroupName.Trim(), model.IsAlreadyMasterAG, true));//call db and search for assessmentGroups that carry a similar name.         /// the functionality of this function was later changed to get both similar and dissimilar assessment groups, differentiated from each other by a flag

                        model.AssessmentGroups = (IList<AssignmentVM>)(allAGs.Where(x => x.isDirectlyTargeted)?.ToList());

                        if (model.AssessmentGroups.Where(x => x.AssessmentGroup?.Trim() == model.AssessmentGroupName?.Trim()) != null && model.AssessmentGroups.Where(x => x.AssessmentGroup?.Trim() == model.AssessmentGroupName?.Trim()).Count() > 0)
                        {
                            model.AssessmentGroups.Where(x => x.AssessmentGroup?.Trim() == model.AssessmentGroupName?.Trim()).FirstOrDefault().IsSelectedForCampaign = true;

                        }

                        model.RemainingAssessmentGroups = allAGs?.Where(x => !x.isDirectlyTargeted);
                        model.RemainingAssessmentGroupsList = model.RemainingAssessmentGroups?.ToList();

                        model.SerializedRemainingAGList = JsonConvert.SerializeObject(model.RemainingAssessmentGroups);


                        //I want primary assessment groups of the mag to be checked and disabled, the the remaining, that are directly targeted, to be shown with check enabled
                        //model.CuratedData = model.CuratedData.Where(c => (filter.SubscriptionStatus.Count() > 0 ? filter.SubscriptionStatus.Contains(c.SubscriptionStatus) : c.SubscriptionStatus == null)
                        //&& (filter.CuratedGroupName.Count() > 0 ? filter.CuratedGroupName.Contains(c.CuratedGroupName) : c.CuratedGroupName == null)).ToList();


                        foreach (var item in secondaryAssessmentGroupsForMag)
                        {
                            foreach (var ag in model.AssessmentGroups)
                            {
                                if (ag.id == item.AssessmentGroupId)
                                {
                                    ag.IsSelectedForCampaign = true;
                                }
                            }
                        }

                        var listOfAgsToBeRemoved = new List<AssignmentVM>();

                        foreach (var item in secondaryAssessmentGroupsForMag)
                        {
                            foreach (var ag in model.RemainingAssessmentGroupsList)
                            {
                                if (ag.id == item.AssessmentGroupId)
                                {
                                    listOfAgsToBeRemoved.Add(ag);
                                    //model.RemainingAssessmentGroupsList.Remove(ag);
                                }
                            }
                        }
                        model.RemainingAssessmentGroupsList = model.RemainingAssessmentGroupsList.Except(listOfAgsToBeRemoved)?.ToList();
                        //var intersectingAGS = model.AssessmentGroups.Where(c => )
                        model.RemainingAssessmentGroups = model.RemainingAssessmentGroupsList;
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

        [HttpPost]
        [Authorize]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> EditMAG(AGCampaignModel model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SupportAdmin")
                    {
                        // create mag only
                        if (await _irfService.EditMAG(model, emailid))
                        {
                            TempData["SuccessErrorMessageInterviewer"] = $"{model.AssessmentGroupName} successfully flagged as Master Assessment Group";
                            return RedirectToAction("InterviewerEntitle");
                        }
                        else
                        {
                            TempData["SuccessErrorMessageInterviewer"] = $"Failed to edit mag : {model.AssessmentGroupName}.";
                            return RedirectToAction("InterviewerEntitle");
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
                TempData["SuccessErrorMessageInterviewer"] = $"Failed to edit mag : {model.AssessmentGroupName}.";
                return RedirectToAction("InterviewerEntitle");
            }
        }

        //[AcceptVerbs("Post","Get")]
        [HttpPost]
        public async Task<bool> CheckAssessmentGroupAlraedyExist(string AssessmentGroupName)
        {
            try
            {
                var data = await _irfService.CheckAssessmentGroupAlraedyExist(AssessmentGroupName);
                if (data)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        #endregion 

        [HttpGet]
        public async Task<IActionResult> SkillChannelsByTemplate(int returnBack)
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Client" || profileName == "SeniorInterviewerManagement" || profileName == "InterviewerManagement")
                    {
                        var model = await _irfService.GetSkillChannelsCreatedUsingTemplates(emailid);
                        ViewBag.Profile = profileName;
                        if (model != null)
                        {
                            return View(model);
                        }
                        else { return View(); }
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
        #region Callibration Meetings
        [HttpGet]
        public async Task<IActionResult> GetCalibrationMeetingDetails(int irfid)
        {
            var model = await _irfService.GetCalibrationCallDetails(irfid);
            if (model == null)
            {
                model = new List<CallibrationCallVM>();
            }
            return PartialView(model);
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> CalibrationMeetings(string emailid, string uniqueid)
        {
            try
            {
                ViewBag.id = uniqueid;
                ViewBag.emailid = emailid;
                var model = await _irfService.GetAllCalibrationMeetingsById(emailid);
                return View(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                var model = new List<CallibrationCallVM>();
                return View(model);
            }
        }
        [Authorize]
        public async Task<IActionResult> CalibrationMeetingsDetails(string type, int returnBack)
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
                        var model = await _irfService.GetAllCalibrationMeetingsById(emailid, "All");
                        if (type == "Completed")
                        {
                            ViewBag.Type = "Completed";
                            model = model.Where(x => x.MeetingDate < DateTime.Now).ToList();
                        }
                        else
                        {
                            ViewBag.Type = "UpComing";
                            model = model.Where(x => x.MeetingDate >= DateTime.Now).ToList();
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
        public async Task<IActionResult> CalibrationMeetingNotes(int id)
        {
            ViewBag.MeetingId = id;
            var model = await _irfService.GetCalibrationMeetingDetailsById(id);
            if (string.IsNullOrWhiteSpace(model.Notes))
            {
                string transcriptText;
                using (HttpClient client = _httpClientFactory.CreateClient())
                {
                    transcriptText = await client.GetStringAsync(model.TranscriptLink);
                }
                var aiAnalysis = await _assistantServiceHelper.TranscriptToNotes(transcriptText);
                if (aiAnalysis != null)
                {
                    var notesrespose = aiAnalysis.Data?.First()?.Content?.FirstOrDefault()?.Text?.Value;
                    //var response = JsonConvert.DeserializeObject<TranscriptAnalysisSchema>(aiAnalysis.Data?.First()?.Content?.FirstOrDefault()?.Text?.Value);
                    //bool saveOperationResult = true;
                    if (notesrespose != null)
                    {
                        await _irfService.SaveCalibrationMeetingNotesById(id, notesrespose);
                    }
                    model.Notes = notesrespose;
                }
            }
            return View(model);
        }
        #endregion
        #region Candidate Consent Form
        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 1048576000)]
        [RequestSizeLimit(1048576000)]
        public async Task<JsonResult> UploadConsentFile()
        {
            if (Request.Form.Files.Count() > 0)
            {
                var irfid = Convert.ToInt32(Request.Form["irfid"].ToString());
                string uploadedFileURL = string.Empty;
                var model = Request.Form.Files[0];
                string outputFileName = model.FileName.Replace(" ", "").Replace("+", "").Replace("_", "");
                var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model);
                FileInfo fi = new FileInfo(outputFileName);
                if (fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                {
                    //await _azureStorageHelper.InsertBlob(outputFileName, "consentforms", model);
                    await _azureStorageHelper.InsertResumeBlob(outputFileName, "consentforms", model, fi.Extension);
                    var containerRef = _azureStorageHelper.GetCloudBlobContainer("consentforms");
                    var blob = containerRef.GetBlockBlobReference(outputFileName);
                    uploadedFileURL = blob.Uri.ToString();
                    var result = await _irfService.UpdateConsentFile(irfid, uploadedFileURL);
                    return Json("Upload completed successfully.");
                }
                else
                {
                    return Json("Only PDF file is allowed.");
                }
            }
            else
            {
                return Json("");
            }
        }
        [HttpPost]
        public async Task<JsonResult> GetConsentForm(int irfid)
        {
            var result = await _irfService.GetConsentFile(irfid);
            return Json(result);
        }
        #endregion
        #region Candidate Video Consent Form
        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 1048576000)]
        [RequestSizeLimit(1048576000)]
        public async Task<JsonResult> UploadVideoConsentFile()
        {
            if (Request.Form.Files.Count() > 0)
            {
                var irfid = Convert.ToInt32(Request.Form["irfid"].ToString());
                string uploadedFileURL = string.Empty;
                var model = Request.Form.Files[0];
                string outputFileName = model.FileName.Replace(" ", "").Replace("+", "").Replace("_", "");
                var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model);
                FileInfo fi = new FileInfo(outputFileName);
                if (fileMimeType == "video/mpeg" || fileMimeType == "video/mp4" || fileMimeType == "video/quicktime")
                {
                    await _azureStorageHelper.InsertImageBlob(outputFileName, "candidatevideos", model, fileMimeType);
                    var containerRef = _azureStorageHelper.GetCloudBlobContainer("candidatevideos");
                    var blob = containerRef.GetBlockBlobReference(outputFileName);
                    uploadedFileURL = blob.Uri.ToString();
                    var sas = _sasUrlHelper.CreateSasUrl("candidatevideos");
                    if (!string.IsNullOrEmpty(uploadedFileURL) && !string.IsNullOrEmpty(sas))
                    {
                        uploadedFileURL = uploadedFileURL + "?" + sas;
                    }
                    var result = await _irfService.UpdateVideoConsentFile(irfid, uploadedFileURL);
                    return Json("Upload completed successfully.");
                }
                else
                {
                    return Json("Only MP4 file is allowed.");
                }
            }
            else
            {
                return Json("");
            }
        }
        [HttpPost]
        public async Task<JsonResult> GetVideoConsentForm(int irfid)
        {
            var result = await _irfService.GetVideoConsentForm(irfid);
            return Json(result);
        }
        #endregion

        #region Day Stats
        public async Task<IActionResult> DateChannelDetails(string emailid, bool isClientLogin = false, string uniqueid = null)
        {
            try
            {
                ViewBag.emailid = emailid;
                ViewBag.uniqueid = uniqueid;
                ViewBag.IsClientLogin = isClientLogin;
                var profileName = "Client";
                ViewBag.profile = profileName;
                ViewBag.PageStatus = "Public";
                HttpContext.Session.SetString("email", JsonConvert.SerializeObject(emailid));
                var startdate = DateTime.Now;
                var enddate = DateTime.Now.AddDays(1);
                if (HttpContext.Session.GetString("SkillChannelDetailsFilters") != null)
                {
                    var filter = JsonConvert.DeserializeObject<SkillChannelDetailsFilter>(HttpContext.Session.GetString("SkillChannelDetailsFilters"));
                    if (filter.StartDate != null)
                    {
                        startdate = filter.StartDate.Value;
                        enddate = startdate.AddDays(1);
                    }
                }
                var DayinterviewList = (await _irfService.GetDayStatsByClientUser(startdate, enddate, emailid))?.ToList();
                foreach (var item in DayinterviewList)
                {
                    if (item.InterviewDuration > 0)
                    {
                        item.NoOfIntterview = 12 * item.Concurrency * 60 / item.InterviewDuration;
                    }
                    else
                    {
                        item.NoOfIntterview = 12 * item.Concurrency;
                    }
                }
                return View(DayinterviewList);
            }
            catch (Exception ex)
            {
                return RedirectToAction("PublicCandidateStatus", "Candidate", new { id = uniqueid });
            }
        }
        [HttpPost]
        public async Task<IActionResult> DateChannelDetails(SkillChannelDetailsFilter filter, string emailid, bool isClientLogin = false, string uniqueid = null)
        {
            if (filter != null)
            {
                HttpContext.Session.SetString("SkillChannelDetailsFilters", JsonConvert.SerializeObject(filter));
            }
            return RedirectToAction("DateChannelDetails", new { emailid = emailid, uniqueid = uniqueid, isClientLogin = isClientLogin });
        }
        [HttpGet]
        public async Task<IActionResult> ResetDateChannelDetails(string emailid, bool isClientLogin = false, string uniqueid = null)
        {
            try
            {
                HttpContext.Session.Remove("SkillChannelDetailsFilters");
                return RedirectToAction("DateChannelDetails", new { emailid = emailid, uniqueid = uniqueid, isClientLogin = isClientLogin });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        [HttpGet]
        public async Task<IActionResult> DateChannelStatsDetailedView(int irfintid, string IRFId, string type, string uniqueid)
        {
            try
            {
                DateTime startDate = DateTime.Now;
                var enddate = DateTime.Now.AddDays(1);
                if (HttpContext.Session.GetString("SkillChannelDetailsFilters") != null)
                {
                    var filter = JsonConvert.DeserializeObject<SkillChannelDetailsFilter>(HttpContext.Session.GetString("SkillChannelDetailsFilters"));
                    startDate = filter.StartDate.Value;
                    enddate = startDate.AddDays(1);
                }
                var irfList = (await _irfService.GetGetDayStatsByClientUserDetailedView(irfintid, type, startDate, enddate))?.ToList();
                irfList = irfList.Select(x => { x.InterviewStatus = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription(); x.InterviewStatusTwoDesc = ((InterviewStatus)Convert.ToInt32(x.InterviewStatusTwo)).GetDescription(); return x; }).ToList();
                var portallink = "https://" + Request.Host.ToString();
                foreach (var item in irfList)
                {
                    item.ReportLink = await GenerateExecutiveReportURl(portallink, Convert.ToInt32(item.id), item.Irfintid);
                }
                ViewBag.projectname = IRFId;
                ViewBag.type = type;
                ViewBag.PageType = "DayStats";
                return View("ProjectSkillChannelStatsDetailedView", irfList);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("PublicCandidateStatus", "Candidate", new { id = uniqueid });
            }
        }
        #endregion
        #region Code Snippet
        [HttpGet]
        public async Task<IActionResult> InterviewCodeSnippet(int id, int irfid)
        {
            var profileName = HttpContext.Session.GetString("profileName")?.Trim();
            profileName = profileName?.Replace("\"", "");
            var emailid = HttpContext.Session.GetString("email")?.Trim();
            emailid = emailid?.Replace("\"", "");
            TechSkillComments model = new TechSkillComments();
            //var skills = await _irfService.GetTechSkills(irfid);
            //ViewBag.Skills = new SelectList(skills, "id", "Name");
            var skills = await _irfService.GetIRFTechSkills(irfid);
            ViewBag.Skills = new SelectList(skills);
            model.IRFId = irfid;
            model.interviewid = id;
            model.Profile = profileName;
            model.CreatedBy = emailid;
            return PartialView(model);
        }
        [HttpPost]
        public async Task<IActionResult> InterviewCodeSnippet(TechSkillComments model)
        {
            return View();
        }
        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 1048576000)]
        [RequestSizeLimit(1048576000)]
        public async Task<JsonResult> UploadInterviewCodeSnippet()
        {
            if (Request.Form.Files.Count() > 0)
            {
                var interviewid = Convert.ToInt32(Request.Form["interviewid"].ToString());
                string uploadedFileURL = string.Empty;
                var model = Request.Form.Files[0];
                string outputFileName = interviewid.ToString() + "_" + model.FileName.Replace(" ", "_");
                var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model);
                FileInfo fi = new FileInfo(outputFileName);
                if (fi.Extension.ToLower() == ".txt")
                {
                    await _azureStorageHelper.InsertResumeBlob(outputFileName, "codesnippet", model, fi.Extension.ToLower());
                    var containerRef = _azureStorageHelper.GetCloudBlobContainer("codesnippet");
                    var blob = containerRef.GetBlockBlobReference(outputFileName);
                    uploadedFileURL = blob.Uri.ToString();
                    var result = await _irfService.UpdateCodeSnippetFile(interviewid, uploadedFileURL, null);
                    return Json("Upload completed successfully.");
                }
                else
                {
                    return Json("Only Text file is allowed.");
                }
            }
            else
            {
                return Json("");
            }
        }
        [HttpPost]
        public async Task<JsonResult> UploadInterviewTextCodeSnippet(int interviewid, string codesnippettext)
        {
            var fileName = interviewid.ToString() + ".txt";
            var uploadedFileURL = string.Empty;
            using (Stream myStream = new System.IO.MemoryStream(Encoding.ASCII.GetBytes(codesnippettext)))
            {
                uploadedFileURL = await _azureStorageHelper.InsertBlobCloudCompare(fileName, "codesnippet", null, null, myStream);
            }
            var result = await _irfService.UpdateCodeSnippetFile(interviewid, uploadedFileURL, null);
            return Json("Code snippet saved successfully.");
        }
        [HttpPost]
        public async Task<JsonResult> GetCodeSnippet(int interviewid)
        {
            var result = await _irfService.GetCodeSnippetFile(interviewid);
            return Json(result);
        }
        #endregion
        #region Manage Interview Select/Reject
        [HttpGet]
        public async Task<IActionResult> SkillChannelMeetingsDetails(string uniqueid, int irfId, string meetingtype, string IRFName)
        {
            try
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid?.Replace("\"", "");
                if (string.IsNullOrEmpty(emailid))
                {
                    return RedirectToAction("PublicAllMeetings", "Candidate", new { id = uniqueid });
                }
                var selectedMeeting = new List<MeetingVM>();
                var meetings = new List<MeetingVM>();
                meetings = await _irfService.GetIRFInterviewMeetingsForClient(irfId, "Client", emailid);
                meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.intervieweStatusStatDesc = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription(); x.MeetingStatusStatusDesc = ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription(); return x; }).ToList();
                ViewBag.IRF = IRFName;
                ViewBag.id = uniqueid;
                ViewBag.IRFintId = irfId;
                ViewBag.meetingtype = meetingtype;
                ViewBag.Clientid = meetings?.FirstOrDefault()?.ClientId;
                if (meetingtype == "Select")
                {
                    meetings = meetings?.Where(x => (x.InterviewStatus == 2 || x.InterviewStatus == 4) && !x.RedoInterview)?.ToList();
                }
                else if (meetingtype == "Reject")
                {
                    meetings = meetings?.Where(x => x.InterviewStatus == 3 || x.InterviewStatus == 5 && !x.RedoInterview)?.ToList();
                }
                var role = await _clientService.GetClientRoleByEmailId(emailid);
                ViewBag.ClientRole = role != 0 ? ((ClientRoleEnum)Convert.ToInt32(role)).GetDescription() : string.Empty;
                return View(meetings);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("PublicAllMeetings", "Candidate", new { id = uniqueid });
            }
        }
        [HttpGet]
        public async Task<IActionResult> ProjectMeetingsDetails(string uniqueid, int projectid, string meetingtype, string ProjectUniqueid)
        {
            try
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid?.Replace("\"", "");
                if (string.IsNullOrEmpty(emailid))
                {
                    return RedirectToAction("PublicAllMeetings", "Candidate", new { id = uniqueid });
                }
                var selectedMeeting = new List<MeetingVM>();
                var meetings = new List<MeetingVM>();
                meetings = await _irfService.GetProjectInterviewMeetings(projectid, "Client", emailid);
                meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.intervieweStatusStatDesc = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription(); x.MeetingStatusStatusDesc = ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription(); return x; }).ToList();
                ViewBag.IRF = ProjectUniqueid;
                ViewBag.id = uniqueid;
                ViewBag.IRFintId = projectid;
                ViewBag.meetingtype = meetingtype;
                if (meetingtype == "Select")
                {
                    meetings = meetings?.Where(x => (x.InterviewStatus == 2 || x.InterviewStatus == 4) && !x.RedoInterview)?.ToList();
                }
                else if (meetingtype == "Reject")
                {
                    meetings = meetings?.Where(x => x.InterviewStatus == 3 || x.InterviewStatus == 5 && !x.RedoInterview)?.ToList();
                }
                return View("SkillChannelMeetingsDetails", meetings);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("PublicAllMeetings", "Candidate", new { id = uniqueid });
            }
        }
        #endregion
        #region Multi-Stack
        public ActionResult Task()
        {
            return View();
        }
        public async Task checkSkillSetandCombine(IRFVm irfDetails)
        {
            if (string.IsNullOrWhiteSpace(irfDetails.JDTitleTwo))
            {
                irfDetails.JDTitleTwo = "";
            }
            if (string.IsNullOrWhiteSpace(irfDetails.ClientName))
            {
                irfDetails.ClientName = "";
            }
            foreach (var item in irfDetails.SkillCollection)
            {
                item.SkillSet = 1;
            }
            if (irfDetails.SkillSetsevaluate > 1)
            {
                foreach (var item in irfDetails.SkillTwoCollection)
                {
                    item.SkillSet = 2;
                    irfDetails.SkillCollection.Add(item);
                }
            }
        }

        /// <summary>
        /// HM score-rubric save: replace SkillSet 1 (and 2 when posted) from the form, keep other skill sets from DB.
        /// usp_TechSkillBulkInsert deletes all rows for the IRF — never pass only partial form data without merging.
        /// </summary>
        private static List<TransSkill> BuildDeliveryHeadRubricSkillsForSave(IRFVm model, List<TransSkill> existingAll)
        {
            var result = new List<TransSkill>();
            var primary = (model.SkillCollection ?? new List<TransSkill>())
                .Where(x => x != null && (!string.IsNullOrWhiteSpace(x.SkillName) || x.LevelId > 0))
                .ToList();
            foreach (var item in primary)
            {
                item.SkillSet = 1;
                result.Add(item);
            }

            if (model.SkillSetsevaluate > 1)
            {
                var secondaryFromForm = (model.SkillTwoCollection ?? new List<TransSkill>())
                    .Where(x => x != null && (!string.IsNullOrWhiteSpace(x.SkillName) || x.LevelId > 0))
                    .ToList();
                if (secondaryFromForm.Any())
                {
                    foreach (var item in secondaryFromForm)
                    {
                        item.SkillSet = 2;
                        result.Add(item);
                    }
                }
                else
                {
                    result.AddRange(existingAll.Where(x => x.SkillSet == 2));
                }
                result.AddRange(existingAll.Where(x => x.SkillSet != 1 && x.SkillSet != 2));
            }
            else
            {
                result.AddRange(existingAll.Where(x => x.SkillSet != 1));
            }

            return result;
        }
        public async Task checkSkillSetandSeperate(IRFVm irfDetails)
        {
            irfDetails.SkillTwoCollection = irfDetails.SkillCollection.Where(x => x.SkillSet == 2).ToList();
            irfDetails.SkillCollection = irfDetails.SkillCollection.Where(x => x.SkillSet == 1).ToList();
        }
        [HttpGet]
        public async Task<IActionResult> PublicMultiStackSkillChannelDetails(string emailid, bool isClientLogin = false, string uniqueid = null)
        {
            ViewBag.emailid = emailid;
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
            if (HttpContext.Session.GetString("MultiStackSkillChannelDetailsFilters") == null)
            {
                irfList = (await _irfService.GetAllIRFMultiStackDetailsIRF(DateTime.Now.AddDays(-30), DateTime.Now.AddDays(7), profileName, emailid))?.ToList();
            }
            else
            {
                var filter = JsonConvert.DeserializeObject<SkillChannelDetailsFilter>(HttpContext.Session.GetString("MultiStackSkillChannelDetailsFilters"));
                if (filter.StartDate != null && filter.EndDate != null)
                {
                    irfList = (await _irfService.GetAllIRFMultiStackDetailsIRF(filter.StartDate.Value, filter.EndDate.Value, profileName, emailid))?.ToList();
                }
                else
                {
                    irfList = (await _irfService.GetAllIRFMultiStackDetailsIRF(DateTime.Now.AddDays(-30), DateTime.Now.AddDays(7), profileName, emailid))?.ToList();
                }
            }
            return View(irfList);
        }
        [HttpPost]
        public async Task<IActionResult> PublicMultiStackSkillChannelDetails(SkillChannelDetailsFilter filter, string emailid, bool isClientLogin = false, string uniqueid = null)
        {
            if (filter != null)
            {
                HttpContext.Session.SetString("MultiStackSkillChannelDetailsFilters", JsonConvert.SerializeObject(filter));
            }
            return RedirectToAction("PublicMultiStackSkillChannelDetails", new { emailid = emailid, uniqueid = uniqueid, isClientLogin = isClientLogin });
        }
        [HttpGet]
        public async Task<IActionResult> ResetPublicMultiStackSkillChannelDetailsFilter(string emailid, bool isClientLogin = false, string uniqueid = null)
        {
            try
            {
                HttpContext.Session.Remove("MultiStackSkillChannelDetailsFilters");
                return RedirectToAction("PublicMultiStackSkillChannelDetails", new { emailid = emailid, uniqueid = uniqueid, isClientLogin = isClientLogin });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        [HttpPost]
        public async Task<IActionResult> IRFMutliStackInterviewStatusDetailedView(int irfid, string irfname, string type)
        {
            try
            {
                DateTime startDate = DateTime.Now.AddDays(-30);
                if (HttpContext.Session.GetString("MultiStackSkillChannelDetailsFilters") != null)
                {
                    var filter = JsonConvert.DeserializeObject<SkillChannelDetailsFilter>(HttpContext.Session.GetString("MultiStackSkillChannelDetailsFilters"));
                    startDate = filter.StartDate.Value;
                }
                var irfList = (await _irfService.IRFMutliStackInterviewStatusDetailedView(irfname, type, startDate))?.ToList();
                irfList = irfList.Select(x => { x.InterviewStatus = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription(); x.InterviewStatusTwoDesc = ((InterviewStatus)Convert.ToInt32(x.InterviewStatusTwo)).GetDescription(); return x; }).ToList();
                var portallink = "https://" + Request.Host.ToString();
                foreach (var item in irfList)
                {
                    item.ReportLink = await GenerateExecutiveReportURl(portallink, Convert.ToInt32(item.id), item.Irfintid);
                }
                if (type == "SelectedRejected")
                {
                    type = "Primary Select Secondary Reject";
                }
                else if (type == "RejectedSelected")
                {
                    type = "Primary Reject Secondary Select";
                }
                ViewBag.irfname = irfname;
                ViewBag.type = type;
                ViewBag.irfid = irfid;
                return PartialView(irfList);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return PartialView();
            }
        }
        [HttpGet]
        public async Task<FileContentResult> IRFMutliStackInterviewStatusDetailedExcel(string irfname, string type)
        {
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("SkillChannelDetails");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_MutliStackInterview_" + irfname + ".xlsx";
            try
            {
                DateTime startDate = DateTime.Now.AddDays(-30);
                if (type == "Primary Select Secondary Reject")
                {
                    type = "SelectedRejected";
                }
                else if (type == "Primary Reject Secondary Select")
                {
                    type = "RejectedSelected";
                }
                if (HttpContext.Session.GetString("MultiStackSkillChannelDetailsFilters") != null)
                {
                    var filter = JsonConvert.DeserializeObject<SkillChannelDetailsFilter>(HttpContext.Session.GetString("MultiStackSkillChannelDetailsFilters"));
                    startDate = filter.StartDate.Value;
                }
                var irfList = (await _irfService.IRFMutliStackInterviewStatusDetailedView(irfname, type, startDate))?.ToList();
                irfList = irfList.Select(x => { x.InterviewStatus = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription(); x.InterviewStatusTwoDesc = ((InterviewStatus)Convert.ToInt32(x.InterviewStatusTwo)).GetDescription(); return x; }).ToList();
                var portallink = "https://" + Request.Host.ToString();
                foreach (var item in irfList)
                {
                    item.ReportLink = await GenerateExecutiveReportURl(portallink, Convert.ToInt32(item.id), item.Irfintid);
                }

                Sheet.Cells["A1:AI1"].Style.Font.Bold = true;
                Sheet.Cells["A1:AI1"].Style.Font.Size = 12;
                Sheet.Cells["A1:AI1"].Style.WrapText = true;
                int startColumn = 1; int endColumn = 15;
                for (int a = startColumn; a <= endColumn; a++)
                {
                    Sheet.Column(a).Width = 40;
                }
                Sheet.Cells[1, 1].Value = "Interview Date";
                Sheet.Cells[1, 2].Value = "Candidate Name";
                Sheet.Cells[1, 3].Value = "Candidate Emailid";
                Sheet.Cells[1, 4].Value = "POC";
                Sheet.Cells[1, 5].Value = "Interviewer";
                Sheet.Cells[1, 6].Value = "Report";
                Sheet.Cells[1, 7].Value = "Tech Score(Primary)";
                Sheet.Cells[1, 8].Value = "Interview Status(Primary)";
                Sheet.Cells[1, 9].Value = "Tech Score(Secondary)";
                Sheet.Cells[1, 10].Value = "Interview Status(Secondary)";
                Sheet.Cells[1, 11].Value = "Candidate Solution(Primary)";
                Sheet.Cells[1, 12].Value = "Candidate Solution(Secondary)";
                Sheet.Cells[1, 13].Value = "Combined Status";

                int i = 2;
                if (irfList.Count() > 0)
                {
                    foreach (var item in irfList)
                    {
                        Sheet.Cells[i, 1].Value = item.interviewDate;
                        Sheet.Cells[i, 2].Value = item.candidateName;
                        Sheet.Cells[i, 3].Value = item.candidateEmail;
                        Sheet.Cells[i, 4].Value = item.ClientHrContact;
                        Sheet.Cells[i, 5].Value = item.InterviewerCodedName;
                        if (!string.IsNullOrEmpty(item.ReportLink))
                        {
                            try
                            {
                                Sheet.Cells[i, 6].Hyperlink = new Uri(item.ReportLink, UriKind.Absolute);
                                Sheet.Cells[i, 6].Value = "Report";
                                Sheet.Cells[i, 6].Style.Font.UnderLine = true;
                                Sheet.Cells[i, 6].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                            }
                            catch (Exception)
                            {
                                Sheet.Cells[i, 6].Value = "No Report";
                            }
                        }
                        else
                        {
                            Sheet.Cells[i, 6].Value = "No Report";
                        }
                        Sheet.Cells[i, 7].Value = item.TechScoreone;
                        Sheet.Cells[i, 8].Value = item.InterviewStatus;
                        Sheet.Cells[i, 9].Value = item.TechScoretwo;
                        Sheet.Cells[i, 10].Value = item.InterviewStatusTwo;
                        Sheet.Cells[i, 11].Value = item.CodeSnippet;
                        if (!string.IsNullOrEmpty(item.CodeSnippet))
                        {
                            try
                            {
                                Sheet.Cells[i, 11].Hyperlink = new Uri(item.CodeSnippet, UriKind.Absolute);
                                Sheet.Cells[i, 11].Value = "Solution File";
                                Sheet.Cells[i, 11].Style.Font.UnderLine = true;
                                Sheet.Cells[i, 11].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                            }
                            catch (Exception)
                            {
                                Sheet.Cells[i, 11].Value = "";
                            }
                        }
                        else
                        {
                            Sheet.Cells[i, 11].Value = "";
                        }
                        if (!string.IsNullOrEmpty(item.CodeSnippetTwo))
                        {
                            try
                            {
                                Sheet.Cells[i, 12].Hyperlink = new Uri(item.CodeSnippetTwo, UriKind.Absolute);
                                Sheet.Cells[i, 12].Value = "Solution File";
                                Sheet.Cells[i, 12].Style.Font.UnderLine = true;
                                Sheet.Cells[i, 12].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                            }
                            catch (Exception)
                            {
                                Sheet.Cells[i, 12].Value = "";
                            }
                        }
                        else
                        {
                            Sheet.Cells[i, 12].Value = "";
                        }
                        if (item.InterviewStatusTwoDesc == "0")
                        {
                            item.InterviewStatusTwoDesc = "";
                        }
                        Sheet.Cells[i, 13].Value = item.JDTiltle + ": " + item.InterviewStatus + " / " + item.JDTitleTwo + ": " + item.InterviewStatusTwoDesc;
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
            catch (Exception ex)
            {
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Data export failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
        [HttpGet]
        public async Task<IActionResult> ProjectSkillChannelStats(string tabtype, string emailid, bool isClientLogin = false, string uniqueid = null)
        {
            ViewBag.emailid = emailid;
            ViewBag.uniqueid = uniqueid;
            ViewBag.IsClientLogin = isClientLogin;
            if (!isClientLogin)
            {
                ViewBag.PageStatus = "Public";
            }
            var Emailid = string.Empty;
            if (HttpContext.Session.GetString("email") != null)
            {
                Emailid = HttpContext.Session.GetString("email")?.Trim();
                Emailid = Emailid.Replace("\"", "");
            }
            if (Emailid != emailid)
            {
                return RedirectToAction("PublicCandidateStatus", "Candidate", new { id = uniqueid });
            }
            HttpContext.Session.SetString("email", JsonConvert.SerializeObject(emailid));
            var profileName = "Client";
            ViewBag.profile = profileName;
            var irfList = new List<ProjectStatsVM>();
            if (HttpContext.Session.GetString("MultiStackSkillChannelDetailsFilters") == null)
            {
                irfList = (await _irfService.GetProjectSkillChannelStatsCount(DateTime.Now.AddDays(-30), DateTime.Now.AddDays(7), profileName, emailid))?.ToList();
            }
            else
            {
                var filter = JsonConvert.DeserializeObject<SkillChannelDetailsFilter>(HttpContext.Session.GetString("MultiStackSkillChannelDetailsFilters"));
                if (filter.StartDate != null && filter.EndDate != null)
                {
                    irfList = (await _irfService.GetProjectSkillChannelStatsCount(filter.StartDate.Value, filter.EndDate.Value, profileName, emailid))?.ToList();
                }
                else
                {
                    irfList = (await _irfService.GetProjectSkillChannelStatsCount(DateTime.Now.AddDays(-30), DateTime.Now.AddDays(7), profileName, emailid))?.ToList();
                }
            }
            if (string.IsNullOrWhiteSpace(tabtype))
            {
                tabtype = "SingleStack";
            }
            ViewBag.Type = tabtype;
            if (tabtype == "SingleStack")
            {
                irfList = irfList.Where(x => x.SkillSetsevaluate == 1).ToList();
            }
            else if (tabtype == "MultiStack")
            {
                irfList = irfList.Where(x => x.SkillSetsevaluate > 1).ToList();
            }
            return View(irfList);
        }
        [HttpPost]
        public async Task<IActionResult> ProjectSkillChannelStats(SkillChannelDetailsFilter filter, string tabtype, string emailid, bool isClientLogin = false, string uniqueid = null)
        {
            if (filter != null)
            {
                HttpContext.Session.SetString("ProjectSkillChannelStatsFilters", JsonConvert.SerializeObject(filter));
            }
            return RedirectToAction("ProjectSkillChannelStats", new { tabtype = tabtype, emailid = emailid, uniqueid = uniqueid, isClientLogin = isClientLogin });
        }
        [HttpGet]
        public async Task<IActionResult> ResetProjectSkillChannelStats(string tabtype, string emailid, bool isClientLogin = false, string uniqueid = null)
        {
            try
            {
                HttpContext.Session.Remove("ProjectSkillChannelStatsFilters");
                return RedirectToAction("ProjectSkillChannelStats", new { tabtype = tabtype, emailid = emailid, uniqueid = uniqueid, isClientLogin = isClientLogin });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        [HttpGet]
        public async Task<IActionResult> ProjectSkillChannelStatsDetailedView(int projectid, string projectname, string type, string uniqueid)
        {
            try
            {
                DateTime startDate = DateTime.Now.AddDays(-30);
                if (HttpContext.Session.GetString("ProjectSkillChannelStatsFilters") != null)
                {
                    var filter = JsonConvert.DeserializeObject<SkillChannelDetailsFilter>(HttpContext.Session.GetString("ProjectSkillChannelStatsFilters"));
                    startDate = filter.StartDate.Value;
                }
                var irfList = (await _irfService.GetProjectSkillChannelStatsDetailedView(projectid, type, startDate))?.ToList();
                irfList = irfList.Select(x => { x.InterviewStatus = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription(); x.InterviewStatusTwoDesc = ((InterviewStatus)Convert.ToInt32(x.InterviewStatusTwo)).GetDescription(); return x; }).ToList();
                var portallink = "https://" + Request.Host.ToString();
                foreach (var item in irfList)
                {
                    item.ReportLink = await GenerateExecutiveReportURl(portallink, Convert.ToInt32(item.id), item.Irfintid);
                }
                ViewBag.projectname = projectname;
                if (type == "SelectedNRejected")
                {
                    type = "Primary Select and Secondary Reject";
                }
                else if (type == "RejectedNSelected")
                {
                    type = "Primary Reject and Secondary Select";
                }
                ViewBag.type = type;
                ViewBag.PageType = "ProjectStats";
                return View(irfList);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("PublicCandidateStatus", "Candidate", new { id = uniqueid });
            }
        }
        #endregion
        #region Partners Stats
        [HttpGet]
        public async Task<IActionResult> PartnerDetailsStats(string emailid, bool isClientLogin = false, string uniqueid = null)
        {
            ViewBag.emailid = emailid;
            ViewBag.uniqueid = uniqueid;
            ViewBag.IsClientLogin = isClientLogin;
            if (!isClientLogin)
            {
                ViewBag.PageStatus = "Public";
            }
            var Emailid = string.Empty;
            if (HttpContext.Session.GetString("email") != null)
            {
                Emailid = HttpContext.Session.GetString("email")?.Trim();
                Emailid = Emailid.Replace("\"", "");
            }
            if (Emailid != emailid)
            {
                return RedirectToAction("PublicCandidateStatus", "Candidate", new { id = uniqueid });
            }
            HttpContext.Session.SetString("email", JsonConvert.SerializeObject(emailid));
            var profileName = "Client";
            ViewBag.profile = profileName;
            var irfList = new List<ProjectStatsVM>();
            if (HttpContext.Session.GetString("PartnersStatsFilters") == null)
            {
                irfList = (await _irfService.GetPartnerDetailsStatsCount(DateTime.Now.AddDays(-30), DateTime.Now.AddDays(7), profileName, emailid))?.ToList();
            }
            else
            {
                var filter = JsonConvert.DeserializeObject<SkillChannelDetailsFilter>(HttpContext.Session.GetString("PartnersStatsFilters"));
                if (filter.StartDate != null && filter.EndDate != null)
                {
                    irfList = (await _irfService.GetPartnerDetailsStatsCount(filter.StartDate.Value, filter.EndDate.Value, profileName, emailid))?.ToList();
                }
                else
                {
                    irfList = (await _irfService.GetPartnerDetailsStatsCount(DateTime.Now.AddDays(-30), DateTime.Now.AddDays(7), profileName, emailid))?.ToList();
                }
            }
            return View(irfList);
        }
        [HttpPost]
        public async Task<IActionResult> PartnerDetailsStats(SkillChannelDetailsFilter filter, string emailid, bool isClientLogin = false, string uniqueid = null)
        {
            if (filter != null)
            {
                HttpContext.Session.SetString("PartnersStatsFilters", JsonConvert.SerializeObject(filter));
            }
            return RedirectToAction("PartnerDetailsStats", new { emailid = emailid, uniqueid = uniqueid, isClientLogin = isClientLogin });
        }
        [HttpGet]
        public async Task<IActionResult> ResetPartnerDetailsStats(string emailid, bool isClientLogin = false, string uniqueid = null)
        {
            try
            {
                HttpContext.Session.Remove("PartnersStatsFilters");
                return RedirectToAction("PartnerDetailsStats", new { emailid = emailid, uniqueid = uniqueid, isClientLogin = isClientLogin });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        [HttpGet]
        public async Task<IActionResult> PartnerDetailsStatsDetailedView(int userid, string partnername, string type, string uniqueid)
        {
            try
            {
                DateTime startDate = DateTime.Now.AddDays(-30);
                var enddate = DateTime.Now.AddDays(7);
                if (HttpContext.Session.GetString("PartnersStatsFilters") != null)
                {
                    var filter = JsonConvert.DeserializeObject<SkillChannelDetailsFilter>(HttpContext.Session.GetString("PartnersStatsFilters"));
                    startDate = filter.StartDate.Value;
                    enddate = filter.EndDate.Value.AddDays(1);
                }
                var irfList = (await _irfService.GetPartnerDetailsStatsDetailedView(userid, type, startDate, enddate))?.ToList();
                irfList = irfList.Select(x => { x.InterviewStatus = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription(); x.InterviewStatusTwoDesc = ((InterviewStatus)Convert.ToInt32(x.InterviewStatusTwo)).GetDescription(); return x; }).ToList();
                var portallink = "https://" + Request.Host.ToString();
                foreach (var item in irfList)
                {
                    item.ReportLink = await GenerateExecutiveReportURl(portallink, Convert.ToInt32(item.id), item.Irfintid);
                }
                ViewBag.projectname = partnername;
                ViewBag.type = type;
                ViewBag.PageType = "PartnerStats";
                return View("ProjectSkillChannelStatsDetailedView", irfList);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("PublicCandidateStatus", "Candidate", new { id = uniqueid });
            }
        }
        #endregion
        #region Client User SKill Channel Stats
        [HttpGet]
        public async Task<IActionResult> UserChannelDetailsStats(string emailid, bool isClientLogin = false, string uniqueid = null)
        {
            ViewBag.emailid = emailid;
            ViewBag.uniqueid = uniqueid;
            ViewBag.IsClientLogin = isClientLogin;
            if (!isClientLogin)
            {
                ViewBag.PageStatus = "Public";
            }
            var Emailid = string.Empty;
            if (HttpContext.Session.GetString("email") != null)
            {
                Emailid = HttpContext.Session.GetString("email")?.Trim();
                Emailid = Emailid.Replace("\"", "");
            }
            if (Emailid != emailid)
            {
                return RedirectToAction("PublicCandidateStatus", "Candidate", new { id = uniqueid });
            }
            HttpContext.Session.SetString("email", JsonConvert.SerializeObject(emailid));
            var profileName = "Client";
            ViewBag.profile = profileName;
            var irfList = new List<IRF>();
            if (HttpContext.Session.GetString("UserStatsFilters") == null)
            {
                irfList = (await _irfService.GetUserDetailsStatsCount(DateTime.Now.AddDays(-30), DateTime.Now.AddDays(7), profileName, emailid))?.ToList();
            }
            else
            {
                var filter = JsonConvert.DeserializeObject<SkillChannelDetailsFilter>(HttpContext.Session.GetString("UserStatsFilters"));
                if (filter.StartDate != null && filter.EndDate != null)
                {
                    irfList = (await _irfService.GetUserDetailsStatsCount(filter.StartDate.Value, filter.EndDate.Value, profileName, emailid))?.ToList();
                }
                else
                {
                    irfList = (await _irfService.GetUserDetailsStatsCount(DateTime.Now.AddDays(-30), DateTime.Now.AddDays(7), profileName, emailid))?.ToList();
                }
            }
            return View(irfList);
        }
        [HttpPost]
        public async Task<IActionResult> UserChannelDetailsStats(SkillChannelDetailsFilter filter, string emailid, bool isClientLogin = false, string uniqueid = null)
        {
            if (filter != null)
            {
                HttpContext.Session.SetString("UserStatsFilters", JsonConvert.SerializeObject(filter));
            }
            return RedirectToAction("UserChannelDetailsStats", new { emailid = emailid, uniqueid = uniqueid, isClientLogin = isClientLogin });
        }
        [HttpGet]
        public async Task<IActionResult> ResetUserChannelDetailsStats(string emailid, bool isClientLogin = false, string uniqueid = null)
        {
            try
            {
                HttpContext.Session.Remove("UserStatsFilters");
                return RedirectToAction("UserChannelDetailsStats", new { emailid = emailid, uniqueid = uniqueid, isClientLogin = isClientLogin });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        [HttpGet]
        public async Task<IActionResult> UserStatsDetailedView(int irfId, string irfName, string type, string emailid, string uniqueid)
        {
            try
            {
                var Emailid = string.Empty;
                if (HttpContext.Session.GetString("email") != null)
                {
                    Emailid = HttpContext.Session.GetString("email")?.Trim();
                    Emailid = Emailid.Replace("\"", "");
                }
                if (Emailid != emailid)
                {
                    return RedirectToAction("PublicCandidateStatus", "Candidate", new { id = uniqueid });
                }
                DateTime startDate = DateTime.Now.AddDays(-30);
                var enddate = DateTime.Now.AddDays(7);
                if (HttpContext.Session.GetString("PartnersStatsFilters") != null)
                {
                    var filter = JsonConvert.DeserializeObject<SkillChannelDetailsFilter>(HttpContext.Session.GetString("PartnersStatsFilters"));
                    startDate = filter.StartDate.Value;
                    enddate = filter.EndDate.Value.AddDays(1);
                }
                var irfList = (await _irfService.GetUserStatsDetailedView(irfId, emailid, type, startDate, enddate))?.ToList();
                irfList = irfList.Select(x => { x.InterviewStatus = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription(); x.InterviewStatusTwoDesc = ((InterviewStatus)Convert.ToInt32(x.InterviewStatusTwo)).GetDescription(); return x; }).ToList();
                //foreach (var item in irfList)
                //{
                //    item.ReportLink = "https://" + Request.Host.ToString() + "/Interview/ExecutiveReport?interviewId=" + item.id + "&irfId=" + item.Irfintid + "&isClient=true";
                //}
                ViewBag.projectname = irfName;
                ViewBag.type = type;
                ViewBag.PageType = "UserStats";
                return View("ProjectSkillChannelStatsDetailedView", irfList);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("PublicCandidateStatus", "Candidate", new { id = uniqueid });
            }
        }
        #endregion

        [HttpPost]
        public async Task<IActionResult> GenerateInterviewerApplicationLink(int irfId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {

                    if (irfId > 0)
                    {
                        var encryptedId = await _encryptionHelper.EncryptStringValue(irfId.ToString());

                        //var url = "https://portal.risebird.io/Interviewer/Apply?id=" + HttpUtility.UrlEncode(encryptedId);

                        var url = Request.Scheme + "://" + Request.Host.Value + "/Interviewer/ApplicationForm?id=" + HttpUtility.UrlEncode(encryptedId);
                        //var url = Url.Action("ApplicationForm", "Interviewer",
                        //        new { id = HttpUtility.UrlEncode(encryptedId) },
                        //        Request.Scheme);
                        return Json(url);
                    }
                    else
                    {
                        return Json(null);
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
                //return Json("Some error occured. Please try again!");
                return Json(null);
            }
        }


        //irfLink for interviewers who are already a part of the system
        [HttpPost]
        public async Task<IActionResult> GenerateInternalInterviewerApplicationLink(int irfId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {

                    if (irfId > 0)
                    {
                        var encryptedId = await _encryptionHelper.EncryptStringValue(irfId.ToString());
                        //var url = Url.Action("Apply", "Interviewer",
                        //        new { id = encryptedId },
                        //        Request.Scheme);
                        //var url = "https://portal.risebird.io/Interviewer/FTApply?id=" + irfId.ToString();

                        var url = "https://portal.risebird.io/Interviewer/FTApply?id=" + HttpUtility.UrlEncode(encryptedId);
                        //var url = Url.Action("Apply", "Interviewer",
                        //        new { id = irfId },
                        //        Request.Scheme);
                        return Json(url);
                    }
                    else
                    {
                        return Json(null);
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
                //return Json("Some error occured. Please try again!");
                return Json(null);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerateMAGApplicationLink(int assessmentGroupId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (assessmentGroupId > 0)
                    {
                        var encryptedId = await _encryptionHelper.EncryptStringValue(assessmentGroupId.ToString());
                        var url = "https://portal.risebird.io/Interviewer/MAGApply?id=" + HttpUtility.UrlEncode(encryptedId);

                        return Json(url);
                    }
                    else
                    {
                        return Json(null);
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
                //return Json("Some error occured. Please try again!");
                return Json(null);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerateExternalMAGApplicationLink(int assessmentGroupId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (assessmentGroupId > 0)
                    {
                        var encryptedId = await _encryptionHelper.EncryptStringValue(assessmentGroupId.ToString());
                        var url = "https://portal.risebird.io/Interviewer/MagApplicationForm?id=" + Uri.EscapeDataString(encryptedId);
                        return Json(url);
                    }
                    else
                    {
                        return Json(null);
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
                return Json(null);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddInterviewerToIRF(int irfId, int userId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SupportAdmin" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement")
                    {
                        var result = await _irfService.AddAGInterviewerToSkillChannel(irfId, userId, emailid);
                        if (result)
                        {
                            return Json("Added successfully!");

                        }
                        else
                        {
                            return Json("Sorry! Failed to add!");
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
                //return Json("Some error occured. Please try again!");
                return Json("An error has occured. Failed to add!");
            }
        }
        #region Executive Report 
        public async Task<string> GenerateExecutiveReportURl(string portalurl, int interviewid, int irfid)
        {
            var meetingid = await _encryptionHelper.EncryptIntegerValue(interviewid, "ExecutiveReport");
            var irf = await _encryptionHelper.EncryptIntegerValue(irfid, "ExecutiveReport");
            var reportlink = string.Empty;
            if (portalurl.LastOrDefault() == '/')
            {
                reportlink = portalurl + "Interview/ExecutiveReport?meetingid=" + meetingid + "&irf=" + irf;
            }
            else
            {
                reportlink = portalurl + "/Interview/ExecutiveReport?meetingid=" + meetingid + "&irf=" + irf;
            }
            return reportlink;
        }
        #endregion
        #region Client Billing Rate
        [HttpPost]
        public async Task<JsonResult> SaveInterviewerBillingRate(int billingrate, int Userid, int IRFid)
        {
            try
            {
                await _irfService.SaveInterviewerBillingRate(billingrate, Userid, IRFid);
                return Json("Success");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("");
            }
        }
        #endregion
        [HttpGet]
        public async Task<IActionResult> GetIRFInterviewerBillingMembers(int irfId, string type)
        {
            try
            {
                ViewBag.Profile = "";
                if (HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    ViewBag.Profile = profileName;
                }
                ViewBag.Type = "IRF";
                var model = await _irfService.GetIRFInterviewerMembers(irfId);
                if (type == "Active")
                {
                    model = model?.Where(x => x.IsActive).ToList();
                }
                else if (type == "Inactive")
                {
                    model = model?.Where(x => !x.IsActive).ToList();
                }
                return PartialView("IRFInterviewerMembersPartial", model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<IActionResult> EditIRFInterviewers(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SupportAdmin" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SeniorHR")
                    {
                        IRFVm model = new IRFVm();

                        model = await _irfService.GetSkillChannelInterviewersForEdit(id);
                        //var interviewers = await _irfService.GetAssessmentGroupInterviewerMembers(Convert.ToInt16(model.AssessmentGroupId), true);

                        //ViewBag.Interviewers = new SelectList(interviewers, "UserId", "EmailId");
                        if (TempData["SuccessErrorMessageAddInterviewers"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageAddInterviewers"].ToString();
                            TempData.Remove("SuccessErrorMessageAddInterviewers");
                        }
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
                return null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> SkillChannelInterviewers(int irfId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SupportAdmin" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SeniorHR")
                    {
                        //var viewModel = new List<Interviewer>() { };
                        var viewModel = new List<IRFInterviewerMembers>() { };
                        IRFVm model = new IRFVm();
                        //model.LoggedEmail = emailid;
                        //model.Profile = profileName;
                        model = await _irfService.GetSkillChannelInterviewersForEdit(irfId);

                        DateTime absoluteTime = new DateTime(2024, 12, 2, 16, 30, 0);
                        var AssessmentsGroups = new List<AssignmentVM>();
                        if (model.CreatedOn < absoluteTime)
                        {
                            var interviewers = await _irfService.GetInterviewersForIRF(model.AssessmentGroupId, model.id);
                            interviewers.RemoveAll(item => model.InterviewerCollection.Any(other => other.UserId == item.UserId));
                            foreach (var interviewer in interviewers)
                            {
                                var obj = new IRFInterviewerMembers();
                                obj.UserId = interviewer.UserId;
                                obj.EmailId = interviewer.EmailId;
                                obj.CompanyName = interviewer.CompanyName;
                                obj.PrimarySkill = interviewer.PrimarySkill;
                                obj.ResumeLink = interviewer.ResumeLink;
                                obj.PerSuccessRate = model.FlatRate;
                                obj.IRFId = irfId;
                                viewModel.Add(obj);
                                //add interviewer into viewModal
                            }
                            //ViewBag.Interviewers = new SelectList(interviewerOptions, "UserId", "EmailId");
                        }
                        else
                        {
                            viewModel = (await _irfService.GetAssessmentGroupInterviewerMembers(Convert.ToInt16(model.AssessmentGroupId))).ToList();
                            viewModel.RemoveAll(item => model.InterviewerCollection.Any(other => other.UserId == item.UserId));
                            viewModel.ToList().ForEach(x => x.PerSuccessRate = model.FlatRate);
                        }

                        ViewBag.IRFId = irfId;
                        ViewBag.IsNewSkillChannel = model.IsNewSkillChannel;
                        ViewBag.LoggedEmail = emailid;
                        ViewBag.Profile = profileName;
                        ViewBag.IsValid = "True";
                        ViewBag.Result = true;
                        return PartialView("SkillChannelInterviewersV2", (IList<IRFInterviewerMembers>)viewModel);
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

        //[HttpPost]
        //[DisableRequestSizeLimit]
        //public async Task<IActionResult> AddIRFInterviewers(IList<IRFInterviewerMembers> interviewers, int irfId)//IRFVm model
        //{
        //    try
        //    {
        //        if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
        //        {
        //            var profileName = HttpContext.Session.GetString("profileName")?.Trim();
        //            profileName = profileName.Replace("\"", "");
        //            var emailid = HttpContext.Session.GetString("email")?.Trim();
        //            emailid = emailid.Replace("\"", "");
        //            if (profileName == "Admin" || profileName == "SupportAdmin" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SeniorHR")
        //            {
        //                //var result = await _irfService.AddIRFInterviewers(model);
        //                interviewers.ToList().ForEach(x => x.IRFId = irfId);
        //                var result = await _irfService.AddIRFInterviewersV2(interviewers, emailid);
        //                if (result)
        //                {
        //                    //TempData["SuccessErrorMessageAddInterviewers"] = $"Successfully added interviewer(s) to skill channel";// : {model.IRFId}
        //                    ViewBag.Result = "True";
        //                }
        //                else
        //                {
        //                    //TempData["SuccessErrorMessageAddInterviewers"] = $"Failed to add interviewer(s) to skill channel";//: {model.IRFId}
        //                    ViewBag.Result = "False";
        //                }

        //                ViewBag.IsValid = "True";

        //                return PartialView("SkillChannelInterviewersV2", interviewers);


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
        //        //return Json("Some error occured. Please try again!");
        //        return Json("An error has occured. Failed to add!");
        //    }
        //}


        [HttpPost]
        [DisableRequestSizeLimit]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddIRFInterviewers([FromBody] InterviewerListToAddDTO dtoObject)//IRFVm model ; , int irfId
        {
            try
            {
                if (dtoObject == null || dtoObject.Interviewers == null || !dtoObject.Interviewers.Any())
                {
                    return BadRequest("No data provided.");
                }
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SupportAdmin" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SeniorHR")
                    {
                        var result = await _irfService.AddIRFInterviewersV2(dtoObject.Interviewers, emailid);
                        if (result)
                        {
                            return Ok(new { success = true, message = "Data saved successfully." });
                        }
                        else
                        {
                            return StatusCode(500, new { success = false, message = "An unexpected error occurred." });
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
                //return Json("An error has occured. Failed to add!");
                return StatusCode(500, new { success = false, message = "An unexpected error occurred.", error = ex.Message });

            }
        }


        //InterviewerListToAddDTO
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> InterviewerWithOutBillingRate()
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
                        var model = await _irfService.GetInterviewersWithOutBillingRate();
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

        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 1048576000)]
        [RequestSizeLimit(1048576000)]
        public async Task<JsonResult> UploadInterviewerResume()//UploadCandidateResumesViewModel model
        {
            try
            {
                var emailId = HttpContext.Session.GetString("email")?.Trim();
                emailId = emailId.Replace("\"", "");
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");

                if (profileName == "Admin" || profileName == "SupportAdmin" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SeniorHR"
                    || (string.Equals(profileName, "Interviewer", StringComparison.OrdinalIgnoreCase)
                        && int.TryParse(Request.Form["IrfId"], out var smeIrfId)
                        && smeIrfId > 0
                        && await _interviewerService.IsSmeAssignedToIrfAsync(emailId, smeIrfId)))
                {
                    if (Request.Form.Files.Count() > 0)
                    {
                        int userId = Convert.ToInt32(Request.Form["UserId"].ToString());
                        string interviewerEmailId = Request.Form["InterviewerEmailId"].ToString();
                        var addedByUserId = _irfService.GetUserId(emailId);
                        string sasUrl = string.Empty;
                        if (addedByUserId > 0)
                        {
                            foreach (var file in Request.Form.Files)
                            {
                                if (!ResumeUploadValidator.IsAllowedResumeUpload(file, _fileMimeTypeHelper, out var resumeValidationError))
                                {
                                    return Json(resumeValidationError);
                                }

                                var fileName = file.FileName.Replace(" ", "_");
                                FileInfo fi = new FileInfo(fileName);

                                await _azureStorageHelper.InsertResumeBlob(fileName, "resumes", file, fi.Extension);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                var blob = containerRef.GetBlockBlobReference(fileName);
                                string uploadedaudioFileURL = blob.Uri.ToString();

                                if (uploadedaudioFileURL == null)
                                {
                                    return Json("Resume upload has failed.");
                                }

                                var sas = _sasUrlHelper.CreateSasUrl("resumes");
                                sasUrl = uploadedaudioFileURL + "?" + sas;
                            }
                            var result = await _interviewerService.UpdateInterviewerResume(userId, sasUrl, emailId);
                            if (result)
                            {
                                return Json($"Resume uploaded Successfully for {interviewerEmailId}!");

                            }
                            else
                            {
                                return Json($"Failed to upload Resume for {interviewerEmailId}!");
                            }
                        }
                        else
                        {
                            return Json($"Failed to upload Resume for {interviewerEmailId}!");
                        }
                    }
                    else
                    {
                        return Json("Please select valid file for import(.pdf, .doc, .docx)!");
                    }
                }
                else
                {
                    return Json("Unauthorized!");
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Failed to upload Resume!");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangeInterviewerActiveState(int userId, bool status, int irfId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SupportAdmin" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SeniorHR")
                    {

                        var result = await _interviewerService.UpdateIRFInterviewerActiveStatus(userId, status, irfId, emailid);

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
        public async Task<IActionResult> ChangeInterviewerAvailabilityStatus(int userId, bool availabilityStatus, int irfId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SupportAdmin" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SeniorHR")
                    {

                        var result = await _interviewerService.UpdateInterviewerAvailabilityStatus(userId, availabilityStatus, irfId, emailid);
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
        public async Task<IActionResult> AddUpdateUSTJobTitle(int agId, string ustJobTitle)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SupportAdmin")
                    {
                        var result = await _irfService.UpdateUSTJobTitle(agId, ustJobTitle, emailid);
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
        public async Task<JsonResult> UpdateInterviewerRbVet(int transInterviewerId, bool isVerified, int irfId, int userId, string interviewerEmail, string reasonCategory = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null)
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }
                var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");

                if (string.Equals(profileName, "Interviewer", StringComparison.OrdinalIgnoreCase))
                {
                    if (!await _interviewerService.IsSmeAssignedToIrfAsync(emailid, irfId))
                    {
                        return Json(new { success = false, message = "Forbidden" });
                    }
                }
                else if (profileName == "Client" || profileName == "Public")
                {
                    return Json(new { success = false, message = "Forbidden" });
                }

                var channelIv = await _irfRepository.GetSkillChannelInterviewersForEdit(irfId);
                var ti = channelIv?.InterviewerCollection?.FirstOrDefault(x => x.id == transInterviewerId);

                if (string.IsNullOrWhiteSpace(interviewerEmail) && userId > 0)
                {
                    interviewerEmail = ti?.EmailId;
                    if (string.IsNullOrWhiteSpace(interviewerEmail))
                    {
                        var ivRow = (await _irfService.GetEditIRF(irfId, 0, false))?.InterviewerCollection?
                            .FirstOrDefault(x => x.UserId == userId);
                        interviewerEmail = ivRow?.EmailId;
                    }
                }

                if (string.IsNullOrWhiteSpace(interviewerEmail))
                {
                    return Json(new { success = false, message = "Interviewer not found." });
                }

                var interviewers = new List<TransInterviewer>
                {
                    new TransInterviewer { id = transInterviewerId, IsSMEVerified = isVerified }
                };
                var ok = await _interviewerService.UpdateVerifiedInterviewers(interviewers, emailid);

                if (ok)
                {
                    var clientAction = isVerified ? "Shortlisted" : "Rejected";
                    await _meetingService.RemoveOrShortlistInterviewerFromIRF(irfId, interviewerEmail, clientAction);

                    var (isActive, isAvailable) = DmIvRecordHelper.ComputeFlagsFromVet(isVerified, clientAction);
                    await _interviewerService.UpdateIRFInterviewerActiveStatus(userId, isActive, irfId, emailid, isAvailable: isAvailable);

                    if (ti != null)
                    {
                        if (!isVerified)
                        {
                            var rbBefore = ti.IsSMEVerified || ti.IsSMEVerifiedStored == true ? "1" : "0";
                            await _transInterviewerAuditRepository.InsertAsync(
                                transInterviewerId,
                                DmIvInactiveAuditEventTypes.RbVetFlippedToNo,
                                rbBefore,
                                "0",
                                reasonCategory,
                                null,
                                emailid);
                        }
                        else
                        {
                            await _transInterviewerAuditRepository.InsertAsync(
                                transInterviewerId,
                                DmIvInactiveAuditEventTypes.RbVetFlippedToYes,
                                "0",
                                "1",
                                null,
                                null,
                                emailid);
                            await _transInterviewerAuditRepository.ClearAutoQcFieldsAsync(transInterviewerId);
                        }
                    }
                }

                var rbClientAction = isVerified ? "Shortlisted" : "Rejected";
                var rbFlags = DmIvRecordHelper.ComputeFlagsFromVet(isVerified, rbClientAction);
                return Json(new
                {
                    success = ok,
                    active = ok && rbFlags.isActive,
                    available = ok && rbFlags.isAvailable,
                    clientVet = ok && isVerified,
                    transId = transInterviewerId,
                    irfId,
                    userId,
                    email = DeliveryHeadInstructionViewMode.IsSmeViewForIrf(HttpContext, irfId) ? null : interviewerEmail
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> UpdateInterviewerClientVet(int irfId, string interviewerEmail, bool isApproved, int userId, string Type = null, string reasonCategory = null)
        {
            try
            {
                var isClientShare = IsClientShareRequest(irfId, Type);

                if (isClientShare)
                {
                    if (!await AuthorizeClientShareAccessAsync(irfId, Type))
                        return Json(new { success = false, message = "Forbidden", error = "You do not have access to this skill channel." });
                }
                else if (HttpContext.Session.GetString("email") == null)
                {
                    return Json(new { success = false, message = "Unauthorized", error = "You don't have permission to vet this interviewer." });
                }
                else
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
                    if (profileName == "Client" || profileName == "Interviewer" || profileName == "Public")
                    {
                        return Json(new { success = false, message = "You don't have permission to vet this interviewer.", error = "You don't have permission to vet this interviewer." });
                    }
                }

                var emailid = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
                // Same scope as DM Approval grid (usp_GetSkillChannelDetailsForUpdate) — not assessment-group filtered.
                var channelIv = await _irfRepository.GetSkillChannelInterviewersForEdit(irfId);
                var ti = channelIv?.InterviewerCollection?.FirstOrDefault(x => x.UserId == userId);
                if (ti == null)
                {
                    return Json(new { success = false, message = "Interviewer not found on this channel.", error = "Interviewer not found on this channel." });
                }
                var rbVetYes = ti.IsSMEVerified || ti.IsSMEVerifiedStored == true;
                if (isApproved && !rbVetYes)
                {
                    return Json(new { success = false, message = "Client Vet requires RB Vet first.", error = "Client Vet requires RB Vet first." });
                }

                var action = isApproved ? "Shortlisted" : "Rejected";
                var emailForUpdate = !string.IsNullOrWhiteSpace(ti.EmailId) ? ti.EmailId.Trim() : interviewerEmail?.Trim();
                if (string.IsNullOrWhiteSpace(emailForUpdate))
                {
                    return Json(new { success = false, message = "Interviewer email is missing.", error = "Interviewer email is missing." });
                }

                var ok = await _meetingService.RemoveOrShortlistInterviewerFromIRF(irfId, emailForUpdate, action);
                if (!ok)
                {
                    return Json(new { success = false, message = "Could not update client vet status. Please try again.", error = "Could not update client vet status. Please try again." });
                }

                var (isActive, isAvailable) = DmIvRecordHelper.ComputeFlagsFromVet(rbVetYes, action);
                var activeOk = await _interviewerService.UpdateIRFInterviewerActiveStatus(userId, isActive, irfId, emailid, isAvailable: isAvailable);
                if (!activeOk)
                {
                    return Json(new { success = false, message = "Client vet saved but active status could not be updated. Please refresh and try again.", error = "Client vet saved but active status could not be updated. Please refresh and try again." });
                }

                var flags = DmIvRecordHelper.ComputeFlagsFromVet(rbVetYes, action);
                if (ti != null)
                {
                    if (!isApproved)
                    {
                        await _transInterviewerAuditRepository.InsertAsync(
                            ti.id,
                            DmIvInactiveAuditEventTypes.ClientVetFlippedToNo,
                            ti.ClientAction ?? string.Empty,
                            action,
                            reasonCategory,
                            null,
                            emailid ?? "ClientShare");
                    }
                    else
                    {
                        await _transInterviewerAuditRepository.InsertAsync(
                            ti.id,
                            DmIvInactiveAuditEventTypes.ClientVetFlippedToYes,
                            "Rejected",
                            action,
                            null,
                            null,
                            emailid ?? "ClientShare");
                    }
                }
                return Json(new { success = true, clientAction = action, active = flags.isActive, available = flags.isAvailable });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = ex.Message, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetInterviewerResumeSummary(string resumeLink)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null)
                    return Json(new { success = false });

                if (string.IsNullOrEmpty(resumeLink))
                    return View("~/Views/Candidate/_CandidateResumeSummaryBodyPartial.cshtml", (ResumeSummaryVM)null);

                var summary = await _candidateService.ResumeSummaryFromLink(resumeLink);
                return View("~/Views/Candidate/_CandidateResumeSummaryBodyPartial.cshtml", summary);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return View("~/Views/Candidate/_CandidateResumeSummaryBodyPartial.cshtml", (ResumeSummaryVM)null);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetInterviewerCodedCvSummary(string codedCvLink, int? irfId = null, string Type = null)
        {
            const string viewPath = "~/Views/Candidate/_CandidateResumeSummaryBodyPartial.cshtml";
            try
            {
                var isClientShare = irfId.HasValue && irfId.Value > 0 && IsClientShareRequest(irfId.Value, Type);
                if (isClientShare)
                {
                    if (!await AuthorizeClientShareAccessAsync(irfId.Value, Type))
                        return StatusCode(403);
                }
                else if (HttpContext.Session.GetString("email") == null)
                    return View(viewPath, (ResumeSummaryVM)null);

                if (string.IsNullOrWhiteSpace(codedCvLink))
                    return View(viewPath, (ResumeSummaryVM)null);

                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
                var json = await client.GetStringAsync(codedCvLink);
                if (string.IsNullOrWhiteSpace(json))
                    return View(viewPath, (ResumeSummaryVM)null);

                var summary = JsonConvert.DeserializeObject<ResumeSummaryVM>(json);
                return View(viewPath, summary);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return View(viewPath, (ResumeSummaryVM)null);
            }
        }

        /// <summary>
        /// AI resume summary for DM Approval: generates summary, persists JSON to InterviewerProfile.CodedResumeLink.
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> GenerateInterviewerResumeSummaryAndSave(int userId, string resumeLink, int irfId = 0)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null)
                    return Json(new { success = false, message = "Unauthorized" });

                var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");

                if (string.Equals(profileName, "Interviewer", StringComparison.OrdinalIgnoreCase))
                {
                    if (irfId <= 0 || !await _interviewerService.IsSmeAssignedToIrfAsync(emailid, irfId))
                        return Json(new { success = false, message = "Not authorized." });
                }
                else if (profileName == "Client" || profileName == "Public")
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }
                else if (profileName != "Admin" && profileName != "SupportAdmin" && profileName != "InterviewerManagement"
                    && profileName != "SeniorInterviewerManagement" && profileName != "SeniorHR")
                {
                    return Json(new { success = false, message = "Not authorized." });
                }

                if (userId <= 0 || string.IsNullOrWhiteSpace(resumeLink))
                    return Json(new { success = false, message = "Resume is required." });

                // Always overwrite InterviewerProfile.CodedResumeLink — legacy Affinda/XML/PDF links are replaced on explicit Generate.
                var summary = await _candidateService.ResumeSummaryFromLink(resumeLink);
                if (summary == null)
                    return Json(new { success = false, message = "OpenAI did not return a valid resume summary. The resume may be unreadable, empty, or in an unsupported format." });

                var uploadedLink = await UploadInterviewerResumeSummaryAsCodedCvAsync(summary);
                if (string.IsNullOrEmpty(uploadedLink))
                    return Json(new { success = false, message = "Could not upload coded CV to storage." });

                var saveModel = new InterviewerProfileVM
                {
                    UserId = userId,
                    CodedResumeLink = uploadedLink,
                    IsResumeRedactAttempted = true,
                    IsResumeRedactFailed = false
                };
                var saved = await _interviewerService.SaveInterviewerCodedResume(saveModel);
                if (!saved)
                    return Json(new { success = false, message = "Could not save coded CV to interviewer profile." });

                var summaryHtml = await RenderResumeSummaryPartialToStringAsync(summary);
                var cvCode = _interviewerService.GetNickNames(userId)?.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(cvCode))
                    cvCode = "CV";
                else
                    cvCode = cvCode.Trim();

                return Json(new
                {
                    success = true,
                    codedCvLink = uploadedLink,
                    cvCode,
                    savedAt = DateTime.UtcNow.ToString("o"),
                    summaryHtml
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = DmResumeSummaryFailureMessage(ex) });
            }
        }

        private static string DmResumeSummaryFailureMessage(Exception ex)
        {
            if (ex == null) return "Failed to generate resume summary.";
            if (ex is TaskCanceledException || ex is OperationCanceledException)
                return "OpenAI API timeout — please retry.";
            if (ex.InnerException is TaskCanceledException || ex.InnerException is OperationCanceledException)
                return "OpenAI API timeout — please retry.";
            var msg = ex.Message?.Trim();
            if (!string.IsNullOrEmpty(msg) && msg.Length <= 240)
                return msg;
            return "Failed to generate resume summary.";
        }

        private async Task<string> UploadInterviewerResumeSummaryAsCodedCvAsync(ResumeSummaryVM summary)
        {
            var jsonPayload = JsonConvert.SerializeObject(summary, Newtonsoft.Json.Formatting.Indented);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonPayload));
            var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var fileName = "ResumeSummary_" + unixTime + "_" + Guid.NewGuid().ToString("N").Substring(0, 10);
            var uploadedLink = await _azureStorageHelper.InsertBlobCloudCompare(
                fileName, "resumes", null, stream, contentType: "application/json");
            if (string.IsNullOrEmpty(uploadedLink))
                return null;

            var sas = _sasUrlHelper.CreateSasUrl("resumes");
            if (!string.IsNullOrEmpty(sas))
                uploadedLink = uploadedLink + "?" + sas;

            return uploadedLink;
        }

        private async Task<string> RenderResumeSummaryPartialToStringAsync(ResumeSummaryVM summary)
        {
            const string viewPath = "~/Views/Candidate/_CandidateResumeSummaryBodyPartial.cshtml";
            var viewEngine = HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
            var tempDataProvider = HttpContext.RequestServices.GetService(typeof(ITempDataProvider)) as ITempDataProvider;
            if (viewEngine == null || tempDataProvider == null)
                return null;

            var actionContext = new ActionContext(HttpContext, RouteData, ControllerContext.ActionDescriptor);
            using var sw = new StringWriter();
            var viewResult = viewEngine.FindView(actionContext, viewPath, isMainPage: false);
            if (viewResult.View == null)
                return null;

            var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                Model = summary
            };
            var viewContext = new ViewContext(
                actionContext,
                viewResult.View,
                viewDictionary,
                new TempDataDictionary(HttpContext, tempDataProvider),
                sw,
                new HtmlHelperOptions());

            await viewResult.View.RenderAsync(viewContext);
            return sw.ToString();
        }

        [HttpPost]
        public async Task<JsonResult> RequestVideoKYC(string interviewerEmail, string interviewerName)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null)
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }
                var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
                if (profileName == "Client" || profileName == "Interviewer" || profileName == "Public")
                {
                    return Json(new { success = false, message = "Forbidden" });
                }

                var dashboardLink = $"https://{Request.Host}/Dashboard/InterviewerIndex";
                var result = await _notificationHelper.SendRequestVideoKYCEmail(interviewerEmail, interviewerName ?? interviewerEmail, dashboardLink);
                bool sent = !string.IsNullOrEmpty(result) && !result.Contains("Failed");
                return Json(new { success = sent });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> SaveDmIvPolicy(int irfId, bool dmIvLockNew, string dmAssignmentPolicy, int dmDefaultInterviewCap)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null)
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }
                var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
                if (profileName == "Client" || profileName == "Interviewer" || profileName == "Public")
                {
                    return Json(new { success = false, message = "Forbidden" });
                }
                var emailid = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
                var policy = string.Equals(dmAssignmentPolicy, "limited", StringComparison.OrdinalIgnoreCase) ? "limited" : "free";
                var cap = Math.Max(1, Math.Min(99, dmDefaultInterviewCap));
                var ok = await _irfService.UpdateDmIvPolicy(irfId, dmIvLockNew, policy, cap, emailid);
                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false });
            }
        }

        [HttpPost]
        public async Task<JsonResult> UpdateInterviewerCapacity(int transInterviewerId, int interviewCapacity)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null)
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }
                var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
                if (profileName == "Client" || profileName == "Interviewer" || profileName == "Public")
                {
                    return Json(new { success = false, message = "Forbidden" });
                }
                var emailid = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
                var cap = Math.Max(0, Math.Min(99, interviewCapacity));
                var ok = await _irfService.UpdateTransInterviewerCapacity(transInterviewerId, cap, emailid);
                return Json(new
                {
                    success = ok,
                    interviewCapacity = cap,
                    capConfigured = true,
                    capFromDefault = false,
                    message = ok ? null : "Could not update interview limit."
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false });
            }
        }

        [HttpPost]
        public async Task<JsonResult> UpdateInterviewerPerSuccessRate(int transInterviewerId, int perSuccessRate)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null)
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }
                var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
                if (profileName == "Client" || profileName == "Interviewer" || profileName == "Public")
                {
                    return Json(new { success = false, message = "Forbidden" });
                }

                return Json(new
                {
                    success = false,
                    message = "Per-interview rate cannot be changed from DM Approval."
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Per-interview rate cannot be changed from DM Approval." });
            }
        }

        [HttpPost]
        public async Task<JsonResult> ApplyDefaultInterviewCap(int irfId, int defaultCap)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null)
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }
                var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
                if (profileName == "Client" || profileName == "Interviewer" || profileName == "Public")
                {
                    return Json(new { success = false, message = "Forbidden" });
                }
                var emailid = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
                var cap = Math.Max(1, Math.Min(99, defaultCap));
                if (irfId <= 0)
                {
                    return Json(new { success = false, message = "Invalid skill channel id." });
                }
                var ok = await _irfService.ApplyDefaultInterviewCapToIrf(irfId, cap, emailid);
                if (!ok)
                {
                    return Json(new { success = false, message = "Could not update interviewer caps. Check that TransInterviewer.InterviewCapacity exists on this database." });
                }
                return Json(new { success = true, defaultCap = cap });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Server error while applying cap." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveSurcharge(int userId, int surcharge, int irfId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SupportAdmin")
                    {
                        var panel = await _irfRepository.GetTransInterviewer(irfId);
                        var ti = panel?.FirstOrDefault(x => x.UserId == userId);
                        if (ti == null)
                        {
                            return Json(new { success = false, message = "Interviewer not found on this channel." });
                        }

                        if (!DmIvSurchargeHelper.TryValidate(surcharge, out var validationError))
                        {
                            return Json(new { success = false, message = validationError });
                        }

                        var result = await _irfService.UpdateInterviewerSurcharge(userId, surcharge, irfId, emailid);
                        return Json(new { success = result, surcharge });
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
                return Json(new { success = false, message = "Failed to save surcharge." });

            }
        }
        public async Task<IActionResult> CreateTemplateFromMAG(int assessmentGroupId, string assessmentGroupName)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SupportAdmin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement")
                    {

                        var result = await _clonableSkillChannelTemplateService.CreateClonableSkillChannelFromMAG(assessmentGroupId, assessmentGroupName, emailid);
                        if (result)
                        {
                            return Json($"Successfully created a template named : {assessmentGroupName} Template");
                        }
                        else
                        {
                            return Json($"Failed to create a template for the assessment group : {assessmentGroupName}");
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
        [HttpPost]
        public async Task<JsonResult> UpdateCandidateChecks(int irfid, bool CandidateCheckvalue, string CandidateCheckName)
        {
            var result = await _irfService.UpdateCandidateChecks(irfid, CandidateCheckvalue, CandidateCheckName);
            return Json("Successfully");
        }
        [HttpPost]
        public async Task<JsonResult> SaveNewPriority(int irfid, int PriorityLevel)
        {
            var result = await _irfService.UpdateNewPriority(irfid, PriorityLevel);
            return Json("Successfully");
        }

        [HttpPost]
        public async Task<IActionResult> SendMagApplicationLinks(int agId, string agName)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SupportAdmin" || profileName == "Finance" || profileName == "SeniorHR" || profileName == "SeniorInterviewerManagement" || profileName == "InterviewerManagement" || profileName == "HR")
                    {

                        var interviewers = await _irfRepository.GetAssessmentGroupInterviewerMembers(agId, true, false); //item.id here is the id of the assessment group and not a mag table primary id

                        if (interviewers != null && interviewers.Count() > 0)
                        {
                            //create mag link
                            var encryptedId = await _encryptionHelper.EncryptStringValue(agId.ToString());
                            var magLink = "https://portal.risebird.io/Interviewer/MAGApply?id=" + HttpUtility.UrlEncode(encryptedId);

                            //send the mag link to all the interviewers, will have to create a template
                            //foreach (var interviewer in interviewers)
                            //{
                            //    await _notificationHelper.SendMAGApplicationLinkToInterviewers(interviewer.EmailId, interviewer.uniqueid, magLink, agId);

                            //}
                            await Parallel.ForEachAsync(interviewers, async (interviewer, cancellationToken) =>
                            {
                                _ = await _notificationHelper.SendMAGApplicationLinkToInterviewers(interviewer.EmailId, interviewer.uniqueid, magLink, agId);

                            });
                            await _irfService.UpdateMagLinkEmailTriggeredTime(agId, emailid);
                            return Json("Successful.");
                        }
                        else
                        {
                            return Json("No applicable interviewers found!");
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
        public async Task<IActionResult> IRFDetails()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                ViewBag.profile = profileName;
                ViewBag.PageStatus = "Main";

                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");

                var irfList = new List<IRF>();

                if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter" || profileName == "CuratedUser" || profileName == "Finance" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                {
                    if (HttpContext.Session.GetString("IRFDetailsFilters") == null)
                    {
                        irfList = (await _irfService.GetAllIRFDetailsIRF(DateTime.Now.AddDays(-30), DateTime.Now.AddDays(7), profileName, emailid))?.ToList();
                    }
                    else
                    {
                        var filter = JsonConvert.DeserializeObject<SkillChannelDetailsFilter>(HttpContext.Session.GetString("IRFDetailsFilters"));
                        if (filter.StartDate != null && filter.EndDate != null)
                        {
                            irfList = (await _irfService.GetAllIRFDetailsIRF(filter.StartDate.Value, filter.EndDate.Value, profileName, emailid))?.ToList();
                        }
                        else
                        {
                            irfList = (await _irfService.GetAllIRFDetailsIRF(DateTime.Now.AddDays(-30), DateTime.Now.AddDays(7), profileName, emailid))?.ToList();
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
        public async Task<IActionResult> IRFDetails(SkillChannelDetailsFilter filter)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                if (filter != null)
                {
                    HttpContext.Session.SetString("IRFDetailsFilters", JsonConvert.SerializeObject(filter));
                }
                return RedirectToAction("IRFDetails");
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResetIRFDetailsFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("IRFDetailsFilters");
                    return RedirectToAction("IRFDetails");
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
        public async Task<JsonResult> FetchIRFInterviewerBillingCycle(int irfId)
        {
            var billingCycle = await _irfService.FetchIRFInterviewerBillingCycle(irfId);
            return Json(billingCycle);
        }
        [HttpPost]
        public async Task<JsonResult> SaveIRFInterviewerBillingCycle(int irfId, int billingCycle)
        {
            if (HttpContext.Session.GetString("email") != null)
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                await _irfService.UpdateIRFInterviewerBillingCycle(irfId, billingCycle, emailid);
                return Json("Billing Cycle saved: " + billingCycle + " Month(s) for IRF ID: " + irfId);
            }
            else
            {
                return Json("Fail to Update Biiling Cycle, Try again to update the billing cycle.");
            }
        }

        private sealed class RubricQuestionGenerationResult
        {
            public object Response { get; set; }
            public bool RetryAttempted { get; set; }
        }

        private async Task<RubricQuestionGenerationResult> RunRubricQuestionGenerationAsync(
            string assistantId, string apiKey, string rubricInput, int irfId, string emailid,
            HashSet<string> validSkillNames, bool isRetry = false)
        {
            var raw = await FetchAssistantTextResponseAsync(assistantId, apiKey, rubricInput);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new RubricQuestionGenerationResult
                {
                    Response = new { success = false, message = "AI returned an empty response." }
                };
            }

            var parseResult = await ParseAndSaveRubricQuestionsAsync(raw, irfId, emailid, validSkillNames);
            if (!parseResult.Parsed)
            {
                return new RubricQuestionGenerationResult { Response = parseResult.ErrorResponse };
            }

            var total = parseResult.TotalCount;
            var rejected = parseResult.Mismatches.Count;
            if (!isRetry && total > 0 && rejected > total / 2)
            {
                var retryInput = rubricInput + "\n\nREMINDER: Every question's skill_or_competency MUST be copied exactly from the Score Rubric skill list above.";
                var retry = await RunRubricQuestionGenerationAsync(assistantId, apiKey, retryInput, irfId, emailid, validSkillNames, isRetry: true);
                retry.RetryAttempted = true;
                return retry;
            }

            foreach (var mismatch in parseResult.Mismatches)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Info,
                    new Exception($"GenerateQuestionsFromRubric IRF {irfId}: rejected skill '{mismatch.Returned}' (valid: {string.Join(", ", validSkillNames)})."));
            }

            object response;
            if (parseResult.Saved.Count == 0)
            {
                response = new
                {
                    success = false,
                    message = "All generated questions were rejected because skill names did not match the rubric.",
                    rejectedSkills = parseResult.Mismatches.Select(m => m.Returned).Distinct().ToList(),
                    validSkills = validSkillNames.ToList()
                };
            }
            else
            {
                response = new
                {
                    success = true,
                    questions = parseResult.Saved,
                    count = parseResult.Saved.Count,
                    rejectedCount = rejected,
                    warning = rejected > 0
                        ? $"{rejected} question(s) were rejected due to skill-name mismatches."
                        : null
                };
            }

            return new RubricQuestionGenerationResult { Response = response, RetryAttempted = isRetry };
        }

        private async Task<string> FetchAssistantTextResponseAsync(string assistantId, string apiKey, string rubricInput)
        {
            var useResponsesApi = _configuration.GetValue("OpenAI:UseResponsesApi", false);
            if (useResponsesApi)
            {
                var result = await _assistantServiceHelper.GenerateRubricQuestions(rubricInput);
                return result?.Data?.FirstOrDefault()?.Content?.FirstOrDefault()?.Text?.Value;
            }

            using (var http = _httpClientFactory.CreateClient())
            {
                http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                http.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");
                http.Timeout = TimeSpan.FromSeconds(120);

                var threadResp = await http.PostAsync("https://api.openai.com/v1/threads",
                    new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
                var threadJson = Newtonsoft.Json.Linq.JObject.Parse(await threadResp.Content.ReadAsStringAsync());
                var threadId = threadJson["id"]?.ToString();
                if (string.IsNullOrEmpty(threadId)) return null;

                var msgPayload = Newtonsoft.Json.JsonConvert.SerializeObject(new { role = "user", content = rubricInput });
                var msgResp = await http.PostAsync($"https://api.openai.com/v1/threads/{threadId}/messages",
                    new StringContent(msgPayload, System.Text.Encoding.UTF8, "application/json"));
                if (!msgResp.IsSuccessStatusCode) return null;

                var runPayload = Newtonsoft.Json.JsonConvert.SerializeObject(new { assistant_id = assistantId });
                var runResp = await http.PostAsync($"https://api.openai.com/v1/threads/{threadId}/runs",
                    new StringContent(runPayload, System.Text.Encoding.UTF8, "application/json"));
                var runJson = Newtonsoft.Json.Linq.JObject.Parse(await runResp.Content.ReadAsStringAsync());
                var runId = runJson["id"]?.ToString();
                var runStatus = runJson["status"]?.ToString();
                if (string.IsNullOrEmpty(runId)) return null;

                var pollStart = DateTime.UtcNow;
                while (runStatus == "queued" || runStatus == "in_progress")
                {
                    if ((DateTime.UtcNow - pollStart).TotalSeconds > 90) return null;
                    await System.Threading.Tasks.Task.Delay(3000);
                    var statusResp = await http.GetAsync($"https://api.openai.com/v1/threads/{threadId}/runs/{runId}");
                    var statusJson = Newtonsoft.Json.Linq.JObject.Parse(await statusResp.Content.ReadAsStringAsync());
                    runStatus = statusJson["status"]?.ToString() ?? "unknown";
                }

                if (runStatus != "completed") return null;

                var msgsResp = await http.GetAsync($"https://api.openai.com/v1/threads/{threadId}/messages");
                var msgsJson = Newtonsoft.Json.Linq.JObject.Parse(await msgsResp.Content.ReadAsStringAsync());
                var dataArr = msgsJson["data"] as Newtonsoft.Json.Linq.JArray;
                if (dataArr == null) return null;

                foreach (var msg in dataArr)
                {
                    if (msg["role"]?.ToString() != "assistant") continue;
                    var contentArr = msg["content"] as Newtonsoft.Json.Linq.JArray;
                    if (contentArr == null) continue;
                    foreach (var block in contentArr)
                    {
                        if (block["type"]?.ToString() == "text")
                        {
                            var raw = block["text"]?["value"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(raw)) return raw;
                        }
                    }
                }
            }
            return null;
        }

        private sealed class RubricQuestionParseResult
        {
            public bool Parsed { get; set; }
            public object ErrorResponse { get; set; }
            public List<object> Saved { get; set; } = new List<object>();
            public List<(string Returned, string Question)> Mismatches { get; set; } = new List<(string, string)>();
            public int TotalCount { get; set; }
        }

        private async Task<RubricQuestionParseResult> ParseAndSaveRubricQuestionsAsync(
            string raw, int irfId, string emailid, HashSet<string> validSkillNames)
        {
            var result = new RubricQuestionParseResult();
            var cleaned = raw.Trim();
            if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                cleaned = cleaned.Substring(7).TrimStart();
            else if (cleaned.StartsWith("```"))
                cleaned = cleaned.Substring(3).TrimStart();
            if (cleaned.EndsWith("```"))
                cleaned = cleaned.Substring(0, cleaned.Length - 3).TrimEnd();

            var jsonStart = cleaned.IndexOf('{');
            var jsonEnd = cleaned.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
                cleaned = cleaned.Substring(jsonStart, jsonEnd + 1 - jsonStart);

            Newtonsoft.Json.Linq.JObject jsonObj;
            try { jsonObj = Newtonsoft.Json.Linq.JObject.Parse(cleaned); }
            catch (Newtonsoft.Json.JsonException parseEx)
            {
                var preview = cleaned.Length > 300 ? cleaned.Substring(0, 300) + "..." : cleaned;
                result.Parsed = false;
                result.ErrorResponse = new { success = false, message = "AI returned a non-JSON response: " + preview, parseError = parseEx.Message };
                return result;
            }

            var bank = (jsonObj["question_answer_score_bank"]
                ?? jsonObj["questions"]
                ?? jsonObj["question_bank"]
                ?? jsonObj["sample_questions"]) as Newtonsoft.Json.Linq.JArray;

            if (bank == null)
            {
                var firstArr = jsonObj.Properties()
                    .FirstOrDefault(p => p.Value is Newtonsoft.Json.Linq.JArray arr && arr.Count > 0 && arr[0] is Newtonsoft.Json.Linq.JObject);
                bank = firstArr?.Value as Newtonsoft.Json.Linq.JArray;
            }

            if (bank == null || !bank.Any())
            {
                result.Parsed = false;
                result.ErrorResponse = new { success = false, message = "AI did not return any questions in the expected format." };
                return result;
            }

            result.Parsed = true;
            result.TotalCount = bank.Count;

            foreach (var q in bank)
            {
                var skill = q["skill_or_competency"]?.ToString()?.Trim();
                if (string.IsNullOrEmpty(skill))
                    skill = q["rubric_parameter"]?.ToString()?.Trim();

                if (!DmIvQuestionRubricHelper.TryMatchSkill(skill, validSkillNames, out var matchedSkill))
                {
                    result.Mismatches.Add((skill ?? "(empty)", (q["interviewer_question"] ?? q["question"])?.ToString() ?? ""));
                    continue;
                }

                var question = (q["interviewer_question"] ?? q["question"] ?? q["question_text"])?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(question)) continue;

                var strongAnswer = q["expected_candidate_answer"]?["strong_answer"]?.ToString()
                    ?? q["expected_answer"]?.ToString()
                    ?? q["strong_answer"]?.ToString()
                    ?? q["answer"]?.ToString() ?? "";
                var aiGist = (q["question_gist"] ?? q["questionGist"] ?? q["spoken_question"] ?? q["avatar_script"])?.ToString();
                var questionGist = DmIvQuestionRubricHelper.BuildSpokenQuestionGist(question, aiGist);
                var payload = q.ToString(Newtonsoft.Json.Formatting.None);

                var newId = await _irfService.AddAIGeneratedSampleQuestion(irfId, matchedSkill, question, strongAnswer, payload, emailid, questionGist);
                if (newId > 0)
                    result.Saved.Add(new { id = newId, skillName = matchedSkill, comment = question, expectedAnswer = strongAnswer, questionGist, isGeneratedByAi = true, isAIGenerated = true });
            }

            return result;
        }

        #region AI interview question sets

        /// <summary>
        /// Question sets are confidential: they must never reach a candidate or a client. Operational
        /// profiles and the channel's own SMEs may see them; everyone else is refused before any set
        /// data is read. usp_GetAIQuestionSetsByIRF enforces the preset check independently, so a
        /// non-preset channel returns nothing regardless of who is asking.
        /// </summary>
        private async Task<bool> AuthorizeAIQuestionSetAccessAsync(int irfId)
        {
            if (irfId <= 0) return false;
            var profile = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
            if (DeliveryHeadInstructionViewMode.IsClientRoleProfile(profile)) return false;
            return await AuthorizeDhiMutatingAccessAsync(irfId);
        }

        /// <summary>
        /// Deals the channel's questions into balanced sets. The grouping is proposed by a stored
        /// prompt and then re-validated twice -- here in C# and again inside the save transaction --
        /// before anything is written.
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> CreateAIQuestionSets(int irfid)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null)
                    return Json(new { success = false, message = "Unauthorized" });
                if (!await AuthorizeAIQuestionSetAccessAsync(irfid))
                    return Json(new { success = false, message = "Forbidden" });

                var emailid = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");

                var result = await _irfService.CreateAIQuestionSets(irfid, emailid);
                if (result == null)
                    return Json(new { success = false, message = "The question sets could not be created. Please try again." });

                return Json(new { success = result.Success, message = result.Message, setCount = result.SetCount });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "The question sets could not be created. Please try again." });
            }
        }

        /// <summary>
        /// Switches this channel's question sets on or off. Same authorization as the rest of the
        /// section: a client or a candidate must never learn whether a channel deals papers, let alone
        /// change it.
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> SetAIQuestionSetsEnabled(int irfid, bool enabled)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null)
                    return Json(new { success = false, message = "Unauthorized" });
                if (!await AuthorizeAIQuestionSetAccessAsync(irfid))
                    return Json(new { success = false, message = "Forbidden" });

                var emailid = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");

                var result = await _irfService.SetAIQuestionSetsEnabled(irfid, enabled, emailid);
                if (result == null)
                    return Json(new { success = false, message = "This setting could not be changed. Please try again." });

                return Json(new { success = result.Success, enabled = result.Enabled, message = result.Message });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "This setting could not be changed. Please try again." });
            }
        }

        [HttpPost]
        public async Task<JsonResult> GetAIQuestionSetsByIRF(int irfid)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null)
                    return Json(new { success = false, message = "Unauthorized" });
                if (!await AuthorizeAIQuestionSetAccessAsync(irfid))
                    return Json(new { success = false, message = "Forbidden" });

                var sets = await _irfService.GetAIQuestionSetsByIRF(irfid) ?? new List<AIQuestionSetVM>();

                return Json(new
                {
                    success = true,
                    sets = sets.Select(s => new
                    {
                        setId = s.SetId,
                        setName = s.SetName,
                        totalMinutes = s.TotalMinutes,
                        totalPoints = s.TotalPoints,
                        promptId = s.PromptId,
                        promptVersion = s.PromptVersion,
                        assignedInterviews = s.AssignedInterviews,
                        // Already in ask order. Do not re-sort.
                        questions = s.Items.Select(i => new
                        {
                            itemId = i.ItemId,
                            questionId = i.QuestionId,
                            skillName = i.SkillName,
                            questionText = i.QuestionText
                        })
                    })
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "The question sets could not be loaded. Please try again." });
            }
        }

        #endregion
    }
}