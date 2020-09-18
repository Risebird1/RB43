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
using System.Linq;
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
                var ids = string.Join(",", idList);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await con.QueryAsync("Delete from TransVMTriggerDetails where id in (" +ids +")");
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

        public async Task<bool> AddVMTrigger(int vmId, string action, string expectedStatus)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@vmId", vmId);
                dynamicParameters.Add("@action", action);
                dynamicParameters.Add("@expectedStatus", expectedStatus);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = SqlMapper.Query(con, "usp_AddVMTrigger", dynamicParameters,
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

        public async Task<string> AddNonExistingVMs(List<VMProperties> vmList)
        {
            try
            {
                if (vmList != null && vmList.Count() > 0)
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        
                        DataTable vmPropertiesDt = new DataTable();
                        vmPropertiesDt.Columns.Add("Name", typeof(string));
                        vmPropertiesDt.Columns.Add("ResourceGroupName", typeof(string));
                        vmPropertiesDt.Columns.Add("Status", typeof(int));
                        vmPropertiesDt.Columns.Add("IsAdded", typeof(bool));
                        vmPropertiesDt.Columns.Add("IsRemoved", typeof(bool));
                        vmPropertiesDt.Columns.Add("IsDeleted", typeof(bool));

                        foreach (var vm in vmList)
                        {
                            vmPropertiesDt.Rows.Add(vm.Name,vm.ResourceGroupName,vm.Status,true, false, false);
                        }

                        SqlCommand cmd = new SqlCommand("usp_VMPropertiesBulkInsert", con);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add(new SqlParameter("@VMPropertiesBulkData", SqlDbType.Structured));
                        cmd.Parameters["@VMPropertiesBulkData"].Value = vmPropertiesDt;
                        con.Open();
                        if (cmd.ExecuteNonQuery() != 0)
                        {
                            con.Close();
                        }
                    }

                }
                return "Successfully added all the selected Virtual Machines.";

            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "Failed";
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "Failed";
            }
        }

        public int GetVM(string name, string rgName)
        {
            try
            {
                var query = "Select id from VMProperties where Name = @Name and ResourceGroupName=@ResourceGroupName";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Name", name);
                dynamicParameters.Add("@ResourceGroupName", rgName);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<int>(query, dynamicParameters);
                    return result.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return 0;
        }

        public async Task<bool> AddBulkVmTrigger(List<TriggeredVM> vmList)
        {
            try
            {
                if (vmList != null && vmList.Count() > 0)
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {

                        DataTable vmPropertiesDt = new DataTable();
                        vmPropertiesDt.Columns.Add("VMPropertyId", typeof(int));
                        vmPropertiesDt.Columns.Add("LastAction", typeof(string));
                        vmPropertiesDt.Columns.Add("ExpectedStatus", typeof(string));

                        foreach (var vm in vmList)
                        {
                            vmPropertiesDt.Rows.Add(vm.VMPropertyId, vm.LastAction, vm.ExpectedStatus);
                        }

                        SqlCommand cmd = new SqlCommand("usp_TransVMTriggerDetailsBulkInsert", con);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add(new SqlParameter("@TransVMTriggerDetailsBulkData", SqlDbType.Structured));
                        cmd.Parameters["@TransVMTriggerDetailsBulkData"].Value = vmPropertiesDt;
                        con.Open();
                        if (cmd.ExecuteNonQuery() != 0)
                        {
                            con.Close();
                        }
                    }

                }
                return true;

            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            return false;
        }

    }
}
