using Microsoft.Extensions.Configuration;

using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using System;

namespace Risebird.CommonHelper
{
    public class AzureHelper : IAzureHelper
    {

        private readonly string _AADTenantDomain;
        private readonly string _AADClientId;
        private readonly string _AADAccessKey;
        private readonly string _AuthenticationUri;
        private readonly string _SubscriptionId;
        private readonly string _ARMEndpoint;

        private readonly IConfiguration _configuration;
        public AzureHelper(IConfiguration configuration)
        {
            _configuration = configuration;
            _AADTenantDomain = _configuration["AzureVmCredentials:AADTenantDomain"];
            _AADClientId = _configuration["AzureVmCredentials:AADClientId"];
            _AADAccessKey = _configuration["AzureVmCredentials:AADAccessKey"];
            _AuthenticationUri = _configuration["AzureVmCredentials:AuthenticationUri"];
            _SubscriptionId = _configuration["AzureVmCredentials:SubscriptionId"];
            _ARMEndpoint = _configuration["AzureVmCredentials:ARMEndpoint"];

        }


        public AzureCredentials GetAzureCredential()
        {
            var azureCredentials = new AzureCredentials()
            {
                //using for developemnt
                SubscriptionId = _SubscriptionId,
                AADTenantDomain = _AADTenantDomain,
                AADAccessKey = _AADAccessKey,
                AADClientId = _AADClientId,
                Token = ""
            };
            azureCredentials.Token = GetAzureTokenCredentials();
            return azureCredentials;
        }
        private string GetAzureTokenCredentials()
        {
            string token = string.Empty;
            try
            {
                return GetAuthorizationHeaderForAccessToken();
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        /// <summary>
        /// Service-principal token for Azure Resource Manager via Azure.Identity. ADAL
        /// (Microsoft.IdentityModel.Clients.ActiveDirectory), used here before, is retired.
        /// One credential per client id is kept for the process so its token cache is reused
        /// instead of a fresh token being requested for every ARM call.
        /// </summary>
        private string GetAuthorizationHeaderForAccessToken()
        {
            var credential = Credentials.GetOrAdd($"{_AADTenantDomain}|{_AADClientId}",
                _ => new Azure.Identity.ClientSecretCredential(_AADTenantDomain, _AADClientId, _AADAccessKey));
            var scope = (string.IsNullOrWhiteSpace(_ARMEndpoint) ? "https://management.azure.com" : _ARMEndpoint.TrimEnd('/')) + "/.default";
            // Callers are synchronous; a failure surfaces as a null token (see GetAzureTokenCredentials)
            // rather than, as before, an exception message sent to Azure as the bearer token.
            return credential.GetToken(new Azure.Core.TokenRequestContext(new[] { scope }), default).Token;
        }

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Azure.Identity.ClientSecretCredential> Credentials =
            new System.Collections.Concurrent.ConcurrentDictionary<string, Azure.Identity.ClientSecretCredential>();
    }
}
