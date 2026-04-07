using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
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
    public class SupportTicketRepository : ISupportTicketRepository
    {
        private readonly string                  _connectionString;
        private readonly IApplicationErrorLogger _logger;

        public SupportTicketRepository(IConfiguration configuration, IApplicationErrorLogger logger)
        {
            _connectionString = configuration["ConnectionString:DBConnectionString"];
            _logger           = logger;
        }

        public async Task<int> InsertTicketAsync(SupportTicket ticket)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var result = await conn.QuerySingleAsync<int>(
                    "usp_InsertSupportTicket",
                    new
                    {
                        ticket.SenderEmail,
                        ticket.Subject,
                        ticket.MailContent,
                        ticket.ReceivedAt,
                        ticket.Intent,
                        ticket.PrimarySpecialization,
                        ticket.MailBodyIntent,
                        ticket.AttachmentJobDescription,
                        ticket.AttachmentPdf,
                        ticket.AttachmentResumes,
                        ticket.DriveFolderLink,
                        ticket.SummaryMail,
                        ticket.SummaryCandidates,
                        ticket.SummaryInterviewScores
                    },
                    commandType: CommandType.StoredProcedure);
                return result;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<bool> UpdateTicketStatusAsync(int id, string status, string changedBy)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var rows = await conn.QuerySingleAsync<int>(
                    "usp_UpdateSupportTicketStatus",
                    new { Id = id, Status = status, ChangedBy = changedBy ?? "System" },
                    commandType: CommandType.StoredProcedure);
                return rows > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<List<SupportTicketHistory>> GetTicketHistoryAsync(int ticketId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var results = await conn.QueryAsync<SupportTicketHistory>(
                    "usp_GetSupportTicketHistory",
                    new { TicketId = ticketId },
                    commandType: CommandType.StoredProcedure);
                return results.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<SupportTicketHistory>();
            }
        }

        public async Task<List<string>> GetEligibleAssigneesAsync(int ticketId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var results = await conn.QueryAsync<string>(
                    "usp_GetEligibleAssignees",
                    new { TicketId = ticketId },
                    commandType: CommandType.StoredProcedure);
                return results.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<string>();
            }
        }

        public async Task<bool> UpdateTicketAssignmentAsync(int id, string assignedTo, string changedBy)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var rows = await conn.QuerySingleAsync<int>(
                    "usp_UpdateTicketAssignment",
                    new { Id = id, AssignedTo = assignedTo, ChangedBy = changedBy ?? "System" },
                    commandType: CommandType.StoredProcedure);
                return rows > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<List<IrfMatchVM>> GetIRFsForSenderAsync(string senderEmail)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var results = await conn.QueryAsync<IrfMatchVM>(
                    "usp_GetIRFsForSender",
                    new { SenderEmail = senderEmail },
                    commandType: CommandType.StoredProcedure);
                return results.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<IrfMatchVM>();
            }
        }

        public async Task<IrfMatchVM> GetIRFByStringIdAsync(string irfStringId)
        {
            if (string.IsNullOrWhiteSpace(irfStringId))
                return null;

            try
            {
                using var conn = new SqlConnection(_connectionString);
                var row = await conn.QuerySingleOrDefaultAsync<IrfMatchVM>(
                    "usp_GetIRFByStringId",
                    new { IRFStringId = irfStringId.Trim() },
                    commandType: CommandType.StoredProcedure);
                return row;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<int> InsertCandidateResumeAsync(
            int    irfId,
            string resumeLink,
            int    addedBy,
            string clientHrEmailId,
            int?   supportTicketId = null)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var newId = await conn.QuerySingleAsync<int>(
                    "usp_InsertCandidateResume",
                    new
                    {
                        IRFId           = irfId,
                        ResumeLink      = resumeLink,
                        AddedBy         = addedBy,
                        ClientHREmailId = clientHrEmailId,
                        SupportTicketId = supportTicketId
                    },
                    commandType: CommandType.StoredProcedure);
                return newId;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<List<SupportTicketVM>> GetTicketsAsync(
            string    status      = null,
            string    intent      = null,
            string    senderEmail = null,
            DateTime? dateFrom    = null,
            DateTime? dateTo      = null,
            int       page        = 1,
            int       pageSize    = 25)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var results = await conn.QueryAsync<SupportTicketVM>(
                    "usp_GetSupportTickets",
                    new
                    {
                        Status      = string.IsNullOrWhiteSpace(status)      ? null : status,
                        Intent      = string.IsNullOrWhiteSpace(intent)      ? null : intent,
                        SenderEmail = string.IsNullOrWhiteSpace(senderEmail) ? null : senderEmail,
                        DateFrom    = dateFrom,
                        DateTo      = dateTo,
                        Page        = page,
                        PageSize    = pageSize
                    },
                    commandType: CommandType.StoredProcedure);
                return results.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<SupportTicketVM>();
            }
        }

        public async Task<List<SupportTicketVM>> GetSystemAssignedTicketsAsync(int page = 1, int pageSize = 25)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var results = await conn.QueryAsync<SupportTicketVM>(
                    "usp_GetSystemAssignedTickets",
                    new { Page = page, PageSize = pageSize },
                    commandType: CommandType.StoredProcedure);
                return results.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<SupportTicketVM>();
            }
        }

        public async Task<List<SupportTicketVM>> GetHybridTicketsAsync(int page = 1, int pageSize = 25)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var results = await conn.QueryAsync<SupportTicketVM>(
                    "usp_GetHybridTickets",
                    new { Page = page, PageSize = pageSize },
                    commandType: CommandType.StoredProcedure);
                return results.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<SupportTicketVM>();
            }
        }

        public async Task<IrfMatchVM> AutoMapUserToIRFAsync(string senderEmail, string jobTitle, string specialization = null)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var result = await conn.QueryFirstOrDefaultAsync<IrfMatchVM>(
                    "usp_AutoMapUserToIRF",
                    new
                    {
                        SenderEmail    = senderEmail,
                        JobTitle       = jobTitle,
                        Specialization = specialization
                    },
                    commandType: CommandType.StoredProcedure);
                return result;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<string> GetHRForSenderAsync(string senderEmail)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var result = await conn.QueryFirstOrDefaultAsync<string>(
                    "usp_GetHRForSender",
                    new { SenderEmail = senderEmail },
                    commandType: CommandType.StoredProcedure);
                return result;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<IrfMatchVM> FindBestIRFForClientAsync(string senderEmail, string jobTitle, string specialization = null, bool createMapping = true)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var result = await conn.QueryFirstOrDefaultAsync<IrfMatchVM>(
                    "usp_FindBestIRFForClient",
                    new
                    {
                        SenderEmail    = senderEmail,
                        JobTitle       = jobTitle,
                        Specialization = specialization,
                        CreateMapping  = createMapping
                    },
                    commandType: CommandType.StoredProcedure);
                return result;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<int> InsertCandidateResumeForAutoScheduleAsync(
            int irfId,
            string resumeLink,
            int addedBy,
            string clientHrEmailId,
            int? supportTicketId,
            string candidateName,
            string candidateEmail,
            DateTime? scheduledInterviewDateTime)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var result = await conn.QueryFirstOrDefaultAsync<int>(
                    "usp_InsertCandidateResumeForAutoSchedule",
                    new
                    {
                        IRFId                      = irfId,
                        ResumeLink                 = resumeLink,
                        AddedBy                    = addedBy,
                        ClientHREmailId            = clientHrEmailId,
                        SupportTicketId            = supportTicketId,
                        CandidateName              = candidateName,
                        CandidateEmail             = candidateEmail,
                        ScheduledInterviewDateTime = scheduledInterviewDateTime
                    },
                    commandType: CommandType.StoredProcedure);
                return result;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<List<AutoScheduleCandidateVM>> GetAutoScheduleCandidatesForTicketAsync(int ticketId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var results = await conn.QueryAsync<AutoScheduleCandidateVM>(
                    "usp_GetAutoScheduleCandidatesForTicket",
                    new { TicketId = ticketId },
                    commandType: CommandType.StoredProcedure);
                return results.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<AutoScheduleCandidateVM>();
            }
        }

        public async Task<bool> SetTicketHasAutoScheduleCandidatesAsync(int ticketId, bool value)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.ExecuteAsync(
                    "UPDATE dbo.SupportTickets SET HasAutoScheduleCandidates = @Value WHERE Id = @TicketId",
                    new { TicketId = ticketId, Value = value });
                return true;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<int> InsertPendingResumeAsync(
            int supportTicketId,
            string resumeUrl,
            string resumeFileName,
            string candidateName,
            string candidateEmail,
            int? resolvedIRFId,
            string resolvedIRFStringId,
            string resolvedJobTitle,
            string resolvedHREmail,
            bool hasSchedulingData,
            DateTime? scheduledInterviewDateTime,
            string scheduleTimezone)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var result = await conn.QueryFirstOrDefaultAsync<int>(
                    "usp_InsertPendingResume",
                    new
                    {
                        SupportTicketId            = supportTicketId,
                        ResumeUrl                  = resumeUrl,
                        ResumeFileName             = resumeFileName,
                        CandidateName              = candidateName,
                        CandidateEmail             = candidateEmail,
                        ResolvedIRFId              = resolvedIRFId,
                        ResolvedIRFStringId        = resolvedIRFStringId,
                        ResolvedJobTitle           = resolvedJobTitle,
                        ResolvedHREmail            = resolvedHREmail,
                        HasSchedulingData          = hasSchedulingData,
                        ScheduledInterviewDateTime = scheduledInterviewDateTime,
                        ScheduleTimezone           = scheduleTimezone
                    },
                    commandType: CommandType.StoredProcedure);
                return result;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<List<PendingResumeVM>> GetPendingResumesForTicketAsync(int ticketId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var results = await conn.QueryAsync<PendingResumeVM>(
                    "usp_GetPendingResumesForTicket",
                    new { TicketId = ticketId },
                    commandType: CommandType.StoredProcedure);
                return results.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<PendingResumeVM>();
            }
        }

        public async Task<PendingResumeVM> GetPendingResumeByIdAsync(int pendingResumeId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var sql = @"
                    SELECT
                        pr.Id, pr.SupportTicketId, pr.ResumeUrl, pr.CandidateName, pr.CandidateEmail,
                        pr.ResolvedIRFId, pr.ResolvedIRFStringId, pr.ResolvedJobTitle, pr.ResolvedHREmail,
                        pr.HasSchedulingData, pr.ScheduledInterviewDateTime, pr.ScheduleTimezone,
                        pr.Status, pr.IngestedResumeId, pr.ProcessedAt, pr.ProcessedBy, pr.CreatedAt,
                        irf.JDTiltle AS IRFJobTitle, cl.Name AS ClientName, cl.tenantId AS TenantId
                    FROM dbo.SupportTicketPendingResumes pr
                    LEFT JOIN dbo.IRF irf ON irf.id = pr.ResolvedIRFId
                    LEFT JOIN dbo.Client cl ON cl.id = irf.ClientId
                    WHERE pr.Id = @Id";
                var result = await conn.QueryFirstOrDefaultAsync<PendingResumeVM>(sql, new { Id = pendingResumeId });
                return result;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> UpdatePendingResumeStatusAsync(int pendingResumeId, string status, int? ingestedResumeId, string processedBy)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.ExecuteAsync(@"
                    UPDATE dbo.SupportTicketPendingResumes 
                    SET Status           = @Status, 
                        IngestedResumeId = @IngestedResumeId, 
                        ProcessedAt      = GETUTCDATE(), 
                        ProcessedBy      = @ProcessedBy 
                    WHERE Id = @Id",
                    new { Id = pendingResumeId, Status = status, IngestedResumeId = ingestedResumeId, ProcessedBy = processedBy });
                return true;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task RefreshTicketPendingResumeFlagAsync(int ticketId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.ExecuteAsync(@"
                    UPDATE dbo.SupportTickets 
                    SET HasPendingResumes = CASE 
                        WHEN EXISTS (SELECT 1 FROM dbo.SupportTicketPendingResumes WHERE SupportTicketId = @TicketId AND Status = 'Pending') 
                        THEN 1 ELSE 0 END 
                    WHERE Id = @TicketId",
                    new { TicketId = ticketId });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task<List<IRFOptionVM>> GetIRFsForClientAsync(string senderEmail)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var results = await conn.QueryAsync<IRFOptionVM>(
                    "usp_GetIRFsForClient",
                    new { SenderEmail = senderEmail },
                    commandType: CommandType.StoredProcedure);
                return results.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<IRFOptionVM>();
            }
        }

        public async Task<bool> UpdatePendingResumeIRFAsync(int pendingResumeId, int newIRFId, string changedBy, string reason)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                
                // Get IRF details for the new IRF
                var irf = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
                    SELECT irf.id, irf.IRFId AS IRFStringId, irf.JDTiltle, ul.EmailId AS HREmail
                    FROM dbo.IRF irf
                    LEFT JOIN dbo.UserList ul ON ul.id = irf.RisebirdHr
                    WHERE irf.id = @IRFId",
                    new { IRFId = newIRFId });
                
                if (irf == null)
                    return false;
                
                // Update the pending resume with new IRF and track the change
                await conn.ExecuteAsync(@"
                    UPDATE dbo.SupportTicketPendingResumes 
                    SET ResolvedIRFId       = @NewIRFId,
                        ResolvedIRFStringId = @IRFStringId,
                        ResolvedJobTitle    = @JDTitle,
                        ResolvedHREmail     = @HREmail,
                        IRFChangedByHR      = 1,
                        IRFChangedAt        = GETUTCDATE(),
                        IRFChangeReason     = @Reason
                    WHERE Id = @PendingResumeId",
                    new 
                    { 
                        PendingResumeId = pendingResumeId,
                        NewIRFId        = newIRFId,
                        IRFStringId     = (string)irf.IRFStringId,
                        JDTitle         = (string)irf.JDTiltle,
                        HREmail         = (string)irf.HREmail,
                        Reason          = reason
                    });
                
                return true;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<(int ManualReviewCount, int SystemOnlyCount, int HybridCount)> GetTicketCountsAsync()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var result = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "usp_GetTicketCounts",
                    commandType: CommandType.StoredProcedure);
                
                if (result == null)
                    return (0, 0, 0);
                
                return (
                    (int)result.ManualReviewCount,
                    (int)result.SystemOnlyCount,
                    (int)result.HybridCount
                );
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return (0, 0, 0);
            }
        }

        public async Task<int> GetHRUserIdForIRFAsync(int irfId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var hrId = await conn.QueryFirstOrDefaultAsync<int?>(
                    "SELECT RisebirdHr FROM dbo.IRF WHERE id = @IRFId AND ISNULL(IsDeleted, 0) = 0",
                    new { IRFId = irfId });
                return hrId ?? 0;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<int> GetSystemUserIdAsync()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                // Get any active HR-level user (idProfile = 3 is typically HR) as fallback
                var userId = await conn.QueryFirstOrDefaultAsync<int?>(
                    @"SELECT TOP 1 id FROM dbo.UserList 
                      WHERE IsActive = 1 AND idProfile IN (3, 1, 2)
                      ORDER BY idProfile ASC, id ASC");
                return userId ?? 0;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<string> GetTenantIdForIRFAsync(int irfId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var tenantId = await conn.QueryFirstOrDefaultAsync<string>(
                    @"SELECT c.tenantId 
                      FROM dbo.IRF irf
                      INNER JOIN dbo.Client c ON c.id = irf.ClientId
                      WHERE irf.id = @IRFId",
                    new { IRFId = irfId });
                return tenantId ?? string.Empty;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return string.Empty;
            }
        }

        public async Task<int> LogProcessingCheckpointAsync(
            int? supportTicketId,
            int? pendingResumeId,
            int? candidateResumeId,
            string checkpoint,
            string status,
            string message = null,
            string details = null,
            string processedBy = null,
            int? durationMs = null)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var logId = await conn.QueryFirstOrDefaultAsync<int>(
                    "usp_InsertProcessingLog",
                    new
                    {
                        SupportTicketId    = supportTicketId,
                        PendingResumeId    = pendingResumeId,
                        CandidateResumeId  = candidateResumeId,
                        Checkpoint         = checkpoint,
                        Status             = status,
                        Message            = message,
                        Details            = details,
                        ProcessedBy        = processedBy,
                        DurationMs         = durationMs
                    },
                    commandType: CommandType.StoredProcedure);
                return logId;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<List<ProcessingLogVM>> GetProcessingLogsAsync(int? supportTicketId = null, int? pendingResumeId = null, int limit = 100)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var logs = await conn.QueryAsync<ProcessingLogVM>(
                    "usp_GetProcessingLogs",
                    new
                    {
                        SupportTicketId = supportTicketId,
                        PendingResumeId = pendingResumeId,
                        Limit           = limit
                    },
                    commandType: CommandType.StoredProcedure);
                return logs.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<ProcessingLogVM>();
            }
        }

        public async Task<List<TemplateMatchVM>> GetTemplatesForSpecializationAsync(string primarySpecialization)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var templates = await conn.QueryAsync<TemplateMatchVM>(
                    "usp_GetTemplatesForPrimarySpecialization",
                    new { PrimarySpecialization = primarySpecialization },
                    commandType: CommandType.StoredProcedure);
                return templates.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<TemplateMatchVM>();
            }
        }

        public async Task<SupportTicketVM> GetTicketByIdAsync(int ticketId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                const string sql = @"
                    SELECT 
                        t.Id,
                        t.SenderEmail,
                        t.Subject,
                        t.Intent,
                        t.PrimarySpecialization,
                        t.AttachmentJobDescription,
                        t.AttachmentPdf,
                        t.AttachmentResumes,
                        t.DriveFolderLink,
                        t.SummaryMail,
                        t.Status,
                        t.AssignedTo,
                        t.JobTitle,
                        t.HasPendingResumes,
                        t.ReceivedAt
                    FROM SupportTicket t
                    WHERE t.Id = @TicketId";
                return await conn.QueryFirstOrDefaultAsync<SupportTicketVM>(sql, new { TicketId = ticketId });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
    }
}
