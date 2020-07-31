using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RestSharp;
using Risebird.CommonHelper.Interface;
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

        public ChatController(IConfiguration configuration, INotificationHelper notificationHelper)
        {
            _configuration = configuration;
            _notificationHelper = notificationHelper;
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
                var resultset = GetConversation(user, "", "Chat", "");
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
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@id", userid);
                    parameters.Add("@contact", contact);
                    if (status == "Chat")
                    {
                        var result = SqlMapper.Query<Conversation>(con, "usp_GetConversation", parameters,
                              commandType: CommandType.StoredProcedure);
                        return (List<Conversation>)result;
                    }
                    else
                    {
                        parameters.Add("@WhoSend", WhoSend);
                        var result = SqlMapper.Query<Conversation>(con, "usp_GetConversationdetails", parameters,
                               commandType: CommandType.StoredProcedure);
                        var resultset = new List<Conversation>();
                        foreach (var item in result)
                        {
                            if (item.id == 0)
                            {

                            }
                            else
                            {
                                item.Sender = item.Sender.ToUpper();
                                item.Reciver = item.Reciver.ToUpper();
                                if (item.Sender == userid.ToUpper())
                                {
                                    item.self = 1;
                                }
                                else
                                {
                                    item.self = 0;
                                }
                                resultset.Add(item);
                            }
                        }
                        return resultset;
                    }

                }
            }
            catch (Exception)
            {
                return new List<Conversation>();
            }
        }
        public void AddConversation(string userid, string contact, string message)
        {
            try
            {
                string connectionString = _configuration["ConnectionString:DBConnectionString"];
                if (String.IsNullOrEmpty(userid))
                {
                    userid = HttpContext.Session.GetString("email")?.Trim();
                }
                userid = userid.Replace("\"", "");
                contact = contact.Replace("\"", "");
                string WhoSend = "U";
                if (contact.Contains("@"))
                {
                    WhoSend = "U";
                }
                else
                {
                    WhoSend = "G";
                }
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@id", userid);
                    parameters.Add("@contact", contact);
                    parameters.Add("@message", message);
                    parameters.Add("@WhoSend", WhoSend);

                    var result = SqlMapper.Query(con, "usp_AddConversation", parameters,
                            commandType: CommandType.StoredProcedure);
                    // return result;
                }
            }
            catch (Exception)
            {

            }
        }
        public void AddGroup(string GroupName)
        {
            try
            {
                string connectionString = _configuration["ConnectionString:DBConnectionString"];
                if (!String.IsNullOrEmpty(HttpContext.Session.GetString("email")?.Trim()))
                {
                    var userid = HttpContext.Session.GetString("email")?.Trim();
                    userid = userid.Replace("\"", "");
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var query = "Insert into ChatGroupName Values(@GroupName)";
                        var parameters = new DynamicParameters();
                        parameters.Add("@GroupName", GroupName);
                        var results = con.Query(query, parameters);
                    }
                }
            }
            catch (Exception)
            {

            }
        }
        public JsonResult FetchGroup()
        {
            try
            {
                string connectionString = _configuration["ConnectionString:DBConnectionString"];
                if (!String.IsNullOrEmpty(HttpContext.Session.GetString("email")?.Trim()))
                {
                    var userid = HttpContext.Session.GetString("email")?.Trim();
                    userid = userid.Replace("\"", "");
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var query = "Select id,GroupName from ChatGroupName";
                        var results = con.Query<GroupDetails>(query);
                        // return result;
                        return Json(results);
                    }
                }
                return Json("");
            }
            catch (Exception)
            {
                return Json("");
            }
        }
        public void DeleteGroup(string GroupId)
        {
            try
            {
                string connectionString = _configuration["ConnectionString:DBConnectionString"];
                if (!String.IsNullOrEmpty(HttpContext.Session.GetString("email")?.Trim()))
                {
                    var userid = HttpContext.Session.GetString("email")?.Trim();
                    userid = userid.Replace("\"", "");
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var query = "Delete from ChatGroupUsers where Groupid=@Id";
                        var parameters = new DynamicParameters();
                        parameters.Add("@Id", GroupId);
                        var results = con.Query(query, parameters);
                    }
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var query = "Delete from ChatGroupName where Id=@Id";
                        var parameters = new DynamicParameters();
                        parameters.Add("@Id", GroupId);
                        var results = con.Query(query, parameters);
                    }
                }
            }
            catch (Exception)
            {

            }
        }
        public JsonResult FetchUser(string GroupId)
        {
            try
            {
                string connectionString = _configuration["ConnectionString:DBConnectionString"];
                if (!String.IsNullOrEmpty(HttpContext.Session.GetString("email")?.Trim()))
                {
                    var userid = HttpContext.Session.GetString("email")?.Trim();
                    userid = userid.Replace("\"", "");
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var query = "Select cg.id,ul.Emailid from ChatGroupUsers cg join UserList ul on cg.Userid= ul.id where GroupId=@Id";
                        var parameters = new DynamicParameters();
                        parameters.Add("@Id", GroupId);
                        var results = con.Query<GroupDetails>(query, parameters);
                        // return result;
                        return Json(results);
                    }
                }
                return Json("");
            }
            catch (Exception)
            {
                return Json("");
            }

        }
        public JsonResult AddUser(string GroupId, string Emailid)
        {
            try
            {
                string connectionString = _configuration["ConnectionString:DBConnectionString"];
                int id = 0;
                int count = 0;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var query = "Select Id from UserList where Emailid=@Emailid";
                    var parameters = new DynamicParameters();
                    parameters.Add("@Emailid", Emailid);
                    id = con.Query<int>(query, parameters).FirstOrDefault();
                }
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var query = "Select Count(*) from ChatGroupUsers where Userid=@Id and Groupid=@GroupId";
                    var parameters = new DynamicParameters();
                    parameters.Add("@Id", id);
                    parameters.Add("@GroupId", GroupId);
                    count = con.Query<int>(query, parameters).FirstOrDefault();
                }
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    if (count == 0)
                    {
                        if (id != 0)
                        {
                            var query = "Insert into ChatGroupUsers values(@GroupId,@Id)";
                            var parameters = new DynamicParameters();
                            parameters.Add("@Id", id);
                            parameters.Add("@GroupId", GroupId);
                            var results = con.Query(query, parameters);
                            return Json("success");
                        }
                        else
                        {
                            return Json("User Not found");

                        }
                    }
                    else
                    {
                        return Json("Duplicate User");
                    }
                }
            }
            catch (Exception)
            {
                return Json("User Not found");
            }
        }
        public void DeleteUser(string Userid)
        {
            try
            {
                string connectionString = _configuration["ConnectionString:DBConnectionString"];
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var query = "Delete from ChatGroupUsers where Id=@Id";
                    var parameters = new DynamicParameters();
                    parameters.Add("@Id", Userid);
                    var results = con.Query(query, parameters);
                }
            }
            catch (Exception)
            {

            }
        }

        /* Store theme in database user table     */



        public JsonResult Savetheme(string theme)
        {
            try
            {
                string connectionString = _configuration["ConnectionString:DBConnectionString"];
                if (!String.IsNullOrEmpty(HttpContext.Session.GetString("email")?.Trim()))
                {
                    var userid = HttpContext.Session.GetString("email")?.Trim();
                    userid = userid.Replace("\"", "");
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var query = "Update  UserList SET Usertheme = @Usertheme where EmailId = @id";
                        var parameters = new DynamicParameters();
                        parameters.Add("@id", userid);
                        parameters.Add("@Usertheme", theme);
                        var results = con.Query(query, parameters);

                        HttpContext.Session.SetString("Currenttheme", theme);
                        // return result;
                        return Json(results);
                    }
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
            RestClient _client = new RestClient("http://localhost:7071/api/");
            RestRequest request = new RestRequest("MSTeamMeeting", Method.POST);
            OnlineMeeting _meeting = new OnlineMeeting();
            _meeting.subject = Subject;
            _meeting.body = new Body();
            _meeting.body.content = "HTML";
            _meeting.body.contentType = "Does next month work for you?";
            _meeting.start = new Start();
            _meeting.start.dateTime = startdate;
            _meeting.start.timeZone = "India Standard Time";
            _meeting.end = new End();
            _meeting.end.dateTime = enddate;
            _meeting.end.timeZone = "India Standard Time";
            _meeting.location = new Location();
            _meeting.location.displayName = "Tim Tom";
            _meeting.attendees.Add(new Attendee { emailAddress = new EmailAddress { address = emailid, name = "Prem Prakash" }, type = "required" });
            _meeting.isOnlineMeeting = true;
            _meeting.onlineMeetingProvider = "teamsForBusiness";

            string connectionString = _configuration["ConnectionString:DBConnectionString"];
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@startdate", startdate);
                parameters.Add("@enddate", enddate);
                var result = SqlMapper.Query(con, "usp_checkOnlinemettings", parameters,
                        commandType: CommandType.StoredProcedure);
                _meeting.AccountEmail = result.FirstOrDefault().ToString();
            }
            request.AddBody(_meeting);
            var response = _client.Execute<OnlineMeetingResponse>(request);
            
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@startdate", startdate);
                parameters.Add("@enddate", enddate);
                parameters.Add("@Whocreate", Emailaddress);
                parameters.Add("@Meetingid", _meeting.AccountEmail);
                parameters.Add("@LinkUrl", response.ToString());
                //parameters.Add("@Whom", emailid);
                var result = SqlMapper.Query(con, "usp_InsertOnlinemeeting", parameters,
                        commandType: CommandType.StoredProcedure);
            }

            Mail _mail = new Mail
            {
                TO = emailid,
                CC = Emailaddress,
                Subject = Subject
            };
            var resultset = _notificationHelper.OnlineMeetingInvite(_mail, response.ToString(), Message);
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
