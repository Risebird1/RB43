using System;

namespace Risebird.Entities.ViewModel
{
    public class InterviewerSkillsFilter
    {
        public string[] ISID { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string[] ClientName { get; set; }
        public string[] ArchiveStatus { get; set; }
        public string[] ClientRole { get; set; }
    }
}
