using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using static System.Data.CommandType;

namespace Risebird.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private IApplicationErrorLogger _applicationErrorLogger;

        public UserRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        #region User Repository Functions

        /// <summary>
        /// Get all the Risebird User- Admin and HR
        /// </summary>
        /// <returns></returns>
        public IEnumerable<UserList> GetRisebirdUsers()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<UserList>(con, "[dbo].[sp_Risebirdusers]", null,
                        commandType: StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> AddUser(UserList user)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = SetStoredProcedureParameterForUser(user);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<int>(con, "usp_AddUser", parameters,
                        commandType: CommandType.StoredProcedure);
                    user.Id = result.FirstOrDefault();
                    DataTable UserclientDt = new DataTable();
                    UserclientDt.Columns.Add("IRFId", typeof(int));
                    UserclientDt.Columns.Add("ClientUserId", typeof(int));
                    if (user.ClientsArray != null)
                    {
                        foreach (var item in user.ClientsArray)
                        {
                            UserclientDt.Rows.Add(user.Id, item);
                        }

                        SqlCommand cmdClientUser = new SqlCommand("usp_HRUserClientBulkInsert", con);
                        cmdClientUser.CommandType = CommandType.StoredProcedure;
                        cmdClientUser.Parameters.Add(new SqlParameter("@HRUserClientBulkData", SqlDbType.Structured));
                        cmdClientUser.Parameters["@HRUserClientBulkData"].Value = UserclientDt;
                        con.Open();
                        if (cmdClientUser.ExecuteNonQuery() != 0)
                        {
                            con.Close();
                        }
                    }
                    return true;
                }
            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<UserList> GetUser(int id)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QuerySingleAsync<UserList>(con, "usp_GetUserById", parameters,
                        commandType: CommandType.StoredProcedure);
                }
            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> UpdateUser(UserList user)
        {
            try
            {
                DynamicParameters parameters = SetStoredProcedureParameterForUser(user);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_UpdateUser", parameters,
                        commandType: CommandType.StoredProcedure);
                    DataTable UserclientDt = new DataTable();
                    UserclientDt.Columns.Add("IRFId", typeof(int));
                    UserclientDt.Columns.Add("ClientUserId", typeof(int));
                    if (user.ClientsArray != null)
                    {
                        foreach (var item in user.ClientsArray)
                        {
                            UserclientDt.Rows.Add(user.Id, item);
                        }
                        SqlCommand cmdClientUser = new SqlCommand("usp_HRUserClientBulkInsert", con);
                        cmdClientUser.CommandType = CommandType.StoredProcedure;
                        cmdClientUser.Parameters.Add(new SqlParameter("@HRUserClientBulkData", SqlDbType.Structured));
                        cmdClientUser.Parameters["@HRUserClientBulkData"].Value = UserclientDt;
                        con.Open();
                        if (cmdClientUser.ExecuteNonQuery() != 0)
                        {
                            con.Close();
                        }
                    }
                    return true;
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<bool> DeleteUser(int id)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_DeleteUser", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        private DynamicParameters SetStoredProcedureParameterForUser(UserList entity)
        {
            var parameters = new DynamicParameters();

            if (entity.Id > 0)
            {
                parameters.Add("@id", entity.Id);
                parameters.Add("@IsActive", entity.IsActive);
            }
            //if (entity.IdProfile.HasValue)

            parameters.Add("@selectedProfileId", entity.IdProfile);
            parameters.Add("@userFirstName", entity.FirstName);
            parameters.Add("@userLastName", entity.LastName);
            parameters.Add("@userEmailId", entity.EmailId);
            parameters.Add("@userMobileNumber", entity.MobileNumber);
            parameters.Add("@WhatsappNumber", entity.WhatsappNumber);

            return parameters;
        }

        #endregion

        #region Client User Repository Function

        /// <summary>
        /// Get all the Client Users
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ClientUserVM> GetClientUsers(string EmailId)
        {
            try
            {
                DynamicParameters dp = new DynamicParameters();
                dp.Add("@EmailId", EmailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<ClientUserVM>(con, "[dbo].[sp_RisebirdClientUser]", dp,
                        commandType: StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<ClientUserVM> GetClientUserDetails(string emailID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("Email", emailID);
                    var result = await SqlMapper.QueryAsync<ClientUserVM>(con, "sp_ClientUserDetail", parameters,
                         commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> AddClientUser(ClientUserVM client)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = SetStoredProcedureParameterForClientUser(client, true);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_AddClientUser", parameters,
                        commandType: CommandType.StoredProcedure);

                    return true;

                }
            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<bool> DeleteClientUser(int id)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_DeleteClientUser", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<ClientUserVM> GetClientUser(int id)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QuerySingleAsync<ClientUserVM>(con, "usp_GetClientUserById", parameters,
                        commandType: CommandType.StoredProcedure);
                }
            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> UpdateClientUser(ClientUserVM clientUser)
        {
            try
            {
                DynamicParameters parameters = SetStoredProcedureParameterForClientUser(clientUser);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_UpdateClientUser", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        private DynamicParameters SetStoredProcedureParameterForClientUser(ClientUserVM entity, bool isAdd = false)
        {
            var parameters = new DynamicParameters();

            if (entity.id > 0)
                parameters.Add("@id", entity.id);
            if (entity.SelectedClientRole.HasValue)
                parameters.Add("@selectedClientRoleId", entity.SelectedClientRole);

            parameters.Add("@userFirstName", entity.FirstName);
            parameters.Add("@userLastName", entity.LastName);
            parameters.Add("@userEmailId", entity.EmailId);
            parameters.Add("@userMobileNumber", entity.MobileNumber);
            parameters.Add("@UserPassword", entity.UserPassword);
            parameters.Add("@WhatsappNumber", entity.WhatsappNumber);

            if (isAdd)
            {
                parameters.Add("@IsPasswordReset", entity.IsPasswordReset);
                if (entity.SelectedClient.HasValue)
                    parameters.Add("@selectedClientId", entity.SelectedClient);
                parameters.Add("@message", SqlDbType.VarChar, direction: ParameterDirection.Output);

            }

            return parameters;
        }

        public async Task<bool> ResetNavigation(int userId)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@userId", userId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_ResetNavigation", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }



        #endregion        

        #region Interviewer Repository Functions

        /// <summary>
        /// Get all the Interviewer
        /// </summary>
        /// <returns></returns>
        public IEnumerable<InterviewerVM> GetInterviewers()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<InterviewerVM>(con, "[dbo].[sp_RisebirdInterviewer]", null,
                        commandType: StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<InterviewerVM> GetInterviewersDetails()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<InterviewerVM>(con, "[dbo].[usp_RisebirdInterviewerDetails]", null,
                        commandType: StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<bool> AddInterviewer(InterviewerVM interviewer)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = SetStoredProcedureParameterForInterviewer(interviewer);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_AddInterviewer", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<bool> DeleteInterviewer(int id)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_DeleteInterviewer", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<InterviewerVM> GetInterviewer(int id)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QuerySingleAsync<InterviewerVM>(con, "usp_GetInterviewerById", parameters,
                        commandType: CommandType.StoredProcedure);
                }
            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public IEnumerable<InterviewerVM> GetInterviewerVideos(string Emailid)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Emailid", Emailid);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<InterviewerVM>(con, "[dbo].[usp_GetInterviewerVideos]", parameters,
                        commandType: StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public MeetingVM GetMeetingbyid(int id)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@interviewId", id);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = SqlMapper.Query<MeetingVM>(con, "[dbo].[usp_GetInterviewerMeetingsbyId]", parameters,
                        commandType: StoredProcedure);
                    return result.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<bool> UpdateInterviewer(InterviewerVM Interviewer)
        {
            try
            {
                DynamicParameters parameters = SetStoredProcedureParameterForInterviewer(Interviewer);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_UpdateInterviewer", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        private DynamicParameters SetStoredProcedureParameterForInterviewer(InterviewerVM entity)
        {
            var parameters = new DynamicParameters();

            if (entity.id > 0)
                parameters.Add("@id", entity.id);

            parameters.Add("@selectedSkills", entity.PrimarySkill);
            parameters.Add("@resumeLink", entity.ResumeLink);
            parameters.Add("@isApproved", entity.IsApproved);
            parameters.Add("@slots", entity.Slots);
            parameters.Add("@userFirstName", entity.FirstName);
            parameters.Add("@userLastName", entity.LastName);
            parameters.Add("@userEmailId", entity.EmailId.Trim().ToLower());
            parameters.Add("@userMobileNumber", entity.MobileNumber);
            parameters.Add("@Heuristicsscore", entity.Heuristicsscore);
            return parameters;
        }

        #endregion


        #region QCInterviewer Repository Functions

        public IEnumerable<QCInterviewerVM> GetQCInterviewers()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<QCInterviewerVM>(con, "[dbo].[sp_RisebirdQCInterviewer]", null,
                        commandType: StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> AddQCInterviewer(QCInterviewerVM qcInterviewer)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = SetStoredProcedureParameterForQCInterviewer(qcInterviewer);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_AddQCInterviewer", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<QCInterviewerVM> GetQCInterviewer(int id)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QuerySingleAsync<QCInterviewerVM>(con, "usp_GetQCInterviewerById", parameters,
                        commandType: CommandType.StoredProcedure);
                }
            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> UpdateQCInterviewer(QCInterviewerVM qcInterviewer)
        {
            try
            {
                DynamicParameters parameters = SetStoredProcedureParameterForQCInterviewer(qcInterviewer);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_UpdateQCInterviewer", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<bool> DeleteQCInterviewer(int id)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_DeleteQCInterviewer", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        private DynamicParameters SetStoredProcedureParameterForQCInterviewer(QCInterviewerVM entity)
        {
            var parameters = new DynamicParameters();

            if (entity.id > 0)
                parameters.Add("@id", entity.id);

            parameters.Add("@selectedSkills", entity.PrimarySkill);
            parameters.Add("@resumeLink", entity.ResumeLink);
            parameters.Add("@successRate", entity.SuccessRate);
            parameters.Add("@slots", entity.Slots);
            parameters.Add("@noOfInterview", entity.NoOfInterview);

            parameters.Add("@userFirstName", entity.FirstName);
            parameters.Add("@userLastName", entity.LastName);
            parameters.Add("@userEmailId", entity.EmailId);
            parameters.Add("@userMobileNumber", entity.MobileNumber);

            return parameters;
        }


        #endregion

        /// <summary>
        /// get all the menu listed based on logged in user
        /// </summary>
        /// <param name="emailId"></param>
        /// <returns></returns>
        public IEnumerable<Menu> GetMenuBasedOnUserEmailld(string emailId, string sessionId)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@EmailId", emailId);
                parameters.Add("@SessionId", sessionId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<Menu>(con, "[dbo].[sp_GetMenuBasedOnEmailId]", parameters,
                        commandType: StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public bool CheckMultiSession(string emailId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var isOldSessionLoggedOut = SqlMapper.Query<bool>(
                    con, "select top 1 uss.IsActive from [dbo].[UserSessions] uss join UserList us on uss.UserId= us.id where us.EmailId = @EmailId order by uss.Id desc", new { EmailId = emailId }, commandType: Text).FirstOrDefault();
                    return isOldSessionLoggedOut;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
        public bool ClearSessionForOthersUsers(string emailId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlMapper.Query<bool>(
                       con, "update uss  set IsActive =0,Logout=GETUTCDATE()  from [dbo].[UserSessions] uss  join UserList us on uss.UserId= us.id  where us.EmailId = @emailId", new { value = true, emailId = emailId }, commandType: Text).FirstOrDefault();
                    return true;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }

        public IEnumerable<string> GetAllHrsEmailld()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<string>(
                        con, "select Emailid from [dbo].[UserList] where idprofile = 2", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<ResumeEvaluator> GetResumeEvaluators()
        {
            try
            {
                var query = "select re.*, ul.FirstName, ul.LastName, ul.EmailId, ul.MobileNumber from ResumeEvaluator re join UserList ul on re.UserId=ul.id";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<ResumeEvaluator>(query);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> AddResumeEvaluator(ResumeEvaluator resumeEvaluator)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@FirstName", resumeEvaluator.FirstName);
                parameters.Add("@LastName", resumeEvaluator.LastName);
                parameters.Add("@EmailId", resumeEvaluator.EmailId.Trim().ToLower());
                parameters.Add("@MobileNumber", resumeEvaluator.MobileNumber);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_AddResumeEvaluator", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            return false;
        }

        public ResumeEvaluator GetResumeEvaluator(int id)
        {
            try
            {
                var query = "select re.*, ul.FirstName, ul.LastName, ul.EmailId, ul.MobileNumber from ResumeEvaluator re join UserList ul on re.UserId= ul.id where re.id=@id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<ResumeEvaluator>(query, dynamicParameters);
                    return result.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public bool EditResumeEvaluator(ResumeEvaluator resumeEvaluator)
        {
            try
            {
                var query = "update UserList set FirstName= @FirstName, LastName = @LastName , EmailId= @EmailId , MobileNumber = @MobileNumber where id = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", resumeEvaluator.UserId);
                dynamicParameters.Add("@FirstName", resumeEvaluator.FirstName);
                dynamicParameters.Add("@LastName", resumeEvaluator.LastName);
                dynamicParameters.Add("@EmailId", resumeEvaluator.EmailId);
                dynamicParameters.Add("@MobileNumber", resumeEvaluator.MobileNumber);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<int>(query, dynamicParameters);
                    return true;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }


        public async Task<bool> DeleteResumeEvaluator(int userId)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@userId", userId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_DeleteResumeEvaluator", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public IEnumerable<Recruiter> GetRecruiters()
        {
            try
            {
                var query = "select Recruiter.*, ul.FirstName, ul.LastName, ul.EmailId, ul.MobileNumber from Recruiter join UserList ul on Recruiter.UserId=ul.id";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<Recruiter>(query);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> AddRecruiter(Recruiter recruiter)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@FirstName", recruiter.FirstName);
                parameters.Add("@LastName", recruiter.LastName);
                parameters.Add("@EmailId", recruiter.EmailId.Trim().ToLower());
                parameters.Add("@MobileNumber", recruiter.MobileNumber);
                parameters.Add("@Domain", recruiter.Domain);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_AddRecruiter", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            return false;
        }

        public Recruiter GetRecruiter(int id)
        {
            try
            {
                var query = "select re.*, ul.FirstName, ul.LastName, ul.EmailId, ul.MobileNumber from Recruiter re join UserList ul on re.UserId= ul.id where re.id=@id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<Recruiter>(query, dynamicParameters);
                    return result.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }


        public async Task<bool> EditRecruiter(Recruiter recruiter)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@UserId", recruiter.UserId);
                dynamicParameters.Add("@FirstName", recruiter.FirstName);
                dynamicParameters.Add("@LastName", recruiter.LastName);
                dynamicParameters.Add("@EmailId", recruiter.EmailId);
                dynamicParameters.Add("@Domain", recruiter.Domain);
                dynamicParameters.Add("@MobileNumber", recruiter.MobileNumber); using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_UpdateRecruiter", dynamicParameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<bool> DeleteRecruiter(int userId)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@userId", userId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_DeleteRecruiter", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task UpdateUserSigInDetails(string emailId)
        {
            try
            {
                var currentDate = DateTime.Now;
                var query = "update UserList set LastLogin= @LastLogin where EmailId = @EmailId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@LastLogin", currentDate);
                dynamicParameters.Add("@EmailId", emailId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<int>(query, dynamicParameters);
                }
            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);

            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);

            }
        }
        public string GetCurrenttheme(string emailid)
        {
            try
            {
                string connectionString = _configuration["ConnectionString:DBConnectionString"];
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var query = "Select Usertheme  from UserList where EmailId = @id";
                    var parameters = new DynamicParameters();
                    parameters.Add("@id", emailid);
                    var results = con.Query<string>(query, parameters);
                    return results.FirstOrDefault();
                }
            }
            catch (Exception)
            {
                return "";
            }
        }
        public IEnumerable<AssignmentVM> GetInterviewerEntitle()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    // var query = "select sk.id as id,LTRIM(RTRIM(sk.Name)) as PrimarySkill,Count(*) as NoOfInterview from Skill sk join TransInterviewerSkill tis on sk.id=tis.SkillId group by sk.Name,sk.id order by LTRIM(RTRIM(sk.Name))";
                    var query = "Select ag.id,AssessmentGroup,PrimarySkills,(Select count(*) from InterviewerAssessments where Assessmentgroupid=ag.id and isApproved=1) as Assessmentcount,ag.Isinstaavailable,agc.CategoryName from AssessmentGroup ag left join AssessmentGroupCategory agc on agc.id=ag.Categoryid";
                    var parameters = new DynamicParameters();
                    var results = con.Query<AssignmentVM>(query, parameters);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<AssignmentVM> InterviewerEntitleDetails(int id)
        {
            try
            {
                var finalquery = "Select ia.id,ia.Overview,ia.Senioritylevel,ia.Difficultylevel,ia.TestedSkill,ag.AssessmentGroup from InterviewerAssessments ia join AssessmentGroup ag on ia.Assessmentgroupid=ag.id Where ia.Assessmentgroupid = @id and ia.isApproved=1";
                var finaldynamicParameters = new DynamicParameters();
                finaldynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<AssignmentVM>(finalquery, finaldynamicParameters);
                    return result;
                }
                //using (SqlConnection con = new SqlConnection(connectionString))
                //{
                //    var parameters = new DynamicParameters();
                //    parameters.Add("@id", id);
                //    return SqlMapper.Query<InterviewerVM>(con, "[dbo].[usp_RisebirdInterviewerEntitleDetails]", parameters,
                //        commandType: StoredProcedure);
                //}
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public string GetCurrentuserImage(string emailid)
        {
            try
            {
                string connectionString = _configuration["ConnectionString:DBConnectionString"];
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var query = "Select ImageURL  from UserList where EmailId = @id";
                    var parameters = new DynamicParameters();
                    parameters.Add("@id", emailid);
                    var results = con.Query<string>(query, parameters);
                    return results.FirstOrDefault();
                }
            }
            catch (Exception)
            {
                return "";
            }
        }
        public string FetchWhatsAppNumberByEmail(string Emailid)
        {
            try
            {
                var query = "Select WhatsappNumber from UserList Where Emailid=@Emailid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Emailid", Emailid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<string>(query, dynamicParameters);
                    return result.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return "";
        }
        public string FetchNameByEmail(string Emailid)
        {
            try
            {
                var query = "Select FirstName from UserList Where Emailid=@Emailid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Emailid", Emailid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<string>(query, dynamicParameters);
                    return result.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return "";
        }
        public int GetInterviewerid(string emailid)
        {
            try
            {
                var query = "Select * from UserList where Emailid=@Emailid";
                DynamicParameters _dp = new DynamicParameters();
                _dp.Add("@Emailid", emailid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<int>(query, _dp);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return 0;
        }
        public string SaveCallRecording(int interviewid, string IRFId, string CandidateEmail, string WhoCalled, string CallSid, string RecordingUrl)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@interviewid", interviewid);
                parameters.Add("@IRFId", IRFId);
                parameters.Add("@RecordingUrl", RecordingUrl);
                parameters.Add("@Sid", CallSid);
                parameters.Add("@CandidateEmail", CandidateEmail);
                parameters.Add("@WhoCalled", WhoCalled);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlMapper.Query<string>(con, "usp_AddorUpdateCallRecording", parameters,
                       commandType: CommandType.StoredProcedure);
                    return "success";
                }
            }
            catch (SqlException e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "Fail";
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "Fail";
            }
        }
        public List<CallRecordingDetailsVM> FetchCallRecording(string candidateEmailid, string irfid)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@IRFId", irfid);
                parameters.Add("@CandidateEmail", candidateEmailid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = SqlMapper.Query<CallRecordingDetailsVM>(con, "usp_FetchCallRecording", parameters,
                       commandType: CommandType.StoredProcedure);
                    return (List<CallRecordingDetailsVM>)result;
                }
            }
            catch (SqlException e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            return null;
        }
        public List<string> GetUserClients(int id)
        {
            try
            {
                var query = "Select Clientid From SeniorHR where Userid=@id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<string>(query, dynamicParameters);
                    return (List<string>)result;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return new List<string>();
        }
        public List<ZoomAccountDetailsVM> FetchInstaZoomAccounts()
        {
            try
            {
                var query = "Select * From InstaZoomMeetingAccounts";
                var dynamicParameters = new DynamicParameters();
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<ZoomAccountDetailsVM>(query, dynamicParameters);
                    return (List<ZoomAccountDetailsVM>)result;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public bool DeleteInstaZoomAccount(int Accountid)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", Accountid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlMapper.Query<int>(con, "usp_DeleteInstaZoomAccount", dynamicParameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
        public bool AddInstaZoomAccount(string AccountEmail)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@AccountEmail", AccountEmail);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlMapper.Query(con, "usp_InsertInstaZoomAccount", dynamicParameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }


        public string GetCurrentUsername(string emailid)
        {
            try
            {
                string connectionString = _configuration["ConnectionString:DBConnectionString"];
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var query = "Select firstname  from UserList where EmailId = @id";
                    var parameters = new DynamicParameters();
                    parameters.Add("@id", emailid);
                    var results = con.Query<string>(query, parameters);
                    return results.FirstOrDefault();
                }
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}
