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
    public class SendGridRepository : ISendGridRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public SendGridRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }
        public async Task<bool> SaveSendGridEventDetails(SendgridEventData model)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@email", model.email);
                    parameters.Add("@event", model.@event);
                    parameters.Add("@timestamp", model.timestamp);
                    parameters.Add("@CreatedAt", DateTime.Now);
                    parameters.Add("@smtpid", model.smtpid);
                    parameters.Add("@ip", model.ip);
                    parameters.Add("@category", model.category);
                    parameters.Add("@sg_event_id", model.sg_event_id);
                    parameters.Add("@sg_message_id", model.sg_message_id);
                    parameters.Add("@response", model.response);
                    parameters.Add("@url", model.url);
                    parameters.Add("@useragent", model.useragent);
                    parameters.Add("@reason", model.reason);
                    parameters.Add("@status", model.status);
                    parameters.Add("@Split_Message_Id", model.Split_Message_Id);


                    await SqlMapper.QueryAsync(con, "usp_SaveSendGridEventDetails", parameters,
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

        public async Task<bool> SaveSentMailDetails(SentMailDetails model)
        {
            try
            {
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
                    parameters.Add("@SenntBy", model.SentBy);

                    await SqlMapper.QueryAsync(con, "usp_SaveSentMailDetails", parameters,
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
    }
}
