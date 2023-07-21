using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data
{
    public class AppInsightsPageViews
    {
        public string timestamp { get; set; }
        public string name { get; set; }
        public string url { get; set; }
        public string user_AuthenticatedId { get; set; }
        public bool success { get; set; }
        public string client_IP { get; set; }
        public string client_City { get; set; }
        public string duration { get; set; }
    }
}
