using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class QuestionDetails
    {
        public int Id { get; set; }
        public int InterviewId { get; set; }
        public string VideoTitle { get; set; }
        public string VideoLink { get; set; }
        public int Priority { get; set; }
        public string Question { get; set; }
        public IFormFile VideoFile { get; set; }
    }
    public class Question
    {
        public int InterviewId { get; set; }
        public IList<QuestionDetails> questionset { get; set; }
    }
}
