using Risebird.Entities.Data;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class ClientStatsBase
    {
        public IEnumerable<Meeting> Meetings { get; set; }
        public string MeetingsData { get; set; }
        public string MeetingsDataLabels { get; set; }
        public string EmailId { get; set; }
    }
}
