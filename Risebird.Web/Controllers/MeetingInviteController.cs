using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;

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

        public JsonResult SendMeetingInvite(string Subject, string emailid, DateTime startdate, DateTime enddate, string Message)
        {
            var Emailaddress = string.Empty;
            if (HttpContext.Session.GetString("email") != null)
            {
                Emailaddress = HttpContext.Session.GetString("email")?.Trim();
            }


            DateTime startdateU = TimeZoneInfo.ConvertTime(startdate, TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time"));
            DateTime enddateU = TimeZoneInfo.ConvertTime(enddate, TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time"));

            OnlineMeeting _meeting = new OnlineMeeting();
            _meeting.subject = Subject;
            _meeting.body = new Body();
            _meeting.body.content = "HTML";
            _meeting.body.contentType = Message;
            _meeting.start = new Start();
            _meeting.start.dateTime = startdateU;
            _meeting.start.timeZone = "Central Standard Time";
            _meeting.end = new End();
            _meeting.end.dateTime = enddateU;
            _meeting.end.timeZone = "Central Standard Time";
            _meeting.location = new Location();
            _meeting.location.displayName = "Risebird Meeting";
            _meeting.attendees = new List<Attendee>();
            _meeting.attendees.Add(new Attendee { emailAddress = new EmailAddress { address = emailid, name = "" }, type = "required" });
            _meeting.isOnlineMeeting = true;
            _meeting.onlineMeetingProvider = "teamsForBusiness";
            _meeting.AccountEmail = _meetingInviteService.CheckMeetingAccount(startdateU, enddateU).Result;

            var functionKey = "";
            var path = $"MSTeamMeeting";
            var getResponse = _httpClient.SendRequestAsync(HttpMethod.Post, path, _meeting, functionKey, _cancellationToken).Result;
            var getResult = getResponse.Content.ReadAsStringAsync().Result;





            ////Save upcoming online meeting
            //var resultsetvalue = _chatservice.InsertOnlinemeeting(startdateU, enddateU, Emailaddress, _meeting.AccountEmail, response.ToString(), emailid, Subject);
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
