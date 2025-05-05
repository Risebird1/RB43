using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class InterviewerDirectApplicationVM
    {
        public int id { get; set; }
        public int IRFId { get; set; }
        [Required]
        public string EmailId { get; set; }

        //[Required]
        [DisplayName("Mobile Number")]
        public string Phone { get; set; }
        public string ResumeLink { get; set; }
        public string EncryptedIrfId { get; set; }

        [Required]
        public IFormFile ResumeFile { get; set; }
        public DateTime CreatedAt { get; set; }
        public string SkillChannelName { get; set; }
        public string JDTiltle { get; set; }
        public string JDTitleTwo { get; set; }
        public bool IsIrfDeleted { get; set; }
        public string AssessmentGroup { get; set; }
        public int AssessmentGroupId { get; set; }
        public string ChannelOwner { get; set; }
        public DateTime ChannelCreationDate { get; set; }
        public string OpenAIResponse { get; set; }
        public string AIVerdict { get; set; }
        public DateTime? AIEvaluationGeneratedAt { get; set; }
        public string AIEvaluationPerformedBy { get; set; }
        public string CompanyName { get; set; }
        public bool isVerified { get; set; }
        public bool IsEmployed { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime? DOB { get; set; }
        public string EVGeneratedBy { get; set; }
        public DateTime EVGeneratedAt { get; set; }

        [DisplayName("Referred By")]
        public string ReferredBy { get; set; }
        public string AcknowledgementStatus { get; set; }
        public string ApplicationType { get; set; } //either internal or opentoall
        public int AIAnalysisScore { get; set; } //either internal or opentoall

    }
}
