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

        public async Task<IEnumerable<WalletVM>> GetWallet(int billingType, int? clientId = null, int? billingOwnerId = null, string action = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@BillingType", billingType);
                parameters.Add("@ClientId", clientId);
                parameters.Add("@BillingOwnerId", billingOwnerId);
                parameters.Add("@Action", action);
                parameters.Add("@StartDate", startDate);
                parameters.Add("@EndDate", endDate);

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
        public async Task<bool> AddClientWalletBalance(int billingType, int clientId, int CreditAmount, string loginedUser, DateTime? CreatedOn, int? billingOwnerId = null, string transactionRefNumber = null, string invoicePDF = null, DateTime? transactionDate = null, string transactionType = "Manual", int? clientPOId = null)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@ClientId", clientId);
                parameters.Add("@BillingType", billingType);
                parameters.Add("@BillingOwnerId", billingOwnerId);
                parameters.Add("@CreatedBy", loginedUser);
                parameters.Add("@TransactionType", transactionType);
                parameters.Add("@Credit", CreditAmount);
                parameters.Add("@CreatedOn", DateTime.Now);
                parameters.Add("@TransactionDate", transactionDate);
                parameters.Add("@TransactionRefNumber", transactionRefNumber);
                parameters.Add("@InvoicePDF", invoicePDF);
                parameters.Add("@ClientPOId", clientPOId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_AddCreditWalletDetails", parameters,
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

        public async Task<int> SaveClientPO(int clientId, int billingType, int billingModel, decimal poCreditAmount, int poMonth, int poYear, string createdBy, int? billingOwnerId = null, string poNumber = null, string poDocumentUrl = null, string remarks = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@ClientId", clientId);
                    parameters.Add("@BillingType", billingType);
                    parameters.Add("@BillingOwnerId", billingOwnerId);
                    parameters.Add("@BillingModel", billingModel);
                    parameters.Add("@PONumber", poNumber);
                    parameters.Add("@POCreditAmount", poCreditAmount);
                    parameters.Add("@POMonth", poMonth);
                    parameters.Add("@POYear", poYear);
                    parameters.Add("@PODocumentUrl", poDocumentUrl);
                    parameters.Add("@CreatedBy", createdBy);
                    parameters.Add("@Remarks", remarks);
                    var result = await SqlMapper.QueryAsync<int>(con, "usp_InsertClientPO", parameters, commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return 0;
        }

        public async Task<List<ClientPOVM>> GetClientPOList(int clientId, int? month = null, int? year = null, int? billingOwnerId = null, bool openOnly = false)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@ClientId", clientId);
                    parameters.Add("@Month", month);
                    parameters.Add("@Year", year);
                    parameters.Add("@BillingOwnerId", billingOwnerId);
                    parameters.Add("@OpenOnly", openOnly);
                    var result = await SqlMapper.QueryAsync<ClientPOVM>(con, "usp_GetClientPOList", parameters, commandType: CommandType.StoredProcedure);
                    return result.ToList();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return new List<ClientPOVM>();
        }

        public async Task<List<WalletVM>> GetCashCreditHistory(int clientId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@ClientId", clientId);
                    var result = await SqlMapper.QueryAsync<WalletVM>(con, "usp_GetClientCashCreditHistory", parameters, commandType: CommandType.StoredProcedure);
                    return result.ToList();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return new List<WalletVM>();
        }

        public async Task<bool> UpdatePOStatus(int poId, int clientId, int status)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@POId", poId);
                    parameters.Add("@ClientId", clientId);
                    parameters.Add("@Status", status);
                    parameters.Add("@ModifiedOn", DateTime.UtcNow);
                    var affected = await con.ExecuteAsync(
                        "UPDATE ClientPO SET Status = @Status, ModifiedOn = @ModifiedOn WHERE id = @POId AND ClientId = @ClientId",
                        parameters);
                    return affected > 0;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }

        public async Task<ClientWalletDashboardVM> GetClientWalletDashboard(int clientId, int month, int year, bool allDetails = false)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@ClientId", clientId);
                    parameters.Add("@Month", month);
                    parameters.Add("@Year", year);
                    parameters.Add("@DetailsScope", allDetails);
                    using (var multi = await con.QueryMultipleAsync("usp_GetClientWalletDashboard", parameters, commandType: CommandType.StoredProcedure))
                    {
                        var summary = (await multi.ReadAsync<ClientWalletDashboardVM>()).FirstOrDefault();
                        if (summary == null)
                        {
                            return new ClientWalletDashboardVM { ClientId = clientId, SelectedMonth = month, SelectedYear = year };
                        }
                        summary.SelectedMonth = month;
                        summary.SelectedYear = year;
                        summary.OwnerWallets = (await multi.ReadAsync<OwnerWalletRowVM>()).ToList();
                        summary.PORecords = (await multi.ReadAsync<ClientPOVM>()).ToList();
                        return summary;
                    }
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new ClientWalletDashboardVM { ClientId = clientId, SelectedMonth = month, SelectedYear = year };
            }
        }

        public async Task<AllClientsWalletVM> GetAllClientsWalletSummary(int month, int year)
        {
            var vm = new AllClientsWalletVM { SelectedMonth = month, SelectedYear = year };
            try
            {
                using var con = new SqlConnection(connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@Month", month);
                parameters.Add("@Year",  year);

                using var multi = await con.QueryMultipleAsync(
                    "usp_GetAllClientsWalletSummary",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                vm.Clients            = (await multi.ReadAsync<ClientWalletSummaryRowVM>()).ToList();
                vm.BillingOwnerCounts = (await multi.ReadAsync<BillingOwnerInterviewCountVM>()).ToList();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return vm;
        }
    }
}

