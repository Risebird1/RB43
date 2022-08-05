using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class MCQVM
    {
        public string QuestionType { get; set; }
        public string Question { get; set; }
        public string Option1 { get; set; }
        public string Option2 { get; set; }
        public string Option3 { get; set; }
        public string Option4 { get; set; }
        public string Answer { get; set; }
        public string MCQId { get; set; }
        public int QuestionNumber { get; set; }
        public string SingleAnswer { get; set; }
        public bool MultiAnswerOption1 { get; set; }
        public bool MultiAnswerOption2 { get; set; }
        public bool MultiAnswerOption3 { get; set; }
        public bool MultiAnswerOption4 { get; set; }
        public string final { get; set; }
        public string QuestionAnswer { get; set; }
        public int Rating { get; set; }
        public string QuestionId { get; set; }
        public string CandidateID { get; set; }
        public string Explanation { get; set; }
        public string PartitionKey { get; set; }
        public int id { get; set; }
    }
    public class MCQVMset
    {
        public string Title { get; set; }
        public int QuestionCount { get; set; }
        public string Timer { get; set; }
        //public int ProfileType { get; set; }
        public string ProfileType { get; set; } // changed to difficulty level now
        public string ProfileTypeName { get; set; }
        public string PrimarySkills { get; set; }
        public string SecondarySkill { get; set; }
        public int CompentencyLevel { get; set; }
        public string CompentencyLevelName { get; set; }
        public string PartitionKey { get; set; }
        public int id { get; set; }
        public string SeniorityLevel { get; set; }
        public string EmploymentType { get; set; }
        public int? CompanyType { get; set; }
        public int? CountryType { get; set; }
        //public int PerHourRate { get; set; }
        public int TotalInvite { get; set; }
        public int CandidateAttempt { get; set; }
        public string Clients { get; set; }
        public bool AadhaarVerification { get; set; }
    }
    public class QuestionList
    {
        public int QuestionNumber { get; set; }
        public bool QuestionAnswered { get; set; }
    }
    public class QuestionDetailsList
    {
        public QuestionDetailsList()
        {
            _timmerdetails = new MCQVMset();
        }
        public List<QuestionList> _questionlist { get; set; }
        public MCQVM _question { get; set; }
        public MCQVMset _timmerdetails { get; set; }
    }
    public class CandidateDetailsForTest
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Candidate Email")]
        public string CandidateEmail { get; set; }
        public string ApplyingCompany { get; set; }
        public string ClientHR { get; set; }
        [Required]
        [Display(Name = "Total Exprience(in Years)")]
        public string TotalExprience { get; set; }
        [Required]
        public string Skills { get; set; }
        public string MCQId { get; set; }
        public int CandidateScore { get; set; }
        public int TotalQuestions { get; set; }
        public string VideoLink { get; set; }
        public int Feedbackreating { get; set; }
        public string FeedbackComment { get; set; }
        public string PartitionKey { get; set; }
        public int id { get; set; }

        public string Name { get; set; }
        public string Address { get; set; }
        public string Gender { get; set; }
        public string DOB { get; set; }
        public string CandidateImage { get; set; }
        public bool AadhaarVerification { get; set; }

    }
    public class MCQVMShare
    {
        public string EmailAddress { get; set; }
        public string MCQId { get; set; }
        public int status { get; set; }
        public string MCQSet { get; set; }
        public string ApplyingCompany { get; set; }
        public string ClientHR { get; set; }
        public int Videorecording { get; set; }
        public string PartitionKey { get; set; }
        public int id { get; set; }
        public string roomId { get; set; }
        public DateTime ShareDate { get; set; }
        public int Expiretime { get; set; }
        public string WhoShared { get; set; }
    }
    public class ProfileTypeSkills
    {
        public SelectList ProfileTypeList { get; set; }
        public int ProfileType { get; set; } //Now changed to difficulty level 
        public string ProfileTypeName { get; set; }
        public SelectList PrimarySkillsList { get; set; }
        public string PrimarySkills { get; set; }
        public SelectList CompentencyLevelList { get; set; }
        public SelectList SeniorityLevelList { get; set; }
        public SelectList EmploymentTypeOptions { get; set; }
    }
    public class MCQDropdowndetails
    {
        public MCQDropdowndetails()
        {
            _ProfileTypeSkills = new ProfileTypeSkills();
        }
        public ProfileTypeSkills _ProfileTypeSkills { get; set; }
        public List<MCQVMset> _MCQVMset { get; set; }
        public SelectList ClientList { get; set; }


    }
    public class CandidateDetailsForTestDuplicate
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Candidate Email")]
        public string CandidateEmail { get; set; }
        public string ApplyingCompany { get; set; }
        public string ClientHR { get; set; }
        [Required]
        [Display(Name = "Total Exprience(in Years)")]
        public string TotalExprience { get; set; }
        [Required]
        public string Skills { get; set; }
        public string Id { get; set; }
        public int CandidateScore { get; set; }
        public int TotalQuestions { get; set; }
        public DateTime Sharetime { get; set; }
        public string PartitionKey { get; set; }
        public string SameID { get; set; }
        public string VideoLink { get; set; }
        public int Feedbackreating { get; set; }
        public string FeedbackComment { get; set; }
        public List<MCQVM> QuestionDetails { get; set; }
        public string roomId { get; set; }
        public IList<SelectabilityEvaluatorReport> SelectabilityEvaluatorReportCollection { get; set; }
        public IList<JoinabilityHRReport> JoinabilityHRReportCollection { get; set; }
        public SelectList Level { get; set; }
        public int Selectability { get; set; }
        public int Joinability { get; set; }
        [Display(Name = "Recruiter Comment")]
        public string RecruiterComment { get; set; }
        public SelectList JoinabilityTypeList { get; set; }
        public SelectList SelectabilityTypeList { get; set; }
    }
    public class ShareMCQRisebirdMeeting : RBMeetCreateMeetingObject
    {
        //public string agentname { get; set; }
        //public string visitorName { get; set; }
        //public string datetime { get; set; }
    }
    public class ShareMCQRisebirdMeetingResponse : RBMeetCreateMeetingResponse
    {
        //public string roomId { get; set; }
        //public string meetingAgent { get; set; }
        //public string meetingAttendee { get; set; }
        //public string videoUrl { get; set; }
    }
}
