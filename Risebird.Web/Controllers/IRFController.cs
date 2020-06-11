using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using Risebird.CommonHelper;
using System.Data;
using Microsoft.AspNetCore.Authorization;
using OfficeOpenXml;
using Newtonsoft.Json;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class IRFController : Controller
    {
        private IIRFRepository _irfRepository;
        private IUserService _userService;
        private IIRFService _irfService;
        private IEvaluatorService _evaluatorService;
        private IAzureStorageHelper _azureStorageHelper;
        private IMeetingService _meetingService;
        private IApplicationErrorLogger _applicationErrorLogger;
        public IRFController(IIRFRepository iRFRepository, IApplicationErrorLogger applicationErrorLogger, IIRFService irfService, IMeetingService meetingService, IAzureStorageHelper azureStorageHelper, IEvaluatorService evaluatorService, IUserService userService)
        {
            _userService = userService;
            _irfRepository = iRFRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _irfService = irfService;
            _meetingService = meetingService;
            _azureStorageHelper = azureStorageHelper;
            _evaluatorService = evaluatorService;
        }


        [Authorize]
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");//"CuratedUser";
                var emailid = HttpContext.Session.GetString("email")?.Trim();//"harshitjajoorb@gmail.com"
                emailid = emailid.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter" || profileName == "CuratedUser")
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
                        filter.ArchiveStatus = filter.ArchiveStatus == null ? filter.ArchiveStatus = model.IRFList.Select(x => x.ArchiveStatus).ToArray() : filter.ArchiveStatus;
                        filter.SubscriptionStatus = filter.SubscriptionStatus == null ? filter.SubscriptionStatus = model.IRFList.Select(x => x.SubscriptionStatus).ToArray() : filter.SubscriptionStatus;
                        filter.ClientName = filter.ClientName == null ? filter.ClientName = model.IRFList.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;
                        filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = model.IRFList.Select(x => x.JDTiltle).ToArray() : filter.PrimarySkill = filter.PrimarySkill;

                        model.IRFList = model.IRFList.Where(c => filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                        && filter.RFID.Contains(c.IRFId)
                        && (filter.ArchiveStatus.Count() > 0 ? filter.ArchiveStatus.Contains(c.ArchiveStatus) : c.ArchiveStatus == null)
                        && (filter.SubscriptionStatus.Count() > 0 ? filter.SubscriptionStatus.Contains(c.SubscriptionStatus) : c.SubscriptionStatus == null)
                        && (filter.ClientName.Count() > 0 ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)
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
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter" || profileName == "CuratedUser")
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
                    filter.ArchiveStatus = filter.ArchiveStatus == null ? filter.ArchiveStatus = irfList.Select(x => x.ArchiveStatus).ToArray() : filter.ArchiveStatus;
                    filter.SubscriptionStatus = filter.SubscriptionStatus == null ? filter.SubscriptionStatus = irfList.Select(x => x.SubscriptionStatus).ToArray() : filter.SubscriptionStatus;
                    filter.ClientName = filter.ClientName == null ? filter.ClientName = irfList.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;
                    filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = model.IRFList.Select(x => x.JDTiltle).ToArray() : filter.PrimarySkill = filter.PrimarySkill;

                    irfList = irfList.Where(c => filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                    && filter.RFID.Contains(c.IRFId)
                    && (filter.ArchiveStatus.Count() > 0 ? filter.ArchiveStatus.Contains(c.ArchiveStatus) : c.ArchiveStatus == null)
                    && (filter.SubscriptionStatus.Count() > 0 ? filter.SubscriptionStatus.Contains(c.SubscriptionStatus) : c.SubscriptionStatus == null)
                    && (filter.ClientName.Count() > 0 ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)
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

                                var data = new IRFInterviewVM();
                                data.IRFId = item.IRFId;
                                data.PrimarySkill = item.JDTiltle;
                                data.ClientName = item.ClientName;
                                data.EndClientName = item.ClientCompName;
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
                                    var data = new IRFInterviewVM();
                                    data.IRFId = item.IRFId;
                                    data.PrimarySkill = item.JDTiltle;
                                    data.ClientName = item.ClientName;
                                    data.EndClientName = item.ClientCompName;
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
                                    var data = new IRFInterviewVM();
                                    data.IRFId = item.IRFId;
                                    data.PrimarySkill = item.JDTiltle;
                                    data.ClientName = item.ClientName;
                                    data.EndClientName = item.ClientCompName;
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
                                    var data = new IRFInterviewVM();
                                    data.IRFId = item.IRFId;
                                    data.PrimarySkill = item.JDTiltle;
                                    data.ClientName = item.ClientName;
                                    data.EndClientName = item.ClientCompName;
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
                                    var data = new IRFInterviewVM();
                                    data.IRFId = item.IRFId;
                                    data.PrimarySkill = item.JDTiltle;
                                    data.ClientName = item.ClientName;
                                    data.EndClientName = item.ClientCompName;
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
                                    var data = new IRFInterviewVM();
                                    data.IRFId = item.IRFId;
                                    data.PrimarySkill = item.JDTiltle;
                                    data.ClientName = item.ClientName;
                                    data.EndClientName = item.ClientCompName;
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
                                    var data = new IRFInterviewVM();
                                    data.IRFId = item.IRFId;
                                    data.PrimarySkill = item.JDTiltle;
                                    data.ClientName = item.ClientName;
                                    data.EndClientName = item.ClientCompName;
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
        public IActionResult Add()
        {
            if (HttpContext.Session.GetString("email") != null)
            {
                var model = new IRFVm();
                var interviewers = _irfRepository.GetInterviewers();
                model.JoinabilityTypeList = new SelectList(_irfService.GetJoinabilityMatrix(), "id", "Name");
                model.SkillGeneralTypeList = new SelectList(_irfService.GetGeneralSkillMatrix(), "id", "Name");
                model.SelectabilityTypeList = new SelectList(_irfService.GetSelectabilityMatrix(), "id", "Name");
                var priorityTypes = from PriorityLevel s in Enum.GetValues(typeof(PriorityLevel))
                                    select new { ID = (int)s, Name = s.GetDescription() };
                model.PriorityLevelList = new SelectList(priorityTypes, "ID", "Name");

                ViewBag.Clients = new SelectList(_irfRepository.GetClients(), "id", "Name");
                ViewBag.Level = new SelectList(_irfRepository.GetLevels(), "id", "Name");
                ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
                ViewBag.Skill = new SelectList(_irfRepository.GetSkills(), "id", "Name");
                ViewBag.ClientUser = new SelectList(_irfRepository.GetCientUsers(), "id", "EmailId");
                ViewBag.ClientRole = new SelectList(_irfRepository.GetClientRoles(), "id", "Name");
                ViewBag.Interviewers = new SelectList(interviewers, "id", "EmailId");
                ViewBag.Recruiters = new SelectList(_irfRepository.GetRecruiters(), "UserId", "EmailId");
                ViewBag.ResumeEvaluators = new SelectList(_irfRepository.GetResumeEvaluators(), "UserId", "EmailId");
                ViewBag.HRs = new SelectList(_irfRepository.GetHRs(), "UserId", "EmailId");
                ViewBag.QualityController = new SelectList(interviewers, "id", "EmailId");
                ViewBag.EmergencyInterviewers = new SelectList(interviewers, "id", "EmailId");
                ViewBag.RiseBirdHR = new SelectList(_irfService.GetRiseBirdHR(), "id", "EmailId");
                var jobLevels = from JobLevels s in Enum.GetValues(typeof(JobLevels))
                                select new { ID = (int)s, Name = s.GetDescription() };
                ViewBag.JobLevelList = new SelectList(jobLevels, "ID", "Name"); ;

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

                        if (_irfService.AddIRFDetails(irfDetails).Result)
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


        [Authorize]
        public string GetInterviewerSkills(int id)
        {
            var name = _irfService.GetInterviewerSkills(id);
            return name;
        }

        [Authorize]
        public string GetRecruiterDomain(string userId)
        {
            return _irfService.GetRecruiterDomain(Convert.ToInt16(userId));

        }
        [Authorize]
        public SelectList GetInterviewers(string skills)
        {

            var list = new List<SelectListItem>();
            var interviewers = _irfService.GetInterviewers(skills);

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
        public bool CheckUniqueness(string value)
        {
            return _irfService.CheckUniqueness(value);
        }

        [Authorize]
        public IActionResult SampleQuestions(string id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                TechSkillComments model = new TechSkillComments();
                var result = _irfService.GetTechSkillComments(Convert.ToInt16(id));
                //if (result.Count() > 0)
                model.SkillCollection = result;
                model.Skills = new SelectList(_irfService.GetTechSkills(Convert.ToInt16(id)), "id", "Name");
                model.IRFId = Convert.ToInt16(id);
                return View(model);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpPost]
        public IActionResult SampleQuestions(TechSkillComments model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        if (_irfService.AddSkillComments(model))
                        {
                            //TempData["SuccessErrorMessage"] = 
                            model.Message = "Questions saved successfully";
                            model.Skills = new SelectList(_irfService.GetTechSkills(Convert.ToInt16(model.IRFId)), "id", "Name");
                            return View(model);
                        }
                        else
                        {
                            //TempData["SuccessErrorMessage"] = "Questions were not saved.";
                            model.Message = "Questions were not saved.";
                            model.Skills = new SelectList(_irfService.GetTechSkills(Convert.ToInt16(model.IRFId)), "id", "Name");
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
                TempData["SuccessErrorMessage"] = "Questions were not saved";
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        public IActionResult DeleteIRF(string id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        if (_irfService.DeleteIRF(Convert.ToInt16(id)).Result)
                        {
                            TempData["SuccessErrorMessage"] = "IRF deleted Successfully";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "IRF delete Failed";
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
                TempData["SuccessErrorMessage"] = "IRF delete Failed";
                return RedirectToAction("Index");
            }

        }
        [Authorize]
        public IActionResult UnArchiveIRF(string id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        if (_irfService.UnArchiveIRF(Convert.ToInt16(id)).Result)
                        {
                            TempData["SuccessErrorMessage"] = "IRF restored Successfully";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "IRF restore Failed";
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
                TempData["SuccessErrorMessage"] = "IRF restore Failed";
                return RedirectToAction("Index");
            }

        }

        [Authorize]
        [HttpGet]
        public IActionResult SelectedInterviewMeetings(string id, string irfId)
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
                        var meetings = _irfService.GetIRFInterviewMeetings(Convert.ToInt16(id), profileName, emailid).Where(x => x.QCInterview > 0);
                        meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.Date).ToList();
                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((ReceiveMeetingStatus)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                        ViewBag.IRF = irfId;
                        ViewBag.IRFintId = id;
                        //ViewBag.IsSelected = true;
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

                        return View(selectedMeeting);
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
        public IActionResult GetFilteredSelectedMeetings(string filterValue, string id, string irfId)
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
                        var meetings = _irfService.GetIRFInterviewMeetings(Convert.ToInt16(id), profileName, emailid).Where(x => x.QCInterview > 0);

                        if (filterValue == "Today")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }

                        }
                        else if (filterValue == "Tomorrow")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.AddDays(1).Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }
                        else if (filterValue == "Yesterday")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.AddDays(-1).Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }
                        else if (filterValue == "Next Seven Days")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date <= DateTime.Now.AddDays(7).Date && x.interviewDate.Date >= DateTime.Now.Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }




                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((ReceiveMeetingStatus)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                        ViewBag.IRF = irfId;
                        ViewBag.IRFintId = id;
                        //ViewBag.IsSelected = true;
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
                        ViewBag.ProfileName = profileName.ToLower();

                        return PartialView("GetFilteredSelectedMeetings", selectedMeeting);
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
        [ActionName("MultipleCandidateInvites")]
        public async Task<IActionResult> MultipleCandidateInvites(string irfId)
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
        [ActionName("InterviewMeetings")]
        public IActionResult InterviewMeetings(string id, string irfId)
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
                        var meetings = _irfService.GetIRFInterviewMeetings(Convert.ToInt16(id), profileName, emailid);
                        meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.Date).ToList();
                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((ReceiveMeetingStatus)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                        ViewBag.IRF = irfId;
                        ViewBag.IRFintId = id;
                        //ViewBag.IsSelected = false;
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }

        }

        public IActionResult GetFilteredInterviewMeetings(string filterValue, string id, string irfId)
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
                        var meetings = _irfService.GetIRFInterviewMeetings(Convert.ToInt16(id), profileName, emailid);

                        if (filterValue == "Today")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }

                        }
                        else if (filterValue == "Tomorrow")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.AddDays(1).Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }
                        else if (filterValue == "Yesterday")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.AddDays(-1).Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
                        }
                        else if (filterValue == "Next Seven Days")
                        {
                            meetings = meetings.Where(x => x.interviewDate.Date <= DateTime.Now.AddDays(7).Date && x.interviewDate.Date >= DateTime.Now.Date).ToList();
                            if (meetings.Count() == 0)
                            {
                                return new EmptyResult();
                            }
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
                        meetings = meetings.OrderByDescending(x => x.interviewDate).ToList();
                        return PartialView("GetFilteredInterviewMeetings", meetings);
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
        public IActionResult EditIRF(string id)
        {
            try


            {
                if (HttpContext.Session.GetString("email") != null)
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
                    var model = _irfService.GetEditIRF(Convert.ToInt16(id)).Result;
                    string skills = string.Empty;
                    skills = string.Join(",", from item in model.SkillCollection select item.SkillId);
                    var interviewers = _irfService.GetInterviewers(skills);
                    ViewBag.Clients = new SelectList(_irfRepository.GetClients(), "id", "Name");
                    ViewBag.Level = new SelectList(_irfRepository.GetLevels(), "id", "Name");
                    ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
                    ViewBag.Skill = new SelectList(_irfRepository.GetSkills(), "id", "Name");
                    ViewBag.ClientUser = new SelectList(_irfService.GetClientUser(model.ClientId), "id", "EmailId");
                    ViewBag.ClientRole = new SelectList(_irfRepository.GetClientRoles(), "id", "Name");
                    ViewBag.Interviewers = new SelectList(interviewers, "UserId", "EmailId");
                    ViewBag.Recruiters = new SelectList(_irfRepository.GetRecruiters(), "UserId", "EmailId");
                    ViewBag.QualityController = new SelectList(interviewers, "UserId", "EmailId");
                    ViewBag.EmergencyInterviewers = new SelectList(interviewers, "UserId", "EmailId");
                    ViewBag.RiseBirdHR = new SelectList(_irfService.GetRiseBirdHR(), "id", "EmailId");
                    ViewBag.ResumeEvaluators = new SelectList(_irfRepository.GetResumeEvaluators(), "UserId", "EmailId");
                    ViewBag.HRs = new SelectList(_irfRepository.GetHRs(), "UserId", "EmailId");
                    var jobLevels = from JobLevels s in Enum.GetValues(typeof(JobLevels))
                                    select new { ID = (int)s, Name = s.GetDescription() };
                    ViewBag.JobLevelList = new SelectList(jobLevels, "ID", "Name");

                    model.JoinabilityTypeList = new SelectList(_irfService.GetJoinabilityMatrix(), "id", "Name");
                    model.SkillGeneralTypeList = new SelectList(_irfService.GetGeneralSkillMatrix(), "id", "Name");
                    model.SelectabilityTypeList = new SelectList(_irfService.GetSelectabilityMatrix(), "id", "Name");

                    var companyTypes = from CompanyType s in Enum.GetValues(typeof(CompanyType))
                                       select new { ID = (int)s, Name = s.GetDescription() };
                    model.CompanyTypeList = new SelectList(companyTypes, "ID", "Name");
                    var priorityTypes = from PriorityLevel s in Enum.GetValues(typeof(PriorityLevel))
                                        select new { ID = (int)s, Name = s.GetDescription() };
                    model.PriorityLevelList = new SelectList(priorityTypes, "ID", "Name");
                    TempData["JDLink"] = model.JDLink;
                    TempData["ComapanyDetails"] = model.CompanyDetails;
                    TempData["EditIRFId"] = model.IRFId;
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    model.LoggedEmail = emailid;

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
                    if (profileName == "Admin" || profileName == "HR")
                    {

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
                            model.JDLink = TempData["JDLink"] != null ? TempData["JDLink"].ToString() : null;
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
                            model.CompanyDetails = TempData["ComapanyDetails"] != null ? TempData["ComapanyDetails"].ToString() : null;
                        }
                        if (TempData["EditIRFId"] != null && (TempData["EditIRFId"].ToString() != model.IRFId))
                        {
                            if (!(_irfService.CheckUniqueness(model.IRFId)))
                            {
                                TempData["SuccessErrorMessage"] = "Sorry!Skills Channel with this name already exits!";
                                return RedirectToAction("Index");
                            }
                            if (_irfService.AddIRFDetails(model).Result)
                            {
                                TempData["SuccessErrorMessage"] = "Skills Channel saved Successfully";
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
                            if (_irfService.UpdateIRF(model))
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

                return PartialView("AddReportPartial", model);

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("MyInterview");
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
                            await _azureStorageHelper.InsertBlob(model.VideoFile.FileName, "videos", model.VideoFile);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("videos");
                            var blob = containerRef.GetBlockBlobReference(model.VideoFile.FileName);
                            string uploadedvideoFileURL = blob.Uri.ToString();
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
                        if (_meetingService.AddInterviewReport(model).Result)
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
            model.ConfirmationTypeList = new SelectList(confirmationTypes, "ID", "Name"); ;
            model.QuestionsList = new SelectList(_meetingService.GetTechSkillComments(irfId), "Comment", "Comment");

        }

        [Authorize]
        [HttpGet]
        public IActionResult NewChart()
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
                        var meetings = _irfService.GetIRFInterviewMeetings(Convert.ToInt16(id), profileName, emailid);
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }



        }

        [Authorize]
        [HttpGet]
        public IActionResult NewChartSelected()
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
                        var meetings = _irfService.GetIRFInterviewMeetings(Convert.ToInt16(id), profileName, emailid);

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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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
        public IActionResult NewChartClientMeetingData()
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
                        var meetings = _irfService.GetIRFInterviewMeetings(Convert.ToInt16(id), profileName, emailid);
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
                            var obj = new ClientChartData();
                            obj.Name = item;
                            obj.NoShowCount = meetings.Where(x => x.MeetingStatus.HasValue && x.MeetingStatus.Value == 1 && (x.ClientHrContact?.Trim() == item || x.staffingAgency?.Trim() == item)).Count();
                            obj.ScheduledCount = meetings.Where(x => !x.MeetingStatus.HasValue && x.interviewDate >= DateTime.Now && x.confirmationStatus == "1" && (x.ClientHrContact?.Trim() == item || x.staffingAgency?.Trim() == item)).Count();
                            obj.CompletedCount = meetings.Where(x => x.MeetingStatus.HasValue && x.MeetingStatus.Value == 4 && (x.ClientHrContact?.Trim() == item || x.staffingAgency?.Trim() == item)).Count();
                            obj.DroppedCount = meetings.Where(x => x.MeetingStatus.HasValue && x.MeetingStatus.Value == 3 && (x.ClientHrContact?.Trim() == item || x.staffingAgency?.Trim() == item)).Count();
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult NewChartClientFeedbackData()
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
                        var meetings = _irfService.GetIRFInterviewMeetings(Convert.ToInt16(id), profileName, emailid);
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
                            var obj = new ClientChartFeedbackData();
                            obj.Name = item;
                            obj.ProxyCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 1 && (x.ClientHrContact?.Trim() == item || x.staffingAgency?.Trim() == item)).Count();
                            obj.SelectedCount = meetings.Where(x => x.InterviewStatus.HasValue && x.InterviewStatus.Value == 2 && (x.ClientHrContact?.Trim() == item || x.staffingAgency?.Trim() == item)).Count(); ;
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult BudgetConsumedChart()
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
                        var meetings = _irfService.GetIRFInterviewMeetings(Convert.ToInt16(id), profileName, emailid);
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
                                var obj = new MeetingBudgetChart();
                                obj.Date = item.interviewDate;
                                obj.Cost = item.PerMintuteRate;
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult FunnelChart()
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

                        var chartData = _irfService.GetFunnelData(Convert.ToInt16(irfId), irfName);
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }
        [Authorize]
        [HttpGet]
        public IActionResult Candidates(string id, string irfName)
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

                        var meetings = _irfService.GetIRFInterviewMeetings(Convert.ToInt16(id), profileName, emailid)?.ToList();
                        var candidateList = _irfService.GetIRFCandidates(Convert.ToInt32(id))?.ToList();
                        if (profileName == "Recruiter")
                        {
                            var userId = _irfService.GetUserId(emailid, profileName);
                            candidateList = candidateList.Where(x => x.AddedByUserId == userId).ToList();
                        }

                        if (candidateList != null && candidateList.Count > 0)
                        {

                            candidateList.ForEach(x => x.InviteConfirmation = meetings.Where(y => Convert.ToInt32(y.confirmationStatus) == 1 && y.candidateEmail == x.EmailId).ToList().Count);
                            candidateList.Where(x => x.Status != null).ToList().ForEach(x => x.StatusValue = ((CandidateStatus)x.Status).GetDescription() );
                            candidateList.ForEach(x => { x.confirmationStatusDesc = ((ReceiveMeetingStatus)x.confirmationStatus).GetDescription(); x.MeetingStatusDesc = ((MeetingStatus)x.MeetingStatus).GetDescription(); x.InterviewStatusDesc = ((InterviewStatus)x.InterviewStatus).GetDescription(); });
                        }

                        if (TempData["SuccessErrorMessageCandidate"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageCandidate"].ToString();
                            TempData.Remove("SuccessErrorMessageCandidate");
                        }
                        return View(candidateList);

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
        public FileContentResult ExportCandidateList(string irfId, string irfName)
        {
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Candidate Report");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_" + irfName + "_Candidate_Report" + ".xlsx";
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
                        var meetings = _irfService.GetIRFInterviewMeetings(Convert.ToInt16(irfId), profileName, emailid)?.ToList();
                        var candidateList = _irfService.GetIRFCandidates(Convert.ToInt32(irfId))?.ToList();
                        if (candidateList != null && candidateList.Count > 0)
                        {
                            candidateList.Where(x => x.Status != null).ToList().ForEach(x => x.StatusValue = ((CandidateStatus)x.Status).GetDescription());
                            candidateList.ForEach(x => x.InviteConfirmation = meetings.Where(y => Convert.ToInt32(y.confirmationStatus) == 1 && y.candidateEmail == x.EmailId).ToList().Count);
                        }

                        string userEmailId = string.Empty;

                        Sheet.Cells["A1:Z1"].Style.Font.Bold = true;
                        Sheet.Cells["A1:Z1"].Style.Font.Size = 12;
                        Sheet.Cells["A1:Z1"].Style.WrapText = true;
                        //Sheet.Column(1).Width = 30;
                        //Sheet.Column(2).Width = 30;
                        int startColumn = 1; int endColumn = 25;
                        for (int a = startColumn; a <= endColumn; a++)
                        {
                            Sheet.Column(a).Width = 30;
                        }
                        Sheet.Cells[1, 1].Value = "Candidate Name";
                        Sheet.Cells[1, 2].Value = "Candidate Email";
                        Sheet.Cells[1, 3].Value = "Resume";
                        Sheet.Cells[1, 4].Value = "Type";
                        Sheet.Cells[1, 5].Value = "Location";
                        Sheet.Cells[1, 6].Value = "Skill";
                        Sheet.Cells[1, 7].Value = "Work Auth";
                        Sheet.Cells[1, 8].Value = "Current Location";
                        Sheet.Cells[1, 9].Value = "Current Employer";
                        Sheet.Cells[1, 10].Value = "Phone";
                        Sheet.Cells[1, 11].Value = "Total Experience";
                        Sheet.Cells[1, 12].Value = "Current Salary";
                        Sheet.Cells[1, 13].Value = "Expected Salary";
                        Sheet.Cells[1, 14].Value = "Notice";
                        Sheet.Cells[1, 15].Value = "Invites Sent";
                        Sheet.Cells[1, 16].Value = "Invite Confirmation";
                        Sheet.Cells[1, 17].Value = "Schedule Status";

                        int i = 2;
                        //string LastAction = null; ;

                        if (candidateList.Count() > 0)
                        {
                            foreach (var item in candidateList)
                            {
                                Sheet.Cells[i, 1].Value = item.Name;
                                Sheet.Cells[i, 2].Value = item.EmailId;
                                if (!string.IsNullOrEmpty(item.ResumeLink))
                                {
                                    try
                                    {
                                        Sheet.Cells[i, 3].Hyperlink = new Uri(item.ResumeLink, UriKind.Absolute);
                                        Sheet.Cells[i, 3].Value = "Resume Link";
                                        Sheet.Cells[i, 3].Style.Font.UnderLine = true;
                                        Sheet.Cells[i, 3].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                                    }
                                    catch (Exception e)
                                    {
                                        Sheet.Cells[i, 3].Value = "No Resume";
                                    }
                                }
                                else
                                {
                                    Sheet.Cells[i, 3].Value = "No Resume";
                                }
                                Sheet.Cells[i, 4].Value = item.Type != null ? item.Type : "NA";
                                Sheet.Cells[i, 5].Value = item.Location != null ? item.Location : "NA";

                                Sheet.Cells[i, 6].Value = item.Skill != null ? item.Skill : "NA";

                                Sheet.Cells[i, 7].Value = item.WorkAuth != null ? item.WorkAuth : "NA";

                                Sheet.Cells[i, 8].Value = item.CurrentLocation != null ? item.CurrentLocation : "NA";

                                Sheet.Cells[i, 9].Value = item.CurrentEmployer != null ? item.CurrentEmployer : "NA";

                                Sheet.Cells[i, 10].Value = item.Phone != null ? item.Phone : "NA";

                                Sheet.Cells[i, 11].Value = Convert.ToString(item.TotalExperience);//item.TotalExperience != 0 ? Convert.ToString(item.TotalExperience) : "NA";
                                Sheet.Cells[i, 12].Value = Convert.ToString(item.CurrentSalary);//item.CurrentSalary != 0 ? Convert.ToString(item.CurrentSalary) : "NA";

                                Sheet.Cells[i, 13].Value = Convert.ToString(item.ExpectedSalary);// item.ExpectedSalary != 0 ? Convert.ToString(item.ExpectedSalary) : "NA";
                                Sheet.Cells[i, 14].Value = Convert.ToString(item.Notice);// item.Notice != 0 ? Convert.ToString(item.Notice) : "NA";

                                Sheet.Cells[i, 15].Value = item.TotalInvites;
                                Sheet.Cells[i, 16].Value = item.InviteConfirmation;
                                Sheet.Cells[i, 17].Value = item.StatusValue;
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
                        Rng.Value = "Data export failed !!!";
                        return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                }
                else
                {
                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                    Rng.Value = "Data export failed !!!";
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Data export failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }

        }

        [Authorize]
        [HttpGet]
        public IActionResult AddCandidate(string irfId, string irfName)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter")
                {
                    var model = new Candidate();
                    model.IRFId = Convert.ToInt32(irfId);
                    model.IRFName = irfName;
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
        public async Task<IActionResult> AddCandidate(Candidate model)
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
                        var result = _irfService.AddCandidate(model, emailid, profileName).Result;
                        TempData["SuccessErrorMessageCandidate"] = result;
                        return RedirectToAction("Candidates", new { id = model.IRFId, irfName = model.IRFName });

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
                TempData["SuccessErrorMessage"] = "Adding candidate failed";
                return RedirectToAction("Index");
            }
        }

        public ActionResult ImportCandidates(IFormCollection formCollection, string irfId, string IRFName)
        {

            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");

                    if (Request != null)
                    {
                        var file = formCollection.Files[0];
                        var IRFId = Convert.ToInt32((formCollection["IRFId"].ToString()));
                        if ((file != null) && (file.Length > 0) && !string.IsNullOrEmpty(file.FileName))
                        {

                            string fileName = file.FileName;
                            string fileContentType = file.ContentType;
                            byte[] fileBytes = new byte[file.Length];
                            var data = file.OpenReadStream();
                            var candidateList = new List<Candidate>();
                            using (var package = new ExcelPackage(file.OpenReadStream()))
                            {
                                var currentSheet = package.Workbook.Worksheets;
                                var workSheet = currentSheet.First();
                                var noOfCol = workSheet.Dimension.End.Column;
                                var noOfRow = workSheet.Dimension.End.Row;
                                for (int rowIterator = 2; rowIterator <= noOfRow; rowIterator++)
                                {
                                    var candidate = new Candidate();
                                    candidate.Name = (workSheet.Cells[rowIterator, 1] != null) ? workSheet.Cells[rowIterator, 1].Value.ToString() : "";
                                    candidate.EmailId = workSheet.Cells[rowIterator, 2].Value.ToString();
                                    candidate.ResumeLink = (workSheet.Cells[rowIterator, 3].Value != null) ? workSheet.Cells[rowIterator, 3].Value.ToString() : "";         //workSheet.Cells[rowIterator, 3].Value.ToString();
                                    candidate.Type = (workSheet.Cells[rowIterator, 4].Value != null) ? workSheet.Cells[rowIterator, 4].Value.ToString() : ""; //workSheet.Cells[rowIterator, 4].Value.ToString();
                                    candidate.Location = (workSheet.Cells[rowIterator, 5].Value != null) ? workSheet.Cells[rowIterator, 5].Value.ToString() : "";            //workSheet.Cells[rowIterator, 5].Value.ToString();
                                    candidate.Skill = (workSheet.Cells[rowIterator, 6].Value != null) ? workSheet.Cells[rowIterator, 6].Value.ToString() : "";            // workSheet.Cells[rowIterator, 6].Value.ToString();
                                    candidate.WorkAuth = (workSheet.Cells[rowIterator, 7].Value != null) ? workSheet.Cells[rowIterator, 7].Value.ToString() : "";               //workSheet.Cells[rowIterator, 7].Value.ToString();
                                    candidate.CurrentLocation = (workSheet.Cells[rowIterator, 8].Value != null) ? workSheet.Cells[rowIterator, 8].Value.ToString() : "";                 //workSheet.Cells[rowIterator, 8].Value.ToString();
                                    candidate.CurrentEmployer = (workSheet.Cells[rowIterator, 9].Value != null) ? workSheet.Cells[rowIterator, 9].Value.ToString() : "";                 //workSheet.Cells[rowIterator, 9].Value.ToString();
                                    candidate.Phone = (workSheet.Cells[rowIterator, 10].Value != null) ? ("+" + workSheet.Cells[rowIterator, 10].Value.ToString()) : "";                  // "+" + workSheet.Cells[rowIterator, 10].Value.ToString();
                                    candidate.TotalExperience = (workSheet.Cells[rowIterator, 11].Value != null) ? Convert.ToDecimal(workSheet.Cells[rowIterator, 11].Value) : 0;                // Convert.ToDecimal(workSheet.Cells[rowIterator, 11].Value);
                                    candidate.CurrentSalary = (workSheet.Cells[rowIterator, 12].Value != null) ? Convert.ToDecimal(workSheet.Cells[rowIterator, 12].Value) : 0;                  // Convert.ToDecimal(workSheet.Cells[rowIterator, 12].Value);
                                    candidate.ExpectedSalary = (workSheet.Cells[rowIterator, 13].Value != null) ? Convert.ToDecimal(workSheet.Cells[rowIterator, 13].Value) : 0;                //Convert.ToDecimal(workSheet.Cells[rowIterator, 13].Value);
                                    candidate.Notice = (workSheet.Cells[rowIterator, 14].Value != null) ? Convert.ToDecimal(workSheet.Cells[rowIterator, 14].Value) : 0;                   //Convert.ToDecimal(workSheet.Cells[rowIterator, 14].Value);

                                    candidate.IRFId = IRFId;
                                    candidateList.Add(candidate);
                                }

                            }
                            var result = _irfService.AddMultipleCandidates(candidateList, emailid, profileName).Result;
                            TempData["SuccessErrorMessageCandidate"] = result;
                            return RedirectToAction("Candidates", new { id = IRFId, irfName = IRFName });

                        }

                        else
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Importing candidates failed!";
                            return RedirectToAction("Candidates", new { id = IRFId, irfName = IRFName });
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageCandidate"] = "Importing candidates failed!";
                return RedirectToAction("Candidates", new { id = Convert.ToInt16(irfId), irfName = IRFName });
            }
            return null;
        }

        [Authorize]
        public IActionResult DeleteCandidate(string id, string irfId, string IRFName)
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
                        if (_irfService.DeleteCandidate(Convert.ToInt16(id)).Result)
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Candidate deleted Successfully";
                            return RedirectToAction("Candidates", new { id = Convert.ToInt32(irfId), irfName = IRFName });
                        }
                        else
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Candidate delete Failed";
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageCandidate"] = "Candidate delete Failed";
                return RedirectToAction("Index");
            }

        }

        [Authorize]
        [HttpGet]
        public IActionResult EditCandidate(int id, string irfName, string irfId)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter")
                {
                    var model = _irfService.GetCandidate(id, Convert.ToInt32(irfId));
                    model.IRFName = irfName;
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
        public async Task<IActionResult> EditCandidate(Candidate model)
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
                        if (_irfService.UpdateCandidate(model))
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Candidate updated successfully!";
                            return RedirectToAction("Candidates", new { id = model.IRFId, irfName = model.IRFName });
                        }
                        else
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Candidate update failed!";
                            return RedirectToAction("Candidates", new { id = model.IRFId, irfName = model.IRFName });
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
                TempData["SuccessErrorMessageCandidate"] = "Candidate update failed!";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public IActionResult CandidateReport(int candidateId, int irfId)
        {
            try
            {

                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter")
                {
                    var model = _evaluatorService.GetEvaluatorReport(candidateId, irfId).Result;
                    model.HRReport = _evaluatorService.GetHRJoinabilityReport(candidateId, irfId).Result;
                    model.TechSkillEvaluatorReportCollection = _evaluatorService.GetTechReport(candidateId, irfId).Result;
                    model.SkillGeneralEvaluatorReportCollection = _evaluatorService.GetSkillGeneralReport(candidateId, irfId).Result;
                    model.CandidateAudioCollection = _evaluatorService.GetCandidateAudios(candidateId, irfId).Result.ToList().Where(x => !x.IsVoiceBiometric).ToList();
                    ViewBag.Profile = profileName;
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
                return RedirectToAction("MyEvaluations");
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CandidateReport(EvaluatorReport model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null &&
                    HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Recruiter")
                    {

                        if (_evaluatorService.AddSelectabilityReport(model).Result)
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Data saved successfully";
                            return RedirectToAction("Candidates", new { id = model.IRFId, irfName = model.IRFName });
                        }
                        else
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Sorry! Could not save data!";
                            return RedirectToAction("Candidates", new { id = model.IRFId, irfName = model.IRFName });
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
                TempData["SuccessErrorMessageCandidate"] = "Sorry! Could not save data!";
                return RedirectToAction("Candidates", new { id = model.IRFId, irfName = model.IRFName });
            }
        }
        public IActionResult ShowCalendly(string irfName, string calendlyLink)
        {
            ViewBag.IRFName = irfName;
            ViewBag.CalendlyLink = calendlyLink;
            return View();
        }

        [Authorize]
        [HttpGet]
        public IActionResult SendInvite(string irfId, string IRFName, string emailId, DateTime endDate)
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
                        if (endDate < DateTime.Now)
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Sorry! Could not send invite! The requested IRF positions have been filled.";
                            return RedirectToAction("Candidates", new { id = Convert.ToInt32(irfId), irfName = IRFName });
                        }
                        if (_irfService.SendInvite(Convert.ToInt32(irfId), emailId))
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageCandidate"] = "Sorry! Could not send invite!";
                return RedirectToAction("Candidates", new { id = Convert.ToInt32(irfId), irfName = IRFName });
            }

        }

        [Authorize]
        [HttpGet]
        public IActionResult CancelInvite(string irfId, string IRFName, string emailId)
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

                        if (_irfService.CancelInvite(Convert.ToInt32(irfId), emailId))
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageCandidate"] = "Sorry! Could not send invite!";
                return RedirectToAction("Candidates", new { id = Convert.ToInt32(irfId), irfName = IRFName });
            }

        }

        [Authorize]
        [HttpGet]
        public FileContentResult DownloadTemplate()
        {
            string userEmailId = string.Empty;
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
        public IActionResult InterviewPlanner(int irfId, string irfName)
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
                        var meetings = _irfService.GetIRFInterviewMeetings(Convert.ToInt16(irfId), profileName, emailid);
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

                        planner = _irfService.GetInterviewPlanner(irfId);
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }

        }

        [Authorize]
        [HttpGet]
        public IActionResult Invoice(int irfId, string irfName, DateTime startDate, DateTime endDate, string srNumber, string poNumber, string reqNumber, string currencyCode, string projectCode, bool isGST, string discount, string clientAddress)
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
                        model = _irfService.GetInvoiceModel(irfId, startDate, endDate, srNumber, poNumber, reqNumber, currencyCode, projectCode, isGST, discount, clientAddress);

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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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

        [Authorize]
        [HttpGet]
        public IActionResult OpenCandidateInvites()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Candidate")
                    {
                        var irfList = _irfService.GetCandidateIRFInvites(emailid)?.ToList();
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
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }

        }

        [HttpGet]
        public IActionResult UploadAudio(int candidateId, int irfId, string rfName)
        {
            try
            {

                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Recruiter")
                {
                    var model = _evaluatorService.GetCandidate(candidateId, irfId);
                    model.CandidateAudios = /*(IList<CandidateAudio>)*/_evaluatorService.GetCandidateAudios(candidateId, irfId).Result.Where(x => x.IsVoiceBiometric == true).ToList();
                    
                    if(model.CandidateAudios.Where(x=>x.IsVoiceBiometric).Count()>0)
                    {
                        model.HasVoiceBiometricAudio = true;
                        model.PhraseList = new SelectList(_evaluatorService.GetPhrases(), "text", "text",model.CandidateAudios.Where(x=>x.IsVoiceBiometric).FirstOrDefault().PhraseDetail);
                    }
                    else
                    {
                        model.PhraseList = new SelectList(_evaluatorService.GetPhrases(), "text", "text");
                    }
                    if(model.CandidateAudios.Where(x => x.IsVoiceBiometric && !x.IsEnrolled).Count()>0)
                    {
                        model.ContainsUnEnrolled = true;
                    }
                    model.EnrollmentList = GetEnrollmentList();
                    if (TempData["SuccessErrorMessageUploadAudio"] != null)
                    {
                        ViewBag.Message = TempData["SuccessErrorMessageUploadAudio"].ToString();
                        TempData.Remove("SuccessErrorMessageUploadAudio");
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Candidates", new { id = irfId.ToString(), irfName = rfName });
            }
        }

        [Authorize]
        [HttpPost]
        [DisableRequestSizeLimit]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAudio(CandidateAudio model)
        {

            try
            {
                if (HttpContext.Session.GetString("email") != null &&
                    HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Recruiter")
                    {
                        if (model.AudioFile != null)
                        {
                            await _azureStorageHelper.InsertBlob(model.AudioFile.FileName, "audios", model.AudioFile);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("audios");
                            var blob = containerRef.GetBlockBlobReference(model.AudioFile.FileName);
                            string uploadedaudioFileURL = blob.Uri.ToString();
                            if (uploadedaudioFileURL == null)
                            {
                                ModelState.AddModelError(string.Empty, "Audio Upload failed.");
                                return View("UploadAudio", model);
                            }
                            else
                            {
                                model.AudioFileLink = uploadedaudioFileURL;
                            }
                        }

                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");

                        if (_evaluatorService.AddCandidateAudio(model, emailid, profileName).Result)
                        {
                            TempData["SuccessErrorMessageUploadAudio"] = "Audio uploaded successfully";
                            return RedirectToAction("UploadAudio", new { candidateId = model.CandidateId, irfId = model.IRFId, rfName=model.IRFName });
                        }
                        else
                        {
                            TempData["SuccessErrorMessageUploadAudio"] = "Audio Upload failed.";
                            return RedirectToAction("UploadAudio", new { candidateId = model.CandidateId, irfId = model.IRFId, rfName = model.IRFName });
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
                TempData["SuccessErrorMessageUploadAudio"] = "Audio Upload failed.";
                return RedirectToAction("UploadAudio", new { candidateId = model.CandidateId, irfId = model.IRFId, rfName = model.IRFName });
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
                    var datamodel = new CandidateAudioVM();
                    datamodel.IRFName = irfName;
                    datamodel.IRFId = irfId;
                    datamodel.CandidateEmail = candidateEmail;
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

        //[Authorize]
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> EnrollVoiceItUser(IFormCollection model)
        //{

        //    try
        //    {
        //        if (HttpContext.Session.GetString("email") != null &&
        //            HttpContext.Session.GetString("profileName") != null)
        //        {
        //            var profileName = HttpContext.Session.GetString("profileName")?.Trim();
        //            profileName = profileName.Replace("\"", "");
        //            if (profileName == "Admin" || profileName == "HR" || profileName == "Recruiter")
        //            {
        //                var count = model.Count();
        //                int imax = (count - 3) / 8;
        //                var objList = new List<CandidateAudio>();
        //                for (int i = 0; i < imax; i++)
        //                {
        //                    var obj = new CandidateAudio();
        //                    obj.id = Convert.ToInt32(model["CandidateAudios[" + i + "].id"]);
        //                    obj.IRFId = Convert.ToInt32(model["CandidateAudios[" + i + "].IRFId"]);
        //                    obj.IRFName = model["CandidateAudios[" + i + "].IRFName"];
        //                    obj.CandidateId = Convert.ToInt32(model["CandidateAudios[" + i + "].CandidateId"]);
        //                    obj.IsEnrolled = Convert.ToBoolean(model["CandidateAudios[" + i + "].IsEnrolled"]);
        //                    obj.AudioFileLink = model["CandidateAudios[" + i + "].AudioFileLink"];
        //                    obj.PhraseDetail = model["CandidateAudios[" + i + "].PhraseDetail"];
        //                    //VoiceItUserId
        //                    obj.VoiceItUserId = model["CandidateAudios[" + i + "].VoiceItUserId"];
        //                    objList.Add(obj);
        //                }
        //                var result = _evaluatorService.EnrollCandidate(objList);
        //                TempData["SuccessErrorMessageEnroll"] = result;
        //                return RedirectToAction("EnrollVoiceItUser", new { candidateId = model["CandidateAudios[0].CandidateId"], candidateEmail = model["CandidateEmail"], irfId = model["CandidateAudios[0].IRFId"], irfName = model["IRFName"] });

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
        //        TempData["SuccessErrorMessageEnrollment"] = "Failed!";
        //        return RedirectToAction("EnrollVoiceItUser", new { candidateId = model["CandidateAudios[0].CandidateId"], irfId = model["CandidateAudios[0].IRFId"] });

        //    }
        //}
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
                            var obj = new CandidateAudio();
                            obj.id = Convert.ToInt32(model["CandidateAudios[" + i + "].id"]);
                            obj.IRFId = Convert.ToInt32(model["CandidateAudios[" + i + "].IRFId"]);
                            obj.IRFName = model["CandidateAudios[" + i + "].IRFName"];
                            obj.CandidateId = Convert.ToInt32(model["CandidateAudios[" + i + "].CandidateId"]);
                            obj.IsEnrolled = Convert.ToBoolean(model["CandidateAudios[" + i + "].IsEnrolled"]);
                            obj.AudioFileLink = model["CandidateAudios[" + i + "].AudioFileLink"];
                            obj.PhraseDetail = model["CandidateAudios[" + i + "].PhraseDetail"];
                            //VoiceItUserId
                            obj.VoiceItUserId = model["CandidateAudios[" + i + "].VoiceItUserId"];
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
        public async Task<IActionResult> InterviewerDetail()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (TempData["SuccessErrorMessageInterviewer"] != null)
                    {
                        ViewBag.Message = TempData["SuccessErrorMessageInterviewer"].ToString();
                        TempData.Remove("SuccessErrorMessageInterviewer");

                    }

                    return View(_userService.GetInterviewers());
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
                //details for InterviewerDetails Page
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }


        }
        public async Task<IActionResult> InterviewerDetailById(string Id)
        {
            //int ID = Convert.ToInt32(Id);
            //var model=_userService.GetInterviewers();
            //model = model.Where(x => x.id == ID).ToList();
            // code for fetch data or any thing else
            return View();
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
                return _irfService.SubscribeIRF(id, emailid, profileName).Result;

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
                return _irfService.UnSubscribeIRF(id, emailid, profileName).Result;

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }

        }

        private SelectList GetEnrollmentList()
        {
            var listItems = new List<SelectListItem>();
            foreach (var item in Enum.GetValues(typeof(VoiceEnrollment)))
            {
                listItems.Add(new SelectListItem { Text = item.ToString(), Value = item.ToString() });
            }

            return new SelectList(listItems, "Value", "Text", listItems.First().Value);
        }

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

    }
}