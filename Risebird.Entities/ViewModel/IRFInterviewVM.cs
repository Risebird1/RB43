using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
   public class IRFInterviewVM
    {
        public string IRFId { get; set; }
        public string PrimarySkill { get; set; }
        public string ClientName { get; set; }
        public string EndClientName { get; set; }
        public string InterviewCount { get; set; }
        public string UnassignedInterviewCount { get; set; }
        public string InterviewStatusCount { get; set; }
        public int InterviewsReportCount { get; set; }
        public string SelectionCount { get; set; }
        public int QualityCheck { get; set; }

    }
}
