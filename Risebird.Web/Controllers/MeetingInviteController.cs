using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Net.Http;
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

        public MeetingInviteController(HttpClientHelper factory, IConfiguration configuration, INotificationHelper notificationHelper, IMeetingInviteService meetingInviteService, IApplicationErrorLogger applicationErrorLogger)
        {
            _httpClient = factory.GetHttpClient();
            _configuration = configuration;
            _notificationHelper = notificationHelper;
            _meetingInviteService = meetingInviteService;
            _applicationErrorLogger = applicationErrorLogger;
        }
        
        [HttpPost]
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
                if (!String.IsNullOrEmpty(AdditionalUser))
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
                        var resultset = _notificationHelper.MeetingInvite(startdate, enddate, TimeZoneDetails, accountEmail, resultValue, Message, Subject, loggedinUser, candidateEmailId, AdditionalUser, meetingURL);
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
    }
}
