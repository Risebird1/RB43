using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.Data
{
    public class AddCandidatesRequest
    {
        //public string skillChannelName { get; set; }
        //public string planType{ get; set; }
        public string requestedBy { get; set; }
        public string jobId { get; set; } // this is primarily being used for Turing
        public string jobTitle { get; set; } // these jobtitles will be mapped against MAGs in our system as a preceding step/prerequisite
        public string jdLink { get; set; } 
        public string requestId { get; set; }
        //public List<CandidateQuickAddModel> CandidateList { get; set; }
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
        public DateTime InterviewDate { get; set; }

    }
    public class ScheduleCandidatesRequest
    {
        public string skillChannelId { get; set; }
        public string requestedBy { get; set; }
        public string RequestId { get; set; }
        public List<ScheduleCandidateList> CandidateList { get; set; }
    }
    public class ScheduleCandidateList
    {
        public DateTime Interviewdate { get; set; }
        public string EmailId { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Uniqueid { get; set; }
        public string ResumeLink { get; set; }
        public string ClientName { get; set; }
        public bool isCandidateportal { get; set; }
        public ScheculeInterviewIRFVM _IRFDetails { get; set; }
    }
    public class ScheduleCandidateResponse
    {
        public ScheduleCandidateResponse()
        {
            list = new List<ScheduleCandidateStatus>();
        }
        public string performedAt { get; set; }
        public string SkillChannelId { get; set; }
        public List<ScheduleCandidateStatus> list { get; set; }
    }

    public class ScheduleCandidateStatus
    {
        public string CandidateEmail { get; set; }
        public string onboardingStatus { get; set; }//successful/ duplicate/ User exists with different profile/failed
        public DateTime Interviewdate { get; set; }
        public string meetingLink { get; set; }//; NA
    }
}



