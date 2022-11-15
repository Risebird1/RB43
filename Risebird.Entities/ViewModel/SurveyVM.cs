using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class SurveyVM : TableEntity
    {

        [Required]
        [Display(Name = "Enter Your Email Address")]
        public string EmailAddress { get; set; }
        public string Type { get; set; }
        [Required]
        [Display(Name = "Overall, how would you rate the video interview experience?")]
        public string Question1 { get; set; }
        [Required]
        [Display(Name = "How would you rate the evaluation process of interviewer?")]
        public string Question2 { get; set; }
        [Required]
        [Display(Name = "Given a chance would you like to reappear for the video interview?")]
        public string Question3 { get; set; }
        [Required]
        [Display(Name = "What did you like about the video interviews event?")]
        public string Question4 { get; set; }
        [Required]
        [Display(Name = "What did you dislike about the video interviews event?")]
        public string Question5 { get; set; }
        [Required]
        [Display(Name = "How friendly was the Risebird staff from scheduling to completion of interviews ?")]
        public string Question6 { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Phone { get; set; }
        public DateTime CreatedOn { get; set; }
        public int interviewid { get; set; }
        public string CommentforInterviewer { get; set; }
        public int Interviewerid { get; set; }
        public string interviewerEmail { get; set; }
        public string PrimarySkill { get; set; }

        public bool IsClient { get; set; }
    }
    public class SurveyCandidateInterviewer
    {
        public int id { get; set; }
        public string Interviewer { get; set; }
        public string CandidateEmail { get; set; }
        public string PrimarySkill { get; set; }
        public string CandidateName { get; set; }
    }
    public class SurveyDetails
    {
        public int Interviewerid { get; set; }
        public string Intervieweremail { get; set; }
        public int interviewid { get; set; }
        public string ClientName { get; set; }
        public string interviewdate { get; set; }
    }
    public class InterviewerRating
    {
        public int StarRating { get; set; }
        public string Comment { get; set; }
        public string Emailid { get; set; }
        public int Interviewid { get; set; }
        public DateTime interviewdate { get; set; }
        public string Name { get; set; }
        public string ClientAction { get; set; }
        public string interviewer { get; set; }
        public string candidateEmail { get; set; }
    }
    public class ClientReviewVM
    {
        public int id { get; set; }
        public string ClientEmailid { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedOn { get; set; }
        public string ClientName { get; set; }
    }
}
