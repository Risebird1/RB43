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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    public class TrialFlagReviewRepository : ITrialFlagReviewRepository
    {
        /// <summary>List slow-query warning threshold (seconds). Do not raise command timeout.</summary>
        public const double SlowQueryWarnSecondsList = 3.0;
        /// <summary>Detail slow-query warning threshold (seconds).</summary>
        public const double SlowQueryWarnSecondsDetail = 2.0;

        private readonly string _connectionString;
        private readonly IApplicationErrorLogger _logger;
        private readonly int _artificialDelayMsList;
        private readonly int _artificialDelayMsDetail;

        public TrialFlagReviewRepository(IConfiguration configuration, IApplicationErrorLogger logger)
        {
            _connectionString = configuration["ConnectionString:DBConnectionString"];
            _logger = logger;
            // Staging/dev only: set TrialFlagReview:ArtificialDelayMsList/Detail > warn thresholds to prove slow-query logging.
            _artificialDelayMsList = ParseNonNegativeMs(configuration["TrialFlagReview:ArtificialDelayMsList"]);
            _artificialDelayMsDetail = ParseNonNegativeMs(configuration["TrialFlagReview:ArtificialDelayMsDetail"]);
        }

        private static int ParseNonNegativeMs(string raw) =>
            int.TryParse(raw, out var ms) && ms > 0 ? ms : 0;

        public async Task<(bool TableExists, int TotalRows)> GetDiagnosticsAsync()
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var tableExists = await con.ExecuteScalarAsync<int>(
                    "SELECT CASE WHEN OBJECT_ID(N'dbo.TrialFlags', N'U') IS NOT NULL THEN 1 ELSE 0 END");
                if (tableExists == 0)
                    return (false, 0);

                var totalRows = await con.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.TrialFlags");
                return (true, totalRows);
            }
            catch (SqlException ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                throw;
            }
        }

        public async Task<(int Pending, int Kept, int Removed)> GetStatusCountsAsync()
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var rows = await con.QueryAsync<StatusCountRow>(
                    @"SELECT ReviewStatus, COUNT(*) AS Cnt
                      FROM dbo.TrialFlags
                      GROUP BY ReviewStatus");

                var pending = 0;
                var kept = 0;
                var removed = 0;
                foreach (var row in rows ?? Enumerable.Empty<StatusCountRow>())
                {
                    var status = row.ReviewStatus?.Trim();
                    if (string.Equals(status, TrialFlagConstants.ReviewPending, StringComparison.OrdinalIgnoreCase))
                        pending = row.Cnt;
                    else if (string.Equals(status, TrialFlagConstants.ReviewKept, StringComparison.OrdinalIgnoreCase))
                        kept = row.Cnt;
                    else if (string.Equals(status, TrialFlagConstants.ReviewRemoved, StringComparison.OrdinalIgnoreCase))
                        removed = row.Cnt;
                }

                return (pending, kept, removed);
            }
            catch (SqlException ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                throw;
            }
        }

        private sealed class StatusCountRow
        {
            public string ReviewStatus { get; set; }
            public int Cnt { get; set; }
        }

        public async Task<(IReadOnlyList<TrialFlagReviewRowVm> Rows, int TotalRows)> GetTrialFlagsListAsync(
            string reviewStatus, int offset, int pageSize, string search = null, string severity = null, string channelOwner = null)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var con = new SqlConnection(_connectionString);
                var rows = (await con.QueryAsync<TrialFlagListDbRow>(
                    "usp_GetTrialFlagsForReview_List",
                    new
                    {
                        ReviewStatus = string.IsNullOrWhiteSpace(reviewStatus) ? null : reviewStatus.Trim(),
                        Offset = Math.Max(0, offset),
                        PageSize = pageSize <= 0 ? 25 : Math.Min(pageSize, 200),
                        Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
                        Severity = string.IsNullOrWhiteSpace(severity) ? null : severity.Trim(),
                        ChannelOwner = string.IsNullOrWhiteSpace(channelOwner) ? null : channelOwner.Trim()
                    },
                    commandType: CommandType.StoredProcedure)).ToList();

                if (_artificialDelayMsList > 0)
                    await Task.Delay(_artificialDelayMsList);

                sw.Stop();
                await WarnIfSlowAsync(
                    "usp_GetTrialFlagsForReview_List",
                    sw.Elapsed,
                    SlowQueryWarnSecondsList,
                    $"status={reviewStatus};offset={offset};pageSize={pageSize};search={search};severity={severity};owner={channelOwner};rows={rows.Count}");

                var total = rows.FirstOrDefault()?.TotalRows ?? 0;
                return (rows.Select(MapListRow).ToList(), total);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                var msg = ex.GetBaseException()?.Message ?? ex.Message;
                if (msg != null
                    && msg.IndexOf("Could not find stored procedure", StringComparison.OrdinalIgnoreCase) >= 0
                    && msg.IndexOf("usp_GetTrialFlagsForReview_List", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    msg = "usp_GetTrialFlagsForReview_List is not deployed on this database. " + msg;
                }
                throw new InvalidOperationException(msg, ex);
            }
        }

        public async Task<IReadOnlyList<TrialFlagOwnerSummaryVm>> GetOwnerSummariesAsync(string reviewStatus)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var rows = await con.QueryAsync<TrialFlagOwnerSummaryVm>(
                    "usp_GetTrialFlagOwnerSummary",
                    new { ReviewStatus = string.IsNullOrWhiteSpace(reviewStatus) ? null : reviewStatus.Trim() },
                    commandType: CommandType.StoredProcedure);
                return rows?.ToList() ?? new List<TrialFlagOwnerSummaryVm>();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Array.Empty<TrialFlagOwnerSummaryVm>();
            }
        }

        public async Task<TrialFlagReviewRowVm> GetTrialFlagDetailAsync(int trialFlagId)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var con = new SqlConnection(_connectionString);
                var row = await con.QueryFirstOrDefaultAsync<TrialFlagDetailDbRow>(
                    "usp_GetTrialFlagDetail",
                    new { TrialFlagId = trialFlagId },
                    commandType: CommandType.StoredProcedure);

                if (_artificialDelayMsDetail > 0)
                    await Task.Delay(_artificialDelayMsDetail);

                sw.Stop();
                await WarnIfSlowAsync(
                    "usp_GetTrialFlagDetail",
                    sw.Elapsed,
                    SlowQueryWarnSecondsDetail,
                    $"trialFlagId={trialFlagId}");

                return row == null ? null : MapDetailRow(row);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                throw;
            }
        }

        private async Task WarnIfSlowAsync(string proc, TimeSpan elapsed, double warnSeconds, string parameters)
        {
            if (elapsed.TotalSeconds < warnSeconds)
                return;
            await _logger.LogMessage(
                LogLevelInfo.Warning,
                $"Slow trial-flag query: {proc} took {elapsed.TotalMilliseconds:0}ms (warn>{warnSeconds * 1000:0}ms). {parameters}");
        }

        private static TrialFlagReviewRowVm MapListRow(TrialFlagListDbRow row) =>
            new TrialFlagReviewRowVm
            {
                TrialFlagId = row.TrialFlagId,
                InterviewerId = row.InterviewerId,
                IrfId = row.IrfId,
                ChannelId = row.ChannelId,
                TriggeringInterviewId = row.TriggeringInterviewId,
                TriggeringInterviewDate = row.TriggeringInterviewDate,
                OverallVerdict = row.OverallVerdict,
                DisplayVerdict = row.DisplayVerdict,
                ReviewStatus = row.ReviewStatus,
                FlaggedAtUtc = row.FlaggedAtUtc,
                ReviewedAtUtc = row.ReviewedAtUtc,
                ReviewedBy = row.ReviewedBy,
                ReviewerComment = row.ReviewerComment,
                InterviewerName = row.InterviewerName,
                InterviewerEmail = row.InterviewerEmail,
                InterviewerCodedId = row.InterviewerCodedId,
                InterviewerUserId = row.InterviewerUserId,
                InterviewerMobileNumber = row.InterviewerMobileNumber,
                InterviewerAltMobileNumber = row.InterviewerAltMobileNumber,
                IsSuspicious = row.IsSuspicious,
                IrfName = row.IrfName,
                ChannelOwner = row.ChannelOwner,
                ChannelOwnerEmail = row.ChannelOwnerEmail,
                Severity = row.Severity,
                RiskCategoryLabels = row.RiskCategoryLabels,
                RiskCategoryReasons = row.RiskCategoryReasons,
                QcMergeVerdict = row.QcMergeVerdict,
                QcMergeTotalPenalty = row.QcMergeTotalPenalty,
                QcMergeQcScore = row.QcMergeQcScore,
                QcMergeScore = row.QcMergeScore ?? row.QcMergeTotalPenalty
            };

        private static TrialFlagReviewRowVm MapDetailRow(TrialFlagDetailDbRow row) =>
            new TrialFlagReviewRowVm
            {
                TrialFlagId = row.TrialFlagId,
                InterviewerId = row.InterviewerId,
                IrfId = row.IrfId,
                ChannelId = row.ChannelId,
                TriggeringInterviewId = row.TriggeringInterviewId,
                TriggeringInterviewDate = row.TriggeringInterviewDate,
                TranscriptQualityPass = ToBool(row.TranscriptQualityPass),
                ReportQualityPass = ToBool(row.ReportQualityPass),
                DecisionQualityPass = ToBool(row.DecisionQualityPass),
                CandidateRatingPass = ToBool(row.CandidateRatingPass),
                JoinTimePass = ToBool(row.JoinTimePass),
                InterviewDurationPass = ToBool(row.InterviewDurationPass),
                TranscriptQualityScore = row.TranscriptQualityScore,
                ReportQualityScore = row.ReportQualityScore,
                CandidateRating = row.CandidateRating,
                JoinTimeMinutes = row.JoinTimeMinutes,
                InterviewDurationMinutes = row.InterviewDurationMinutes,
                OverallVerdict = row.OverallVerdict,
                DisplayVerdict = row.DisplayVerdict,
                ReviewStatus = row.ReviewStatus,
                FlaggedAtUtc = row.FlaggedAtUtc,
                ReviewedAtUtc = row.ReviewedAtUtc,
                ReviewedBy = row.ReviewedBy,
                ReviewerComment = row.ReviewerComment,
                Severity = row.Severity,
                RiskCategoryLabels = row.RiskCategoryLabels,
                RiskCategoryReasons = row.RiskCategoryReasons,
                SeverityBreakdown = row.SeverityBreakdown,
                CandidateFeedbackComment = row.CandidateFeedbackComment,
                QcMergeVerdict = row.QcMergeVerdict,
                QcMergeTotalPenalty = row.QcMergeTotalPenalty,
                QcMergeQcScore = row.QcMergeQcScore,
                QcMergeScore = row.QcMergeScore ?? row.QcMergeTotalPenalty,
                QcMergeFlagsJson = row.QcMergeFlagsJson,
                PaymentDeduction = row.PaymentDeduction,
                PaymentReasonValuesJson = row.PaymentReasonValuesJson,
                PaymentDeductionReasons = row.PaymentDeductionReasons,
                PaymentRecommendation = row.PaymentRecommendation,
                PaymentRecommendationComments = row.PaymentRecommendationComments
            };

        private static bool ToBool(int? value) => value.HasValue && value.Value != 0;

        public async Task<int> SyncTrialFlagsVerdictFromRiskCategoryAsync()
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var exists = await con.ExecuteScalarAsync<int>(
                    "SELECT CASE WHEN OBJECT_ID(N'dbo.usp_SyncTrialFlagsVerdictFromRiskCategory', N'P') IS NOT NULL THEN 1 ELSE 0 END");
                if (exists == 0)
                    return 0;

                var updated = await con.QueryFirstOrDefaultAsync<int>(
                    "usp_SyncTrialFlagsVerdictFromRiskCategory",
                    commandType: CommandType.StoredProcedure);
                return updated;
            }
            catch (SqlException ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        private sealed class TrialFlagListDbRow
        {
            public int TrialFlagId { get; set; }
            public int InterviewerId { get; set; }
            public int IrfId { get; set; }
            public int ChannelId { get; set; }
            public int TriggeringInterviewId { get; set; }
            public DateTime TriggeringInterviewDate { get; set; }
            public string OverallVerdict { get; set; }
            public string DisplayVerdict { get; set; }
            public string ReviewStatus { get; set; }
            public DateTime FlaggedAtUtc { get; set; }
            public DateTime? ReviewedAtUtc { get; set; }
            public string ReviewedBy { get; set; }
            public string ReviewerComment { get; set; }
            public string InterviewerName { get; set; }
            public string InterviewerEmail { get; set; }
            public string InterviewerCodedId { get; set; }
            public int? InterviewerUserId { get; set; }
            public string InterviewerMobileNumber { get; set; }
            public string InterviewerAltMobileNumber { get; set; }
            public bool IsSuspicious { get; set; }
            public string IrfName { get; set; }
            public string ChannelOwnerEmail { get; set; }
            public string ChannelOwner { get; set; }
            public string Severity { get; set; }
            public string RiskCategoryLabels { get; set; }
            public string RiskCategoryReasons { get; set; }
            public string QcMergeVerdict { get; set; }
            public int? QcMergeTotalPenalty { get; set; }
            public int? QcMergeQcScore { get; set; }
            public int? QcMergeScore { get; set; }
            public int TotalRows { get; set; }
        }

        private sealed class TrialFlagDetailDbRow
        {
            public int TrialFlagId { get; set; }
            public int InterviewerId { get; set; }
            public int IrfId { get; set; }
            public int ChannelId { get; set; }
            public int TriggeringInterviewId { get; set; }
            public DateTime TriggeringInterviewDate { get; set; }
            public int? TranscriptQualityPass { get; set; }
            public int? ReportQualityPass { get; set; }
            public int? DecisionQualityPass { get; set; }
            public int? CandidateRatingPass { get; set; }
            public int? JoinTimePass { get; set; }
            public int? InterviewDurationPass { get; set; }
            public decimal? TranscriptQualityScore { get; set; }
            public decimal? ReportQualityScore { get; set; }
            public decimal? CandidateRating { get; set; }
            public decimal? JoinTimeMinutes { get; set; }
            public decimal? InterviewDurationMinutes { get; set; }
            public string OverallVerdict { get; set; }
            public string DisplayVerdict { get; set; }
            public string ReviewStatus { get; set; }
            public DateTime FlaggedAtUtc { get; set; }
            public DateTime? ReviewedAtUtc { get; set; }
            public string ReviewedBy { get; set; }
            public string ReviewerComment { get; set; }
            public string Severity { get; set; }
            public string RiskCategoryLabels { get; set; }
            public string RiskCategoryReasons { get; set; }
            public string SeverityBreakdown { get; set; }
            public string CandidateFeedbackComment { get; set; }
            public string QcMergeVerdict { get; set; }
            public int? QcMergeTotalPenalty { get; set; }
            public int? QcMergeQcScore { get; set; }
            public int? QcMergeScore { get; set; }
            public string QcMergeFlagsJson { get; set; }
            public int PaymentDeduction { get; set; }
            public string PaymentReasonValuesJson { get; set; }
            public string PaymentDeductionReasons { get; set; }
            public string PaymentRecommendation { get; set; }
            public string PaymentRecommendationComments { get; set; }
        }

        public async Task<IReadOnlyList<InterviewAiRiskSummaryRowVm>> GetInterviewAiRiskSummaryAsync(int interviewId)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var exists = await con.ExecuteScalarAsync<int>(
                    "SELECT CASE WHEN OBJECT_ID(N'dbo.usp_GetInterviewAiRiskSummary', N'P') IS NOT NULL THEN 1 ELSE 0 END");
                if (exists == 0)
                    return Array.Empty<InterviewAiRiskSummaryRowVm>();

                var rows = await con.QueryAsync<InterviewAiRiskSummaryRowVm>(
                    "usp_GetInterviewAiRiskSummary",
                    new { InterviewId = interviewId },
                    commandType: CommandType.StoredProcedure);
                return rows?.ToList() ?? new List<InterviewAiRiskSummaryRowVm>();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                throw;
            }
        }

        public async Task<TrialFlagReviewRowVm> GetTrialFlagByIdAsync(int trialFlagId)
        {
            const string sql = @"
SELECT
    tf.Id AS TrialFlagId,
    tf.InterviewerId,
    tf.IrfId,
    tf.ReviewStatus,
    COALESCE(
        NULLIF(LTRIM(RTRIM(ul.EmailId)), ''),
        NULLIF(LTRIM(RTRIM(im.interviewer)), ''),
        'Interviewer #' + CAST(tf.InterviewerId AS VARCHAR(12))
    ) AS InterviewerName,
    COALESCE(
        NULLIF(LTRIM(RTRIM(ul.EmailId)), ''),
        NULLIF(LTRIM(RTRIM(im.interviewer)), ''),
        ''
    ) AS InterviewerEmail
FROM dbo.TrialFlags tf
LEFT JOIN dbo.InterviewMeetings im ON im.id = tf.TriggeringInterviewId
LEFT JOIN dbo.Interviewer iv ON iv.id = tf.InterviewerId
LEFT JOIN dbo.UserList ul ON ul.id = iv.UserId
WHERE tf.Id = @TrialFlagId;";

            using var con = new SqlConnection(_connectionString);
            var row = await con.QueryFirstOrDefaultAsync<TrialFlagActionRow>(sql, new { TrialFlagId = trialFlagId });
            if (row == null)
                return null;

            return new TrialFlagReviewRowVm
            {
                TrialFlagId = row.TrialFlagId,
                InterviewerId = row.InterviewerId,
                IrfId = row.IrfId,
                ReviewStatus = row.ReviewStatus,
                InterviewerName = row.InterviewerName,
                InterviewerEmail = row.InterviewerEmail
            };
        }

        private sealed class TrialFlagActionRow
        {
            public int TrialFlagId { get; set; }
            public int InterviewerId { get; set; }
            public int IrfId { get; set; }
            public string ReviewStatus { get; set; }
            public string InterviewerName { get; set; }
            public string InterviewerEmail { get; set; }
        }

        public async Task<bool> UpdateReviewAsync(int trialFlagId, string reviewStatus, string reviewedBy, string reviewerComment)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                return await con.QueryFirstOrDefaultAsync<bool>(
                    "usp_UpdateTrialFlagReview",
                    new
                    {
                        TrialFlagId = trialFlagId,
                        ReviewStatus = reviewStatus,
                        ReviewedBy = reviewedBy,
                        ReviewerComment = reviewerComment
                    },
                    commandType: CommandType.StoredProcedure);
            }
            catch (SqlException ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
    }
}
