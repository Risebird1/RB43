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
        private string connectionString;//readonly
        private readonly string connectionStringForTesting;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public SendGridRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
            connectionStringForTesting = "Server=tcp:risebirdtest.database.windows.net,1433;Initial Catalog=productTestProdDB;Persist Security Info=False;User ID=risebirdtestadmin;Password=Ri$£bird@567;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

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

        public async Task<bool> SaveSentMailDetails(SentMailDetails model, bool isStaging = false)
        {
            try
            {
                if (isStaging)
                {
                    connectionString = connectionStringForTesting;
                }
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    if (model.InterviewIdList != null)
                    {
                        //use bulk insert
                        DataTable sentMailDt = new DataTable();
                        sentMailDt.Columns.Add("To", typeof(string));
                        sentMailDt.Columns.Add("Cc", typeof(string));
                        sentMailDt.Columns.Add("Bcc", typeof(string));
                        sentMailDt.Columns.Add("MailBody", typeof(string));
                        sentMailDt.Columns.Add("MailSubject", typeof(string));
                        sentMailDt.Columns.Add("sg_message_id", typeof(string));
                        sentMailDt.Columns.Add("IRFId", typeof(int));
                        sentMailDt.Columns.Add("InterviewId", typeof(int));
                        sentMailDt.Columns.Add("CreatedAt", typeof(DateTime));
                        sentMailDt.Columns.Add("SentBy", typeof(string));


                        foreach (var item in model.InterviewIdList)
                        {
                            sentMailDt.Rows.Add(model.To, model.Cc, model.Bcc, model.MailBody, model.MailSubject, model.sg_message_id, null, item, DateTime.Now, model.SentBy);
                        }

                        SqlCommand cmd = new SqlCommand("usp_SaveSentMailDetailsBulkInsert", con);
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add(new SqlParameter("@SentMailDetailsBulkData", SqlDbType.Structured));
                        cmd.Parameters["@SentMailDetailsBulkData"].Value = sentMailDt;

                        con.Open();

                        if (await cmd.ExecuteNonQueryAsync() != 0)
                        {
                            con.Close();
                        }

                        return true;

                    }
                    else
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
                        parameters.Add("@SentBy", model.SentBy);
                        parameters.Add("@IRFName", model.IRFName);
                        parameters.Add("@AssessmentGroupId", model.AssessmentGroupId);
                        parameters.Add("@CampaignId", model.CampaignId);
                        parameters.Add("@IsACS", false);

                        await SqlMapper.QueryAsync(con, "usp_SaveSentMailDetails", parameters,
                             commandType: CommandType.StoredProcedure);
                        return true;

                    }

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
