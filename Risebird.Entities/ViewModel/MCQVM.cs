using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
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
}
