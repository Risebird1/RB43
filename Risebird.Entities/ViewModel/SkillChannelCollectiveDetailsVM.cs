using System;

namespace Risebird.Entities.ViewModel
{
    public class SkillChannelCollectiveDetailsVM
    {
        public int IRFId { get; set; }
        public string type { get; set; }

        public int NeedClarity { get; set; }
        public int MissedCandidate { get; set; }

        public int HighSelectCount { get; set; }
        public int HighRejectCount { get; set; }
        public int HighNegCandidateRating { get; set; }
        public int ProbableNoShowCount { get; set; }
        public int NotProcessedResumeCount { get; set; }
        public int LatejoinCount { get; set; }
        public int UnassignedCount { get; set; }
        public int NoShowInterviewer { get; set; }
        public int ColdCandidate { get; set; }

        public bool CalibrationCall { get; set; }
        public bool DMApproved { get; set; }
        public int ReportPending { get; set; }
        public int RescheduleInterview { get; set; }
        public int QcPendingCount { get; set; }

        public int HighNegClientRating { get; set; }
        public int RedoRequests { get; set; }
        public int LessSelectComments { get; set; }
        public int LessRejectComments { get; set; }
        public int LessQuestions { get; set; }

        public int ProxyCount { get; set; }
        public string IRFName { get; set; }
        public string ProbableNoShowLatestStatus { get; set; }
        public string NotProcessedResumesLatestStatus { get; set; }
        public string MissedCandidatesLatestStatus { get; set; }
        public string LatejoinStatus { get; set; }
        public string UnassignedStatus { get; set; }
        public string NeedClarityStatus { get; set; }
        public string NoShowInterviewerStatus { get; set; }
        public string ColdCandidateStatus { get; set; }

        public string HighSelectionRateLatestStatus { get; set; }
        public string HighRejectionRateLatestStatus { get; set; }
        public string PotentialRiskByCandidateLatestStatus { get; set; }
        public string PotentialRiskByClientLatestStatus { get; set; }
        public string RedoRequestStatus { get; set; }
        public string LessSelectCommentStatus { get; set; }
        public string LessRejectCommentStatus { get; set; }
        public string LessQuestionsStatus { get; set; }

        public string ReportPendingStatus { get; set; }
        public string RescheduleStatus { get; set; }
        public string QcPendingStatus { get; set; }
        public int BouncedOrDroppedCount { get; set; }
        public string BouncedOrDroppedTaskLatestStatus { get; set; }
    }
    public class SkillChannelTaskSpeedDetailsVM
    {
        public DateTime interviewDate { get; set; }
        public string interviewer { get; set; }
        public string candidateEmail { get; set; }
        public int imid { get; set; }
        public int irfIntId { get; set; }
        public string CalendlyLink { get; set; }
        public string ResumeLink { get; set; }
        public int CandidateId { get; set; }
    }
    public class SkillChannelTaskCompletionDetailsVM
    {
        public int id { get; set; }
        public DateTime interviewDate { get; set; }
        public int MeetingStatus { get; set; }
        public int TotalCount { get; set; }
        public bool IsMeetingNotificationSent { get; set; }
        public bool IsRescheduleNotificationSent { get; set; }
        public DateTime? Joinedon { get; set; }
        public DateTime? PerformedOn { get; set; }
        public bool EIAssigned { get; set; }
        public string confirmationStatus { get; set; }
        public string MeetingStatusStatusDesc { get; set; }
        public string interviewer { get; set; }
        public string candidateEmail { get; set; }
        public int irfIntId { get; set; }
        public DateTime? FirstJoinedon { get; set; }
        public DateTime? FirstPerformedOn { get; set; }
        public string candidateName { get; set; }
        public string IRFId { get; set; }
        public string ChannelOwner { get; set; }
        public string SchedulerUniqueid { get; set; }
        public DateTime LastInterviewDate { get; set; }
        public int LastMeetingStatus { get; set; }
        public string LastMeetingStatusStatusDesc { get; set; }
        public string JDTiltle { get; set; }
        public int TotalLatejoin { get; set; }
        public DateTime? Createdon { get; set; }
    }
}
