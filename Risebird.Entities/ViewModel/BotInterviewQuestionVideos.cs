using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.ViewModel
{
    public class BotInterviewQuestionVideos
    {
        public int id { get; set; }
        public string SessionId { get; set; }
        public string VideoLink { get; set; }
        public string question { get; set; }
        public DateTime CreatedAt { get; set; }
        public string BotSynthesizedVideoLink { get; set; }
        public string CandidateAnswer { get; set; }
        public string BotAudioLink { get; set; }
        public int DeliveryHeadQuestionId { get; set; }

    }

    public class AIInterviewReportWrapperClass
    {
        public List<BotInterviewQuestionVideos> videoQuestionDetails { get; set; }
        public ChatMessageViewModel chatMessageViewModel { get; set; }
        public AIInterviewEvaluation AIInterviewEvaluation { get; set; }

        public int TotalQuestions { get; set; }
        public string CandidateName { get; set; }
        public string CandidateEmail { get; set; }
        public string resumeLink { get; set; }
        public int MatchScorePercentage { get; set; }
        public DateTime InterviewDate { get; set; }
        public int InterviewId { get; set; }
        public string SessionId { get; set; } //Guid
    }
    public class VideoSentimentAnalysis
    {
        public string CandidateName { get; set; } = string.Empty;
        public string CandidateEmail { get; set; } = string.Empty;
        public DateTime InterviewDate { get; set; } = DateTime.Now;
        public string SessionId { get; set; } = string.Empty;
        public int InterviewId { get; set; } = 0;
        public int SentimentScore { get; set; } = 0; // Range: -100 to +100
        public int StarRating { get; set; } = 0; // Range: 1-5
        public int Mentions { get; set; } = 0;
        public double AverageRating { get; set; } = 0.0;
        public int id { get; set; }

        public int biqvId { get; set; }

        public float? FEAR { get; set; }

        public float? CONFUSED { get; set; }

        public float? SAD { get; set; }

        public float? CALM { get; set; }

        public float? SURPRISED { get; set; }

        public float? DISGUSTED { get; set; }

        public float? ANGRY { get; set; }

        public float? HAPPY { get; set; }

        public DateTime CreatedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
        public List<KeywordModifier> TopModifiers { get; set; } = new List<KeywordModifier>();

    }
  
    public class KeywordModifier
    {
        public string Keyword { get; set; } = string.Empty;
        public int MentionCount { get; set; }
        public string SentimentType { get; set; } = "neutral"; // "positive", "negative", "neutral"
        public List<KeywordMention> Mentions { get; set; } = new List<KeywordMention>();
    }

    public class KeywordMention
    {
        public string UserName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string Keyword { get; set; } = string.Empty;
    }

}
