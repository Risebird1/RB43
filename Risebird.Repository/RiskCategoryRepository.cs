using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using static System.Data.CommandType;

namespace Risebird.Repository
{
    public class RiskCategoryRepository : IRiskCategoryRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public RiskCategoryRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<IEnumerable<RiskCategory>> GetRiskCategories(DateTime startDate, DateTime endDate)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@StartDate", startDate);
                parameters.Add("@EndDate", endDate);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<RiskCategory>(
                        con, "usp_GetRiskCategories", parameters, commandType: StoredProcedure);
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

        public async Task<RiskCategory> GetRiskCategoryByInterviewId(int interviewId)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@InterviewId", interviewId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryFirstOrDefaultAsync<RiskCategory>(
                        con, "usp_GetRiskCategoryByInterviewId", parameters, commandType: StoredProcedure);
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

        public async Task<bool> AddUpdateRiskCategory(int interviewId)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@InterviewId", interviewId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.ExecuteAsync(
                        con, "usp_AddUpdateRiskCategory", parameters, commandType: StoredProcedure);
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
