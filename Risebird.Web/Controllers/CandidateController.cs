using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
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
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using X.PagedList;

namespace Risebird.Web.Controllers
{
    public class CandidateController : Controller
    {
        private readonly ICandidateService _candidateService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private static List<CandidateTimelineVM> _modelDetails;
        private static List<Candidate> _candidateModelDetails;
        private readonly IIRFService _irfService;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IEvaluatorService _evaluatorService;
        private readonly IMeetingService _meetingService;
        private readonly IUserService _userService;
        private readonly IIRFRepository _iRFRepository;
        private readonly INotificationHelper _notificationHelper;
        private readonly IWhatsAppHelper _whatsAppHelper;
        private readonly ISASUrlHelper _sasUrlHelper;
        private readonly IClientService _clientService;
        private readonly ICalendlyHelper _calendlyHelper;
        private readonly IFileMimeTypeHelper _fileMimeTypeHelper;
        private readonly IIRFRepository _irfRepository;
        private readonly IInterviewSchedulerRepository _interviewSchedulerRepository;
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly ITrackPublicUrlAccessor _trackPublicUrlAccessor;

        //private static string Searchvalue;
        public CandidateController(ICandidateService candidateService, IApplicationErrorLogger applicationErrorLogger, IIRFService irfService, IAzureStorageHelper azureStorageHelper, IEvaluatorService evaluatorService,
            IMeetingService meetingService, IUserService userService, IIRFRepository iRFRepository, INotificationHelper notificationHelper, IWhatsAppHelper whatsAppHelper,
            ISASUrlHelper sasUrlHelper, IClientService clientService, ICalendlyHelper calendlyHelper, IFileMimeTypeHelper fileMimeTypeHelper, IIRFRepository irfRepository,
            IInterviewSchedulerRepository interviewSchedulerRepository, IUserRepository userRepository, IConfiguration configuration, ITrackPublicUrlAccessor trackPublicUrlAccessor)
        {
            _candidateService = candidateService;
            _applicationErrorLogger = applicationErrorLogger;
            _irfService = irfService;
            _azureStorageHelper = azureStorageHelper;
            _evaluatorService = evaluatorService;
            _meetingService = meetingService;
            _userService = userService;
            _iRFRepository = iRFRepository;
            _notificationHelper = notificationHelper;
            _whatsAppHelper = whatsAppHelper;
            _sasUrlHelper = sasUrlHelper;
            _calendlyHelper = calendlyHelper;
            _clientService = clientService;
            _fileMimeTypeHelper = fileMimeTypeHelper;
            _irfRepository = irfRepository;
            _interviewSchedulerRepository = interviewSchedulerRepository;
            _userRepository = userRepository;
            _configuration = configuration;
            _trackPublicUrlAccessor = trackPublicUrlAccessor;
        }

