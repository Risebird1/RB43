using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
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
    public class UnapprovedInterviewerMeetingsRepository : IUnapprovedInterviewerMeetingsRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public UnapprovedInterviewerMeetingsRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<IEnumerable<MeetingVM>> GetAllUnapprovedInterviewerMeetings(DateTime startDate, DateTime endDate)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("startDate", startDate);
                    parameters.Add("endDate", endDate);
                    var result = await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetIAllDateWiseUnApprovedInterviewerMeetings", parameters,
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

        public async Task<IEnumerable<InterviewerSkillsVM>> GetISCList()
        {
            try
            {
                var query = "Select * from InterviewerSkillsChannel where IsDeleted= 0";

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<InterviewerSkillsVM>(query);
                    return results;
                }

            }
            catch (SqlException ex)
            {
               await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> AddorupdateMeeting(InterviewerMeetingVM meeting)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@interviewid", meeting.imid);
                parameters.Add("@ISCId", meeting.ISCId);
                parameters.Add("@interviewDate", meeting.interviewDate);
                parameters.Add("@interviewSlot", meeting.interviewSlot);
                parameters.Add("@interviewDuration", meeting.interviewDuration);
                parameters.Add("@candidateName", meeting.candidateName);
                parameters.Add("@candidateEmail", meeting.candidateEmail);
                parameters.Add("@ZoomId", meeting.ZoomId);
                parameters.Add("@zoomLink", meeting.zoomLink);
                parameters.Add("@comfirmationStatus", meeting.confirmationStatus);
                parameters.Add("@interviewer", meeting.interviewer);
                parameters.Add("@sCreatedBy", meeting.sCreatedBy);
                if (meeting.IsEditable)
                {
                    parameters.Add("@editdate", 1);
                }
                else
                {
                    parameters.Add("@editdate", 0);
                }
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_AddInterviewerMeeting", parameters,
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
        public async Task<InterviewerMeetingVM> GetInterviewMeetingById(int id)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@interviewId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryFirstAsync<InterviewerMeetingVM>(con, "usp_GetUnapprovedInterviewerMeetingById", parameters,
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
    }
}
