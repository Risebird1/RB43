using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class SelectedPriorityViewFilter
    {
        public string[] PrimarySkill { get; set; }
        public string[] CandidateName { get; set; }
        public string[] ClientHR { get; set; }
        public string[] InterviewDate { get; set; }
        public string[] ClientName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
