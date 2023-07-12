using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class ClientRoleWiseStatsWrapper
    {
        public ClientRoleWiseStatsWrapper()
        {
            StaffingAgencyStats = new List<StaffingAgencyStatsDerived>();
            ClientHRStats = new List<ClientHRStatsDerived>();
        }
        public IEnumerable<StaffingAgencyStatsDerived> StaffingAgencyStats { get; set; }
        public IEnumerable<ClientHRStatsDerived> ClientHRStats { get; set; }
    }
}
