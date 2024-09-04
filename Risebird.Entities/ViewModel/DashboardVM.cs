using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Entities.Data;
using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class DashboardVM
    {
        public IEnumerable<Meeting> Meetings { get; set; }
        public IEnumerable<DailyInterviewSlot> DailyInterviewSlots { get; set; }
        public string MeetingsData { get; set; }
        public string MeetingsDataLabels { get; set; }
        public int TotalCompletedMeetings { get; set; }
        public DashboardInterviewerCount InterviewerCountObj { get; set; }
        public MeetingResultCount MeetingResultCountObj { get; set; }
        public CandidatesCount CandidatesCountObj { get; set; }

        [BindProperty]
        //public List<IRF> IrfProgressList { get; set; }
        //public IEnumerable<IRF> IrfProgressList { get; set; }
        public IList<IRF> IrfProgressList { get; set; }
        public SelectList ProgressOptions { get; set; }
        public DateTime ProgressDate { get; set; }
        public DateTime ChannelrequestDate { get; set; }
        public SelectList AssassinationList { get; set; }
        public IList<ChannelManagementVM> ChannelRequest { get; set; }
        public NotUpdatedMeetings NotUpdatedMeetingsObj { get; set; }
        public IList<InterviewManagementVM> InterviewManagement { get; set; }
    }
    public class InterviewManagementVM
    {
        public int id { get; set; }
        public DateTime interviewDate { get; set; }
        public int MeetingStatus { get; set; }
        public int TotalCount { get; set; }
        public int IsMeetingNotificationSent { get; set; }
        public int IsRescheduleNotificationSent { get; set; }
        public int IsNotificationSentToEI { get; set; }
        public DateTime? Joinedon { get; set; }
        public DateTime? PerformedOn { get; set; }
        public bool EIAssigned { get; set; }
        public string confirmationStatus { get; set; }
        public string MeetingStatusStatusDesc { get; set; }
        public string interviewer { get; set; }
        public string candidateEmail { get; set; }
        public string ClientName { get; set; }
        public int irfIntId { get; set; }
        public DateTime? FirstJoinedon { get; set; }
        public DateTime? FirstPerformedOn { get; set; }
        public string candidateName { get; set; }
        public string MobileNumber { get; set; }
        public string IRFId { get; set; }
        public string ChannelOwner { get; set; }
        public string SchedulerUniqueid { get; set; }
        public DateTime LastInterviewDate { get; set; }
        public int LastMeetingStatus { get; set; }
        public string LastMeetingStatusStatusDesc { get; set; }
        public string resumeLink { get; set; }
        public string JDTiltle { get; set; }
        public string ZoomMeetingId { get; set; }
        public int CandidateId { get; set; }
        public int CandidateMasterId { get; set; }
    }
}
