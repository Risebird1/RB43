using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Risebird.Entities.Data;

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
    }
}