        [HttpGet]
        public async Task<IActionResult> CandidateTimeline()
        {
            try
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                ViewBag.profile = profileName;
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                {
                    var model = new List<CandidateTimelineVM>();
                    var candidateData = new List<CandidateTimelineData>();
                    if (HttpContext.Session.GetString("CandidateTimelinefilters") != null)
                    {
                        var filter = JsonConvert.DeserializeObject<CandidateTimelineFilter>(HttpContext.Session.GetString("CandidateTimelinefilters"));
                        if (filter.StartDate != null && filter.EndDate != null)
                        {
                            candidateData = (await _candidateService.GetCandidateTimelinebyDate(filter.StartDate.Value, filter.EndDate.Value, profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin" ? emailid : null)).ToList();
                        }
                        else
                        {
                            //candidateData = (await _candidateService.GetCandidateTimeline(profileName == "Client" ? emailid : null)).ToList();
                            candidateData = (await _candidateService.GetCandidateTimelinebyDate(DateTime.Now.AddDays(-7), DateTime.Now, profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin" ? emailid : null)).ToList();
                        }
                    }
                    else
                    {
                        //candidateData = (await _candidateService.GetCandidateTimeline(profileName == "Client" ? emailid : null)).ToList();
                        candidateData = (await _candidateService.GetCandidateTimelinebyDate(DateTime.Now.AddDays(-7), DateTime.Now, profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin" ? emailid : null)).ToList();
                    }
                    var distinctCandidates = candidateData.Select(x => (x.IRFId, x.CandidateId)).Distinct();
                    foreach (var candidate in distinctCandidates)
                    {
                        var firstData = new CandidateTimelineData();
                        var data = candidateData.Where(x => (x.CandidateId == candidate.CandidateId && x.IRFId == candidate.IRFId));
                        //if (data.Where(x => x.confirmationStatus == 1).Any() && data.Where(x => x.confirmationStatus == 2).Any())
                        //{
                        //    data = data.Where(x => x.confirmationStatus == 1);
                        //    var interviewDates = data.Select(x => (x.interviewDate, x.InterviewId)).Distinct();
                        //    var latestInterviewDate = interviewDates.OrderByDescending(x => x.interviewDate).FirstOrDefault();
                        //    firstData = data.FirstOrDefault();
                        //}
                        //else if (data.Where(x => x.confirmationStatus == 1).Any() && !data.Where(x => x.confirmationStatus == 2).Any())
                        //{
                        //    var interviewDates = data.Select(x => (x.interviewDate, x.InterviewId)).Distinct();
                        //    var latestInterviewDate = interviewDates.OrderByDescending(x => x.interviewDate).FirstOrDefault();
                        //    data = data.Where(x => x.interviewDate == latestInterviewDate.interviewDate);

                        //    firstData = data.FirstOrDefault();
                        //}
                        if (data.Where(x => x.confirmationStatus == 1).Any() || data.Where(x => x.confirmationStatus == 2).Any())
                        {
                            var interviewDates = data.Select(x => (x.interviewDate, x.InterviewId)).Distinct();
                            var latestInterviewDate = interviewDates.OrderByDescending(x => x.interviewDate).FirstOrDefault();
                            data = data.Where(x => x.interviewDate == latestInterviewDate.interviewDate);
                            firstData = data.FirstOrDefault();
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
                        candidateTimeline.ProfileSource = firstData.ProfileSource;
                        candidateTimeline.IsIRFArchived = firstData.IsIRFArchived;
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
                                candidateTimeline.CurrentStatus = lastStatus.ScheduleStatusComments;
                                candidateTimeline.CurrentStatusDate = lastStatus.ScheduleStatusDate;
                            }
                            else
                            {
                                candidateTimeline.CurrentStatus = lastStatus.ScheduleStatusValue;
                                candidateTimeline.CurrentStatusDate = lastStatus.ScheduleStatusDate;
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
                                //if ((item.InterviewStatus == InterviewStatus.Selected.GetDescription() || item.InterviewStatus == InterviewStatus.BorderLine.GetDescription()) && candidateTimeline.QCStatus != 1)
                                //{
                                //    continue;
                                //}
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
                        if (candidateTimeline.CurrentStatus == CandidateStatusOptions.QCPending.GetDescription())
                        {
                            if (firstData.CurrentInterviewStatus.HasValue && firstData.CurrentInterviewStatus.Value == (int)InterviewStatus.BorderLine && firstData.CurrentInterviewStatus.Value == (int)InterviewStatus.Selected)
                            {
                                //candidateTimeline.CurrentStatus = candidateTimeline.CurrentStatus + "(" + ((InterviewStatus)firstData.CurrentInterviewStatus).GetDescription() + ")";
                                candidateTimeline.CurrentStatus = ((InterviewStatus)firstData.CurrentInterviewStatus).GetDescription();
                            }
                            else if (firstData.CurrentMeetingStatus.HasValue)
                            {
                                candidateTimeline.CurrentStatus = ((MeetingStatus)firstData.CurrentMeetingStatus).GetDescription();
                            }
                        }
                        //add candidate to final model
                        if (candidateTimeline.InterviewStatus != null || candidateTimeline.MeetingStatus != null || (candidateTimeline.ScheduleStatus != null && candidateTimeline.ScheduleStatus.Count() != 0) || !(string.IsNullOrEmpty(candidateTimeline.CurrentStatus)))
                        {
                            model.Add(candidateTimeline);
                        }
                    }

                    //if(profileName=="Client")
                    //{
                    //    var newModel = new List<CandidateTimelineVM>();
                    //    foreach (var item in model)
                    //    {
                    //        if(item.IsIRFArchived)
                    //        {
                    //            var date = item.ScheduleStatus?.Select(x => x.ActualDate)?.Min();
                    //            if(date !=null && date.HasValue)
                    //            {
                    //                if(date?.Date >= DateTime.Now.AddDays(-30).Date)
                    //                {
                    //                    newModel.Add(item);
                    //                }
                    //            }

                    //        }
                    //        else
                    //        {
                    //            newModel.Add(item);
                    //        }
                    //    }
                    //    model = newModel;
                    //}

                    if (HttpContext.Session.GetString("CandidateTimelinefilters") != null)
                    {
                        var filter = JsonConvert.DeserializeObject<CandidateTimelineFilter>(HttpContext.Session.GetString("CandidateTimelinefilters"));

                        filter.HREmail = filter.HREmail == null ? filter.HREmail = model.Select(x => x.HREmail).ToArray() : filter.HREmail = filter.HREmail;
                        filter.ProfileSource = filter.ProfileSource == null ? filter.ProfileSource = model.Select(x => x.ProfileSource).ToArray() : filter.ProfileSource = filter.ProfileSource;

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
                        if (filter.ProfileSource != null)
                        {
                            model = model.Where(x => filter.ProfileSource.Contains(x.ProfileSource)).ToList();
                        }
                    }
                    if (profileName == "Client")
                    {
                        var role = await _clientService.GetClientRoleByEmailId(emailid);
                        ViewBag.ClientRole = role != 0 ? ((ClientRoleEnum)Convert.ToInt32(role)).GetDescription() : string.Empty;
                    }
                    ViewBag.profile = profileName;
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
                return RedirectToAction("CandidateTimeline");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CandidateTimeline(CandidateTimelineFilter filter)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("CandidateTimelinefilters", JsonConvert.SerializeObject(filter));
                    }
                    return RedirectToAction("CandidateTimeline");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("CandidateTimeline");
            }
        }

        /// <summary>
        /// Export selected candidates data to excel
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet]
        public async Task<FileContentResult> CandidateStatusExporttoExcel(/*string modelData*/)
        {
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Candidate Status");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_Candidate_Status" + ".xlsx";
            try
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                //var model = JsonConvert.DeserializeObject<List<CandidateTimelineVM>>(modelData);
                var model = new List<CandidateTimelineVM>();
                var candidateData = new List<CandidateTimelineData>();
                if (HttpContext.Session.GetString("CandidateTimelinefilters") != null)
                {
                    var filter = JsonConvert.DeserializeObject<CandidateTimelineFilter>(HttpContext.Session.GetString("CandidateTimelinefilters"));
                    if (filter.StartDate != null && filter.EndDate != null)
                    {
                        candidateData = (await _candidateService.GetCandidateTimelinebyDate(filter.StartDate.Value, filter.EndDate.Value, profileName == "Client" ? emailid : null)).ToList();
                    }
                    else
                    {
                        candidateData = (await _candidateService.GetCandidateTimelinebyDate(DateTime.Now.AddDays(-7), DateTime.Now, profileName == "Client" ? emailid : null)).ToList();
                    }
                }
                else
                {
                    //candidateData = (await _candidateService.GetCandidateTimeline(profileName == "Client" ? emailid : null)).ToList();
                    candidateData = (await _candidateService.GetCandidateTimelinebyDate(DateTime.Now.AddDays(-7), DateTime.Now, profileName == "Client" ? emailid : null)).ToList();
                }

                var distinctCandidates = candidateData.Select(x => (x.IRFId, x.CandidateId)).Distinct();
                foreach (var candidate in distinctCandidates)
                {
                    var data = candidateData.Where(x => (x.CandidateId == candidate.CandidateId && x.IRFId == candidate.IRFId));
                    //if (data.Where(x => x.confirmationStatus == 1).Any() && data.Where(x => x.confirmationStatus == 2).Any())
                    //{
                    //    data = data.Where(x => x.confirmationStatus != 2);
                    //    data = data.OrderByDescending(x => x.InterviewId);
                    //}
                    //var firstData = data.FirstOrDefault();
                    var firstData = new CandidateTimelineData();

                    //if (data.Where(x => x.confirmationStatus == 1).Any() && data.Where(x => x.confirmationStatus == 2).Any())
                    //{
                    //    data = data.Where(x => x.confirmationStatus == 1);
                    //    var interviewDates = data.Select(x => (x.interviewDate, x.InterviewId)).Distinct();
                    //    var latestInterviewDate = interviewDates.OrderByDescending(x => x.interviewDate).FirstOrDefault();
                    //    firstData = data.FirstOrDefault();
                    //}
                    //else if (data.Where(x => x.confirmationStatus == 1).Any() && !data.Where(x => x.confirmationStatus == 2).Any())
                    //{
                    //    var interviewDates = data.Select(x => (x.interviewDate, x.InterviewId)).Distinct();
                    //    var latestInterviewDate = interviewDates.OrderByDescending(x => x.interviewDate).FirstOrDefault();
                    //    data = data.Where(x => x.interviewDate == latestInterviewDate.interviewDate);

                    //    firstData = data.FirstOrDefault();
                    //}
                    if (data.Where(x => x.confirmationStatus == 1).Any() || data.Where(x => x.confirmationStatus == 2).Any())
                    {
                        var interviewDates = data.Select(x => (x.interviewDate, x.InterviewId)).Distinct();
                        var latestInterviewDate = interviewDates.OrderByDescending(x => x.interviewDate).FirstOrDefault();
                        data = data.Where(x => x.interviewDate == latestInterviewDate.interviewDate);
                        firstData = data.FirstOrDefault();
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
                    candidateTimeline.ProfileSource = firstData.ProfileSource;
                    candidateTimeline.IsIRFArchived = firstData.IsIRFArchived;
                    candidateTimeline.IRFName = firstData.IRFName;
                    candidateTimeline.Phone = firstData.Phone;
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
                            candidateTimeline.CurrentStatus = lastStatus.ScheduleStatusComments;
                            candidateTimeline.CurrentStatusDate = lastStatus.ScheduleStatusDate;
                        }
                        else
                        {
                            candidateTimeline.CurrentStatus = lastStatus.ScheduleStatusValue;
                            candidateTimeline.CurrentStatusDate = lastStatus.ScheduleStatusDate;
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
                            //if ((item.InterviewStatus == InterviewStatus.Selected.GetDescription() || item.InterviewStatus == InterviewStatus.BorderLine.GetDescription()) && candidateTimeline.QCStatus != 1)
                            //{
                            //    continue;
                            //}
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
                    if (candidateTimeline.CurrentStatus == CandidateStatusOptions.QCPending.GetDescription())
                    {
                        if (firstData.CurrentInterviewStatus.HasValue && firstData.CurrentInterviewStatus.Value == (int)InterviewStatus.BorderLine && firstData.CurrentInterviewStatus.Value == (int)InterviewStatus.Selected)
                        {
                            //candidateTimeline.CurrentStatus = candidateTimeline.CurrentStatus + "(" + ((InterviewStatus)firstData.CurrentInterviewStatus).GetDescription() + ")";
                            candidateTimeline.CurrentStatus = ((InterviewStatus)firstData.CurrentInterviewStatus).GetDescription();
                        }
                        else if (firstData.CurrentMeetingStatus.HasValue)
                        {
                            candidateTimeline.CurrentStatus = ((MeetingStatus)firstData.CurrentMeetingStatus).GetDescription();
                        }
                    }
                    //add candidate to final model
                    if (candidateTimeline.InterviewStatus != null || candidateTimeline.MeetingStatus != null || (candidateTimeline.ScheduleStatus != null && candidateTimeline.ScheduleStatus.Count() != 0) || !(string.IsNullOrEmpty(candidateTimeline.CurrentStatus)))
                    {
                        model.Add(candidateTimeline);
                    }
                }
                //if (profileName == "Client")
                //{
                //    var newModel = new List<CandidateTimelineVM>();
                //    foreach (var item in model)
                //    {
                //        if (item.IsIRFArchived)
                //        {
                //            var date = item.ScheduleStatus?.Select(x => x.ActualDate)?.Min();
                //            if (date != null && date.HasValue)
                //            {
                //                if (date?.Date >= DateTime.Now.AddDays(-30).Date)
                //                {
                //                    newModel.Add(item);
                //                }
                //            }

                //        }
                //        else
                //        {
                //            newModel.Add(item);
                //        }
                //    }
                //    model = newModel;
                //}

                if (HttpContext.Session.GetString("CandidateTimelinefilters") != null)
                {
                    var filter = JsonConvert.DeserializeObject<CandidateTimelineFilter>(HttpContext.Session.GetString("CandidateTimelinefilters"));

                    filter.HREmail = filter.HREmail == null ? filter.HREmail = model.Select(x => x.HREmail).ToArray() : filter.HREmail = filter.HREmail;
                    filter.ProfileSource = filter.ProfileSource == null ? filter.ProfileSource = model.Select(x => x.ProfileSource).ToArray() : filter.ProfileSource = filter.ProfileSource;

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
                    if (filter.ProfileSource != null)
                    {
                        model = model.Where(x => filter.ProfileSource.Contains(x.ProfileSource)).ToList();
                    }
                }
                int role = 0;
                if (profileName == "Client")
                {
                    role = await _clientService.GetClientRoleByEmailId(emailid);
                }

                Sheet.Cells["A1:S1"].Style.Font.Bold = true;
                Sheet.Cells["A1:S1"].Style.Font.Size = 12;
                Sheet.Cells["A1:S1"].Style.WrapText = true;
                int startColumn = 1; int endColumn = 12;
                for (int a = startColumn; a <= endColumn; a++)
                {
                    Sheet.Column(a).Width = 30;
                }
                Sheet.Cells[1, 1].Value = "Candidate name";
                Sheet.Cells[1, 2].Value = "Candidate EmailId";
                Sheet.Cells[1, 3].Value = "Primary Skill";
                Sheet.Cells[1, 4].Value = "Client HR Email";
                Sheet.Cells[1, 5].Value = "Candidated Added On/At";
                Sheet.Cells[1, 6].Value = "Schedule Status";
                Sheet.Cells[1, 7].Value = "Phone";

                if (role != (int)ClientRoleEnum.RestrictedIRFowner)
                {
                    Sheet.Cells[1, 8].Value = "Current Status";
                    Sheet.Cells[1, 9].Value = "Current Status Date";
                    Sheet.Cells[1, 10].Value = "Profile Source";
                    Sheet.Cells[1, 11].Value = "Skill Channel ID";
                }

                int i = 2;
                if (model.Count() > 0)
                {
                    foreach (var item in model)
                    {
                        Sheet.Cells[i, 1].Value = item.CandidateName;
                        Sheet.Cells[i, 2].Value = item.CandidateEmailId;
                        Sheet.Cells[i, 3].Value = item.PrimarySkill;
                        Sheet.Cells[i, 4].Value = item.HREmail;
                        if (item.ScheduleStatus != null && item.ScheduleStatus.Count() != 0)
                        {
                            var addedOnData = item.ScheduleStatus.Where(x => x.Status == CandidateStatusOptions.Added.GetDescription())?.FirstOrDefault();
                            if (addedOnData != null)
                            {
                                Sheet.Cells[i, 5].Value = addedOnData.Date + " " + addedOnData.StatusDate + ", " + addedOnData.Time;
                            }
                            else
                            {
                                Sheet.Cells[i, 5].Value = string.Empty;
                            }

                            if (item.ScheduleStatus.Where(x => x.Status == MeetingStatus.Scheduled.GetDescription()).Any())
                            {
                                Sheet.Cells[i, 6].Value = ScheduleStatusTypes.Scheduled.GetDescription();
                            }
                            else
                            {
                                Sheet.Cells[i, 6].Value = ScheduleStatusTypes.NotYetScheduled.GetDescription();
                            }
                        }
                        else
                        {
                            Sheet.Cells[i, 5].Value = string.Empty;
                            Sheet.Cells[i, 6].Value = ScheduleStatusTypes.NotYetScheduled.GetDescription();
                        }

                        Sheet.Cells[i, 7].Value = item.Phone ?? string.Empty;

                        if (role != (int)ClientRoleEnum.RestrictedIRFowner)
                        {
                            if (!string.IsNullOrEmpty(item.CurrentStatus))
                            {
                                Sheet.Cells[i, 8].Value = item.CurrentStatus;
                                Sheet.Cells[i, 9].Value = item.CurrentStatusDate.HasValue && item.CurrentStatusDate.Value != DateTime.MinValue ? item.CurrentStatusDate.Value.ToString() : string.Empty;
                            }
                            else
                            {
                                Sheet.Cells[i, 8].Value = string.Empty;
                                Sheet.Cells[i, 9].Value = string.Empty;
                            }
                            if (!string.IsNullOrEmpty(item.ProfileSource))
                            {
                                Sheet.Cells[i, 10].Value = item.ProfileSource;
                            }
                            else
                            {
                                Sheet.Cells[i, 10].Value = string.Empty;
                            }
                            Sheet.Cells[i, 11].Value = item.IRFName;
                        }
                        i++;
                    }
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else
                {
                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                    Rng.Value = "No data available!";
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Data export failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ResetCandidateTimelineFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("CandidateTimelinefilters");
                    return RedirectToAction("CandidateTimeline");
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
        public async Task<IActionResult> GetCandidates(int id, string irfName, int? Page, string searchValue)
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
                        var ClientRole = HttpContext.Session.GetString("ClientRole")?.Trim();
                        ClientRole = ClientRole.Replace("\"", "");
                        ViewBag.ClientRole = ClientRole;
                        var meetings = (await _irfService.GetIRFInterviewMeetings(id, profileName, emailid))?.ToList();
                        var candidateList = (await _irfService.GetIRFCandidates(id, emailid))?.ToList();
                        if (profileName == "Recruiter")
                        {
                            var userId = _irfService.GetUserId(emailid, profileName);
                            candidateList = candidateList.Where(x => x.AddedByUserId == userId).ToList();
                        }
                        ViewBag.ButtonAccess = true;
                        if (profileName == "Client")
                        {
                            var client = await _userRepository.GetClientUserDetailsByIRF(emailid, id);
                            if (client == null || (client != null && !client.IsRecruiterActive))
                            {
                                ViewBag.ButtonAccess = false;
                            }
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
                        _candidateModelDetails = candidateList;
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

        [HttpPost]
        public async Task<IActionResult> CandidatesJson(int id, string searchValue)
        {
            int? Page = 1;
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");

                var meetings = (await _irfService.GetIRFInterviewMeetings(id, profileName, emailid))?.ToList();
                var candidateList = (await _irfService.GetIRFCandidates(id, emailid))?.ToList();
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
                if (!string.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.ToUpper().Trim();
                    candidateList = candidateList.Where(x => (!string.IsNullOrEmpty(x.Name) ? x.Name.ToUpper().Contains(searchValue) : x.Name == "") || (!string.IsNullOrEmpty(x.EmailId) ? x.EmailId.ToUpper().Contains(searchValue) : x.EmailId == "")).ToList();
                }
                int totalcount = candidateList.Count;
                int Pagestart = 0;
                int Pageend = 0;

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
                return PartialView(candidateList.ToPagedList(Page ?? 1, 10));
            }
            else
            {
                var candidateList = new List<Candidate>();
                return PartialView(candidateList.ToPagedList(Page ?? 1, 10));
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Add(string irfId, string irfName, int id)
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
                        var model = new Candidate
                        {
                            IRFId = Convert.ToInt32(irfId),
                            IRFName = irfName
                        };
                        var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                                         select new { ID = (int)s, Name = s.GetDescription() };
                        model.Heuristicsscorelist = new SelectList(Heuristics, "ID", "Name");
                        model.profileName = profileName;
                        if (id > 0)
                        {
                            model.ResumeLink = await _candidateService.FetchCandidateResumeById(id);
                            model.Candidateesumeid = id;
                        }
                        return PartialView("AddCandidate", model);
                        //return View(model);
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
                return RedirectToAction("GetCandidates", new { id = irfId, irfName = irfName });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Add(Candidate model, string emailids, string id)
        {
            try
            {
                if (string.IsNullOrEmpty(emailids))
                {
                    emailids = HttpContext.Session.GetString("email")?.Trim();
                    emailids = emailids.Replace("\"", "");
                }
                if (!string.IsNullOrEmpty(model.EmailId))
                {
                    model.EmailId = model.EmailId.ToLower();
                }

                var ClientEmailid = await _candidateService.FetchBillingownerById(model.IRFId);
                var ClientDomain = string.Empty;
                if (string.IsNullOrWhiteSpace(ClientEmailid))
                {
                    ClientDomain = "risebird.io";
                }
                else
                {
                    ClientDomain = ClientEmailid.Split("@")[1];
                }
                string result = string.Empty;
                if ((model.EmailId.Split("@").Contains("risebird.io")) || model.EmailId.Split("@").Contains(ClientDomain)) //HttpContext.Session.GetString("IRFId")
                {
                    if (ClientDomain == "risebird.io")
                    {
                        result = "Domain @Risebird.io is prohibited for a candidate";
                    }
                    else
                    {
                        result = "Domain @Risebird.io and " + ClientDomain + " is prohibited for a candidate";
                    }
                }
                else
                {
                    result = await _irfService.AddCandidate(model, emailids, model.profileName);

                    if (model.Candidateesumeid > 0)
                    {
                        await _candidateService.ProcessCandidateResume(model.Candidateesumeid);
                        var sendinvitetocandidate = await _irfService.SendInvite(model.IRFId, model.EmailId, emailids);
                        if (string.IsNullOrEmpty(id))
                        {
                            TempData["SuccessErrorMessageCandidateResume"] = result;
                            return RedirectToAction("Resumes", new { irfId = model.IRFId, irfName = model.IRFName });
                        }
                        else
                        {
                            return RedirectToAction("ResumesPublicly", new { irfId = model.IRFId, irfName = model.IRFName, emailid = emailids, id = id });
                        }
                    }
                }
                TempData["SuccessErrorMessageCandidate"] = result;
                return RedirectToAction("GetCandidates", new { id = model.IRFId, irfName = model.IRFName });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                if (model.Candidateesumeid > 0)
                {
                    TempData["SuccessErrorMessageCandidateResume"] = "Adding candidate failed";
                    if (string.IsNullOrEmpty(id))
                    {
                        return RedirectToAction("Resumes", new { irfId = model.IRFId, irfName = model.IRFName });
                    }
                    else
                    {
                        return RedirectToAction("ResumesPublicly", new { irfId = model.IRFId, irfName = model.IRFName, emailid = emailids, id = id });
                    }
                }
                TempData["SuccessErrorMessage"] = "Adding candidate failed";
                return RedirectToAction("GetCandidates", new { id = model.IRFId, irfName = model.IRFName });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string irfName, string irfId)
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
                        //TODO to check in future
                        var model = await _irfService.GetCandidate(id, Convert.ToInt32(irfId));
                        if (model == null)
                            return RedirectToAction("GetCandidates", new { id = irfId, irfName = irfName });

                        model.IRFName = irfName;
                        //var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                        //                 select new { ID = (int)s, Name = s.GetDescription() };
                        //model.Heuristicsscorelist = new SelectList(Heuristics, "ID", "Name");
                        model.profileName = profileName;
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
                return RedirectToAction("GetCandidates", new { id = irfId, irfName = irfName });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Edit(Candidate model)
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
                        if (!string.IsNullOrEmpty(model.EmailId))
                        {
                            model.EmailId = model.EmailId.ToLower();
                        }
                        //if (!string.IsNullOrEmpty(model.ClientHRAttemptContact))
                        //{
                        //    model.ClientHRAttemptContact = model.ClientHRAttemptContact.ToLower();
                        //}
                        //var result = "Risebird.io is not allowed to add as candidate";
                        var ClientEmailid = await _candidateService.FetchBillingownerById(model.IRFId);
                        var ClientDomain = string.Empty;
                        if (string.IsNullOrWhiteSpace(ClientEmailid))
                        {
                            ClientDomain = "risebird.io";
                        }
                        else
                        {
                            ClientDomain = ClientEmailid.Split("@")[1];
                        }
                        string result = string.Empty;
                        if (!model.EmailId.Split("@").Contains("risebird.io") && !model.EmailId.Split("@").Contains(ClientDomain)) //HttpContext.Session.GetString("IRFId"))
                        {
                            if (await _irfService.UpdateCandidate(model, emailid))
                            {
                                TempData["SuccessErrorMessageCandidate"] = "Candidate has been updated Successfully!";
                                return RedirectToAction("GetCandidates", new { id = model.IRFId, irfName = model.IRFName });
                            }
                            else
                            {
                                TempData["SuccessErrorMessageCandidate"] = "Failed to update Candidate!";
                                return RedirectToAction("GetCandidates", new { id = model.IRFId, irfName = model.IRFName });
                            }
                        }
                        else
                        {
                            if (ClientDomain == "risebird.io")
                            {
                                result = "Domain @Risebird.io is prohibited for a candidate";
                                return RedirectToAction("GetCandidates", new { id = model.IRFId, irfName = model.IRFName });
                            }
                            else
                            {
                                result = "Domain @Risebird.io  and " + ClientDomain + " is prohibited for a candidate";
                                return RedirectToAction("GetCandidates", new { id = model.IRFId, irfName = model.IRFName });
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
                TempData["SuccessErrorMessageCandidate"] = "Failed to update  Candidate!";
                return RedirectToAction("GetCandidates", new { id = model.IRFId, irfName = model.IRFName });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<FileContentResult> ExportCandidateList(string irfId, string irfName)
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        var meetings = (await _irfService.GetIRFInterviewMeetings(Convert.ToInt32(irfId), profileName, emailid))?.ToList();
                        var candidateList = (await _irfService.GetIRFCandidates(Convert.ToInt32(irfId), emailid))?.ToList();
                        if (candidateList != null && candidateList.Count > 0)
                        {
                            candidateList.Where(x => x.Status != null).ToList().ForEach(x => x.StatusValue = ((CandidateStatusOptions)x.Status).GetDescription());
                            candidateList.ForEach(x => x.InviteConfirmation = meetings.Where(y => Convert.ToInt32(y.confirmationStatus) == 1 && y.candidateEmail == x.EmailId).ToList().Count);
                            candidateList.ForEach(x => { x.confirmationStatusDesc = ((ReceiveMeetingStatus)x.confirmationStatus).GetDescription(); x.MeetingStatusDesc = ((MeetingStatus)x.MeetingStatus).GetDescription(); x.InterviewStatusDesc = ((InterviewStatus)x.InterviewStatus).GetDescription(); });
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
                        Sheet.Cells[1, 18].Value = "Confirmation Status";
                        Sheet.Cells[1, 19].Value = "Meeting Status";
                        Sheet.Cells[1, 20].Value = "Interview Status";
                        Sheet.Cells[1, 21].Value = "Interview Date";
                        Sheet.Cells[1, 22].Value = "Video Link";

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
                                    catch (Exception)
                                    {
                                        Sheet.Cells[i, 3].Value = "No Resume";
                                    }
                                }
                                else
                                {
                                    Sheet.Cells[i, 3].Value = "No Resume";
                                }
                                Sheet.Cells[i, 4].Value = item.Type ?? "NA";
                                Sheet.Cells[i, 5].Value = item.Location ?? "NA";

                                Sheet.Cells[i, 6].Value = item.Skill ?? "NA";

                                Sheet.Cells[i, 7].Value = item.WorkAuth ?? "NA";

                                Sheet.Cells[i, 8].Value = item.CurrentLocation ?? "NA";

                                Sheet.Cells[i, 9].Value = item.CurrentEmployer ?? "NA";

                                Sheet.Cells[i, 10].Value = item.Phone ?? "NA";

                                Sheet.Cells[i, 11].Value = Convert.ToString(item.TotalExperience);//item.TotalExperience != 0 ? Convert.ToString(item.TotalExperience) : "NA";
                                Sheet.Cells[i, 12].Value = Convert.ToString(item.CurrentSalary);//item.CurrentSalary != 0 ? Convert.ToString(item.CurrentSalary) : "NA";

                                Sheet.Cells[i, 13].Value = Convert.ToString(item.ExpectedSalary);// item.ExpectedSalary != 0 ? Convert.ToString(item.ExpectedSalary) : "NA";
                                Sheet.Cells[i, 14].Value = Convert.ToString(item.Notice);// item.Notice != 0 ? Convert.ToString(item.Notice) : "NA";

                                Sheet.Cells[i, 15].Value = item.TotalInvites;
                                Sheet.Cells[i, 16].Value = item.InviteConfirmation;
                                Sheet.Cells[i, 17].Value = item.InviteConfirmation > 0 ? "Scheduled" : item.StatusValue;
                                Sheet.Cells[i, 18].Value = item.confirmationStatusDesc != "0" ? item.confirmationStatusDesc : "NA";
                                Sheet.Cells[i, 19].Value = item.MeetingStatusDesc != "0" ? item.MeetingStatusDesc : "NA";
                                Sheet.Cells[i, 20].Value = item.InterviewStatusDesc != "0" ? item.InterviewStatusDesc : "NA";
                                Sheet.Cells[i, 21].Value = item.interviewDate != null ? item.interviewDate?.ToString("dd MMMM yyyy") : "NA";
                                if (!string.IsNullOrEmpty(item.VideoLink))
                                {
                                    try
                                    {
                                        Sheet.Cells[i, 22].Hyperlink = new Uri(item.VideoLink, UriKind.Absolute);
                                        Sheet.Cells[i, 22].Value = "Video Link";
                                        Sheet.Cells[i, 22].Style.Font.UnderLine = true;
                                        Sheet.Cells[i, 22].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                                    }
                                    catch (Exception)
                                    {
                                        Sheet.Cells[i, 22].Value = "No Video";
                                    }
                                }
                                else
                                {
                                    Sheet.Cells[i, 22].Value = "No Video";
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
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Data export failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Delete(int id, int irfId, string IRFName)
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
                        if (await _irfService.DeleteCandidate(id))
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Candidate has been deleted Successfully!";
                            return RedirectToAction("GetCandidates", new { id = irfId, irfName = IRFName });
                        }
                        else
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Failed to delete Candidate!";
                            return RedirectToAction("GetCandidates", new { id = irfId, irfName = IRFName });
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
                TempData["SuccessErrorMessageCandidate"] = "Failed to delete Candidate!";
                return RedirectToAction("GetCandidates", new { id = irfId, irfName = IRFName });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SendInvite(int irfId, string IRFName, string emailId, DateTime endDate)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var loggedInUserEmail = HttpContext.Session.GetString("email")?.Trim();
                    loggedInUserEmail = loggedInUserEmail.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        if (endDate < DateTime.Now)
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Sorry! Could not send invite! The requested Skill Channel positions have been filled.";
                            return RedirectToAction("GetCandidates", new { id = irfId, irfName = IRFName });
                        }
                        if (await _irfService.SendInvite(irfId, emailId, loggedInUserEmail))
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Invitation has been sent Successfully!";
                            return RedirectToAction("GetCandidates", new { id = irfId, irfName = IRFName });
                        }
                        else
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Sorry! Could not send invite!";
                            return RedirectToAction("GetCandidates", new { id = irfId, irfName = IRFName });
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
                return RedirectToAction("GetCandidates", new { id = irfId, irfName = IRFName });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CancelInvite(int irfId, string IRFName, string emailId)
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
                        if (await _irfService.CancelInvite(irfId, emailId))
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Invite has been cancelled Successfully!";
                            return RedirectToAction("GetCandidates", new { id = irfId, irfName = IRFName });
                        }
                        else
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Sorry! Could not cancel invite!";
                            return RedirectToAction("GetCandidates", new { id = irfId, irfName = IRFName });
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
                TempData["SuccessErrorMessageCandidate"] = "Sorry! Could not cancel invite!";
                return RedirectToAction("GetCandidates", new { id = irfId, irfName = IRFName });
            }
        }

        [HttpGet]
        public async Task<IActionResult> UploadAudio(int candidateId, int irfId, string rfName)
        {
            try
            {

                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Recruiter" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                {
                    var model = await _evaluatorService.GetCandidate(candidateId, irfId);
                    model.CandidateAudios = (await _evaluatorService.GetCandidateAudios(candidateId, irfId)).Where(x => x.IsVoiceBiometric == true).ToList();
                    if (model.CandidateAudios.Where(x => x.IsVoiceBiometric).Count() > 0)
                    {
                        model.HasVoiceBiometricAudio = true;
                        model.PhraseList = new SelectList(await _evaluatorService.GetPhrases(), "text", "text", model.CandidateAudios.Where(x => x.IsVoiceBiometric).FirstOrDefault().PhraseDetail);
                    }
                    else
                    {
                        model.PhraseList = new SelectList(await _evaluatorService.GetPhrases(), "text", "text");
                    }
                    if (model.CandidateAudios.Where(x => x.IsVoiceBiometric && !x.IsEnrolled).Count() > 0)
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
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("GetCandidates", new { id = irfId, irfName = rfName });
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
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Recruiter" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        if (model.AudioFile != null)
                        {
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.AudioFile);
                            if (fileMimeType == "audio/basic" || fileMimeType == "audio/mpeg" || fileMimeType == "audio/x-wav")
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
                        }

                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");

                        if (await _evaluatorService.AddCandidateAudio(model, emailid, profileName))
                        {
                            TempData["SuccessErrorMessageUploadAudio"] = "Audio has been uploaded Successfully!";
                            return RedirectToAction("UploadAudio", new { candidateId = model.CandidateId, irfId = model.IRFId, rfName = model.IRFName });
                        }
                        else
                        {
                            TempData["SuccessErrorMessageUploadAudio"] = "Failed to upload Audio!";
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
                TempData["SuccessErrorMessageUploadAudio"] = "Failed to upload Audio!";
                return RedirectToAction("UploadAudio", new { candidateId = model.CandidateId, irfId = model.IRFId, rfName = model.IRFName });
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

        [HttpGet]
        public async Task<IActionResult> Report(int candidateId, int irfId, string rfName)
        {
            try
            {

                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                {
                    var model = await _evaluatorService.GetEvaluatorReport(candidateId, irfId);
                    model.HRReport = await _evaluatorService.GetHRJoinabilityReport(candidateId, irfId);
                    model.TechSkillEvaluatorReportCollection = await _evaluatorService.GetTechReport(candidateId, irfId);
                    model.SkillGeneralEvaluatorReportCollection = await _evaluatorService.GetSkillGeneralReport(candidateId, irfId);
                    model.CandidateAudioCollection = (await _evaluatorService.GetCandidateAudios(candidateId, irfId)).ToList().Where(x => !x.IsVoiceBiometric).ToList();
                    ViewBag.profile = profileName;
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
                return RedirectToAction("GetCandidates", new { id = irfId, irfName = rfName });
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Report(EvaluatorReport model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Recruiter" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        if (await _evaluatorService.AddSelectabilityReport(model))
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Data has been saved Successfully!";
                            return RedirectToAction("GetCandidates", new { id = model.IRFId, irfName = model.IRFName });
                        }
                        else
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Sorry! Could not save data!";
                            return RedirectToAction("GetCandidates", new { id = model.IRFId, irfName = model.IRFName });
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
                return RedirectToAction("GetCandidates", new { id = model.IRFId, irfName = model.IRFName });
            }
        }

        [HttpGet]
        public async Task<IActionResult> FitmentReport(int candidateId, int irfId, string rfName)
        {
            try
            {

                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                {
                    var model = await _candidateService.GetFitmentReportReport(candidateId, irfId);
                    //model.HRReport = await _evaluatorService.GetHRJoinabilityReport(candidateId, irfId);
                    //model.TechSkillEvaluatorReportCollection = await _evaluatorService.GetTechReport(candidateId, irfId);
                    //model.SkillGeneralEvaluatorReportCollection = await _evaluatorService.GetSkillGeneralReport(candidateId, irfId);
                    //model.CandidateAudioCollection = (await _evaluatorService.GetCandidateAudios(candidateId, irfId)).ToList().Where(x => !x.IsVoiceBiometric).ToList();
                    model.Profile = profileName;
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
                return RedirectToAction("GetCandidates", new { id = irfId, irfName = rfName });
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FitmentReport(FitmentReportModel model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        if (await _candidateService.AddEditFitmentReport(model))
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Data has been saved Successfully!";
                            return RedirectToAction("GetCandidates", new { id = model.IRFId, irfName = model.IRFName });
                        }
                        else
                        {
                            TempData["SuccessErrorMessageCandidate"] = "Sorry! Could not save data!";
                            return RedirectToAction("GetCandidates", new { id = model.IRFId, irfName = model.IRFName });
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
                return RedirectToAction("GetCandidates", new { id = model.IRFId, irfName = model.IRFName });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> StatusList(int irfId, int candidateId, string irfName, string candidateName)
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
                return RedirectToAction("GetCandidates", new { id = irfId, irfName = irfName });
            }
        }

        [HttpGet]
        public async Task<IActionResult> AddStatus(int candidateId, int irfId, string rfName, string candidateName)
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
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("GetCandidates", new { id = irfId, irfName = rfName });
            }
        }

        //[ValidateAntiForgeryToken]
        //[HttpPost]
        //public async Task<IActionResult> AddStatus(CandidateStatus model)
        //{

        //    try
        //    {
        //        if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
        //        {
        //            var profileName = HttpContext.Session.GetString("profileName")?.Trim();
        //            profileName = profileName.Replace("\"", "");
        //            var emailid = HttpContext.Session.GetString("email")?.Trim();
        //            emailid = emailid.Replace("\"", "");
        //            if (profileName != "Client")
        //            {
        //                model.PerformedBy = emailid;
        //                if (model.AudioFile != null)
        //                {
        //                    await _azureStorageHelper.InsertBlob(model.AudioFile.FileName, "audios", model.AudioFile);
        //                    var containerRef = _azureStorageHelper.GetCloudBlobContainer("audios");
        //                    var blob = containerRef.GetBlockBlobReference(model.AudioFile.FileName);
        //                    string uploadedaudioFileURL = blob.Uri.ToString();
        //                    if (uploadedaudioFileURL == null)
        //                    {
        //                        TempData["SuccessErrorCandidateStatus"] = "Audio Upload failed.";
        //                        return RedirectToAction("CandidateStatusList", new { irfId = model.IRFId, candidateId = model.CandidateId, irfName = model.IRFName, candidateName = model.CandidateName });

        //                    }
        //                    else
        //                    {
        //                        model.AudioFileLink = uploadedaudioFileURL;
        //                    }
        //                }

        //                if (await _irfService.AddCandidateStatus(model))
        //                {
        //                    TempData["SuccessErrorCandidateStatus"] = "Candidate status saved successfully.";
        //                    return RedirectToAction("StatusList", new { irfId = model.IRFId, candidateId = model.CandidateId, irfName = model.IRFName, candidateName = model.CandidateName });
        //                }
        //                else
        //                {
        //                    TempData["SuccessErrorCandidateStatus"] = "Error saving candidate status.";
        //                    return RedirectToAction("StatusList", new { irfId = model.IRFId, candidateId = model.CandidateId, irfName = model.IRFName, candidateName = model.CandidateName });
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
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        TempData["SuccessErrorCandidateStatus"] = "Error saving candidate status.";
        //        return RedirectToAction("StatusList", new { irfId = model.IRFId, candidateId = model.CandidateId, irfName = model.IRFName, candidateName = model.CandidateName });
        //    }
        //}

        [HttpPost]
        public async Task<IActionResult> AddStatus()
        {
            CandidateStatus model = new CandidateStatus();
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
                    int irfId = 0;
                    int candidateId = 0;
                    int status = 0;
                    if (!string.IsNullOrEmpty(nvc["irfId"]))
                    {
                        irfId = Convert.ToInt32(nvc["irfId"]);
                    }
                    if (!string.IsNullOrEmpty(nvc["candidateId"]))
                    {
                        candidateId = Convert.ToInt32(nvc["candidateId"]);
                    }

                    if (!string.IsNullOrEmpty(nvc["status"]))
                    {
                        status = Convert.ToInt32(nvc["status"]);
                    }

                    if (!string.IsNullOrEmpty(nvc["comment"]))
                    {
                        comment = nvc["comment"];
                    }


                    model.IRFId = irfId;
                    model.CandidateId = candidateId;
                    model.PerformedBy = emailid;
                    model.Status = status;
                    model.Comments = comment;

                    if (profileName != "Client")
                    {
                        if (irfId != 0 && candidateId != 0 && status != 0)
                        {
                            if (Request.Form.Files.Count() > 0)
                            {
                                var audioFile = Request.Form.Files[0];
                                if (audioFile != null)
                                {
                                    var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(audioFile);
                                    if (fileMimeType == "audio/basic" || fileMimeType == "audio/mpeg" || fileMimeType == "audio/x-wav")
                                    {
                                        await _azureStorageHelper.InsertBlob(audioFile.FileName, "audios", audioFile);
                                        var containerRef = _azureStorageHelper.GetCloudBlobContainer("audios");
                                        var blob = containerRef.GetBlockBlobReference(audioFile.FileName);
                                        string uploadedaudioFileURL = blob.Uri.ToString();
                                        if (uploadedaudioFileURL == null)
                                        {
                                            TempData["SuccessErrorMessageCandidate"] = "Failed to upload Audio!";
                                            return Json(false);
                                        }
                                        else
                                        {
                                            model.AudioFileLink = uploadedaudioFileURL;
                                        }
                                    }
                                }
                            }
                            bool result = await _irfService.AddCandidateStatus(model);
                            if (result)
                            {
                                TempData["SuccessErrorMessageCandidate"] = "Candidate status has been saved Successfully!";
                            }
                            else
                            {
                                TempData["SuccessErrorMessageCandidate"] = "Error saving candidate status.";
                            }
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
                TempData["SuccessErrorMessageCandidate"] = "Error saving candidate status.";
                return Json(false);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditStatus(int id, int candidateId, int irfId, string rfName, string candidateName)
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
                return RedirectToAction("StatusList", new { irfId = irfId, candidateId = candidateId, irfName = rfName, candidateName = candidateName });
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> EditStatus(CandidateStatus model)
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
                        model.PerformedBy = emailid;
                        if (model.AudioFile != null)
                        {
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.AudioFile);
                            if (fileMimeType == "audio/basic" || fileMimeType == "audio/mpeg" || fileMimeType == "audio/x-wav")
                            {
                                await _azureStorageHelper.InsertBlob(model.AudioFile.FileName, "audios", model.AudioFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("audios");
                                var blob = containerRef.GetBlockBlobReference(model.AudioFile.FileName);
                                string uploadedaudioFileURL = blob.Uri.ToString();
                                if (uploadedaudioFileURL == null)
                                {
                                    TempData["SuccessErrorCandidateStatus"] = "Failed to upload Audio!";
                                    return RedirectToAction("StatusList", new { irfId = model.IRFId, candidateId = model.CandidateId, irfName = model.IRFName, candidateName = model.CandidateName });
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
                            return RedirectToAction("StatusList", new { irfId = model.IRFId, candidateId = model.CandidateId, irfName = model.IRFName, candidateName = model.CandidateName });
                        }
                        else
                        {
                            TempData["SuccessErrorCandidateStatus"] = "Error updating candidate status.";
                            return RedirectToAction("StatusList", new { irfId = model.IRFId, candidateId = model.CandidateId, irfName = model.IRFName, candidateName = model.CandidateName });
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
                return RedirectToAction("StatusList", new { irfId = model.IRFId, candidateId = model.CandidateId, irfName = model.IRFName, candidateName = model.CandidateName });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeleteStatus(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
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

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> OpenInvites()
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
                        var irfList = (await _irfService.GetCandidateIRFInvites(emailid))?.ToList();
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
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Interviews(string status)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Candidate")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var myInterview = await _meetingService.GetCandidateInterviews(emailid, status);
                        myInterview = myInterview.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); x.JobLevelDesc = ((JobLevels)Convert.ToInt32(x.JobLevel)).GetDescription(); return x; }).ToList();
                        foreach (var item in myInterview)
                        {
                            if (!string.IsNullOrEmpty(item.resumeLink))
                            {
                                try
                                {
                                    item.resumeLink = new Uri(item.resumeLink, UriKind.Absolute).ToString();
                                }
                                catch (Exception)
                                {
                                    item.resumeLink = "";
                                }
                            }
                            else
                            {
                                item.resumeLink = "";
                            }

                            if (item.InterviewStatus.HasValue)
                            {
                                item.InterviewStatusDesc = ((InterviewStatus)(item.InterviewStatus)).GetDescription();
                            }
                            if (item.MeetingStatus.HasValue)
                            {
                                item.MeetingStatusDesc = ((MeetingStatus)(item.MeetingStatus)).GetDescription();
                            }

                            if (item.interviewDate <= DateTime.Now)
                            {
                                item.CancelationStatus = "N";
                            }
                            else
                            {
                                item.CancelationStatus = "Y";
                            }
                        }
                        return View(myInterview);
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
        public async Task<JsonResult> ShortListList(string IRFID)
        {
            var emailid = HttpContext.Session.GetString("email")?.Trim();
            emailid = emailid.Replace("\"", "");
            var model = await _meetingService.ShortListedClient(emailid, IRFID);
            foreach (var item in model)
            {
                item.ClientName = item.ClientName.Split("@")[1].Split(".")[0].ToString();
                item.DateRequired = item.interviewDate.ToString("dd/MMM/yyyy");
            }
            return Json(model);
        }

        [HttpPost]
        public async Task<JsonResult> InterestList(string IRFID)
        {
            var emailid = HttpContext.Session.GetString("email")?.Trim();
            emailid = emailid.Replace("\"", "");
            var model = await _meetingService.InterestListClient(emailid, IRFID);
            foreach (var item in model)
            {
                item.ClientName = item.ClientName.Split("@")[1].Split(".")[0].ToString();
                item.DateRequired = item.interviewDate.ToString("dd/MMM/yyyy");
            }
            return Json(model);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Video(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    ViewBag.actionstatus = "Risetube";
                    ViewBag.ResultStatus = "VideoPresent";
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Candidate")
                    {
                        var emailId = HttpContext.Session.GetString("email")?.Trim();
                        emailId = emailId.Replace("\"", "");
                        if (TempData["SuccessErrorMessageInterview"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                            TempData.Remove("SuccessErrorMessageInterview");
                        }
                        //Start:Save View counts of vedio
                        if (HttpContext.Session.GetString("UserLogedIn") != null)
                        {
                            string values = string.Empty;
                            var valuestatus = HttpContext.Session.GetString("UserLogedIn")?.Trim();
                            if (valuestatus == "Yes")
                            {
                                var emailaddress = HttpContext.Session.GetString("email")?.Trim();
                                emailaddress = emailaddress.Replace("\"", "");
                                await _meetingService.AddTotalViewCount(id, emailaddress, "Public");
                                //save into database to increase view count save into main table from where data is picked
                                HttpContext.Session.SetString("UserLogedIn", "No");
                            }
                            else
                            {
                                if (HttpContext.Session.GetString("UserLogedInVedioId") != null)
                                {
                                    values = HttpContext.Session.GetString("UserLogedInVedioId")?.Trim();
                                    var _lst = values.Split(";");
                                    var flag = true;
                                    for (int i = 0; i < _lst.Count(); i++)
                                    {
                                        if (_lst[i] == id.ToString())
                                        {
                                            flag = false;
                                        }
                                    }
                                    if (flag == true)
                                    {
                                        var emailaddress = HttpContext.Session.GetString("email")?.Trim();
                                        emailaddress = emailaddress.Replace("\"", "");
                                        await _meetingService.AddTotalViewCount(id, emailaddress, "Public");
                                        //save into database to increase view count save into main table from where data is picked
                                    }
                                }
                            }
                            values = values + ";" + id;
                            HttpContext.Session.SetString("UserLogedInVedioId", values);
                        }
                        //End:Save View counts of vedio

                        MeetingVM model = await _meetingService.GetMeetingbyid(id, emailId);

                        if (model == null)
                        {
                            model = new MeetingVM();
                            ViewBag.ResultStatus = "VideoNotPresent";
                            return View(model);
                        }
                        var IRListModel = await _meetingService.GetInteviewReport(model.imid, model.irfIntId);

                        MeetingVM NewModel = new MeetingVM();
                        NewModel.candidateName = model.candidateName;
                        ViewBag.profileName = profileName;
                        NewModel.CandidateId = model.CandidateId;
                        NewModel.interviewDate = model.interviewDate;
                        NewModel.imid = model.imid;
                        NewModel.IRFId = model.IRFId;
                        NewModel.ClientName = model.ClientName;
                        NewModel.InterviewerComment = model.InterviewerComment;
                        NewModel.InterviewStatus = model.InterviewStatus;
                        NewModel.JDTiltle = model.JDTiltle;
                        NewModel.JDLink = model.JDLink;
                        NewModel.MeetingStatus = model.MeetingStatus;
                        NewModel.confirmationStatus = model.confirmationStatus;
                        NewModel.VideoLink = model.VideoLink;
                        NewModel.candidateEmail = model.candidateEmail;
                        NewModel.reportLink = model.reportLink;
                        NewModel.resumeLink = model.resumeLink;
                        NewModel.zoomLink = model.zoomLink;
                        NewModel.IreportModel = IRListModel ?? new InterviewReport();
                        NewModel.IsLike = model.IsLike;
                        NewModel.TotalView = model.TotalView;
                        NewModel.IsShared = model.IsShared;
                        NewModel.TotalVedioLike = model.TotalVedioLike;
                        NewModel.TotalVedioDisLike = model.TotalVedioDisLike;
                        NewModel.ShortListCount = model.ShortListCount;
                        NewModel.IsShortlisted = model.IsShortlisted;
                        NewModel.ThumbnailImage = model.ThumbnailImage;
                        NewModel.IsOnline = model.IsOnline;
                        NewModel.candidateEmail = model.candidateEmail;
                        NewModel.MeetingPatch = model.MeetingPatch;
                        if (model.MeetingPatch.Count() > 0)
                        {
                            NewModel.VideoLink = model.MeetingPatch.FirstOrDefault().VideoLink;
                        }
                        if (NewModel.IreportModel.QuestionsCollection.Where(x => x.VideoLink != null && x.VideoLink != "").Count() > 0)
                        {
                            NewModel.IreportModel.QuestionsCollection = NewModel.IreportModel.QuestionsCollection.Where(x => x.VideoLink != null && x.VideoLink != "").ToList();
                        }
                        // to get videocount details
                        var videodetailscount = await _meetingService.GetTotalViewCount(id);
                        NewModel.VideoCountDetail = videodetailscount.ToList();
                        NewModel.FeedbackAudioLink = model.FeedbackAudioLink;
                        if (NewModel.IreportModel.QuestionsCollection == null)
                        {
                            ViewBag.QuestionsDiscussedCount = 0;
                        }
                        else
                        {
                            ViewBag.QuestionsDiscussedCount = NewModel.IreportModel.QuestionsCollection?.Count();
                        }
                        var comments = await _meetingService.GetvedioComments(id);
                        NewModel.Comments = comments;
                        bool isQC = false;
                        NewModel.IreportModel.IsQC = isQC;
                        if (IRListModel != null)
                        {
                            await GetStatusList(IRListModel, model.irfIntId);
                        }
                        ViewBag.VideoStatus = "Private";
                        return View(NewModel);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    //Start: Share button functionaly
                    ViewBag.ResultStatus = "VideoPresent";
                    MeetingVM model = await _meetingService.GetMeetingbyid(id, "");

                    if (model != null)
                    {
                        var IRListModel = await _meetingService.GetInteviewReport(model.imid, model.irfIntId);
                        if (IRListModel == null)
                        {
                            IRListModel = new InterviewReport();
                        }

                        MeetingVM NewModel = new MeetingVM
                        {
                            SubVideo_List = new List<MeetingVM>()
                        };
                        NewModel.candidateName = model.candidateName;
                        ViewBag.profileName = "";
                        NewModel.CandidateId = model.CandidateId;
                        NewModel.interviewDate = model.interviewDate;
                        NewModel.imid = model.imid;
                        NewModel.IRFId = model.IRFId;
                        NewModel.ClientName = model.ClientName;

                        NewModel.InterviewerComment = model.InterviewerComment;
                        NewModel.InterviewStatus = model.InterviewStatus;
                        NewModel.JDTiltle = model.JDTiltle;

                        NewModel.JDLink = model.JDLink;
                        NewModel.MeetingStatus = model.MeetingStatus;
                        NewModel.confirmationStatus = model.confirmationStatus;
                        NewModel.VideoLink = model.VideoLink;
                        NewModel.candidateEmail = model.candidateEmail;
                        NewModel.reportLink = model.reportLink;
                        NewModel.resumeLink = model.resumeLink;
                        NewModel.zoomLink = model.zoomLink;
                        NewModel.IreportModel = IRListModel ?? new InterviewReport();
                        NewModel.IsLike = model.IsLike;
                        NewModel.TotalView = model.TotalView;
                        NewModel.IsShared = model.IsShared;
                        NewModel.TotalVedioLike = model.TotalVedioLike;
                        NewModel.TotalVedioDisLike = model.TotalVedioDisLike;
                        NewModel.ShortListCount = model.ShortListCount;
                        NewModel.IsShortlisted = model.IsShortlisted;
                        NewModel.ThumbnailImage = model.ThumbnailImage;
                        NewModel.IsIntrested = model.IsIntrested;
                        NewModel.IsOnline = model.IsOnline;
                        NewModel.candidateEmail = model.candidateEmail;
                        NewModel.FeedbackAudioLink = model.FeedbackAudioLink;
                        var videodetailscount = await _meetingService.GetTotalViewCount(id);
                        NewModel.VideoCountDetail = videodetailscount.ToList();
                        if (NewModel.IreportModel.QuestionsCollection == null)
                        {
                            ViewBag.QuestionsDiscussedCount = 0;
                        }
                        else
                        {
                            ViewBag.QuestionsDiscussedCount = NewModel.IreportModel.QuestionsCollection?.Count();
                        }
                        bool isQC = false;
                        NewModel.IreportModel.IsQC = isQC;
                        ViewBag.VideoStatus = "Public";
                        //ViewBag.VideoStatus = "Private";
                        var menuData = await _userService.GetMenuBasedOnUserEmailld("ExternalUser");
                        var menus = menuData.GroupBy(p => p.functionName).Select(g => g.First()).ToList();
                        if (menus != null)
                        {
                            menus = menus.Where(x => x.functionName != "Support").ToList();
                            HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
                        }
                        if (IRListModel != null)
                        {
                            await GetStatusList(IRListModel, model.irfIntId);
                        }
                        //desible Like and dilisk button and share button and comment opetion
                        return View(NewModel);
                        //END: Share button functionaly
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
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

            model.Level = new SelectList(await _iRFRepository.GetLevels(), "id", "Name");
            model.LevelTech = new SelectList(await _iRFRepository.GetLevelTech(), "id", "Name");
            var questionsLevel = from QuestionsLevel s in Enum.GetValues(typeof(QuestionsLevel))
                                 select new { ID = (int)s, Name = s.GetDescription() };
            model.QuestionsLevelList = new SelectList(questionsLevel, "ID", "Name");

            model.QuestionsList = new SelectList(_meetingService.GetTechSkillComments(irfId).Result, "Comment", "Comment");
        }

        [HttpGet]
        public JsonResult ChangeCandidateStatus(int videoid, string Status)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Candidate")
                    {
                        //To update candidate status
                        var model = _meetingService.UpdateCandidateStatus(videoid, Status);
                    }
                    return Json("Success");
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
        public async Task<IActionResult> CancelInterview(int id, DateTime interviewDate, string candidateEmail)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var email = HttpContext.Session.GetString("email");
                    email = email.Replace("\"", "");
                    if (profileName == "Candidate")
                    {
                        if (await _meetingService.CancelInterview(id, interviewDate.ToString("dd MMM yyyy"), candidateEmail))
                        {
                            //TempData["SuccessErrorMessage"] = "Interview cancelled successfully";
                            return Json(new { url = Url.Action("Interviews", "Candidate") });
                        }
                        else
                        {
                            //TempData["SuccessErrorMessage"] = "Sorry! Could not cancel interview!";
                            return Json(new { url = Url.Action("Interviews", "Candidate") });
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
                //TempData["SuccessErrorMessageMyInterview"] = "Unassignment is Failed";
                return RedirectToAction("Interviews");
            }
        }

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
                int startColumn = 1; int endColumn = 6;
                for (int a = startColumn; a <= endColumn; a++)
                {
                    Sheet.Column(a).Width = 35;
                }
                Sheet.Cells[1, 1].Value = "Name";
                Sheet.Cells[1, 2].Value = "EmailId";
                Sheet.Cells[1, 3].Value = "Resume Link";
                Sheet.Cells[1, 4].Value = "Phone(With Country code)";
                Sheet.Cells[1, 5].Value = "WhatsApp Number(Without Country code)";
                Sheet.Cells[1, 6].Value = "Client HR Email ID";
                Sheet.Cells[1, 7].Value = "Staffing Agency Email ID";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception)
            {
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Download failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        [HttpPost]
        public string ImportQuickCandidates(IFormCollection formCollection)
        {
            var IRFId = Convert.ToInt32((formCollection["IRFId"].ToString()));
            var isSendInvite = Convert.ToBoolean((formCollection["IsSendInvite"].ToString()));
            var IRFName = formCollection["IRFName"].ToString();
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    StringBuilder result = new StringBuilder();

                    if (Request != null && formCollection.Files.Count > 0)
                    {
                        var file = formCollection.Files[0];

                        if ((file != null) && (file.Length > 0) && !string.IsNullOrEmpty(file.FileName))
                        {
                            FileInfo fi = new FileInfo(file.FileName);
                            if (fi.Extension == ".xls" || fi.Extension == ".xlsx")
                            {
                                var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(file);
                                if (fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                                    || fileMimeType == "application/vnd.ms-excel")
                                {
                                    _applicationErrorLogger.LogMessage(LogLevelInfo.Info, $"Mime type of file {file.FileName} is {fileMimeType}");
                                    string fileName = file.FileName;
                                    string fileContentType = file.ContentType;
                                    byte[] fileBytes = new byte[file.Length];
                                    var data = file.OpenReadStream();
                                    var candidateList = new List<CandidateQuickAddModel>();
                                    using (var package = new ExcelPackage(file.OpenReadStream()))
                                    {
                                        var currentSheet = package.Workbook.Worksheets;
                                        var workSheet = currentSheet.First();
                                        var noOfCol = workSheet.Dimension.End.Column;
                                        var noOfRow = workSheet.Dimension.End.Row;
                                        var ClientEmailid = _candidateService.FetchBillingownerById(IRFId).Result;
                                        var ClientDomain = string.Empty;
                                        if (string.IsNullOrWhiteSpace(ClientEmailid))
                                        {
                                            ClientDomain = "risebird.io";
                                        }
                                        else
                                        {
                                            ClientDomain = ClientEmailid.Split("@")[1];
                                        }
                                        for (int rowIterator = 2; rowIterator <= noOfRow; rowIterator++)
                                        {
                                            if (workSheet.Cells[rowIterator, 2].Value != null)
                                            {
                                                try
                                                {
                                                    string str = workSheet.Cells[rowIterator, 4].Value.ToString();
                                                    int numberOfLetters = str.ToCharArray().Length;
                                                    var phoneValidate = numberOfLetters >= 9 && int.TryParse(str, out int phoneNumber);
                                                    var candidate = new CandidateQuickAddModel
                                                    {
                                                        Name = workSheet.Cells[rowIterator, 1] != null ? workSheet.Cells[rowIterator, 1].Value.ToString() : string.Empty,
                                                        EmailId = workSheet.Cells[rowIterator, 2].Value.ToString().Trim().ToLower(),
                                                        ResumeLink = (workSheet.Cells[rowIterator, 3].Value != null)
                                              ? workSheet.Cells[rowIterator, 3].Value.ToString()
                                              : string.Empty,
                                                        Phone = ((workSheet.Cells[rowIterator, 4].Value != null) && phoneValidate == true)
                                              ? ("+" + workSheet.Cells[rowIterator, 4].Value)
                                              : string.Empty,
                                                        WhatsappNumber = workSheet.Cells[rowIterator, 5].Value != null
                                              ? workSheet.Cells[rowIterator, 5].Value.ToString()
                                              : string.Empty,
                                                        //ClientHREmail = workSheet.Cells[rowIterator, 6].Value != null
                                                        //    ? workSheet.Cells[rowIterator, 6].Value.ToString().ToLower()
                                                        //    : string.Empty,
                                                        ClientHREmail = workSheet.Cells[rowIterator, 6].Value.ToString().ToLower(),

                                                        ProfileSource = workSheet.Cells[rowIterator, 7].Value != null ? workSheet.Cells[rowIterator, 7].Value.ToString() : string.Empty,
                                                        IRFId = IRFId

                                                    };
                                                    if (!candidate.EmailId.Split("@").Contains("risebird.io") && !candidate.EmailId.Split("@").Contains(ClientDomain))//(!EmailId.Split("@").Contains("risebird.io"))
                                                    {
                                                        candidateList.Add(candidate);
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    if (workSheet.Cells[rowIterator, 2] == null || workSheet.Cells[rowIterator, 2].Value == null
                                                        || workSheet.Cells[rowIterator, 6] == null || workSheet.Cells[rowIterator, 6].Value == null)
                                                    {
                                                        result.Append("Failed! Candidate EmailId and Client HR EmailId are mandatory. Please check for the missing entries.");
                                                    }

                                                    result.Append("Failed!");
                                                    _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

                                                    return result.ToString();
                                                }
                                            }
                                        }
                                    }
                                    if (candidateList != null && candidateList.Any())
                                    {
                                        result = _irfService.AddQuickMultipleCandidates(candidateList, emailid, profileName, IRFId, isSendInvite).Result;
                                    }
                                    else
                                    {
                                        result.Append("No candidates to add!");
                                    }
                                    return result.ToString();
                                }
                                else
                                {
                                    return "Please select valid file for import(.xls, .xslx)!";
                                }
                            }
                            else
                            {
                                return "Please select valid file for import(.xls, .xslx)!";
                            }
                        }
                        else
                        {
                            return "Failed to import candidates!";
                        }
                    }
                    return "Please select valid file for import!";
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to import candidates!"; ;
            }
            return null;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetCandidateStatusList(int irfId, int candidateId, string irfName, string candidateName)
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
                    return PartialView("StatusListPartial", model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("GetCandidates", new { id = irfId, irfName = irfName });
            }
        }

        [HttpGet]
        public async Task<IActionResult> AutoCallCandidate(string phoneNumber, int irfId, int candidateId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var result = await _candidateService.AutoCallCandidate(phoneNumber, irfId, candidateId, emailid);
                    return Json(result);
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

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> CancelInterviewProcess(int id)
        {
            var model = await _candidateService.FetchInterviewDetails(id);
            var menuData = await _userService.GetMenuBasedOnUserEmailld("ExternalUser");
            var menus = menuData.GroupBy(p => p.functionName).Select(g => g.First()).ToList();
            if (menus != null)
            {
                menus = menus.Where(x => x.functionName != "Support" && x.functionName != "Common Pool").ToList();
                HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
            }
            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<JsonResult> CancelInterviewProcess(int id, string CandidateEmail)
        {
            var model = await _candidateService.CancelInterviewDetails(id, CandidateEmail);

            if (model.Contains("Successfully"))
            {
                var result = await _candidateService.FetchChannelOwnerandInterviewer(id);
                var irfOwnersAndRestrictedIrfOwner = await _candidateService.GetCandidateStaffingAgencyAndClientHRByInterviewId(id);
                string oneTimeSchedulingLink = string.Empty;
                if (result.CalendlyLink.Contains("InterviewScheduler/ScheduleEvent"))
                {
                    string stepfirst = result.CalendlyLink.Split("?")[result.CalendlyLink.Split("?").Length];
                    var olduniqueid = stepfirst.Split("=")[stepfirst.Split("=").Length];
                    oneTimeSchedulingLink = _interviewSchedulerRepository.GenerateOneTimeEvent(olduniqueid).Result;
                    if (string.IsNullOrEmpty(oneTimeSchedulingLink))
                    {
                        oneTimeSchedulingLink = _interviewSchedulerRepository.GenerateOneTimeEvent(olduniqueid).Result;
                    }
                }
                else
                {
                    oneTimeSchedulingLink = _calendlyHelper.GenerateOneTimeSchedulingLink(result.irfIntId).Result;
                }

                List<string> interviewers = new List<string>();
                if (!string.IsNullOrEmpty(result.interviewer))
                {
                    interviewers.Add(result.interviewer);
                }
                if (!string.IsNullOrEmpty(result.EmailId))
                {
                    interviewers.Add(result.EmailId);
                }

                await _notificationHelper.SendInterviewCancelledMail(interviewers, result.interviewDate.ToString(), irfOwnersAndRestrictedIrfOwner?.ToList(), null, id);
                //WhatsApp Send
                WhatsAppVM _whatsapp = new WhatsAppVM();
                _whatsapp.Emailid = CandidateEmail;
                if (result.CalendlyLink.Contains("InterviewScheduler/ScheduleEvent"))
                {
                    _whatsapp.Event = "Interview Reschedule Send Risebird";
                }
                else
                {
                    _whatsapp.Event = "Interview Reschedule Send";
                }
                _whatsapp.InterviewDate = result.interviewDate.ToString();
                _whatsapp.Header = result.ClientName;
                _whatsapp.MessageValue = result.JDTiltle;
                if (!string.IsNullOrEmpty(oneTimeSchedulingLink))
                {
                    _whatsapp.buttonlink = oneTimeSchedulingLink.Replace("https://calendly.com/", "");
                }
                else
                {
                    _whatsapp.buttonlink = !string.IsNullOrEmpty(result.CalendlyLink) ? result.CalendlyLink.Replace("https://calendly.com/", "") : "";
                }
                if (_whatsapp.buttonlink.Contains("InterviewScheduler/ScheduleEvent"))
                {
                    _whatsapp.buttonlink = _whatsapp.buttonlink.Split("=").LastOrDefault().ToString();
                }
                await _whatsAppHelper.WhatsAppSendCancleReScheduling(_whatsapp);
            }
            return Json(model);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> UploadResumes(int irfId, string irfName)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        var model = new UploadCandidateResumesViewModel();
                        //var candidateDetails = await _candidateService.GetCandidatesDetails(irfId, candidateId);
                        model.IRFId = irfId;
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
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        //[HttpPost]
        //[RequestFormLimits(MultipartBodyLengthLimit = 1048576000)]
        //[RequestSizeLimit(1048576000)]
        //public async Task<IActionResult> UploadResumes(UploadCandidateResumesViewModel model)
        //{
        //    try
        //    {
        //        if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
        //        {
        //            var profileName = HttpContext.Session.GetString("profileName")?.Trim();
        //            profileName = profileName.Replace("\"", "");
        //            if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
        //            {
        //                if (ModelState.IsValid)
        //                {
        //                    var emailId = HttpContext.Session.GetString("email")?.Trim();
        //                    emailId = emailId.Replace("\"", "");
        //                    if (model.ResumeFiles != null && model.ResumeFiles.Count() > 0)
        //                    {
        //                        var addedByUserId = _irfService.GetUserId(emailId);
        //                        if (addedByUserId > 0)
        //                        {
        //                            var resumeList = new List<CandidateResume>();
        //                            foreach (var file in model.ResumeFiles)
        //                            {
        //                                //upload in blob and get the link and and the entry in the list
        //                                var fileName = file.FileName.Replace(" ", "_");
        //                                FileInfo fi = new FileInfo(fileName);
        //                                await _azureStorageHelper.InsertResumeBlob(fileName, "resumes", file, fi.Extension);
        //                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
        //                                var blob = containerRef.GetBlockBlobReference(fileName);
        //                                string uploadedaudioFileURL = blob.Uri.ToString();
        //                                string sasUrl = string.Empty;
        //                                if (uploadedaudioFileURL == null)
        //                                {
        //                                    ModelState.AddModelError(string.Empty, "Resume upload has failed.");
        //                                    return View(model);
        //                                }
        //                                else
        //                                {
        //                                    //generate SAS URL from the uploaded video url
        //                                    var sas = _sasUrlHelper.CreateSasUrl("resumes");
        //                                    sasUrl = uploadedaudioFileURL + "?" + sas;
        //                                    resumeList.Add(new CandidateResume { IRFId = model.IRFId, AddedBy = addedByUserId, ResumeLink = sasUrl });
        //                                }
        //                            }

        //                            //save the resumeList in db 
        //                            var result = await _candidateService.AddResume(resumeList);
        //                            if (result)
        //                            {
        //                                TempData["SuccessErrorMessageCandidateResume"] = "Resume(s) uploaded successfully.";
        //                            }
        //                            else
        //                            {
        //                                TempData["SuccessErrorMessageCandidateResume"] = "Failed to upload Resume(s)";

        //                            }
        //                            return RedirectToAction("Resumes", new { irfId = model.IRFId, irfName = model.IRFName });

        //                        }
        //                        else
        //                        {
        //                            return RedirectToAction("SignOut", "Session");

        //                        }
        //                    }
        //                    else
        //                    {
        //                        ModelState.AddModelError(string.Empty, "Please select file(s) to upload.");
        //                        return View(model);
        //                    }
        //                }
        //                return View(model); 
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
        //        return RedirectToAction("UploadResumes", new {irfId = model.IRFId, irfName = model.IRFName});
        //    }

        //}

        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 1048576000)]
        [RequestSizeLimit(1048576000)]
        public async Task<IActionResult> UploadResumes(UploadCandidateResumesViewModel model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        if (ModelState.IsValid)
                        {
                            var emailId = HttpContext.Session.GetString("email")?.Trim();
                            emailId = emailId.Replace("\"", "");
                            if (model.ResumeFiles != null && model.ResumeFiles.Count() > 0)
                            {
                                var addedByUserId = _irfService.GetUserId(emailId);
                                if (addedByUserId > 0)
                                {
                                    var resumeList = new List<CandidateResume>();
                                    foreach (var file in model.ResumeFiles)
                                    {
                                        //upload in blob and get the link and and the entry in the list
                                        var fileName = file.FileName.Replace(" ", "_");
                                        FileInfo fi = new FileInfo(fileName);
                                        if (fi.Extension.ToLower() == ".docx" || fi.Extension.ToLower() == ".doc" || fi.Extension.ToLower() == ".pdf")
                                        {
                                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(file);
                                            if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                                || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                                            {

                                                await _azureStorageHelper.InsertResumeBlob(fileName, "resumes", file, fi.Extension);
                                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                                var blob = containerRef.GetBlockBlobReference(fileName);
                                                string uploadedaudioFileURL = blob.Uri.ToString();
                                                string sasUrl = string.Empty;
                                                if (uploadedaudioFileURL == null)
                                                {
                                                    ModelState.AddModelError(string.Empty, "Resume upload has failed.");
                                                    return View(model);
                                                }
                                                else
                                                {
                                                    //generate SAS URL from the uploaded video url
                                                    var sas = _sasUrlHelper.CreateSasUrl("resumes");
                                                    sasUrl = uploadedaudioFileURL + "?" + sas;
                                                    resumeList.Add(new CandidateResume { IRFId = model.IRFId, AddedBy = addedByUserId, ResumeLink = sasUrl });
                                                }
                                            }
                                            else
                                            {
                                                ModelState.AddModelError(string.Empty, "Please select valid file for import(.pdf, .doc, .docx)!");
                                                return View(model);
                                            }
                                        }
                                        else
                                        {
                                            ModelState.AddModelError(string.Empty, "Please select valid file for import(.pdf, .doc, .docx)!");
                                            return View(model);
                                        }
                                    }
                                    //save the resumeList in db 
                                    var result = await _candidateService.AddResume(resumeList);
                                    if (result)
                                    {
                                        TempData["SuccessErrorMessageCandidateResume"] = "Resume(s) has been uploaded Successfully!";
                                    }
                                    else
                                    {
                                        TempData["SuccessErrorMessageCandidateResume"] = "Failed to upload Resume(s)!";

                                    }
                                    return RedirectToAction("Resumes", new { irfId = model.IRFId, irfName = model.IRFName });
                                }
                                else
                                {
                                    return RedirectToAction("SignOut", "Session");
                                }
                            }
                            else
                            {
                                //ModelState.AddModelError(string.Empty, "Please select file(s) to upload.");
                                //return View(model);
                                return RedirectToAction("Resumes", new { irfId = model.IRFId, irfName = model.IRFName });
                            }
                        }
                        //return View(model);
                        return RedirectToAction("Resumes", new { irfId = model.IRFId, irfName = model.IRFName });
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
                return RedirectToAction("UploadResumes", new { irfId = model.IRFId, irfName = model.IRFName });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Resumes(int irfId, string irfName, int actiontype)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");

                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        var model = new ResumeListViewModel();
                        model.ResumeList = (await _candidateService.GetCandidatesResumes(irfId))?.ToList();
                        model.ResumeList = model.ResumeList.OrderBy(x => x.IsProcessed).ToList();
                        model.IRFName = irfName;
                        model.IRFId = irfId;
                        model.Profile = profileName;

                        if (TempData["SuccessErrorMessageCandidateResume"] != null)
                        {
                            model.Message = TempData["SuccessErrorMessageCandidateResume"].ToString();
                            TempData.Remove("SuccessErrorMessageCandidateResume");
                        }
                        if (actiontype == 1)
                        {
                            ViewBag.backbutton = false;
                        }
                        else
                        {
                            ViewBag.backbutton = true;
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
                return RedirectToAction("GetCandidates", new { id = irfId, irfName = irfName });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeleteResume(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        var result = await _candidateService.DeleteCandidateResume(id);
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
        public async Task<FileContentResult> CandidateResumesExporttoExcel(int irfId, string irfName)
        {
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Resumes");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_CandidateResumes_" + irfName + ".xlsx";
            try
            {
                var model = await _candidateService.GetCandidatesResumes(irfId);
                Sheet.Cells["A1:F1"].Style.Font.Bold = true;
                Sheet.Cells["A1:F1"].Style.Font.Size = 12;
                Sheet.Cells["A1:F1"].Style.WrapText = true;

                //Sheet.Column(1).Width = 30;
                //Sheet.Column(2).Width = 30;
                int startColumn = 1; int endColumn = 6;
                for (int a = startColumn; a <= endColumn; a++)
                {
                    Sheet.Column(a).Width = 40;
                }
                Sheet.Cells[1, 1].Value = "Date";
                Sheet.Cells[1, 2].Value = "Skills Channel";
                Sheet.Cells[1, 3].Value = "File Name";
                Sheet.Cells[1, 4].Value = "Resume URL";
                //ExcelHyperLink excelHyperLink = new ExcelHyperLink();
                int i = 2;
                //string LastAction = null; ;

                if (model.Count() > 0)
                {
                    foreach (var item in model)
                    {
                        Sheet.Cells[i, 1].Value = item.AddedOn.ToString("MM/dd/yyyy HH:mm");
                        Sheet.Cells[i, 2].Value = irfName;
                        var resumeFileName = string.Empty;
                        if (item.ResumeLink.Split('/')?.Last().Split('?') != null)
                        {
                            resumeFileName = item.ResumeLink.Split('/')?.Last().Split('?')?.First();
                        }
                        else
                        {
                            resumeFileName = item.ResumeLink.Split('/')?.Last();
                        }
                        Sheet.Cells[i, 3].Value = resumeFileName;

                        if (!string.IsNullOrEmpty(item.ResumeLink))
                        {
                            Sheet.Cells[i, 4].Value = item.ResumeLink;
                            //try
                            //{
                            //    Sheet.Cells[i, 4].Hyperlink = new Uri(item.ResumeLink, UriKind.Absolute);
                            //    Sheet.Cells[i, 4].Value = "Resume";
                            //    Sheet.Cells[i, 4].Style.Font.UnderLine = true;
                            //    Sheet.Cells[i, 4].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                            //}
                            //catch (Exception)
                            //{
                            //    Sheet.Cells[i, 4].Value = "No Resume";
                            //}
                        }
                        else
                        {
                            Sheet.Cells[i, 4].Value = "No Resume";
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

        [HttpGet]
        public async Task<IActionResult> PublicCandidateStatus(string emailid, string id)
        {
            try
            {
                var Emailid = string.Empty;
                if (HttpContext.Session.GetString("email") != null)
                {
                    Emailid = HttpContext.Session.GetString("email")?.Trim();
                    Emailid = Emailid.Replace("\"", "");
                }
                var client = await _userRepository.GetClientUserDetails(emailid);
                if (client != null)
                {
                    var currentUrl = Request.Scheme + "://" + Request.Host.Value + Request.Path;
                    _trackPublicUrlAccessor.SendTelemetry(JsonConvert.SerializeObject(Emailid), currentUrl);
                    var logovalue = await _clientService.Fetchlogoforclient(Emailid, id);
                    if (string.IsNullOrEmpty(logovalue))
                    {
                        HttpContext.Session.SetString("Logo", "");
                    }
                    else
                        HttpContext.Session.SetString("Logo", logovalue);
                    var model = new List<CandidateTimelineVM>();
                    ViewBag.emailid = emailid;
                    ViewBag.id = id;
                    HttpContext.Session.SetString("email", JsonConvert.SerializeObject(emailid));
                    HttpContext.Session.SetString("profileName", JsonConvert.SerializeObject("Client"));
                    HttpContext.Session.Remove("PublicCandidateStatusfilters");
                    var candidateData = new List<CandidateTimelineData>();
                    candidateData = (await _candidateService.GetCandidateTimelinebyDateStatus(DateTime.Now.AddDays(-7), DateTime.Now.AddDays(7), 2, emailid)).ToList();
                    var distinctCandidates = candidateData.Select(x => (x.IRFId, x.CandidateId)).Distinct();
                    foreach (var candidate in distinctCandidates)
                    {
                        var firstData = new CandidateTimelineData();
                        var data = candidateData.Where(x => (x.CandidateId == candidate.CandidateId && x.IRFId == candidate.IRFId));
                        var scheduleStatusData = data.Select(x => (x.ScheduleStatusDate, x.ScheduleStatusValue, x.ScheduleStatusComments, x.InterviewId)).Distinct();
                        var lastvalue = scheduleStatusData.OrderBy(x => x.ScheduleStatusDate).LastOrDefault();
                        if (data.Where(x => x.confirmationStatus == 1).Any() || data.Where(x => x.confirmationStatus == 2).Any())
                        {
                            firstData = data.Where(x => x.confirmationStatus == 1 || x.confirmationStatus == 2).OrderByDescending(x => x.interviewDate).FirstOrDefault();
                        }
                        else
                        {
                            firstData = data.FirstOrDefault();
                        }
                        var _datavalue = data.Select(x => x.InterviewId).Distinct().Count();
                        var candidateTimeline = new CandidateTimelineVM();
                        candidateTimeline.IsQCRequired = client.IsQCRequired;
                        candidateTimeline.IsRedoInterview = client.IsRedoInterview;
                        candidateTimeline.IsClientCancelInterview = client.IsClientCancelInterview;
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
                        candidateTimeline.ProfileSource = firstData.ProfileSource;
                        candidateTimeline.IsIRFArchived = firstData.IsIRFArchived;
                        candidateTimeline.Phone = firstData.MobileNumber;
                        candidateTimeline.interviewDate = firstData.interviewDate;
                        candidateTimeline.IRFname = firstData.IRFName;
                        candidateTimeline.imid = firstData.imid;
                        candidateTimeline.CalendlyLink = firstData.CalendlyLink;
                        candidateTimeline.ClientAction = firstData.ClientAction;
                        candidateTimeline.NickName = firstData.InterviewerNickName;
                        candidateTimeline.reportcount = _datavalue;
                        candidateTimeline.Interviewer = firstData.Interviewer;
                        candidateTimeline.WhatsappCount = firstData.WhatsappCount;
                        candidateTimeline.MailCount = firstData.MailCount;
                        candidateTimeline.CallsCount = firstData.CallsCount;
                        candidateTimeline.IsReportBlocked = firstData.IsReportBlocked;
                        //candidateTimeline.staffingAgency = firstData.staffingAgency;
                        candidateTimeline.ScheduleStatus = new List<TimelineStatus>();

                        //schedule status
                        if (scheduleStatusData.FirstOrDefault().ScheduleStatusDate != DateTime.MinValue)
                        {
                            scheduleStatusData = scheduleStatusData.OrderBy(x => x.ScheduleStatusDate);
                            var lastStatus = scheduleStatusData.Where(x => x.InterviewId == firstData.InterviewId).LastOrDefault();
                            if (lastStatus.ScheduleStatusValue == "Meeting Status" || lastStatus.ScheduleStatusValue == "Interview Status")
                            {
                                candidateTimeline.CurrentStatus = lastStatus.ScheduleStatusComments;
                                candidateTimeline.CurrentStatusDate = lastStatus.ScheduleStatusDate;
                            }
                            else
                            {
                                candidateTimeline.CurrentStatus = lastStatus.ScheduleStatusValue;
                                candidateTimeline.CurrentStatusDate = lastStatus.ScheduleStatusDate;
                            }
                        }
                        if (candidateTimeline.CurrentStatus == CandidateStatusOptions.QCPending.GetDescription())
                        {
                            if (firstData.CurrentInterviewStatus.HasValue && firstData.CurrentInterviewStatus.Value == (int)InterviewStatus.BorderLine && firstData.CurrentInterviewStatus.Value == (int)InterviewStatus.Selected)
                            {
                                candidateTimeline.CurrentStatus = ((InterviewStatus)firstData.CurrentInterviewStatus).GetDescription();
                            }
                            else if (firstData.CurrentMeetingStatus.HasValue)
                            {
                                candidateTimeline.CurrentStatus = ((MeetingStatus)firstData.CurrentMeetingStatus).GetDescription();
                            }
                        }
                        if (firstData.confirmationStatus != 2)
                        {
                            if (firstData.interviewDate < DateTime.Now && firstData.CurrentMeetingStatus == 2)
                            {
                                candidateTimeline.CurrentStatus = "Status Pending";
                            }
                            else if (firstData.interviewDate < DateTime.Now && candidateTimeline.CurrentStatus == "Scheduled")
                            {
                                if (firstData.CurrentMeetingStatus != null)
                                    candidateTimeline.CurrentStatus = ((MeetingStatus)firstData.CurrentMeetingStatus).GetDescription();
                            }
                            else if (firstData.CurrentMeetingStatus == 2)
                            {
                                candidateTimeline.CurrentStatus = "Scheduled";
                            }
                        }
                        else
                        {
                            candidateTimeline.CurrentStatus = "Cancelled";
                        }
                        //add candidate to final model
                        model.Add(candidateTimeline);
                    }

                    var role = await _clientService.GetClientRoleByEmailId(emailid);
                    ViewBag.ClientRole = role != 0 ? ((ClientRoleEnum)Convert.ToInt32(role)).GetDescription() : string.Empty;
                    ViewBag.profile = "Client";
                    ViewBag.PageStatus = "Public";
                    if (Emailid != emailid)
                    {
                        return RedirectToAction("PublicAllMeetings", new { id = id });
                    }
                    var Whatsapp = _userService.GetWhatsappno(emailid);
                    if (Whatsapp == null)
                    {
                        ViewBag.Whatsappflag = true;
                    }
                    else
                    {
                        ViewBag.Whatsappflag = false;
                    }
                    model = model.OrderByDescending(x => x.interviewDate).ToList();
                    return View(model);
                }
                else
                {
                    return RedirectToAction("PublicCandidateStatus", new { emailid = emailid });
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("PublicCandidateStatus", new { emailid = emailid });
            }
        }

        [HttpPost]
        public async Task<IActionResult> PublicCandidateStatus(CandidateTimelineFilter filter, string emailid, string id)
        {
            ViewBag.emailid = emailid;
            ViewBag.id = id;
            ViewBag.Whatsappflag = false;
            try
            {
                var client = await _userRepository.GetClientUserDetails(emailid);
                var model = new List<CandidateTimelineVM>();
                if (filter != null)
                {
                    HttpContext.Session.SetString("PublicCandidateStatusfilters", JsonConvert.SerializeObject(filter));
                }
                var candidateData = new List<CandidateTimelineData>();
                int datatype = !string.IsNullOrEmpty(filter.DateType) ? Convert.ToInt32(filter.DateType) : 2;
                if (filter.StartDate != null && filter.EndDate != null)
                {
                    candidateData = (await _candidateService.GetCandidateTimelinebyDateStatus(filter.StartDate.Value, filter.EndDate.Value.AddDays(1), datatype, emailid)).ToList();
                }
                else
                {
                    candidateData = (await _candidateService.GetCandidateTimelinebyDateStatus(DateTime.Now.AddDays(-7), DateTime.Now.AddDays(7), datatype, emailid)).ToList();
                }
                var distinctCandidates = candidateData.Select(x => (x.IRFId, x.CandidateId)).Distinct();
                foreach (var candidate in distinctCandidates)
                {
                    var firstData = new CandidateTimelineData();
                    var data = candidateData.Where(x => (x.CandidateId == candidate.CandidateId && x.IRFId == candidate.IRFId));
                    var scheduleStatusData = data.Select(x => (x.ScheduleStatusDate, x.ScheduleStatusValue, x.ScheduleStatusComments, x.InterviewId)).Distinct();
                    var lastvalue = scheduleStatusData.OrderBy(x => x.ScheduleStatusDate).LastOrDefault();
                    if (data.Where(x => x.confirmationStatus == 1).Any() || data.Where(x => x.confirmationStatus == 2).Any())
                    {
                        firstData = data.Where(x => x.confirmationStatus == 1 || x.confirmationStatus == 2).OrderByDescending(x => x.interviewDate).FirstOrDefault();
                    }
                    else
                    {
                        firstData = data.FirstOrDefault();
                    }
                    //if (data.Select(x => x.interviewDate).Count() > 1)
                    //{
                    //    firstData = data.Where(x => x.InterviewId == lastvalue.InterviewId).FirstOrDefault();
                    //}
                    var _datavalue = data.Select(x => x.InterviewId).Distinct().Count();
                    var candidateTimeline = new CandidateTimelineVM();
                    candidateTimeline.IsQCRequired = client.IsQCRequired;
                    candidateTimeline.IsRedoInterview = client.IsRedoInterview;
                    candidateTimeline.IsClientCancelInterview = client.IsClientCancelInterview;
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
                    candidateTimeline.ProfileSource = firstData.ProfileSource;
                    candidateTimeline.IsIRFArchived = firstData.IsIRFArchived;
                    candidateTimeline.Phone = firstData.MobileNumber;
                    candidateTimeline.interviewDate = firstData.interviewDate;
                    candidateTimeline.IRFname = firstData.IRFName;
                    candidateTimeline.imid = firstData.imid;
                    candidateTimeline.CalendlyLink = firstData.CalendlyLink;
                    candidateTimeline.ClientAction = firstData.ClientAction;
                    candidateTimeline.NickName = firstData.InterviewerNickName;
                    candidateTimeline.reportcount = _datavalue;
                    candidateTimeline.Interviewer = firstData.Interviewer;
                    candidateTimeline.ScheduleStatus = new List<TimelineStatus>();
                    candidateTimeline.WhatsappCount = firstData.WhatsappCount;
                    candidateTimeline.MailCount = firstData.MailCount;
                    candidateTimeline.CallsCount = firstData.CallsCount;
                    //schedule status

                    if (scheduleStatusData.FirstOrDefault().ScheduleStatusDate != DateTime.MinValue)
                    {
                        scheduleStatusData = scheduleStatusData.OrderBy(x => x.ScheduleStatusDate);
                        var lastStatus = scheduleStatusData.Where(x => x.InterviewId == firstData.InterviewId).LastOrDefault();
                        if (lastStatus.ScheduleStatusValue == "Meeting Status" || lastStatus.ScheduleStatusValue == "Interview Status")
                        {
                            candidateTimeline.CurrentStatus = lastStatus.ScheduleStatusComments;
                            candidateTimeline.CurrentStatusDate = lastStatus.ScheduleStatusDate;
                        }
                        else
                        {
                            candidateTimeline.CurrentStatus = lastStatus.ScheduleStatusValue;
                            candidateTimeline.CurrentStatusDate = lastStatus.ScheduleStatusDate;
                        }
                    }
                    if (candidateTimeline.CurrentStatus == CandidateStatusOptions.QCPending.GetDescription())
                    {
                        if (firstData.CurrentInterviewStatus.HasValue && firstData.CurrentInterviewStatus.Value == (int)InterviewStatus.BorderLine && firstData.CurrentInterviewStatus.Value == (int)InterviewStatus.Selected)
                        {
                            //candidateTimeline.CurrentStatus = candidateTimeline.CurrentStatus + "(" + ((InterviewStatus)firstData.CurrentInterviewStatus).GetDescription() + ")";
                            candidateTimeline.CurrentStatus = ((InterviewStatus)firstData.CurrentInterviewStatus).GetDescription();
                        }
                        else if (firstData.CurrentMeetingStatus.HasValue)
                        {
                            candidateTimeline.CurrentStatus = ((MeetingStatus)firstData.CurrentMeetingStatus).GetDescription();
                        }
                    }
                    if (firstData.confirmationStatus != 2)
                    {
                        if (firstData.interviewDate < DateTime.Now && firstData.CurrentMeetingStatus == 2)
                        {
                            candidateTimeline.CurrentStatus = "Status Pending";
                        }
                        else if (firstData.interviewDate < DateTime.Now && candidateTimeline.CurrentStatus == "Scheduled")
                        {
                            if (firstData.CurrentMeetingStatus != null)
                                candidateTimeline.CurrentStatus = ((MeetingStatus)firstData.CurrentMeetingStatus).GetDescription();
                        }
                        else if (firstData.CurrentMeetingStatus == 2)
                        {
                            candidateTimeline.CurrentStatus = "Scheduled";
                        }
                    }
                    else
                    {
                        candidateTimeline.CurrentStatus = "Cancelled";
                    }
                    //add candidate to final model
                    model.Add(candidateTimeline);
                }
                filter.HREmail = filter.HREmail == null ? filter.HREmail = model.Select(x => x.PrimarySkill).ToArray() : filter.HREmail = filter.HREmail;
                filter.ProfileSource = filter.ProfileSource == null ? filter.ProfileSource = model.Select(x => x.ProfileSource).ToArray() : filter.ProfileSource = filter.ProfileSource;
                filter.MeetingStatus = filter.MeetingStatus == null ? filter.MeetingStatus = model.Select(x => x.CurrentStatus).ToArray() : filter.MeetingStatus = filter.MeetingStatus;
                if (filter.HREmail != null)
                {
                    model = model.Where(x => filter.HREmail.Contains(x.PrimarySkill)).ToList();
                }
                if (filter.ProfileSource != null)
                {
                    model = model.Where(x => filter.ProfileSource.Contains(x.ProfileSource)).ToList();
                }
                if (filter.MeetingStatus != null)
                {
                    model = model.Where(x => filter.MeetingStatus.Contains(x.CurrentStatus)).ToList();
                }
                var role = await _clientService.GetClientRoleByEmailId(emailid);
                ViewBag.ClientRole = role != 0 ? ((ClientRoleEnum)Convert.ToInt32(role)).GetDescription() : string.Empty;
                ViewBag.profile = "Client";
                ViewBag.PageStatus = "Public";
                model = model.OrderByDescending(x => x.interviewDate).ToList();
                return View(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("PublicCandidateStatus", new { emailid = emailid });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PublicAllMeetings(string id)
        {
            var model = new List<CandidateTimelineVM>();
            ViewBag.PageType = "Public";
            ViewBag.Id = id;
            ViewBag.Status = "Client";
            ViewBag.Whatsappflag = false;
            var menuData = await _userService.GetMenuBasedOnUserEmailld("ExternalUser");
            var menus = menuData.GroupBy(p => p.functionName).Select(g => g.First()).ToList();
            if (menus != null)
            {
                menus = menus.Where(x => x.functionName != "Support" && x.functionName != "Common Pool" && x.functionName != "Risetube").ToList();
                HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
            }
            return View("PublicCandidateStatus", model);
        }

        [HttpPost]
        public async Task<JsonResult> PublicAllMeetings(string id, string Emailid)
        {
            //HttpContext.Session.SetString("PublicUrlAccessor", JsonConvert.SerializeObject(Emailid));
            var currentUrl = Request.Scheme + "://" + Request.Host.Value + Request.Path;
            _trackPublicUrlAccessor.SendTelemetry(JsonConvert.SerializeObject(Emailid), currentUrl);
            HttpContext.Session.SetString("email", JsonConvert.SerializeObject(Emailid));
            HttpContext.Session.SetString("profileName", "Client");
            var role = await _clientService.GetUniqueClientRoleByEmailId(Emailid, id);
            var logovalue = await _clientService.Fetchlogoforclient(Emailid, id);
            if (string.IsNullOrEmpty(logovalue))
            {
                HttpContext.Session.SetString("Logo", "");
            }
            else
                HttpContext.Session.SetString("Logo", logovalue);
            if (role > 0)
            {
                return Json("success");
            }
            else
            {
                return Json("Emailid don't have access.");
            }
        }

        [HttpGet]
        public async Task<FileContentResult> PublicCandidateStatusExporttoExcel(string emailid)
        {
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Candidate Status");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_Candidate_Status" + ".xlsx";
            try
            {
                var currentUrl = Request.Scheme + "://" + Request.Host.Value + Request.Path;
                _trackPublicUrlAccessor.SendTelemetry(JsonConvert.SerializeObject(emailid), currentUrl);
                var model = new List<CandidateTimelineVM>();
                var candidateData = new List<CandidateTimelineData>();
                if (HttpContext.Session.GetString("PublicCandidateStatusfilters") != null)
                {
                    var filter = JsonConvert.DeserializeObject<CandidateTimelineFilter>(HttpContext.Session.GetString("PublicCandidateStatusfilters"));
                    int datatype = !string.IsNullOrEmpty(filter.DateType) ? Convert.ToInt32(filter.DateType) : 2;
                    if (filter.StartDate != null && filter.EndDate != null)
                    {
                        candidateData = (await _candidateService.GetCandidateTimelinebyDateStatus(filter.StartDate.Value, filter.EndDate.Value.AddDays(1), datatype, emailid)).ToList();
                    }
                    else
                    {
                        candidateData = (await _candidateService.GetCandidateTimelinebyDateStatus(DateTime.Now.AddDays(-7), DateTime.Now.AddDays(7), datatype, emailid)).ToList();
                    }
                }
                else
                {
                    //candidateData = (await _candidateService.GetCandidateTimelinebyDate(DateTime.Now.AddDays(-30), DateTime.Now, emailid)).ToList();
                    candidateData = (await _candidateService.GetCandidateTimelinebyDateStatus(DateTime.Now.AddDays(-7), DateTime.Now.AddDays(7), 2, emailid)).ToList();
                }

                var distinctCandidates = candidateData.Select(x => (x.IRFId, x.CandidateId)).Distinct();
                foreach (var candidate in distinctCandidates)
                {
                    var firstData = new CandidateTimelineData();
                    var data = candidateData.Where(x => (x.CandidateId == candidate.CandidateId && x.IRFId == candidate.IRFId));
                    if (data.Where(x => x.confirmationStatus == 1).Any() || data.Where(x => x.confirmationStatus == 2).Any())
                    {
                        firstData = data.Where(x => x.confirmationStatus == 1 || x.confirmationStatus == 2).OrderByDescending(x => x.interviewDate).FirstOrDefault();
                    }
                    else
                    {
                        firstData = data.FirstOrDefault();
                    }
                    firstData = data.FirstOrDefault();

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
                    candidateTimeline.ProfileSource = firstData.ProfileSource;
                    candidateTimeline.IsIRFArchived = firstData.IsIRFArchived;
                    candidateTimeline.IRFName = firstData.IRFName;
                    candidateTimeline.Phone = firstData.Phone;
                    candidateTimeline.interviewDate = firstData.interviewDate;
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
                            candidateTimeline.CurrentStatus = lastStatus.ScheduleStatusComments;
                            candidateTimeline.CurrentStatusDate = lastStatus.ScheduleStatusDate;
                        }
                        else
                        {
                            candidateTimeline.CurrentStatus = lastStatus.ScheduleStatusValue;
                            candidateTimeline.CurrentStatusDate = lastStatus.ScheduleStatusDate;
                        }
                    }
                    if (!string.IsNullOrEmpty(candidateTimeline.CurrentStatus))
                    {
                        model.Add(candidateTimeline);
                    }
                }
                if (HttpContext.Session.GetString("PublicCandidateStatusfilters") != null)
                {
                    var filter = JsonConvert.DeserializeObject<CandidateTimelineFilter>(HttpContext.Session.GetString("PublicCandidateStatusfilters"));
                    filter.HREmail = filter.HREmail == null ? filter.HREmail = model.Select(x => x.PrimarySkill).ToArray() : filter.HREmail = filter.HREmail;
                    filter.ProfileSource = filter.ProfileSource == null ? filter.ProfileSource = model.Select(x => x.ProfileSource).ToArray() : filter.ProfileSource = filter.ProfileSource;
                    filter.MeetingStatus = filter.MeetingStatus == null ? filter.MeetingStatus = model.Select(x => x.CurrentStatus).ToArray() : filter.MeetingStatus = filter.MeetingStatus;
                    if (filter.HREmail != null)
                    {
                        model = model.Where(x => filter.HREmail.Contains(x.PrimarySkill)).ToList();
                    }
                    if (filter.ProfileSource != null)
                    {
                        model = model.Where(x => filter.ProfileSource.Contains(x.ProfileSource)).ToList();
                    }
                    if (filter.MeetingStatus != null)
                    {
                        model = model.Where(x => filter.MeetingStatus.Contains(x.CurrentStatus)).ToList();
                    }
                }
                int role = 0;
                role = await _clientService.GetClientRoleByEmailId(emailid);
                Sheet.Cells["A1:S1"].Style.Font.Bold = true;
                Sheet.Cells["A1:S1"].Style.Font.Size = 12;
                Sheet.Cells["A1:S1"].Style.WrapText = true;
                int startColumn = 1; int endColumn = 12;
                for (int a = startColumn; a <= endColumn; a++)
                {
                    Sheet.Column(a).Width = 30;
                }
                Sheet.Cells[1, 1].Value = "Candidate name";
                Sheet.Cells[1, 2].Value = "Candidate EmailId";
                Sheet.Cells[1, 3].Value = "JD Title";
                //Sheet.Cells[1, 4].Value = "Candidated Added On/At";
                Sheet.Cells[1, 4].Value = "Interview Date";
                Sheet.Cells[1, 5].Value = "Interview Time";
                //Sheet.Cells[1,6].Value = "Schedule Status";
                Sheet.Cells[1, 6].Value = "Phone";
                Sheet.Cells[1, 7].Value = "Current Status";
                //Sheet.Cells[1, 8].Value = "Current Status Date";
                Sheet.Cells[1, 8].Value = "Staffing Agency EmailId";
                Sheet.Cells[1, 9].Value = "Skill Channel ID";
                Sheet.Cells[1, 10].Value = "Client Hr";
                Sheet.Cells[1, 11].Value = "Executive URL";

                int i = 2;
                if (model.Count() > 0)
                {
                    foreach (var item in model)
                    {
                        Sheet.Cells[i, 1].Value = item.CandidateName;
                        Sheet.Cells[i, 2].Value = item.CandidateEmailId;
                        Sheet.Cells[i, 3].Value = item.PrimarySkill;
                        //DateTime datetime = DateTime.Parse(item.interviewDate);
                        // var s_today = datetime.ToShortDateString();
                        Sheet.Cells[i, 4].Value = item.interviewDate.ToShortDateString();
                        //string timeString = datetime.ToLongTimeString();
                        Sheet.Cells[i, 5].Value = item.interviewDate.ToLongTimeString();
                        //if (item.ScheduleStatus != null && item.ScheduleStatus.Count() != 0)
                        //{                           
                        //    if (item.ScheduleStatus.Where(x => x.Status == MeetingStatus.Scheduled.GetDescription()).Any())
                        //    {
                        //        Sheet.Cells[i, 6].Value = ScheduleStatusTypes.Scheduled.GetDescription();
                        //    }
                        //    else
                        //    {
                        //        Sheet.Cells[i, 6].Value = ScheduleStatusTypes.NotYetScheduled.GetDescription();
                        //    }
                        //}
                        //else
                        //{                           
                        //    Sheet.Cells[i, 6].Value = ScheduleStatusTypes.NotYetScheduled.GetDescription();
                        //}
                        Sheet.Cells[i, 6].Value = item.Phone ?? string.Empty;
                        if (!string.IsNullOrEmpty(item.CurrentStatus))
                        {
                            Sheet.Cells[i, 7].Value = item.CurrentStatus;
                            //Sheet.Cells[i, 8].Value = item.CurrentStatusDate.HasValue && item.CurrentStatusDate.Value != DateTime.MinValue ? item.CurrentStatusDate.Value.ToString() : string.Empty;
                        }
                        else
                        {
                            Sheet.Cells[i, 7].Value = string.Empty;
                            //Sheet.Cells[i, 8].Value = string.Empty;
                        }
                        if (!string.IsNullOrEmpty(item.ProfileSource))
                        {
                            Sheet.Cells[i, 8].Value = item.ProfileSource;
                        }
                        else
                        {
                            Sheet.Cells[i, 8].Value = string.Empty;
                        }
                        Sheet.Cells[i, 9].Value = item.IRFName;
                        Sheet.Cells[i, 10].Value = item.HREmail;
                        var executiveurl = "https://portal.risebird.io/Interview/ExecutiveReport?interviewId=" + item.InterviewId + "&irfId=" + item.IRFId + "&isClient=true";
                        try
                        {
                            Sheet.Cells[i, 11].Hyperlink = new Uri(executiveurl, UriKind.Absolute);
                            Sheet.Cells[i, 11].Value = "Executive Report";
                            Sheet.Cells[i, 11].Style.Font.UnderLine = true;
                            Sheet.Cells[i, 11].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                        }
                        catch (Exception)
                        {
                            Sheet.Cells[i, 11].Value = "No Report";
                        }

                        i++;
                    }
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else
                {
                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                    Rng.Value = "No data available!";
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
        public async Task<IActionResult> ResetPublicCandidateTimelineFilter(string emailid, string id)
        {
            try
            {
                HttpContext.Session.Remove("PublicCandidateStatusfilters");
                return RedirectToAction("PublicCandidateStatus", new { emailid = emailid, id = id });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("PublicCandidateStatus", new { emailid = emailid, id = id });
            }

        }

        [HttpGet]
        public async Task<IActionResult> PublicAllMeetingsAdmin()
        {
            var model = new List<CandidateTimelineVM>();
            ViewBag.PageType = "Public";
            ViewBag.Status = "Admin";
            var menuData = await _userService.GetMenuBasedOnUserEmailld("ExternalUser");
            var menus = menuData.GroupBy(p => p.functionName).Select(g => g.First()).ToList();
            if (menus != null)
            {
                menus = menus.Where(x => x.functionName != "Support" && x.functionName != "Common Pool" && x.functionName != "Risetube").ToList();
                HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
            }
            return View("PublicCandidateStatus", model);
        }

        [HttpPost]
        public async Task<JsonResult> PublicAllMeetingsAdmin(string Emailid)
        {
            var role = await _clientService.GetClientRoleByEmailId(Emailid);
            //var currentUrl = Request.Scheme + "://" + Request.Host.Value + Request.Path;
            //_trackPublicUrlAccessor.SendTelemetry(JsonConvert.SerializeObject(Emailid), currentUrl);
            if (role > 0)
            {
                return Json("success");
            }
            else
            {
                return Json("Emailid don't have access.");
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<JsonResult> PublicSendInvite(string irfId, string IRFName, string emailId, string logiduser)
        {
            try
            {
                if (await _irfService.SendInvite(Convert.ToInt32(irfId), emailId, logiduser))
                {
                    return Json("Invitation has been sent Successfully!");
                }
                else
                {
                    return Json("Sorry! Could not send invite!");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Sorry! Could not send invite!");
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<JsonResult> PublicCancelInterview(int id, DateTime interviewDate, string candidateEmail, string emailId, int IRFId, string zoomMeetingId = null)
        {
            try
            {
                if (await _meetingService.PublicCancelInterview(id, emailId))
                {
                    var candidateId = await _irfRepository.GetCandidateIdByEmail(candidateEmail);
                    var updatedStatus = await _irfRepository.UpdateCancellationSource(id, emailId);
                    var result1 = await _irfService.AddCandidateStatus(emailId, candidateId, IRFId, "Candidate Cancelled", (int)CandidateStatusOptions.Cancelled, id);
                    var result = await _candidateService.FetchChannelOwnerandInterviewer(id);
                    var irfOwnersAndRestrictedIrfOwner = await _candidateService.GetCandidateStaffingAgencyAndClientHRByInterviewId(id);
                    List<string> interviewers = new List<string>();
                    if (!string.IsNullOrEmpty(result.interviewer))
                    {
                        interviewers.Add(result.interviewer);
                    }
                    if (!string.IsNullOrEmpty(result.EmailId))
                    {
                        interviewers.Add(result.EmailId);
                    }
                    var emailresult = await _notificationHelper.SendInterviewCancelledMail(interviewers, result.interviewDate.ToString(), irfOwnersAndRestrictedIrfOwner?.ToList(), IRFId, id);
                    var emailresult1 = await _notificationHelper.SendInterviewCancelledMailClient(result.interviewDate.ToString(), result.CandidateName, irfOwnersAndRestrictedIrfOwner?.ToList(), IRFId, id);
                    var emailresult2 = await _notificationHelper.SendInterviewCancelledMailCandidate(result.candidateEmail, result.interviewDate.ToString(), result.CandidateName, IRFId, id);

                    if (!string.IsNullOrEmpty(zoomMeetingId))
                    {
                        DeleteMeetingLink(zoomMeetingId);
                    }

                    return Json("Invitation has been cancelled Successfully!");
                }
                else
                {
                    return Json("Sorry! Could not cancel invite!");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Sorry! Could not cancel invite!");
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<JsonResult> FetchSchedulingLinks(string emailid)
        {
            try
            {
                var model = await _meetingService.FetchSchedulingLinks(emailid);
                if (model != null)
                {
                    return Json(model);
                }
                return Json("");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("");
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<JsonResult> PublicStatusList(int irfId, int candidateId)
        {
            try
            {
                var model = new CandidateStatusListVM();
                model.CandidateStatusList = await _irfService.GetCandidatesStatusList(irfId, candidateId);
                model.CandidateId = candidateId;
                model.IRFId = irfId;
                return Json(model);
            }
            catch (Exception)
            {
                return Json("Failed");
            }
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> FetchInterviewerCommunicator(string emailid, string id)
        {
            var model = new List<SchedulingLinkVM>();
            try
            {
                ViewBag.id = id;
                ViewBag.emailid = emailid;
                model = await _meetingService.FetchSchedulingLinks(emailid);
                if (model != null)
                {
                    return View(model);
                }
                return View(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return View(model);
            }
        }
        public string DeleteMeetingLink(string meetingid)
        {
            string token = ZoomToken();
            var client = new RestClient(string.Concat(_configuration["ZoomAppDetails:endPoint"], $"meetings/" + meetingid));
            var request = new RestRequest(string.Concat(_configuration["ZoomAppDetails:endPoint"], $"meetings/" + meetingid), Method.Delete);
            request.AddHeader("content-type", "application/json");
            request.AddHeader("authorization", "Bearer " + token);
            RestResponse response = client.Execute(request);
            var content = response.Content;
            return "Delete successfully";
        }
        public string ZoomToken()
        {
            DateTime Expiry = DateTime.UtcNow.AddDays(1);
            string ApiKey = _configuration["ZoomAppDetails:apiKey"];
            string ApiSecret = _configuration["ZoomAppDetails:secretKey"];
            int ts = (int)(Expiry - new DateTime(1970, 1, 1)).TotalSeconds;
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ApiSecret));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var header = new JwtHeader(credentials);
            var payload = new JwtPayload
            {
                { "iss", ApiKey},
                { "exp", ts },
            };
            var secToken = new JwtSecurityToken(header, payload);
            var handler = new JwtSecurityTokenHandler();
            var tokenString = handler.WriteToken(secToken);
            return tokenString;
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ProcessingResume(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        var result = await _candidateService.ProcessCandidateResume(id);
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
        [RequestFormLimits(MultipartBodyLengthLimit = 1048576000)]
        [RequestSizeLimit(1048576000)]
        public async Task<JsonResult> UploadResumesPublicly(UploadCandidateResumesViewModel model)
        {
            try
            {
                var emailId = HttpContext.Session.GetString("email")?.Trim();
                emailId = emailId.Replace("\"", "");
                if (model != null && Request.Form.Files.Count() > 0)
                {
                    model.IRFId = Convert.ToInt32(Request.Form["IRFId"].ToString());
                    model.IRFName = Request.Form["IRFName"].ToString();
                    var addedByUserId = _irfService.GetUserId(emailId);
                    if (addedByUserId > 0)
                    {
                        var resumeList = new List<CandidateResume>();
                        foreach (var file in Request.Form.Files)
                        {
                            var fileName = file.FileName.Replace(" ", "_");
                            FileInfo fi = new FileInfo(fileName);
                            if (fi.Extension.ToLower() == ".docx" || fi.Extension.ToLower() == ".doc" || fi.Extension.ToLower() == ".pdf")
                            {
                                var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(file);
                                if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                    || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                                {
                                    await _azureStorageHelper.InsertResumeBlob(fileName, "resumes", file, fi.Extension);
                                    var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                    var blob = containerRef.GetBlockBlobReference(fileName);
                                    string uploadedaudioFileURL = blob.Uri.ToString();
                                    string sasUrl = string.Empty;
                                    if (uploadedaudioFileURL == null)
                                    {
                                        return Json("Resume upload has failed.");
                                    }
                                    else
                                    {
                                        var sas = _sasUrlHelper.CreateSasUrl("resumes");
                                        sasUrl = uploadedaudioFileURL + "?" + sas;
                                        resumeList.Add(new CandidateResume { IRFId = model.IRFId, AddedBy = addedByUserId, ResumeLink = sasUrl });
                                    }
                                }
                                else
                                {
                                    return Json("Please select valid file for import(.pdf, .doc, .docx)!");
                                }
                            }
                            else
                            {
                                return Json("Please select valid file for import(.pdf, .doc, .docx)!");
                            }
                        }
                        var result = await _candidateService.AddResume(resumeList);
                        if (result)
                        {
                            return Json("Resume(s) has been uploaded Successfully!");
                        }
                        else
                        {
                            return Json("Failed to upload Resume(s)!");
                        }
                    }
                    else
                    {
                        return Json("Failed to upload Resume(s)!");
                    }
                }
                else
                {
                    return Json("Please select valid file for import(.pdf, .doc, .docx)!");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Failed to upload Resume(s)!");
            }
        }
        [HttpGet]
        public async Task<IActionResult> ResumesPublicly(int irfId, string irfName, string emailid, string id)
        {
            try
            {
                var model = new ResumeListViewModel();
                model.ResumeList = (await _candidateService.GetCandidatesResumes(irfId))?.ToList();
                model.ResumeList = model.ResumeList.OrderBy(x => x.IsProcessed).ToList();
                model.IRFName = irfName;
                model.IRFId = irfId;
                model.Profile = "Client";
                ViewBag.emailid = emailid;
                ViewBag.id = id;
                if (TempData["SuccessErrorMessageCandidateResume"] != null)
                {
                    model.Message = TempData["SuccessErrorMessageCandidateResume"].ToString();
                    TempData.Remove("SuccessErrorMessageCandidateResume");
                }
                ViewBag.backbutton = true;
                return View(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("FetchInterviewerCommunicator", new { emailid = emailid, id = id });
            }
        }
        [HttpGet]
        public async Task<IActionResult> AddPublicly(string irfId, string irfName, int Candidateesumeid, string emailids, string id)
        {
            try
            {
                var model = new Candidate
                {
                    IRFId = Convert.ToInt32(irfId),
                    IRFName = irfName
                };
                var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                                 select new { ID = (int)s, Name = s.GetDescription() };
                model.Heuristicsscorelist = new SelectList(Heuristics, "ID", "Name");
                model.profileName = "Client";
                model.ResumeLink = await _candidateService.FetchCandidateResumeById(Candidateesumeid);
                model.Candidateesumeid = Candidateesumeid;
                ViewBag.emailid = emailids;
                ViewBag.id = id;
                return PartialView("AddCandidate", model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("ResumesPublicly", new { irfId = irfId, irfName = irfName, emailid = emailids, id = id });
            }
        }
        [HttpPost]
        public async Task<JsonResult> PauseIRF(int irfId, string irfName)
        {
            try
            {
                if (await _irfService.PauseIRF(irfId, irfName))
                {
                    return Json("IRF has been paused Successfully!");
                }
                else
                {
                    return Json("Failed to pause IRF!");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("IRF pause Failed");
            }
        }
        [HttpPost]
        public JsonResult PlayIRF(int irfId, string irfName)
        {
            try
            {
                if (_irfService.PlayIRF(irfId, irfName))
                {
                    return Json("IRF has been enabled Successfully!");
                }
                else
                {
                    return Json("Failed to enable IRF!");
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("IRF play Failed");
            }
        }
        [HttpGet]
        public async Task<ActionResult> IRFFlagDetails(int id, int type, string irfName)
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
                        ViewBag.type = type;
                        var ClientRole = HttpContext.Session.GetString("ClientRole")?.Trim();
                        ClientRole = ClientRole.Replace("\"", "");
                        ViewBag.ClientRole = ClientRole;
                        DateTime startDate = DateTime.Now.AddDays(-30);
                        DateTime endDate = DateTime.Now.AddDays(7);
                        if (HttpContext.Session.GetString("SkillChannelDetailsFilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<SkillChannelDetailsFilter>(HttpContext.Session.GetString("SkillChannelDetailsFilters"));
                            startDate = filter.StartDate.Value;
                            endDate = filter.EndDate.Value;
                        }
                        var meetings = new List<InterviewDetailsVM>();
                        if (type == 27538 || type == 73532)
                        {
                            meetings = (await _irfService.FetchIRFCompletedInterviewDetails(id, type, startDate, endDate))?.ToList();
                        }
                        else if (type == 34657)
                        {
                            //Reschedule Interview
                            meetings = (await _irfService.FetchIRFCompletedInterviewDetails(id, type, startDate, endDate))?.ToList();
                        }
                        else if (type == 64868)
                        {
                            //Not Schedue Interview
                            meetings = (await _irfService.FetchIRFCompletedInterviewDetails(id, type, startDate, endDate))?.ToList();
                        }
                        meetings = meetings.Select(x =>
                        {
                            x.confirmationStatusStatusDesc = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription();
                            x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty;
                            x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty;
                            return x;
                        }).ToList();
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
                return RedirectToAction("Index");
            }
        }
        [HttpPost]
        public async Task<JsonResult> SendRemovalRequest(string message, int interviewerid, int totalinterview, int selectedinterview, int rejectedinterview, int irfid, string interviewer)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var _details = await _irfService.FetchIRFInterviewerDetailsById(irfid);
                var _supportadminlist = await _irfService.FetchSupportAdminEmailid();
                if (string.IsNullOrWhiteSpace(_details.ClientHRAttemptContact))
                {
                    _supportadminlist.Add(_details.ClientHRAttemptContact);
                }
                string Emailids = string.Join(";", _supportadminlist);
                Mail _mail = new Mail
                {
                    TO = Emailids,
                    CC = "talent@risebird.io",
                    Subject = "Interviewer Removal Request : " + _details.IRFId
                };
                var emailsend = await _notificationHelper.SendInteviewerRemovalNotification(_mail, totalinterview, selectedinterview, rejectedinterview, interviewer, _details.ClientName, _details.IRFId, emailid);
                if (emailsend.Contains("success"))
                {
                    return Json("Removal request succesfully sent.");
                }
                return Json("Removal request failed, please try again.");
            }
            else
            {
                return Json("Removal request failed, please try again.");
            }
        }
        [HttpPost]
        public async Task<JsonResult> Notification()
        {
            var emailid = HttpContext.Session.GetString("email")?.Trim();
            emailid = emailid.Replace("\"", "");
            var irfList = (await _irfService.GetAllIRFDetailsIRF(DateTime.Now.AddDays(-30), DateTime.Now.AddDays(7), "Client", emailid))?.ToList();
            var flags = 0;
            if (irfList != null)
            {
                flags = irfList.Select(x => x.Flags).Sum();
            }
            return Json(flags);
        }

        public async Task<IActionResult> GetCommunicationDetails(int interviewId, int communicationMedium)
        {
            try
            {
                var model = new CommunicationDetailsWrapperVM();
                var communicationList = await _candidateService.GetCandidateCommunicationDetails(interviewId, communicationMedium);
                model.CommunicationList = communicationList;
                model.CommunicationMedium = communicationMedium;
                return PartialView("GetCommunicationDetails", model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Error retrieving data! Please try again.");
            }
        }

    }
}
