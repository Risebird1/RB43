using System.Collections.Generic;

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
