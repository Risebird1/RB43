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
        public string FeedSubject { get; set; } //this is only relevant if the feedType is a broadcast email 
        public string FeedUrl { get; set; }
        public int FeedRating { get; set; }
        public string RatedBy { get; set; } //this is only relevant if the feedType is a Rating
        public string CandidateName { get; set; } //this is only relevant if the feedType is a Rating
        public DateTime? InterviewDate { get; set; } //this is only relevant if the feedType is a Rating 
        public string Client { get; set; } //this is only relevant if the feedType is a Rating 
    }
}
