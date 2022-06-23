namespace Risebird.Entities.Data
{
    public class Settings
    {
        public int InterviewerUnassignedMinutes { get; set; }
        public int AutoCancellationMailMinutes { get; set; }
        public int ClientReportingMailMinutes { get; set; }
        public int InterviewWindupReminderMinutes { get; set; }
        public int InterviewReportReminderMinutes { get; set; }
        public int QCCommentReminderMinutes { get; set; }
        public int QCFormdeductionRate { get; set; }

    }
}
