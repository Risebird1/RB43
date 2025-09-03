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
    }

}
