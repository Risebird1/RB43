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
    public class ActivityTrackerRepository : IActivityTrackerRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private IApplicationErrorLogger _applicationErrorLogger;
        public ActivityTrackerRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<IEnumerable<ActivityTrackerVM>> GetAllActivities(DateTime startDate, DateTime endDate)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("@startDate", startDate);
                    parameters.Add("@endDate", endDate);
                    return await SqlMapper.QueryAsync<ActivityTrackerVM>(con, "usp_GetAllActivities", parameters,
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

        public async Task<bool> AddActivity(ActivityTrackerVM model)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@ActivityType", model.ActivityType); // Normal parametersmeters           
                parameters.Add("@PerformedBy", model.PerformedBy);
                parameters.Add("@Summary", model.Summary);
                parameters.Add("@FileUrl", model.FileUrl);
                parameters.Add("@PerformedAt", DateTime.Now);
                parameters.Add("@Particulars", model.Particulars);
                parameters.Add("@ResourceUrl", model.ResourceUrl);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync(con, "usp_AddActivity", parameters,
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
        public async Task<bool> SaveCaptureClickDetails(CapturePageDM model)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@currenturl", model.currenturl);
                parameters.Add("@PerformedBy", model.PerformedBy);
                parameters.Add("@Functiontype", model.Functiontype);
                parameters.Add("@ButtonAction", model.ButtonAction);
                parameters.Add("@PerformedOn", DateTime.Now);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync(con, "usp_SavePageClickDetails", parameters,
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
