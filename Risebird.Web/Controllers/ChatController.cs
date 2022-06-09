using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Web.Controllers
{
    [Authorize]
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
                if (HttpContext.Session.GetString("email") != null)
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

        [HttpGet]
        public List<Conversation> GetConversation(string userid, string contact, string status, string WhoSend)
        {
            try
            {
                if (string.IsNullOrEmpty(userid))
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
                if (string.IsNullOrEmpty(contact))
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

        [HttpGet]
        public void AddConversation(string userid, string contact, string message)
        {
            try
            {
                if (string.IsNullOrEmpty(userid))
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
        [HttpGet]
        public void AddGroup(string GroupName)
        {
            try
            {
                if (!string.IsNullOrEmpty(HttpContext.Session.GetString("email")?.Trim()))
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
        [HttpGet]
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
        [HttpGet]
        public void DeleteGroup(string GroupId)
        {
            try
            {
                if (!string.IsNullOrEmpty(HttpContext.Session.GetString("email")?.Trim()))
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
        [HttpGet]
        public JsonResult FetchUser(string GroupId)
        {
            try
            {
                if (!string.IsNullOrEmpty(HttpContext.Session.GetString("email")?.Trim()))
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
        [HttpGet]
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
        [HttpGet]
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
        [HttpGet]
        public JsonResult Savetheme(string theme)
        {
            try
            {
                if (!string.IsNullOrEmpty(HttpContext.Session.GetString("email")?.Trim()))
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

    }
}
