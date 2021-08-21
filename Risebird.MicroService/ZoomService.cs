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

        private readonly string ContainerName;
        private readonly string SASConnectionString;

        public ZoomService(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, IMeetingRespository meetingRespository)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _meetingRespository = meetingRespository;
            ContainerName = _configuration["SASURLSettings:ContainerName"];
            SASConnectionString = _configuration["SASURLSettings:ConnectionString"];


        }
        [FunctionName("ZoomService")]
        public async Task Run([TimerTrigger("0 0 */6 * * *")] TimerInfo myTimer, ILogger log) //after every 3 hours
        {
            try
            {
                //get all the completed meetings where video upload is pending

                var pendingVideoMeetings = await _meetingRespository.GetPendingVideoMeetings();


                Parallel.ForEach(pendingVideoMeetings, meeting =>
                {

                    try
                    {
                        if (!string.IsNullOrEmpty(meeting.ZoomMeetingId))
                        {
                            if (meeting.ZoomMeetingId == "0" && !string.IsNullOrEmpty(meeting.zoomLink))
                            {
                                long zoommeetingId = 0;
                                try
                                {
                                    zoommeetingId = Convert.ToInt64(meeting.zoomLink.Split('/').Last());
                                }
                                catch (Exception)
                                {
                                    try
                                    {
                                        var link = meeting.zoomLink.Split('?').First();
                                        zoommeetingId = Convert.ToInt64(link.Split('/').Last());
                                    }
                                    catch (Exception)
                                    {
                                        zoommeetingId = 0;
                                    }
                                }
                                meeting.ZoomMeetingId = zoommeetingId.ToString();
                            }

                            //get jwt authentication token 
                            string token = ZoomToken();

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
                                        var sas = CreateSasUrl();

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

                log.LogInformation($"C# Zoom Service function(Video Upload) executed at: {DateTime.Now}");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        private string ZoomToken()
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

            // Token to String so you can use it in your client
            var tokenString = handler.WriteToken(secToken);

            return tokenString;
        }

        private string CreateSasUrl()
        {
            BlobContainerClient blobcontainerclient = new BlobContainerClient(SASConnectionString, ContainerName);
            var uri = GetServiceSasUriForContainer(blobcontainerclient);
            return uri.AbsoluteUri.ToString().Split("?")[1].ToString();
        }
        private Uri GetServiceSasUriForContainer(BlobContainerClient containerClient, string storedPolicyName = null)
        {
            if (containerClient.CanGenerateSasUri)
            {
                BlobSasBuilder sasBuilder = new BlobSasBuilder()
                {
                    BlobContainerName = containerClient.Name,
                    Resource = "c",
                };
                if (storedPolicyName == null)
                {

                    sasBuilder.SetPermissions(BlobContainerSasPermissions.Read);
                    sasBuilder.StartsOn = DateTimeOffset.UtcNow;
                    sasBuilder.ExpiresOn = DateTimeOffset.UtcNow.AddMonths(7);
                    sasBuilder.Protocol = SasProtocol.Https;

                }
                else
                {
                    sasBuilder.Identifier = storedPolicyName;
                }
                Uri sasUri = containerClient.GenerateSasUri(sasBuilder);
                return sasUri;
            }
            else
            {
                return null;
            }
        }

    }
}
