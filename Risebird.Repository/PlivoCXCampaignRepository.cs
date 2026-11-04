using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data.PlivoCXModels;
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
    public class PlivoCXCampaignRepository : IPlivoCXCampaignRepository
    {
        private readonly string _connectionString;
        private readonly IApplicationErrorLogger _logger;

        public PlivoCXCampaignRepository(IConfiguration configuration, IApplicationErrorLogger logger)
        {
            _connectionString = configuration["ConnectionString:DBConnectionString"];
            _logger = logger;
        }

        // ── Agents ────────────────────────────────────────────────────────────────

        public async Task<List<PlivoCXAgent>> GetAgentsAsync(bool activeOnly = true)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var rows = await conn.QueryAsync<PlivoCXAgent>(
                    "SELECT * FROM PlivoCXAgent WHERE (@ActiveOnly = 0 OR IsActive = 1) ORDER BY Id",
                    new { ActiveOnly = activeOnly });
                return rows.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<PlivoCXAgent>();
            }
        }

        public async Task<PlivoCXAgent> GetAgentAsync(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                return await conn.QueryFirstOrDefaultAsync<PlivoCXAgent>(
                    "SELECT * FROM PlivoCXAgent WHERE Id = @Id", new { Id = id });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<int> InsertAgentAsync(PlivoCXAgent agent)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                // Plivo rows are written exactly as before the provider columns existed, so seeding
                // keeps working on a database where the Provider scripts have not run yet.
                bool plivo = PlivoCXCampaignProvider.Normalize(agent.Provider) == PlivoCXCampaignProvider.Plivo;
                string sql = plivo ? @"
                    INSERT INTO PlivoCXAgent (Name, Description, TriggerUrl, Purpose, TargetType, CollectsSlot, Channel, IsActive)
                    VALUES (@Name, @Description, @TriggerUrl, @Purpose, @TargetType, @CollectsSlot, @Channel, @IsActive);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);" : @"
                    INSERT INTO PlivoCXAgent (Name, Description, TriggerUrl, Purpose, TargetType, CollectsSlot, Channel, IsActive, Provider, ProviderAgentId)
                    VALUES (@Name, @Description, @TriggerUrl, @Purpose, @TargetType, @CollectsSlot, @Channel, @IsActive, @Provider, @ProviderAgentId);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";
                return await conn.ExecuteScalarAsync<int>(sql, agent);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task UpdateAgentAsync(PlivoCXAgent agent)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.ExecuteAsync(@"
                UPDATE PlivoCXAgent SET
                    Name            = @Name,
                    Description     = @Description,
                    TriggerUrl      = @TriggerUrl,
                    Purpose         = @Purpose,
                    TargetType      = @TargetType,
                    CollectsSlot    = @CollectsSlot,
                    Channel         = @Channel,
                    IsActive        = @IsActive,
                    Provider        = @Provider,
                    ProviderAgentId = @ProviderAgentId
                WHERE Id = @Id", agent);
        }

        // ── Campaigns ─────────────────────────────────────────────────────────────

        public async Task<int> InsertCampaignAsync(PlivoCXCampaign campaign)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                const string sql = @"
                    INSERT INTO PlivoCXCampaign (
                        Name, IRFId, IRFName, ClientName, SourceType, AgentId, CallPurpose, Status,
                        WindowOpens, WindowCloses, TimeZone, AttemptsPerDay, TotalAttempts,
                        MinGapMinutes, ConcurrentCalls, StartsOn, EndsOn, CreatedBy, CreatedAt
                    ) VALUES (
                        @Name, @IRFId, @IRFName, @ClientName, @SourceType, @AgentId, @CallPurpose, @Status,
                        @WindowOpens, @WindowCloses, @TimeZone, @AttemptsPerDay, @TotalAttempts,
                        @MinGapMinutes, @ConcurrentCalls, @StartsOn, @EndsOn, @CreatedBy, GETUTCDATE()
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";
                int id = await conn.ExecuteScalarAsync<int>(sql, campaign);
                // Written separately so a Plivo campaign saves on a database without the column yet.
                if (id > 0 && PlivoCXCampaignProvider.Normalize(campaign.Provider) != PlivoCXCampaignProvider.Plivo)
                    await conn.ExecuteAsync("UPDATE PlivoCXCampaign SET Provider = @Provider WHERE Id = @Id",
                        new { Id = id, Provider = PlivoCXCampaignProvider.Normalize(campaign.Provider) });
                return id;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<PlivoCXCampaign> GetCampaignAsync(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                return await conn.QueryFirstOrDefaultAsync<PlivoCXCampaign>(
                    "SELECT * FROM PlivoCXCampaign WHERE Id = @Id", new { Id = id });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<List<PlivoCXCampaignVM>> ListCampaignsAsync(string search)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var rows = await conn.QueryAsync<PlivoCXCampaignVM>(
                    "usp_GetPlivoCXCampaigns", new { Search = search },
                    commandType: CommandType.StoredProcedure);
                return rows.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<PlivoCXCampaignVM>();
            }
        }

        public async Task<List<PlivoCXCampaign>> GetRunningCampaignsAsync()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var rows = await conn.QueryAsync<PlivoCXCampaign>(
                    "SELECT * FROM PlivoCXCampaign WHERE Status = @Status ORDER BY Id",
                    new { Status = PlivoCXCampaignStatus.Running });
                return rows.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<PlivoCXCampaign>();
            }
        }

        public async Task UpdateCampaignPolicyAsync(PlivoCXCampaign campaign)
        {
            using var conn = new SqlConnection(_connectionString);
            const string sql = @"
                UPDATE PlivoCXCampaign SET
                    WindowOpens     = @WindowOpens,
                    WindowCloses    = @WindowCloses,
                    TimeZone        = @TimeZone,
                    AttemptsPerDay  = @AttemptsPerDay,
                    TotalAttempts   = @TotalAttempts,
                    MinGapMinutes   = @MinGapMinutes,
                    ConcurrentCalls = @ConcurrentCalls,
                    StartsOn        = @StartsOn,
                    EndsOn          = @EndsOn,
                    UpdatedAt       = GETUTCDATE()
                WHERE Id = @Id";
            await conn.ExecuteAsync(sql, campaign);
        }

        public async Task SetCampaignStatusAsync(int id, string status)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.ExecuteAsync(
                    "UPDATE PlivoCXCampaign SET Status = @Status, UpdatedAt = GETUTCDATE() WHERE Id = @Id",
                    new { Id = id, Status = status });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        // ── Campaign candidates ───────────────────────────────────────────────────

        public async Task<int> AddCampaignCandidatesAsync(int campaignId, IEnumerable<PlivoCXCampaignCandidate> rows)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();
            const string sql = @"
                IF NOT EXISTS (SELECT 1 FROM PlivoCXCampaignCandidate WHERE CampaignId = @CampaignId AND CandidateId = @CandidateId)
                INSERT INTO PlivoCXCampaignCandidate (
                    CampaignId, CandidateId, CandidateName, CandidateEmail, CandidatePhone,
                    IRFId, IRFName, JobTitle, ClientName, InterviewId, OriginalInterviewDate,
                    State, LastDisposition, CreatedAt
                ) VALUES (
                    @CampaignId, @CandidateId, @CandidateName, @CandidateEmail, @CandidatePhone,
                    @IRFId, @IRFName, @JobTitle, @ClientName, @InterviewId, @OriginalInterviewDate,
                    @State, @LastDisposition, GETUTCDATE()
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

        public async Task<List<PlivoCXCampaignCandidate>> GetCampaignCandidatesAsync(int campaignId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var multi = await conn.QueryMultipleAsync(
                    "usp_GetPlivoCXCampaignCandidates", new { CampaignId = campaignId },
                    commandType: CommandType.StoredProcedure);
                return (await multi.ReadAsync<PlivoCXCampaignCandidate>()).ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<PlivoCXCampaignCandidate>();
            }
        }

        public async Task<PlivoCXCampaignCallStats> GetCampaignCallStatsAsync(int campaignId, int inFlightMinutes = 30)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var multi = await conn.QueryMultipleAsync(
                    "usp_GetPlivoCXCampaignCandidates", new { CampaignId = campaignId, InFlightMinutes = inFlightMinutes },
                    commandType: CommandType.StoredProcedure);
                await multi.ReadAsync<PlivoCXCampaignCandidate>();
                var stats = await multi.ReadFirstOrDefaultAsync<PlivoCXCampaignCallStatsRow>();
                return new PlivoCXCampaignCallStats
                {
                    FinishedCalls = stats?.FinishedCalls ?? 0,
                    AnsweredCalls = stats?.AnsweredCalls ?? 0,
                    InFlightCalls = stats?.InFlightCalls ?? 0
                };
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new PlivoCXCampaignCallStats();
            }
        }

        // SUM over no rows is NULL, so the stats row reads into nullable ints first.
        private class PlivoCXCampaignCallStatsRow
        {
            public int? FinishedCalls { get; set; }
            public int? AnsweredCalls { get; set; }
            public int? InFlightCalls { get; set; }
        }

        public async Task<PlivoCXCampaignCandidate> GetCampaignCandidateAsync(int campaignId, int candidateId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                return await conn.QueryFirstOrDefaultAsync<PlivoCXCampaignCandidate>(
                    "SELECT * FROM PlivoCXCampaignCandidate WHERE CampaignId = @CampaignId AND CandidateId = @CandidateId",
                    new { CampaignId = campaignId, CandidateId = candidateId });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<List<PlivoCXCampaignCandidate>> GetDueCandidatesAsync(int campaignId, int take, DateTime nowUtc, DateTime dayStartUtc, int inFlightMinutes = 30)
        {
            if (take <= 0) return new List<PlivoCXCampaignCandidate>();
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var rows = await conn.QueryAsync<PlivoCXCampaignCandidate>(
                    "usp_GetDuePlivoCXCampaignCandidates",
                    new { CampaignId = campaignId, Take = take, NowUtc = nowUtc, DayStartUtc = dayStartUtc, InFlightMinutes = inFlightMinutes },
                    commandType: CommandType.StoredProcedure);
                return rows.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<PlivoCXCampaignCandidate>();
            }
        }

        public async Task<bool> TryClaimForDialAsync(int campaignCandidateId, int seconds)
        {
            // Not wrapped in try/catch: if the claim cannot run, dialling must not go ahead unguarded.
            using var conn = new SqlConnection(_connectionString);
            int rows = await conn.ExecuteAsync(@"
                UPDATE PlivoCXCampaignCandidate SET
                    DialLockUntil = DATEADD(SECOND, @Seconds, GETUTCDATE())
                WHERE Id = @Id
                  AND State = 'Pending'
                  AND (DialLockUntil IS NULL OR DialLockUntil < GETUTCDATE())",
                new { Id = campaignCandidateId, Seconds = seconds });
            return rows == 1;
        }

        public async Task ReleaseDialClaimAsync(int campaignCandidateId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.ExecuteAsync(
                    "UPDATE PlivoCXCampaignCandidate SET DialLockUntil = NULL WHERE Id = @Id",
                    new { Id = campaignCandidateId });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task<bool> MarkMeetingConfirmedByCandidateAsync(int interviewId, string candidateEmail)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                int rows = await conn.ExecuteAsync(@"
                    UPDATE im SET
                        IsCandidateResponsed = 1,
                        CandidateResponse    = 1
                    FROM InterviewMeetings im
                    WHERE im.id = @InterviewId
                      AND LTRIM(RTRIM(im.candidateEmail)) = LTRIM(RTRIM(@CandidateEmail))
                      AND ISNULL(im.IsDeleted, 0) = 0
                      AND ISNULL(im.confirmationStatus, '') <> '2'",
                    new { InterviewId = interviewId, CandidateEmail = candidateEmail });
                return rows > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task BumpCampaignCandidateAsync(int campaignCandidateId, int callLogId, int minGapMinutes)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                const string sql = @"
                    UPDATE PlivoCXCampaignCandidate SET
                        Attempts      = Attempts + 1,
                        LastCallAt    = GETUTCDATE(),
                        LastCallLogId = @CallLogId,
                        NextAttemptAt = DATEADD(MINUTE, @MinGapMinutes, GETUTCDATE()),
                        DialLockUntil = NULL,
                        UpdatedAt     = GETUTCDATE()
                    WHERE Id = @Id";
                await conn.ExecuteAsync(sql, new { Id = campaignCandidateId, CallLogId = callLogId > 0 ? callLogId : (int?)null, MinGapMinutes = minGapMinutes });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task SetCampaignCandidateStateAsync(int campaignCandidateId, string state, string disposition, string bookedSlot, DateTime? nextAttemptAtUtc)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                const string sql = @"
                    UPDATE PlivoCXCampaignCandidate SET
                        State           = @State,
                        LastDisposition = COALESCE(@Disposition, LastDisposition),
                        BookedSlot      = COALESCE(@BookedSlot, BookedSlot),
                        NextAttemptAt   = @NextAttemptAt,
                        UpdatedAt       = GETUTCDATE()
                    WHERE Id = @Id";
                await conn.ExecuteAsync(sql, new
                {
                    Id = campaignCandidateId,
                    State = state,
                    Disposition = disposition,
                    BookedSlot = bookedSlot,
                    NextAttemptAt = nextAttemptAtUtc
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task<int> CountInFlightAsync(int campaignId, int inFlightMinutes = 30)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                return await conn.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*) FROM PlivoCXCallLogs
                    WHERE CampaignId = @CampaignId
                      AND CallStatus IN ('initiated', 'triggered')
                      AND CreatedAt >= DATEADD(MINUTE, -@InFlightMinutes, GETUTCDATE())",
                    new { CampaignId = campaignId, InFlightMinutes = inFlightMinutes });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<int> CountAllInFlightAsync(int inFlightMinutes, string provider = "Plivo")
        {
            // Not swallowed: if the count cannot be read, the dispatcher must not assume free lines.
            using var conn = new SqlConnection(_connectionString);
            try
            {
                // Calls without a campaign (timer jobs, one-off and test calls) are Plivo's.
                return await conn.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*)
                    FROM PlivoCXCallLogs l
                    LEFT JOIN PlivoCXCampaign pc ON pc.Id = l.CampaignId
                    WHERE l.CallStatus IN ('initiated', 'triggered')
                      AND l.CreatedAt >= DATEADD(MINUTE, -@InFlightMinutes, GETUTCDATE())
                      AND ISNULL(pc.Provider, 'Plivo') = @Provider",
                    new { InFlightMinutes = inFlightMinutes, Provider = PlivoCXCampaignProvider.Normalize(provider) ?? PlivoCXCampaignProvider.Plivo });
            }
            catch (SqlException ex) when (ex.Number == 207) // Provider column not deployed yet: everything is Plivo
            {
                return await conn.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*) FROM PlivoCXCallLogs
                    WHERE CallStatus IN ('initiated', 'triggered')
                      AND CreatedAt >= DATEADD(MINUTE, -@InFlightMinutes, GETUTCDATE())",
                    new { InFlightMinutes = inFlightMinutes });
            }
        }

        public async Task RefundAttemptAsync(int campaignCandidateId, DateTime nextAttemptAtUtc)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.ExecuteAsync(@"
                    UPDATE PlivoCXCampaignCandidate SET
                        Attempts      = CASE WHEN Attempts > 0 THEN Attempts - 1 ELSE 0 END,
                        State         = 'Pending',
                        NextAttemptAt = @NextAttemptAt,
                        UpdatedAt     = GETUTCDATE()
                    WHERE Id = @Id",
                    new { Id = campaignCandidateId, NextAttemptAt = nextAttemptAtUtc });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task<int> CountPendingAsync(int campaignId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                return await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM PlivoCXCampaignCandidate WHERE CampaignId = @CampaignId AND State = 'Pending'",
                    new { CampaignId = campaignId });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<int> RetireExhaustedCandidatesAsync(int campaignId, int inFlightMinutes = 30)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                return await conn.ExecuteAsync(@"
                    UPDATE cc SET
                        State           = 'Cold',
                        LastDisposition = COALESCE(cc.LastDisposition, 'exhausted'),
                        NextAttemptAt   = NULL,
                        UpdatedAt       = GETUTCDATE()
                    FROM PlivoCXCampaignCandidate cc
                    INNER JOIN PlivoCXCampaign pc ON pc.Id = cc.CampaignId
                    WHERE cc.CampaignId = @CampaignId
                      AND cc.State = 'Pending'
                      AND cc.Attempts >= pc.TotalAttempts
                      AND NOT EXISTS (
                            SELECT 1 FROM PlivoCXCallLogs l
                            WHERE l.CampaignId  = cc.CampaignId
                              AND l.CandidateId = cc.CandidateId
                              AND l.CallStatus IN ('initiated', 'triggered')
                              AND l.CreatedAt >= DATEADD(MINUTE, -@InFlightMinutes, GETUTCDATE()))",
                    new { CampaignId = campaignId, InFlightMinutes = inFlightMinutes });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        // ── Create-screen sources ─────────────────────────────────────────────────

        public async Task<List<PlivoCXCampaignIRFOption>> GetActiveIRFsAsync()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var rows = await conn.QueryAsync<PlivoCXCampaignIRFOption>(@"
                    SELECT irf.id AS Id, irf.IRFId, irf.JDTiltle AS JobTitle, cl.Name AS ClientName
                    FROM IRF irf
                    INNER JOIN Client cl ON cl.id = irf.ClientId
                    WHERE ISNULL(irf.IsDeleted, 0) = 0
                      AND CAST(irf.EndDate AS date) > CAST(GETDATE() AS date)
                    ORDER BY irf.id DESC");
                return rows.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<PlivoCXCampaignIRFOption>();
            }
        }

        public async Task<List<PlivoCXCampaignIRFPickerRow>> GetPickerRowsForIRFAsync(int irfId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var rows = await conn.QueryAsync<PlivoCXCampaignIRFPickerRow>(
                    "usp_GetPlivoCXCampaignPickerForIRF",
                    new { IRFId = irfId, NowIST = DateTime.Now },
                    commandType: CommandType.StoredProcedure);
                return rows.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<PlivoCXCampaignIRFPickerRow>();
            }
        }

        public async Task<List<PlivoCXCampaignIRFPickerRow>> GetNotScheduledAsync(string email)
        {
            // Errors propagate so the picker shows "could not load" instead of an empty list.
            using var conn = new SqlConnection(_connectionString);
            var rows = await conn.QueryAsync<PlivoCXCampaignIRFPickerRow>(
                "usp_GetPlivoCXCampaignNotScheduled", new { EmailId = email },
                commandType: CommandType.StoredProcedure, commandTimeout: 120);
            return rows.ToList();
        }

        public async Task<List<PlivoCXCampaignIRFPickerRow>> GetRescheduleAsync(string email, DateTime startDate, DateTime endDate)
        {
            // Errors propagate so the picker shows "could not load" instead of an empty list.
            using var conn = new SqlConnection(_connectionString);
            var rows = await conn.QueryAsync<PlivoCXCampaignIRFPickerRow>(
                "usp_GetPlivoCXCampaignReschedule", new { EmailId = email, Startdate = startDate, Enddate = endDate },
                commandType: CommandType.StoredProcedure, commandTimeout: 120);
            return rows.ToList();
        }

        public async Task SetCallLogCampaignAsync(int callLogId, int campaignId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.ExecuteAsync(
                    "UPDATE PlivoCXCallLogs SET CampaignId = @CampaignId WHERE Id = @Id",
                    new { Id = callLogId, CampaignId = campaignId });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
