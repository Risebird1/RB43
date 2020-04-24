using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using static System.Data.CommandType;
using System.Threading.Tasks;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;

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
                    await SqlMapper.QueryAsync<int>(con, "usp_AddUser", parameters,
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
                parameters.Add("@id", entity.Id);
            //if (entity.IdProfile.HasValue)

            parameters.Add("@selectedProfileId", entity.IdProfile);
            parameters.Add("@userFirstName", entity.FirstName);
            parameters.Add("@userLastName", entity.LastName);
            parameters.Add("@userEmailId", entity.EmailId);
            parameters.Add("@userMobileNumber", entity.MobileNumber);

            return parameters;
        }

        #endregion

        #region Client User Repository Function

        /// <summary>
        /// Get all the Client Users
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ClientUserVM> GetClientUsers()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<ClientUserVM>(con, "[dbo].[sp_RisebirdClientUser]", null,
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

        public ClientUserVM GetClientUserDetails(string emailID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("Email", emailID);
                    return SqlMapper.Query<ClientUserVM>(con, "sp_ClientUserDetail", parameters,
                         commandType: CommandType.StoredProcedure).FirstOrDefault();
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

        public async Task<bool> AddClientUser(ClientUserVM client)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = SetStoredProcedureParameterForClientUser(client);

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
                clientUser.SelectedClient = null;
                clientUser.SelectedClientRole = null;
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

        private DynamicParameters SetStoredProcedureParameterForClientUser(ClientUserVM entity)
        {
            var parameters = new DynamicParameters();

            if (entity.id > 0)
                parameters.Add("@id", entity.id);
            if (entity.SelectedClient.HasValue)
                parameters.Add("@selectedClientId", entity.SelectedClient);
            if (entity.SelectedClientRole.HasValue)
                parameters.Add("@selectedClientRoleId", entity.SelectedClientRole);

            parameters.Add("@userFirstName", entity.FirstName);
            parameters.Add("@userLastName", entity.LastName);
            parameters.Add("@userEmailId", entity.EmailId);
            parameters.Add("@userMobileNumber", entity.MobileNumber);
            parameters.Add("@message", SqlDbType.VarChar, direction: ParameterDirection.Output);

            return parameters;
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
            parameters.Add("@slots", entity.Slots);
            parameters.Add("@userFirstName", entity.FirstName);
            parameters.Add("@userLastName", entity.LastName);
            parameters.Add("@userEmailId", entity.EmailId);
            parameters.Add("@userMobileNumber", entity.MobileNumber);
            parameters.Add("@isApproved", entity.IsApproved);

            return parameters;
        }

        #endregion

        #region Interviewer Profile Repository function

        public async Task<bool> AddOrEditInterviewerProfile(InterviewerProfileVM interviewerProfile)
        {
            try
            {
                int ProfileId;

                DynamicParameters parameters = SetSPParametersForInterviewProfile(interviewerProfile);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    //Just added to get Id this way
                    //var result = await SqlMapper.QueryAsync<int>(con, "usp_AddOrEditInterviewerProfile", parameters,
                    //   commandType: CommandType.StoredProcedure);
                    //int ProfileId = result.FirstOrDefault();

                    await SqlMapper.QueryAsync<int>(con, "usp_AddOrEditInterviewerProfile", parameters,
                       commandType: CommandType.StoredProcedure);

                    var ProfileList = SqlMapper.QueryAsync<InterviewerProfileVM>(
                        con, "select id, NickName, userId from InterviewerProfile orderby ", null, commandType: Text).Result.AsList();
                    ProfileId = ProfileList.Where(x => x.NickName == interviewerProfile.NickName).Last().id;//irfList.Find(x => x.IRFId == irfDetails.IRFId).id;
                }

                //To Add Skills
                foreach (var item in interviewerProfile.InterviewerSkill)
                {
                    DynamicParameters Params = new DynamicParameters();
                    Params.Add("@InterviewerProfileId", ProfileId);
                    Params.Add("@SkillId", item.SkillId);
                    Params.Add("@Category", item.Category);
                    Params.Add("@Ip", item.Ip);
                    Params.Add("@Level", item.Level);
                    Params.Add("@SkillExperience", item.SkillExperience);
                    Params.Add("@Nature", item.Nature);

                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        await SqlMapper.QueryAsync<int>(con, "usp_AddInterviewerSkills", Params,
                       commandType: CommandType.StoredProcedure);

                    }
                }

                foreach (var item in interviewerProfile.InterviewerCertificateDetails)
                {
                    DynamicParameters Params = new DynamicParameters();
                    Params.Add("@InterviewerProfileId", ProfileId);
                    Params.Add("@CertificateID", item.CertificateID);
                    Params.Add("@CertificateName", item.CertificateName);
                    Params.Add("@Version", item.Version);
                    Params.Add("@ExpiryDate", item.Expirydate);

                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        await SqlMapper.QueryAsync<int>(con, "usp_AddInterviewerCertificate", Params,
                       commandType: CommandType.StoredProcedure);

                    }
                }

                foreach (var item in interviewerProfile.InterviewerProfessionalDetails)
                {
                    DynamicParameters Params = new DynamicParameters();
                    Params.Add("@InterviewerProfileId", ProfileId);
                    Params.Add("@CompanyName", item.CompanyName);
                    Params.Add("@Type", item.Type);
                    Params.Add("@CompanyStage", item.CompanyStage);
                    Params.Add("@Size", item.Size);
                    Params.Add("@JobFunction", item.JobFunction);
                    Params.Add("@Industry", item.Industry);
                    Params.Add("@Headquarters", item.Headquarters);
                    Params.Add("@Role", item.Role);
                    Params.Add("@Domain", item.Domain);
                    Params.Add("@SeniorityLevel", item.SeniorityLevel);
                    Params.Add("@Experience", item.Experience);
                    Params.Add("@WorkNature", item.WorkNature);
                    Params.Add("@PrimaryResponsibility", item.PrimaryResponsibility);
                    Params.Add("@CompanyLinkedinPage", item.CompanyLinkedinPage);
                    Params.Add("@CompanyWebsite", item.CompanyWebsite);

                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        await SqlMapper.QueryAsync<int>(con, "usp_AddInterviewerProfessional", Params,
                       commandType: CommandType.StoredProcedure);

                    }
                }

                foreach (var item in interviewerProfile.InterviewerEducationDetails)
                {
                    DynamicParameters Params = new DynamicParameters();
                    Params.Add("@InterviewerProfileId", ProfileId);
                    Params.Add("@EducationType", item.EducationType);
                    Params.Add("@Degree", item.Degree);
                    Params.Add("@EducationMode", item.EducationMode);
                    Params.Add("@CollegeType", item.CollegeType);
                    Params.Add("@UniversityName", item.UniversityName);
                    Params.Add("@PassOutYear", item.PassOutYear);
                    Params.Add("@AnyGap", item.AnyGap);

                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        await SqlMapper.QueryAsync<int>(con, "TransInterviewerEducation", Params,
                       commandType: CommandType.StoredProcedure);

                    }
                }

                foreach (var item in interviewerProfile.InterviewerProjectsDetails)
                {
                    DynamicParameters Params = new DynamicParameters();
                    Params.Add("@InterviewerProfileId", ProfileId);
                    Params.Add("@TechnologyEnvironment", item.TechnologyEnvironment);
                    Params.Add("@IsPrimarySkillUsed", item.IsPrimarySkillUsed);
                    Params.Add("@Description", item.Description);
                    Params.Add("@IsCommercialProject", item.IsCommercialProject);
                    Params.Add("@NatureOfProject", item.NatureOfProject);

                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        await SqlMapper.QueryAsync<int>(con, "usp_AddInterviewerProjects", Params,
                       commandType: CommandType.StoredProcedure);

                    }
                }

                foreach (var item in interviewerProfile.InterviewerAvailabilityDetails)
                {
                    DynamicParameters Params = new DynamicParameters();
                    Params.Add("@InterviewerProfileId", ProfileId);
                    Params.Add("@Weekday", item.Weekday);
                    Params.Add("@Weekend", item.Weekend);
                    Params.Add("@IsAvailableOnPublicHolidays", item.IsAvailableOnPublicHolidays);

                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        await SqlMapper.QueryAsync<int>(con, "usp_AddInterviewerAvailability", Params,
                       commandType: CommandType.StoredProcedure);

                    }
                }

                foreach (var item in interviewerProfile.InterviewerAccDetails)
                {
                    DynamicParameters Params = new DynamicParameters();
                    Params.Add("@InterviewerProfileId", ProfileId);
                    Params.Add("@AccountName", item.AccountNumber);
                    Params.Add("@AccountNumber", item.AccountNumber);
                    Params.Add("@IFSCCode", item.IFSCCode);
                    Params.Add("@UPIId", item.UPIId);
                    Params.Add("@PAN", item.PAN);

                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        await SqlMapper.QueryAsync<int>(con, "usp_AddInterviewerAccDetails", Params,
                       commandType: CommandType.StoredProcedure);

                    }
                }

                return true;

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

        private DynamicParameters SetSPParametersForInterviewProfile(InterviewerProfileVM entity)
        {
            var parameters = new DynamicParameters();

            parameters.Add("@UserId", entity.UserId);
            parameters.Add("@FirstName", entity.FirstName);
            parameters.Add("@LastName", entity.LastName);
            parameters.Add("@NickName", entity.NickName);
            parameters.Add("@EmailId", entity.EmailId);
            parameters.Add("@AltEmailId", entity.AltEmailId);
            parameters.Add("@DOB", entity.DOB);
            parameters.Add("@Gender", entity.Gender);
            parameters.Add("@AnnualSalary", entity.AnnualSalary);
            parameters.Add("@Country", entity.Country);
            parameters.Add("@City", entity.City);
            parameters.Add("@MobileNumber", entity.MobileNumber);
            parameters.Add("@AltMobileNumber", entity.AltMobileNumber);
            parameters.Add("@CurrentRole", entity.CurrentRole);
            parameters.Add("@Experience", entity.Experience);
            parameters.Add("@PreferredDomain", entity.PreferredDomain);
            parameters.Add("@GitHubLink", entity.GitHubLink);
            parameters.Add("@ResumeLink", entity.ResumeLink);
            parameters.Add("@CodedResumeLink", entity.CodedResumeLink);
            parameters.Add("@StackOverflowLink", entity.StackOverflowLink);
            parameters.Add("@LinkedinLink", entity.LinkedinLink);
            parameters.Add("@Blog", entity.Blog);
            parameters.Add("@IsActive", entity.IsActive);
            parameters.Add("@IsApproved", entity.IsApproved);
            parameters.Add("@IsLeadInterviewer", entity.IsLeadInterviewer);
            parameters.Add("@InterviewerProfileId", SqlDbType.Int, direction: ParameterDirection.Output);

            return parameters;
        }
        private DynamicParameters SetSPParametersForSkills(InterviewerProfileVM entity)
        {
            var parameters = new DynamicParameters();

            parameters.Add("@UserId", entity.UserId);
            parameters.Add("@FirstName", entity.FirstName);
            parameters.Add("@LastName", entity.LastName);
            parameters.Add("@NickName", entity.NickName);
            parameters.Add("@EmailId", entity.EmailId);
            parameters.Add("@AltEmailId", entity.AltEmailId);
            parameters.Add("@DOB", entity.DOB);
            parameters.Add("@Gender", entity.Gender);
            parameters.Add("@AnnualSalary", entity.AnnualSalary);
            parameters.Add("@Country", entity.Country);
            parameters.Add("@City", entity.City);
            parameters.Add("@MobileNumber", entity.MobileNumber);
            parameters.Add("@AltMobileNumber", entity.AltMobileNumber);
            parameters.Add("@CurrentRole", entity.CurrentRole);
            parameters.Add("@Experience", entity.Experience);
            parameters.Add("@PreferredDomain", entity.PreferredDomain);
            parameters.Add("@GitHubLink", entity.GitHubLink);
            parameters.Add("@ResumeLink", entity.ResumeLink);
            parameters.Add("@CodedResumeLink", entity.CodedResumeLink);
            parameters.Add("@StackOverflowLink", entity.StackOverflowLink);
            parameters.Add("@LinkedinLink", entity.LinkedinLink);
            parameters.Add("@Blog", entity.Blog);
            parameters.Add("@IsActive", entity.IsActive);
            parameters.Add("@IsApproved", entity.IsApproved);
            parameters.Add("@IsLeadInterviewer", entity.IsLeadInterviewer);
            parameters.Add("@InterviewerProfileId", SqlDbType.Int, direction: ParameterDirection.Output);

            return parameters;
        }

        //Get NickNames
        public IEnumerable<string> GetNickNames(int UserId)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@UserId", UserId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<string>(
                        con, "select NickName from [dbo].[InterviewerProfile] where UserId != @UserId", parameters, commandType: Text);
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

        //Get NickNames
        public IEnumerable<string> GetEmailIds(int UserId)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@UserId", UserId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<string>(
                        con, "select EmailId from [dbo].[InterviewerProfile] where UserId != @UserId", parameters, commandType: Text);
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

        public IEnumerable<LevelTech> GetLevelTech()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<LevelTech>(con, "Select Id, Name, Description from LevelTech",
                        null, commandType: Text);
                }
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<IEnumerable<InterviewerProfileVM>> GetInterviewerProfileById(int userid)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@userid", userid);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<InterviewerProfileVM>(con, "usp_GetInterviewerProfiletByUserId",
                        parameters, commandType: CommandType.StoredProcedure);
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

        public async Task<IEnumerable<TransInterviewerSkill>> GetSkills(int ProfileId)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Profileid", ProfileId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<TransInterviewerSkill>(
                        con, "select Id, InterviewerProfileId,  SkillId, Level, SkillName from [dbo].[TransInterviewerSkill] where InterviewerProfileId = @ProfileId",
                        parameters, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        //certi
        public async Task<IEnumerable<TransInterviewerCertificate>> GetCertificateList(int ProfileId)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Profileid", ProfileId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<TransInterviewerCertificate>(
                        con, "select Id, InterviewerProfileId,  CertificateID, CertificateName from [dbo].[TransInterviewerCertificate] where InterviewerProfileId = @ProfileId",
                        parameters, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        //Education
        public async Task<IEnumerable<TransInterviewerEducation>> GetEducationList(int ProfileId)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Profileid", ProfileId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<TransInterviewerEducation>(
                        con, "select Id, InterviewerProfileId,  Graduation, CollegeType, Specialization, Score from [dbo].[TransInterviewerEducation] where InterviewerProfileId = @ProfileId",
                        parameters, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        // Profess
        public async Task<IEnumerable<TransInterviewerProfessional>> GetProfessionalList(int ProfileId)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Profileid", ProfileId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<TransInterviewerProfessional>(
                        con, "select Id, InterviewerProfileId,  CompanyName, CompanyType, Domain, StartDate, EndDate from [dbo].[TransInterviewerProfessional] where InterviewerProfileId = @ProfileId",
                        parameters, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        //Project
        public async Task<IEnumerable<TransInterviewerProjects>> GetProjectsList(int ProfileId)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Profileid", ProfileId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<TransInterviewerProjects>(
                        con, "select Id, InterviewerProfileId,  ProjectName, ClientName, StartDate, EndDate, Role, ProjectDescriptions from [dbo].[TransInterviewerProjects] where InterviewerProfileId = @ProfileId",
                        parameters, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        //Availability
        public async Task<IEnumerable<TransInterviewerAvailability>> GetAvailabilityList(int ProfileId)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Profileid", ProfileId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<TransInterviewerAvailability>(
                        con, "select Id, InterviewerProfileId,  DayTimeSlot, DaySlot, StartTime, EndTime from [dbo].[TransInterviewerAvailability] where InterviewerProfileId = @ProfileId",
                        parameters, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        //AccDetails
        public async Task<IEnumerable<TransInterviewerAccDetails>> GetAccDetailsList(int ProfileId)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Profileid", ProfileId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<TransInterviewerAccDetails>(
                        con, "select Id, InterviewerProfileId,  AccountHolderName, AccountNumber, IFSCCode, Location, UPIId from [dbo].[TransInterviewerAccDetails] where InterviewerProfileId = @ProfileId",
                        parameters, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

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
        public IEnumerable<Menu> GetMenuBasedOnUserEmailld(string emailId)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@EmailId", emailId); // Normal parametersmeters 

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
                parameters.Add("@EmailId", resumeEvaluator.EmailId);
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
                parameters.Add("@EmailId", recruiter.EmailId);
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



    }
}
