using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Risebird.Repository.Interface;
using Risebird.Entities.ViewModel;

namespace Risebird.Repository
{
    public class MailRepository : IMailRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private IApplicationErrorLogger _applicationErrorLogger;
        public MailRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }
        /// <summary>
        /// gets all the distinct recipients - by profile - who have recieved an email in the last 30 days
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        public async Task<IEnumerable<SendgridEventData>> GetRecipients(string recipientType, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@recipientType", recipientType);
                parameters.Add("@startDate", startDate);
                parameters.Add("@endDate", endDate);
                parameters.Add("@timeNow", DateTime.Now);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<SendgridEventData>(con, "usp_GetEmailRecipients", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        /// <summary>
        /// gets all the distinct recipients - by profile - who have recieved an email in the last 30 days
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        public async Task<IEnumerable<MailRecipientActivityDetailsVM>> GetEmailRecipientActivities(string recipientEmail, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@recipientEmail", recipientEmail);
                parameters.Add("@startDate", startDate);
                parameters.Add("@endDate", endDate);
                parameters.Add("@timeNow", DateTime.Now);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<MailRecipientActivityDetailsVM>(con, "usp_GetEmailRecipientActivities", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
    }
}
