using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class ChatMessageViewModel
    {
        public Guid SessionId { get; set; } = Guid.NewGuid();
        public string Message { get; set; }
        //public string BotReply { get; set; }
        public List<ChatLine> ChatLog { get; set; } = new();
        public IFormFile AudioFile { get; set; }  
        public IFormFile VideoFile { get; set; }

        public string candidateName { get; set; }
        public string SynthesizedVideoUrl { get; set; } //synthesised by synthesia, this will be the video of the AI Interviewer speaking
        public int TimeLeftInSeconds { get; set; }
        public int InterviewId { get; set; }
        public string candidateName { get; set; }
        public bool IsAdaptive { get; set; }

    }

    public class ChatLine
    {
        public string Role { get; set; }
        public string Message { get; set; }
    }
}
