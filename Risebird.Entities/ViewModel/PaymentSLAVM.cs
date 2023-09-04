using Microsoft.VisualBasic;
using Risebird.Entities.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata;

namespace Risebird.Entities.ViewModel
{
    public class PaymentSLAVM
    {
        public int Interviewid { get; set; }
        public int Userid { get; set; }
        public string Emailid { get; set; }
        public string Profile { get; set; }
        public string Interviewer { get; set; }
        public string candidateEmail { get; set; }
        public string CreatedBy { get; set; }
        public string IRFId { get; set; }
        public string Client { get; set; }
        public DateTime InterviewDate { get; set; }
        public int PaymentDeduction { get; set; }
        public bool Isedit { get; set; }
        public string Type { get; set; }
        public string uniqueid { get; set; }

        //Video Quality Check
        [Display(Name = "Inaccurate/ Arbitrary decision making")]
        public string Inaccuratedecisionmaking { get; set; } 
        [Display(Name = "Time Management ")]
        public string TimeManagement  { get; set; }
        [Display(Name = "Questions Relevance")]
        public string QuestionsRelevance  { get; set; }
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

    }
}
