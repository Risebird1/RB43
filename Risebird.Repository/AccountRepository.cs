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
using System.Threading.Tasks;
using static System.Data.CommandType;

namespace Risebird.Repository
{
    public class AccountRepository : IAccountRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public AccountRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        public IEnumerable<Account> GetAllAccounts()
        {
            IEnumerable<Account> accounts = new List<Account>();
            try
            {
                //select * from Project where IsDeleted=0
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    accounts = SqlMapper.Query<Account>(
                        con, "usp_GetAllAccounts", null, commandType: CommandType.StoredProcedure);
                }
                return accounts;
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<Account>> GetAccounts(string emailID, string profileName)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@EmailId", emailID);
                    parameters.Add("@Profile", profileName);
                    return await SqlMapper.QueryAsync<Account>(con, "usp_GetAccounts", parameters,
                         commandType: CommandType.StoredProcedure);
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

        public async Task<bool> AddAccount(Account accountDetails)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Name", accountDetails.Name);
                    parameters.Add("@ClientId", accountDetails.ClientId);
                    parameters.Add("@EndClient", accountDetails.EndClient);
                    parameters.Add("@Location", accountDetails.Location);
                    parameters.Add("@AccountOwnerId", accountDetails.AccountOwnerId);
                    parameters.Add("@CreatedOn", accountDetails.CreatedOn);
                    parameters.Add("@IsDeleted", false);
                    parameters.Add("@NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    await con.ExecuteAsync("usp_InsertAccount", parameters, commandType: CommandType.StoredProcedure);
                    var accountId = parameters.Get<int>("@NewId");

                    if (accountDetails.AccountProjectMappings != null)
                    {
                        foreach (var item in accountDetails.AccountProjectMappings)
                        {
                            var mappingParameters = new DynamicParameters();
                            mappingParameters.Add("@AccountId", accountId);
                            mappingParameters.Add("@ProjectId", item.ProjectId);
                            mappingParameters.Add("@IsAdded", item.IsAdded);
                            await con.ExecuteAsync("usp_InsertAccountProjectMapping", mappingParameters,
                                commandType: CommandType.StoredProcedure);
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

        public IEnumerable<AccountProjectMapping> GetAccountCluster(int accountId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@AccountId", accountId);
                    return SqlMapper.Query<AccountProjectMapping>(con, "usp_GetAccountCluster", parameters,
                         commandType: CommandType.StoredProcedure);
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

        public Account GetAccountById(int accountId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", accountId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<Account>("usp_GetAccountById", dynamicParameters,
                        commandType: CommandType.StoredProcedure);
                    return results.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;

            }
        }
        public async Task<bool> UpdateAccount(Account accountDetails)
        {
            try
            {

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", accountDetails.id);
                parameters.Add("@Name", accountDetails.Name);
                parameters.Add("@ClientId", accountDetails.ClientId);
                parameters.Add("@EndClient", accountDetails.EndClient);
                parameters.Add("@Location", accountDetails.Location);
                parameters.Add("@AccountOwnerId", accountDetails.AccountOwnerId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_UpdateAccount", parameters,
                       commandType: CommandType.StoredProcedure);

                    if (accountDetails.AccountProjectMappings != null)
                    {
                        foreach (var item in accountDetails.AccountProjectMappings)
                        {
                            var mappingParameters = new DynamicParameters();
                            mappingParameters.Add("@AccountId", accountDetails.id);
                            mappingParameters.Add("@ProjectId", item.ProjectId);
                            mappingParameters.Add("@IsAdded", item.IsAdded);
                            await con.ExecuteAsync("usp_InsertAccountProjectMapping", mappingParameters,
                                commandType: CommandType.StoredProcedure);
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

        public async Task<bool> ArchiveAccount(int id)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await con.ExecuteAsync("usp_ArchiveAccount", parameters, commandType: CommandType.StoredProcedure);
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

        public async Task<bool> UnArchiveAccount(int id)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await con.ExecuteAsync("usp_UnArchiveAccount", dynamicParameters, commandType: CommandType.StoredProcedure);
                    return true;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }

        }

        public IEnumerable<AccountProjectMapping> GetProjectsbyClientId(int clientId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@ClientId", clientId);
                    return SqlMapper.Query<AccountProjectMapping>(con, "usp_GetProjectbyClientId", parameters,
                         commandType: CommandType.StoredProcedure);
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

        public IEnumerable<AccountProjectMapping> GetProjectByClientId(int clientId, string projectIdList = null)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IsDeleted", 0);
                dynamicParameters.Add("@ClientId", clientId);
                dynamicParameters.Add("@ExcludeProjectIds", projectIdList);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<AccountProjectMapping>("usp_GetProjectsByClientId", dynamicParameters,
                        commandType: CommandType.StoredProcedure);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public string Signout(string EmailId, string sessionId)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@EmailId", EmailId);
                parameters.Add("@SessionId", sessionId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlMapper.Query<string>(con, "[dbo].[sp_UpdateSingoutDetails]", parameters,
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
            return "";
        }


        public InterviewerProfileVM FetchUserProfile(string EmailId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Emailid", EmailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<InterviewerProfileVM>("usp_FetchUserProfile", dynamicParameters,
                        commandType: CommandType.StoredProcedure);
                    return (InterviewerProfileVM)results.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<bool> UpdateUserProfile(InterviewerProfileVM userProfile)
        {
            try
            {

                // In this case, Only passing the required parameters to fil in the tables

                var parameters = new DynamicParameters();
                parameters.Add("@FirstName", userProfile.FirstName);
                parameters.Add("@LastName", userProfile.LastName);
                parameters.Add("@Emailid", userProfile.EmailId);
                parameters.Add("@MobileNumber", userProfile.MobileNumber);
                parameters.Add("@WhatsappNumber", userProfile.WhatsappNumber);
                parameters.Add("@ImageURL", userProfile.ImageURL);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await con.ExecuteAsync("usp_UpdateUserProfile", parameters, commandType: CommandType.StoredProcedure);
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
    }
}
