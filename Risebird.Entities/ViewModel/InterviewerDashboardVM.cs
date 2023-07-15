namespace Risebird.Entities.ViewModel
{
    public class InterviewerDashboardVM
    {
        public int MyInterviewCount { get; set; }
        public int OpenInterviewCount { get; set; }
        public int PendingReportCount { get; set; }
        public string CodedName { get; set; }
        public int QCReportCount { get; set; }
        public bool IsResumePresent { get; set; }
        public int AvgInterviewTime { get; set; }
        public bool FirstTimePopupDisplayed { get; set; }

    }

}
