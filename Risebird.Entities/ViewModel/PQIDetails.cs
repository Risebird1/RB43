namespace Risebird.Entities.ViewModel
{
    public class PQIDetails
    {
        public int id { get; set; }
        public int UserId { get; set; }
        public string IRFId { get; set; }
        public string interviewer { get; set; }
        public string Client { get; set; }
        public int TotalInterview { get; set; }
        public int TotalCompletedMeetings { get; set; }
        public int TotalSelected { get; set; }
        public int selectionrate { get; set; }
        public int TotalRejected { get; set; }
        public int rejectionrate { get; set; }
        public string ChannelOwner { get; set; }
        public string CompanyName { get; set; }
        public string Clienthr { get; set; }
        public string JDTiltle { get; set; }
        public int NegativeRating { get; set; }

        public int TotalRedoCount { get; set; }
        public int TotallessComment { get; set; }
        public int TotallessQuestions { get; set; }
        public int TotalLatejoin { get; set; }
        public string NickName { get; set; }
        public string ShortListstatus { get; set; }
        public string ProjectUniqueid { get; set; }
        public int RedMarker { get; set; }
        public string AIVerdict { get; set; }
        public string ResumeLink { get; set; }
        public int TotalFuture { get; set; }
        public int TotalInterviewers { get; set; }
        public int BillingRate { get; set; }
        public bool IsActive { get; set; }
        public int TotalLessduration { get; set; }
        public string TranscriptVerdit { get; set; }
        public bool IsEmployed { get; set; }
        public bool AcknowledgementStatus { get; set; }
        public int VideoConfirmation { get; set; }
        public int RedMarkerStatus { get; set; }
    }
    public class ToalInterviewerInterviewDetails
    {
        public int id { get; set; }
        public string IRFId { get; set; }
        public int UserId { get; set; }
        public string interviewer { get; set; }
        public int TotalInterview { get; set; }
        public int TotalCompletedMeetings { get; set; }
        public int TotalSelectMeetings { get; set; }
        public int TotalRejectMeetings { get; set; }
        public string CompanyName { get; set; }
        public bool IsEmployed { get; set; }
        public bool Isavailable { get; set; }
        public string NickName { get; set; }
        public int RedMarker { get; set; }
        public string AIVerdict { get; set; }
        public string ResumeLink { get; set; }
        public int TotalFuture { get; set; }
        public int TranscriptVerdit { get; set; }
        public int SkillChannelFutureAssignedInterview { get; set; }
        public int TotalDeficitMeetings { get; set; }
        public string VideoKYC { get; set; }
    }
}
