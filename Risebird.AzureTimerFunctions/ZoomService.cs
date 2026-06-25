using Azure.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository;
using Risebird.Repository.Interface;
using System.Net;
using System.Text.RegularExpressions;

namespace Risebird.AzureTimerFunctions
{
    public class ZoomService
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IMeetingRespository _meetingRespository;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IZoomHelper _zoomHelper;
        private readonly ISASUrlHelper _sasUrlHelper;
        private readonly ITeamsMeetingHelper _iteamsMeetingHelper;
        private readonly IGoogleEventHelper _googleEventHelper;
        private readonly ILogger log;
        private readonly string ContainerName;
        private readonly string TranscriptsContainer;
        private readonly IZohoMeetingHelper _zoho;
        private readonly IOpenAIInterviewerAssistantHelper _ai;
        //private readonly string SASConnectionString;

        public ZoomService(ILoggerFactory loggerFactory, IConfiguration configuration,
            IApplicationErrorLogger applicationErrorLogger,
            IAzureStorageHelper azureStorageHelper, IMeetingRespository meetingRespository,
            IZoomHelper zoomHelper, ISASUrlHelper sasUrlHelper,
            ITeamsMeetingHelper iteamsMeetingHelper, IGoogleEventHelper googleEventHelper,
            IZohoMeetingHelper zoho, IOpenAIInterviewerAssistantHelper ai)
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
            _iteamsMeetingHelper = iteamsMeetingHelper;
            _googleEventHelper = googleEventHelper;
            log = loggerFactory.CreateLogger<ZoomService>();
            _zoho = zoho;
            _ai = ai;
        }
        [Function("ZoomService")]
        public async Task Run([TimerTrigger("0 */15 * * * *")] TimerInfo myTimer) //after every 30 min
        {
            try
            {
                //get all the completed meetings where video upload is pending

                var pendingVideoMeetings = await _meetingRespository.GetPendingVideoMeetings();

                //var pendingRBMeetings = pendingVideoMeetings.Where(x => Regex.IsMatch(x.ZoomMeetingId, "[a-zA-Z]")).ToList();
                var pendingRBMeetings = pendingVideoMeetings.Where(x => x.InterviewMeetingPlatForm == MeetingPlatforms.Zoho).ToList();

                var pendingTeamsMeetings = pendingVideoMeetings.Where(x => Regex.IsMatch(x.ZoomMeetingId, @"^(?=.*[0-9])(?=.*[a-zA-Z])([a-zA-Z0-9]+)$")).ToList();

                var pendingZoomMeetings = pendingVideoMeetings.Except(pendingRBMeetings).ToList();
                pendingZoomMeetings = pendingZoomMeetings.Except(pendingTeamsMeetings).ToList();

                //get jwt authentication token 
                //string token = _zoomHelper.ZoomToken();

                //get oauth token
                string token = await _zoomHelper.GenerateAccessToken();

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
                            string uploadedVideoClientExternalLink = string.Empty;
                            int? videoDuration = null;
                            if ((string.IsNullOrEmpty(meeting.VideoLink) || (!string.IsNullOrEmpty(meeting.VideoLink) && meeting.VideoDuration < 15)) && (videoRecordingFiles != null || videoRecordingFiles.Count() == 1))
                            {
                                var recordingFile = videoRecordingFiles?.FirstOrDefault();
                                var downloadUrl = recordingFile?.download_url;

                                //upload the video to azure blob using the download_Url

                                //WebClient myWebClient = new WebClient();
                                if (downloadUrl != null)
                                {
                                    HttpClient myWebClient = new HttpClient();
                                    string uploadedVideoUrl = string.Empty;
                                    //url of the video uploaded to the client's storage exclusively in cases where the client has requested for such a service. 
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

                                        }

                                        log.LogInformation($"Zoom : Video uploaded in blob for meeting with ZoomMeetingId {meeting.ZoomMeetingId} function executed at: {DateTime.Now}");

                                        //if (meeting.ClientCompanyName.ToLower() == ClientCompanyNames.Turing.ToLower())
                                        //{
                                        //    try
                                        //    {
                                        //        string fileNameExternal = "recording.mp4"; //meeting.imid.ToString() + 
                                        //        // Replace Azure logic with Google Cloud Storage upload
                                        //        myStream.Position = 0;
                                        //        uploadedVideoClientExternalLink = _googleEventHelper.UploadToGoogleCloudStorage(fileNameExternal, myStream, meeting.imid).Result;
                                        //    }
                                        //    catch (Exception ex)
                                        //    {
                                        //        _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                        //        log.LogError($"Error while uploading video to Google Cloud Storage for meeting with id: {meeting.imid} and ZoomMeetingId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message: {ex.Message}");
                                        //        
                                        //    }

                                        //    log.LogInformation($"Client External Storage Video Upload : Video uploaded in client's storage for meeting with ZoomMeetingId {meeting.ZoomMeetingId} function executed at: {DateTime.Now}");

                                        //}


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
                                    if (meeting.ClientCompanyName.ToLower() == ClientCompanyNames.Turing.ToLower())
                                    {
                                        using (Stream myStream = myWebClient.GetStreamAsync(downloadUrl).Result)
                                        {
                                            try
                                            {
                                                string fileNameExternal = "recording.mp4"; //meeting.imid.ToString() + 
                                                uploadedVideoClientExternalLink = _googleEventHelper.UploadToGoogleCloudStorage(fileNameExternal, myStream, meeting.imid).Result;
                                            }
                                            catch (Exception ex)
                                            {
                                                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                                log.LogError($"Error while uploading video to Google Cloud Storage for meeting with id: {meeting.imid} and ZoomMeetingId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message: {ex.Message}");

                                            }
                                        }

                                        log.LogInformation($"Client External Storage Video Upload : Video uploaded in client's storage for meeting with ZoomMeetingId {meeting.ZoomMeetingId} function executed at: {DateTime.Now}");

                                    }
                                }

                            }

                            var audioTranscriptFiles = convertedResponse.recording_files.Where(x => x.file_type == "TRANSCRIPT" && x.status.ToLower() == "completed");
                            string transcriptSasUrl = string.Empty;
                            string uploadedTranscriptClientExternalLink = string.Empty;
                            if (string.IsNullOrEmpty(meeting.AudioTranscriptFileLink) && (audioTranscriptFiles != null || audioTranscriptFiles.Count() == 1))
                            {
                                var transcriptFile = audioTranscriptFiles?.FirstOrDefault();
                                var downloadUrl = transcriptFile?.download_url;

                                //upload the video to azure blob using the download_Url
                                if (downloadUrl != null)
                                {
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

                                        }

                                        log.LogInformation($"Zoom : Video uploaded in blob for meeting with ZoomMeetingId {meeting.ZoomMeetingId} function executed at: {DateTime.Now}");

                                        //if (meeting.ClientCompanyName.ToLower() == ClientCompanyNames.Turing.ToLower())
                                        //{
                                        //    try
                                        //    {
                                        //        string fileNameExternal ="transcript.json"; // meeting.imid.ToString() + 
                                        //        myStream.Position = 0;
                                        //        // Replace Azure logic with Google Cloud Storage upload
                                        //        uploadedTranscriptClientExternalLink = _googleEventHelper.UploadToGoogleCloudStorage(fileNameExternal, myStream, meeting.imid).Result;
                                        //    }
                                        //    catch (Exception ex)
                                        //    {
                                        //        _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                        //        log.LogError($"Error while uploading video to Google Cloud Storage for meeting with id: {meeting.imid} and ZoomMeetingId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message: {ex.Message}");
                                        //        
                                        //    }

                                        //    log.LogInformation($"Client External Storage Video Upload : Video uploaded in client's storage for meeting with ZoomMeetingId {meeting.ZoomMeetingId} function executed at: {DateTime.Now}");

                                        //}
                                        //generate SAS URL from the uploaded video url
                                        var transcriptSasToken = _sasUrlHelper.CreateSasUrl(TranscriptsContainer);

                                        if (!string.IsNullOrEmpty(uploadedTranscriptUrl) && !string.IsNullOrEmpty(transcriptSasToken))
                                        {
                                            transcriptSasUrl = uploadedTranscriptUrl + "?" + transcriptSasToken;
                                        }

                                    }
                                    if (meeting.ClientCompanyName.ToLower() == ClientCompanyNames.Turing.ToLower())
                                    {
                                        using (Stream myStream = myWebClient.GetStreamAsync(downloadUrl).Result)
                                        {
                                            try
                                            {
                                                string fileNameExternal = "transcript.json"; // meeting.imid.ToString() + 
                                                uploadedTranscriptClientExternalLink = _googleEventHelper.UploadToGoogleCloudStorage(fileNameExternal, myStream, meeting.imid).Result;
                                            }
                                            catch (Exception ex)
                                            {
                                                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                                log.LogError($"Error while uploading video to Google Cloud Storage for meeting with id: {meeting.imid} and ZoomMeetingId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message: {ex.Message}");

                                            }
                                        }


                                        log.LogInformation($"Client External Storage Video Upload : Video uploaded in client's storage for meeting with ZoomMeetingId {meeting.ZoomMeetingId} function executed at: {DateTime.Now}");

                                    }
                                }

                            }

                            if (!string.IsNullOrEmpty(videoSasUrl) || !string.IsNullOrEmpty(transcriptSasUrl))
                            {

                                //insert the SAS URL of the video, transcript and video duration in the database table
                                _ = _meetingRespository.UpdateUploadedVideoLinkAndTranscriptLink(meeting.imid, videoDuration, videoSasUrl, transcriptSasUrl, uploadedVideoClientExternalLink, uploadedTranscriptClientExternalLink).Result;

                                log.LogInformation($"Zoom : Video link and/or transcript url updated in database for meeting with Id {meeting.ZoomMeetingId} at: {DateTime.Now}");

                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        log.LogError($"Zoom : Video upload error for meeting with id: {meeting.imid} and ZoomMeetingId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message : {ex.Message}");

                    }
                });
                var accesstoken = await _zoho.GetRecordingAccessTokenAsync();
                // RBMeet : fetch video link and dump in db
                if (pendingRBMeetings.Count() > 0)
                {
                    log.LogInformation($"Zoho: AccessToken :{accesstoken}");
                    Parallel.ForEach(pendingRBMeetings, async meeting =>
                    {
                        try
                        {
                            //ZohoAzureuploadVideoandTranscript(meeting);
                            string videoSasUrl = string.Empty;
                            string uploadedVideoClientExternalLink = string.Empty;
                            string uploadedTranscriptClientExternalLink = string.Empty;
                            int? videoDuration = null;
                            string transcriptSasUrl = string.Empty;

                            try
                            {
                                var recordinginfo = _zoho.GetRecordingAsync(meeting.ZoomMeetingId, accesstoken).Result;
                                int maxduration = 0;
                                if (recordinginfo.Count() > 0)
                                    maxduration = recordinginfo.Max(r => r.duration);
                                var _recordingupload = recordinginfo?.Where(x => x.duration == maxduration).FirstOrDefault();
                                if (string.IsNullOrEmpty(meeting.VideoLink) || (!string.IsNullOrEmpty(meeting.VideoLink) && meeting.VideoDuration < 15))
                                {
                                    var downloadUrl = _recordingupload?.downloadUrl;

                                    if (downloadUrl != null)
                                    {
                                        HttpClient myWebClient = new HttpClient();
                                        string uploadedVideoUrl = string.Empty;
                                        var accessToken = accesstoken;
                                        myWebClient.DefaultRequestHeaders.Authorization =
                                            new System.Net.Http.Headers.AuthenticationHeaderValue("Zoho-oauthtoken", accessToken?.ToString());
                                        //url of the video uploaded to the client's storage exclusively in cases where the client has requested for such a service. 
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
                                                log.LogError($"Zoho : Error while inserting blob for meeting with id: {meeting.imid} and ZohoMeetingId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message: {ex.Message}");
                                                return;
                                            }
                                            log.LogInformation($"Zoho : Video uploaded in blob for meeting with ZohoMeetingId {meeting.ZoomMeetingId} function executed at: {DateTime.Now}");
                                            //generate SAS URL from the uploaded video url
                                            var sas = _sasUrlHelper.CreateSasUrl(ContainerName);
                                            if (!string.IsNullOrEmpty(uploadedVideoUrl) && !string.IsNullOrEmpty(sas))
                                            {
                                                videoSasUrl = uploadedVideoUrl + "?" + sas;
                                            }
                                        }
                                        if (!string.IsNullOrEmpty(videoSasUrl))
                                        {
                                            videoDuration = _recordingupload.duration / 60000;
                                        }
                                        if (meeting.ClientCompanyName.ToLower() == ClientCompanyNames.Turing.ToLower())
                                        {
                                            using (Stream myStream = myWebClient.GetStreamAsync(downloadUrl).Result)
                                            {
                                                try
                                                {
                                                    string fileNameExternal = "recording.mp4";
                                                    uploadedVideoClientExternalLink = _googleEventHelper.UploadToGoogleCloudStorage(fileNameExternal, myStream, meeting.imid).Result;
                                                }
                                                catch (Exception ex)
                                                {
                                                    _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                                    log.LogError($"Error while uploading video to Google Cloud Storage for meeting with id: {meeting.imid} and ZohoMeetingId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message: {ex.Message}");
                                                }
                                            }
                                            log.LogInformation($"Client External Storage Video Upload : Video uploaded in client's storage for meeting with ZohoMeetingId {meeting.ZoomMeetingId} function executed at: {DateTime.Now}");
                                        }
                                    }
                                }
                                var transcriptSummaryCleanedJson = string.Empty;

                                if (string.IsNullOrEmpty(meeting.AudioTranscriptFileLink))
                                {
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
                                                uploadedTranscriptUrl = _azureStorageHelper.InsertBlobCloudCompare(fileName, TranscriptsContainer, null, null, myStream).Result;
                                            }
                                            catch (Exception ex)
                                            {
                                                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                                log.LogError($"Zoho : Error while inserting blob for meeting with id: {meeting.imid} and ZohoMeetingId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message: {ex.Message}");
                                                return;
                                            }
                                            log.LogInformation($"Zoho : Video uploaded in blob for meeting with ZohoMeetingId {meeting.ZoomMeetingId} function executed at: {DateTime.Now}");
                                            //generate SAS URL from the uploaded video url
                                            var transcriptSasToken = _sasUrlHelper.CreateSasUrl(TranscriptsContainer);
                                            if (!string.IsNullOrEmpty(uploadedTranscriptUrl) && !string.IsNullOrEmpty(transcriptSasToken))
                                            {
                                                transcriptSasUrl = uploadedTranscriptUrl + "?" + transcriptSasToken;
                                            }
                                        }
                                        if (meeting.ClientCompanyName.ToLower() == ClientCompanyNames.Turing.ToLower())
                                        {
                                            using (Stream myStream = myWebClient.GetStreamAsync(transcriptionDownloadUrl).Result)
                                            {
                                                try
                                                {
                                                    string fileNameExternal = "transcript.json"; // meeting.imid.ToString() + 
                                                    uploadedTranscriptClientExternalLink = _googleEventHelper.UploadToGoogleCloudStorage(fileNameExternal, myStream, meeting.imid).Result;
                                                }
                                                catch (Exception ex)
                                                {
                                                    _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                                    log.LogError($"Error while uploading video to Google Cloud Storage for meeting with id: {meeting.imid} and ZohoMeetingId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message: {ex.Message}");
                                                }
                                            }
                                            log.LogInformation($"Client External Storage Video Upload : Video uploaded in client's storage for meeting with ZohoMeetingId {meeting.ZoomMeetingId} function executed at: {DateTime.Now}");
                                        }

                                        ///write code to generate transcript summary
                                        //if (meeting.generateTranscriptSummary)
                                        //{
                                        var transcriptString = await DownloadTranscriptStringAsync(transcriptionDownloadUrl, accessToken);
                                        if (!string.IsNullOrEmpty(transcriptString))
                                        {
                                            var transcriptSummary = new TranscriptSummaryVM();
                                            var threadId = await _ai.CreateThreadAsync();
                                            await _ai.AddMessageAsync(threadId, "user", transcriptString);

                                            //await _ai.AddMessageAsync(threadId, "user", resume.ResumeLink);

                                            var runId = await _ai.CreateRunAsync(threadId, "asst_ebkDMtxPbZS8o6d6NabRGlrC"); //assistant id of the assistant named : AI Interviewee response extraction and analysis 
                                            await _ai.WaitForRunAsync(threadId, runId);


                                            var replies = await _ai.GetMessagesAsync(threadId);

                                            try
                                            {
                                                transcriptSummary = TryParseTranscriptSummaryVM(replies?.FirstOrDefault());
                                                if (transcriptSummary != null)
                                                {
                                                    transcriptSummaryCleanedJson = JsonConvert.SerializeObject(transcriptSummary);
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                            }
                                        }
                                        //}

                                    }
                                }
                                //upload the video to azure blob using the download_Url
                                if (!string.IsNullOrEmpty(videoSasUrl) || !string.IsNullOrEmpty(transcriptSasUrl))
                                {
                                    var result = _meetingRespository.UpdateUploadedVideoLinkAndTranscriptLink(meeting.imid, videoDuration, videoSasUrl, transcriptSasUrl, uploadedVideoClientExternalLink, uploadedTranscriptClientExternalLink, transcriptSummaryCleanedJson).Result;
                                    if (result && recordinginfo.Count() == 1)
                                    {
                                        _zoho.DeleteRecordingAsync(_recordingupload.erecordingId);
                                    }
                                    if (!string.IsNullOrEmpty(transcriptSummaryCleanedJson))
                                    {
                                        try
                                        {
                                            using (HttpClient client = new HttpClient())
                                            {
                                                client.Timeout = TimeSpan.FromMinutes(15);
                                                var request = new HttpRequestMessage(HttpMethod.Get, $"https://portal.risebird.io/Interview/AIClientReport?interviewId={meeting.imid}&irfId={meeting.irfIntId}&IsContradictionAI=true&isBackgroudJobRequest=true");
                                                var response = await client.SendAsync(request);
                                                response.EnsureSuccessStatusCode();
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            log.LogInformation($"Zoho : API Call For transcriptSummaryCleanedJson {ex.Message}");
                                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                        }
                                    }
                                    log.LogInformation($"Zoho : Video link and/or transcript url updated in database for meeting with Id {meeting.ZoomMeetingId} at: {DateTime.Now}");
                                }
                            }
                            catch (Exception ex)
                            {
                                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                log.LogError($"Zoho : Video upload error for meeting with id: {meeting.imid} and ZohoMeetingId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message : {ex.Message}");
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                            log.LogError($"RBMeet : Video Link error for meeting with id: {meeting.imid} and RoomId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message : {ex.Message}");
                            return;
                        }
                    });
                }
                // Teams : fetch video link and dump in db
                Parallel.ForEach(pendingTeamsMeetings, meeting =>
                {
                    try
                    {
                        string videoSasUrl = string.Empty;
                        int? videoDuration = null;
                        videoSasUrl = _iteamsMeetingHelper.GetTeamMeetingVideo(meeting.ZoomMeetingId).Result;
                        if (!string.IsNullOrEmpty(videoSasUrl))
                        {
                            //insert the SAS URL of the video, transcript and video duration in the database table
                            _ = _meetingRespository.UpdateUploadedVideoLinkAndTranscriptLink(meeting.imid, videoDuration, videoSasUrl).Result;
                            log.LogInformation($"Teams : Video link url updated in database for meeting with Id {meeting.ZoomMeetingId} at: {DateTime.Now}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        log.LogError($"Zoom : Video upload error for meeting with id: {meeting.imid} and ZoomMeetingId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message : {ex.Message}");
                        return;
                    }
                });


                #region Addressing the case when videolink is saved but transcript link is missing (in db)
                var solelyTranscriptsPendingMeetings = await _meetingRespository.GetPendingTranscriptMeetings();

                //var solelyTranscriptPendingRBMeetings = solelyTranscriptsPendingMeetings.Where(x => Regex.IsMatch(x.ZoomMeetingId, "[a-zA-Z]")).ToList();
                var solelyTranscriptPendingRBMeetings = solelyTranscriptsPendingMeetings.Where(x => x.InterviewMeetingPlatForm == MeetingPlatforms.Zoho).ToList();

                var solelyTranscriptsPendingTeamsMeetings = solelyTranscriptsPendingMeetings.Where(x => Regex.IsMatch(x.ZoomMeetingId, @"^(?=.*[0-9])(?=.*[a-zA-Z])([a-zA-Z0-9]+)$")).ToList();

                var solelyTranscriptsPendingZoomMeetings = solelyTranscriptsPendingMeetings.Except(solelyTranscriptPendingRBMeetings).ToList();
                solelyTranscriptsPendingZoomMeetings = solelyTranscriptsPendingMeetings.Except(solelyTranscriptsPendingTeamsMeetings).ToList();


                //get oauth token
                string tokenTranscript = await _zoomHelper.GenerateAccessToken();

                Parallel.ForEach(solelyTranscriptsPendingZoomMeetings, meeting =>
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

                            var audioTranscriptFiles = convertedResponse.recording_files.Where(x => x.file_type == "TRANSCRIPT" && x.status.ToLower() == "completed");
                            string transcriptSasUrl = string.Empty;
                            string uploadedTranscriptClientExternalLink = string.Empty;
                            if (string.IsNullOrEmpty(meeting.AudioTranscriptFileLink) && (audioTranscriptFiles != null || audioTranscriptFiles?.Count() == 1))
                            {
                                var transcriptFile = audioTranscriptFiles?.FirstOrDefault();
                                var downloadUrl = transcriptFile?.download_url;

                                //upload the video to azure blob using the download_Url
                                if (downloadUrl != null)
                                {
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

                                        }

                                        log.LogInformation($"Zoom : Video uploaded in blob for meeting with ZoomMeetingId {meeting.ZoomMeetingId} function executed at: {DateTime.Now}");

                                        //if (meeting.ClientCompanyName.ToLower() == ClientCompanyNames.Turing.ToLower())
                                        //{
                                        //    try
                                        //    {
                                        //        string fileNameExternal ="transcript.json"; // meeting.imid.ToString() + 
                                        //        myStream.Position = 0;
                                        //        // Replace Azure logic with Google Cloud Storage upload
                                        //        uploadedTranscriptClientExternalLink = _googleEventHelper.UploadToGoogleCloudStorage(fileNameExternal, myStream, meeting.imid).Result;
                                        //    }
                                        //    catch (Exception ex)
                                        //    {
                                        //        _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                        //        log.LogError($"Error while uploading video to Google Cloud Storage for meeting with id: {meeting.imid} and ZoomMeetingId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message: {ex.Message}");
                                        //        
                                        //    }

                                        //    log.LogInformation($"Client External Storage Video Upload : Video uploaded in client's storage for meeting with ZoomMeetingId {meeting.ZoomMeetingId} function executed at: {DateTime.Now}");

                                        //}
                                        //generate SAS URL from the uploaded video url
                                        var transcriptSasToken = _sasUrlHelper.CreateSasUrl(TranscriptsContainer);

                                        if (!string.IsNullOrEmpty(uploadedTranscriptUrl) && !string.IsNullOrEmpty(transcriptSasToken))
                                        {
                                            transcriptSasUrl = uploadedTranscriptUrl + "?" + transcriptSasToken;
                                        }

                                    }
                                    if (meeting.ClientCompanyName.ToLower() == ClientCompanyNames.Turing.ToLower())
                                    {
                                        using (Stream myStream = myWebClient.GetStreamAsync(downloadUrl).Result)
                                        {
                                            try
                                            {
                                                string fileNameExternal = "transcript.json"; // meeting.imid.ToString() + 
                                                uploadedTranscriptClientExternalLink = _googleEventHelper.UploadToGoogleCloudStorage(fileNameExternal, myStream, meeting.imid).Result;
                                            }
                                            catch (Exception ex)
                                            {
                                                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                                log.LogError($"Error while uploading video to Google Cloud Storage for meeting with id: {meeting.imid} and ZoomMeetingId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message: {ex.Message}");

                                            }
                                        }
                                        log.LogInformation($"Client External Storage Video Upload : Video uploaded in client's storage for meeting with ZoomMeetingId {meeting.ZoomMeetingId} function executed at: {DateTime.Now}");
                                    }
                                }
                            }
                            if (!string.IsNullOrEmpty(transcriptSasUrl))
                            {
                                //insert the SAS URL of the video, transcript and video duration in the database table
                                _ = _meetingRespository.UpdateTranscriptLink(meeting.imid, transcriptSasUrl, uploadedTranscriptClientExternalLink).Result;
                                log.LogInformation($"Zoom : Video link and/or transcript url updated in database for meeting with Id {meeting.ZoomMeetingId} at: {DateTime.Now}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        log.LogError($"Zoom : Video upload error for meeting with id: {meeting.imid} and ZoomMeetingId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message : {ex.Message}");

                    }
                });

                Parallel.ForEach(solelyTranscriptPendingRBMeetings, meeting =>
                {
                    try
                    {
                        try
                        {
                            //ZohoAzureuploadVideoandTranscript(meeting);
                            string videoSasUrl = string.Empty;
                            string uploadedVideoClientExternalLink = string.Empty;
                            string uploadedTranscriptClientExternalLink = string.Empty;
                            int? videoDuration = null;
                            string transcriptSasUrl = string.Empty;

                            try
                            {
                                var recordinginfo = _zoho.GetRecordingAsync(meeting.ZoomMeetingId, accesstoken).Result;
                                int maxduration = 0;
                                if (recordinginfo.Count() > 0)
                                    maxduration = recordinginfo.Max(r => r.duration);
                                var _recordingupload = recordinginfo?.Where(x => x.duration == maxduration).FirstOrDefault();
                                if (string.IsNullOrEmpty(meeting.VideoLink) || (!string.IsNullOrEmpty(meeting.VideoLink) && meeting.VideoDuration < 15))
                                {
                                    var downloadUrl = _recordingupload?.downloadUrl;

                                    if (downloadUrl != null)
                                    {
                                        HttpClient myWebClient = new HttpClient();
                                        string uploadedVideoUrl = string.Empty;
                                        var accessToken = accesstoken;
                                        myWebClient.DefaultRequestHeaders.Authorization =
                                            new System.Net.Http.Headers.AuthenticationHeaderValue("Zoho-oauthtoken", accessToken?.ToString());
                                        //url of the video uploaded to the client's storage exclusively in cases where the client has requested for such a service. 
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
                                                log.LogError($"Zoho : Error while inserting blob for meeting with id: {meeting.imid} and ZohoMeetingId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message: {ex.Message}");
                                                return;
                                            }
                                            log.LogInformation($"Zoho : Video uploaded in blob for meeting with ZohoMeetingId {meeting.ZoomMeetingId} function executed at: {DateTime.Now}");
                                            //generate SAS URL from the uploaded video url
                                            var sas = _sasUrlHelper.CreateSasUrl(ContainerName);
                                            if (!string.IsNullOrEmpty(uploadedVideoUrl) && !string.IsNullOrEmpty(sas))
                                            {
                                                videoSasUrl = uploadedVideoUrl + "?" + sas;
                                            }
                                        }
                                        if (!string.IsNullOrEmpty(videoSasUrl))
                                        {
                                            videoDuration = _recordingupload.duration / 60000;
                                        }
                                        if (meeting.ClientCompanyName.ToLower() == ClientCompanyNames.Turing.ToLower())
                                        {
                                            using (Stream myStream = myWebClient.GetStreamAsync(downloadUrl).Result)
                                            {
                                                try
                                                {
                                                    string fileNameExternal = "recording.mp4";
                                                    uploadedVideoClientExternalLink = _googleEventHelper.UploadToGoogleCloudStorage(fileNameExternal, myStream, meeting.imid).Result;
                                                }
                                                catch (Exception ex)
                                                {
                                                    _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                                    log.LogError($"Error while uploading video to Google Cloud Storage for meeting with id: {meeting.imid} and ZohoMeetingId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message: {ex.Message}");
                                                }
                                            }
                                            log.LogInformation($"Client External Storage Video Upload : Video uploaded in client's storage for meeting with ZohoMeetingId {meeting.ZoomMeetingId} function executed at: {DateTime.Now}");
                                        }
                                    }
                                }
                                if (string.IsNullOrEmpty(meeting.AudioTranscriptFileLink))
                                {
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
                                                uploadedTranscriptUrl = _azureStorageHelper.InsertBlobCloudCompare(fileName, TranscriptsContainer, null, null, myStream).Result;
                                            }
                                            catch (Exception ex)
                                            {
                                                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                                log.LogError($"Zoho : Error while inserting blob for meeting with id: {meeting.imid} and ZohoMeetingId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message: {ex.Message}");
                                            }
                                            log.LogInformation($"Zoho : Video uploaded in blob for meeting with ZohoMeetingId {meeting.ZoomMeetingId} function executed at: {DateTime.Now}");
                                            //generate SAS URL from the uploaded video url
                                            var transcriptSasToken = _sasUrlHelper.CreateSasUrl(TranscriptsContainer);
                                            if (!string.IsNullOrEmpty(uploadedTranscriptUrl) && !string.IsNullOrEmpty(transcriptSasToken))
                                            {
                                                transcriptSasUrl = uploadedTranscriptUrl + "?" + transcriptSasToken;
                                            }
                                        }
                                        if (meeting.ClientCompanyName.ToLower() == ClientCompanyNames.Turing.ToLower())
                                        {
                                            using (Stream myStream = myWebClient.GetStreamAsync(transcriptionDownloadUrl).Result)
                                            {
                                                try
                                                {
                                                    string fileNameExternal = "transcript.json"; // meeting.imid.ToString() + 
                                                    uploadedTranscriptClientExternalLink = _googleEventHelper.UploadToGoogleCloudStorage(fileNameExternal, myStream, meeting.imid).Result;
                                                }
                                                catch (Exception ex)
                                                {
                                                    _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                                    log.LogError($"Error while uploading video to Google Cloud Storage for meeting with id: {meeting.imid} and ZohoMeetingId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message: {ex.Message}");
                                                }
                                            }
                                            log.LogInformation($"Client External Storage Video Upload : Video uploaded in client's storage for meeting with ZohoMeetingId {meeting.ZoomMeetingId} function executed at: {DateTime.Now}");
                                        }
                                    }
                                }
                                //upload the video to azure blob using the download_Url
                                if (!string.IsNullOrEmpty(videoSasUrl) || !string.IsNullOrEmpty(transcriptSasUrl))
                                {
                                    var result = _meetingRespository.UpdateUploadedVideoLinkAndTranscriptLink(meeting.imid, videoDuration, videoSasUrl, transcriptSasUrl, uploadedVideoClientExternalLink, uploadedTranscriptClientExternalLink).Result;
                                    if (result && recordinginfo.Count() == 1)
                                    {
                                        _zoho.DeleteRecordingAsync(_recordingupload.erecordingId);
                                    }
                                    log.LogInformation($"Zoho : Video link and/or transcript url updated in database for meeting with Id {meeting.ZoomMeetingId} at: {DateTime.Now}");
                                }
                            }
                            catch (Exception ex)
                            {
                                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                log.LogError($"Zoho : Video upload error for meeting with id: {meeting.imid} and ZohoMeetingId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message : {ex.Message}");
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                            log.LogError($"RBMeet : Video Link error for meeting with id: {meeting.imid} and RoomId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message : {ex.Message}");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        log.LogError($"Zoho : Video upload error for meeting with id: {meeting.imid} and ZohoMeetingId: {meeting.ZoomMeetingId} at: {DateTime.Now}, Message : {ex.Message}");
                        return;
                    }
                });
                #endregion
                log.LogInformation($"C# Zoho Service function(Video Upload) executed at: {DateTime.Now}");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }


        }

        private async Task<string> DownloadTranscriptStringAsync(string url, string accessToken)
        {

            using var myWebClient = new HttpClient();

            myWebClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Zoho-oauthtoken", accessToken);

            using var myStream = await myWebClient.GetStreamAsync(url);
            using var reader = new StreamReader(myStream);

            string transcript = await reader.ReadToEndAsync();
            return transcript;

        }

        private TranscriptSummaryVM TryParseTranscriptSummaryVM(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var cleaned = raw.Trim();

            // Strip ```json ... ``` wrappers if present
            if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                cleaned = cleaned.Substring(7).TrimStart();

            if (cleaned.EndsWith("```"))
                cleaned = cleaned.Substring(0, cleaned.Length - 3).TrimEnd();

            cleaned = cleaned.TrimStart('\n');

            //
            // 1. STRICT DESERIALIZATION
            //
            try
            {
                var strict = JsonConvert.DeserializeObject<TranscriptSummaryVM>(cleaned);
                if (strict != null &&
                    (!string.IsNullOrEmpty(strict.JobTitle) ||
                     strict.IntervieweeAnalysis != null ||
                     strict.Flags != null))
                {
                    return strict;
                }
            }
            catch
            {
                // ignore and fall back
            }

            //
            // 2. FALLBACK PARSING (LOOSE MODE)
            //
            try
            {
                var root = JsonConvert.DeserializeObject<JObject>(cleaned);
                if (root == null) return null;

                var result = new TranscriptSummaryVM
                {
                    JobTitle = root.Value<string>("job_title") ?? root.Value<string>("jobTitle")
                };

                //
                // Interviewee Analysis
                //
                var iaNode = (JObject)(root["interviewee_analysis"] ?? root["intervieweeAnalysis"]);
                if (iaNode != null)
                {
                    var analysis = new IntervieweeAnalysis();

                    // QA Pairs
                    var qaArr = (JArray)(iaNode["qa_pairs"] ?? iaNode["qaPairs"] ?? iaNode["qa"]);
                    if (qaArr != null)
                    {
                        analysis.QaPairs = qaArr.Select(q => new QaPair
                        {
                            Timestamp = q.Value<string>("timestamp") ?? q.Value<string>("time"),
                            Question = q.Value<string>("question") ?? q.Value<string>("q"),
                            Answer = q.Value<string>("answer") ?? q.Value<string>("a"),
                            AnswerRating = q.Value<int?>("answer_rating")
                                           ?? q.Value<int?>("rating")
                                           ?? 0,
                            Comment = q.Value<string>("comment") ?? q.Value<string>("reason")
                        }).ToList();
                    }

                    // Summary
                    var summaryNode = (JObject)(iaNode["summary"] ?? iaNode["analysis_summary"] ?? iaNode["interview_summary"]);
                    if (summaryNode != null)
                    {
                        analysis.Summary = new InterviewSummary
                        {
                            TotalQuestions =
                                summaryNode.Value<int?>("total_questions")
                                ?? summaryNode.Value<int?>("totalQuestions")
                                ?? 0,

                            AverageAnswerRating =
                                summaryNode.Value<double?>("average_answer_rating")
                                ?? summaryNode.Value<double?>("avg_answer_rating")
                                ?? 0,

                            Strengths =
                                summaryNode["strengths"] is JArray strengthsArr
                                    ? strengthsArr.Select(x => x.ToString()).ToList()
                                    : new List<string>(),

                            AreasOfImprovement =
                                (summaryNode["areas_of_improvement"]
                                 ?? summaryNode["weaknesses"]
                                 ?? summaryNode["areasForImprovement"]) is JArray aoiArr
                                    ? aoiArr.Select(x => x.ToString()).ToList()
                                    : new List<string>()
                        };
                    }

                    // Verdict
                    var verdictNode = (JObject)(iaNode["verdict"] ?? iaNode["decision"]);
                    if (verdictNode != null)
                    {
                        analysis.Verdict = new Verdict
                        {
                            FinalRecommendation =
                                verdictNode.Value<string>("final_recommendation")
                                ?? verdictNode.Value<string>("recommendation")
                                ?? verdictNode.Value<string>("finalRecommendation"),

                            Justification =
                                verdictNode.Value<string>("justification")
                                ?? verdictNode.Value<string>("reason")
                                ?? verdictNode.Value<string>("explanation")
                        };
                    }

                    result.IntervieweeAnalysis = analysis;
                }

                //
                // Flags
                //
                var flagsNode = (JObject)(root["flags"] ?? root["Flags"]);
                if (flagsNode != null)
                {
                    var flags = new Flags();

                    var personalInfoArr = (JArray)(flagsNode["personal_info_shared"] ?? flagsNode["personalInfoShared"]);
                    if (personalInfoArr != null)
                    {
                        flags.PersonalInfoShared = personalInfoArr.Select(f => new FlagInstance
                        {
                            Timestamp = f.Value<string>("timestamp") ?? f.Value<string>("time"),
                            Content = f.Value<string>("content") ?? f.Value<string>("text") ?? f.Value<string>("statement")
                        }).ToList();
                    }

                    var unethicalArr = (JArray)(flagsNode["unethical_behavior"] ?? flagsNode["unethicalBehavior"]);
                    if (unethicalArr != null)
                    {
                        flags.UnethicalBehavior = unethicalArr.Select(f => new FlagInstance
                        {
                            Timestamp = f.Value<string>("timestamp") ?? f.Value<string>("time"),
                            Content = f.Value<string>("content") ?? f.Value<string>("text") ?? f.Value<string>("statement")
                        }).ToList();
                    }

                    result.Flags = flags;
                }

                return result;
            }
            catch
            {
                return null;
            }
        }

    }
}
