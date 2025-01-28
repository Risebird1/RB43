using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;

namespace Risebird.AzureTimerFunctions
{
    public class FetchMeetingVideoService
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IMeetingRespository _meetingRespository;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IZoomHelper _zoomHelper;
        private readonly ISASUrlHelper _sasUrlHelper;
        private readonly ILogger log;

        public FetchMeetingVideoService(ILoggerFactory loggerFactory, IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, IMeetingRespository meetingRespository, IZoomHelper zoomHelper, ISASUrlHelper sasUrlHelper)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _meetingRespository = meetingRespository;
            _zoomHelper = zoomHelper;
            _sasUrlHelper = sasUrlHelper;
            log = loggerFactory.CreateLogger<FetchMeetingVideoService>();
        }

        [Function("FetchMeetingVideoService")]
        public async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            try
            {
                var pendingVideoMeetings = await _meetingRespository.FetchMeetingPendingVideo();
                //get oauth token
                string token = await _zoomHelper.GenerateAccessToken();
                Parallel.ForEach(pendingVideoMeetings, meeting =>
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(meeting.zoomLink))
                        {
                            string zoommeetingId;
                            try
                            {
                                zoommeetingId = (meeting.zoomLink.Split('/').LastOrDefault()).Split('?').FirstOrDefault();
                            }
                            catch (Exception)
                            {
                                try
                                {
                                    var link = meeting.zoomLink.Split('?').First();
                                    zoommeetingId = link.Split('/').Last();
                                }
                                catch (Exception)
                                {
                                    zoommeetingId = null;
                                }
                            }
                            meeting.ZoomMeetingId = zoommeetingId;
                        }
                        var ContainerName = meeting.meetingtype.ToLower();
                        //create HTTP Rest Client
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
                            if ((string.IsNullOrEmpty(meeting.VideoLink) || (!string.IsNullOrEmpty(meeting.VideoLink) && meeting.VideoDuration < 15)) && (videoRecordingFiles != null || videoRecordingFiles.Count() == 1))
                            {
                                var recordingFile = videoRecordingFiles?.FirstOrDefault();
                                var downloadUrl = recordingFile?.download_url;

                                //upload the video to azure blob using the download_Url
                                if (downloadUrl != null)
                                {
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
                                }
                            }
                            if (!string.IsNullOrEmpty(videoSasUrl))
                            {
                                //insert the SAS URL of the video, transcript and video duration in the database table
                                _ = _meetingRespository.UpdateVideoinfo(meeting.imid, videoSasUrl, meeting.meetingtype).Result;
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
                log.LogInformation($"C# Zoom Service function(Video Upload) executed at: {DateTime.Now}");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
