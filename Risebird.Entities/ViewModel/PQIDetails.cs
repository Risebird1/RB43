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
    }
}
