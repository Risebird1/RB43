using System;

namespace Risebird.Entities.ViewModel
{
    public class AINegativeVerdictQCReports
    {
        //public int id { get; set; }
        public int InterviewId { get; set; }
        //public bool IsQCdone { get; set; }
        //public string CreatedBy { get; set; }
        //public DateTime? CreatedOn { get; set; }
        public int irfIntId { get; set; }
        public string confirmationStatus { get; set; }
        public int? MeetingStatus { get; set; }
        public string MeetingStatusStatusDesc { get; set; }
        public DateTime? interviewDate { get; set; }
        public int? InterviewStatus { get; set; }
        public string intervieweStatusStatDesc { get; set; }
        public string IRFId { get; set; }
        public string candidateName { get; set; }
        public string candidateEmail { get; set; }
        public string interviewer { get; set; }
        public string InterviewerMobileNumber { get; set; }
        public string ChannelOwner { get; set; }
        public string AudioTranscriptFileLink { get; set; }
        public bool IsAITranscriptAnalysisAttempted { get; set; }
        public bool IsAITranscriptAnalysisFailed { get; set; }
        public string AITranscriptVerdict { get; set; }
        public string AIQCVerdict { get; set; }
        public int AITranscriptAnalysisScore { get; set; }
        public string CandidateMobileNumber { get; set; }
        public int AIReportQCAnalysisScore { get; set; }
        //public intg AudioTranscriptFileLink { get; set; }
        //public int QCPresent { get; set; }
        public bool IsAIInterview { get; set; }
        public string SessionId { get; set; }
    }
}
