using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class AssignYourselfVM
    {
        public int id { get; set; }
        [Display(Name = "Interview Date")]
        public string InterviewDate { get; set; }
        [Display(Name = "Candidate Email")]
        public string CandidateEmail { get; set; }
        [Display(Name = "Interview Email")]
        [DataType(DataType.EmailAddress)]
        public string Interviewer { get; set; }
    }
}
