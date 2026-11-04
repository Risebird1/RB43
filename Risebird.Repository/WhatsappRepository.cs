using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
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
                                AND (IRFId IS NULL OR IRFId = @IRFId)
                                AND ReEnabledAt IS NULL   -- re-enabled rows are history";
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

        public async Task<bool> SaveOptOutAsync(string audience, string email, int? irfId, int? prospectId, string source, string templateEvent = null)
        {
            try
            {
                var query = @"IF NOT EXISTS (SELECT 1 FROM dbo.WhatsAppOptOut
                                             WHERE Audience = @Audience
                                               AND ISNULL(EmailId, '') = ISNULL(@EmailId, '')
                                               AND ISNULL(ProspectId, 0) = ISNULL(@ProspectId, 0)
                                               AND ISNULL(IRFId, 0) = ISNULL(@IRFId, 0)
                                               AND ReEnabledAt IS NULL)
                                  INSERT INTO dbo.WhatsAppOptOut (Audience, EmailId, ProspectId, IRFId, Source, TemplateEvent)
                                  VALUES (@Audience, @EmailId, @ProspectId, @IRFId, @Source, @TemplateEvent)";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await con.ExecuteAsync(query, new
                    {
                        Audience = audience,
                        EmailId = NormalizeEmail(email),
                        ProspectId = prospectId,
                        IRFId = irfId.HasValue && irfId.Value > 0 ? irfId : null,
                        Source = source,
                        TemplateEvent = string.IsNullOrWhiteSpace(templateEvent) ? null : Cut(templateEvent.Trim(), 100)
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

        public async Task<List<WhatsAppSendStatusRow>> GetSendStatusAsync(IEnumerable<(string Email, int IRFId)> keys, string eventName)
        {
            var list = keys?.Where(k => !string.IsNullOrWhiteSpace(k.Email) && k.IRFId > 0)
                            .Select(k => new { e = k.Email.Trim(), i = k.IRFId }).Distinct().ToList();
            if (list == null || list.Count == 0) return new List<WhatsAppSendStatusRow>();
            try
            {
                // Same rules as IsOptedOutAsync and CountSentAsync, for the whole list in one round trip.
                const string query = @"
                    SELECT j.Email, j.IRFId,
                        CAST(CASE WHEN EXISTS (
                            SELECT 1 FROM dbo.WhatsAppOptOut o
                            WHERE o.Audience = 'Candidate'
                              AND o.EmailId = LOWER(REPLACE(j.Email, ' ', '+'))
                              AND (o.IRFId IS NULL OR o.IRFId = j.IRFId)
                              AND o.ReEnabledAt IS NULL) THEN 1 ELSE 0 END AS BIT) AS OptedOut,
                        (SELECT COUNT(*) FROM dbo.SentWhatsAppMessageDetails s
                         WHERE s.Recipient = j.Email AND s.IRFId = j.IRFId AND s.[Event] = @Event
                           AND (s.[Status] = 'Sent' OR s.[Status] IS NULL)) AS SendsUsed
                    FROM OPENJSON(@Keys) WITH (Email NVARCHAR(256) '$.e', IRFId INT '$.i') j";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var rows = await con.QueryAsync<WhatsAppSendStatusRow>(query,
                        new { Keys = Newtonsoft.Json.JsonConvert.SerializeObject(list), Event = eventName }, commandTimeout: 120);
                    return rows.ToList();
                }
            }
            catch (Exception ex)
            {
                // The buttons then stay enabled; the send itself still refuses opted-out and at-limit candidates.
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<WhatsAppSendStatusRow>();
            }
        }

        private const string OptOutColumns = @"o.Id, o.Audience, o.EmailId, o.ProspectId, o.IRFId, irf.IRFId AS IRFName, o.Source,
                    o.TemplateEvent, o.Reason, o.CreatedAt, o.ReEnabledAt, o.ReEnabledBy";

        public async Task<List<WhatsAppOptOutRow>> SearchOptOutsAsync(string search, bool includeHistory)
        {
            try
            {
                string query = @"
                    SELECT TOP (200) " + OptOutColumns + @"
                    FROM dbo.WhatsAppOptOut o
                    LEFT JOIN dbo.IRF irf ON irf.id = o.IRFId
                    WHERE (@IncludeHistory = 1 OR o.ReEnabledAt IS NULL)
                      AND (@Search IS NULL
                       OR o.EmailId LIKE '%' + @Search + '%'
                       OR irf.IRFId LIKE '%' + @Search + '%'
                       OR CAST(o.ProspectId AS VARCHAR(20)) = @Search)
                    ORDER BY o.Id DESC";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var rows = await con.QueryAsync<WhatsAppOptOutRow>(query, new
                    {
                        Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim().ToLowerInvariant(),
                        IncludeHistory = includeHistory
                    });
                    return rows.ToList();
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<WhatsAppOptOutRow>();
            }
        }

        public async Task<WhatsAppOptOutRow> GetOptOutAsync(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await con.QueryFirstOrDefaultAsync<WhatsAppOptOutRow>(@"
                        SELECT " + OptOutColumns + @"
                        FROM dbo.WhatsAppOptOut o LEFT JOIN dbo.IRF irf ON irf.id = o.IRFId
                        WHERE o.Id = @Id", new { Id = id });
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> ReEnableOptOutAsync(int id, string reEnabledBy)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    // Kept as history: the opt-out and its reversal both stay visible.
                    return await con.ExecuteAsync(@"
                        UPDATE dbo.WhatsAppOptOut SET ReEnabledAt = GETUTCDATE(), ReEnabledBy = @By
                        WHERE Id = @Id AND ReEnabledAt IS NULL", new { Id = id, By = Cut(reEnabledBy, 150) }) > 0;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> SetOptOutReasonAsync(string audience, string email, int? irfId, string reason)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await con.ExecuteAsync(@"
                        UPDATE dbo.WhatsAppOptOut SET Reason = @Reason
                        WHERE Audience = @Audience AND EmailId = @EmailId
                          AND ISNULL(IRFId, 0) = ISNULL(@IRFId, 0)
                          AND ReEnabledAt IS NULL", new
                    {
                        Audience = audience,
                        EmailId = NormalizeEmail(email),
                        IRFId = irfId.HasValue && irfId.Value > 0 ? irfId : null,
                        Reason = Cut(reason?.Trim(), 300)
                    }) > 0;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public Task MarkCandidateColdAsync(string email, int? irfId, string markedBy, string coldInfo, string coldSource) =>
            MarkColdAsync(email, irfId, true, markedBy, coldInfo, coldSource);

        public Task UndoCandidateColdAsync(string email, int? irfId, string undoneBy) =>
            MarkColdAsync(email, irfId, false, undoneBy, null, null);

        private async Task MarkColdAsync(string email, int? irfId, bool mark, string by, string coldInfo, string coldSource)
        {
            if (string.IsNullOrWhiteSpace(email)) return;
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await con.ExecuteAsync("usp_MarkCandidateColdForWhatsAppOptOut", new
                    {
                        Email = email.Trim(),
                        IRFId = irfId.HasValue && irfId.Value > 0 ? irfId : null,
                        Mark = mark,
                        MarkedBy = Cut(by, 100),
                        ColdInfo = Cut(coldInfo, 100),
                        ColdSource = Cut(coldSource, 150),
                        Now = DateTime.Now
                    }, commandType: CommandType.StoredProcedure);
                }
            }
            catch (Exception ex)
            {
                // The opt-out itself is already saved; a failed cold mark is only logged.
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task<bool> UpdateColdReasonAsync(string email, int irfId, string reason)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    // Only a cold mark the unsubscribe made, so a reason never overwrites an ops user's note.
                    return await con.ExecuteAsync(@"
                        UPDATE cci SET ColdReason = @Reason, ColdInfo = @ColdInfo
                        FROM dbo.ColdCandidateInfos cci
                        INNER JOIN dbo.Candidate c ON c.id = cci.FKCandidateId
                        INNER JOIN dbo.CandidatesMaster cm ON cm.id = c.CandidateMasterId
                        INNER JOIN dbo.UserList u ON u.id = cm.UserId
                        WHERE u.EmailId = @Email AND c.IRFId = @IRFId
                          AND cci.IsColdCandidate = 1 AND cci.ColdMarkedBY = 'WhatsApp unsubscribe'",
                        new
                        {
                            Email = email?.Trim(),
                            IRFId = irfId,
                            Reason = Cut(reason, 300),
                            ColdInfo = Cut("WhatsApp unsubscribe: " + reason, 100)
                        }) > 0;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task MarkInterviewerChurnAsync(string email, string churnSource)
        {
            if (string.IsNullOrWhiteSpace(email)) return;
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    // A new opt-out starts a fresh churn record; the reason follows from the unsubscribe page.
                    await con.ExecuteAsync(@"
                        UPDATE ipr SET ChurnSource = @Source, ChurnReason = NULL, ChurnDate = GETDATE()
                        FROM dbo.InterviewerProfile ipr
                        INNER JOIN dbo.UserList u ON u.id = ipr.UserId
                        WHERE u.EmailId = @Email",
                        new { Email = email.Trim(), Source = Cut(churnSource, 150) });
                }
            }
            catch (Exception ex)
            {
                // The opt-out itself is already saved; a failed churn mark is only logged.
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task<bool> SetInterviewerChurnReasonAsync(string email, string reason)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await con.ExecuteAsync(@"
                        UPDATE ipr SET ChurnReason = @Reason
                        FROM dbo.InterviewerProfile ipr
                        INNER JOIN dbo.UserList u ON u.id = ipr.UserId
                        WHERE u.EmailId = @Email AND ipr.ChurnSource LIKE 'WhatsApp%'",
                        new { Email = email?.Trim(), Reason = Cut(reason, 300) }) > 0;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task UndoInterviewerChurnAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return;
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await con.ExecuteAsync(@"
                        UPDATE ipr SET ChurnSource = NULL, ChurnReason = NULL, ChurnDate = NULL
                        FROM dbo.InterviewerProfile ipr
                        INNER JOIN dbo.UserList u ON u.id = ipr.UserId
                        WHERE u.EmailId = @Email AND ipr.ChurnSource LIKE 'WhatsApp%'",
                        new { Email = email.Trim() });
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task<List<WhatsAppNumberOwner>> FindUsersByWhatsAppNumberAsync(string lastDigits)
        {
            if (string.IsNullOrWhiteSpace(lastDigits) || lastDigits.Length < 8) return new List<WhatsAppNumberOwner>();
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    // Numbers are stored in many shapes (+91 98765 43210, 919876543210, 9876543210): match on the
                    // number the sends use (WhatsappNumber, else MobileNumber), ignoring spaces, dashes and the country code.
                    var rows = await con.QueryAsync<WhatsAppNumberOwner>(@"
                        SELECT DISTINCT u.EmailId AS Email,
                            CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.CandidatesMaster cm WHERE cm.UserId = u.id) THEN 1 ELSE 0 END AS BIT) AS IsCandidate
                        FROM dbo.UserList u
                        WHERE (u.WhatsappNumber LIKE '%' + @Digits + '%' OR u.MobileNumber LIKE '%' + @Digits + '%'
                               OR REPLACE(REPLACE(ISNULL(u.WhatsappNumber, u.MobileNumber), ' ', ''), '-', '') LIKE '%' + @Digits)
                          AND RIGHT(REPLACE(REPLACE(REPLACE(ISNULL(u.WhatsappNumber, u.MobileNumber), ' ', ''), '-', ''), '+', ''), LEN(@Digits)) = @Digits
                          AND u.EmailId IS NOT NULL",
                        new { Digits = lastDigits }, commandTimeout: 10);
                    return rows.ToList();
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<WhatsAppNumberOwner>();
            }
        }

        private static string Cut(string s, int max) =>
            string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max);

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
