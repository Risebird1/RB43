using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data
{
    public class QOSPayloadInternalObject
    {
        public string uuid { get; set; }
        public string id { get; set; }
        public QOSParticipantData participant { get; set; }
    }
}
