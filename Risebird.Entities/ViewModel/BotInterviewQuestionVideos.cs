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
    }

    public class AIInterviewReportWrapperClass
    {
        public List<BotInterviewQuestionVideos> videoQuestionDetails { get; set; }
        public ChatMessageViewModel chatMessageViewModel { get; set; }
    }

}
