using System;

namespace Risebird.Entities.ViewModel
{
    [Serializable]
    public class VideoSentimentService
    {
        public int InterviewId { get; set; }
        public string ImageUrl { get; set; }

        public int EmotionType { get; set; }
        public double Value { get; set; }
    }
}
