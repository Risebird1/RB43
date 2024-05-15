using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Risebird.Entities.Data.RchilliResumeMapFields;
using Risebird.Entities.Data;

namespace Risebird.Repository
{
    public class TaskRepository : ITaskRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TaskRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<SkillChannelCollectiveDetailsVM> SkillChannelCollectiveDetails(int irfId, string type)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@irfId", irfId);
                    parameters.Add("@type", type);
                    parameters.Add("@timeNow", DateTime.Now);
                    var result = await SqlMapper.QueryAsync<SkillChannelCollectiveDetailsVM>(con, "usp_GetSkillChannelCollectiveDetails", parameters,
                        commandType: CommandType.StoredProcedure, commandTimeout: 300);
                    return result.FirstOrDefault();
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
        public async Task<IEnumerable<PQIDetails>> GetSkillChannelAccuracyDetails(int irfId, string type)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@irfId", irfId);
                    parameters.Add("@type", type);
                    var result = await SqlMapper.QueryAsync<PQIDetails>(con, "usp_GetSkillChannelAccuracyDetails", parameters,
                        commandType: CommandType.StoredProcedure, commandTimeout: 300);
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
        public async Task<IEnumerable<SkillChannelTaskSpeedDetailsVM>> SkillChannelSpeedDetails(int irfId, string type)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@irfId", irfId);
                    parameters.Add("@type", type);
                    var result = await SqlMapper.QueryAsync<SkillChannelTaskSpeedDetailsVM>(con, "usp_GetSkillChannelSpeedDetails", parameters,
                        commandType: CommandType.StoredProcedure, commandTimeout: 300);
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

        public async Task<bool> AddTaskStatus(SkillChannelTasks model)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@IRFId", model.IRFId); // Normal parametersmeters           
                parameters.Add("@Comments", model.Comments);
                parameters.Add("@CreatedOn", DateTime.Now);
                parameters.Add("@Status", model.Status);
                parameters.Add("@TaskName", model.TaskName);
                parameters.Add("@TaskValue", model.TaskValue);
                parameters.Add("@PerformedBy", model.PerformedBy);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync(con, "usp_AddTaskStatus", parameters,
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
        public async Task<IEnumerable<SkillChannelTaskCompletionDetailsVM>> SkillChannelCompletionDetails(int irfId, string type)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@irfId", irfId);
                    parameters.Add("@type", type);
                    var result = await SqlMapper.QueryAsync<SkillChannelTaskCompletionDetailsVM>(con, "usp_GetSkillChannelCompletionDetails", parameters,
                        commandType: CommandType.StoredProcedure, commandTimeout: 300);
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

        public async Task<IEnumerable<SkillChannelTasks>> GetTaskStatusList(int irfId, string tab, string taskValue)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", irfId);
                dynamicParameters.Add("@taskName", tab);
                dynamicParameters.Add("@taskValue", taskValue);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync<SkillChannelTasks>(con, "usp_GetTaskStatusList", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

    }
}
