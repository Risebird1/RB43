using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Entities.Data;
using System;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class PaymentSLAVM
    {
        public int id { get; set; }
        public int Interviewid { get; set; }
        public int Userid { get; set; }
        public string Emailid { get; set; }
        public string Interviewer { get; set; }
        public string CompanyName { get; set; }
        public string MobileNumber { get; set; }
        public string ChannelOwner { get; set; }
        public string candidateEmail { get; set; }
        public string candidateName { get; set; }
        public string CreatedBy { get; set; }
        public string IRFId { get; set; }
        public int irfIntId { get; set; }
        public string InterviewerName { get; set; }
        public DateTime InterviewDate { get; set; }
        public bool Isedit { get; set; }
        public int isapproved { get; set; }
        public bool isapprovedchecked { get; set; }
        public string Type { get; set; }
        public string uniqueid { get; set; }
        public bool QCStatus { get; set; }
        public int Redflags { get; set; }
        public int PaymentComplianceCount { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime Joinedon { get; set; }
        public string Pagetype { get; set; }

        [Display(Name = "Payment Deduction")]
        public int PaymentDeduction { get; set; }
        public SelectList PaymentDeductionList { get; set; }
        //Video Quality Check
        [Display(Name = "Inaccurate/ Arbitrary decision making")]
        public string Inaccuratedecisionmaking { get; set; }
        [Display(Name = "Time Management ")]
        public string TimeManagement { get; set; }
        [Display(Name = "Questions Relevance")]
        public string QuestionsRelevance { get; set; }
        [Display(Name = "Score Rubric Ignorance")]
        public string ScoreRubricIgnorance { get; set; }
        [Display(Name = "Poor Feedback Comments")]
        public string PoorFeedbackComments { get; set; }
        [Display(Name = "Candidate Interaction Quality")]
        public string CandidateInteractionQuality { get; set; }
        [Display(Name = "Limited Handon coding / Practical scenarios")]
        public string LimitedHandoncoding { get; set; }
        [Display(Name = "Wrong Status Updates")]
        public string WrongStatusUpdates { get; set; }
        [Display(Name = "Weak Infrastructure")]
        public string WeakInfrastructure { get; set; }
        [Display(Name = "Proctoring Ignorance")]
        public string ProctoringIgnorance { get; set; }
        [Display(Name = "Unethical Practices")]
        public string UnethicalPractices { get; set; }
        [Display(Name = "Poor Communication Skills")]
        public string PoorCommunicationSkills { get; set; }
        public string ResumeLink { get; set; }
        public int RedMarker { get; set; }
        public string interviewer { get; set; }
        public int Rating { get; set; }
        [Display(Name = "Video Reward")]
        public int VideoReward { get; set; }
        public bool VideoEnable { get; set; }
        public bool isQcDone { get; set; }
        public bool techqc { get; set; }
        public bool IsVideoRequired { get; set; }
        public int TotalPaymentComplianceCount { get; set; }
        [Display(Name = "Interviewer quality check")]
        public string Recommendation { get; set; }
        public string RecommendationComments { get; set; }
        [Display(Name = "Interviewer Video")]
        public string InterviewerVideo { get; set; }
        [Display(Name = "Correct Interview flow followed")]
        public string CorrectInterview{ get; set; }
    }
    public class PaymentSlabsVM
    {
        public int id { get; set; }
        public string Name { get; set; }
    }
}
