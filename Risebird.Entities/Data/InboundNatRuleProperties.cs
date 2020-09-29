namespace Risebird.Entities.Data
{
    public class InboundNatRuleProperties
    {
        public string provisioningState { get; set; }
        public int frontendPort { get; set; }
        public int backendPort { get; set; }
        public bool enableFloatingIP { get; set; }
        public int idleTimeoutInMinutes { get; set; }
        public string protocol { get; set; }
    }
}
