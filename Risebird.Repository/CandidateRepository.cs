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
using System.Threading.Tasks;

namespace Risebird.Repository
{
    public class CandidateRepository : ICandidateRepository
    {

        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private IApplicationErrorLogger _applicationErrorLogger;
        public CandidateRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<IEnumerable<CandidateTimelineData>> GetCandidateTimeline(string emailId = null)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@EmailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<CandidateTimelineData>(con, "usp_GetCandidateTimeline", parameters,
                        commandType: CommandType.StoredProcedure);
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
            return null;
        }
        public async Task<IEnumerable<CandidateTimelineData>> GetCandidateTimelinebyDate(DateTime startDate, DateTime endDate, string emailId = null)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@EmailId", emailId);
                parameters.Add("@startDate", startDate);
                parameters.Add("@endDate", endDate);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<CandidateTimelineData>(con, "usp_GetCandidateTimelineByDate", parameters,
                        commandType: CommandType.StoredProcedure);
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
            return null;
        }


        public async Task<IEnumerable<CandidateLastStatus>> GetCandidateLastStatus()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@dateValue", DateTime.Now.AddDays(-1));
                    var result = SqlMapper.Query<CandidateLastStatus>(con, "usp_GetCandidateLastStatus", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result;
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
            return null;
        }
    }
}
