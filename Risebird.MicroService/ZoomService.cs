using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Twilio.Types;

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
        private readonly string TranscriptsContainer;
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
            TranscriptsContainer = _configuration["SASURLSettings:TranscriptsContainer"];
            //SASConnectionString = _configuration["SASURLSettings:ConnectionString"];


        }
        [FunctionName("ZoomService")]
        public async Task Run([TimerTrigger("0 0 */1 * * *")] TimerInfo myTimer, ILogger log) //after every 1 hour 
        {
            try
            {
                //get all the completed meetings where video upload is pending

                var pendingVideoMeetings = await _meetingRespository.GetPendingVideoMeetings();

                var pendingRBMeetings = pendingVideoMeetings.Where(x => Regex.IsMatch(x.ZoomMeetingId, "[a-zA-Z]")).ToList();

                var pendingZoomMeetings = pendingVideoMeetings.Except(pendingRBMeetings).ToList();

                //get jwt authentication token 
                //string token = ZoomToken();
                string token = _zoomHelper.ZoomToken();
                
                Parallel.ForEach(pendingZoomMeetings, meeting =>
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

                        //create HTTP Rest Client
                        //var client = new RestClient(string.Concat(_configuration["ZoomAppDetails:endPoint"], $"/meetings/{meeting.ZoomMeetingId}/recordings"));
                        var client = new RestClient(string.Concat(_configuration["ZoomAppDetails:endPoint"], $"meetings/{meeting.ZoomMeetingId}/recordings"));

                        //create HTTP Request 
                        var request = new RestRequest(string.Concat(_configuration["ZoomAppDetails:endPoint"], $"meetings/{meeting.ZoomMeetingId}/recordings"), Method.Get);
                        request.AddHeader("content-type", "application/json");
                        request.AddHeader("authorization", "Bearer " + token);
                        RestResponse response = client.Execute(request);
                        var content = response.Content;

                        var convertedResponse = JsonConvert.DeserializeObject<ZoomRecordingResponse>(content);

                        //get the download url from the response
                        if (convertedResponse != null && convertedResponse.recording_files != null && convertedResponse.recording_files.Count() > 0)
                        {
                            var videoRecordingFiles = convertedResponse.recording_files.Where(x => x.file_type == "MP4" && x.status.ToLower() == "completed");
                            string videoSasUrl = string.Empty;
                            int? videoDuration = null;
                            if (string.IsNullOrEmpty(meeting.VideoLink) && (videoRecordingFiles != null || videoRecordingFiles.Count() == 1))
                            {
                                var recordingFile = videoRecordingFiles.FirstOrDefault();
                                var downloadUrl = recordingFile.download_url;

                                //upload the video to azure blob using the download_Url

                                //WebClient myWebClient = new WebClient();
                                HttpClient myWebClient = new HttpClient();
                                string uploadedVideoUrl = string.Empty;
                                using (Stream myStream = myWebClient.GetStreamAsync(downloadUrl).Result)
                                {
                                    var topic = convertedResponse.topic?.Replace(" ", "_").Replace("/", "-").Replace("\t", "").Replace("\n", "").Replace("\r", "");
                                    var fileName = string.IsNullOrEmpty(topic) ? meeting.interviewDate.ToString("yyyy-MM-ddTHH:mm:ss") : topic;
                                    long unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                                    fileName = fileName + "_" + unixTime.ToString();
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
                                    var sas = _sasUrlHelper.CreateSasUrl(ContainerName);

                                    if (!string.IsNullOrEmpty(uploadedVideoUrl) && !string.IsNullOrEmpty(sas))
                                    {
                                        videoSasUrl = uploadedVideoUrl + "?" + sas;
                                    }
                                }
                                if (!string.IsNullOrEmpty(videoSasUrl))
                                {
                                    //get the video duration
                                    var startTime = Convert.ToDateTime(recordingFile.recording_start);
                                    var endTime = Convert.ToDateTime(recordingFile.recording_end);

                                    TimeSpan ts = endTime - startTime;
                                    videoDuration = Convert.ToInt32(Math.Round(ts.TotalMinutes));

                                }
                            }

                            var audioTranscriptFiles = convertedResponse.recording_files.Where(x => x.file_type == "TRANSCRIPT" && x.status.ToLower() == "completed");
                            string transcriptSasUrl = string.Empty;
                            if (!string.IsNullOrEmpty(meeting.AudioTranscriptFileLink) && (audioTranscriptFiles != null || audioTranscriptFiles.Count() == 1))
                            {
                                var transcriptFile = audioTranscriptFiles.FirstOrDefault();
                                var downloadUrl = transcriptFile.download_url;

                                //upload the video to azure blob using the download_Url

                                HttpClient myWebClient = new HttpClient();
                                string uploadedTranscriptUrl = string.Empty;
                                using (Stream myStream = myWebClient.GetStreamAsync(downloadUrl).Result)
                                {
                                    var topic = convertedResponse.topic?.Replace(" ", "_").Replace("/", "-").Replace("\t", "").Replace("\n", "").Replace("\r", "");
                                    var fileName = (string.IsNullOrEmpty(topic) ? meeting.interviewDate.ToString("yyyy-MM-ddTHH:mm:ss") : topic) + "_Transcript";
                                    long unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                                    fileName = fileName + "_" + unixTime.ToString();
                                    try
                                    {
                                        uploadedTranscriptUrl = _azureStorageHelper.InsertBlobCloudCompare(fileName, TranscriptsContainer, null, null, myStream).Result;

                                    }
                                    catch (Exception ex)
                                    {
                                        _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                        log.LogError($"Zoom : Error while inserting blob for meeting with id: {meeting.imid} and ZoomMeetingId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message: {ex.Message}");
                                        return;
                                    }

                                    log.LogInformation($"Zoom : Video uploaded in blob for meeting with ZoomMeetingId {meeting.ZoomMeetingId} function executed at: {DateTime.Now}");


                                    //generate SAS URL from the uploaded video url
                                    var transcriptSasToken = _sasUrlHelper.CreateSasUrl(TranscriptsContainer);

                                    if (!string.IsNullOrEmpty(uploadedTranscriptUrl) && !string.IsNullOrEmpty(transcriptSasToken))
                                    {
                                        transcriptSasUrl = uploadedTranscriptUrl + "?" + transcriptSasToken;
                                    }
                                }
                            }

                            if (!string.IsNullOrEmpty(videoSasUrl) || !string.IsNullOrEmpty(transcriptSasUrl))
                            {

                                //insert the SAS URL of the video, transcript and video duration in the database table
                                _ = _meetingRespository.UpdateUploadedVideoLinkAndTranscriptLink(meeting.imid, videoDuration, videoSasUrl, transcriptSasUrl).Result;

                                log.LogInformation($"Zoom : Video link and/or transcript url updated in database for meeting with Id {meeting.ZoomMeetingId} at: {DateTime.Now}");

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
                        var request = new RestRequest("https://meet.risebird.io/api/v1/room/" + meeting.ZoomMeetingId, Method.Get);
                        request.AddHeader("authorization", _configuration["RBMeetingDetails:apiKey"]);//"v7sy3J8cAFdEJp5S");
                        RestResponse response = _meeting.Execute(request);
                        var content = response.Content;
                        if (!string.IsNullOrEmpty(content))
                        {
                            content = content.Replace("\n", "").Replace("\t", "").Replace("\r", "");
                        }
                        var convertedResponse = JsonConvert.DeserializeObject<RBMeetCreateMeetingResponse>(content);

                        string rbMeetVideoLink = convertedResponse.videoUrl;
                        int? rbMeetVideoDurationInSeconds = convertedResponse.duration;
                        int rbMeetVideoDurationInMinutes = 0;
                        if (rbMeetVideoDurationInSeconds.HasValue)
                        {
                            rbMeetVideoDurationInMinutes = (rbMeetVideoDurationInSeconds.Value / 60);
                        }

                        if (!string.IsNullOrEmpty(rbMeetVideoLink))
                        {
                            _ = _meetingRespository.UpdateUploadedVideoLink(meeting.imid, rbMeetVideoLink, rbMeetVideoDurationInMinutes).Result;
                            log.LogInformation($"RBMeet : Video link updated in database for meeting with Id {meeting.ZoomMeetingId} at: {DateTime.Now}");
                        }

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
