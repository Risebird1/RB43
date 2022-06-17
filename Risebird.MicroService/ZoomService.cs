using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using RestSharp;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class ZoomService
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IMeetingRespository _meetingRespository;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IZoomHelper _zoomHelper;
        private readonly ISASUrlHelper _sasUrlHelper;

        private readonly string ContainerName;
        //private readonly string SASConnectionString;

        public ZoomService(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, IMeetingRespository meetingRespository, IZoomHelper zoomHelper, ISASUrlHelper sasUrlHelper)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _meetingRespository = meetingRespository;
            _zoomHelper = zoomHelper;
            _sasUrlHelper = sasUrlHelper;
            ContainerName = _configuration["SASURLSettings:ContainerName"];
            //SASConnectionString = _configuration["SASURLSettings:ConnectionString"];


        }
        [FunctionName("ZoomService")]
        public async Task Run([TimerTrigger("0 0 */10 * * *")] TimerInfo myTimer, ILogger log) //after every 6 hours
        {
            try
            {
                //get all the completed meetings where video upload is pending

                var pendingVideoMeetings = await _meetingRespository.GetPendingVideoMeetings();
                
                var pendingZoomMeetings = pendingVideoMeetings?.Where(x=>x.MeetingPlatform == null || x.MeetingPlatform == MeetingPlatforms.Zoom);
                var pendingRBMeetings = pendingVideoMeetings?.Where(x=>x.MeetingPlatform == MeetingPlatforms.RisebirdMeet);

                //get jwt authentication token 
                //string token = ZoomToken();
                string token = _zoomHelper.ZoomToken();
                // zoom : upload video and dump video link in db
                Parallel.ForEach(pendingZoomMeetings, meeting =>
                {

                    try
                    {
                        if (!string.IsNullOrEmpty(meeting.ZoomMeetingId))
                        {
                            if (meeting.ZoomMeetingId == "0" && !string.IsNullOrEmpty(meeting.zoomLink))
                            {
                                //long zoommeetingId = 0;
                                string zoommeetingId;
                                try
                                {
                                    //zoommeetingId = Convert.ToInt64(meeting.zoomLink.Split('/').Last());
                                    zoommeetingId = meeting.zoomLink.Split('/').Last();
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

                            

                            //create HTTP Rest Client
                            var client = new RestClient(string.Concat(_configuration["ZoomAppDetails:endPoint"], $"/meetings/{meeting.ZoomMeetingId}/recordings"));

                            //create HTTP Request 
                            var request = new RestRequest(Method.GET);
                            request.AddHeader("content-type", "application/json");
                            request.AddHeader("authorization", "Bearer " + token);
                            IRestResponse response = client.Execute(request);
                            var content = response.Content;

                            var convertedResponse = JsonConvert.DeserializeObject<ZoomRecordingResponse>(content);

                            //get the download url from the response
                            if (convertedResponse.recording_files != null && convertedResponse.recording_files.Count() > 0)
                            {
                                var videoRecordingFiles = convertedResponse.recording_files.Where(x => x.file_type == "MP4" && x.status.ToLower() == "completed");

                                if (videoRecordingFiles != null || videoRecordingFiles.Count() == 1)
                                {
                                    var recordingFile = videoRecordingFiles.FirstOrDefault();
                                    var downloadUrl = recordingFile.download_url;

                                    //upload the video to azure blob using the download_Url

                                    WebClient myWebClient = new WebClient();
                                    string uploadedVideoUrl = string.Empty;
                                    string sasUrl = string.Empty;
                                    using (Stream myStream = myWebClient.OpenRead(downloadUrl))
                                    {
                                        var topic = convertedResponse.topic?.Replace(" ", "_").Replace("/", "-").Replace("\t", "").Replace("\n", "").Replace("\r", "");
                                        var fileName = string.IsNullOrEmpty(topic) ? meeting.interviewDate.ToString("yyyy-MM-ddTHH:mm:ss") : topic;

                                        try
                                        {
                                            uploadedVideoUrl = _azureStorageHelper.InsertBlobCloudCompare(fileName, ContainerName, null, myStream).Result;

                                        }
                                        catch (Exception ex)
                                        {
                                            _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                            log.LogError($"Zoom : Error while inserting blob for meeting with id: {meeting.imid} and ZoomMeetingId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message: {ex.Message}");
                                            return;
                                        }

                                        log.LogInformation($"Zoom : Video uploaded in blob for meeting with ZoomMeetingId {meeting.ZoomMeetingId} function executed at: {DateTime.Now}");


                                        //generate SAS URL from the uploaded video url
                                        //var sas = CreateSasUrl();
                                        var sas = _sasUrlHelper.CreateSasUrl(ContainerName);

                                        if (!string.IsNullOrEmpty(uploadedVideoUrl) && !string.IsNullOrEmpty(sas))
                                        {
                                            sasUrl = uploadedVideoUrl + "?" + sas;
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(sasUrl))
                                    {
                                        //get the video duration
                                        var startTime = Convert.ToDateTime(recordingFile.recording_start);
                                        var endTime = Convert.ToDateTime(recordingFile.recording_end);

                                        TimeSpan ts = endTime - startTime;
                                        var videoDuration = Convert.ToInt32(Math.Round(ts.TotalMinutes));

                                        //insert the SAS URL of the video in the database table
                                        var result = _meetingRespository.UpdateUploadedVideoLink(meeting.imid, sasUrl, videoDuration).Result;

                                        log.LogInformation($"Zoom : Video link updated in database for meeting with Id {meeting.ZoomMeetingId} at: {DateTime.Now}");


                                    }
                                }


                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        log.LogError($"Zoom : Video upload error for meeting with id: {meeting.imid} and ZoomMeetingId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message : {ex.Message}");
                        return;
                    }
                });


                // RBMeet : fetch video link and dump in db
                Parallel.ForEach(pendingRBMeetings, meeting =>
                {
                    try
                    {
                        var _meeting = new RestClient("https://meet.risebird.io/api/v1/room/" + meeting.ZoomMeetingId);
                        var request = new RestRequest(Method.GET);
                        request.AddHeader("authorization", _configuration["RBMeetingDetails:apiKey"]);//"v7sy3J8cAFdEJp5S");
                        IRestResponse response = _meeting.Execute(request);
                        var content = response.Content;
                        var convertedResponse = JsonConvert.DeserializeObject<RBMeetCreateMeetingResponse>(content);
                        
                        string rbMeetVideoLink = convertedResponse.videoUrl;
                        float rbMeetVideoDurationInSeconds = convertedResponse.duration;
                        int rbMeetVideoDurationInMinutes = ((int)rbMeetVideoDurationInSeconds / 60);

                        _ = _meetingRespository.UpdateUploadedVideoLink(meeting.imid, rbMeetVideoLink, rbMeetVideoDurationInMinutes).Result;

                    }
                    catch (Exception ex)
                    {
                        _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        log.LogError($"RBMeet : Video Link error for meeting with id: {meeting.imid} and RoomId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message : {ex.Message}");
                        return;
                    }
                });

                log.LogInformation($"C# Zoom Service function(Video Upload) executed at: {DateTime.Now}");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

    }
}
