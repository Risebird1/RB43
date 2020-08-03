using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Risebird.Repository
{
    public class ChatRepository : IChatRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string dbconnectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        public ChatRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            dbconnectionString = _configuration["ConnectionString:DBConnectionString"];
        }
        public List<Conversation> GetConversation(string userid, string contact, string status, string WhoSend)
        {
            try
            {
                string connectionString = dbconnectionString;
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
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<Conversation>();
            }
        }

        public void AddConversation(string userid, string contact, string message)
        {
            try
            {
                string connectionString = dbconnectionString;
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
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

            }
        }
        public void AddGroup(string GroupName)
        {
            try
            {
                string connectionString = dbconnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var query = "Insert into ChatGroupName Values(@GroupName)";
                    var parameters = new DynamicParameters();
                    parameters.Add("@GroupName", GroupName);
                    var results = con.Query(query, parameters);
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
        public List<GroupDetails> FetchGroup()
        {
            try
            {
                string connectionString = dbconnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var query = "Select id,GroupName from ChatGroupName";
                    var results = con.Query<GroupDetails>(query);
                    return (List<GroupDetails>)results;
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public void DeleteGroup(string GroupId)
        {
            try
            {
                string connectionString = dbconnectionString;
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
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
        public List<GroupDetails> FetchUser(string GroupId)
        {
            try
            {
                string connectionString = dbconnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var query = "Select cg.id,ul.Emailid from ChatGroupUsers cg join UserList ul on cg.Userid= ul.id where GroupId=@Id";
                    var parameters = new DynamicParameters();
                    parameters.Add("@Id", GroupId);
                    var results = con.Query<GroupDetails>(query, parameters);
                    // return result;
                    return (List<GroupDetails>)results;
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public string AddUser(string GroupId, string Emailid)
        {
            try
            {
                string connectionString = dbconnectionString;
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
                            return "success";
                        }
                        else
                        {
                            return "User Not found";
                        }
                    }
                    else
                    {
                        return "Duplicate User";
                    }
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "User Not found";
            }
        }
        public void DeleteUser(string Userid)
        {
            try
            {
                string connectionString = dbconnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var query = "Delete from ChatGroupUsers where Id=@Id";
                    var parameters = new DynamicParameters();
                    parameters.Add("@Id", Userid);
                    var results = con.Query(query, parameters);
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
        public string Savetheme(string theme, string userid)
        {
            try
            {
                string connectionString = dbconnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var query = "Update  UserList SET Usertheme = @Usertheme where EmailId = @id";
                    var parameters = new DynamicParameters();
                    parameters.Add("@id", userid);
                    parameters.Add("@Usertheme", theme);
                    var results = con.Query(query, parameters);
                    return "success";
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "";
            }
        }
    }
}
