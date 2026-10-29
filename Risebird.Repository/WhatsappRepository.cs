using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    public class WhatsappRepository : IWhatsappRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private IApplicationErrorLogger _applicationErrorLogger;
        public WhatsappRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<bool> SaveSentWhatsappMessageDetails(SentWhatsAppMessageDetails model)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Recipient", model.Recipient);
                    parameters.Add("@CountryCode", model.CountryCode);
                    parameters.Add("@WhatsappNumber", model.WhatsappNumber);
                    parameters.Add("@Message", model.Message);
                    parameters.Add("@ButtonLink", model.ButtonLink);
                    parameters.Add("@Event", model.Event);
                    parameters.Add("@IRFId", model.IRFId);
                    parameters.Add("@InterviewId", model.InterviewId);
                    parameters.Add("@InterviewDate", model.InterviewDate);
                    parameters.Add("@Header", model.Header);
                    parameters.Add("@CreatedAt", DateTime.Now);
                    parameters.Add("@SentBy", model.SentBy);
                    parameters.Add("@Status", model.Status ?? "Sent");

                    await SqlMapper.QueryAsync(con, "usp_SaveSentWhatsappMessageDetails", parameters,
                         commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }

        // Addresses in unsubscribe URLs arrive trimmed or not, any case, and with '+' decoded to a space.
        public static string NormalizeEmail(string email)
        {
            return string.IsNullOrWhiteSpace(email) ? null : email.Trim().Replace(" ", "+").ToLowerInvariant();
        }

        public async Task<bool> IsOptedOutAsync(string audience, string email, int? irfId, int? prospectId = null)
        {
            try
            {
                var query = @"SELECT TOP 1 1 FROM dbo.WhatsAppOptOut
                              WHERE Audience = @Audience
                                AND ((@EmailId IS NOT NULL AND EmailId = @EmailId) OR (@ProspectId IS NOT NULL AND ProspectId = @ProspectId))
                                AND (IRFId IS NULL OR IRFId = @IRFId)";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await con.QueryFirstOrDefaultAsync<int?>(query, new
                    {
                        Audience = audience,
                        EmailId = NormalizeEmail(email),
                        ProspectId = prospectId,
                        IRFId = irfId.HasValue && irfId.Value > 0 ? irfId : null
                    });
                    return result.HasValue;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }

        public async Task<bool> SaveOptOutAsync(string audience, string email, int? irfId, int? prospectId, string source)
        {
            try
            {
                var query = @"IF NOT EXISTS (SELECT 1 FROM dbo.WhatsAppOptOut
                                             WHERE Audience = @Audience
                                               AND ISNULL(EmailId, '') = ISNULL(@EmailId, '')
                                               AND ISNULL(ProspectId, 0) = ISNULL(@ProspectId, 0)
                                               AND ISNULL(IRFId, 0) = ISNULL(@IRFId, 0))
                                  INSERT INTO dbo.WhatsAppOptOut (Audience, EmailId, ProspectId, IRFId, Source)
                                  VALUES (@Audience, @EmailId, @ProspectId, @IRFId, @Source)";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await con.ExecuteAsync(query, new
                    {
                        Audience = audience,
                        EmailId = NormalizeEmail(email),
                        ProspectId = prospectId,
                        IRFId = irfId.HasValue && irfId.Value > 0 ? irfId : null,
                        Source = source
                    });
                    return true;
                }
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                // Two clicks raced past the existence check; the opt-out is recorded either way.
                return true;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }

        public async Task<int> CountSentAsync(string email, int irfId, string eventName)
        {
            try
            {
                var query = @"SELECT COUNT(*) FROM dbo.SentWhatsAppMessageDetails
                              WHERE Recipient = @Recipient AND IRFId = @IRFId AND [Event] = @Event
                                AND ([Status] = 'Sent' OR [Status] IS NULL)   -- NULL = written before Status existed";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await con.ExecuteScalarAsync<int>(query, new { Recipient = email?.Trim(), IRFId = irfId, Event = eventName });
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return 0;
        }

        public async Task<string> FetchUniqueIdByEmailAsync(string email)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await con.QueryFirstOrDefaultAsync<string>("SELECT TOP 1 uniqueid FROM UserList WHERE EmailId = @EmailId", new { EmailId = email?.Trim() });
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<string> FetchEmailByUniqueIdAsync(string uniqueId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await con.QueryFirstOrDefaultAsync<string>("SELECT TOP 1 EmailId FROM UserList WHERE uniqueid = @UniqueId", new { UniqueId = uniqueId?.Trim() });
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<int> FetchIrfIdByInterviewIdAsync(int interviewId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await con.QueryFirstOrDefaultAsync<int?>(
                        "SELECT TOP 1 IRF.id FROM InterviewMeetings im JOIN IRF ON IRF.IRFId = im.IRFId WHERE im.id = @InterviewId",
                        new { InterviewId = interviewId }) ?? 0;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return 0;
        }
    }
}
