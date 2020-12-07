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
    public class DashboardRepository : IDashboarRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private IApplicationErrorLogger _applicationErrorLogger;
        public DashboardRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<IEnumerable<Meeting>> GetDateWiseMeetings(DateTime date)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@dateValue", date);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<Meeting>(con, "usp_GetDateWiseMeetings", parameters,
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

        public async Task<IEnumerable<DailyInterviewSlot>> GetDateWiseMeetingsSlots(DateTime date)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@dateValue", date);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<DailyInterviewSlot>(con, "usp_GetDateWiseMeetingsSlots", parameters,
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

        public async Task<DashboardInterviewerCount> GetInterviewerCount()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<DashboardInterviewerCount>(con, "usp_GetInterviewerCount",
                        commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<IRF>> GetIRFProgress(DateTime date)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@dateValue", date);
                    var result = await SqlMapper.QueryAsync<IRF>(con, "[usp_GetIRFProgress]", parameters,
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

        public IEnumerable<IrfProgress> GetProgressOptions()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {

                    return SqlMapper.Query<IrfProgress>(con, "usp_GetProgressOptions", commandType: CommandType.StoredProcedure);

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

        public async Task<bool> UpdateIRFProgress(IList<IRF> irfProgressList)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {

                    //DataTable progressDt = new DataTable();
                    //progressDt.Columns.Add("id", typeof(int));
                    //progressDt.Columns.Add("Comment", typeof(string));
                    //progressDt.Columns.Add("idProgress", typeof(int));

                    DataTable activityprogressDt = new DataTable();
                    activityprogressDt.Columns.Add("id", typeof(int));
                    activityprogressDt.Columns.Add("Comment", typeof(string));
                    activityprogressDt.Columns.Add("idProgress", typeof(int));
                    activityprogressDt.Columns.Add("Userid", typeof(string));
                    activityprogressDt.Columns.Add("Status", typeof(string));

                    //foreach (var item in irfProgressList)
                    //{
                    //    progressDt.Rows.Add(item.id, item.Comment, item.idProgress);
                    //}
                    foreach (var item in irfProgressList)
                    {
                        activityprogressDt.Rows.Add(item.id, item.Comment, item.idProgress, item.AssignTo, item.AssignStatus);
                    }

                    SqlCommand cmdProgress = new SqlCommand("usp_IRFProgressStatusBulkUpdate", con);
                    cmdProgress.CommandType = CommandType.StoredProcedure;
                    cmdProgress.Parameters.Add(new SqlParameter("@ProgressBulkData", SqlDbType.Structured));
                    cmdProgress.Parameters["@ProgressBulkData"].Value = activityprogressDt;
                    con.Open();
                    if (cmdProgress.ExecuteNonQuery() != 0)
                    {
                        con.Close();
                    }
                    //Create Sp and Update/Insert value into db
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

        public async Task<MeetingResultCount> GetMeetingResultCount()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<MeetingResultCount>(con, "usp_GetMeetingsResults",
                        commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<CandidatesCount> GetCandidatesCount()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<CandidatesCount>(con, "usp_GetCandidatesCount",
                        commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<Meeting>> GetDateWiseClientMeetings(DateTime date, string emailId)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@dateValue", date);
                parameters.Add("@emailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<Meeting>(con, "usp_GetDateWiseClientMeetings", parameters,
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

        public async Task<IRFFunnel> GetClientFunnelData(string emailId)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@emailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<IRFFunnel>(con, "usp_GetClientFunnelData", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<ClientDailySlotsStatus>> GetDateWiseClientMeetingsSlots(DateTime date, string emailId)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@dateValue", date);
                parameters.Add("@emailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<ClientDailySlotsStatus>(con, "usp_GetDateWiseClientMeetingsSlots", parameters,
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

        public async Task<CandidatesCount> GetClientCandidatesCount(string emailId)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@emailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<CandidatesCount>(con, "usp_GetClientCandidatesCount", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<MeetingResultCount> GetClientMeetingsResults(string emailId)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@emailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<MeetingResultCount>(con, "usp_GetClientMeetingsResults", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IRF FetchActivityDetails(int id, int idprogess)
        {
            try
            {
                var query = "Select ul.EmailId as AssignTo,IR.Status AssignStatus,IR.Comment from IRFActivityStatus IR Left join UserList ul on IR.Userid=ul.id where IR.IRFID=@id and IR.idProgress=@idprogess";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                dynamicParameters.Add("@idprogess", idprogess);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<IRF>(query, dynamicParameters);
                    return result.FirstOrDefault();
                }
            }
            catch (SqlException e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
    }
}
