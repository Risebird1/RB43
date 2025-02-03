using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
//using OfficeOpenXml.Core.ExcelPackage;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System.Data;

namespace Risebird.AzureTimerFunctions
{
    public class CandidateStatusNotifierTurning
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private readonly ILogger log;
        private readonly ICandidateService _candidateService;

        private readonly string connectionString;

        public CandidateStatusNotifierTurning(ILoggerFactory loggerFactory, IConfiguration configuration, ICandidateService candidateService, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper)
        {
            _configuration = configuration;
            _candidateService = candidateService;
            _applicationErrorLogger = applicationErrorLogger;
            log = loggerFactory.CreateLogger<CandidateStatusNotifierTurning>();
            _notificationHelper = notificationHelper;
            connectionString = _configuration["ConnectionString:DBConnectionString"];

        }

        [Function("CandidateStatusNotifierTurning")]
        public async Task Run([TimerTrigger("0 0 0 * * *")] TimerInfo myTimer)//0 0 0 * * *
        {
            try
            {
                log.LogInformation($"C# Timer trigger CandidateStatusNotifierTurning executed at: {DateTime.Now}");

                var mailList = new List<string>() { "prem@risebird.io",
                    "amika.s@turing.com",
                    "annu.s@turing.com",
                    "ashutosh@risebird.io",
                    "danijel.j@turing.com",
                    "neha.khaitan@turing.com",
                    "parth.gohel@turing.com",
                    "priyanka.k@turing.com",
                    "soumya.m@turing.com",
                    "anabella.morales@turing.com"};
                ExcelPackage Ep = new ExcelPackage();
                ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Candidate Status");
                string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_Candidate_Status" + ".xlsx";
                var model = new List<CandidateTimelineVM>();
                var candidateData = new List<CandidateTimelineData>();
                candidateData = (await _candidateService.GetCandidateTimelinebyDate(DateTime.Now.AddDays(-1), DateTime.Now, "danijel.j@turing.com")).ToList();

                var distinctCandidates = candidateData.Select(x => (x.IRFId, x.CandidateId)).Distinct();
                foreach (var candidate in distinctCandidates)
                {
                    var data = candidateData.Where(x => (x.CandidateId == candidate.CandidateId && x.IRFId == candidate.IRFId));

                    var firstData = new CandidateTimelineData();


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
                Sheet.Cells[1, 8].Value = "Current Status";
                Sheet.Cells[1, 9].Value = "Current Status Date";
                Sheet.Cells[1, 10].Value = "Profile Source";
                Sheet.Cells[1, 11].Value = "Skill Channel ID";

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

                        i++;
                    }

                    fileName = fileName.Replace(" ", "_");
                    await _notificationHelper.SendCandidateLastStatusMail(mailList.ToList(), null, Convert.ToBase64String(Ep.GetAsByteArray()), fileName);

                }
                log.LogInformation($"C# Timer trigger CandidateStatusNotifierTurning completed at: {DateTime.Now}");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

    }
}
