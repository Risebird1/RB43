using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using Twilio.Jwt.AccessToken;

namespace Risebird.MicroService
{
    public class DeleteZoomMeetings
    {

        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IMeetingRespository _meetingRespository;
        private readonly IZoomHelper _zoomHelper;
        

        public DeleteZoomMeetings(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, IMeetingRespository meetingRespository, IZoomHelper zoomHelper)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _meetingRespository = meetingRespository;
            _zoomHelper = zoomHelper;
        }

        [FunctionName("DeleteZoomMeetings")]
        public async Task Run([TimerTrigger("0 0 */3 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"DeleteZoomMeetings : C# Timer trigger function started executing at: {DateTime.Now}");

            var meetingsToBeDeleted = await _meetingRespository.GetZoomMeetingsToBeDeletedOnceUploadedOnBlob();
            
            string token = _zoomHelper.ZoomToken();

            Parallel.ForEach(meetingsToBeDeleted, meeting =>
            {
                try
                {
                    if (!string.IsNullOrEmpty(meeting.zoomLink))
                    {
                        //long zoommeetingId = 0;
                        string zoommeetingId;
                        try
                        {
                            //zoommeetingId = Convert.ToInt64(meeting.zoomLink.Split('/').Last());
                            zoommeetingId = (meeting.zoomLink.Split('/').LastOrDefault()).Split('?').FirstOrDefault();
                        }
                        catch (Exception)
                        {
                            try
                            {
                                var link = meeting.zoomLink.Split('?').First();
                                //zoommeetingId = Convert.ToInt64(link.Split('/').Last());
                                zoommeetingId = link.Split('/').Last();
                            }
                            catch (Exception)
                            {
                                //zoommeetingId = 0;
                                zoommeetingId = null;
                            }
                        }
                        //meeting.ZoomMeetingId = zoommeetingId.ToString();
                        meeting.ZoomMeetingId = zoommeetingId;
                    }

                    //delete recording
                    //create HTTP Request 

                    var clientDeleteMeetingRecordings = new RestClient(string.Concat(_configuration["ZoomAppDetails:endPoint"], $"meetings/{meeting.ZoomMeetingId}/recordings?action=trash"));

                    var requestDeleteMeetingRecordings = new RestRequest(string.Concat(_configuration["ZoomAppDetails:endPoint"], $"meetings/{meeting.ZoomMeetingId}/recordings?action=trash"), Method.Delete);
                    requestDeleteMeetingRecordings.AddHeader("content-type", "application/json");
                    requestDeleteMeetingRecordings.AddHeader("authorization", "Bearer " + token);
                    RestResponse responseDeleteMeetingRecordings = clientDeleteMeetingRecordings.Execute(requestDeleteMeetingRecordings);
                    //var content = response.Content;
                    if (responseDeleteMeetingRecordings.StatusCode == System.Net.HttpStatusCode.NoContent) //status code 204, indicates success
                    {
                        log.LogInformation($"Zoom : Meeting Recordings with ZoomMeetingId : {meeting.ZoomMeetingId} deleted at: {DateTime.Now}");
                    }
                    else
                    {
                        string message = (responseDeleteMeetingRecordings != null && responseDeleteMeetingRecordings.Content != null) ? responseDeleteMeetingRecordings.Content : string.Empty;
                        log.LogInformation($"Error! Zoom : Meeting Recordings with ZoomMeetingId : {meeting.ZoomMeetingId} could not be deleted! Message : {responseDeleteMeetingRecordings.Content ?? responseDeleteMeetingRecordings.StatusDescription}");
                    }

                }
                catch (Exception ex)
                {
                    _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    log.LogError($"DeleteZoomMeetings : Video Link error for meeting with id: {meeting.imid} and RoomId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message : {ex.Message}");
                    return;
                }
            });

            log.LogInformation($"DeleteZoomMeetings : Zoom Service executed at: {DateTime.Now}");
        }
    }
}
