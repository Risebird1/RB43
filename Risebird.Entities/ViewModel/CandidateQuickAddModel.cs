using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class CandidateQuickAddModel
    {
        public int IRFId { get; set; }
        public string IRFName { get; set; }
        [Required]
        [RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        ErrorMessage = "Please Enter Correct Email Address")]
        [Display(Name = "Email")]
        public string EmailId { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Phone { get; set; }
        [Required]
        [Display(Name = "Resume Link")]
        public string ResumeLink { get; set; }
        public bool IsSendInvite { get; set; }
        public int AddedByUserId { get; set; }
        public string ClientHREmail { get; set; }
        public string ProfileSource { get; set; }
        public string WhatsappNumber { get; set; }
        public int CandidateResumeId { get; set; }// id from CandidateResume table
        public int AddedByUserEmail { get; set; }



    }
}
