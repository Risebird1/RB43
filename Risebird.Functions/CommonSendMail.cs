using Dapper;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Functions
{
    public class CommonSendMail
    {
        private readonly string _sendGridClientKey = "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";
        //public string SendMail(SendGridMessage msg, )
        //{
        //    try
        //    {
        //        var mailFrom = "notification@risebird.io";
        //        msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
        //        var client = new SendGridClient(_sendGridClientKey);

        //        var response = client.SendEmailAsync(msg).Result;
        //        if (response.StatusCode != HttpStatusCode.Accepted)
        //        {
        //            return "Failed to send mail. Please check all fields and try again.";

        //        }
        //        return "Mail sent successfully.";
        //    }
        //    catch (Exception ex)
        //    {
        //        return "Failed to send mail. Please check all fields and try again.";
        //    }
        //}
        public async Task<string> SendMail(SendGridMessage msg, int? IRFId = null, int? interviewId = null)
        {
            try
            {
                // ── Hard suppression: an inactive user (UserList.IsActive = 0) must NEVER be emailed ──
                if (msg.Personalizations != null && msg.Personalizations.Count > 0)
                {
                    var allRecipients = new List<string>();
                    foreach (var p in msg.Personalizations)
                    {
                        if (p.Tos != null) allRecipients.AddRange(p.Tos.Select(x => x.Email));
                        if (p.Ccs != null) allRecipients.AddRange(p.Ccs.Select(x => x.Email));
                        if (p.Bccs != null) allRecipients.AddRange(p.Bccs.Select(x => x.Email));
                    }

                    var inactiveRecipients = await GetInactiveRecipientEmailsAsync(allRecipients);
                    if (inactiveRecipients.Count > 0)
                    {
                        bool IsInactive(SendGrid.Helpers.Mail.EmailAddress e) =>
                            e != null && !string.IsNullOrWhiteSpace(e.Email) && inactiveRecipients.Contains(e.Email.Trim());

                        foreach (var p in msg.Personalizations)
                        {
                            if (p.Tos != null) p.Tos = p.Tos.Where(x => !IsInactive(x)).ToList();
                            if (p.Ccs != null) p.Ccs = p.Ccs.Where(x => !IsInactive(x)).ToList();
                            if (p.Bccs != null) p.Bccs = p.Bccs.Where(x => !IsInactive(x)).ToList();
                        }

                        msg.Personalizations = msg.Personalizations
                            .Where(p => p.Tos != null && p.Tos.Count > 0)
                            .ToList();

                        if (msg.Personalizations.Count == 0)
                            return "Mail not sent: recipient is inactive.";
                    }
                }

                var client = new SendGridClient(_sendGridClientKey);

                var response = await client.SendEmailAsync(msg);
                if (response.StatusCode != HttpStatusCode.Accepted)
                {
                    return "Failed to send mail. Please check all fields and try again.";

                }
                SentMailDetails obj = new SentMailDetails();
                IEnumerable<string> values;
                //string message_id; 
                if (response.Headers.TryGetValues("X-Message-Id", out values))
                {
                    obj.sg_message_id = values?.FirstOrDefault();
                }

                obj.MailSubject = msg.Subject;
                StringBuilder tos = new StringBuilder();
                StringBuilder ccs = new StringBuilder();
                StringBuilder bccs = new StringBuilder();

                if (msg.Personalizations != null)//foreach (var item in msg.Personalizations)
                {
                    var item = msg.Personalizations[0];
                    if (tos.Length == 0)
                    {
                        if (item.Tos != null)
                        {
                            tos.Append(string.Join(",", item.Tos.Select(x => x.Email)));
                        }
                    }
                    else
                    {
                        tos = tos.Append(",");
                        tos.Append(string.Join(",", item.Tos.Select(x => x.Email)));
                    }
                    if (ccs.Length == 0)
                    {
                        if (item.Ccs != null)
                        {
                            ccs.Append(string.Join(",", item.Ccs.Select(x => x.Email)));
                        }
                    }
                    else
                    {
                        ccs = ccs.Append(",");
                        ccs.Append(string.Join(",", item.Ccs.Select(x => x.Email)));
                    }
                    if (bccs.Length == 0)
                    {
                        if (item.Bccs != null)
                        {
                            bccs.Append(string.Join(",", item.Bccs.Select(x => x.Email)));
                        }
                    }
                    else
                    {
                        bccs = bccs.Append(",");
                        bccs.Append(string.Join(",", item.Bccs.Select(x => x.Email)));
                    }
                }
                obj.To = tos != null ? tos.ToString() : null;
                obj.Cc = ccs != null ? ccs.ToString() : null;
                obj.Bcc = bccs != null ? bccs.ToString() : null;
                obj.MailBody = msg.Contents?[0].Value;
                obj.IRFId = IRFId;
                obj.InterviewId = interviewId;
                obj.SentBy = "System";
                _ = await SaveSentMailDetails(obj);
                return "Mail sent successfully.";
            }
            catch (Exception ex)
            {
                //log?.Error(ex.Message);
                return "Failed to send mail!";
            }
        }
        private async Task<HashSet<string>> GetInactiveRecipientEmailsAsync(IEnumerable<string> emails)
        {
            var inactive = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var distinctEmails = (emails ?? Enumerable.Empty<string>())
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Select(e => e.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (distinctEmails.Count == 0)
                    return inactive;

                var connectionString = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var rows = await SqlMapper.QueryAsync<string>(con,
                        "SELECT DISTINCT LTRIM(RTRIM(EmailId)) FROM UserList WHERE ISNULL(IsActive, 1) = 0 AND LTRIM(RTRIM(EmailId)) IN @Emails",
                        new { Emails = distinctEmails });

                    foreach (var row in rows)
                    {
                        if (!string.IsNullOrWhiteSpace(row))
                            inactive.Add(row.Trim());
                    }
                }
            }
            catch (Exception)
            {
                // Fail open only for the lookup itself; never throw from the suppression guard.
            }
            return inactive;
        }

        public async Task<bool> SaveSentMailDetails(SentMailDetails model)
        {
            try
            {
                var connectionString = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@To", model.To);
                    parameters.Add("@Cc", model.Cc);
                    parameters.Add("@Bcc", model.Bcc);
                    parameters.Add("@MailBody", model.MailBody);
                    parameters.Add("@MailSubject", model.MailSubject);
                    parameters.Add("@sg_message_id", model.sg_message_id);
                    parameters.Add("@IRFId", model.IRFId);
                    parameters.Add("@InterviewId", model.InterviewId);
                    parameters.Add("@CreatedAt", DateTime.Now);

                    await SqlMapper.QueryAsync(con, "usp_SaveSentMailDetails", parameters,
                         commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                return false;
                //log?.Error(ex.Message);
            }
            catch (Exception exe)
            {
                return false;
                //log?.Error(ex.Message);
            }

        }
    }
}
