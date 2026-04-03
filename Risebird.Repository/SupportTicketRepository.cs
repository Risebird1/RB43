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
    }
}
