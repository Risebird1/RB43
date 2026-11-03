using Dapper;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data.WhatsAppCampaignModels;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    public class WhatsAppCampaignRepository : IWhatsAppCampaignRepository
    {
        private readonly string _connectionString;
        private readonly IApplicationErrorLogger _logger;

        public WhatsAppCampaignRepository(IConfiguration configuration, IApplicationErrorLogger logger)
        {
            _connectionString = configuration["ConnectionString:DBConnectionString"];
            _logger = logger;
        }

        // ── Campaigns ─────────────────────────────────────────────────────────────

        public async Task<int> InsertCampaignAsync(WhatsAppCampaign campaign)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                const string sql = @"
                    INSERT INTO WhatsAppCampaign (
                        Name, SourceType, Purpose, EventName, IRFId, IRFName, ClientName, Status,
                        WindowOpens, WindowCloses, TimeZone, MaxSends, ResendAfterHours, BatchSize,
                        StartsOn, EndsOn, CreatedBy, CreatedAt
                    ) VALUES (
                        @Name, @SourceType, @Purpose, @EventName, @IRFId, @IRFName, @ClientName, @Status,
                        @WindowOpens, @WindowCloses, @TimeZone, @MaxSends, @ResendAfterHours, @BatchSize,
                        @StartsOn, @EndsOn, @CreatedBy, GETUTCDATE()
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";
                return await conn.ExecuteScalarAsync<int>(sql, campaign);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<WhatsAppCampaign> GetCampaignAsync(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                return await conn.QueryFirstOrDefaultAsync<WhatsAppCampaign>(
                    "SELECT * FROM WhatsAppCampaign WHERE Id = @Id", new { Id = id });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<List<WhatsAppCampaignVM>> ListCampaignsAsync(string search)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var rows = await conn.QueryAsync<WhatsAppCampaignVM>(
                    "usp_GetWhatsAppCampaigns", new { Search = search },
                    commandType: CommandType.StoredProcedure);
                return rows.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<WhatsAppCampaignVM>();
            }
        }

        public async Task<List<WhatsAppCampaign>> GetRunningCampaignsAsync()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var rows = await conn.QueryAsync<WhatsAppCampaign>(
                    "SELECT * FROM WhatsAppCampaign WHERE Status = @Status ORDER BY Id",
                    new { Status = WhatsAppCampaignStatus.Running });
                return rows.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<WhatsAppCampaign>();
            }
        }

        public async Task UpdateCampaignPolicyAsync(WhatsAppCampaign campaign)
        {
            using var conn = new SqlConnection(_connectionString);
            const string sql = @"
                UPDATE WhatsAppCampaign SET
                    WindowOpens      = @WindowOpens,
                    WindowCloses     = @WindowCloses,
                    TimeZone         = @TimeZone,
                    MaxSends         = @MaxSends,
                    ResendAfterHours = @ResendAfterHours,
                    BatchSize        = @BatchSize,
                    StartsOn         = @StartsOn,
                    EndsOn           = @EndsOn,
                    UpdatedAt        = GETUTCDATE()
                WHERE Id = @Id";
            await conn.ExecuteAsync(sql, campaign);
        }

        public async Task SetCampaignStatusAsync(int id, string status)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.ExecuteAsync(
                    "UPDATE WhatsAppCampaign SET Status = @Status, UpdatedAt = GETUTCDATE() WHERE Id = @Id",
                    new { Id = id, Status = status });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        // ── Campaign candidates ───────────────────────────────────────────────────

        public async Task<int> AddCampaignCandidatesAsync(int campaignId, IEnumerable<WhatsAppCampaignCandidate> rows)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();
            const string sql = @"
                IF NOT EXISTS (SELECT 1 FROM WhatsAppCampaignCandidate WHERE CampaignId = @CampaignId AND CandidateId = @CandidateId)
                INSERT INTO WhatsAppCampaignCandidate (
                    CampaignId, CandidateId, CandidateName, CandidateEmail, Phone,
                    IRFId, IRFName, JobTitle, ClientName, InterviewId, OriginalInterviewDate,
                    State, OutcomeNote, CreatedAt
                ) VALUES (
                    @CampaignId, @CandidateId, @CandidateName, @CandidateEmail, @Phone,
                    @IRFId, @IRFName, @JobTitle, @ClientName, @InterviewId, @OriginalInterviewDate,
                    @State, @OutcomeNote, GETUTCDATE()
                );";
            int added = 0;
            foreach (var row in rows)
            {
                row.CampaignId = campaignId;
                added += await conn.ExecuteAsync(sql, row, tx);
            }
            tx.Commit();
            return added;
        }

        public async Task<List<WhatsAppCampaignCandidate>> GetCampaignCandidatesAsync(int campaignId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var rows = await conn.QueryAsync<WhatsAppCampaignCandidate>(@"
                    SELECT * FROM WhatsAppCampaignCandidate
                    WHERE CampaignId = @CampaignId
                    ORDER BY CASE State WHEN 'Pending' THEN 0 WHEN 'Sent' THEN 1 WHEN 'Failed' THEN 2 ELSE 3 END,
                             ISNULL(LastSentAt, CreatedAt) DESC, Id",
                    new { CampaignId = campaignId });
                return rows.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<WhatsAppCampaignCandidate>();
            }
        }

        public async Task<WhatsAppCampaignCandidate> GetCampaignCandidateAsync(int campaignId, int candidateId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                return await conn.QueryFirstOrDefaultAsync<WhatsAppCampaignCandidate>(
                    "SELECT * FROM WhatsAppCampaignCandidate WHERE CampaignId = @CampaignId AND CandidateId = @CandidateId",
                    new { CampaignId = campaignId, CandidateId = candidateId });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<List<WhatsAppCampaignCandidate>> ClaimDueCandidatesAsync(int campaignId, int take, int lockMinutes, int? campaignCandidateId = null)
        {
            if (take <= 0) return new List<WhatsAppCampaignCandidate>();
            // Not wrapped in try/catch: if the claim cannot run, sending must not go ahead unguarded.
            using var conn = new SqlConnection(_connectionString);
            var rows = await conn.QueryAsync<WhatsAppCampaignCandidate>(
                "usp_ClaimDueWhatsAppCampaignCandidates",
                new { CampaignId = campaignId, Take = take, LockMinutes = lockMinutes, CampaignCandidateId = campaignCandidateId },
                commandType: CommandType.StoredProcedure);
            return rows.ToList();
        }

        public async Task SaveSendResultAsync(int campaignCandidateId, WhatsAppCandidateTransition transition, string lastResult, string lastError)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                const string sql = @"
                    UPDATE WhatsAppCampaignCandidate SET
                        State         = @State,
                        Sends         = Sends + CASE WHEN @Counted = 1 THEN 1 ELSE 0 END,
                        FailedSends   = @FailedSends,
                        FirstSentAt   = CASE WHEN @Counted = 1 THEN ISNULL(FirstSentAt, GETUTCDATE()) ELSE FirstSentAt END,
                        LastSentAt    = CASE WHEN @Counted = 1 THEN GETUTCDATE() ELSE LastSentAt END,
                        NextSendAt    = @NextSendAt,
                        LastResult    = @LastResult,
                        LastError     = @LastError,
                        OutcomeNote   = COALESCE(@OutcomeNote, OutcomeNote),
                        SendLockUntil = NULL,
                        UpdatedAt     = GETUTCDATE()
                    WHERE Id = @Id";
                await conn.ExecuteAsync(sql, new
                {
                    Id = campaignCandidateId,
                    transition.State,
                    transition.Counted,
                    transition.FailedSends,
                    transition.NextSendAt,
                    transition.OutcomeNote,
                    LastResult = Cut(lastResult, 200),
                    LastError = Cut(lastError, 1000)
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task SetCampaignCandidateStateAsync(int campaignCandidateId, string state, string outcomeNote)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                const string sql = @"
                    UPDATE WhatsAppCampaignCandidate SET
                        State       = @State,
                        NextSendAt  = CASE WHEN @State = 'Pending' THEN GETUTCDATE() ELSE NULL END,
                        FailedSends = CASE WHEN @State = 'Pending' THEN 0 ELSE FailedSends END,
                        ConvertedAt = CASE WHEN @State = 'Converted' THEN ISNULL(ConvertedAt, GETUTCDATE()) ELSE ConvertedAt END,
                        OutcomeNote = @OutcomeNote,
                        UpdatedAt   = GETUTCDATE()
                    WHERE Id = @Id";
                await conn.ExecuteAsync(sql, new { Id = campaignCandidateId, State = state, OutcomeNote = Cut(outcomeNote, 300) });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task<int> AdvanceResendsAsync(int campaignId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                return await conn.ExecuteScalarAsync<int>(
                    "usp_AdvanceWhatsAppCampaignResends", new { CampaignId = campaignId },
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<int> RefreshOutcomesAsync(int campaignId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                return await conn.ExecuteScalarAsync<int>(
                    "usp_RefreshWhatsAppCampaignOutcomes", new { CampaignId = campaignId },
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<int> CountOpenAsync(int campaignId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                return await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM WhatsAppCampaignCandidate WHERE CampaignId = @CampaignId AND State IN ('Pending', 'Sent')",
                    new { CampaignId = campaignId });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                // Unknown counts as open, so a database hiccup never completes a campaign.
                return 1;
            }
        }

        public async Task<int> CountPendingAsync(int campaignId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                return await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM WhatsAppCampaignCandidate WHERE CampaignId = @CampaignId AND State = 'Pending'",
                    new { CampaignId = campaignId });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        // ── Create-screen and send support ────────────────────────────────────────

        public async Task<List<WhatsAppCampaignEligibilityRow>> GetEligibilityAsync(IEnumerable<WhatsAppCampaignEligibilityRow> keys, string eventName)
        {
            var list = keys?.Where(k => !string.IsNullOrWhiteSpace(k.Email)).ToList() ?? new List<WhatsAppCampaignEligibilityRow>();
            if (list.Count == 0) return new List<WhatsAppCampaignEligibilityRow>();
            // One round trip for the whole picker: the number a send would use (usp_FetchWhatsAppNumberByEmail),
            // the WhatsApp opt-out (WhatsAppRepository.IsOptedOutAsync), sends counted toward the 3-per-IRF limit
            // (WhatsAppRepository.CountSentAsync) and another open campaign already holding the candidate.
            const string sql = @"
                SELECT
                    j.CandidateId,
                    j.Email,
                    j.IRFId,
                    num.WhatsAppNumber,
                    CAST(CASE WHEN EXISTS (
                        SELECT 1 FROM dbo.WhatsAppOptOut o
                        WHERE o.Audience = 'Candidate'
                          AND o.EmailId = LOWER(REPLACE(LTRIM(RTRIM(j.Email)), ' ', '+'))
                          AND (o.IRFId IS NULL OR o.IRFId = j.IRFId)
                          AND o.ReEnabledAt IS NULL) THEN 1 ELSE 0 END AS BIT) AS OptedOut,
                    (SELECT COUNT(*) FROM dbo.SentWhatsAppMessageDetails s
                     WHERE s.Recipient = LTRIM(RTRIM(j.Email)) AND s.IRFId = j.IRFId AND s.[Event] = @Event
                       AND (s.[Status] = 'Sent' OR s.[Status] IS NULL)) AS SendsUsed,
                    active.Name AS ActiveCampaignName
                FROM OPENJSON(@Keys) WITH (CandidateId INT '$.c', Email NVARCHAR(256) '$.e', IRFId INT '$.i') j
                OUTER APPLY (
                    SELECT TOP (1) ISNULL(NULLIF(LTRIM(RTRIM(u.WhatsappNumber)), ''), u.MobileNumber) AS WhatsAppNumber
                    FROM dbo.UserList u WHERE u.Emailid = LTRIM(RTRIM(j.Email))
                ) num
                OUTER APPLY (
                    SELECT TOP (1) wc.Name
                    FROM dbo.WhatsAppCampaignCandidate wcc
                    INNER JOIN dbo.WhatsAppCampaign wc ON wc.Id = wcc.CampaignId
                    WHERE wcc.CandidateId = j.CandidateId
                      AND wcc.[State] IN ('Pending', 'Sent')
                      AND wc.[Status] IN ('Draft', 'Running', 'Paused')
                    ORDER BY wc.Id DESC
                ) active";
            var json = JsonConvert.SerializeObject(list.Select(k => new { c = k.CandidateId, e = k.Email.Trim(), i = k.IRFId }));
            using var conn = new SqlConnection(_connectionString);
            var rows = await conn.QueryAsync<WhatsAppCampaignEligibilityRow>(sql, new { Keys = json, Event = eventName }, commandTimeout: 120);
            return rows.ToList();
        }

        private static string Cut(string s, int max) =>
            string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max);
    }
}
