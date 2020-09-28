using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Text;

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
                AADTenantDomain =_AADTenantDomain,
                AADAccessKey =_AADAccessKey,
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


        private string GetAuthorizationHeaderForAccessToken()
        {
            try
            {
                var cc = new ClientCredential(_AADClientId, _AADAccessKey);
                var context = new AuthenticationContext(_AuthenticationUri + _AADTenantDomain);
                AuthenticationResult result = null;
                result = context.AcquireTokenAsync(_ARMEndpoint, cc).Result;
                
                if (result == null)
                {
                    throw new InvalidOperationException("Failed to obtain the JWT token");
                }
                string token = result.AccessToken;
                return token;
            }
            catch (Exception ex)
            {
                return ex.InnerException.Message;
            }
        }
    }
}
