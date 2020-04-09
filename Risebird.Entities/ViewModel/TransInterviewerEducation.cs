using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class TransInterviewerEducation
    {
        public int id { get; set; }
        public int InterviewerProfileId { get; set; }
        public string Graduation { get; set; }
        public string CollegeType { get; set; }
        public string Specialization { get; set; }
        public string Score { get; set; }
    }
}
