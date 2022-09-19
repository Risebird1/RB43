using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.Data
{
    public class ClientFeedDataWrapper
    {
        public int? InterviewId { get; set; }
        public IEnumerable<ClientFeedDataVM> ClientFeeds { get; set; }
    }
}
