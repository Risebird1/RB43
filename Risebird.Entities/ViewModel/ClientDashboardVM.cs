using Risebird.Entities.Data;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class ClientDashboardVM : ClientStatsBase
    {
        //public IEnumerable<Meeting> Meetings { get; set; }
        //public string MeetingsData { get; set; }
        //public string MeetingsDataLabels { get; set; }
        //public string EmailId { get; set; }
        public string FunnelData { get; set; }
        public IEnumerable<ClientDailySlotsStatus> DailyMeetingResultsSlots { get; set; }
        //public CandidatesCount CandidatesCountObj { get; set; }
        public MeetingResultCount MeetingResultCountObj { get; set; }
        public CandidateCurrentStatusCount CandidatesCountObj { get; set; }
        [Display(Name = "Whatsapp Number")]
        public string WhatsappNo { get; set; }


    }
}
