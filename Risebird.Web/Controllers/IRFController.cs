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
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using Risebird.Web.Controllers.AzureMediaservice;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class IRFController : Controller
    {
        private readonly IIRFRepository _irfRepository;
        private readonly IUserService _userService;
        private readonly IIRFService _irfService;
        private readonly IEvaluatorService _evaluatorService;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IMeetingService _meetingService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private ISkillService _skillService;
        private static List<Candidate> _modelDetails;
        private static List<MeetingVM> _modelDetail;
        private static List<MeetingVM> _modelDetle;
        private IConfiguration _configuration;
        private IInterviewerSkillsService _iInterviewerSkillsService;
        private readonly ICandidateService _candidateService;
        private IInterviewerService _interviewerService;
        private readonly INotificationHelper _notificationHelper;
        public IRFController(IIRFRepository iRFRepository, IApplicationErrorLogger applicationErrorLogger, IIRFService irfService, IMeetingService meetingService, IAzureStorageHelper azureStorageHelper, IEvaluatorService evaluatorService, IUserService userService, ISkillService skillService, IConfiguration configuration, ICandidateService candidateService, IInterviewerSkillsService iInterviewerSkillsService, IInterviewerService interviewerService, INotificationHelper notificationHelper)
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
        }


        [Authorize]
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter" || profileName == "CuratedUser" || profileName == "Finance")
                {
                    var model = new IRFViewModel();
                    var irfList = _irfService.GetAllIRFs(emailid, profileName);
                    irfList.ToList().ForEach(x => { x.ArchiveStatus = ((ArchiveStatus)Convert.ToInt16(x.IsDeleted)).GetDescription(); x.SubscriptionStatus = ((SubscriptionStatus)Convert.ToInt16(x.IsSubscribed)).GetDescription(); });
                    model.IRFList = irfList.Where(x => x.EndDate.Date > DateTime.Now).OrderByDescending(y => y.EndDate);
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
                        else
                        {
                            item.ImageUrl = "/img/Cards_Png/01_Green-White.png";
                            item.IRFStatus = "Active";
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
                    if (HttpContext.Session.GetString("IRFfilters") != null)
                    {
                        var filter = JsonConvert.DeserializeObject<IRFFilter>(HttpContext.Session.GetString("IRFfilters"));
                        if (filter.StartDate != null && filter.EndDate != null)
                        {
                            model.IRFList = irfList.Where(t => t.StartDate >= filter.StartDate && t.EndDate <= filter.EndDate).ToList();
                        }
                        else
                        {
                            model.IRFList = irfList.Where(x => x.EndDate >= DateTime.Now);
                        }
                        filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = irfList.Select(x => x.JDTiltle.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                        filter.RFID = filter.RFID == null ? filter.RFID = model.IRFList.Select(x => x.IRFId).ToArray() : filter.RFID = filter.RFID;
                        filter.ArchiveStatus = filter.ArchiveStatus ?? (filter.ArchiveStatus = model.IRFList.Select(x => x.ArchiveStatus).ToArray());
                        filter.SubscriptionStatus = filter.SubscriptionStatus ?? (filter.SubscriptionStatus = model.IRFList.Select(x => x.SubscriptionStatus).ToArray());
                        filter.ClientName = filter.ClientName == null ? filter.ClientName = model.IRFList.Select(x => x.ClientCompName).ToArray() : filter.ClientName = filter.ClientName;
                        filter.EndClientName = filter.EndClientName ?? model.IRFList.Select(x => x.ClientName).ToArray();
                        filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = model.IRFList.Select(x => x.JDTiltle).ToArray() : filter.PrimarySkill = filter.PrimarySkill;

                        model.IRFList = model.IRFList.Where(c => filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                        && filter.RFID.Contains(c.IRFId)
                        && (filter.ArchiveStatus.Count() > 0 ? filter.ArchiveStatus.Contains(c.ArchiveStatus) : c.ArchiveStatus == null)
                        && (filter.SubscriptionStatus.Count() > 0 ? filter.SubscriptionStatus.Contains(c.SubscriptionStatus) : c.SubscriptionStatus == null)
                        && (filter.ClientName.Count() > 0 ? filter.ClientName.Contains(c.ClientCompName) : c.ClientCompName == null)
                        && (filter.EndClientName.Count() > 0 ? filter.EndClientName.Contains(c.ClientName) : c.ClientName == null)
                        && (filter.PrimarySkill.Count() > 0 ? filter.PrimarySkill.Contains(c.JDTiltle) : c.JDTiltle == null)).ToList();

                        if (model.IRFList.Count() == 0 && filter.StartDate != null && filter.EndDate != null)
                        {
                            model.IRFList = irfList.Where(t => t.StartDate.Date >= filter.StartDate && t.EndDate.Date <= filter.EndDate).ToList();
                        }
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
                        model.IRFList = model.IRFList.OrderByDescending(x => x.EndDate).ToList();
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

        [Authorize]
        [HttpPost]
        public IActionResult Index(IRFFilter filter)
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
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter" || profileName == "CuratedUser" || profileName == "Finance")
                {

                    var model = new IRFViewModel();
                    var irfList = _irfService.GetAllIRFs(emailid, profileName);
                    irfList.ToList().ForEach(x => { x.ArchiveStatus = ((ArchiveStatus)Convert.ToInt16(x.IsDeleted)).GetDescription(); x.SubscriptionStatus = ((SubscriptionStatus)Convert.ToInt16(x.IsSubscribed)).GetDescription(); });
                    if (filter.StartDate != null && filter.EndDate != null)
                    {
                        irfList = irfList.Where(t => t.StartDate.Date >= filter.StartDate && t.EndDate.Date <= filter.EndDate).ToList();
                    }
                    else
                    {
                        irfList = irfList.Where(x => x.EndDate.Date > DateTime.Now);
                    }
                    if (profileName != null && profileName.Contains("Client"))
                    {
                        model.IsClientUser = true;
                    }
                    filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = irfList.Select(x => x.JDTiltle.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                    filter.RFID = filter.RFID == null ? filter.RFID = irfList.Select(x => x.IRFId).ToArray() : filter.RFID = filter.RFID;
                    filter.ArchiveStatus = filter.ArchiveStatus ?? (filter.ArchiveStatus = irfList.Select(x => x.ArchiveStatus).ToArray());
                    filter.SubscriptionStatus = filter.SubscriptionStatus ?? (filter.SubscriptionStatus = irfList.Select(x => x.SubscriptionStatus).ToArray());
                    filter.ClientName = filter.ClientName == null ? filter.ClientName = irfList.Select(x => x.ClientCompName).ToArray() : filter.ClientName = filter.ClientName;
                    filter.EndClientName = filter.EndClientName ?? irfList.Select(x => x.ClientName).ToArray();
                    filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = model.IRFList.Select(x => x.JDTiltle).ToArray() : filter.PrimarySkill = filter.PrimarySkill;

                    irfList = irfList.Where(c => filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                    && filter.RFID.Contains(c.IRFId)
                    && (filter.ArchiveStatus.Count() > 0 ? filter.ArchiveStatus.Contains(c.ArchiveStatus) : c.ArchiveStatus == null)
                    && (filter.SubscriptionStatus.Count() > 0 ? filter.SubscriptionStatus.Contains(c.SubscriptionStatus) : c.SubscriptionStatus == null)
                    && (filter.ClientName.Count() > 0 ? filter.ClientName.Contains(c.ClientCompName) : c.ClientCompName == null)
                    && (filter.EndClientName.Count() > 0 ? filter.EndClientName.Contains(c.ClientName) : c.ClientName == null)
                    && (filter.PrimarySkill.Count() > 0 ? filter.PrimarySkill.Contains(c.JDTiltle) : c.JDTiltle == null)).ToList();


                    if (irfList.Count() == 0 && filter.StartDate != null && filter.EndDate != null)
                    {
                        irfList = _irfService.GetAllIRFs(emailid, profileName).Where(x => x.StartDate.Date >= filter.StartDate && x.EndDate.Date <= filter.EndDate);
                        irfList.ToList().ForEach(x => { x.ArchiveStatus = ((ArchiveStatus)Convert.ToInt16(x.IsDeleted)).GetDescription(); x.SubscriptionStatus = ((SubscriptionStatus)Convert.ToInt16(x.IsSubscribed)).GetDescription(); });

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
                    model.IRFList = irfList.OrderByDescending(x => x.EndDate).ToList();
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
        public IActionResult Add(string IntrviewerId)
        {
            if (HttpContext.Session.GetString("email") != null)
            {
                var model = new IRFVm();
                if (!string.IsNullOrEmpty(IntrviewerId))
                {
                    model.InterviewerId = IntrviewerId;

                }
                SetAddIRFProperties(model);

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
                    if (profileName == "Admin" || profileName == "HR")
                    {

                        string uploadedFileURL = string.Empty;
                        if (irfDetails.JDFile == null && (irfDetails.JDLink == null))
                        {
                            TempData["SuccessErrorMessage"] = "Adding Skills Channel has Failed. JDLink not found.";
                            return RedirectToAction("Index");
                        }
                        if (irfDetails.JDFile != null)
                        {
                            await _azureStorageHelper.InsertBlob(irfDetails.JDFile.FileName, "resumes", irfDetails.JDFile);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                            var blob = containerRef.GetBlockBlobReference(irfDetails.JDFile.FileName);
                            uploadedFileURL = blob.Uri.ToString();
                            irfDetails.JDLink = uploadedFileURL;
                        }
                        if (irfDetails.CompanyDetailFile != null)
                        {
                            await _azureStorageHelper.InsertBlob(irfDetails.CompanyDetailFile.FileName, "resumes", irfDetails.CompanyDetailFile);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                            var blob = containerRef.GetBlockBlobReference(irfDetails.CompanyDetailFile.FileName);
                            uploadedFileURL = blob.Uri.ToString();
                            irfDetails.CompanyDetails = uploadedFileURL;
                        }
                        var domain = Request.Scheme + "://" + Request.Host.Value;
                        if (await _irfService.AddIRFDetails(irfDetails, emailId, domain))
                        {
                            TempData["SuccessErrorMessage"] = "Skills Channel is added Successfully";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Adding Skills Channel has Failed";
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


        private void SetAddIRFProperties(IRFVm model)
        {
            var skills = _irfRepository.GetSkills();
            var AssessmentsGroups = _interviewerService.FetchAssessmentGroups();
            var interviewers = _irfService.GetInterviewersForIRF(AssessmentsGroups.FirstOrDefault()?.id.ToString());
            model.JoinabilityTypeList = new SelectList(_irfService.GetJoinabilityMatrix(), "id", "Name");
            model.SkillGeneralTypeList = new SelectList(_irfService.GetGeneralSkillMatrix(), "id", "Name");
            model.SelectabilityTypeList = new SelectList(_irfService.GetSelectabilityMatrix(), "id", "Name");
            var priorityTypes = from PriorityLevel s in Enum.GetValues(typeof(PriorityLevel))
                                select new { ID = (int)s, Name = s.GetDescription() };
            model.PriorityLevelList = new SelectList(priorityTypes, "ID", "Name");
            var clients = _irfRepository.GetClients();
            ViewBag.Clients = new SelectList(clients, "id", "Name");
            ViewBag.Level = new SelectList(_irfRepository.GetLevels(), "id", "Name");
            ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
            var Skillset = skills;
            if (Skillset != null)
            {
                Skillset = Skillset.OrderBy(x => x.Name.Trim()).ToList();
            }
            ViewBag.Skill = new SelectList(Skillset, "id", "Name");
            ViewBag.ClientUser = new SelectList(_irfRepository.GetCientUsers(), "id", "EmailId");
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
            ViewBag.JobLevelList = new SelectList(jobLevels, "ID", "Name"); ;

            var companyTypes = from CompanyType s in Enum.GetValues(typeof(CompanyType))
                               select new { ID = (int)s, Name = s.GetDescription() };
            model.CompanyTypeList = new SelectList(companyTypes, "ID", "Name");
            if (!String.IsNullOrEmpty(model.InterviewerId))
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
            return;
        }

        [Authorize]
        public string GetInterviewerSkills(int id)
        {
            var name = _irfService.GetInterviewerSkills(id);
            return name;
        }
        [Authorize]
        public string GetInterviewerSkillsFromProfile(int id)
        {
            var name = _iInterviewerSkillsService.GetInterviewerSkills(id);
            return name;
        }

        [Authorize]
        public string GetRecruiterDomain(string userId)
        {
            return _irfService.GetRecruiterDomain(Convert.ToInt16(userId));

        }
        [Authorize]
        public SelectList GetInterviewers(string assessmentgroupid)
        {

            var list = new List<SelectListItem>();
            var interviewers = _irfService.GetInterviewers(assessmentgroupid);

            if (interviewers != null && interviewers.Count() > 0)
            {
                return new SelectList(interviewers, "UserId", "EmailId");
            }
            else
                list.Add(new SelectListItem { Text = "No Interviewer", Value = "" });
            //clientUsers.ToList().Add(new Users { id = 0, FirstName = "No Client User" });
            return new SelectList(list, "Value", "Text");
        }

        [Authorize]
        public string GetQCSkills(string id)
        {
            return _irfService.GetQCSkills(Convert.ToInt16(id));
        }

        //GetClientUserRole
        [Authorize]
        public string GetClientUserRole(string id)
        {
            return _irfService.GetClientUserRole(Convert.ToInt16(id));
        }

        [Authorize]
        public string GetQuestionSkills(string id)
        {
            return _irfService.GetQuestionSkills(id);
        }


        [Authorize]
        public async Task<string> GetTechSkillofSkillChannelByQuestion(string question, int irfId)
        {
            return await _irfService.GetTechSkillofSkillChannelByQuestion(question, irfId);
        }

        [Authorize]
        public bool CheckUniqueness(string value)
        {
            return _irfService.CheckUniqueness(value);
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
        //                    model.Skills = new SelectList(_irfService.GetTechSkills(Convert.ToInt16(model.IRFId)), "id", "Name");
        //                    return View(model);
        //                    //return RedirectToAction("Index");

        //                }
        //                else
        //                {
        //                    //TempData["SuccessErrorMessage"] = "Questions were not saved.";
        //                    model.Message = "Questions were not saved.";
        //                    model.Skills = new SelectList(_irfService.GetTechSkills(Convert.ToInt16(model.IRFId)), "id", "Name");
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
        public IActionResult SampleQuestions(int id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Interviewer" /*|| profileName == "Quality Controller"*/)
                {
                    TechSkillComments model = new TechSkillComments();
                    var result = _irfService.GetTechSkillComments(id);
                    //if (result.Count() > 0)
                    model.SkillCollection = result;
                    var skills = _irfService.GetTechSkills(id);
                    ViewBag.Skills = new SelectList(skills, "id", "Name");
                    model.SkillsOptionsData = JsonConvert.SerializeObject(skills);
                    model.IRFId = id;
                    model.Profile = profileName;
                    model.UserId = _irfService.GetUserId(emailid);
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
        [ValidateAntiForgeryToken]
        public IActionResult SampleQuestions(TechSkillComments model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Interviewer" /*|| profileName == "Quality Controller"*/)
                    {
                        if (ModelState.IsValid)
                        {

                            var questionsCollection = model.SkillCollection.Select(x => (x.Comment, x.Skill, x.UserId));
                            if (questionsCollection.Count() != questionsCollection.Distinct().Count())
                            {
                                //duplicates exist
                                ModelState.AddModelError("", "The skill,question combination added by you should contain unique entries.");
                                var skills = _irfService.GetTechSkills(model.IRFId);
                                ViewBag.Skills = new SelectList(skills, "id", "Name");
                                model.SkillsOptionsData = JsonConvert.SerializeObject(skills);
                                model.Profile = profileName;
                                return View(model);
                            }

                            if (_irfService.AddSkillComments(model))
                            {
                                TempData["SuccessErrorMessageQuestions"] = "Questions saved Successfully";
                                return RedirectToAction("SampleQuestions", new { id = model.IRFId });
                            }
                            else
                            {
                                TempData["SuccessErrorMessageQuestions"] = "Questions were not saved.";
                                return RedirectToAction("SampleQuestions", new { id = model.IRFId });
                            }
                        }
                        else
                        {
                            var skills = _irfService.GetTechSkills(model.IRFId);
                            ViewBag.Skills = new SelectList(skills, "id", "Name");
                            model.SkillsOptionsData = JsonConvert.SerializeObject(skills);

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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageQuestions"] = "Questions were not saved";
                return RedirectToAction("SampleQuestions", new { id = model.IRFId });
            }
        }


        [Authorize]
        public async Task<IActionResult> ArchiveIRF(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
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
        public async Task<IActionResult> UnArchiveIRF(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        var selectedMeeting = new List<MeetingVM>();
                        var meetings = (await _irfService.GetIRFInterviewMeetings(Convert.ToInt16(id), profileName, emailid)).Where(x => x.QCInterview > 0 && x.QCStatus == 1);

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
                        var list = new SelectList(new[]
{
    new { ID = "Today", Name = "Today"},
    new { ID = "Tomorrow", Name = "Tomorrow"  },
    new { ID = "Yesterday", Name = "Yesterday"  },
    new { ID = "Next Seven Days", Name = "Next Seven Days" },
    new { ID = "All", Name = "All"},
},
"ID", "Name", "All");

                        ViewData["list"] = list;
                        ViewBag.ProfileName = profileName.ToLower();
                        if (!String.IsNullOrEmpty(searchValue))
                        {
                            searchValue = searchValue.ToUpper().Trim();
                            selectedMeeting = selectedMeeting.Where(x => (!String.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == "") || (!String.IsNullOrEmpty(x.ClientHrContact) ? x.ClientHrContact.ToUpper().Contains(searchValue) : x.ClientHrContact == "") || (!String.IsNullOrEmpty(x.confirmationStatus) ? x.confirmationStatus.ToUpper().Contains(searchValue) : x.confirmationStatus == "") || (!String.IsNullOrEmpty(x.MeetingStatusStatusDesc) ? x.MeetingStatusStatusDesc.ToUpper().Contains(searchValue) : x.MeetingStatusStatusDesc == "") || (!String.IsNullOrEmpty(x.intervieweStatusStatDesc) ? x.intervieweStatusStatDesc.ToUpper().Contains(searchValue) : x.intervieweStatusStatDesc == "")).ToList();

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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {

                        var modelList = _meetingService.IRFMultipleCandidateInvites(irfId);
                        if (profileName == "Admin")
                        {
                            modelList.ForEach(x => x.IsAdmin = true);
                        }
                        modelList = modelList.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                        if (!String.IsNullOrEmpty(searchValue))
                        {
                            searchValue = searchValue.ToUpper().Trim();
                            modelList = modelList.Where(x => (!String.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == "") || (!String.IsNullOrEmpty(x.JDTiltle) ? x.JDTiltle.Split("|")[0].ToUpper().Contains(searchValue) : x.JDTiltle == "") || (!String.IsNullOrEmpty(x.confirmationStatus) ? x.confirmationStatus.ToUpper().Contains(searchValue) : x.confirmationStatus == "") || (!String.IsNullOrEmpty(x.MeetingStatusStatusDesc) ? x.MeetingStatusStatusDesc.ToUpper().Contains(searchValue) : x.MeetingStatusStatusDesc == "") || (!String.IsNullOrEmpty(x.intervieweStatusStatDesc) ? x.intervieweStatusStatDesc.ToUpper().Contains(searchValue) : x.intervieweStatusStatDesc == "")).ToList();

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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        var meetings = await _irfService.GetIRFInterviewMeetings(Convert.ToInt16(id), profileName, emailid);

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
                        var list = new SelectList(new[]
{
    new { ID = "Today", Name = "Today"},
    new { ID = "Tomorrow", Name = "Tomorrow"  },
    new { ID = "Yesterday", Name = "Yesterday"  },
    new { ID = "Next Seven Days", Name = "Next Seven Days" },
    new { ID = "All", Name = "All"},
},
"ID", "Name", "All");

                        ViewData["list"] = list;
                        meetings = meetings.OrderByDescending(x => x.interviewDate).ToList();

                        if (!String.IsNullOrEmpty(searchValue))
                        {
                            searchValue = searchValue.ToUpper().Trim();
                            meetings = meetings.Where(x => (!String.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == "") || (!String.IsNullOrEmpty(x.ClientHrContact) ? x.ClientHrContact.ToUpper().Contains(searchValue) : x.ClientHrContact == "")
                            || (!String.IsNullOrEmpty(x.confirmationStatus) ? x.confirmationStatus.ToUpper().Contains(searchValue) : x.confirmationStatus == "") || (!String.IsNullOrEmpty(x.MeetingStatusStatusDesc) ? x.MeetingStatusStatusDesc.ToUpper().Contains(searchValue) : x.MeetingStatusStatusDesc == "")
                            || (!String.IsNullOrEmpty(x.intervieweStatusStatDesc) ? x.intervieweStatusStatDesc.ToUpper().Contains(searchValue) : x.intervieweStatusStatDesc == "")).ToList();

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
        [HttpGet]
        public async Task<IActionResult> EditIRF(int id, bool isClone = false)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    var model = await _irfService.GetEditIRF(id);
                    model.IsClone = isClone;
                    SetEditIRFProperties(model);
                    //TempData["JDLink"] = model.JDLink;
                    model.OldJDLink = model.JDLink;
                    //TempData["ComapanyDetails"] = model.CompanyDetails;
                    model.OldCompanyDetails = model.CompanyDetails;
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

        [Authorize]
        [HttpPost]

        public async Task<IActionResult> EditIRF(IRFVm model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
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

                        var qualityControllerCollection = model.QualityController.Select(x => (x.UserId));
                        if (qualityControllerCollection.Count() != qualityControllerCollection.Distinct().Count())
                        {
                            //duplicates exist
                            isDuplicate = true;
                            ModelState.AddModelError("", "Please remove duplicate entries from Quality Controllers' list.");
                        }

                        if (isDuplicate)
                        {
                            model.isDuplicateEntries = isDuplicate;
                            SetEditIRFProperties(model);
                            return View("EditIRF", model);
                        }
                        if (!model.IsClone)
                        {
                            model.ResourceUrl = Request.Scheme + "://" + Request.Host.Value + Request.Path + Request.QueryString;
                        }
                        else
                        {
                            model.id = 0;
                        }
                        if ((model.JDFile != null))
                        {

                            await _azureStorageHelper.InsertBlob(model.JDFile.FileName, "resumes", model.JDFile);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                            var blob = containerRef.GetBlockBlobReference(model.JDFile.FileName);
                            string uploadedFileURL = blob.Uri.ToString();
                            if (uploadedFileURL == null)
                            {
                                TempData["SuccessErrorMessage"] = "IRF edits were not saved.";
                                return RedirectToAction("Index");

                            }

                            model.JDLink = uploadedFileURL;
                        }
                        else if (model.JDLink == null || model.JDLink == "")
                        {
                            model.JDLink = model.OldJDLink;
                        }
                        if ((model.CompanyDetailFile != null))
                        {

                            await _azureStorageHelper.InsertBlob(model.CompanyDetailFile.FileName, "resumes", model.CompanyDetailFile);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                            var blob = containerRef.GetBlockBlobReference(model.CompanyDetailFile.FileName);
                            string uploadedFileURL = blob.Uri.ToString();
                            if (uploadedFileURL == null)
                            {
                                TempData["SuccessErrorMessage"] = "IRF edits were not saved.";
                                return RedirectToAction("Index");

                            }

                            model.CompanyDetails = uploadedFileURL;
                        }
                        else if (model.CompanyDetails == null || model.CompanyDetails == "")
                        {
                            model.CompanyDetails = model.OldCompanyDetails;
                        }
                        //if (TempData["EditIRFId"] != null && (TempData["EditIRFId"].ToString() != model.IRFId))
                        //{

                        //}
                        if (model.IsClone && !string.IsNullOrEmpty(model.IRFId))
                        {
                            if (!(_irfService.CheckUniqueness(model.IRFId)))
                            {
                                TempData["SuccessErrorMessage"] = "Sorry! Skills Channel with this name already exists!";
                                return RedirectToAction("Index");
                            }
                            var domain = Request.Scheme + "://" + Request.Host.Value;
                            if (_irfService.AddIRFDetails(model, emailId, domain).Result)
                            {
                                TempData["SuccessErrorMessage"] = "Skills Channel saved Successfully!";
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
                                TempData["SuccessErrorMessage"] = "Skills Channel saved Successfully";
                                return RedirectToAction("Index");
                            }
                            else
                            {
                                TempData["SuccessErrorMessage"] = "IRF edits were not saved.";
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
                TempData["SuccessErrorMessage"] = "IRF edits were not saved";
                return RedirectToAction("Index");
            }
        }

        private void SetEditIRFProperties(IRFVm model)
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
                        item.PerSuccessRate = Convert.ToInt32(model.PerMintuteRate);
                    }
                }
            }
            var AssessmentsGroups = _interviewerService.FetchAssessmentGroups();
            var interviewers = _irfService.GetInterviewersForIRF(model.AssessmentGroupId);
            ViewBag.Clients = new SelectList(_irfRepository.GetClients(), "id", "Name");
            ViewBag.Level = new SelectList(_irfRepository.GetLevels(), "id", "Name");
            ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");

            var Skillset = _irfRepository.GetSkills();
            if (Skillset != null)
            {
                Skillset = Skillset.OrderBy(x => !String.IsNullOrEmpty(x.Name) ? x.Name.Trim() : "").ToList();
            }
            ViewBag.Skill = new SelectList(Skillset, "id", "Name");
            ViewBag.ClientUser = new SelectList(_irfService.GetClientUser(model.ClientId), "id", "EmailId");
            ViewBag.ClientRole = new SelectList(_irfRepository.GetClientRoles(), "id", "Name");
            ViewBag.Interviewers = new SelectList(interviewers, "UserId", "EmailId");
            ViewBag.Recruiters = new SelectList(_irfRepository.GetRecruiters(), "UserId", "EmailId");
            ViewBag.QualityController = new SelectList(interviewers, "UserId", "EmailId");
            ViewBag.EmergencyInterviewers = new SelectList(interviewers, "UserId", "EmailId");
            //ViewBag.RiseBirdHR = new SelectList(_irfService.GetRiseBirdHR(), "id", "EmailId");
            //ViewBag.RiseBirdHR = new SelectList(_irfService.GetAllHRs(), "id", "EmailId");
            ViewBag.ChannelOwnerOptions = new SelectList(_irfService.GetEligibleChannelOwners(), "id", "EmailId");
            ViewBag.ResumeEvaluators = new SelectList(_irfRepository.GetResumeEvaluators(), "UserId", "EmailId");
            ViewBag.HRs = new SelectList(_irfRepository.GetHRs(), "UserId", "EmailId");
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
            if (string.IsNullOrEmpty(model.Type))
            {
                model.Type = "B2B";
            }
            if (String.IsNullOrEmpty(model.AssessmentGroupId))
            {
                model.InterviewerMembers = _irfService.GetIRFInterviewerMembers(model.id).Result;
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
            return;
        }

        [Authorize]
        public IActionResult Stats(string id, string irfId)
        {
            TempData["id"] = id;
            TempData["irfId"] = irfId;
            TempData.Keep();
            ViewBag.IRF = irfId;
            return View();

        }

        [HttpGet]
        public IActionResult AddReport(int interviewId, int irfId, bool IsSelected = false)
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

                var model = _meetingService.GetInteviewReport(interviewId, irfId).Result;

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
                ViewBag.ProfileName = prof.ToLower();

                GetStatusList(model, irfId);
                ViewBag.PageStatus = "Mainurl";
                return PartialView("AddReportPartial", model);

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        if (model.AudioFile != null)
                        {
                            await _azureStorageHelper.InsertBlob(model.AudioFile.FileName, "audios", model.AudioFile);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("audios");
                            var blob = containerRef.GetBlockBlobReference(model.AudioFile.FileName);
                            string uploadedaudioFileURL = blob.Uri.ToString();
                            if (uploadedaudioFileURL == null)
                            {
                                ModelState.AddModelError(String.Empty, "Audio Upload is failed.");
                                GetStatusList(model);
                                return View("AddReport", model);
                            }
                            else
                            {
                                model.AudioLink = uploadedaudioFileURL;
                            }
                        }
                        if (model.VideoFile == null && model.PageVideoLink == null && model.VideoLink == null)
                        {
                            ModelState.AddModelError(String.Empty, "Video file/link not found.");
                            GetStatusList(model);
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
                            AzureCall _call = new AzureCall(_configuration);
                            //var resulttype = _call.CallApi(model.VideoFile, model.VideoFile.FileName, model.VideoFile.Length);
                            var resulttype = _call.CallApiUpload(model.VideoFile);
                            resulttype = resulttype.Replace("\"", "");
                            var uploadedvideoFileURL = resulttype;
                            if (uploadedvideoFileURL == null)
                            {
                                ModelState.AddModelError(String.Empty, "Video Upload is failed.");
                                GetStatusList(model);
                                return View("AddReport", model);
                            }
                            else
                            {
                                model.VideoLink = uploadedvideoFileURL;
                            }
                        }
                        else if (model.PageVideoLink != null)
                        {
                            model.VideoLink = model.PageVideoLink;
                        }
                        if (_meetingService.AddInterviewReport(model, emailid).Result)
                        {
                            TempData["SuccessErrorMessage"] = "Report saved Successfully";
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

        private void GetStatusList(InterviewReport model, int irfId = 0)
        {
            var interviewStatuses = from InterviewStatus s in Enum.GetValues(typeof(InterviewStatus))
                                    select new { ID = (int)s, Name = s.GetDescription() };
            model.InterviewStatusList = new SelectList(interviewStatuses, "ID", "Name");

            var meetingStatuses = from MeetingStatus s in Enum.GetValues(typeof(MeetingStatus))
                                  select new { ID = (int)s, Name = s.GetDescription() };

            model.MeetingStatusList = new SelectList(meetingStatuses, "ID", "Name");

            model.Level = new SelectList(_irfRepository.GetLevels(), "id", "Name");
            model.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
            var questionsLevel = from QuestionsLevel s in Enum.GetValues(typeof(QuestionsLevel))
                                 select new { ID = (int)s, Name = s.GetDescription() };
            model.QuestionsLevelList = new SelectList(questionsLevel, "ID", "Name");
            var confirmationTypes = from ConfirmationStatus s in Enum.GetValues(typeof(ConfirmationStatus))
                                    select new { ID = (int)s, Name = s.GetDescription() };
            model.ConfirmationTypeList = new SelectList(confirmationTypes, "ID", "Name");
            model.QuestionsList = new SelectList(_meetingService.GetTechSkillComments(irfId).Result, "Comment", "Comment");

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
                        var meetings = await _irfService.GetIRFInterviewMeetings(Convert.ToInt16(id), profileName, emailid);
                        var noShowCount = meetings.Where(x => x.MeetingStatus.HasValue && x.MeetingStatus.Value == 1).Count();
                        var ScheduledCount = meetings.Where(x => !x.MeetingStatus.HasValue && x.interviewDate >= DateTime.Now && x.confirmationStatus == "1").Count();
                        var DroppedCount = meetings.Where(x => x.MeetingStatus.HasValue && x.MeetingStatus.Value == 3).Count();
                        var CompletedCount = meetings.Where(x => x.MeetingStatus.HasValue && x.MeetingStatus.Value == 4).Count();
                        ViewBag.IRF = irfId;
                        ViewBag.IRFintId = id;

                        List<object> iData = new List<object>();

                        DataTable dt = new DataTable();
                        dt.Columns.Add("Meeting Status", System.Type.GetType("System.String"));
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
                        var meetings = await _irfService.GetIRFInterviewMeetings(Convert.ToInt16(id), profileName, emailid);

                        var ProxyCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 1).Count();
                        var SelectedCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 2).Count();
                        var RejectedCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 3).Count();
                        var BorderLineCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 4).Count();



                        ViewBag.IRF = irfId;
                        ViewBag.IRFintId = id;
                        List<object> iData = new List<object>();
                        //Creating sample data  
                        DataTable dt = new DataTable();
                        dt.Columns.Add("Interview Status", System.Type.GetType("System.String"));
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
                        var meetings = await _irfService.GetIRFInterviewMeetings(Convert.ToInt16(id), profileName, emailid);
                        var clientList = new List<string>();
                        var chartData = new List<ClientChartData>();
                        var clientUserList = _irfRepository.GetTransClientUser(Convert.ToInt16(id));
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
                        var meetings = await _irfService.GetIRFInterviewMeetings(Convert.ToInt16(id), profileName, emailid);
                        var clientList = new List<string>();
                        var chartData = new List<ClientChartFeedbackData>();
                        var clientUserList = _irfRepository.GetTransClientUser(Convert.ToInt16(id));
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
                        var meetings = await _irfService.GetIRFInterviewMeetings(Convert.ToInt16(id), profileName, emailid);
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {

                        var chartData = await _irfService.GetFunnelData(Convert.ToInt16(irfId), irfName);
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter")
                    {
                        ViewBag.IRFName = irfName;
                        ViewBag.IRFId = id;
                        ViewBag.Profile = profileName;

                        var meetings = (await _irfService.GetIRFInterviewMeetings(Convert.ToInt16(id), profileName, emailid))?.ToList();
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

                        if (!String.IsNullOrEmpty(searchValue))
                        {
                            searchValue = searchValue.ToUpper().Trim();
                            candidateList = candidateList.Where(x => (!String.IsNullOrEmpty(x.Name) ? x.Name.ToUpper().Contains(searchValue) : x.Name == "") || (!String.IsNullOrEmpty(x.EmailId) ? x.EmailId.ToUpper().Contains(searchValue) : x.EmailId == "")).ToList();

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
        //                var meetings = (await _irfService.GetIRFInterviewMeetings(Convert.ToInt16(irfId), profileName, emailid))?.ToList();
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
        //        return RedirectToAction("Candidates", new { id = Convert.ToInt16(irfId), irfName = IRFName });
        //    }
        //    return null;
        //}


        public IActionResult ShowCalendly(string irfName, string calendlyLink)
        {
            ViewBag.IRFName = irfName;
            ViewBag.CalendlyLink = calendlyLink;
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
                        if (endDate < DateTime.Now)
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Sorry! Could not send invite! The requested IRF positions have been filled.";
                            return RedirectToAction("Candidates", new { id = Convert.ToInt32(irfId), irfName = IRFName });
                        }
                        if (await _irfService.SendInvite(Convert.ToInt32(irfId), emailId, loggedInUserEmail))
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Invitation sent successfully!";
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
                            TempData["SuccessErrorMessageCandidate"] = "Invite cancelled successfully!";
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        var planner = new InterviewPlanner();
                        var meetings = await _irfService.GetIRFInterviewMeetings(Convert.ToInt16(irfId), profileName, emailid);
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {

                        if (_irfService.AddUpdateInterviewPlanner(model).Result)
                        {
                            TempData["SuccessErrorMessage"] = "Interview Planner updated Successfully";
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
        public IActionResult PauseIRF(int irfId, string irfName)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        if (_irfService.PauseIRF(irfId, irfName))
                        {
                            TempData["SuccessErrorMessage"] = "IRF paused Successfully";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "IRF pause Failed";
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
                TempData["SuccessErrorMessage"] = "IRF pause Failed";
                return RedirectToAction("Index");
            }

        }
        [Authorize]
        public IActionResult PlayIRF(int irfId, string irfName)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        if (_irfService.PlayIRF(irfId, irfName))
                        {
                            TempData["SuccessErrorMessage"] = "IRF enabled Successfully";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "IRF enable Failed";
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
                        var model = _userService.GetInterviewersDetails();
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
                        InterviewerVM interviewerVM = _userService.GetInterviewer(Convert.ToInt32(Id));
                        interviewerVM.id = Convert.ToInt32(Id);
                        if (!String.IsNullOrEmpty(Videoid))
                        {
                            //fetch vediolink wth details
                            var model = _userService.GetMeetingbyid(Convert.ToInt32(Videoid));
                            interviewerVM.VideoLink = model.VideoLink;
                        }
                        interviewerVM.SubVideo_List = _userService.GetInterviewerVideos(interviewerVM.EmailId);
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

        public bool SaveCandidateStatus(int candidateId, int statusValue)
        {
            var result = _irfService.SaveCandidateStatus(candidateId, statusValue);
            if (result)
            {
                TempData["SuccessErrorMessageCandidate"] = "Candidate status saved Successfully";

            }
            else
                TempData["SuccessErrorMessageCandidate"] = "Failed to save candidate status";

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
                    if (profileName == "Admin")
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
                        TempData["SuccessErrorMessageCG"] = "Curated Group added Successfully";
                        return Json(true);
                    }
                    else
                    {
                        TempData["SuccessErrorMessageCG"] = "Adding Curated Group has Failed";
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
                        TempData["SuccessErrorMessageCG"] = "Curated Group edited successfully!";
                        return Json(true);
                    }
                    else
                    {
                        TempData["SuccessErrorMessageCG"] = "Saving changes for Curated Group failed!";
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
                        TempData["SuccessErrorMessageCG"] = "Curated Group users updated successfully!";
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

        public async Task<IActionResult> GetIRFInterviewerMembers(int irfId)
        {
            try
            {
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
        public async Task<IActionResult> InterviewerEntitle(int? Page)
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
                        var model = new Assessment();
                        var modellist = _userService.GetInterviewerEntitle();
                        if (modellist == null)
                        {
                            modellist = new List<AssignmentVM>();
                        }
                        model._assessment = modellist;
                        var skillset = _skillService.GetSkills();
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
        public ActionResult EditAssessment(int assessmentid)
        {
            var model = new Assessment();
            var modellist = _userService.GetInterviewerEntitle();
            if (modellist == null)
            {
                modellist = new List<AssignmentVM>();
            }
            var modeldata = modellist.Where(x => x.id == assessmentid).FirstOrDefault();
            model._assessment = modellist.Where(x => x.id == assessmentid).ToList();
            var skillset = _skillService.GetSkills();
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
            }
            else
            {
                model._skilldetails.id = 0;
                model._skilldetails.AssessmentGroupName = "";
            }
            return PartialView(model);
            //return Json(new { id = modeldata.id, Assessmentname = modeldata.AssessmentGroup, primaryskillset = primaryskilllist });
        }
        /// <summary>
        /// Fetch Assessment Related to Assessment Group
        /// </summary>
        /// <param name="Page"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> InterviewerEntitleDetails(int? Page, int id)
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
                        var model = _userService.InterviewerEntitleDetails(id);

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
        public IActionResult InterviewerEntitle(Assessment model)
        {
            List<Skill> skills = _skillService.GetSkills();
            List<String> selectedSkill = skills.Where(x => model._skilldetails.SelectedSkillIds.Contains(x.id)).Select(x => x.Name).ToList();
            model._skilldetails.PrimarySkill = String.Join(",", selectedSkill);
            var result = _irfService.Addassignmentgroup(model._skilldetails);
            return RedirectToAction("InterviewerEntitle");
        }
        /// <summary>
        /// Assessment Detailed page
        /// </summary>
        /// <returns></returns>
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

                        if (await _irfService.AddCandidateStatus(model))
                        {
                            TempData["SuccessErrorCandidateStatus"] = "Candidate status saved successfully.";
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

                        if (await _irfService.EditCandidateStatus(model))
                        {
                            TempData["SuccessErrorCandidateStatus"] = "Candidate status updated successfully.";
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
            if (!String.IsNullOrEmpty(searchValue))
            {
                searchValue = searchValue.ToUpper().Trim();
                candidateList = candidateList.Where(x => (!String.IsNullOrEmpty(x.Name) ? x.Name.ToUpper().Contains(searchValue) : x.Name == "") || (!String.IsNullOrEmpty(x.EmailId) ? x.EmailId.ToUpper().Contains(searchValue) : x.EmailId == "")).ToList();

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



        public JsonResult InterviewMeetingsJson(string searchValue)
        {
            int Page = 1;
            var meetings = _modelDetail;
            if (!String.IsNullOrEmpty(searchValue))
            {
                searchValue = searchValue.ToUpper().Trim();
                meetings = meetings.Where(x => (!String.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == "") || (!String.IsNullOrEmpty(x.ClientHrContact) ? x.ClientHrContact.ToUpper().Contains(searchValue) : x.ClientHrContact == "")
                            || (!String.IsNullOrEmpty(x.confirmationStatus) ? x.confirmationStatus.ToUpper().Contains(searchValue) : x.confirmationStatus == "") || (!String.IsNullOrEmpty(x.MeetingStatusStatusDesc) ? x.MeetingStatusStatusDesc.ToUpper().Contains(searchValue) : x.MeetingStatusStatusDesc == "")
                            || (!String.IsNullOrEmpty(x.intervieweStatusStatDesc) ? x.intervieweStatusStatDesc.ToUpper().Contains(searchValue) : x.intervieweStatusStatDesc == "")).ToList();

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

        public JsonResult SelectedInterviewMeetingsJson(string searchValue)
        {
            int Page = 1;
            var selectedMeeting = _modelDetail;
            if (!String.IsNullOrEmpty(searchValue))
            {
                searchValue = searchValue.ToUpper().Trim();
                selectedMeeting = selectedMeeting.Where(x => (!String.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == "") || (!String.IsNullOrEmpty(x.ClientHrContact) ? x.ClientHrContact.ToUpper().Contains(searchValue) : x.ClientHrContact == "") || (!String.IsNullOrEmpty(x.confirmationStatus) ? x.confirmationStatus.ToUpper().Contains(searchValue) : x.confirmationStatus == "") || (!String.IsNullOrEmpty(x.MeetingStatusStatusDesc) ? x.MeetingStatusStatusDesc.ToUpper().Contains(searchValue) : x.MeetingStatusStatusDesc == "") || (!String.IsNullOrEmpty(x.intervieweStatusStatDesc) ? x.intervieweStatusStatDesc.ToUpper().Contains(searchValue) : x.intervieweStatusStatDesc == "") || (!String.IsNullOrEmpty(x.confirmationStatus) ? x.confirmationStatus.ToUpper().Contains(searchValue) : x.confirmationStatus == "") || (!String.IsNullOrEmpty(x.MeetingStatusStatusDesc) ? x.MeetingStatusStatusDesc.ToUpper().Contains(searchValue) : x.MeetingStatusStatusDesc == "") || (!String.IsNullOrEmpty(x.intervieweStatusStatDesc) ? x.intervieweStatusStatDesc.ToUpper().Contains(searchValue) : x.intervieweStatusStatDesc == "")).ToList();

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
        public JsonResult MultipleCandidateInvitesJson(string searchValue)
        {
            int Page = 1;
            var modelList = _modelDetle;
            if (!String.IsNullOrEmpty(searchValue))
            {
                searchValue = searchValue.ToUpper().Trim();
                modelList = modelList.Where(x => (!String.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == "") ||
                (!String.IsNullOrEmpty(x.JDTiltle) ? x.JDTiltle.Split("|")[0].ToUpper().Contains(searchValue) : x.JDTiltle == string.Empty) || (!String.IsNullOrEmpty(x.confirmationStatus) ? x.confirmationStatus.ToUpper().Contains(searchValue) : x.confirmationStatus == string.Empty) || (!String.IsNullOrEmpty(x.MeetingStatusStatusDesc) ? x.MeetingStatusStatusDesc.ToUpper().Contains(searchValue) : x.MeetingStatusStatusDesc == null) || (!String.IsNullOrEmpty(x.intervieweStatusStatDesc) ? x.intervieweStatusStatDesc.ToUpper().Contains(searchValue) : x.intervieweStatusStatDesc == null)).ToList();

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
        public async Task<IActionResult> GetCandidateStatus(int id, string irfId)
        {
            var profileName = HttpContext.Session.GetString("profileName")?.Trim();
            profileName = profileName.Replace("\"", "");
            var emailid = HttpContext.Session.GetString("email")?.Trim();
            emailid = emailid.Replace("\"", "");
            if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
            {
                if (HttpContext.Session.GetString("IRFCandidateStatusfilters") != null)
                {
                    HttpContext.Session.Remove("IRFCandidateStatusfilters");
                }

                var model = new List<CandidateTimelineVM>();
                var candidateData = await _irfService.GetCandidateTimeline(profileName == "Client" ? emailid : null, id);
                var distinctCandidates = candidateData.Select(x => (x.IRFId, x.CandidateId, x.Name)).Distinct().OrderBy(x => x.Name);
                foreach (var candidate in distinctCandidates)
                {
                    var data = candidateData.Where(x => (x.CandidateId == candidate.CandidateId && x.IRFId == candidate.IRFId));
                    if (data.Where(x => x.confirmationStatus == 1).Any() && data.Where(x => x.confirmationStatus == 2).Any())
                    {
                        data = data.Where(x => x.confirmationStatus == 1);
                    }
                    var firstData = data.FirstOrDefault();

                    var candidateTimeline = new CandidateTimelineVM();

                    candidateTimeline.HREmail = firstData.HREmail;
                    candidateTimeline.CandidateEmailId = firstData.EmailId;
                    candidateTimeline.CandidateName = firstData.Name;
                    candidateTimeline.IRFId = firstData.IRFId;
                    candidateTimeline.InterviewId = firstData.InterviewId;
                    candidateTimeline.CurrentStatus = firstData.CurrentStatus != null ? firstData.CurrentStatus : string.Empty;
                    candidateTimeline.CandidateMasterId = firstData.CandidateMasterId;
                    candidateTimeline.CurrentInterviewStatus = firstData.CurrentInterviewStatus;
                    candidateTimeline.QCStatus = firstData.QCStatus;
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
                        foreach (var item in scheduleStatusData.Where(x => x.ScheduleStatusValue != "Meeting Status" && x.ScheduleStatusValue != "Interview Status"))
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
                            if ((lastStatus.ScheduleStatusComments == InterviewStatus.Selected.GetDescription() || lastStatus.ScheduleStatusComments == InterviewStatus.BorderLine.GetDescription()) && candidateTimeline.QCStatus != 1)
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
                            if ((item.InterviewStatus == InterviewStatus.Selected.GetDescription() || item.InterviewStatus == InterviewStatus.BorderLine.GetDescription()) && candidateTimeline.QCStatus != 1)
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
            if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
            {
                var model = new List<CandidateTimelineVM>();
                var candidateData = await _irfService.GetCandidateTimeline(profileName == "Client" ? emailid : null, id);
                var distinctCandidates = candidateData.Select(x => (x.IRFId, x.CandidateId, x.Name)).Distinct().OrderBy(x => x.Name);
                foreach (var candidate in distinctCandidates)
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("IRFCandidateStatusfilters", JsonConvert.SerializeObject(filter));
                    }
                    var data = candidateData.Where(x => (x.CandidateId == candidate.CandidateId && x.IRFId == candidate.IRFId));
                    if (data.Where(x => x.confirmationStatus == 1).Any() && data.Where(x => x.confirmationStatus == 2).Any())
                    {
                        data = data.Where(x => x.confirmationStatus == 1);
                    }
                    var firstData = data.FirstOrDefault();

                    var candidateTimeline = new CandidateTimelineVM();

                    candidateTimeline.HREmail = firstData.HREmail;
                    candidateTimeline.CandidateEmailId = firstData.EmailId;
                    candidateTimeline.CandidateName = firstData.Name;
                    candidateTimeline.IRFId = firstData.IRFId;
                    candidateTimeline.InterviewId = firstData.InterviewId;
                    candidateTimeline.CurrentStatus = firstData.CurrentStatus != null ? firstData.CurrentStatus : string.Empty;
                    candidateTimeline.CandidateMasterId = firstData.CandidateMasterId;
                    candidateTimeline.CurrentInterviewStatus = firstData.CurrentInterviewStatus;
                    candidateTimeline.QCStatus = firstData.QCStatus;
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
                        foreach (var item in scheduleStatusData.Where(x => x.ScheduleStatusValue != "Meeting Status" && x.ScheduleStatusValue != "Interview Status"))
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
                            if ((lastStatus.ScheduleStatusComments == InterviewStatus.Selected.GetDescription() || lastStatus.ScheduleStatusComments == InterviewStatus.BorderLine.GetDescription()) && candidateTimeline.QCStatus != 1)
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
                            if ((item.InterviewStatus == InterviewStatus.Selected.GetDescription() || item.InterviewStatus == InterviewStatus.BorderLine.GetDescription()) && candidateTimeline.QCStatus != 1)
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
        public async Task<FileContentResult> GetCandidateStatusExportToExcel(int id, string searchValue)
        {
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("CandidateStatus");
            string fileName = "CandidateStatus_" + DateTime.Now.ToString("dd/MM/yyyy") + ".xlsx";
            var profileName = HttpContext.Session.GetString("profileName")?.Trim();
            profileName = profileName.Replace("\"", "");
            var emailid = HttpContext.Session.GetString("email")?.Trim();
            emailid = emailid.Replace("\"", "");
            if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
            {
                var model = new List<CandidateTimelineVM>();
                var candidateData = await _irfService.GetCandidateTimeline(profileName == "Client" ? emailid : null, id);
                var distinctCandidates = candidateData.Select(x => (x.IRFId, x.CandidateId, x.Name)).Distinct().OrderBy(x => x.Name);
                foreach (var candidate in distinctCandidates)
                {
                    var data = candidateData.Where(x => (x.CandidateId == candidate.CandidateId && x.IRFId == candidate.IRFId));
                    if (data.Where(x => x.confirmationStatus == 1).Any() && data.Where(x => x.confirmationStatus == 2).Any())
                    {
                        data = data.Where(x => x.confirmationStatus == 1);
                    }
                    var firstData = data.FirstOrDefault();
                    var candidateTimeline = new CandidateTimelineVM();
                    candidateTimeline.HREmail = firstData.HREmail;
                    candidateTimeline.CandidateEmailId = firstData.EmailId;
                    candidateTimeline.CandidateName = firstData.Name;
                    candidateTimeline.IRFId = firstData.IRFId;
                    candidateTimeline.InterviewId = firstData.InterviewId;
                    candidateTimeline.CurrentStatus = firstData.CurrentStatus != null ? firstData.CurrentStatus : string.Empty;
                    candidateTimeline.CandidateMasterId = firstData.CandidateMasterId;
                    candidateTimeline.CurrentInterviewStatus = firstData.CurrentInterviewStatus;
                    candidateTimeline.QCStatus = firstData.QCStatus;
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
                        foreach (var item in scheduleStatusData.Where(x => x.ScheduleStatusValue != "Meeting Status" && x.ScheduleStatusValue != "Interview Status"))
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
                            if ((lastStatus.ScheduleStatusComments == InterviewStatus.Selected.GetDescription() || lastStatus.ScheduleStatusComments == InterviewStatus.BorderLine.GetDescription()) && candidateTimeline.QCStatus != 1)
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
                            if ((item.InterviewStatus == InterviewStatus.Selected.GetDescription() || item.InterviewStatus == InterviewStatus.BorderLine.GetDescription()) && candidateTimeline.QCStatus != 1)
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
        public JsonResult SendEmailForCandidateFeedbackByIRF(string IRFId)
        {
            var modelData = new List<SurveyVM>();
            AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
            var result = _azureTableStorageService.GetAllEntities<SurveyVM>("RisebirdSurvey");
            var interviewerids = _meetingService.FetchAllInterviewidfromIrf(IRFId);
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
                        string currenturl = "https://" + Request.Host.ToString() + "/Survey/Index/" + item;
                        Mail _mail = new Mail
                        {
                            TO = CandidateEmail,
                            Subject = "Risebird: Feedback form"
                        };
                        var emailsend = _notificationHelper.SurveyNotification(_mail, currenturl);
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
        public IActionResult PublicAddReport(int interviewId, int irfId, bool IsSelected = false)
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
                ViewBag.ProfileName = "";
                ViewBag.PageStatus = "Public";
                GetStatusList(model, irfId);
                var menus = _userService.GetMenuBasedOnUserEmailld("ExternalUser").GroupBy(p => p.functionName).Select(g => g.First()).ToList();
                if (menus != null)
                {
                    menus = menus.Where(x => x.functionName != "Support" && x.functionName != "Common Pool").ToList();
                    HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
                }
                return PartialView("AddReportPartial", model);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }

        }
        [AllowAnonymous]
        [HttpGet]
        public IActionResult PublicAddReportExternal(int interviewId, int irfId, bool IsSelected = false)
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
                ViewBag.ProfileName = "";
                ViewBag.PageStatus = "PublicExternal";
                GetStatusList(model, irfId);
                var menus = _userService.GetMenuBasedOnUserEmailld("ExternalUser").GroupBy(p => p.functionName).Select(g => g.First()).ToList();
                if (menus != null)
                {
                    menus = menus.Where(x => x.functionName != "Support" && x.functionName != "Common Pool").ToList();
                    HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
                }
                return PartialView("AddReportPartial", model);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }

        }
    }
}