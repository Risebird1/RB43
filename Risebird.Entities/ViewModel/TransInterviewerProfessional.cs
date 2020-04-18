using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }
    }
}
