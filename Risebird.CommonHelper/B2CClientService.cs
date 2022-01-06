using Microsoft.Extensions.Configuration;
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
            var graphUser = UserViewModelToGraphUser(externalUser, forcePasswordChangeNextLogin);
            var userJson = JsonConvert.SerializeObject(graphUser);
            var response = await SendGraphPostRequest("/users", userJson);
            var odataResponse = ODataResponseDto<GraphUserVM>.Parse(response);

            if (odataResponse.Error != null)
            {
                return false;

            }

            return true;
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
        private GraphUserVM UserViewModelToGraphUser(UserList userObj, bool forcePasswordChangeNextLogin = false)
        {
            var mailNickname = string.Empty;

            if (!string.IsNullOrEmpty(userObj.EmailId) && userObj.EmailId.Contains("@"))
            {
                mailNickname = FormatMailnickname(userObj.EmailId.Substring(0, userObj.EmailId.IndexOf("@")));
            }
            else
            {
                throw new Exception("User email must be specified");
            }

            return new GraphUserVM
            {
                AccountEnabled = true,
                CreationType = "LocalAccount",
                DisplayName = $"{userObj.FirstName} {userObj.LastName}",
                GivenName = userObj.FirstName,
                Surname = userObj.LastName,
                MailNickname = mailNickname,
                PasswordProfile = !string.IsNullOrWhiteSpace(userObj.UserPassword) ? new GraphPasswordProfileVM
                {
                    Password = userObj.UserPassword,
                    ForceChangePasswordNextLogin = forcePasswordChangeNextLogin
                } : null,
                City = "N/A",
                PostalCode = "N/A",
                StreetAddress = "N/A",
                PasswordPolicies = "DisablePasswordExpiration",
                SignInNames = new List<GraphSignInNameVM>
                {
                    new GraphSignInNameVM
                    {
                        Type = "emailAddress",
                        Value = userObj.EmailId
                    }
                }
            };

        }

        private async Task<string> SendGraphPostRequest(string api, string json, HttpMethod httpMethod = null)
        {
            httpMethod = httpMethod == null ? HttpMethod.Post : httpMethod;
            try
            {
                // NOTE: This client uses ADAL v2, not ADAL v4
                AuthenticationResult result = await authContext.AcquireTokenAsync(aadGraphResourceId, credential);
                HttpClient http = new HttpClient();
                string url = aadGraphEndpoint + tenant + api + "?" + aadGraphVersion;

                HttpRequestMessage request = new HttpRequestMessage(httpMethod, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                request.Content = !string.IsNullOrEmpty(json) ? new StringContent(json, Encoding.UTF8, "application/json") : null;
                HttpResponseMessage response = await http.SendAsync(request);

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception)
            {
                return "";

            }
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
