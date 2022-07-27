using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RestSharp;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;

namespace Risebird.MicroService
{
    public class DeleteOldZoomMeetingsService
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IMeetingRespository _meetingRespository;
        private readonly IZoomHelper _zoomHelper;
        
        public DeleteOldZoomMeetingsService(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, IMeetingRespository meetingRespository, IZoomHelper zoomHelper)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _meetingRespository = meetingRespository;
            _zoomHelper = zoomHelper;

        }
        [FunctionName("DeleteOldZoomMeetingsService")]
        public async Task Run([TimerTrigger("0 0 3 * * *")] TimerInfo myTimer, ILogger log) //daily at 3 am ; 0 * * * * *   0 0 3 * * *
        {
            try
            {
                //get all the meetings older than 45 days

                var meetingsToBeDeleted = await _meetingRespository.GetZoomMeetingsToBeDeleted();

                //get jwt authentication token 
                
                string token = _zoomHelper.ZoomToken();

                // delete zoom meeting and its recording from Zoom
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


                        //Delete meeting
                        //create HTTP Rest Client
                        var clientDeleteMeeting = new RestClient(string.Concat(_configuration["ZoomAppDetails:endPoint"], $"meetings/{meeting.ZoomMeetingId}"));

                        //create HTTP Request 
                        var requestDeleteMeeting = new RestRequest(Method.DELETE);
                        requestDeleteMeeting.AddHeader("content-type", "application/json");
                        requestDeleteMeeting.AddHeader("authorization", "Bearer " + token);
                        IRestResponse responseDeleteMeeting = clientDeleteMeeting.Execute(requestDeleteMeeting);

                        //var content = response.Content;
                        if (responseDeleteMeeting != null && responseDeleteMeeting.StatusCode == System.Net.HttpStatusCode.NoContent) //status code 204, indicates success
                        {
                            log.LogInformation($"Zoom : Meeting with ZoomMeetingId : {meeting.ZoomMeetingId} deleted at: {DateTime.Now}");
                        }
                        else
                        {
                            string message = (responseDeleteMeeting != null && responseDeleteMeeting.Content != null) ? responseDeleteMeeting.Content : string.Empty;
                            log.LogInformation($"Error! Zoom : Meeting with ZoomMeetingId : {meeting.ZoomMeetingId} could not be deleted! Message : {message}");
                        }

                        //Delete video recordings for the meeting
                        //create HTTP Request 

                        var clientDeleteMeetingRecordings = new RestClient(string.Concat(_configuration["ZoomAppDetails:endPoint"], $"meetings/{meeting.ZoomMeetingId}/recordings?action=trash"));

                        var requestDeleteMeetingRecordings = new RestRequest(Method.DELETE);
                        requestDeleteMeetingRecordings.AddHeader("content-type", "application/json");
                        requestDeleteMeetingRecordings.AddHeader("authorization", "Bearer " + token);
                        IRestResponse responseDeleteMeetingRecordings = clientDeleteMeetingRecordings.Execute(requestDeleteMeetingRecordings);
                        //var content = response.Content;
                        if (responseDeleteMeetingRecordings.StatusCode == System.Net.HttpStatusCode.NoContent) //status code 204, indicates success
                        {
                            log.LogInformation($"Zoom : Meeting Recordings with ZoomMeetingId : {meeting.ZoomMeetingId} deleted at: {DateTime.Now}");
                        }
                        else
                        {
                            string message = (responseDeleteMeetingRecordings != null && responseDeleteMeetingRecordings.Content != null) ? responseDeleteMeeting.Content : string.Empty;
                            log.LogInformation($"Error! Zoom : Meeting Recordings with ZoomMeetingId : {meeting.ZoomMeetingId} could not be deleted! Message : {responseDeleteMeetingRecordings.Content ?? responseDeleteMeetingRecordings.StatusDescription}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        log.LogError($"Zoom : Video upload error for meeting with id: {meeting.imid} and ZoomMeetingId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message : {ex.Message}");
                        return;
                    }
                });

                log.LogInformation($"C# Delete old Zoom Meetings Service function executed at: {DateTime.Now}");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

    }
}
