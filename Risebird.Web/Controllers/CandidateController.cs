using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.ViewModel;
using X.PagedList;

namespace Risebird.Web.Controllers
{
    public class CandidateController : Controller
    {

        private readonly ICandidateService _candidateService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
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
            if(profileName == "Admin" || profileName == "HR" || profileName == "Client")
            {
                var model = new List<CandidateTimelineVM>();
                var candidateData = await _candidateService.GetCandidateTimeline();
                if (candidateData == null)
                    return  View(model.ToPagedList(Page ?? 1, 10));
                var distinctCandidates = candidateData.Select(x => (x.IRFId, x.CandidateId)).Distinct();
                foreach (var candidate in distinctCandidates)
                {
                    var data = candidateData.Where(x => (x.CandidateId == candidate.CandidateId && x.IRFId == candidate.IRFId));
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
                    candidateTimeline.ScheduleStatus = new List<TimelineStatus>();
                    //onboarding 
                    var onboardingData = data.FirstOrDefault().OnboardingDate;
                    if (onboardingData != DateTime.MinValue)
                    {
                        var onboardingTimeLineObj = new TimelineStatus();
                        onboardingTimeLineObj.Date = onboardingData.ToString("dd");
                        onboardingTimeLineObj.Day = onboardingData.Date.ToString("dddd");
                        onboardingTimeLineObj.Status = "Onboarded";
                        onboardingTimeLineObj.StatusDate = onboardingData.ToString("MMMM yyyy");
                        //candidateTimeline.OnboardingDetails = onboardingTimeLineObj;
                        candidateTimeline.ScheduleStatus.Add(onboardingTimeLineObj);
                    }
                    //invitation attempts
                    var invitationData = data.Select(x => x.InvitationDate).Distinct();
                    if (invitationData.FirstOrDefault() != DateTime.MinValue)
                    {
                        invitationData = invitationData.OrderBy(x => x);
                        var attemptObjList = new List<TimelineStatus>();
                        //int y = 1;
                        foreach (var item in invitationData)
                        {
                            var attemptObj = new TimelineStatus();

                            attemptObj.Date = item.ToString("dd");
                            attemptObj.Day = item.Date.ToString("dddd");
                            attemptObj.Status = "Invitation Sent";
                            attemptObj.StatusDate = item.Date.ToString("MMMM yyyy");
                            attemptObjList.Add(attemptObj);
                            //y++;
                        }
                        //candidateTimeline.RisebirdAttempt = attemptObjList;
                        candidateTimeline.ScheduleStatus.AddRange(attemptObjList);
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
                            interviewStatusObjList.Add(interviewStatusObj);
                        }
                        candidateTimeline.InterviewStatus = interviewStatusObjList;
                    }
                    //current status 
                    //add candidate to final model
                    if(candidateTimeline.InterviewStatus != null || candidateTimeline.MeetingStatus!=null || (candidateTimeline.ScheduleStatus!=null && candidateTimeline.ScheduleStatus.Count()!=0) || !(string.IsNullOrEmpty(candidateTimeline.CurrentStatus)))
                    {
                        model.Add(candidateTimeline);

                    }
                }
                if (!String.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.ToUpper();
                    model = model.Where(x => (!String.IsNullOrEmpty(x.CandidateName) ? x.CandidateName.ToUpper().Contains(searchValue) : x.CandidateName == "") || (!String.IsNullOrEmpty(x.HREmail) ? x.HREmail.ToUpper().Contains(searchValue) : x.HREmail == "")).ToList();
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
                return View(model.ToPagedList(Page ?? 1, 10));
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }
        

    }
}
