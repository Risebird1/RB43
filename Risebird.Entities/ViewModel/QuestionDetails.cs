using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class QuestionDetails
    {
        public int Id { get; set; }
        public int InterviewId { get; set; }
        public string VideoTitle { get; set; }
        public string VideoLink { get; set; }
        public string ThumbnailLink { get; set; }
        public int Priority { get; set; }
        public string Question { get; set; }
        public IFormFile VideoFile { get; set; }
        public IFormFile Thumbnail { get; set; }
    }
    public class Question
    {
        public int InterviewId { get; set; }
        public IList<QuestionDetails> questionset { get; set; }
    }
}
