using System;

namespace Risebird.Entities.ViewModel
{
    public class DriveIRFVM
    {
        public string IRFId { get; set; }
        public int IrfIntId { get; set; }
        public string PrimarySkill { get; set; }
        public string AssessmentGroupName { get; set; }
        public string ClientName { get; set; }
        public string EndClientName { get; set; }
        public int MAGId { get; set; }
        public int ExpectedMorning { get; set; }
        public int ExpectedEvening { get; set; }
        public int UnassignedMorning { get; set; }
        public int UnassignedEvening { get; set; }
        public int InterviewersInIRF { get; set; }
        public int InterviewersInMAG { get; set; }
        public int InterviewersInRelatedMAGs { get; set; }
        public string ConcurrentInterview { get; set; }
        public string ActionsTaken { get; set; }
        public DateTime WeekendStart { get; set; }
        public int ExpectedTotal => ExpectedMorning + ExpectedEvening;
    }
}
