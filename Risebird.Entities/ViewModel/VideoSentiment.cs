using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class VideoSentiment
    {
        public int id { get; set; }
        public int InterviewId { get; set; }
        public string ImageUrl { get; set; }
        public int EmotionType { get; set; }
        public double Value { get; set; }
    }
}
