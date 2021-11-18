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
                        con, "select Account.*, a.TotalProjects from Account left join (select Count(AccountId) as TotalProjects, AccountId from AccountProjectMapping group by AccountId) a on Account.id=a.AccountId where Account.IsDeleted=0", null, commandType: System.Data.CommandType.Text);
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


                    string sqlInsertReport = "INSERT INTO Account (Name,ClientId, EndClient, Location, AccountOwnerId, IsDeleted, CreatedOn ) Values (@Name,@ClientId,@EndClient, @Location,@AccountOwnerId, @IsDeleted, @CreatedOn);";
                    var affectedRows = con.Execute(sqlInsertReport, new
                    {
                        accountDetails.Name,
                        accountDetails.ClientId,
                        accountDetails.EndClient,
                        accountDetails.Location,
                        accountDetails.AccountOwnerId,
                        accountDetails.CreatedOn,
                        IsDeleted = false

                    });
                    var accountList = SqlMapper.QueryAsync<Account>(
                        con, "select * from Account", null, commandType: System.Data.CommandType.Text).Result.AsList();
                    var accountId = accountList.Where(x => x.Name == accountDetails.Name).Last().id;
                    if (accountDetails.AccountProjectMappings != null)
                    {
                        //update techskillreport table
                        foreach (var item in accountDetails.AccountProjectMappings)
                        {
                            string sqlTechReport = "INSERT INTO AccountProjectMapping (AccountId,ProjectId,IsAdded) Values (@AccountId, @ProjectId, @IsAdded);";
                            var result1 = con.ExecuteAsync(sqlTechReport, new
                            {
                                AccountId = accountId,
                                item.ProjectId,
                                item.IsAdded

                            }).Result;
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
                var query = "select * from Account where id=@id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", accountId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<Account>(query, dynamicParameters);
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
                            string sqlQuery = "INSERT INTO AccountProjectMapping (AccountId, ProjectId, IsAdded) Values (@AccountId, @ProjectId, @IsAdded);";
                            var affectedRows = con.ExecuteAsync(sqlQuery, new
                            {
                                AccountId = accountDetails.id,
                                item.ProjectId,
                                item.IsAdded
                            }).Result;
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
                var query = "update Account set IsDeleted=1 where id=@id";
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<int>(query, parameters);
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
                var query = "update Account set IsDeleted=0 where id = @id;";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.QueryAsync<int>(query, dynamicParameters);
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
                string query = string.Empty;
                if (!string.IsNullOrEmpty(projectIdList))
                {
                    query = "Select Project.id as ProjectId,Project.[Name]as ProjectName, Client.[Name] as Client, Client.id as ClientId from Project inner join Client on Project.ClientId = Client.id where Project.ClientId=@ClientId and IsDeleted = @IsDeleted and Project.id not in (" + projectIdList + ")";
                }
                else
                {
                    query = "Select Project.id as ProjectId,Project.[Name]as ProjectName, Client.[Name] as Client, Client.id as ClientId from Project inner join Client on Project.ClientId = Client.id where Project.ClientId=@ClientId and IsDeleted = @IsDeleted";
                }
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IsDeleted", 0);
                dynamicParameters.Add("@ClientId", clientId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<AccountProjectMapping>(query, dynamicParameters);
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
                string query = "Select ul.FirstName,Ul.LastName,ul.MobileNumber,ul.WhatsappNumber,up.Name as CurrentRole,ul.ImageURL from Userlist ul join Profile up on ul.idProfile=up.id where Emailid=@Emailid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Emailid", EmailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<InterviewerProfileVM>(query, dynamicParameters);
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

                string query = "Update Userlist Set FirstName= @FirstName,LastName= @LastName,MobileNumber=@MobileNumber,WhatsappNumber=@WhatsappNumber,ImageURL=IsNull(@ImageURL,ImageURL) where Emailid=@Emailid";
                var parameters = new DynamicParameters();

                parameters.Add("@FirstName", userProfile.FirstName);
                parameters.Add("@LastName", userProfile.LastName);
                parameters.Add("@Emailid", userProfile.EmailId);
                parameters.Add("@MobileNumber", userProfile.MobileNumber);
                parameters.Add("@WhatsappNumber", userProfile.WhatsappNumber);
                parameters.Add("@ImageURL", userProfile.ImageURL);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query(query, parameters);
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
