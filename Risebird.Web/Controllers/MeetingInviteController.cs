using ExcelDataReader.Log;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Org.BouncyCastle.Ocsp;
using RestSharp;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.WebPages;

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
        public MeetingInviteController(HttpClientHelper factory, IConfiguration configuration, INotificationHelper notificationHelper, IMeetingInviteService meetingInviteService, IApplicationErrorLogger applicationErrorLogger)
        {
            _httpClient = factory.GetHttpClient();
            _configuration = configuration;
            _notificationHelper = notificationHelper;
            _meetingInviteService = meetingInviteService;
            _applicationErrorLogger = applicationErrorLogger;
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
                _meeting.body.content = "Nothing in messsage body";
                _meeting.body.contentType = "HTML";
                _meeting.start = new Start();
                _meeting.start.dateTime = DateTime.Now.AddHours(5);
                _meeting.start.timeZone = TimeZoneDetails;
                _meeting.end = new End();
                _meeting.end.dateTime = DateTime.Now.AddHours(6);
                _meeting.end.timeZone = TimeZoneDetails;
                _meeting.location = new Location();
                _meeting.location.displayName = "Risebird Meeting";
                _meeting.attendees = new List<Attendee>();
                _meeting.attendees.Add(new Attendee { emailAddress = new EmailAddress { address = "prashantindoliya@gmail.com", name = "" }, type = "required" });
                _meeting.attendees.Add(new Attendee { emailAddress = new EmailAddress { address = loggedinUser, name = "" }, type = "required" });
                _meeting.isOnlineMeeting = true;
                _meeting.onlineMeetingProvider = "teamsForBusiness";
                var accountEmail = "Tanishka@Guptatanishkarb.onmicrosoft.com";
                var result = await CreateTeamsMeetingClient(accountEmail, _meeting);
                var urllinkdetails = result.ToString().Split("onlineMeeting")[result.ToString().Split("onlineMeeting").Length - 1];
                var deatils = urllinkdetails.Split("https")[1].Replace(":", "https:").Replace("}}", "").Replace(@"\", "");
                var meetingURL = deatils.Remove(deatils.Length - 2, 2);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            return "";
        }
        public async Task<string> CreateTeamsMeetingClient(string accountEmail, OnlineMeeting data)
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
                return response.Content.ToString();
            }
            return "";
        }
        public async Task<string> CreateTeamsMeeting(string accountEmail, OnlineMeeting data)
        {
            if (!string.IsNullOrEmpty(accountEmail))
            {
                var httpClient = await CreateAuthorizedHttpClient();
                var requestUrl = $"https://graph.microsoft.com/v1.0/users/{accountEmail}/calendar/events";
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                //var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                var content = new StringContent(data.ToString(), Encoding.UTF8, "application/json");
                httpRequest.Content = content;
                var response = await httpClient.SendAsync(httpRequest);
                var result = await response.Content.ReadAsStringAsync();
                return result;
            }
            return "";
        }
        public async Task<HttpClient> CreateAuthorizedHttpClient()
        {
            if (_httpClienttwo != null)
            {
                return _httpClienttwo;
            }
            var token = await GetAccessTokenAsync();
            _httpClienttwo = new HttpClient();
            _httpClienttwo.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return _httpClienttwo;
        }
        private async Task<string> GetAccessTokenAsync()
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_id", "4e52d62b-e1e3-4f46-87a5-6209fd114dae"),
                new KeyValuePair<string, string>("client_secret", "QOI8Q~rH5.iITuamTGSzgRvLOO0vGRb8HwslRawa"),
                new KeyValuePair<string, string>("scope",  "https://graph.microsoft.com/.default"),
                new KeyValuePair<string, string>("grant_type", "client_credentials")
                //new KeyValuePair<string, string>("resource", System.Environment.GetEnvironmentVariable($"Resource").ToString())
            };
            var client = new HttpClient();
            var tenantId = "6446622b-9dfb-4b45-8fc1-f45d4f71c53b";
            var requestUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
            var requestContent = new FormUrlEncodedContent(values);
            var response = await client.PostAsync(requestUrl, requestContent);
            var responseBody = await response.Content.ReadAsStringAsync();
            dynamic tokenResponse = JsonConvert.DeserializeObject(responseBody);
            return tokenResponse?.access_token;
        }
        public async Task<string> FetchMeetingRecording(string accountEmail, string meetingid)
        {
            var client = new RestClient();
            var token = await GetAccessTokenAsync();
            var requestUrl = $"https://graph.microsoft.com/v1.0/users/{accountEmail}/onlineMeetings/{meetingid}/recordings";
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            var request = new RestRequest(requestUrl, Method.Get);
            request.AddHeader("Authorization", $"Bearer {token}");
            request.AddHeader("Content-Type", "application/json");
            RestResponse response = await client.ExecuteAsync(request);
            return response.Content.ToString();
        }
        #endregion
    }
}
