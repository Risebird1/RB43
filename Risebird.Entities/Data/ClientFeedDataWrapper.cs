using Risebird.Entities.ViewModel;
using System.Collections.Generic;

namespace Risebird.Entities.Data
{
    public class ClientFeedDataWrapper
    {
        public int? InterviewId { get; set; }
        public string MeetingLink { get; set; }
        public IEnumerable<ClientFeedDataVM> ClientFeeds { get; set; }
    }
}
