using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Risebird.CommonHelper;
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
        private readonly ISASUrlHelper _sasUrlHelper;
        private readonly ILogger log;
        private readonly IZohoMeetingHelper _zoho;

        public FetchMeetingVideoService(ILoggerFactory loggerFactory, IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, IMeetingRespository meetingRespository, ISASUrlHelper sasUrlHelper, IZohoMeetingHelper zoho)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _meetingRespository = meetingRespository;
            _sasUrlHelper = sasUrlHelper;
            log = loggerFactory.CreateLogger<FetchMeetingVideoService>();
            _zoho = zoho;
        }

        [Function("FetchMeetingVideoService")]
        public async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            try
            {
                string videoSasUrl = string.Empty;
                int? videoDuration = null;
                string transcriptSasUrl = string.Empty;
                var pendingVideoMeetings = await _meetingRespository.FetchMeetingPendingVideo();
                //get oauth token
                var accesstoken = await _zoho.GetRecordingAccessTokenAsync();
                Parallel.ForEach(pendingVideoMeetings, meeting =>
                {
                    try
                    {
                        var ContainerName = meeting.meetingtype.ToLower();
                        var recordinginfo = _zoho.GetRecordingAsync(meeting.ZoomMeetingId, accesstoken).Result;
                        int maxduration = 0;
                        if (recordinginfo.Count() > 0)
                            maxduration = recordinginfo.Max(r => r.duration);
                        var _recordingupload = recordinginfo?.Where(x => x.duration == maxduration).FirstOrDefault();
                        var downloadUrl = _recordingupload?.downloadUrl;
                        //get the download url from the response
                        if (downloadUrl != null)
                        {
                            HttpClient myWebClient = new HttpClient();
                            string uploadedVideoUrl = string.Empty;
                            var accessToken = accesstoken;
                            myWebClient.DefaultRequestHeaders.Authorization =
                                new System.Net.Http.Headers.AuthenticationHeaderValue("Zoho-oauthtoken", accessToken?.ToString());
                            string videoSasUrl = string.Empty;
                            int? videoDuration = null;

                            using (Stream myStream = myWebClient.GetStreamAsync(downloadUrl).Result)
                            {
                                var topic = _recordingupload.erecordingId;
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
                                    log.LogError($"Zoho : Error while inserting blob for meeting with id: {meeting.imid} and ZoomMeetingId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message: {ex.Message}");
                                    return;
                                }
                                log.LogInformation($"Zoho : Video uploaded in blob for meeting with ZoomMeetingId {meeting.ZoomMeetingId} function executed at: {DateTime.Now}");
                                //generate SAS URL from the uploaded video url
                                var sas = _sasUrlHelper.CreateSasUrl(ContainerName);
                                if (!string.IsNullOrEmpty(uploadedVideoUrl) && !string.IsNullOrEmpty(sas))
                                {
                                    videoSasUrl = uploadedVideoUrl + "?" + sas;
                                }
                            }

                        }
                        if (!string.IsNullOrEmpty(videoSasUrl))
                        {
                            videoDuration = _recordingupload.duration / 60000;
                        }
                        var transcriptionDownloadUrl = _recordingupload.transcriptionDownloadUrl;
                        if (transcriptionDownloadUrl != null)
                        {
                            HttpClient myWebClient = new HttpClient();
                            string uploadedTranscriptUrl = string.Empty;
                            var accessToken = accesstoken;
                            myWebClient.DefaultRequestHeaders.Authorization =
                                new System.Net.Http.Headers.AuthenticationHeaderValue("Zoho-oauthtoken", accessToken?.ToString());
                            using (Stream myStream = myWebClient.GetStreamAsync(transcriptionDownloadUrl).Result)
                            {
                                var topic = _recordingupload.erecordingId;
                                var fileName = (string.IsNullOrEmpty(topic) ? meeting.interviewDate.ToString("yyyy-MM-ddTHH:mm:ss") : topic) + "_Transcript";
                                long unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                                fileName = fileName + "_" + unixTime.ToString();
                                try
                                {
                                    uploadedTranscriptUrl = _azureStorageHelper.InsertBlobCloudCompare(fileName, ContainerName, null, null, myStream).Result;
                                }
                                catch (Exception ex)
                                {
                                    _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                    log.LogError($"Zoho : Error while inserting blob for meeting with id: {meeting.imid} and ZohoMeetingId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message: {ex.Message}");
                                    return;
                                }
                                log.LogInformation($"Zoho : Video uploaded in blob for meeting with ZohoMeetingId {meeting.ZoomMeetingId} function executed at: {DateTime.Now}");
                                //generate SAS URL from the uploaded video url
                                var transcriptSasToken = _sasUrlHelper.CreateSasUrl(ContainerName);
                                if (!string.IsNullOrEmpty(uploadedTranscriptUrl) && !string.IsNullOrEmpty(transcriptSasToken))
                                {
                                    transcriptSasUrl = uploadedTranscriptUrl + "?" + transcriptSasToken;
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(videoSasUrl) || !string.IsNullOrEmpty(transcriptSasUrl))
                        {
                            //insert the SAS URL of the video, transcript and video duration in the database table
                            _ = _meetingRespository.UpdateVideoinfo(meeting.imid, videoSasUrl, meeting.meetingtype, transcriptSasUrl).Result;
                            log.LogInformation($"Zoho : Video link and/or transcript url updated in database for meeting with Id {meeting.ZoomMeetingId} at: {DateTime.Now}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        log.LogError($"Zoho : Video upload error for meeting with id: {meeting.imid} and ZoomMeetingId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message : {ex.Message}");
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
