using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using static System.Data.CommandType;

namespace Risebird.Repository
{
    public class IRFRepository : IIRFRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        public IRFRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }
        public IEnumerable<Users> GetCientUsers()
        {
            IEnumerable<ClientUser> clientUsers = new List<ClientUser>();
            IEnumerable<Users> clients = new List<Users>();
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    clientUsers = SqlMapper.Query<ClientUser>(
                        con, "select * from ClientUser", null, commandType: Text);
                    var idList = new List<int>();
                    foreach (var item in clientUsers)
                    {
                        idList.Add(item.UserId);
                    }
                    string ids = string.Join<int>(",", idList);
                    clients = SqlMapper.Query<Users>(
                        con, "select * from UserList where id in ("+ids+")", null, commandType: Text);
                    return clients;
                }
            }
            catch (SqlException ex)
            {
                //_errorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<ClientRole> GetClientRoles()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<ClientRole>(
                        con, "select * from ClientRole", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                //_errorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<Client> GetClients()
        {
            IEnumerable<Client> client = new List<Client>();
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    client= SqlMapper.Query<Client>(
                        con, "select * from Client", null, commandType: Text);
                }
                return client;
            }
            catch (SqlException ex)
            {
                //_errorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<InterviewerVM> GetInterviewers()
        {
            IEnumerable<InterviewerVM> interviewersVm = new List<InterviewerVM>();
            IEnumerable<Interviewer> interviewers = new List<Interviewer>();
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand();
                    interviewers =  SqlMapper.Query<Interviewer>(
                        con, "select * from Interviewer", null, commandType: Text);
                    var idList = new List<int>();
                    foreach(var item in interviewers)
                    {
                        idList.Add(item.UserId);
                    }
                    string ids = string.Join<int>(",", idList);
                    interviewersVm = SqlMapper.Query<InterviewerVM>(
                        con, "select * from UserList where id in ("+ ids + ")", null, commandType: Text);
                    return interviewersVm;
                }
            }
            catch (SqlException ex)
            {
                //_errorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<Level> GetLevels()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<Level>(
                        con, "select * from Level", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                //_errorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<Users> GetQualityController()
        {
            IEnumerable<QualityController> qualityControllers = new List<QualityController>();
            IEnumerable<Users> users = new List<Users>();
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    qualityControllers = SqlMapper.Query<QualityController>(
                        con, "select * from QualityController", null, commandType: Text);
                    var idList = new List<int>();
                    foreach (var item in qualityControllers)
                    {
                        idList.Add(item.UserId);
                    }
                    string ids = string.Join<int>(",", idList);
                    users = SqlMapper.Query<Users>(
                        con, "select * from UserList where  id in (" + ids + ")", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                //_errorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return users;
        }

        public IEnumerable<Skill> GetSkills()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<Skill>(
                        con, "select * from Skill", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                //_errorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<Users> GetUsers()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<Users>(
                        con, "select * from UserList", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                //_errorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
    }
}
