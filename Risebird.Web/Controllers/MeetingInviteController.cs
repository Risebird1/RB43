using EllipticCurve.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.Web
{
    public class MeetingInviteController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly CancellationToken _cancellationToken;
        private IConfiguration _configuration;
        private readonly INotificationHelper _notificationHelper;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IMeetingInviteService _meetingInviteService;
        private HttpClient _httpClienttwo;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly ISASUrlHelper _sasUrlHelper;

        public MeetingInviteController(HttpClientHelper factory, IConfiguration configuration, INotificationHelper notificationHelper, IMeetingInviteService meetingInviteService, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, ISASUrlHelper sasUrlHelper)
        {
            _httpClient = factory.GetHttpClient();
            _configuration = configuration;
            _notificationHelper = notificationHelper;
            _meetingInviteService = meetingInviteService;
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _sasUrlHelper = sasUrlHelper;
        }

        [HttpGet]
        public JsonResult FetchTimeZones()
        {
            //Add TimeZone details
            var alltimezones = TimeZoneInfo.GetSystemTimeZones();
            var currenttimezone = TimeZone.CurrentTimeZone;
            return Json(new { result = alltimezones, current = currenttimezone });
        }

        [HttpPost]
        public async Task<JsonResult> SendMeetingInvite(string Subject, string candidateEmailId, DateTime startdate, DateTime enddate, string Message, string TimeZoneDetails, string AdditionalUser, string irfId = null)
        {
            try
            {
                var loggedinUser = string.Empty;
                if (HttpContext.Session.GetString("email") != null)
                {
                    loggedinUser = HttpContext.Session.GetString("email")?.Trim();
                    loggedinUser = loggedinUser.Replace("\"", "");
                }

                var selectTimeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneDetails);

                OnlineMeeting _meeting = new OnlineMeeting();
                _meeting.subject = Subject;
                _meeting.body = new Body();
                _meeting.body.content = Message;
                _meeting.body.contentType = "HTML";
                _meeting.start = new Start();
                _meeting.start.dateTime = startdate;
                _meeting.start.timeZone = TimeZoneDetails;
                _meeting.end = new End();
                _meeting.end.dateTime = enddate;
                _meeting.end.timeZone = TimeZoneDetails;
                _meeting.location = new Location();
                _meeting.location.displayName = "Risebird Meeting";
                _meeting.attendees = new List<Attendee>();
                _meeting.attendees.Add(new Attendee { emailAddress = new EmailAddress { address = candidateEmailId, name = "" }, type = "required" });
                _meeting.attendees.Add(new Attendee { emailAddress = new EmailAddress { address = loggedinUser, name = "" }, type = "required" });
                if (!string.IsNullOrEmpty(AdditionalUser))
                {
                    var emailaddresses = AdditionalUser.Split(";");
                    foreach (var item in emailaddresses)
                    {
                        _meeting.attendees.Add(new Attendee { emailAddress = new EmailAddress { address = item, name = "" }, type = "required" });
                    }
                }
                _meeting.isOnlineMeeting = true;
                _meeting.onlineMeetingProvider = "teamsForBusiness";
                var accountEmail = await _meetingInviteService.CheckMeetingAccount(startdate, enddate);

                var functionKey = "IWHZK4NxjiJp1PXjPAP3S4vQ1imt3QCbG88Nepk0ZNpWaXxIyMxO6w==";
                var path = $"MSTeamMeeting";
                var response = await _httpClient.SendRequestAsync(HttpMethod.Post, path, _meeting, functionKey, accountEmail, _cancellationToken);
                var result = response.Content.ReadAsStringAsync().Result;

                if (response.IsSuccessStatusCode)
                {
                    var urllinkdetails = result.ToString().Split("onlineMeeting")[result.ToString().Split("onlineMeeting").Length - 1];
                    var deatils = urllinkdetails.Split("https")[1].Replace(":", "https:").Replace("}}", "").Replace(@"\", "");
                    var meetingURL = deatils.Remove(deatils.Length - 2, 2);
                    var resultValue = await _meetingInviteService.InsertMeetingInvite(startdate, enddate, loggedinUser, accountEmail, meetingURL.ToString(), candidateEmailId + ";" + AdditionalUser, Subject, TimeZoneDetails);
                    if (!resultValue.Contains("failed"))
                    {
                        var resultset = await _notificationHelper.MeetingInvite(startdate, enddate, TimeZoneDetails, accountEmail, resultValue, Message, Subject, loggedinUser, candidateEmailId, AdditionalUser, meetingURL);
                        if (resultset == "Mail sent Successfully.")
                        {
                            return Json("Invite sent successfully");
                        }
                        else
                        {
                            return Json("Fail To send invite");
                        }
                    }
                }
                return Json("Fail To send invite");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Fail To send invite");
            }
        }

        #region Teams Meeting Apis
        public async Task<string> GenerateMeting()
        {
            try
            {
                var loggedinUser = string.Empty;
                if (HttpContext.Session.GetString("email") != null)
                {
                    loggedinUser = HttpContext.Session.GetString("email")?.Trim();
                    loggedinUser = loggedinUser.Replace("\"", "");
                }
                string TimeZoneDetails = "India Standard Time";
                var selectTimeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneDetails);

                OnlineMeeting _meeting = new OnlineMeeting();
                _meeting.subject = "Test Data cration";
                _meeting.body = new Body();
                _meeting.body.content = "Nothing in messsage body : Final";
                _meeting.body.contentType = "HTML";
                _meeting.start = new Start();
                _meeting.start.dateTime = DateTime.Now.AddMinutes(1);
                _meeting.start.timeZone = TimeZoneDetails;
                _meeting.end = new End();
                _meeting.end.dateTime = DateTime.Now.AddMinutes(10);
                _meeting.end.timeZone = TimeZoneDetails;
                _meeting.location = new Location();
                _meeting.location.displayName = "Risebird Meeting";
                _meeting.attendees = new List<Attendee>();
                //_meeting.attendees.Add(new Attendee { emailAddress = new EmailAddress { address = "prashantindoliya@gmail.com", name = "" }, type = "required" });
                //_meeting.attendees.Add(new Attendee { emailAddress = new EmailAddress { address = loggedinUser, name = "" }, type = "required" });
                _meeting.isOnlineMeeting = true;
                _meeting.onlineMeetingProvider = "teamsForBusiness";
                var accountEmail = "interviews@Risebird492.onmicrosoft.com";
                var result = await CreateTeamsMeetingClient(accountEmail, _meeting);
                var meetingdetails = await GetOnlineMeetingId(result.onlineMeeting.joinUrl);
                var meetingid = meetingdetails.value?.FirstOrDefault().id;
                await UpdateOnlineMeetingAutoRecord(meetingid);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            return "";
        }
        public async Task<OnlineMeetingResponse> CreateTeamsMeetingClient(string accountEmail, OnlineMeeting data)
        {
            if (!string.IsNullOrEmpty(accountEmail))
            {
                var client = new RestClient();
                var token = await GetAccessTokenAsync();
                var requestUrl = $"https://graph.microsoft.com/v1.0/users/{accountEmail}/calendar/events";
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                var request = new RestRequest(requestUrl, Method.Post);
                request.AddHeader("Authorization", $"Bearer {token}");
                request.AddHeader("Content-Type", "application/json");
                request.AddJsonBody(data);
                RestResponse response = await client.ExecuteAsync(request);
                var content = response.Content;
                var convertedResponse = JsonConvert.DeserializeObject<OnlineMeetingResponse>(content);
                return convertedResponse;
            }
            return null;
        }
        public async Task<OnlineMeetingResult> GetOnlineMeetingId(string joinUrl)
        {
            if (!string.IsNullOrEmpty(joinUrl))
            {
                string userid = "c09f7f50-fd00-4e7e-80da-2a464ca71cbb";
                var client = new RestClient();
                var token = await GetAccessTokenAsync();
                var requestUrl = $"https://graph.microsoft.com/v1.0/users/{userid}/onlineMeetings?$filter=JoinWebUrl%20eq%20'" + joinUrl + "'";
                var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                var request = new RestRequest(requestUrl, Method.Get);
                request.AddHeader("Authorization", $"Bearer {token}");
                request.AddHeader("Content-Type", "application/json");
                RestResponse response = await client.ExecuteAsync(request);
                var content = response.Content;
                var convertedResponse = JsonConvert.DeserializeObject<OnlineMeetingResult>(content);
                return convertedResponse;
            }
            return null;
        }
        public async Task<string> UpdateOnlineMeetingAutoRecord(string meetingid)
        {
            if (!string.IsNullOrEmpty(meetingid))
            {
                string userid = "c09f7f50-fd00-4e7e-80da-2a464ca71cbb";
                var client = new RestClient();
                var token = await GetAccessTokenAsync();
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
            return "";
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
        public async Task VideoURlFetch(string meetingid)
        {
            //var meetingid = "MSpjMDlmN2Y1MC1mZDAwLTRlN2UtODBkYS0yYTQ2NGNhNzFjYmIqMCoqMTk6bWVldGluZ19PVEE1WkRBeU5ERXRNVEpqT1MwME1XWTBMV0V4TXpRdE9XSTFZMlE0TW1FMk5UZGhAdGhyZWFkLnYy";
            var userid = "c09f7f50-fd00-4e7e-80da-2a464ca71cbb";
            var token = await GetAccessTokenAsync();
            var videourl = await UploadRecodingToAzure(meetingid, userid, token);
        }
        public async Task<string> UploadRecodingToAzure(string meetingid, string userid, string token)
        {
            string videoSasUrl = string.Empty;
            var recordingUrl = await FetchMeetingRecording(userid, meetingid, token);
            var client = new RestClient();
            var request = new RestRequest(recordingUrl, Method.Get);
            request.AddHeader("Authorization", $"Bearer {token}");
            request.AddHeader("Content-Type", "application/json");
            RestResponse response = await client.ExecuteAsync(request);
            var content = response.RawBytes;
            using (Stream s = new MemoryStream(content))
            {
                Guid _guid = Guid.NewGuid();
                var filename = _guid.ToString() + ".mp4";
                filename = filename.Replace("-", "");
                var ContainerName = "azurevideos";
                string uploadedVideoUrl = string.Empty;
                uploadedVideoUrl = await _azureStorageHelper.InsertBlobCloudCompare(filename, ContainerName, null, s);
                var sas = _sasUrlHelper.CreateSasUrl(ContainerName);
                if (!string.IsNullOrEmpty(uploadedVideoUrl) && !string.IsNullOrEmpty(sas))
                {
                    videoSasUrl = uploadedVideoUrl + "?" + sas;
                }
                var rawEmail = _azureStorageHelper.GetBlobData(ContainerName, filename);
            }
            return videoSasUrl;
        }
        public async Task<string> FetchMeetingRecording(string userid, string meetingid, string token)
        {
            var client = new RestClient();
            var requestUrl = $"https://graph.microsoft.com/v1.0/users/{userid}/onlineMeetings/{meetingid}/recordings";
            var request = new RestRequest(requestUrl, Method.Get);
            request.AddHeader("Authorization", $"Bearer {token}");
            request.AddHeader("Content-Type", "application/json");
            RestResponse response = await client.ExecuteAsync(request);
            var content = response.Content;
            var convertedResponse = JsonConvert.DeserializeObject<OnlineMeetingRecording>(content);
            return convertedResponse.value?.FirstOrDefault().recordingContentUrl;
        }
        #endregion
    }

}
