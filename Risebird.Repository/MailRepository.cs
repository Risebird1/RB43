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
using System.Threading.Tasks;

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

        public async Task<IEnumerable<MailRecipientActivityDetailsVM>> GetEmailRecipientsLatestActivity(string recipientType, DateTime? startDate = null, DateTime? endDate = null)
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
                    var result = await SqlMapper.QueryAsync<MailRecipientActivityDetailsVM>(con, "usp_GetEmailRecipientsLatestActivity", parameters, commandTimeout: 600,
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
        /// gets events/activities associated with a particular email sent to a recipient
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        public async Task<IEnumerable<MailRecipientActivityDetailsVM>> GetRecipientMailActivitiesByMessageId(string recipientEmail, string messageId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@recipientEmail", recipientEmail);
                parameters.Add("@startDate", startDate);
                parameters.Add("@endDate", endDate);
                parameters.Add("@timeNow", DateTime.Now);
                parameters.Add("@messageId", messageId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<MailRecipientActivityDetailsVM>(con, "usp_GetRecipientMailActivitiesByMessageId", parameters,
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
        public async Task<IEnumerable<MailRecipientActivityDetailsVM>> GetInterviewerMailActivityForAvailabilityByIRFId(int irfId)
        {
            try
            {
                var parameters = new DynamicParameters();
                //parameters.Add("@recipientEmail", recipientEmail);
                //parameters.Add("@startDate", startDate);
                //parameters.Add("@endDate", endDate);
                //parameters.Add("@timeNow", DateTime.Now);
                //parameters.Add("@messageId", messageId);
                parameters.Add("@irfId", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<MailRecipientActivityDetailsVM>(con, "usp_GetInterviewerMailActivityForAvailabilityByIRFId", parameters,
                        commandType: CommandType.StoredProcedure, commandTimeout:300);
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
