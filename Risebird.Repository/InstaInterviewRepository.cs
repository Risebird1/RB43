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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Data.CommandType;

namespace Risebird.Repository
{
    public class InstaInterviewRepository : IInstaInterviewRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public InstaInterviewRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }
        public async Task<IEnumerable<InstaMeetingVM>> GetAllInterviewMeetings(DateTime startDate, DateTime endDate, string emailId = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("startDate", startDate);
                    parameters.Add("endDate", endDate);
                    parameters.Add("EmailId", emailId);
                    var result = SqlMapper.Query<InstaMeetingVM>(con, "usp_GetAllInstaInterviewMeetings", parameters,
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
        public async Task<bool> AddInstaMeeting(InstaMeetingVM meeting)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@RequiredSkill", meeting.RequiredSkill);
                parameters.Add("@CandidateName", meeting.CandidateName);
                parameters.Add("@AssessmentGroup", meeting.AssessmentGroup);
                parameters.Add("@ResumeLink", meeting.ResumeLink);
                parameters.Add("@JDLink", meeting.JDLink);
                parameters.Add("@RequestedBy", meeting.RequestedBy);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_AddInstaMeeting", parameters,
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
        public async Task<InstaMeetingVM> GetInstaMeeting(int id)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@interviewId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryFirstAsync<InstaMeetingVM>(con, "usp_GetInstaMeetingById", parameters,
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
        public async Task<bool> UpdateInstaMeeting(InstaMeetingVM meeting)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Id", meeting.id); // Normal parametersmeters           
                parameters.Add("@RequiredSkill", meeting.RequiredSkill);
                parameters.Add("@CandidateName", meeting.CandidateName);
                parameters.Add("@AssessmentGroup", meeting.AssessmentGroup);
                parameters.Add("@ResumeLink", meeting.ResumeLink);
                parameters.Add("@JDLink", meeting.JDLink);
                parameters.Add("@RequestedBy", meeting.RequestedBy);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_UpdateInstaMeeting", parameters,
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
        public string FetchSkillsByAssessmentGroup(int id)
        {
            try
            {
                var query = "Select PrimarySkills from AssessmentGroup Where Id=@id";
                var parameters = new DynamicParameters();
                parameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<string>(query, parameters);
                    return result.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return "";
        }
        public async Task<IEnumerable<InstaMeetingVM>> GetAllInstaOpenInterviews(string emailId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("emailID", emailId);
                    return await SqlMapper.QueryAsync<InstaMeetingVM>(con, "usp_OpenInstaInterviews", parameters,
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
        public string InstaAssignYourself(int meetingid, string interviewerMail)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlMapper.Query<string>(
                        con, "update [dbo].[InstaInterviewMeetings] set interviewer=@value,confirmationStatus=1 where id = @meetingid", new { value = interviewerMail, meetingid }, commandType: Text);
                    return "Interview assigned Successfully.";
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
        public async Task<IEnumerable<InstaMeetingVM>> GetAllInstaAssignedInterviews(string emailID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("emailID", emailID);
                    return await SqlMapper.QueryAsync<InstaMeetingVM>(con, "usp_MyInstaInterviews", parameters,
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
        public List<string> FetchInstaIntervieweByAssessmentGroup(string Assessmentgroupid)
        {
            try
            {
                var query = ";With CTE AS(Select  * from InterviewerProfile cross Apply string_split(AssessmentGroupId,',') where Isinstaavailable=1) Select Distinct ul.Emailid From CTE join Userlist ul on CTE.Userid=ul.id where CTE.value=@Assessmentgroupid";
                DynamicParameters dp = new DynamicParameters();
                dp.Add("@Assessmentgroupid", Assessmentgroupid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<string>(query, dp);
                    return (List<string>)result;
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
    }
}
