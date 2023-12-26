using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data
{
    public class ZoomQualityOfServiceData
    {
        public string @event { get; set; }
        public int event_ts { get; set; }
        public QOSPayloadData payload { get; set; }
    }
}
