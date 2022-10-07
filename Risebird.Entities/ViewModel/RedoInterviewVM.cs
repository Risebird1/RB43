
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class RedoInterviewVM
    {
        public int id { get; set; }
        public int interviewid { get; set; }
        [Display(Name = "Candidate Face")]
        public string CandidateFace { get; set; }
        [Display(Name = "Interview Quality")]
        public string Inappropriate { get; set; }
        [Display(Name = "Required Interview Duration")]
        public int RequiredDuration { get; set; }
        [Display(Name = "Actual Interview Duration")]
        public int ActualDuration { get; set; }
        [Display(Name = "Hands On Coding")]
        public string HandsOfCoding { get; set; }
        [Display(Name = "Redo Interview Reason")]
        public string Comments { get; set; }
        [Display(Name = "Rejection Comment")]
        public string RejectionComment { get; set; }
        public string ModifiedBy { get; set; }
    }
}
