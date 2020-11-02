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
        public string Question1 { get; set; }
        [Required]
        public string Question2 { get; set; }
        [Required]
        public string Question3 { get; set; }
        [Required]
        public string Question4 { get; set; }
        [Required]
        public string Question5 { get; set; }
        [Required]
        public string Question6 { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Phone { get; set; }
        public DateTime CreatedOn { get; set; }
        public int interviewid { get; set; }
        public string CommentforInterviewer { get; set; }
        public int Interviewerid { get; set; }

    }
    public class SurveyCandidateInterviewer
    {
        public int id { get; set; }
        public string Interviewer { get; set; }
        public string CandidateEmail { get; set; }
    }
}
