namespace Risebird.Entities.ViewModel
{
    public class IRFInterviewerMembers
    {
        public int IRFId { get; set; }

        public string Type { get; set; }
        public string EmailId { get; set; }
        public string ResumeLink { get; set; }
        public int TotalInterviews { get; set; }
        public int TotalQCInterviews { get; set; }
        public string MobileNumber { get; set; }
        public string uniqueid { get; set; }
        public string InterviewerName { get; set; }
        public bool IsActive { get; set; }
        public bool IsAvailable { get; set; }
        public string AvailableStatus { get; set; }
    }
}
