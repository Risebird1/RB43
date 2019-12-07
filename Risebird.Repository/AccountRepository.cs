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
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    public class AccountRepository : IAccountRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private IApplicationErrorLogger _applicationErrorLogger;

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
        public async Task<bool> AddAccount(Account accountDetails)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {


                    string sqlInsertReport = "INSERT INTO Account (Name,Client, EndClient, Location, AccountOwnerId, IsDeleted, CreatedOn ) Values (@Name,@Client,@EndClient, @Location,@AccountOwnerId, @IsDeleted, @CreatedOn);";
                    var affectedRows = con.Execute(sqlInsertReport, new
                    {
                        Name = accountDetails.Name,
                        Client = accountDetails.Client,
                        EndClient = accountDetails.EndClient,
                        Location = accountDetails.Location,
                        AccountOwnerId = accountDetails.AccountOwnerId,
                        CreatedOn = accountDetails.CreatedOn,
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
                                ProjectId = item.ProjectId,
                                IsAdded = item.IsAdded

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
                var query = "select AccountProjectMapping.*, Account.[Name] as AccountName, Project.[Name] as ProjectName, Project.id as ProjectId, Project.Client from AccountProjectMapping inner join Project on AccountProjectMapping.ProjectId = Project.id left join Account on AccountProjectMapping.AccountId = Account.id where AccountProjectMapping.AccountId = @AccountId and AccountProjectMapping.IsAdded = 1";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@AccountId", accountId);
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
                parameters.Add("@Client", accountDetails.Client);
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
                                ProjectId = item.ProjectId,
                                IsAdded = item.IsAdded
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



    }
}
