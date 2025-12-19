using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
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

