using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.Data
{
    public class VoiceItResponse
    {
        public string createdAt { get; set; }
        public string message { get; set; }
        public string timeTaken { get; set; }
        public string responseCode { get; set; }
        public string userId { get; set; }
        public string status { get; set; }
        public string contentLanguage { get; set; }
        public string text { get; set; }
        public string textConfidence { get; set; }
        public double confidence { get; set; }
    }
}
