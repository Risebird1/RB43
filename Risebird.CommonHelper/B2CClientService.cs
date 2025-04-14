using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
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

        public B2CClientService(IConfiguration configuration, IApplicationErrorLogger errorLogger)
        {
            clientId = configuration["RisebirdGraphB2C:RisebirdGraphClientId"];
            clientSecret = configuration["RisebirdGraphB2C:RisebirdGraphClientSecret"];
            tenant = configuration["RisebirdGraphB2C:RisebirdGraphTenant"];

            // The AuthenticationContext is ADAL's primary class, in which you indicate the direcotry to use.
            this.authContext = new AuthenticationContext("https://login.microsoftonline.com/" + tenant);

            // The ClientCredential is where you pass in your client_id and client_secret, which are 
            // provided to Azure AD in order to receive an access_token using the app's identity.
            this.credential = new ClientCredential(clientId, clientSecret);
        }

        public async Task<bool> CreateUser(UserList externalUser, bool forcePasswordChangeNextLogin = false)
        {
            try
            {
                var graphUserToAdd = UserViewModelToGraphUser(externalUser, forcePasswordChangeNextLogin);
                var graphClient = await CreateGraphClient();
                var newUser = await graphClient
                      .Users
                      .PostAsync(graphUserToAdd);

                return newUser != null ? true : false;
            }
            catch (Exception ex)
            {
                return false;
            }

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
