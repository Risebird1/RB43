using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

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
                        con, "select Account.*, a.TotalProjects from Account join (select Count(AccountId) as TotalProjects, AccountId from AccountProjectMapping group by AccountId) a on Account.id=a.AccountId", null, commandType: System.Data.CommandType.Text);
                }
                return accounts;
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }


    }
}
