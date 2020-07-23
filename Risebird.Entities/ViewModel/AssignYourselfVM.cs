using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class AssignYourselfVM
    {
        public int id { get; set; }
        [Display(Name = "Interview Date")]
        public string InterviewDate { get; set; }
        [Display(Name = "Candidate Name")]
        public string CandidateEmail { get; set; }
        [Display(Name = "Interviewer Email")]
        [RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        ErrorMessage = "Please Enter Correct Email Address")]
        [DataType(DataType.EmailAddress)]
        public string Interviewer { get; set; }
    }
}
