using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data
{
    public class QOSPayloadData
    {
        public string account_id { get; set; }
        public QOSPayloadInternalObject @object { get; set; }
    }
}
