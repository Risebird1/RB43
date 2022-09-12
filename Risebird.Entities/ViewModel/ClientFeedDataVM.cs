using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class ClientFeedDataVM
    {
        public string SkillChannelName { get; set; }
        public DateTime? FeedDate { get; set; }
        public string FeedType { get; set; }
        public string FeedText { get; set; }
        public string FeedUrl { get; set; }
    }
}
