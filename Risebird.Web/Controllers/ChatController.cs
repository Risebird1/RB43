using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RestSharp;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Risebird.Web.Controllers
{
    public class ChatController : Controller
    {
        private IConfiguration _configuration;
        private readonly INotificationHelper _notificationHelper;
        private readonly IChatService _chatservice;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public ChatController(IConfiguration configuration, INotificationHelper notificationHelper, IChatService chatservice, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _notificationHelper = notificationHelper;
            _chatservice = chatservice;
            _applicationErrorLogger = applicationErrorLogger;
        }
        [HttpPost]
        public JsonResult ChatBox()
        {
            try
            {
                var user = string.Empty;
                if (HttpContext.Session.GetString("email") == null)
                {

                }
                else
                {
                    user = HttpContext.Session.GetString("email")?.Trim();
                    user = user.Replace("\"", "");
                }
                //To fetch Details of incoming users massage 
                var resultset = _chatservice.GetConversation(user, "", "Chat", "");
                var _lst = resultset.Select(x => x.Sender.ToUpper()).Distinct();
                var _lstset = new List<Conversation>();
                foreach (var item in _lst)
                {
                    var Isonline = resultset.Where(x => x.Sender.ToUpper() == item).Select(x => x.IsOnline).FirstOrDefault();
                    var Whosend = resultset.Where(x => x.Sender.ToUpper() == item).Select(x => x.WhoSend).FirstOrDefault();
                    var status = resultset.Where(x => x.Sender.ToUpper() == item).Select(x => x.Status).FirstOrDefault();
                    _lstset.Add(new Conversation { Sender = item, IsOnline = Isonline, WhoSend = Whosend, Status = status });
                }
                ViewBag.UserList = _lstset;
                ViewBag.Self = user.ToUpper();
                return Json(new { result = _lstset, Self = user.ToUpper() });
            }
            catch (Exception)
            {
                var user = HttpContext.Session.GetString("email")?.Trim();
                user = user.Replace("\"", "");
                var _lstset = new List<Conversation>();
                return Json(new { result = _lstset, Self = user.ToUpper() });
            }
        }
        public List<Conversation> GetConversation(string userid, string contact, string status, string WhoSend)
        {
            try
            {
                string connectionString = _configuration["ConnectionString:DBConnectionString"];
                if (String.IsNullOrEmpty(userid))
                {
                    if (HttpContext.Session.GetString("email")?.Trim() == null)
                    {
                        userid = "";
                    }
                    else
                    {
                        userid = HttpContext.Session.GetString("email")?.Trim();
                    }
                }
                if (String.IsNullOrEmpty(contact))
                {
                    contact = "";
                }
                userid = userid.Replace("\"", "");
                contact = contact.Replace("\"", "");
                //To fetch Details of users massage and fetch user to user converation
                return _chatservice.GetConversation(userid, contact, status, WhoSend);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<Conversation>();
            }
        }
        //User to add message into database
        public void AddConversation(string userid, string contact, string message)
        {
            try
            {
                if (String.IsNullOrEmpty(userid))
                {
                    userid = HttpContext.Session.GetString("email")?.Trim();
                }
                userid = userid.Replace("\"", "");
                contact = contact.Replace("\"", "");
                _chatservice.AddConversation(userid, contact, message);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
        //User to add Group 

        public void AddGroup(string GroupName)
        {
            try
            {
                if (!String.IsNullOrEmpty(HttpContext.Session.GetString("email")?.Trim()))
                {
                    var userid = HttpContext.Session.GetString("email")?.Trim();
                    userid = userid.Replace("\"", "");
                    _chatservice.AddGroup(GroupName);
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
        //To fetch Groupnames
        public JsonResult FetchGroup()
        {
            try
            {
                if (!String.IsNullOrEmpty(HttpContext.Session.GetString("email")?.Trim()))
                {
                    var userid = HttpContext.Session.GetString("email")?.Trim();
                    userid = userid.Replace("\"", "");
                    var results = _chatservice.FetchGroup();
                    return Json(results);
                }
                return Json("");
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("");
            }
        }
        //to delete Existing gropus
        public void DeleteGroup(string GroupId)
        {
            try
            {
                if (!String.IsNullOrEmpty(HttpContext.Session.GetString("email")?.Trim()))
                {
                    var userid = HttpContext.Session.GetString("email")?.Trim();
                    userid = userid.Replace("\"", "");
                    _chatservice.DeleteGroup(GroupId);
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
        //To fetch the users into a group
        public JsonResult FetchUser(string GroupId)
        {
            try
            {
                if (!String.IsNullOrEmpty(HttpContext.Session.GetString("email")?.Trim()))
                {
                    var userid = HttpContext.Session.GetString("email")?.Trim();
                    userid = userid.Replace("\"", "");
                    var results = _chatservice.FetchUser(GroupId);
                    return Json(results);
                }
                return Json("");
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("");
            }

        }
        //To add users into a group
        public JsonResult AddUser(string GroupId, string Emailid)
        {
            try
            {
                var result = _chatservice.AddUser(GroupId, Emailid);
                return Json(result);
            }
            catch (Exception)
            {
                return Json("User Not found");
            }
        }
        //To delete users from the group
        public void DeleteUser(string Userid)
        {
            try
            {
                _chatservice.DeleteUser(Userid);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        /* Store user selected theme in database user table     */
        public JsonResult Savetheme(string theme)
        {
            try
            {
                if (!String.IsNullOrEmpty(HttpContext.Session.GetString("email")?.Trim()))
                {
                    var userid = HttpContext.Session.GetString("email")?.Trim();
                    userid = userid.Replace("\"", "");
                    var results = _chatservice.Savetheme(theme, userid);
                    HttpContext.Session.SetString("Currenttheme", theme);
                    return Json(results);
                }
                return Json("");
            }
            catch (Exception)
            {
                return Json("");
            }
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

            RestClient _client = new RestClient("http://localhost:7071/api/");
            RestRequest request = new RestRequest("MSTeamMeeting", Method.POST);
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
            _meeting.location.displayName = "Tim Tom";
            _meeting.attendees = new List<Attendee>();
            _meeting.attendees.Add(new Attendee { emailAddress = new EmailAddress { address = emailid, name = "" }, type = "required" });
            _meeting.isOnlineMeeting = true;
            _meeting.onlineMeetingProvider = "teamsForBusiness";
            // to chek the emailid for create a meeting for perticular timeslot
            var result = _chatservice.checkOnlinemettings(startdateU, enddateU);
            _meeting.AccountEmail = result;
            request.AddBody(_meeting);
            var response = _client.Execute<OnlineMeetingResponse>(request);
            //Save upcoming online meeting
            var resultsetvalue = _chatservice.InsertOnlinemeeting(startdateU, enddateU, Emailaddress, _meeting.AccountEmail, response.ToString(), emailid);
            //Send mail with meeting invite to candidate and who create meeting
            Mail _mail = new Mail
            {
                TO = emailid,
                CC = Emailaddress,
                Subject = Subject
            };
            var resultset = _notificationHelper.OnlineMeetingInvite(_mail, response.ToString(), Message, startdateU, enddateU);
            if (resultset == "Mail sent Successfully.")
            {
                return Json("Invite Successfully Send");
            }
            else
            {
                return Json("Fail To send invite");
            }
        }
    }
}
