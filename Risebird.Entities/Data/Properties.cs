using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.Data
{
    public class Properties
    {
        public List<IpConfiguration> ipConfigurations { get; set; }
        public List<InboundNatRule> inboundNatRules { get; set; }
        public string ipAddress { get; set; }

    }
}
