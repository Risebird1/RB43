using Newtonsoft.Json;
using RestSharp;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public class TeamsMeetingHelper : ITeamsMeetingHelper
    {
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly ISASUrlHelper _sasUrlHelper;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public TeamsMeetingHelper(IAzureStorageHelper azureStorageHelper, ISASUrlHelper sasUrlHelper, IApplicationErrorLogger applicationErrorLogger)
        {
            _azureStorageHelper = azureStorageHelper;
            _sasUrlHelper = sasUrlHelper;
            _applicationErrorLogger = applicationErrorLogger;
        }
        private async Task<string> GetAccessTokenAsync()
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_id", "370e60c4-cdb4-4dad-8c27-3a0af77ac73f"),
                new KeyValuePair<string, string>("client_secret", "GcH8Q~y1fOYOsRUmspKtPQZT6I0s13g3_oDjAcvA"),
                new KeyValuePair<string, string>("scope",  "https://graph.microsoft.com/.default"),
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            };
            var client = new HttpClient();
            var tenantId = "fb212e2c-ff2c-4f52-9a84-774d6f275cb4";
            var requestUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
            var requestContent = new FormUrlEncodedContent(values);
            var response = await client.PostAsync(requestUrl, requestContent);
            var responseBody = await response.Content.ReadAsStringAsync();
            dynamic tokenResponse = JsonConvert.DeserializeObject(responseBody);
            return tokenResponse?.access_token;
        }
        public async Task<MetingResponse> CreateTeamsMeeting(string subject, DateTime MeetingDate, int interviewduration, string TimeZoneDetails)
        {
            try
            {
                var meetingResponse = new MetingResponse();
                //string TimeZoneDetails = "India Standard Time";
                //var selectTimeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneDetails);
                OnlineMeeting _meeting = new OnlineMeeting();
                _meeting.subject = subject;
                _meeting.body = new Body();
                _meeting.body.content = "";
                _meeting.body.contentType = "HTML";
                _meeting.start = new Start();
                _meeting.start.dateTime = MeetingDate;
                _meeting.start.timeZone = TimeZoneDetails;
                _meeting.end = new End();
                _meeting.end.dateTime = MeetingDate.AddMinutes(interviewduration);
                _meeting.end.timeZone = TimeZoneDetails;
                _meeting.location = new Location();
                _meeting.location.displayName = "Risebird Meeting";
                _meeting.attendees = new List<Attendee>();
                _meeting.isOnlineMeeting = true;
                _meeting.onlineMeetingProvider = "teamsForBusiness";
                //Fetch account details
                var accountEmail = "interviews@Risebird492.onmicrosoft.com";
                string userid = "c09f7f50-fd00-4e7e-80da-2a464ca71cbb";
                //Start: Create Online Calender Event 
                var client = new RestClient();
                var token = await GetAccessTokenAsync();
                var result = new OnlineMeetingResponse();
                if (!string.IsNullOrEmpty(accountEmail))
                {
                    var requestUrl = $"https://graph.microsoft.com/v1.0/users/{accountEmail}/calendar/events";
                    var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                    var request = new RestRequest(requestUrl, Method.Post);
                    request.AddHeader("Authorization", $"Bearer {token}");
                    request.AddHeader("Content-Type", "application/json");
                    request.AddJsonBody(_meeting);
                    RestResponse response = await client.ExecuteAsync(request);
                    var content = response.Content;
                    result = JsonConvert.DeserializeObject<OnlineMeetingResponse>(content);
                }
                //End: Create Online Calender Event
                //Start: Get Online Meeting Details
                var meetingdetails = new OnlineMeetingResult();
                var joinUrl = result?.onlineMeeting?.joinUrl;
                if (!string.IsNullOrEmpty(joinUrl))
                {
                    var requestUrl = $"https://graph.microsoft.com/v1.0/users/{userid}/onlineMeetings?$filter=JoinWebUrl%20eq%20'" + joinUrl + "'";
                    var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                    var request = new RestRequest(requestUrl, Method.Get);
                    request.AddHeader("Authorization", $"Bearer {token}");
                    request.AddHeader("Content-Type", "application/json");
                    RestResponse response = await client.ExecuteAsync(request);
                    var content = response.Content;
                    meetingdetails = JsonConvert.DeserializeObject<OnlineMeetingResult>(content);
                }
                //End: Get Online Meeting Details
                //Start: Update Online Meeting to start video recording automatically
                var meetingid = meetingdetails?.value?.FirstOrDefault().id;
                if (!string.IsNullOrEmpty(meetingid))
                {
                    var requestUrl = $"https://graph.microsoft.com/v1.0/users/{userid}/onlineMeetings/" + meetingid;
                    var httpRequest = new HttpRequestMessage(HttpMethod.Patch, requestUrl);
                    var request = new RestRequest(requestUrl, Method.Patch);
                    request.AddHeader("Authorization", $"Bearer {token}");
                    request.AddHeader("Content-Type", "application/json");
                    OnlineMeetingUpdate model = new OnlineMeetingUpdate();
                    model.recordAutomatically = true;
                    request.AddJsonBody(model);
                    RestResponse response = await client.ExecuteAsync(request);
                    var content = response.Content;
                    var convertedResponse = JsonConvert.DeserializeObject<OnlineMeetingResult>(content);
                }
                //End: Update Online Meeting to start video recording automatically
                meetingResponse.joinUrl = joinUrl;
                meetingResponse.meetingId = meetingid;
                meetingResponse.accountEmail = accountEmail;
                meetingResponse.id = result?.onlineMeeting?.id;
                return meetingResponse;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<string> GetTeamMeetingVideo(string meetingid)
        {
            try
            {
                string userid = "c09f7f50-fd00-4e7e-80da-2a464ca71cbb";
                string videoSasUrl = string.Empty;
                var token = await GetAccessTokenAsync();
                var client = new RestClient();
                //Start : Fetch Online meeting Video URL
                var requestUrl = $"https://graph.microsoft.com/v1.0/users/{userid}/onlineMeetings/{meetingid}/recordings";
                var request = new RestRequest(requestUrl, Method.Get);
                request.AddHeader("Authorization", $"Bearer {token}");
                request.AddHeader("Content-Type", "application/json");
                RestResponse response = await client.ExecuteAsync(request);
                var content = response.Content;
                var convertedResponse = JsonConvert.DeserializeObject<OnlineMeetingRecording>(content);
                var recordingUrl = convertedResponse?.value?.FirstOrDefault().recordingContentUrl;
                //End : Fetch Online meeting Video URL
                //Start : Upload Video over azure blob and get blob URL
                if (!string.IsNullOrWhiteSpace(recordingUrl))
                {
                    var requestvideo = new RestRequest(recordingUrl, Method.Get);
                    requestvideo.AddHeader("Authorization", $"Bearer {token}");
                    requestvideo.AddHeader("Content-Type", "application/json");
                    RestResponse responsevideo = await client.ExecuteAsync(requestvideo);
                    var contentvideo = responsevideo.RawBytes;
                    using (Stream s = new MemoryStream(contentvideo))
                    {
                        Guid _guid = Guid.NewGuid();
                        var filename = _guid.ToString() + ".mp4";
                        var ContainerName = "azurevideos";
                        string uploadedVideoUrl = string.Empty;
                        uploadedVideoUrl = await _azureStorageHelper.InsertBlobCloudCompare(filename, ContainerName, null, s);
                        var sas = _sasUrlHelper.CreateSasUrl(ContainerName);
                        if (!string.IsNullOrEmpty(uploadedVideoUrl) && !string.IsNullOrEmpty(sas))
                        {
                            videoSasUrl = uploadedVideoUrl + "?" + sas;
                        }
                    }
                    return videoSasUrl;
                }
                //End : Upload Video over azure blob and get blob URL
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return "";
        }
        public async Task<string> DeleteTeamMeetingandEvent(string meetingid, string eventid)
        {
            try
            {
                string userid = "c09f7f50-fd00-4e7e-80da-2a464ca71cbb";
                var token = await GetAccessTokenAsync();
                var client = new RestClient();
                //Start : Delete Online meeting 
                if (!string.IsNullOrWhiteSpace(meetingid))
                {
                    var requestUrl = $"https://graph.microsoft.com/v1.0/users/{userid}/onlineMeetings/{meetingid}";
                    var request = new RestRequest(requestUrl, Method.Delete);
                    request.AddHeader("Authorization", $"Bearer {token}");
                    request.AddHeader("Content-Type", "application/json");
                    RestResponse response = await client.ExecuteAsync(request);
                    var content = response.Content;
                }
                //End : Delete Online meeting 
                //Start : Delete Calender Event
                if (!string.IsNullOrWhiteSpace(eventid))
                {
                    var requestUrl = $"https://graph.microsoft.com/v1.0/users/{userid}/events/{eventid}";
                    var request = new RestRequest(requestUrl, Method.Delete);
                    request.AddHeader("Authorization", $"Bearer {token}");
                    request.AddHeader("Content-Type", "application/json");
                    RestResponse response = await client.ExecuteAsync(request);
                    var content = response.Content;
                }
                //End : Delete Calender Event
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return "";
        }
    }
}
