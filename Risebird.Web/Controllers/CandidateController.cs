using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using X.PagedList;

namespace Risebird.Web.Controllers
{
    public class CandidateController : Controller
    {

        private readonly ICandidateService _candidateService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private static List<CandidateTimelineVM> _modelDetails;
        public CandidateController(ICandidateService candidateService, IApplicationErrorLogger applicationErrorLogger)
        {
            _candidateService = candidateService;
            _applicationErrorLogger = applicationErrorLogger;
        }
        public IActionResult Index()
        {
            return View();
        }
        public async Task<IActionResult> CandidateTimeline(int? Page, string searchValue)
        {
            var profileName = HttpContext.Session.GetString("profileName")?.Trim();
            profileName = profileName.Replace("\"", "");
            var emailid = HttpContext.Session.GetString("email")?.Trim();
            emailid = emailid.Replace("\"", "");
            if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
            {
                var model = new List<CandidateTimelineVM>();
                var candidateData = await _candidateService.GetCandidateTimeline(profileName == "Client" ? emailid : null);
                var distinctCandidates = candidateData.Select(x => (x.IRFId, x.CandidateId, x.Name)).Distinct().OrderBy(x => x.Name);
                foreach (var candidate in distinctCandidates)
                {
                    var data = candidateData.Where(x => (x.CandidateId == candidate.CandidateId && x.IRFId == candidate.IRFId));
                    var firstData = data.FirstOrDefault();

                    var condition = (firstData.CurrentMeetingStatus == 4) && (firstData.confirmationStatus.HasValue && firstData.confirmationStatus == 1) && ((firstData.CurrentInterviewStatus != 4 && firstData.CurrentInterviewStatus != 2) || (firstData.QCStatus == 1 && (firstData.CurrentInterviewStatus == 4 || firstData.CurrentInterviewStatus == 2)));
                    if (!condition)
                    {
                        continue;
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
                            var meetingStatusObj = new TimelineStatus();
                            meetingStatusObj.Date = item.MeetingStatusDate.ToString("dd");
                            meetingStatusObj.Day = item.MeetingStatusDate.Date.ToString("dddd");
                            meetingStatusObj.Status = item.MeetingStatus;
                            meetingStatusObj.StatusDate = item.MeetingStatusDate.Date.ToString("MMMM yyyy");
                            meetingStatusObj.Time = item.MeetingStatusDate.ToString("hh:mm tt");

                            meetingStatusObjList.Add(meetingStatusObj);
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


                if (!String.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.ToUpper();
                    model = model.Where(x => (!String.IsNullOrEmpty(x.CandidateName) ? x.CandidateName.ToUpper().Contains(searchValue) : x.CandidateName == "") || (!String.IsNullOrEmpty(x.CandidateEmailId) ? x.CandidateEmailId.ToUpper().Contains(searchValue) : x.CandidateEmailId == "")
                    || (!String.IsNullOrEmpty(x.HREmail) ? x.HREmail.ToUpper().Contains(searchValue) : x.HREmail == "") || (!String.IsNullOrEmpty(x.CurrentStatus) ? x.CurrentStatus.ToUpper().Contains(searchValue) : x.CurrentStatus == "")).ToList();


                }


                ViewBag.TotalCount = model.Count;

                ViewBag.PageStartCount = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                int a = model.Count / 10;
                if (model.Count % 10 == 0)
                {
                    ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                }
                else
                {
                    a = a + 1;
                    if (a == Page)
                    {
                        ViewBag.PageEndCount = model.Count;
                    }
                    else
                    {
                        ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));

                    }

                }
                if (model.Count == 0)
                {
                    ViewBag.PageStartCount = 0;
                    ViewBag.PageEndCount = 0;
                }
                _modelDetails = model;
                return View(model.ToPagedList(Page ?? 1, 10));
      
        }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }
        [HttpPost]
        public JsonResult CandidateTimelineJson(string searchValue)
        {
            int Page = 1;
            var model = _modelDetails;
            if (!String.IsNullOrEmpty(searchValue))
            {
                searchValue = searchValue.ToUpper();
                model = model.Where(x => (!String.IsNullOrEmpty(x.CandidateName) ? x.CandidateName.ToUpper().Contains(searchValue) : x.CandidateName == "") || (!String.IsNullOrEmpty(x.CandidateEmailId) ? x.CandidateEmailId.ToUpper().Contains(searchValue) : x.CandidateEmailId == "")
                  || (!String.IsNullOrEmpty(x.HREmail) ? x.HREmail.ToUpper().Contains(searchValue) : x.HREmail == "")|| (!String.IsNullOrEmpty(x.CurrentStatus) ? x.CurrentStatus.ToUpper().Contains(searchValue) : x.CurrentStatus == "")).ToList();

            }
            int totalcount = model.Count;
            int Pagestart = 0;
            int Pageend = 0;
            ViewBag.TotalCount = model.Count;

            Pagestart = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
            int a = model.Count / 10;
            if (model.Count % 10 == 0)
            {
                Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
            }
            else
            {
                a = a + 1;
                if (a == Page)
                {
                    Pageend = model.Count;
                }
                else
                {
                    Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));


                }

            }
            if (model.Count == 0)
            {
                Pagestart = 0;
                Pageend = 0;
            }

            return Json(new { result = model.Take(10), startPage = Pagestart, endPage = Pageend, totalPage = a, totalentry = model.Count });
        }

    }
}
