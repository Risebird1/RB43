using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.Data
{
    public class IpConfProperties
    {
        public string privateIPAddress { get; set; }
        //public string privateIPAllocationMethod { get; set; }
        public PublicIPAddress publicIPAddress { get; set; }
        public List<LoadBalancerInboundNatRule> loadBalancerInboundNatRules { get; set; }
    }
}
