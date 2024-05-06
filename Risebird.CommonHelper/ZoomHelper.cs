using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using RestSharp;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public class ZoomHelper : IZoomHelper
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IConfiguration _configuration;
        private readonly ISettingsRepository _settingsRepository;
        private readonly string accountId;
        private readonly string clientId;
        private readonly string clientSecret;
        private readonly string oauthEndpoint;


        public ZoomHelper(IApplicationErrorLogger applicationErrorLogger, IConfiguration configuration, ISettingsRepository settingsRepository)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _settingsRepository = settingsRepository;
            accountId = _configuration["ZoomOAuthDetails:accountId"];
            clientId = _configuration["ZoomOAuthDetails:clientId"];
            clientSecret = _configuration["ZoomOAuthDetails:clientSecret"];
            oauthEndpoint = _configuration["ZoomOAuthDetails:endPoint"];
        }

        public string ZoomToken()
        {
            try
            {
                // Token will be good for 1 day
                DateTime Expiry = DateTime.UtcNow.AddDays(1);


                string ApiKey = _configuration["ZoomAppDetails:apiKey"];
                string ApiSecret = _configuration["ZoomAppDetails:secretKey"];

                int ts = (int)(Expiry - new DateTime(1970, 1, 1)).TotalSeconds;

                // Create Security key  using private key above:
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ApiSecret));

                // length should be >256b
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                //Finally create a Token
                var header = new JwtHeader(credentials);

                //Zoom Required Payload
                var payload = new JwtPayload
            {
                { "iss", ApiKey},
                { "exp", ts },
            };

                var secToken = new JwtSecurityToken(header, payload);
                var handler = new JwtSecurityTokenHandler();

                // Token to string so you can use it in your client
                var tokenString = handler.WriteToken(secToken);

                return tokenString;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<HttpStatusCode> CheckMeetingbyId(string meetingId, string token)
        {
            var client = new RestClient(string.Concat(_configuration["ZoomAppDetails:endPoint"], $"meetings/" + meetingId));
            var request = new RestRequest(string.Concat(_configuration["ZoomAppDetails:endPoint"], $"meetings/" + meetingId), Method.Get);
            request.AddHeader("content-type", "application/json");
            request.AddHeader("authorization", "Bearer " + token);
            RestResponse response = await client.ExecuteAsync(request);
            //var content = response.Content;
            return response.StatusCode;

        }

        //public string GenerateAccessToken()
        //{
        //    var _model = new ZoomOAuthCredentials();
        //    _model.grant_type = "account_credentials";
        //    _model.account_id = "mKqPkZV2RciTWQ9tHtZyEw";

        //    var tokenResponse = new RestClient("https://zoom.us/oauth/token");
        //    var request = new RestRequest("https://zoom.us/oauth/token", Method.Post);
        //    request.AddHeader("authorization", "Basic bW10dXFIaUdTMWlnRVBNWmhEeldNUTpSemRiNk5Yb3pJeFZRZk1UOU5aOUwzQ1NjbnZidzhCeQ==");//"v7sy3J8cAFdEJp5S"
        //    request.AddBody(_model);
        //    //request.AddJsonBody(_model);
        //    RestResponse response = tokenResponse.Execute(request);
        //    var content = response.Content;
        //    var convertedResponse = JsonConvert.DeserializeObject<string>(content);
        //    if (convertedResponse != null)
        //    {
        //        return convertedResponse;
        //    }
        //    return string.Empty;


        //}

        public async Task<string> GenerateAccessToken()
        {
            try
            {
                string token = string.Empty;
                //check whether we already have a token that has not expired, expires in 1 hour, we update every 50 mins
                var existingTokenObj = await _settingsRepository.GetZoomOAuthToken();
                if (existingTokenObj != null && existingTokenObj.Created_ModifiedOn.HasValue && (DateTime.Now - existingTokenObj.Created_ModifiedOn.Value).TotalMinutes < 50)
                {
                    token = existingTokenObj.Token;
                    return token;
                }

                var client = new RestClient(oauthEndpoint);//"https://zoom.us/oauth/token"
                var request = new RestRequest(oauthEndpoint, Method.Post);//"https://zoom.us/oauth/token"
                string concatenatedString = clientId + ":" + clientSecret;

                byte[] concatenatedBytes = Encoding.UTF8.GetBytes(concatenatedString);
                string base64Encoded = Convert.ToBase64String(concatenatedBytes);
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddParameter("grant_type", "account_credentials");
                request.AddParameter("account_id", accountId);
                //request.AddHeader("Authorization", "Basic bW10dXFIaUdTMWlnRVBNWmhEeldNUTpSemRiNk5Yb3pJeFZRZk1UOU5aOUwzQ1NjbnZidzhCeQ==");
                request.AddHeader("Authorization", "Basic " + base64Encoded);


                var response = await client.ExecuteAsync(request);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var tokenResponse = JsonConvert.DeserializeObject<ZoomTokenResponse>(response.Content);
                    _ = await _settingsRepository.AddUpdateZoomOAuthToken(tokenResponse.access_token);
                    return tokenResponse.access_token;
                }
                return null;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
    }

}
