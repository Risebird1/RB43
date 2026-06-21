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
                    var parameters = new DynamicParameters();
                    parameters.Add("@GroupName", GroupName);
                    SqlMapper.Query(con, "usp_AddChatGroupName", parameters,
                        commandType: CommandType.StoredProcedure);
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
                    var results = SqlMapper.Query<GroupDetails>(con, "usp_FetchChatGroups", null,
                        commandType: CommandType.StoredProcedure);
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
                    var parameters = new DynamicParameters();
                    parameters.Add("@Id", GroupId);
                    SqlMapper.Query(con, "usp_DeleteChatGroupUsersByGroupId", parameters,
                        commandType: CommandType.StoredProcedure);
                }
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Id", GroupId);
                    SqlMapper.Query(con, "usp_DeleteChatGroupName", parameters,
                        commandType: CommandType.StoredProcedure);
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
                    var parameters = new DynamicParameters();
                    parameters.Add("@Id", GroupId);
                    var results = SqlMapper.Query<GroupDetails>(con, "usp_FetchChatGroupUsers", parameters,
                        commandType: CommandType.StoredProcedure);
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
                    var parameters = new DynamicParameters();
                    parameters.Add("@Emailid", Emailid);
                    id = SqlMapper.Query<int>(con, "usp_GetUserIdByEmailId", parameters,
                        commandType: CommandType.StoredProcedure).FirstOrDefault();
                }
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Id", id);
                    parameters.Add("@GroupId", GroupId);
                    count = SqlMapper.Query<int>(con, "usp_GetChatGroupUserCount", parameters,
                        commandType: CommandType.StoredProcedure).FirstOrDefault();
                }
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    if (count == 0)
                    {
                        if (id != 0)
                        {
                            var parameters = new DynamicParameters();
                            parameters.Add("@Id", id);
                            parameters.Add("@GroupId", GroupId);
                            SqlMapper.Query(con, "usp_AddChatGroupUser", parameters,
                                commandType: CommandType.StoredProcedure);
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
                    var parameters = new DynamicParameters();
                    parameters.Add("@Id", Userid);
                    SqlMapper.Query(con, "usp_DeleteChatGroupUser", parameters,
                        commandType: CommandType.StoredProcedure);
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
                    var parameters = new DynamicParameters();
                    parameters.Add("@id", userid);
                    parameters.Add("@Usertheme", theme);
                    SqlMapper.Query(con, "usp_UpdateUserTheme", parameters,
                        commandType: CommandType.StoredProcedure);
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
