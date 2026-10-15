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
    public class AutoQcRemovalMonitorRepository : IAutoQcRemovalMonitorRepository
    {
        private readonly string _connectionString;
        private readonly IApplicationErrorLogger _logger;

        public AutoQcRemovalMonitorRepository(IConfiguration configuration, IApplicationErrorLogger logger)
        {
            _connectionString = configuration["ConnectionString:DBConnectionString"];
            _logger = logger;
        }

        public async Task<AutoQcRemovalMonitorVm> GetMonitorDataAsync(DateTime? fromUtc, DateTime? toUtc, int? irfId, string source, string verdict)
        {
            var vm = new AutoQcRemovalMonitorVm
            {
                FilterFrom = fromUtc,
                FilterTo = toUtc,
                FilterIrfId = irfId,
                FilterSource = source,
                FilterVerdict = verdict
            };

            try
            {
                using var con = new SqlConnection(_connectionString);
                using var multi = await con.QueryMultipleAsync(
                    "usp_GetAutoQcRemovalMonitorData",
                    new
                    {
                        FromUtc = fromUtc,
                        ToUtc = toUtc,
                        IrfId = irfId,
                        AuditSource = string.IsNullOrWhiteSpace(source) ? null : source,
                        Verdict = string.IsNullOrWhiteSpace(verdict) ? null : verdict
                    },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 60);

                var rows = (await multi.ReadAsync<AutoQcRemovalMonitorRowVm>())?.ToList() ?? new List<AutoQcRemovalMonitorRowVm>();
                var irfRows = (await multi.ReadAsync<AutoQcRemovalMonitorIrfBreakdownVm>())?.ToList() ?? new List<AutoQcRemovalMonitorIrfBreakdownVm>();

                foreach (var row in rows)
                {
                    if (row.TriggeringInterviewId.HasValue && row.TriggeringInterviewId.Value > 0)
                        row.QcDetailsUrl = DmIvAutoQcAuditConstants.QcMergedAiDetailsUrl(row.TriggeringInterviewId.Value);
                }

                vm.Removals = rows;
                vm.IrfBreakdown = irfRows;

                var nowUtc = DateTime.UtcNow;
                var todayStart = nowUtc.Date;
                var day7 = nowUtc.AddDays(-7);
                var day30 = nowUtc.AddDays(-30);

                vm.TodayRemovals = rows.Count(x => x.RemovedAt.ToUniversalTime() >= todayStart);
                vm.Rolling7DayRemovals = rows.Count(x => x.RemovedAt.ToUniversalTime() >= day7);
                vm.Rolling30DayRemovals = rows.Count(x => x.RemovedAt.ToUniversalTime() >= day30);
                vm.Rolling30DayReactivations = rows.Count(x => x.WasReactivated && x.RemovedAt.ToUniversalTime() >= day30);
                vm.ForwardTriggerCount = rows.Count(x => x.Source == DmIvAutoQcAuditConstants.SourceForwardTrigger);
                vm.BackfillCount = rows.Count(x => x.Source == DmIvAutoQcAuditConstants.SourceRetroactiveBackfill60Day);
                vm.SuspiciousCount = rows.Count(x => x.Verdict == DmIvAutoQcAuditConstants.VerdictSuspicious);
                vm.RedoCount = rows.Count(x => x.Verdict == DmIvAutoQcAuditConstants.VerdictRedo);
                vm.ReactivationRatePercent = vm.Rolling30DayRemovals == 0
                    ? 0
                    : Math.Round((decimal)vm.Rolling30DayReactivations * 100m / vm.Rolling30DayRemovals, 1);

                vm.LatestBackfill = await GetLatestBackfillSummaryAsync();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }

            return vm;
        }

        public async Task<int> GetBackfillCandidateCountAsync(DateTime? asOfUtc = null, int windowDays = 60)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var row = await con.QueryFirstOrDefaultAsync<dynamic>(
                    "usp_GetAutoQcBackfillCandidateCount",
                    new { AsOfUtc = asOfUtc, WindowDays = windowDays },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120);
                return row == null ? 0 : (int)row.CandidateTransInterviewerCount;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<IReadOnlyList<AutoQcBackfillCandidateRow>> GetBackfillCandidatesAsync(DateTime? asOfUtc = null, int windowDays = 60)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var rows = await con.QueryAsync<AutoQcBackfillCandidateRow>(
                    "usp_GetAutoQcBackfillCandidates",
                    new { AsOfUtc = asOfUtc, WindowDays = windowDays },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 180);
                return rows?.ToList() ?? new List<AutoQcBackfillCandidateRow>();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Array.Empty<AutoQcBackfillCandidateRow>();
            }
        }

        public async Task<(bool HasRun, Guid? LatestBatchId, DateTime? LatestExecutedAtUtc)> GetBackfillRunStatusAsync()
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var row = await con.QueryFirstOrDefaultAsync<dynamic>(
                    "usp_HasAutoQcBackfillRun",
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30);
                if (row == null)
                    return (false, null, null);
                return ((bool)row.HasRun, (Guid?)row.LatestBatchId, (DateTime?)row.LatestExecutedAtUtc);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return (false, null, null);
            }
        }

        public async Task RecordBackfillRunAsync(Guid batchId, int recordsRemoved, int recordsSkipped, int triggeringInterviewCount, string executedBy)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                await con.ExecuteAsync(
                    "usp_RecordAutoQcBackfillRun",
                    new
                    {
                        BatchId = batchId,
                        RecordsRemoved = recordsRemoved,
                        RecordsSkipped = recordsSkipped,
                        TriggeringInterviewCount = triggeringInterviewCount,
                        ExecutedBy = executedBy ?? "System"
                    },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                throw;
            }
        }

        public async Task<AutoQcBackfillRunSummaryVm> GetLatestBackfillSummaryAsync()
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                return await con.QueryFirstOrDefaultAsync<AutoQcBackfillRunSummaryVm>(
                    "usp_GetAutoQcBackfillRunSummary",
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<AutoQcPerIrfBackfillPreviewVm> GetPerIrfBackfillPreviewAsync(int irfId, DateTime fromUtc, DateTime toUtc)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                return new AutoQcPerIrfBackfillPreviewVm
                {
                    IrfId = irfId,
                    ErrorMessage = "Database connection string (ConnectionString:DBConnectionString) is not configured."
                };
            }

            try
            {
                using var con = new SqlConnection(_connectionString);
                var row = await con.QueryFirstOrDefaultAsync<AutoQcPerIrfBackfillPreviewVm>(
                    "usp_GetPerIrfAutoQcBackfillPreview",
                    new { IrfId = irfId, FromUtc = fromUtc, ToUtc = toUtc },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 300);
                if (row == null)
                {
                    return new AutoQcPerIrfBackfillPreviewVm
                    {
                        IrfId = irfId,
                        ErrorMessage = "Preview query returned no row. Deploy the latest usp_GetPerIrfAutoQcBackfillPreview script."
                    };
                }

                row.IrfId = irfId;
                return row;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                var detail = ex.GetBaseException()?.Message ?? ex.Message;
                var isTimeout = detail.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0
                    || detail.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0;
                return new AutoQcPerIrfBackfillPreviewVm
                {
                    IrfId = irfId,
                    ErrorMessage = isTimeout
                        ? "Preview query timed out. Deploy the latest usp_GetPerIrfAutoQcBackfillPreview and Indexes/AutoQcBackfillPerformanceIndexes.sql, then retry."
                        : "Preview query failed. Deploy usp_GetPerIrfAutoQcBackfillPreview and ensure TransInterviewerAuditLog exists. " + detail
                };
            }
        }

        public async Task<IReadOnlyList<AutoQcBackfillCandidateRow>> GetPerIrfBackfillCandidatesAsync(int irfId, DateTime fromUtc, DateTime toUtc)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var rows = await con.QueryAsync<AutoQcBackfillCandidateRow>(
                    "usp_GetPerIrfAutoQcBackfillCandidates",
                    new { IrfId = irfId, FromUtc = fromUtc, ToUtc = toUtc },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 300);
                return rows?.ToList() ?? new List<AutoQcBackfillCandidateRow>();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Array.Empty<AutoQcBackfillCandidateRow>();
            }
        }

        public async Task RecordPerIrfBackfillRunAsync(Guid runId, int irfId, DateTime fromUtc, DateTime toUtc, int verdictsScanned, int newFlagsSet, int alreadyFlaggedSkipped, string executedBy)
        {
            using var con = new SqlConnection(_connectionString);
            await con.ExecuteAsync(
                @"INSERT INTO dbo.AutoQcIrfBackfillRun (RunId, IrfId, DateFromUtc, DateToUtc, VerdictsScanned, NewFlagsSet, AlreadyFlaggedSkipped, ExecutedBy, ExecutedAtUtc)
                  VALUES (@RunId, @IrfId, @DateFromUtc, @DateToUtc, @VerdictsScanned, @NewFlagsSet, @AlreadyFlaggedSkipped, @ExecutedBy, GETUTCDATE())",
                new
                {
                    RunId = runId,
                    IrfId = irfId,
                    DateFromUtc = fromUtc,
                    DateToUtc = toUtc,
                    VerdictsScanned = verdictsScanned,
                    NewFlagsSet = newFlagsSet,
                    AlreadyFlaggedSkipped = alreadyFlaggedSkipped,
                    ExecutedBy = executedBy ?? "System"
                },
                commandTimeout: 30);
        }

        public async Task<IReadOnlyList<IrfAuditDirectoryRowVm>> GetIrfAuditDirectoryAsync(DateTime fromIst, DateTime toIst, string ownerEmail = null, string profileName = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_connectionString))
                    throw new InvalidOperationException("Database connection string is not configured.");

                using var con = new SqlConnection(_connectionString);
                var rows = (await con.QueryAsync<IrfAuditDirectoryRowVm>(
                    "usp_GetIrfAuditDirectory",
                    new
                    {
                        FromIst = fromIst.Date,
                        ToIst = toIst.Date,
                        OwnerEmail = string.IsNullOrWhiteSpace(ownerEmail) ? null : ownerEmail.Trim(),
                        ProfileName = string.IsNullOrWhiteSpace(profileName) ? null : profileName.Trim()
                    },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120))?.ToList() ?? new List<IrfAuditDirectoryRowVm>();

                foreach (var row in rows)
                    row.LastBackfillLabel = BuildLastBackfillLabel(row.LastBackfillAtUtc, row.LastRiskBackfillFromIst, row.LastRiskBackfillToIst);

                return rows;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                throw new InvalidOperationException(
                    "Failed to load IRF Audit Directory. Deploy usp_GetIrfAuditDirectory, AutoQcIrfBackfillRun, and TransInterviewer.AutoQcVerdict scripts.",
                    ex);
            }
        }

        private static string BuildLastBackfillLabel(DateTime? lastBackfillAtUtc, DateTime? riskFromIst, DateTime? riskToIst)
        {
            if (!lastBackfillAtUtc.HasValue)
                return null;

            var utc = DateTime.SpecifyKind(lastBackfillAtUtc.Value, DateTimeKind.Utc);
            var ist = utc.AddMinutes(330);
            var stamp = ist.ToString("yyyy-MM-dd HH:mm") + " IST";

            if (riskFromIst.HasValue && riskToIst.HasValue)
                return $"{stamp} · {riskFromIst.Value:dd MMM}–{riskToIst.Value:dd MMM}";

            return stamp;
        }

        public async Task<(IReadOnlyList<IrfAuditInterviewerRiskRowVm> Active, IReadOnlyList<IrfAuditInterviewerRiskRowVm> Removed)> GetIrfAuditInterviewerRiskBreakdownAsync(int irfId, DateTime fromIst, DateTime toIst)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var exists = await con.ExecuteScalarAsync<int>(
                    "SELECT CASE WHEN OBJECT_ID(N'dbo.usp_GetIrfAuditInterviewerRiskBreakdown', N'P') IS NOT NULL THEN 1 ELSE 0 END");
                if (exists == 0)
                {
                    throw new InvalidOperationException(
                        "Stored procedure dbo.usp_GetIrfAuditInterviewerRiskBreakdown was not found. Deploy Risebird.DB/StoredProcedures/usp_GetIrfAuditInterviewerRiskBreakdown.sql.");
                }

                var rows = (await con.QueryAsync<IrfAuditInterviewerRiskRowVm>(
                    "usp_GetIrfAuditInterviewerRiskBreakdown",
                    new { IrfId = irfId, FromIst = fromIst.Date, ToIst = toIst.Date },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 90))?.ToList() ?? new List<IrfAuditInterviewerRiskRowVm>();

                // Single result set from the SP, partitioned here by IsRemoved (simpler than QueryMultiple —
                // same #Active pool is scanned once server-side; see usp_GetIrfAuditInterviewerRiskBreakdown.sql).
                var active = rows.Where(r => !r.IsRemoved).ToList();
                var removed = rows.Where(r => r.IsRemoved).ToList();
                return (active, removed);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                throw;
            }
        }

        public async Task<IReadOnlyList<IrfAuditInterviewerRiskDetailVm>> GetIrfAuditInterviewerRiskInterviewDetailsAsync(
            int irfId, DateTime fromIst, DateTime toIst, int? transInterviewerId = null)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var exists = await con.ExecuteScalarAsync<int>(
                    "SELECT CASE WHEN OBJECT_ID(N'dbo.usp_GetIrfAuditInterviewerRiskInterviewDetails', N'P') IS NOT NULL THEN 1 ELSE 0 END");
                if (exists == 0)
                {
                    // Soft-degrade: summary still works if detail SP not deployed yet.
                    return Array.Empty<IrfAuditInterviewerRiskDetailVm>();
                }

                var rows = await con.QueryAsync<IrfAuditInterviewerRiskDetailVm>(
                    "usp_GetIrfAuditInterviewerRiskInterviewDetails",
                    new
                    {
                        IrfId = irfId,
                        FromIst = fromIst.Date,
                        ToIst = toIst.Date,
                        TransInterviewerId = transInterviewerId
                    },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 90);
                return rows?.ToList() ?? new List<IrfAuditInterviewerRiskDetailVm>();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                throw;
            }
        }

        public async Task<IReadOnlyList<int>> GetRiskCategoryBackfillCandidatesByIrfAsync(int irfId, DateTime fromIst, DateTime toIst, int maxRows)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var rows = await con.QueryAsync<int>(
                    "usp_GetRiskCategoryBackfillCandidatesByIrf",
                    new
                    {
                        IrfId = irfId,
                        FromIst = fromIst.Date,
                        ToIst = toIst.Date,
                        MaxRows = maxRows
                    },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120);
                return rows?.ToList() ?? new List<int>();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                throw;
            }
        }

        public async Task<int> SyncTrialFlagsVerdictFromRiskCategoryAsync()
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var exists = await con.ExecuteScalarAsync<int>(
                    "SELECT CASE WHEN OBJECT_ID(N'dbo.usp_SyncTrialFlagsVerdictFromRiskCategory', N'P') IS NOT NULL THEN 1 ELSE 0 END");
                if (exists == 0)
                    return 0;

                return await con.QueryFirstOrDefaultAsync<int>(
                    "usp_SyncTrialFlagsVerdictFromRiskCategory",
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<IReadOnlyList<int>> GetExistingRiskCategoryInterviewIdsAsync(IEnumerable<int> interviewIds)
        {
            var ids = interviewIds?.Distinct().ToList() ?? new List<int>();
            if (ids.Count == 0)
                return Array.Empty<int>();

            try
            {
                using var con = new SqlConnection(_connectionString);
                var rows = await con.QueryAsync<int>(
                    "SELECT InterviewId FROM dbo.RiskCategory WHERE InterviewId IN @InterviewIds",
                    new { InterviewIds = ids },
                    commandTimeout: 60);
                return rows?.ToList() ?? new List<int>();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Array.Empty<int>();
            }
        }

        private const string EnsureIrfAuditRiskBackfillRunSql = @"
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'IrfAuditRiskBackfillRun')
BEGIN
    CREATE TABLE [dbo].[IrfAuditRiskBackfillRun] (
        [Id]              INT            IDENTITY(1,1) NOT NULL,
        [IrfId]           INT            NOT NULL,
        [ExecutedAtUtc]   DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
        [FromIst]         DATE           NOT NULL,
        [ToIst]           DATE           NOT NULL,
        [Scanned]         INT            NOT NULL DEFAULT 0,
        [Processed]       INT            NOT NULL DEFAULT 0,
        [SyncUpdates]     INT            NOT NULL DEFAULT 0,
        [ExecutedBy]      VARCHAR(150)   NOT NULL,
        [Message]         VARCHAR(500)   NULL,
        CONSTRAINT [PK_IrfAuditRiskBackfillRun] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    CREATE NONCLUSTERED INDEX [IX_IrfAuditRiskBackfillRun_IrfId]
        ON [dbo].[IrfAuditRiskBackfillRun] ([IrfId] ASC, [ExecutedAtUtc] DESC);
END";

        public async Task RecordRiskBackfillRunAsync(int irfId, DateTime fromIst, DateTime toIst, int scanned, int processed, int syncUpdates, string executedBy, string message)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                await con.ExecuteAsync(EnsureIrfAuditRiskBackfillRunSql, commandTimeout: 30);
                await con.ExecuteAsync(
                    @"INSERT INTO dbo.IrfAuditRiskBackfillRun (IrfId, ExecutedAtUtc, FromIst, ToIst, Scanned, Processed, SyncUpdates, ExecutedBy, Message)
                      VALUES (@IrfId, SYSUTCDATETIME(), @FromIst, @ToIst, @Scanned, @Processed, @SyncUpdates, @ExecutedBy, @Message)",
                    new
                    {
                        IrfId = irfId,
                        FromIst = fromIst.Date,
                        ToIst = toIst.Date,
                        Scanned = scanned,
                        Processed = processed,
                        SyncUpdates = syncUpdates,
                        ExecutedBy = string.IsNullOrWhiteSpace(executedBy) ? "System" : executedBy.Trim(),
                        Message = string.IsNullOrWhiteSpace(message) ? null : (message.Length > 500 ? message.Substring(0, 500) : message)
                    },
                    commandTimeout: 30);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task<string> GetIrfChannelOwnerEmailAsync(int irfId)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var email = await con.QueryFirstOrDefaultAsync<string>(
                    @"SELECT NULLIF(LTRIM(RTRIM(ul.EmailId)), '')
                      FROM dbo.IRF irf
                      LEFT JOIN dbo.UserList ul ON ul.id = irf.RisebirdHr
                      WHERE irf.id = @IrfId",
                    new { IrfId = irfId },
                    commandTimeout: 30);
                return email;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<IrfAuditWindowQualityVm> GetIrfAuditWindowQualityAsync(int irfId, DateTime fromIst, DateTime toIst)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var exists = await con.ExecuteScalarAsync<int>(
                    "SELECT CASE WHEN OBJECT_ID(N'dbo.usp_GetIrfAuditWindowQuality', N'P') IS NOT NULL THEN 1 ELSE 0 END");
                if (exists == 0)
                    return null;

                return await con.QueryFirstOrDefaultAsync<IrfAuditWindowQualityVm>(
                    "usp_GetIrfAuditWindowQuality",
                    new { IrfId = irfId, FromIst = fromIst.Date, ToIst = toIst.Date },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 60);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public const double SlowQueryWarnSecondsOverview = 2.0;
        public const double SlowQueryWarnSecondsChannels = 1.0;

        private async Task WarnIfSlowAsync(string proc, TimeSpan elapsed, double warnSeconds, string parameters)
        {
            if (elapsed.TotalSeconds < warnSeconds)
                return;
            await _logger.LogMessage(
                LogLevelInfo.Warning,
                $"Slow IRF Audit query: {proc} took {elapsed.TotalMilliseconds:0}ms (warn>{warnSeconds * 1000:0}ms). {parameters}");
        }

        public async Task<InterviewerRiskOverviewPageVm> GetInterviewerRiskOverviewAsync(
            DateTime fromIst, DateTime toIst, string ownerEmail, decimal? minRiskRatio,
            bool pendingReviewOnly, int offset, int pageSize)
        {
            var page = new InterviewerRiskOverviewPageVm { Offset = offset, PageSize = pageSize };
            try
            {
                using var con = new SqlConnection(_connectionString);
                var exists = await con.ExecuteScalarAsync<int>(
                    "SELECT CASE WHEN OBJECT_ID(N'dbo.usp_GetInterviewerRiskOverview', N'P') IS NOT NULL THEN 1 ELSE 0 END");
                if (exists == 0)
                {
                    throw new InvalidOperationException(
                        "Stored procedure dbo.usp_GetInterviewerRiskOverview was not found. Deploy Risebird.DB/StoredProcedures/usp_GetInterviewerRiskOverview.sql.");
                }

                var sw = System.Diagnostics.Stopwatch.StartNew();
                using var multi = await con.QueryMultipleAsync(
                    "usp_GetInterviewerRiskOverview",
                    new
                    {
                        FromIst = fromIst.Date,
                        ToIst = toIst.Date,
                        OwnerEmail = string.IsNullOrWhiteSpace(ownerEmail) ? null : ownerEmail.Trim(),
                        MinRiskRatio = minRiskRatio,
                        PendingReviewOnly = pendingReviewOnly,
                        Offset = Math.Max(0, offset),
                        PageSize = pageSize <= 0 ? 25 : Math.Min(pageSize, 200)
                    },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120);
                var rows = (await multi.ReadAsync<InterviewerRiskOverviewRowVm>())?.ToList()
                    ?? new List<InterviewerRiskOverviewRowVm>();
                var summary = await multi.ReadFirstOrDefaultAsync<InterviewerRiskOverviewSummaryVm>()
                    ?? new InterviewerRiskOverviewSummaryVm();
                // Result set 3 (deduction review upheld rate per AI check); absent on an older procedure.
                if (!multi.IsConsumed)
                    summary.ReviewChecks = (await multi.ReadAsync<DeductionReviewCheckStatVM>())?.ToList()
                        ?? new List<DeductionReviewCheckStatVM>();
                sw.Stop();
                await WarnIfSlowAsync(
                    "usp_GetInterviewerRiskOverview",
                    sw.Elapsed,
                    SlowQueryWarnSecondsOverview,
                    $"from={fromIst:yyyy-MM-dd} to={toIst:yyyy-MM-dd} offset={offset} pageSize={pageSize}");

                page.Rows = rows;
                page.Summary = summary;
                page.TotalRows = rows.Count > 0 ? rows[0].TotalRows : summary.InterviewersAboveThreshold;
                return page;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                throw;
            }
        }

        public async Task<IReadOnlyList<InterviewerRiskOverviewChannelRowVm>> GetInterviewerRiskOverviewChannelsAsync(
            int interviewerId, DateTime fromIst, DateTime toIst, string ownerEmail)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var exists = await con.ExecuteScalarAsync<int>(
                    "SELECT CASE WHEN OBJECT_ID(N'dbo.usp_GetInterviewerRiskOverviewChannels', N'P') IS NOT NULL THEN 1 ELSE 0 END");
                if (exists == 0)
                {
                    throw new InvalidOperationException(
                        "Stored procedure dbo.usp_GetInterviewerRiskOverviewChannels was not found. Deploy Risebird.DB/StoredProcedures/usp_GetInterviewerRiskOverviewChannels.sql.");
                }

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var rows = (await con.QueryAsync<InterviewerRiskOverviewChannelRowVm>(
                    "usp_GetInterviewerRiskOverviewChannels",
                    new
                    {
                        InterviewerId = interviewerId,
                        FromIst = fromIst.Date,
                        ToIst = toIst.Date,
                        OwnerEmail = string.IsNullOrWhiteSpace(ownerEmail) ? null : ownerEmail.Trim()
                    },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 90))?.ToList() ?? new List<InterviewerRiskOverviewChannelRowVm>();
                sw.Stop();
                await WarnIfSlowAsync(
                    "usp_GetInterviewerRiskOverviewChannels",
                    sw.Elapsed,
                    SlowQueryWarnSecondsChannels,
                    $"interviewerId={interviewerId} from={fromIst:yyyy-MM-dd} to={toIst:yyyy-MM-dd}");
                return rows;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                throw;
            }
        }

        public async Task<IReadOnlyList<InterviewerRiskOverviewRowVm>> GetInterviewerRiskOverviewExportRowsAsync(
            DateTime fromIst, DateTime toIst, string ownerEmail, decimal? minRiskRatio, bool pendingReviewOnly)
        {
            // Cap export at 2000 interviewers (pageSize max for a single fetch).
            var page = await GetInterviewerRiskOverviewAsync(
                fromIst, toIst, ownerEmail, minRiskRatio, pendingReviewOnly, 0, 2000);
            return page.Rows;
        }
    }
}
