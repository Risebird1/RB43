using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using RestSharp;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class ZoomMeetingCancel
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IConfiguration _configuration;
        private readonly IMeetingService _meetingService;
        private readonly IZoomHelper _zoomHelper;


        public ZoomMeetingCancel(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger,
            IMeetingService meetingService, IZoomHelper zoomHelper)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _meetingService = meetingService;
            _zoomHelper = zoomHelper;
        }
        [FunctionName("ZoomMeetingCancel")]
        public async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            var candidatemeetinglist = await _meetingService.FetchCandidateZoomMeetingsIdForCancel();
            foreach (var item in candidatemeetinglist)
            {
                try
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        await DeleteMeetingLink(item);
                    }
                }
                catch (Exception ex)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                }
            }
        }
        public async Task<string> DeleteMeetingLink(string meetingid)
        {
            //string token = ZoomToken();

            //get oauth token
            string token = await _zoomHelper.GenerateAccessToken();

            var client = new RestClient(string.Concat(_configuration["ZoomAppDetails:endPoint"], $"meetings/" + meetingid));
            var request = new RestRequest(string.Concat(_configuration["ZoomAppDetails:endPoint"], $"meetings/" + meetingid), Method.Delete);
            request.AddHeader("content-type", "application/json");
            request.AddHeader("authorization", "Bearer " + token);
            RestResponse response = client.Execute(request);
            var content = response.Content;
            return "Deleted successfully";
        }
        //public string ZoomToken()
        //{
        //    DateTime Expiry = DateTime.UtcNow.AddDays(1);
        //    string ApiKey = _configuration["ZoomAppDetails:apiKey"];
        //    string ApiSecret = _configuration["ZoomAppDetails:secretKey"];
        //    int ts = (int)(Expiry - new DateTime(1970, 1, 1)).TotalSeconds;
        //    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ApiSecret));
        //    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        //    var header = new JwtHeader(credentials);
        //    var payload = new JwtPayload
        //    {
        //        { "iss", ApiKey},
        //        { "exp", ts },
        //    };
        //    var secToken = new JwtSecurityToken(header, payload);
        //    var handler = new JwtSecurityTokenHandler();
        //    var tokenString = handler.WriteToken(secToken);
        //    return tokenString;
        //}
    }
}
