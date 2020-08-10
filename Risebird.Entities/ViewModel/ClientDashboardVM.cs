using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class ClientDashboardVM
    {
        public IEnumerable<Meeting> Meetings { get; set; }
        public string MeetingsData { get; set; }
        public string MeetingsDataLabels { get; set; }
        public string EmailId { get; set; }
        public string FunnelData { get; set; }
        public IEnumerable<ClientDailySlotsStatus> DailyMeetingResultsSlots { get; set; }
        public CandidatesCount CandidatesCountObj { get; set; }
        public MeetingResultCount MeetingResultCountObj { get; set; }

    }
}
