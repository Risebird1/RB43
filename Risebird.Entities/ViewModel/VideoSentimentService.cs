using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    [Serializable]
    public class VideoSentimentService
    {
        public int InterviewId { get; set; }
        public string ImageUrl { get; set; }
        //public string BlobFileName { get; set; }
        //public MemoryStream ImageStream { get; set; }
        //public string ImageString { get; set; }
        public int EmotionType { get; set; }
        public double Value { get; set; }
    }
}
