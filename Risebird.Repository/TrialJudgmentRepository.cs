using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    public class TrialJudgmentRepository : ITrialJudgmentRepository
    {
        private readonly string _connectionString;
        private readonly IApplicationErrorLogger _logger;

        public TrialJudgmentRepository(IConfiguration configuration, IApplicationErrorLogger logger)
        {
            _connectionString = configuration["ConnectionString:DBConnectionString"];
            _logger = logger;
        }

        private const string EnsureTrialFlagBackfillRunSql = @"
IF OBJECT_ID(N'dbo.TrialFlagBackfillRun', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TrialFlagBackfillRun (
        Id            INT IDENTITY(1,1) NOT NULL,
        ExecutedAtUtc DATETIME      NOT NULL CONSTRAINT DF_TrialFlagBackfillRun_ExecutedAtUtc DEFAULT (GETUTCDATE()),
        ExecutedBy    VARCHAR(150)  NOT NULL,
        InterviewId   INT           NULL,
        Inserted      INT           NOT NULL CONSTRAINT DF_TrialFlagBackfillRun_Inserted DEFAULT (0),
        Skipped       INT           NOT NULL CONSTRAINT DF_TrialFlagBackfillRun_Skipped DEFAULT (0),
        Failed        INT           NOT NULL CONSTRAINT DF_TrialFlagBackfillRun_Failed DEFAULT (0),
        Message       VARCHAR(500)  NULL,
        CONSTRAINT PK_TrialFlagBackfillRun PRIMARY KEY CLUSTERED (Id ASC)
    );
    CREATE NONCLUSTERED INDEX IX_TrialFlagBackfillRun_ExecutedBy_At ON dbo.TrialFlagBackfillRun (ExecutedBy, ExecutedAtUtc);
END";

        public async Task RecordBackfillRunAsync(string executedBy, int? interviewId, int inserted, int skipped, int failed, string message)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                await con.ExecuteAsync(EnsureTrialFlagBackfillRunSql, commandTimeout: 30);
                await con.ExecuteAsync(
                    @"INSERT INTO dbo.TrialFlagBackfillRun (ExecutedAtUtc, ExecutedBy, InterviewId, Inserted, Skipped, Failed, Message)
                      VALUES (GETUTCDATE(), @ExecutedBy, @InterviewId, @Inserted, @Skipped, @Failed, @Message)",
                    new
                    {
                        ExecutedBy = string.IsNullOrWhiteSpace(executedBy) ? "System" : (executedBy.Trim().Length > 150 ? executedBy.Trim().Substring(0, 150) : executedBy.Trim()),
                        InterviewId = interviewId.HasValue && interviewId.Value > 0 ? interviewId : null,
                        Inserted = inserted,
                        Skipped = skipped,
                        Failed = failed,
                        Message = string.IsNullOrWhiteSpace(message) ? null : (message.Length > 500 ? message.Substring(0, 500) : message)
                    },
                    commandTimeout: 30);
            }
            catch (System.Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task<TrialJudgmentContextVm> GetContextAsync(int interviewId)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                return await con.QueryFirstOrDefaultAsync<TrialJudgmentContextVm>(
                    "usp_GetTrialJudgmentContext",
                    new { InterviewId = interviewId },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 60);
            }
            catch (SqlException ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                throw;
            }
        }

        public async Task<(bool Inserted, int TrialFlagId)> InsertTrialFlagAsync(
            TrialJudgmentResultVm judgment,
            TrialJudgmentContextVm context,
            int interviewId)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var row = await con.QueryFirstOrDefaultAsync<(bool Inserted, int TrialFlagId)>(
                    "usp_InsertTrialFlag",
                    new
                    {
                        context.InterviewerId,
                        context.IrfId,
                        context.ChannelId,
                        TriggeringInterviewId = interviewId,
                        TriggeringInterviewDate = context.TriggeringInterviewDate,
                        judgment.TranscriptQualityPass,
                        judgment.ReportQualityPass,
                        judgment.DecisionQualityPass,
                        judgment.CandidateRatingPass,
                        judgment.JoinTimePass,
                        judgment.InterviewDurationPass,
                        TranscriptQualityScore = context.TranscriptQualityScore,
                        CandidateRating = context.CandidateRating,
                        JoinTimeMinutes = context.JoinTimeMinutes,
                        InterviewDurationMinutes = context.InterviewDurationMinutes,
                        judgment.OverallVerdict,
                        judgment.Severity
                    },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30);
                return row;
            }
            catch (SqlException ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                throw;
            }
        }

        public async Task<IReadOnlyList<TrialFlagBackfillCandidateVm>> GetBackfillCandidatesAsync()
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var tableExists = await con.ExecuteScalarAsync<int>(
                    "SELECT CASE WHEN OBJECT_ID(N'dbo.TrialFlags', N'U') IS NOT NULL THEN 1 ELSE 0 END");
                if (tableExists == 0)
                    return new List<TrialFlagBackfillCandidateVm>();

                if (await con.ExecuteScalarAsync<int>(
                    "SELECT CASE WHEN OBJECT_ID(N'dbo.usp_GetTrialFlagBackfillCandidates', N'P') IS NOT NULL THEN 1 ELSE 0 END") != 1)
                {
                    return new List<TrialFlagBackfillCandidateVm>();
                }

                var rows = await con.QueryAsync<TrialFlagBackfillCandidateVm>(
                    "usp_GetTrialFlagBackfillCandidates",
                    new
                    {
                        MaxRows = TrialFlagConstants.BackfillCandidateFetchSize,
                        ActivityWindowDays = TrialFlagConstants.BackfillActivityWindowDays
                    },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 90);
                return rows?.ToList() ?? new List<TrialFlagBackfillCandidateVm>();
            }
            catch (SqlException ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                throw;
            }
        }

        public async Task<(QcInterviewAIMergeAssistantResponse MergeResult, int IrfId)?> GetStoredMergeResultAsync(int interviewId)
        {
            const string sql = @"
SELECT TOP 1
    q.MergedAIQCDetails,
    q.AIMergeInterviewerSuspicious,
    q.AIMergeRedoInterview,
    irf.id AS IrfId
FROM dbo.QcInterviewAIevaluation q
INNER JOIN dbo.InterviewMeetings im ON im.id = q.InterviewId
INNER JOIN dbo.IRF irf ON irf.IRFId = im.IRFId
WHERE q.InterviewId = @InterviewId
  AND ISNULL(q.QCDone, 0) = 1;";

            try
            {
                using var con = new SqlConnection(_connectionString);
                var row = await con.QueryFirstOrDefaultAsync<StoredMergeRow>(sql, new { InterviewId = interviewId });
                if (row == null)
                    return null;

                var merge = QcInterviewAIMergeAssistantResponse.FromMergedDetailsJson(row.MergedAIQCDetails)
                    ?? new QcInterviewAIMergeAssistantResponse();

                if (string.IsNullOrWhiteSpace(merge.interviewer_suspicious))
                    merge.interviewer_suspicious = row.AIMergeInterviewerSuspicious;
                if (!merge.RedoInterview.HasValue)
                    merge.RedoInterview = row.AIMergeRedoInterview;

                return (merge, row.IrfId);
            }
            catch (SqlException ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                throw;
            }
        }

        private sealed class StoredMergeRow
        {
            public string MergedAIQCDetails { get; set; }
            public string AIMergeInterviewerSuspicious { get; set; }
            public bool? AIMergeRedoInterview { get; set; }
            public int IrfId { get; set; }
        }
    }
}
