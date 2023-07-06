using Microsoft.AspNetCore.Mvc.Rendering;

namespace Risebird.Entities.ViewModel
{
    public class ClientStatsVM : ClientStatsBase
    {
        public int id { get; set; }
        public string ClientName { get; set; }
        public SelectList ClientList { get; set; }
    }
}
