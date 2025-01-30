using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.Data
{
    public class AddCandidatesRequest
    {
        //public string skillChannelName { get; set; }
        //public string planType{ get; set; }
        public string requestedBy { get; set; }
        public string jobId { get; set; }
        //public List<CandidateQuickAddModel> CandidateListToBeProcessed { get; set; }
        public List<CandidateQuickAddModelAPIVersion> CandidateList { get; set; }
    }
   
    public class CandidateQuickAddModelAPIVersion
    {
        public int IRFId { get; set; }
        public string IRFName { get; set; }

        [Required]
        [RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        ErrorMessage = "Please Enter Correct Email Address")]
        [Display(Name = "Email")]
        public string EmailId { get; set; }
        //[Required]
        public string Name { get; set; }
        [Required]
        public string Phone { get; set; }
        [Display(Name = "Resume Link")]
        public string ResumeLink { get; set; }
        public bool IsSendInvite { get; set; }
        public int AddedByUserId { get; set; }
        public string ClientHREmail { get; set; }
        public string ProfileSource { get; set; }
        public string WhatsappNumber { get; set; }
        public int CandidateResumeId { get; set; }// id from CandidateResume table
        public int AddedByUserEmail { get; set; }
        public string AffindaResumeIdentifier { get; set; }
        public string countryCode { get; set; }


    }

}
