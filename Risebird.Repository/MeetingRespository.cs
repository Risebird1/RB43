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
using static System.Data.CommandType;

namespace Risebird.Repository
{
    public class MeetingRespository : IMeetingRespository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private IApplicationErrorLogger _applicationErrorLogger;
        public MeetingRespository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<IEnumerable<Meeting>> GetAllInterviewMeetings()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<Meeting>(
                        con, "select * from InterviewMeetings", null, commandType: Text);
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

        public async Task<IEnumerable<Meeting>> GetAllOpenInterviews()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<Meeting>(con, "usp_OpenInterviews", null,
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


        private DynamicParameters SetStoredProcedureParameterForMeeting(MeetingVM entity)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@IRFName", entity.IRFName); // Normal parametersmeters 
            parameters.Add("@clientName", entity.clientName);
            parameters.Add("@JobRole", entity.JobRole);
            parameters.Add("@interviewDate", entity.interviewDate.ToLongDateString());
            parameters.Add("@interviewSlot", entity.interviewSlot);
            parameters.Add("@interviewDuration", entity.interviewDuration);
            parameters.Add("@primaryskill", entity.primaryskill);
            parameters.Add("@candidateEmail", entity.candidateEmail);
            parameters.Add("@ClientHrContact", entity.ClientHrContact);
            parameters.Add("@resumeLink", entity.resumeLink);
            parameters.Add("@zoomLink", entity.zoomLink);
            parameters.Add("@zoomDetails", entity.zoomDetails);
            parameters.Add("@comfirmationStatus", entity.comfirmationStatus);
            parameters.Add("@interviewer", entity.interviewer);
            parameters.Add("@risebirdHR", entity.risebirdHR);
            return parameters;
        }
        public async Task<bool> AddMeeting(MeetingVM meeting)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = SetStoredProcedureParameterForMeeting(meeting);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_AddMeeting", parameters,
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

        public string AssignYourself(int meetingid, string interviewerMail)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlMapper.Query<string>(
                        con, "update [dbo].[InterviewMeetings] set interviewer=@value where id = @meetingid", new { value = interviewerMail, meetingid = meetingid }, commandType: Text);
                    return "Interviewer assigned successfully.";
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to assigned interview, Please contact Risebird HR.";
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to assigned interview, Please contact Risebird HR.";
            }
        }       

    }
}
