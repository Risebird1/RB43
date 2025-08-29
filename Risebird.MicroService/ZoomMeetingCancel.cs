using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RestSharp;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using System;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class ZoomMeetingCancel
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IConfiguration _configuration;
        private readonly IMeetingService _meetingService;
        private readonly IZoomHelper _zoomHelper;
        private readonly ILogger log;
        private readonly IZohoMeetingHelper _zoho;

        public ZoomMeetingCancel(ILoggerFactory loggerFactory, IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger,
            IMeetingService meetingService, IZoomHelper zoomHelper, IZohoMeetingHelper zoho)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _meetingService = meetingService;
            _zoomHelper = zoomHelper;
            log = loggerFactory.CreateLogger<ZoomMeetingCancel>();
            _zoho = zoho;
        }
        [Function("ZoomMeetingCancel")]
        public async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            var candidatemeetinglist = await _meetingService.FetchCandidateZoomMeetingsIdForCancel(MeetingPlatforms.Zoom);
            var Zohocandidatemeetinglist = await _meetingService.FetchCandidateZoomMeetingsIdForCancel(MeetingPlatforms.Zoho);
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
            foreach (var item in Zohocandidatemeetinglist)
            {
                try
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        await _zoho.DeleteMeetingAsync(item);
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
