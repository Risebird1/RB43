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

        public async Task<IReadOnlyList<IrfAuditDirectoryRowVm>> GetIrfAuditDirectoryAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_connectionString))
                    throw new InvalidOperationException("Database connection string is not configured.");

                using var con = new SqlConnection(_connectionString);
                var rows = await con.QueryAsync<IrfAuditDirectoryRowVm>(
                    "usp_GetIrfAuditDirectory",
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120);
                return rows?.ToList() ?? new List<IrfAuditDirectoryRowVm>();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                throw new InvalidOperationException(
                    "Failed to load IRF Audit Directory. Deploy usp_GetIrfAuditDirectory, AutoQcIrfBackfillRun, and TransInterviewer.AutoQcVerdict scripts.",
                    ex);
            }
        }
    }
}
