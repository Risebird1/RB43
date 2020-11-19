using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class MCQVM : TableEntity
    {
        public string QuestionType { get; set; }
        public string Question { get; set; }
        public string Option1 { get; set; }
        public string Option2 { get; set; }
        public string Option3 { get; set; }
        public string Option4 { get; set; }
        public string Answer { get; set; }
        public string Id { get; set; }
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
    }
    public class MCQVMset : TableEntity
    {
        public string Title { get; set; }
        public int QuestionCount { get; set; }
    }
    public class QuestionList
    {
        public int QuestionNumber { get; set; }
        public bool QuestionAnswered { get; set; }
    }
    public class QuestionDetailsList
    {
        public List<QuestionList> _questionlist { get; set; }
        public MCQVM _question { get; set; }
    }
    public class CandidateDetailsForTest : TableEntity
    {
        [Required]
        [Display(Name = "Candidate Email")]
        public string CandidateEmail { get; set; }
        [Required]
        [Display(Name = "Applying For(Company)")]
        public string ApplyingCompany { get; set; }
        [Required]
        [Display(Name = "Client HR Name")]
        public string ClientHR { get; set; }
        [Required]
        [Display(Name = "Total Exprience")]
        public string TotalExprience { get; set; }
        [Required]
        public string Skills { get; set; }
        public string Id { get; set; }
        public int CandidateScore { get; set; }
        public int TotalQuestions { get; set; }
    }
}
