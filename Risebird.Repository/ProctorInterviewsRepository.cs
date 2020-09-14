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
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    public class ProctorInterviewsRepository : IProctorInterviewsRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        public ProctorInterviewsRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<IEnumerable<VMProperties>> GetAllVMs()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = SqlMapper.Query<VMProperties>(con, "usp_GetAllVMs", null,
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

        public async Task<IEnumerable<TriggeredVM>> GetAllTriggeredVMs()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = SqlMapper.Query<TriggeredVM>(con, "usp_GetAllTriggeredVMs", null,
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

        public async Task<bool> DeleteTriggeredVMs(List<int> idList)
        {
            try
            {
                var ids = "(" + String.Join(",", idList) + ")";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@ids", ids);
                    await con.QueryAsync("Delete from TransVMTriggerDetails where id in @ids", parameters);
                    //await SqlMapper.QueryAsync(con, "usp_DeleteTriggeredVMs", parameters,
                    //     commandType: CommandType.StoredProcedure);
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
        public async Task<bool> UpdateVMStatus(int id, int status)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                dynamicParameters.Add("@status", status);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = SqlMapper.Query(con, "usp_UpdateVMStatus", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }


    }
}
