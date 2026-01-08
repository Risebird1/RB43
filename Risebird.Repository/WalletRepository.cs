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

        public async Task<int> GetWalletBalance(int billingType, int? clientId = null, int? billingOwnerId = null)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@BillingType", billingType);
                parameters.Add("@ClientId", clientId);
                parameters.Add("@BillingOwnerId", billingOwnerId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync<int>(con, "Usp_GetClientWalletBalance", parameters,
                        commandType: CommandType.StoredProcedure);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return 0;
        }
        public async Task<bool> AddClientWalletBalance(int billingType, int clientId, int CreditAmount, string loginedUser, DateTime? CreatedOn, int? billingOwnerId = null, string transactionId = null, string invoicePDF = null)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@ClientId", clientId);
                parameters.Add("@BillingType", billingType);
                parameters.Add("@BillingOwnerId", billingOwnerId);
                parameters.Add("@CreatedBy", loginedUser);
                parameters.Add("@TransactionType", "Manual");
                parameters.Add("@Credit", CreditAmount);
                parameters.Add("@CreatedOn", CreatedOn);
                parameters.Add("@TransactionId", transactionId);
                parameters.Add("@InvoicePDF", invoicePDF);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync<int>(con, "usp_AddCreditWalletDetails", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
    }
}

