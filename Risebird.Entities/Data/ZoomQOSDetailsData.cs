using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data
{
    public class ZoomQOSDetailsData
    {
        //"bitrate": "63 kbps",
        //      "latency": "134 ms",
        //      "jitter": "6 ms",
        //      "avg_loss": "0.0%",
        //      "max_loss": "0.0%",
        //      "quality": "good",
        //      "quality_score": "3.65",
        //      "min_rssi": -56,
        //      "avg_rssi": -50,
        //      "max_rssi": -44,
        //      "rssi_unit": "dBm"
        //public string jitter { get; set; }
        public string quality_score { get; set; }
        //public string quality { get; set; }
        public string avg_loss { get; set; }
        public string max_latency { get; set; }
        public string min_latency { get; set; }
        public string avg_latency { get; set; }
        public string max_loss { get; set; }
        public string max_jitter { get; set; }
        public string min_jitter { get; set; }
        public string avg_jitter { get; set; }
    }
}
