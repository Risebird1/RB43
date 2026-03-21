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
    }
}
