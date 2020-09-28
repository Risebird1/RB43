using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.Data
{
    public class InboundNatRule
    {
        public string name { get; set; }
        public string id { get; set; }
        public string etag { get; set; }
        public InboundNatRuleProperties properties { get; set; }
    }
}
