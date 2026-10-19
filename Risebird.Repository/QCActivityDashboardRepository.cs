using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    /// <summary>
    /// QC Activity dashboard data access. Both procedures read usp_QCActivity_CollectRows,
    /// so a summary number and its drill-down always agree. Errors propagate to the service,
    /// which logs them and returns an error message to the page.
    /// </summary>
    public class QCActivityDashboardRepository : IQCActivityDashboardRepository
    {
        private const int CommandTimeoutSeconds = 120;

        private readonly string _connectionString;

        public QCActivityDashboardRepository(IConfiguration configuration)
        {
            _connectionString = configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<(IReadOnlyList<QCActivitySummaryRowVM> Rows, QCActivityBacklogVM Backlog)> GetSummaryAsync(
            DateTime fromIst, DateTime toIst, bool onlyQualityController, string actorEmail = null,
            CancellationToken cancellationToken = default)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@FromIst", fromIst.Date, DbType.Date);
            parameters.Add("@ToIst", toIst.Date, DbType.Date);
            parameters.Add("@OnlyQualityController", onlyQualityController);
            parameters.Add("@ActorEmail", string.IsNullOrWhiteSpace(actorEmail) ? null : actorEmail.Trim(), DbType.AnsiString, size: 150);

            using var con = new SqlConnection(_connectionString);
            // The token cancels the SQL command too: a request the page abandoned (a newer Apply, or the tab
            // closed) stops using the database instead of running on and slowing down the next load.
            using var multi = await con.QueryMultipleAsync(new CommandDefinition(
                "dbo.usp_GetQCActivityDashboardSummary",
                parameters,
                commandTimeout: CommandTimeoutSeconds,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

            var rows = (await multi.ReadAsync<QCActivitySummaryRowVM>()).ToList();
            var backlog = multi.IsConsumed
                ? new QCActivityBacklogVM()
                : (await multi.ReadFirstOrDefaultAsync<QCActivityBacklogVM>()) ?? new QCActivityBacklogVM();

            // Result sets 3 and 4 (high-risk coverage); absent on an older procedure.
            if (!multi.IsConsumed)
            {
                var coverage = (await multi.ReadFirstOrDefaultAsync<QCActivityCoverageVM>()) ?? new QCActivityCoverageVM();
                if (!multi.IsConsumed)
                    coverage.Untouched = (await multi.ReadAsync<QCActivityUntouchedVM>()).ToList();
                backlog.Coverage = coverage;
            }

            return (rows, backlog);
        }

        public async Task<IReadOnlyList<QCActivityDetailRowVM>> GetDetailsAsync(
            string actorEmail, string metric, DateTime fromIst, DateTime toIst)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@ActorEmail", actorEmail?.Trim(), DbType.AnsiString, size: 150);
            parameters.Add("@Metric", metric, DbType.AnsiString, size: 40);
            parameters.Add("@FromIst", fromIst.Date, DbType.Date);
            parameters.Add("@ToIst", toIst.Date, DbType.Date);

            using var con = new SqlConnection(_connectionString);
            var rows = await con.QueryAsync<QCActivityDetailRowVM>(
                "dbo.usp_GetQCActivityDashboardDetails",
                parameters,
                commandTimeout: CommandTimeoutSeconds,
                commandType: CommandType.StoredProcedure);

            return rows.ToList();
        }

        public async Task<IReadOnlyList<InterviewerQCRowVM>> GetInterviewerViewAsync(
            DateTime fromIst, DateTime toIst, string search = null, string interviewerEmail = null,
            int minInterviews = InterviewerQCViewLimits.DefaultMinInterviews, bool includeOpenFlags = false)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@FromIst", fromIst.Date, DbType.Date);
            parameters.Add("@ToIst", toIst.Date, DbType.Date);
            parameters.Add("@Search", string.IsNullOrWhiteSpace(search) ? null : search.Trim(), DbType.String, size: 150);
            parameters.Add("@InterviewerEmail", string.IsNullOrWhiteSpace(interviewerEmail) ? null : interviewerEmail.Trim(), DbType.AnsiString, size: 150);
            parameters.Add("@MinInterviews", minInterviews, DbType.Int32);
            parameters.Add("@IncludeOpenFlags", includeOpenFlags, DbType.Boolean);

            using var con = new SqlConnection(_connectionString);
            var rows = await con.QueryAsync<InterviewerQCRowVM>(
                "dbo.usp_GetInterviewerQCView",
                parameters,
                commandTimeout: CommandTimeoutSeconds,
                commandType: CommandType.StoredProcedure);

            return rows.ToList();
        }

        public async Task<IReadOnlyList<QCActivityDiagnosticRowVM>> GetDiagnosticsAsync(DateTime fromIst, DateTime toIst)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@FromIst", fromIst.Date, DbType.Date);
            parameters.Add("@ToIst", toIst.Date, DbType.Date);

            using var con = new SqlConnection(_connectionString);
            var rows = await con.QueryAsync<QCActivityDiagnosticRowVM>(
                "dbo.usp_QCActivity_Diagnostics",
                parameters,
                commandTimeout: CommandTimeoutSeconds,
                commandType: CommandType.StoredProcedure);

            return rows.ToList();
        }

        public async Task<(IReadOnlyList<InterviewerQCIrfVM> Irfs, IReadOnlyList<InterviewerQCEventVM> Events)> GetInterviewerTimelineAsync(
            string interviewerEmail, DateTime fromIst, DateTime toIst)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@InterviewerEmail", interviewerEmail?.Trim(), DbType.AnsiString, size: 150);
            parameters.Add("@FromIst", fromIst.Date, DbType.Date);
            parameters.Add("@ToIst", toIst.Date, DbType.Date);

            using var con = new SqlConnection(_connectionString);
            using var multi = await con.QueryMultipleAsync(
                "dbo.usp_GetInterviewerQCTimeline",
                parameters,
                commandTimeout: CommandTimeoutSeconds,
                commandType: CommandType.StoredProcedure);

            var irfs = (await multi.ReadAsync<InterviewerQCIrfVM>()).ToList();
            var events = multi.IsConsumed
                ? new List<InterviewerQCEventVM>()
                : (await multi.ReadAsync<InterviewerQCEventVM>()).ToList();
            return (irfs, events);
        }

        public async Task<(IReadOnlyList<QCActivityFlagVM> Flags, IReadOnlyList<QCActivityFlagItemVM> Items, IReadOnlyList<QCActivitySettingRowVM> Settings)> GetFlagsAsync(
            DateTime fromIst, DateTime toIst, string actorEmail = null)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@FromIst", fromIst.Date, DbType.Date);
            parameters.Add("@ToIst", toIst.Date, DbType.Date);
            parameters.Add("@ActorEmail", string.IsNullOrWhiteSpace(actorEmail) ? null : actorEmail.Trim(), DbType.AnsiString, size: 150);

            using var con = new SqlConnection(_connectionString);
            using var multi = await con.QueryMultipleAsync(
                "dbo.usp_GetQCActivityFlags",
                parameters,
                commandTimeout: CommandTimeoutSeconds,
                commandType: CommandType.StoredProcedure);

            var flags = (await multi.ReadAsync<QCActivityFlagVM>()).ToList();
            var items = multi.IsConsumed ? new List<QCActivityFlagItemVM>() : (await multi.ReadAsync<QCActivityFlagItemVM>()).ToList();
            var settings = multi.IsConsumed ? new List<QCActivitySettingRowVM>() : (await multi.ReadAsync<QCActivitySettingRowVM>()).ToList();
            return (flags, items, settings);
        }

        public async Task<QCActivityCoverageVM> GetCoverageAsync(DateTime fromIst, DateTime toIst)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@FromIst", fromIst.Date, DbType.Date);
            parameters.Add("@ToIst", toIst.Date, DbType.Date);

            using var con = new SqlConnection(_connectionString);
            using var multi = await con.QueryMultipleAsync(
                "dbo.usp_GetQCActivityCoverage",
                parameters,
                commandTimeout: CommandTimeoutSeconds,
                commandType: CommandType.StoredProcedure);

            var coverage = (await multi.ReadFirstOrDefaultAsync<QCActivityCoverageVM>()) ?? new QCActivityCoverageVM();
            if (!multi.IsConsumed)
                coverage.Untouched = (await multi.ReadAsync<QCActivityUntouchedVM>()).ToList();
            return coverage;
        }

        public async Task<IReadOnlyList<QCActivitySettingRowVM>> GetSettingsAsync()
        {
            const string sql = @"
                IF OBJECT_ID(N'dbo.QCActivitySetting', N'U') IS NOT NULL
                    EXEC sp_executesql N'SELECT Name, Value FROM dbo.QCActivitySetting';
                ELSE
                    SELECT CAST(NULL AS VARCHAR(50)) AS Name, CAST(NULL AS NVARCHAR(1000)) AS Value WHERE 1 = 0;";
            using var con = new SqlConnection(_connectionString);
            return (await con.QueryAsync<QCActivitySettingRowVM>(sql, commandTimeout: CommandTimeoutSeconds)).ToList();
        }

        public async Task<ReportEditVM> GetReportEditAsync(int id)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Id", id, DbType.Int32);

            using var con = new SqlConnection(_connectionString);
            return await con.QueryFirstOrDefaultAsync<ReportEditVM>(
                "dbo.usp_GetReportEdit",
                parameters,
                commandTimeout: CommandTimeoutSeconds,
                commandType: CommandType.StoredProcedure);
        }
    }
}
