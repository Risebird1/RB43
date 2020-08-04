using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
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
        public async Task<JsonResult> SendMeetingInvite(string Subject, string emailid, DateTime startdate, DateTime enddate, string Message, string TimeZoneDetails, string AdditionalUser)
        {
            try
            {
                var Emailaddress = string.Empty;
                if (HttpContext.Session.GetString("email") != null)
                {
                    Emailaddress = HttpContext.Session.GetString("email")?.Trim();
                }
                emailid = "prashantindoliya@gmail.com";
                Emailaddress = "prashantindoliya@gmail.com";
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
                _meeting.attendees.Add(new Attendee { emailAddress = new EmailAddress { address = emailid, name = "" }, type = "required" });
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

                var functionKey = "Q/8CUsLsbKYeqRMFNPT5QnKsNWzoudpVayC1bN7tQbYK0SEAw1wboQ==";
                var path = $"MSTeamMeeting";
                var response = await _httpClient.SendRequestAsync(HttpMethod.Post, path, _meeting, functionKey, accountEmail, _cancellationToken);
                var result = response.Content.ReadAsStringAsync().Result;

                if (response.IsSuccessStatusCode)
                {
                    var urllinkdetails = result.ToString().Split("onlineMeeting")[result.ToString().Split("onlineMeeting").Length - 1];
                    var deatils = urllinkdetails.Split("https")[1].Replace(":", "https:").Replace("}}", "").Replace(@"\", "");
                    var meetingURL = deatils.Remove(deatils.Length - 2, 2);
                    //var meetingURL = JsonConvert.DeserializeObject<object>(result);
                    var resultsetvalue = _meetingInviteService.InsertMeetingInvite(startdate, enddate, Emailaddress, accountEmail, meetingURL.ToString(), emailid, Subject);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Invite Successfully Send");
            }


            ////Save upcoming online meeting
            //
            ////Send mail with meeting invite to candidate and who create meeting
            //Mail _mail = new Mail
            //{
            //    TO = emailid,
            //    CC = Emailaddress,
            //    Subject = Subject
            //};
            //var resultset = _notificationHelper.OnlineMeetingInvite(_mail, response.ToString(), Message, startdateU, enddateU);
            //if (resultset == "Mail sent Successfully.")
            //{
            //    return Json("Invite Successfully Send");
            //}
            //else
            //{
            //    return Json("Fail To send invite");
            //}
            return Json("Invite Successfully Send");
        }
    }
}
