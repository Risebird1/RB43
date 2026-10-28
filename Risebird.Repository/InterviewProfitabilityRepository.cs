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
    public class InterviewProfitabilityRepository : IInterviewProfitabilityRepository
    {
        // The report reads payouts, AI logs and call logs for every interview in the window.
        private const int ReportCommandTimeoutSeconds = 180;

        private readonly string connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public InterviewProfitabilityRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<InterviewProfitabilityRawData> GetRawDataAsync(
            DateTime startDate,
            DateTime endDate,
            int? clientId,
            int? irfIntId,
            string interviewerEmail,
            bool? isAIInterview,
            bool includeCancelled)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@StartDate", startDate.Date, DbType.Date);
                parameters.Add("@EndDate", endDate.Date, DbType.Date);
                parameters.Add("@ClientId", clientId);
                parameters.Add("@IRFIntId", irfIntId);
                parameters.Add("@InterviewerEmail", string.IsNullOrWhiteSpace(interviewerEmail) ? null : interviewerEmail.Trim());
                parameters.Add("@IsAIInterview", isAIInterview);
                parameters.Add("@IncludeCancelled", includeCancelled);

                using var con = new SqlConnection(connectionString);
                using var multi = await con.QueryMultipleAsync(
                    "usp_GetInterviewProfitability", parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: ReportCommandTimeoutSeconds);

                return new InterviewProfitabilityRawData
                {
                    Meetings = (await multi.ReadAsync<InterviewProfitabilityMeetingRow>()).ToList(),
                    IrfPools = (await multi.ReadAsync<InterviewProfitabilityIrfPoolRow>()).ToList(),
                    Days = (await multi.ReadAsync<InterviewProfitabilityDayRow>()).ToList(),
                    CostFactors = (await multi.ReadAsync<OperationalCostFactor>()).ToList(),
                    // Result 5 is absent when the database still has the previous version of the procedure.
                    MissingCostTables = multi.IsConsumed ? new List<string>() : (await multi.ReadAsync<string>()).ToList(),
                    UnpricedAICalls = multi.IsConsumed ? 0 : (await multi.ReadAsync<int>()).FirstOrDefault()
                };
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new InterviewProfitabilityRawData { LoadError = DescribeLoadError(ex) };
            }
        }

        /// <summary>
        /// What went wrong, in words an admin can act on. The page is limited to Admin/Finance, so the
        /// database's own message is included; the full exception is in the application log.
        /// </summary>
        internal static string DescribeLoadError(Exception ex)
        {
            if (ex is SqlException sql)
            {
                switch (sql.Number)
                {
                    case 208 when !sql.Message.Contains("OperationalCostFactor"):
                        // Invalid object name: a table the procedure reads is not on this database.
                        return "A table the report reads is missing on this database (" + sql.Message.TrimEnd('.') +
                               "). Re-run the latest usp_GetInterviewProfitability.sql: it skips call-log tables that are not installed.";
                    case 208:  // the report's own OperationalCostFactor table
                    case 2812: // Could not find stored procedure
                        return "The Profitability report is not installed on this database yet (" + sql.Message.TrimEnd('.') +
                               "). Run the Profitability SQL scripts: Tables/OperationalCostFactor.sql, " +
                               "Indexes/IX_InterviewProfitability.sql, the usp_*InterviewProfitability* and " +
                               "usp_*OperationalCostFactor* procedures, then Scripts/Seed_OperationalCostFactors.sql.";
                    case -2: // Command timeout
                        return "The report took longer than " + (ReportCommandTimeoutSeconds / 60) + " minutes and was stopped. " +
                               "Try a shorter date range or a single client, and make sure Indexes/IX_InterviewProfitability.sql has been run.";
                    case 207: // Invalid column name
                        return "The database does not match what the report expects (" + sql.Message.TrimEnd('.') +
                               "). Re-run the latest usp_GetInterviewProfitability.sql.";
                }

                return "The report could not be loaded: " + sql.Message;
            }

            return "The report could not be loaded (" + ex.GetType().Name + "). The error has been logged.";
        }

        public async Task<InterviewProfitabilityFilterOptions> GetFilterOptionsAsync(int? clientId)
        {
            try
            {
                using var con = new SqlConnection(connectionString);
                using var multi = await con.QueryMultipleAsync(
                    "usp_GetInterviewProfitabilityFilterOptions", new { ClientId = clientId },
                    commandType: CommandType.StoredProcedure);

                return new InterviewProfitabilityFilterOptions
                {
                    Clients = (await multi.ReadAsync<InterviewProfitabilityOption>()).ToList(),
                    IRFs = (await multi.ReadAsync<InterviewProfitabilityOption>()).ToList()
                };
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new InterviewProfitabilityFilterOptions();
            }
        }

        public async Task<List<OperationalCostFactor>> GetCostFactorsAsync()
        {
            try
            {
                using var con = new SqlConnection(connectionString);
                var rows = await con.QueryAsync<OperationalCostFactor>(
                    "usp_GetOperationalCostFactors", commandType: CommandType.StoredProcedure);
                return rows.ToList();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<OperationalCostFactor>();
            }
        }

        public async Task<int> SaveCostFactorAsync(OperationalCostFactor factor, string changedBy)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Id", factor.Id);
                parameters.Add("@Name", factor.Name);
                parameters.Add("@Category", factor.Category);
                parameters.Add("@Amount", factor.Amount);
                parameters.Add("@Currency", factor.Currency);
                parameters.Add("@Basis", factor.Basis);
                parameters.Add("@EffectiveFrom", factor.EffectiveFrom.Date, DbType.Date);
                parameters.Add("@EffectiveTo", factor.EffectiveTo?.Date, DbType.Date);
                parameters.Add("@IsActive", factor.IsActive);
                parameters.Add("@Notes", factor.Notes);
                parameters.Add("@ChangedBy", changedBy);

                using var con = new SqlConnection(connectionString);
                return await con.ExecuteScalarAsync<int>(
                    "usp_SaveOperationalCostFactor", parameters, commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }
    }
}
