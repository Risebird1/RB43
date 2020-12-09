using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.ViewModel;

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
        public async Task<IActionResult> CandidateTimeline()
        {
            var model = new List<CandidateTimelineVM>();
            //foreach (var item in Model.Select(x => (x.irfIntId, x.IRFId)).Distinct().ToList())
            var candidateData = await _candidateService.GetCandidateTimeline();
            var distinctCandidates = candidateData.Select(x => (x.IRFId, x.CandidateId)).Distinct();

            foreach (var candidate in distinctCandidates)
            {
                var data = candidateData.Where(x => (x.CandidateId == candidate.CandidateId && x.IRFId == candidate.IRFId));
                var firstData = data.FirstOrDefault();
                var candidateTimeline = new CandidateTimelineVM();

                candidateTimeline.HREmail = firstData.HREmail;
                candidateTimeline.CandidateName = firstData.Name;
                candidateTimeline.IRFId = firstData.IRFId;
                candidateTimeline.InterviewId = firstData.InterviewId;
                candidateTimeline.CurrentStatus = firstData.CurrentStatus!=null? firstData.CurrentStatus:string.Empty;
                candidateTimeline.CandidateMasterId = firstData.CandidateMasterId;

                //onboarding 
                var onboardingData = data.FirstOrDefault().OnboardingDate;
                //var onboardingData = DateTime.Now;
                //var minDate = DateTime.MinValue;
                if (onboardingData != DateTime.MinValue)
                {
                    var onboardingTimeLineObj = new TimelineStatus();
                    onboardingTimeLineObj.Date = onboardingData.ToString();
                    onboardingTimeLineObj.Day = onboardingData.Date.ToString("dddd");
                    onboardingTimeLineObj.Status = "Onboarded";
                    onboardingTimeLineObj.StatusDate = onboardingData.ToString("MMMM yyyy");

                    candidateTimeline.OnboardingDetails = onboardingTimeLineObj;
                }
                

                //candidateTimeline.OnboardingDetails.Date = onboardingData.Date.ToString();
                //candidateTimeline.OnboardingDetails.Day = onboardingData.Day.ToString("dddd");
                //candidateTimeline.OnboardingDetails.Status = "Onboarded";
                ////candidateTimeline.OnboardingDetails.StatusDate = onboardingData.ToString("dd MMMM, yyyy");
                //candidateTimeline.OnboardingDetails.StatusDate = onboardingData.ToString("MMMM yyyy");

                //invitation attempts

                var invitationData = data.Select(x => x.InvitationDate).Distinct();
                if(invitationData.FirstOrDefault()!=DateTime.MinValue)
                {
                    invitationData = invitationData.OrderBy(x => x);
                    var attemptObjList = new List<TimelineStatus>();
                    int y = 1;

                    foreach (var item in invitationData)
                    {
                        var attemptObj = new TimelineStatus();

                        attemptObj.Date = item.ToString();
                        attemptObj.Day = item.Date.ToString("dddd");
                        attemptObj.Status = "Attempt " + y.ToString();
                        attemptObj.StatusDate = item.Date.ToString("MMMM yyyy");
                        attemptObjList.Add(attemptObj);
                        y++;

                    }
                    candidateTimeline.RisebirdAttempt = attemptObjList;
                }
                

                //meeting status 

                var meetingStatusData = data.Select(x => (x.MeetingStatusDate, x.MeetingStatus)).Distinct();

                if(meetingStatusData.FirstOrDefault().MeetingStatusDate != DateTime.MinValue)
                {
                    meetingStatusData = meetingStatusData.OrderBy(x => x.MeetingStatusDate);
                    var meetingStatusObjList = new List<TimelineStatus>();

                    foreach (var item in meetingStatusData)
                    {
                        var meetingStatusObj = new TimelineStatus();

                        meetingStatusObj.Date = item.MeetingStatusDate.ToString();
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

                        interviewStatusObj.Date = item.InterviewStatusDate.ToString();
                        interviewStatusObj.Day = item.InterviewStatusDate.Date.ToString("dddd");
                        interviewStatusObj.Status = item.InterviewStatus;
                        interviewStatusObj.StatusDate = item.InterviewStatusDate.Date.ToString("MMMM yyyy");
                        interviewStatusObjList.Add(interviewStatusObj);


                    }
                    candidateTimeline.InterviewStatus = interviewStatusObjList;

                }


                //current status 


                //add candidate to final model
                model.Add(candidateTimeline);




            }

            

            return View(model);
        }


        //public IActionResult CandidateTimeline()
        //{


        //    var a = new List<TimelineStatus>();
        //    a.Add(new TimelineStatus
        //    {
        //        Status = "Onboarding",
        //        StatusDate = "April 2020",
        //        Date = "17",
        //        Day = "Wednesday"
        //    });


        //    a.Add(new TimelineStatus
        //    {
        //        Status = "First Attempt",
        //        StatusDate = "April 2020",
        //        Date = "19",
        //        Day = "Friday"
        //    });



        //    a.Add(new TimelineStatus
        //    {
        //        Status = "Second Attempt",
        //        StatusDate = "April 2020",
        //        Date = "23",
        //        Day = "Monday"
        //    });


        //    a.Add(new TimelineStatus
        //    {
        //        Status = "Third Attempt",
        //        StatusDate = "April 2020",
        //        Date = "25",
        //        Day = "Wednesday"
        //    });
        //    a.Add(new TimelineStatus
        //    {
        //        Status = "Fourth Attempt",
        //        StatusDate = "April 2020",
        //        Date = "27",
        //        Day = "Friday"
        //    });

        //    a.Add(new TimelineStatus
        //    {
        //        Status = "Fifth Attempt",
        //        StatusDate = "April 2020",
        //        Date = "30",
        //        Day = "Monday"
        //    });


        //    a.Add(new TimelineStatus
        //    {
        //        Status = "Sixth Attempt",
        //        StatusDate = "May 2020",
        //        Date = "1",
        //        Day = "Tuesday"
        //    });
        //    a.Add(new TimelineStatus
        //    {
        //        Status = "Seventh Attempt",
        //        StatusDate = "May 2020",
        //        Date = "3",
        //        Day = "Thursday"
        //    });


        //    a.Add(new TimelineStatus
        //    {
        //        Status = "Eight Attempt",
        //        StatusDate = "May 2020",
        //        Date = "4",
        //        Day = "Friday"
        //    });
        //    a.Add(new TimelineStatus
        //    {
        //        Status = "Nineth Attempt",
        //        StatusDate = "May 2020",
        //        Date = "7",
        //        Day = "Monday"
        //    });
        //    var b = new List<TimelineStatus>();
        //    b.Add(new TimelineStatus
        //    {
        //        Status = "Schedule",
        //        StatusDate = " May 2020",
        //        Date = "4",
        //        Day = "Monday"
        //    });
        //    b.Add(new TimelineStatus
        //    {
        //        Status = "Pending",
        //        StatusDate = "May 2020",
        //        Date = "7",
        //        Day = "Wednesday"
        //    });

        //    b.Add(new TimelineStatus
        //    {
        //        Status = "Completed",
        //        StatusDate = " May 2020",
        //        Date = "8",
        //        Day = "Thursday"
        //    });


        //    var c = new List<TimelineStatus>();
        //    c.Add(new TimelineStatus
        //    {
        //        Status = "BorderLine",
        //        StatusDate = " May 2020",
        //        Date = "2",
        //        Day = "Monday"

        //    });

        //    c.Add(new TimelineStatus
        //    {
        //        Status = "Selected",
        //        StatusDate = "May 2020",
        //        Date = "5",
        //        Day = "Wednesday"
        //    });


        //    var d = new List<TimelineStatus>();
        //    d.Add(new TimelineStatus
        //    {
        //        Status = "Onboarding",
        //        StatusDate = "April 2020",
        //        Date = "17",
        //        Day = "Wednesday"
        //    });


        //    d.Add(new TimelineStatus
        //    {
        //        Status = "First Attempt",
        //        StatusDate = "April 2020",
        //        Date = "19",
        //        Day = "Friday"
        //    });



        //    d.Add(new TimelineStatus
        //    {
        //        Status = "Second Attempt",
        //        StatusDate = "April 2020",
        //        Date = "23",
        //        Day = "Monday"
        //    });





        //    var e = new List<TimelineStatus>();
        //    e.Add(new TimelineStatus
        //    {
        //        Status = "Pending",
        //        StatusDate = "May 2020",
        //        Date = "5",
        //        Day = "Monday"

        //    });

        //    e.Add(new TimelineStatus
        //    {
        //        Status = "Completed",
        //        StatusDate = "May 2020",
        //        Date = "7",
        //        Day = "Wednesday"
        //    });


        //    var f = new List<TimelineStatus>();
        //    f.Add(new TimelineStatus
        //    {
        //        Status = "NoShow",
        //        StatusDate = "May 2020",
        //        Date = "12",
        //        Day = "Monday"
        //    });

        //    f.Add(new TimelineStatus
        //    {
        //        Status = "BorderLine",
        //        StatusDate = "May 2020",
        //        Date = "16",
        //        Day = "Thursday"
        //    });

        //    f.Add(new TimelineStatus
        //    {
        //        Status = "Rejected",
        //        StatusDate = "May 2020",
        //        Date = "17",
        //        Day = "Friday"
        //    });


        //    var g = new List<TimelineStatus>();
        //    g.Add(new TimelineStatus
        //    {
        //        Status = "Onboarding",
        //        StatusDate = "April 2020",
        //        Date = "27",
        //        Day = "Monday"
        //    });


        //    g.Add(new TimelineStatus
        //    {
        //        Status = "First Attempt",
        //        StatusDate = "April 2020",
        //        Date = "29",
        //        Day = "Wednesday"
        //    });



        //    var h = new List<TimelineStatus>();


        //    h.Add(new TimelineStatus
        //    {
        //        Status = "Schedule",
        //        StatusDate = "May 2020",
        //        Date = "7",
        //        Day = "Wednesday"
        //    });

        //    h.Add(new TimelineStatus
        //    {
        //        Status = "Completed",
        //        StatusDate = "May 2020",
        //        Date = "9",
        //        Day = "Friday"
        //    });



        //    var i = new List<TimelineStatus>();

        //    i.Add(new TimelineStatus
        //    {
        //        Status = "Selected",
        //        StatusDate = "May 2020",
        //        Date = "17",
        //        Day = "Friday"
        //    });




        //    var j = new List<TimelineStatus>();
        //    j.Add(new TimelineStatus
        //    {
        //        Status = "Onboarding",
        //        StatusDate = "April 2020",
        //        Date = "27",
        //        Day = "Monday"
        //    });


        //    j.Add(new TimelineStatus
        //    {
        //        Status = "First Attempt",
        //        StatusDate = "April 2020",
        //        Date = "29",
        //        Day = "Wednesday"
        //    });

        //    j.Add(new TimelineStatus
        //    {
        //        Status = "Second Attempt",
        //        StatusDate = "April 2020",
        //        Date = "29",
        //        Day = "Wednesday"
        //    });


        //    j.Add(new TimelineStatus
        //    {
        //        Status = "Third Attempt",
        //        StatusDate = "April 2020",
        //        Date = "29",
        //        Day = "Wednesday"
        //    });


        //    j.Add(new TimelineStatus
        //    {
        //        Status = "Fourth Attempt",
        //        StatusDate = "April 2020",
        //        Date = "29",
        //        Day = "Wednesday"
        //    });
        //    var k = new List<TimelineStatus>();


        //    k.Add(new TimelineStatus
        //    {
        //        Status = "Schedule",
        //        StatusDate = "May 2020",
        //        Date = "7",
        //        Day = "Wednesday"
        //    });

        //    k.Add(new TimelineStatus
        //    {
        //        Status = "Completed",
        //        StatusDate = "May 2020",
        //        Date = "9",
        //        Day = "Friday"
        //    });



        //    var l = new List<TimelineStatus>();

        //    l.Add(new TimelineStatus
        //    {
        //        Status = "Selected",
        //        StatusDate = "May 2020",
        //        Date = "17",
        //        Day = "Friday"
        //    });









        //    var m = new List<TimelineStatus>();
        //    m.Add(new TimelineStatus
        //    {
        //        Status = "Onboarding",
        //        StatusDate = "April 2020",
        //        Date = "27",
        //        Day = "Monday"
        //    });


        //    m.Add(new TimelineStatus
        //    {
        //        Status = "First Attempt",
        //        StatusDate = "April 2020",
        //        Date = "29",
        //        Day = "Wednesday"
        //    });

        //    m.Add(new TimelineStatus
        //    {
        //        Status = "Second Attempt",
        //        StatusDate = "April 2020",
        //        Date = "29",
        //        Day = "Wednesday"
        //    });


        //    m.Add(new TimelineStatus
        //    {
        //        Status = "Third Attempt",
        //        StatusDate = "April 2020",
        //        Date = "29",
        //        Day = "Wednesday"
        //    });


        //    m.Add(new TimelineStatus
        //    {
        //        Status = "Fourth Attempt",
        //        StatusDate = "April 2020",
        //        Date = "29",
        //        Day = "Wednesday"
        //    });




        //    var n = new List<TimelineStatus>();


        //    n.Add(new TimelineStatus
        //    {
        //        Status = "Schedule",
        //        StatusDate = "May 2020",
        //        Date = "7",
        //        Day = "Wednesday"
        //    });

        //    n.Add(new TimelineStatus
        //    {
        //        Status = "Completed",
        //        StatusDate = "May 2020",
        //        Date = "9",
        //        Day = "Friday"
        //    });





        //    var o = new List<TimelineStatus>();
        //    o.Add(new TimelineStatus
        //    {
        //        Status = "NoShow",
        //        StatusDate = "May 2020",
        //        Date = "12",
        //        Day = "Monday"
        //    });

        //    o.Add(new TimelineStatus
        //    {
        //        Status = "BorderLine",
        //        StatusDate = "May 2020",
        //        Date = "16",
        //        Day = "Thursday"
        //    });

        //    o.Add(new TimelineStatus
        //    {
        //        Status = "Dropped",
        //        StatusDate = "May 2020",
        //        Date = "17",
        //        Day = "Friday"
        //    });
        //    o.Add(new TimelineStatus
        //    {
        //        Status = "Rejected",
        //        StatusDate = "May 2020",
        //        Date = "20",
        //        Day = "Monday"
        //    });







        //    var model = new List<CandidateTimelineVM>();
        //    model.Add(new CandidateTimelineVM
        //    {
        //        CandidateName = "Lakshmi Tejasri",
        //        HREmail = "kamini@risebird.io",
        //        CurrentStatus = "Selected",
        //        Report = "Available",
        //        RisebirdAttempt = a,
        //        MeetingStatus = b,
        //        InterviewStatus = c

        //    });


        //    model.Add(new CandidateTimelineVM
        //    {
        //        CandidateName = "Omkar Patil",
        //        HREmail = "prasad_srinivas@risebird.io	",
        //        CurrentStatus = "Rejected",
        //        Report = "Not Available",
        //        RisebirdAttempt = d,
        //        MeetingStatus = e,
        //        InterviewStatus = f


        //    });

        //    model.Add(new CandidateTimelineVM
        //    {
        //        CandidateName = "Ashok Mannepalli",
        //        HREmail = "prasad_srinivas@risebird.io	",
        //        CurrentStatus = "Selected",
        //        Report = "Available",
        //        RisebirdAttempt = g,
        //        MeetingStatus = h,
        //        InterviewStatus = i


        //    });


        //    model.Add(new CandidateTimelineVM
        //    {
        //        CandidateName = "Srinivas Avoonuri",
        //        HREmail = "kamini@risebird.io",
        //        CurrentStatus = "Selected",
        //        Report = "Available",
        //        RisebirdAttempt = j,
        //        MeetingStatus = k,
        //        InterviewStatus = l


        //    });




        //    model.Add(new CandidateTimelineVM
        //    {
        //        CandidateName = "khushboo kumari",
        //        HREmail = "kamini@risebird.io",
        //        CurrentStatus = "Rejected",
        //        Report = "Available",
        //        RisebirdAttempt = m,
        //        MeetingStatus = n,
        //        InterviewStatus = o


        //    });



        //    //if (HttpContext.Session.GetString("CandidateTimeLineFilter") != null)
        //    //{
        //    //    var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("CandidateTimeLineFilter"));
        //    //    filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = model.Select(x => x.HREmail).ToArray() : filter.ClientHR = filter.ClientHR;
        //    //    filter.ConfirmationStatus = filter.ConfirmationStatus == null ? filter.ConfirmationStatus = model.Select(x => x.CurrentStatus).ToArray() : filter.ConfirmationStatus = filter.ConfirmationStatus;
        //    //    // filter.MeetingStatus = filter.MeetingStatus == null ? filter.MeetingStatus = model.Select(x => x.MeetingStatus).ToArray() : filter.MeetingStatus = filter.MeetingStatus;
        //    //    // filter.InterviewStatus = filter.InterviewStatus == null ? filter.InterviewStatus = model.Select(x => x.InterviewStatus).ToArray() : filter.InterviewStatus = filter.InterviewStatus;

        //    //    model = model.Where(x => ((filter.ClientHR != null && filter.ClientHR.Count() > 0) ? filter.ClientHR.Contains(x.HREmail) : x.HREmail == null)
        //    //     && ((filter.ConfirmationStatus != null && filter.ConfirmationStatus.Count() > 0) ? filter.ConfirmationStatus.Contains(x.CurrentStatus) : x.CurrentStatus == null)).ToList();


        //    //}
        //    return View(model);
        //}

    }
}
