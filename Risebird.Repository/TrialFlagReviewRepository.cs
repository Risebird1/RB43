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
    public class TrialFlagReviewRepository : ITrialFlagReviewRepository
    {
        private readonly string _connectionString;
        private readonly IApplicationErrorLogger _logger;

        public TrialFlagReviewRepository(IConfiguration configuration, IApplicationErrorLogger logger)
        {
            _connectionString = configuration["ConnectionString:DBConnectionString"];
            _logger = logger;
        }

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

        public async Task<IReadOnlyList<TrialFlagReviewRowVm>> GetTrialFlagsAsync(string reviewStatus)
        {
            using var con = new SqlConnection(_connectionString);
            var rows = await con.QueryAsync<TrialFlagReviewRowDbRow>(
                "usp_GetTrialFlagsForReview",
                new { ReviewStatus = string.IsNullOrWhiteSpace(reviewStatus) ? null : reviewStatus.Trim() },
                commandType: CommandType.StoredProcedure,
                commandTimeout: 60);
            return rows?.Select(MapTrialFlagReviewRow).ToList() ?? new List<TrialFlagReviewRowVm>();
        }

        private static TrialFlagReviewRowVm MapTrialFlagReviewRow(TrialFlagReviewRowDbRow row) =>
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
                InterviewerName = row.InterviewerName,
                InterviewerEmail = row.InterviewerEmail,
                InterviewerCodedId = row.InterviewerCodedId,
                IrfName = row.IrfName,
                Severity = row.Severity,
                RiskCategoryLabels = row.RiskCategoryLabels,
                RiskCategoryReasons = row.RiskCategoryReasons,
                SeverityBreakdown = row.SeverityBreakdown,
                CandidateFeedbackComment = row.CandidateFeedbackComment,
                QcMergeVerdict = row.QcMergeVerdict,
                QcMergeScore = row.QcMergeScore,
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
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120);
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

        private sealed class TrialFlagReviewRowDbRow
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
            public string InterviewerName { get; set; }
            public string InterviewerEmail { get; set; }
            public string InterviewerCodedId { get; set; }
            public string IrfName { get; set; }
            public string Severity { get; set; }
            public string RiskCategoryLabels { get; set; }
            public string RiskCategoryReasons { get; set; }
            public string SeverityBreakdown { get; set; }
            public string CandidateFeedbackComment { get; set; }
            public string QcMergeVerdict { get; set; }
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
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 60);
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
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30);
            }
            catch (SqlException ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
    }
}
