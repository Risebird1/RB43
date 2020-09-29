namespace Risebird.Entities.Data
{
    public class AzureCredentials
    {
        public string SubscriptionId { get; set; }
        public string AADTenantDomain { get; set; }
        public string AADClientId { get; set; }
        public string AADAccessKey { get; set; }
        public string Token { get; set; }
    }
}
