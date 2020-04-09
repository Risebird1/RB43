using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class TransInterviewerProfessional
    {
        public int id { get; set; }
        public int InterviewerProfileId { get; set; }
        public string CompanyName { get; set; }
        public string CompanyType { get; set; }
        public string Domain { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
