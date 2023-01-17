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
using System.Linq;
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

        public async Task<IEnumerable<VMProperties>> GetAllUserVMs(string emailId, string profile)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@EmailId", emailId);
                dynamicParameters.Add("@Profile", profile);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<VMProperties>(con, "usp_GetAllUserVMs", dynamicParameters,
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

        public async Task<IEnumerable<VMProperties>> GetAllVMs()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<VMProperties>(con, "usp_GetAllVMs", null,
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
                    var result = await SqlMapper.QueryAsync<TriggeredVM>(con, "usp_GetAllTriggeredVMs", null,
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
                    await con.QueryAsync("Delete from TransVMTriggerDetails where id in (" + ids + ")");
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
                    var results = await SqlMapper.QueryAsync(con, "usp_UpdateVMStatus", dynamicParameters,
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
                    var results = await SqlMapper.QueryAsync(con, "usp_AddVMTrigger", dynamicParameters,
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

                        vmPropertiesDt.Columns.Add("shutdowntime", typeof(int));
                        vmPropertiesDt.Columns.Add("deletetime", typeof(int));
                        vmPropertiesDt.Columns.Add("VmLocation", typeof(string));

                        foreach (var vm in vmList)
                        {
                            vmPropertiesDt.Rows.Add(vm.Name, vm.ResourceGroupName, vm.Status, true, false, false, vm.shutdowntime, vm.deletetime, vm.VmLocation);
                        }

                        SqlCommand cmd = new SqlCommand("usp_VMPropertiesBulkInsert", con);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add(new SqlParameter("@VMPropertiesBulkData", SqlDbType.Structured));
                        cmd.Parameters["@VMPropertiesBulkData"].Value = vmPropertiesDt;
                        con.Open();
                        if (await cmd.ExecuteNonQueryAsync() != 0)
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
                        if (await cmd.ExecuteNonQueryAsync() != 0)
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

        public VMProperties GetVMbyId(int id)
        {
            try
            {
                var query = "Select * from VMProperties where id = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<VMProperties>(query, dynamicParameters);
                    return result.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task UpdateVMIP(IList<VMProperties> vmList)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {

                    DataTable vmIpDt = new DataTable();
                    vmIpDt.Columns.Add("id", typeof(int));
                    vmIpDt.Columns.Add("OSType", typeof(string));
                    vmIpDt.Columns.Add("PublicIP", typeof(string));
                    vmIpDt.Columns.Add("PrivateIP", typeof(string));


                    foreach (var item in vmList)
                    {
                        vmIpDt.Rows.Add(item.id, item.OSType, item.PublicIP, item.PrivateIP);
                    }

                    SqlCommand cmd = new SqlCommand("usp_VMIPBulkUpdate", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@VMIPBulkData", SqlDbType.Structured));
                    cmd.Parameters["@VMIPBulkData"].Value = vmIpDt;
                    con.Open();
                    if (await cmd.ExecuteNonQueryAsync() != 0)
                    {
                        con.Close();
                    }

                }

            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
        }

        public async Task<bool> UpdateVMNamePassword(int id, string customName, string password, string userName)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                dynamicParameters.Add("@CustomName", customName);
                dynamicParameters.Add("@Password", password);
                dynamicParameters.Add("@UserName", userName);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync(con, "usp_UpdateVMNamePassword", dynamicParameters,
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

        public async Task<bool> UpdateVMPassword(int id, string password)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                dynamicParameters.Add("@Password", password);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync(con, "usp_UpdateVMPassword", dynamicParameters,
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

        public IEnumerable<TransVMUser> GetUserList(int vmId, int profileId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@vmId", vmId);
                dynamicParameters.Add("@profileId", profileId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<TransVMUser>(con, "usp_GetUserList", dynamicParameters,
                         commandType: CommandType.StoredProcedure);
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> UpdateVMUsers(int[] userList, int vmId, int profileId)
        {
            try
            {

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@vmId", vmId);
                parameters.Add("@profileId", profileId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_DeleteTransVmUsers", parameters,
                       commandType: CommandType.StoredProcedure);



                    if (userList != null && userList.Count() > 0)
                    {
                        DataTable vmUserDt = new DataTable();
                        vmUserDt.Columns.Add("VmId", typeof(int));
                        vmUserDt.Columns.Add("UserId", typeof(int));
                        vmUserDt.Columns.Add("ProfileId", typeof(int));
                        vmUserDt.Columns.Add("SharedOn", typeof(DateTime));

                        foreach (var item in userList)
                        {
                            vmUserDt.Rows.Add(vmId, item, profileId, DateTime.Now);
                        }

                        SqlCommand cmd = new SqlCommand("usp_VMUsersBulkInsert", con);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add(new SqlParameter("@VMUsersBulkData", SqlDbType.Structured));
                        cmd.Parameters["@VMUsersBulkData"].Value = vmUserDt;
                        con.Open();
                        if (cmd.ExecuteNonQuery() != 0)
                        {
                            con.Close();
                        }
                    }


                    return true;
                }

            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<IEnumerable<ProctorVMQuestions>> GetVMQuestions(int vmId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@vmId", vmId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<ProctorVMQuestions>(con, "usp_GetVMQuestions", dynamicParameters,
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


        public async Task<bool> UpdateVMQuestions(ProctorQuestionsViewModel model)
        {
            try
            {

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@vmId", model.VmId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_DeleteVMQuestions", parameters,
                       commandType: CommandType.StoredProcedure);



                    if (model.QuestionList != null && model.QuestionList.Count() > 0)
                    {
                        DataTable vmQuestionDt = new DataTable();
                        vmQuestionDt.Columns.Add("VmId", typeof(int));
                        vmQuestionDt.Columns.Add("Priority", typeof(int));
                        vmQuestionDt.Columns.Add("Question", typeof(string));
                        vmQuestionDt.Columns.Add("Detail", typeof(string));
                        vmQuestionDt.Columns.Add("VideoLink", typeof(string));
                        vmQuestionDt.Columns.Add("Time", typeof(int));
                        vmQuestionDt.Columns.Add("Level", typeof(int));
                        vmQuestionDt.Columns.Add("Skill", typeof(string));

                        foreach (var item in model.QuestionList)
                        {
                            vmQuestionDt.Rows.Add(model.VmId, item.Priority, item.Question, item.Detail, item.VideoLink, item.Time, item.Level, item.Skill);
                        }

                        SqlCommand cmd = new SqlCommand("usp_VMQuestionsBulkInsert", con);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add(new SqlParameter("@VMQuestionsBulkData", SqlDbType.Structured));
                        cmd.Parameters["@VMQuestionsBulkData"].Value = vmQuestionDt;
                        con.Open();
                        if (cmd.ExecuteNonQuery() != 0)
                        {
                            con.Close();
                        }
                    }


                    return true;
                }

            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<bool> UpdateActionPerformed(int vmId, string emailId, string action)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@vmId", vmId);
                dynamicParameters.Add("@emailId", emailId);
                dynamicParameters.Add("@action", action);
                dynamicParameters.Add("@time", DateTime.Now);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync(con, "usp_UpdateActionPerformed", dynamicParameters,
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

        public async Task<bool> RemoveCandidateVmAccess()
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@profileId", (int)ProfileEnum.Candidate);
                dynamicParameters.Add("@time", DateTime.Now.AddHours(-2));
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = SqlMapper.Query(con, "usp_RemoveCandidateVmAccess", dynamicParameters,
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

        public async Task<bool> RemoveVM(int vmId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@vmId", vmId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = SqlMapper.Query(con, "usp_RemoveVM", dynamicParameters,
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

        /// <summary>
        /// Get the Emails of all the users who have started a virtual machine 
        /// </summary> 2 hours or before back and is yet to be stopped 
        /// <returns></returns>
        public async Task<IEnumerable<VMActionPerformed>> GetVMStarterEmailId()
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@time", DateTime.Now.AddHours(-2));
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<VMActionPerformed>(con, "usp_GetVMStarterEmailId", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<IEnumerable<VMProperties>> GetDeleteorShutdownVMList()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<VMProperties>(con, "usp_deleteorShutdownVM", null,
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
