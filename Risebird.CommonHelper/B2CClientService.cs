using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public class B2CClientService : IB2CClientService
    {
        private string aadInstance = "https://login.microsoftonline.com/";
        private string aadGraphResourceId = "https://graph.windows.net/";
        private string aadGraphEndpoint = "https://graph.windows.net/";
        private string aadGraphSuffix = "";
        private string aadGraphVersion = "api-version=1.6";
        private string clientId;
        private string clientSecret;
        private string tenant;
        private readonly IConfiguration _configuration;
        private AuthenticationContext authContext;
        private ClientCredential credential;
        private readonly IApplicationErrorLogger _errorLogger;

        public B2CClientService(IConfiguration configuration, IApplicationErrorLogger errorLogger)
        {
            _errorLogger = errorLogger;
            clientId = configuration["RisebirdGraphB2C:RisebirdGraphClientId"];
            clientSecret = configuration["RisebirdGraphB2C:RisebirdGraphClientSecret"];
            tenant = configuration["RisebirdGraphB2C:RisebirdGraphTenant"];

            // The AuthenticationContext is ADAL's primary class, in which you indicate the direcotry to use.
            this.authContext = new AuthenticationContext("https://login.microsoftonline.com/" + tenant);

            // The ClientCredential is where you pass in your client_id and client_secret, which are 
            // provided to Azure AD in order to receive an access_token using the app's identity.
            this.credential = new ClientCredential(clientId, clientSecret);
        }

        public async Task<B2CProvisioningOutcome> CreateUser(UserList externalUser, bool forcePasswordChangeNextLogin = false)
        {
            var emailId = externalUser?.EmailId ?? string.Empty;

            try
            {
                var graphUserToAdd = UserViewModelToGraphUser(externalUser, forcePasswordChangeNextLogin);
                var graphClient = await CreateGraphClient();
                var newUser = await graphClient
                      .Users
                      .PostAsync(graphUserToAdd);

                return newUser != null
                    ? B2CProvisioningOutcome.Created
                    : B2CProvisioningOutcome.Failed;
            }
            catch (ODataError odataError) when (IsAlreadyExists(odataError))
            {
                // Re-adding somebody who is already in the directory is a normal case, not a failure.
                // Reported separately so callers can continue without treating the generated password
                // as valid - it was never applied to the existing account.
                await _errorLogger.LogMessage(LogLevelInfo.Info,
                    "Azure AD B2C account already exists; the generated password was not applied.", emailId);
                return B2CProvisioningOutcome.AlreadyExists;
            }
            catch (Exception ex)
            {
                // Without this log there is no record anywhere of why the B2C account could not be
                // created, and the caller cannot tell a real failure from an existing account.
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex, emailId);
                return B2CProvisioningOutcome.Failed;
            }
        }

        /// <summary>
        /// Graph reports a duplicate B2C identity as 409 with an ObjectConflict error code. The
        /// message check is a fallback in case the code or status is not populated.
        /// </summary>
        private static bool IsAlreadyExists(ODataError odataError)
        {
            if (odataError.ResponseStatusCode == (int)HttpStatusCode.Conflict)
            {
                return true;
            }

            var code = odataError.Error?.Code ?? string.Empty;
            var message = odataError.Error?.Message ?? string.Empty;

            return code.IndexOf("ObjectConflict", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("already exists", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private async Task<GraphServiceClient> CreateGraphClient()
        {
            var scopes = new List<string> { "https://graph.microsoft.com/.default" };
            var tenantId = "a3bbef13-3b06-4492-b31d-22187058b9ed";

            // using Azure.Identity;
            var options = new ClientSecretCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
            };

            // https://learn.microsoft.com/dotnet/api/azure.identity.clientsecretcredential
            var clientSecretCredential = new ClientSecretCredential(
                tenantId, clientId, clientSecret, options);

            return new GraphServiceClient(clientSecretCredential, scopes);


        }

        public async Task<ODataResponseDto<T>> List<T>(string api, string query)
        {
            var userRawResponse = await SendGraphGetRequest(api, query);
            var oDataResponse = JsonConvert.DeserializeObject<ODataResponseDto<T>>(userRawResponse);
            oDataResponse.PreparePaging(this);

            return oDataResponse;
        }

        internal async Task<string> SendGraphGetRequest(string api, string query)
        {
            // First, use ADAL to acquire a token using the app's identity (the credential)
            // The first parameter is the resource we want an access_token for; in this case, the Graph API.
            AuthenticationResult result = await authContext.AcquireTokenAsync("https://graph.windows.net", credential);

            // For B2C user managment, be sure to use the 1.6 Graph API version.
            HttpClient http = new HttpClient();
            string url = "https://graph.windows.net/" + tenant + api + "?" + aadGraphVersion;
            if (!string.IsNullOrEmpty(query))
            {
                url += "&" + query;
            }
            // Append the access token for the Graph API to the Authorization header of the request, using the Bearer scheme.
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            var response = await http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                object formatted = JsonConvert.DeserializeObject(error);
                throw new WebException("Error Calling the Graph API: \n" + JsonConvert.SerializeObject(formatted, Formatting.Indented));
            }

            return await response.Content.ReadAsStringAsync();
        }
        private User UserViewModelToGraphUser(UserList userObj, bool forcePasswordChangeNextLogin = false)
        {
            var mailNickname = FormatMailnickname(userObj.EmailId);

            var graphUser = new User
            {
                AccountEnabled = true,

                CreationType = "LocalAccount",
                DisplayName = $"{userObj.FirstName} {userObj.LastName}",
                GivenName = userObj.FirstName,
                Surname = userObj.LastName,
                MailNickname = mailNickname,
                City = "N/A",
                Country = "India",
                PostalCode = "N/A",
                StreetAddress = "N/A",
                Identities = new List<ObjectIdentity>()
                {
                    new ObjectIdentity
                    {
                        SignInType = "emailAddress",
                        Issuer = tenant,
                        IssuerAssignedId = userObj.EmailId,
                    }

                },
                // AdditionalData = additionalData, Not working now
                PasswordProfile = new PasswordProfile
                {
                    Password = userObj.UserPassword,
                    ForceChangePasswordNextSignIn = forcePasswordChangeNextLogin
                },
                PasswordPolicies = "DisablePasswordExpiration"
            };

            return graphUser;

        }

        private string FormatMailnickname(string value)
        {
            var invalidChars = @"[\!#$%&*+/=?^`{}|~<>()‘;:,]“@".ToList();
            invalidChars.ForEach(ch =>
            {
                value = value.Replace(ch.ToString(), string.Empty);
            });
            if (value.Length > 63)
            {
                value = value.Substring(0, 62);
            }
            return value;
        }
    }
}
