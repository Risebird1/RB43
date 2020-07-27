using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RestSharp;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;

namespace Risebird.Web.Controllers
{
    public class ChatController : Controller
    {
        private IConfiguration _configuration;
        public ChatController(IConfiguration configuration)
        {
            _configuration = configuration;
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
        public JsonResult SendMeetingInvite()
        {
            var client = new RestClient("http://localhost:7071/api/");
            var request = new RestRequest("OnlineMeeting", Method.POST);
            request.RequestFormat = DataFormat.Json;
            dynamic obj = new ExpandoObject();
            obj.startDateTime = "2020-07-28T14:30:34.2444915-07:00";
            obj.endDateTime = "2020-07-28T15:00:34.2464912-07:00";
            obj.subject = "User Token Meeting";

            request.AddBody(obj);
            var result=client.Execute(request);
            return Json("");
        }
    }
}
