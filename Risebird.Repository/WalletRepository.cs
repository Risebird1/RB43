using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper;
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
    public class WalletRepository : IWalletRepository
    {
        private readonly IConfiguration _configuration;
        private string connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public WalletRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<IEnumerable<WalletVM>> GetWallet(int billingType, int? clientId = null, int? billingOwnerId = null, string action = null)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@BillingType", billingType);
                parameters.Add("@ClientId", clientId);
                parameters.Add("@BillingOwnerId", billingOwnerId);
                parameters.Add("@Action", action);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync<WalletVM>(con, "[usp_GetWallet]", parameters,
                        commandType: CommandType.StoredProcedure);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<string> AddWalletCredit(int billingType, int? clientId, int? billingOwnerId, string emailid, int points, string comment, string transactionType = "Manual")
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@BillingType", billingType);
                parameters.Add("@ClientId", clientId);
                parameters.Add("@BillingOwnerId", billingOwnerId);
                parameters.Add("@emailid", emailid);
                parameters.Add("@points", points);
                parameters.Add("@comment", comment);
                parameters.Add("@TransactionType", transactionType);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync(con, "[usp_AddWalletCredit]", parameters,
                        commandType: CommandType.StoredProcedure);
                    return "Success";
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed: " + ex.Message;
            }
        }

        public async Task<string> AddWalletDebit(int billingType, int? clientId, int? billingOwnerId, string emailid, int points, string comment, int? interviewId = null, int? irfId = null, string transactionType = "Manual")
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@BillingType", billingType);
                parameters.Add("@ClientId", clientId);
                parameters.Add("@BillingOwnerId", billingOwnerId);
                parameters.Add("@emailid", emailid);
                parameters.Add("@points", points);
                parameters.Add("@comment", comment);
                parameters.Add("@InterviewId", interviewId);
                parameters.Add("@IRFId", irfId);
                parameters.Add("@TransactionType", transactionType);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync(con, "[usp_AddWalletDebit]", parameters,
                        commandType: CommandType.StoredProcedure);
                    return "Success";
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed: " + ex.Message;
            }
        }

        public async Task<string> DebitWalletByInterview(int interviewId, int? billingType = null, string transactionType = "InterviewBilling")
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@InterviewId", interviewId);
                parameters.Add("@BillingType", billingType);
                parameters.Add("@TransactionType", transactionType);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync(con, "[usp_DebitWalletByInterview]", parameters,
                        commandType: CommandType.StoredProcedure);
                    return "Success";
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed: " + ex.Message;
            }
        }

        public async Task<int> GetWalletBalance(int billingType, int? clientId = null, int? billingOwnerId = null)
        {
            try
            {
                string query = "";
                if (billingType == 1) // Billing Owner
                {
                    query = "SELECT ISNULL(SUM(Credit) - SUM(Debit), 0) as Balance FROM Wallet WHERE BillingType = @BillingType AND BillingOwnerId = @BillingOwnerId";
                    if (clientId.HasValue)
                    {
                        query += " AND ClientId = @ClientId";
                    }
                }
                else if (billingType == 3) // Client
                {
                    query = "SELECT ISNULL(SUM(Credit) - SUM(Debit), 0) as Balance FROM Wallet WHERE BillingType = @BillingType AND ClientId = @ClientId";
                }

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@BillingType", billingType);
                if (clientId.HasValue)
                {
                    parameters.Add("@ClientId", clientId);
                }
                if (billingOwnerId.HasValue)
                {
                    parameters.Add("@BillingOwnerId", billingOwnerId);
                }

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await con.QueryFirstOrDefaultAsync<int>(query, parameters);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return 0;
        }
    }
}

