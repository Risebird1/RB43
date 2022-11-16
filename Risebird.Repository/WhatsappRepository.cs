using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
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
    }
}
