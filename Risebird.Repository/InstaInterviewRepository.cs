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
        public async Task<IEnumerable<InstaMeetingVM>> GetAllInstaRequests(string emailID, string profileName)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@EmailId", emailID);
                    parameters.Add("@Profile", profileName);
                    return await SqlMapper.QueryAsync<InstaMeetingVM>(con, "usp_GetAllInstaRequests", parameters,
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
        public async Task<IEnumerable<InstaMeetingVM>> GetAllInterviewMeetings(DateTime startDate, DateTime endDate, int id, string emailId = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("startDate", startDate);
                    parameters.Add("endDate", endDate);
                    parameters.Add("id", id);
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
        public async Task<bool> CreateRequest(InstaMeetingVM meeting)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@InstaRequestName", meeting.InstaRequestName);
                parameters.Add("@AssessmentGroup", meeting.AssessmentGroup);
                parameters.Add("@JDLink", meeting.JDLink);
                parameters.Add("@RequestedBy", meeting.RequestedBy);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<int>(con, "usp_AddInstaRequest", parameters,
                         commandType: CommandType.StoredProcedure);
                    meeting.id = result.FirstOrDefault();
                    //Add Skills
                    DataTable techSkillDt = new DataTable();
                    techSkillDt.Columns.Add("IRFId", typeof(int));
                    techSkillDt.Columns.Add("LevelId", typeof(int));
                    techSkillDt.Columns.Add("SkillId", typeof(string));
                    techSkillDt.Columns.Add("ActualLevelId", typeof(int));
                    foreach (var item in meeting.TechSkill)
                    {
                        techSkillDt.Rows.Add(meeting.id, item.LevelId, item.SkillId, 0);
                    }
                    SqlCommand cmdTechSkill = new SqlCommand("usp_InstaTechSkillBulkInsert", con);
                    cmdTechSkill.CommandType = CommandType.StoredProcedure;
                    cmdTechSkill.Parameters.Add(new SqlParameter("@TechSkillBulkData", SqlDbType.Structured));
                    cmdTechSkill.Parameters["@TechSkillBulkData"].Value = techSkillDt;
                    con.Open();
                    if (cmdTechSkill.ExecuteNonQuery() != 0)
                    {
                        con.Close();
                    }
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
        public async Task<InstaMeetingVM> FetchRequestById(int id)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                var model = new InstaMeetingVM();
                var query = "Select * from InstaRequests Where Id=@id";
                var parameters = new DynamicParameters();
                parameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<InstaMeetingVM>(query, parameters);
                    model = result.FirstOrDefault();
                }
                //Fetch Skills
                var querySkill = "Select * from TransInstaSkills Where IRFID=@id";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<InstaTechSkill>(querySkill, parameters);
                    model.TechSkill = (List<InstaTechSkill>)result;
                }
                return model;
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return new InstaMeetingVM();
        }
        public async Task<bool> AddInstaMeeting(InstaMeetingVM meeting)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@CandidateName", meeting.CandidateName);
                parameters.Add("@ResumeLink", meeting.ResumeLink);
                parameters.Add("@RequestId", meeting.RequestId);
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
        public async Task<InstaMeetingVM> GetInstaInteviewReport(int interviewid)
        {
            try
            {
                //var parameters = new DynamicParameters();
                //parameters.Add("@interviewId", interviewid);
                //using (SqlConnection con = new SqlConnection(connectionString))
                //{
                //    var result = await SqlMapper.QueryFirstAsync<InstaMeetingVM>(con, "usp_GetMeetingById", parameters,
                //        commandType: CommandType.StoredProcedure);
                //    return result;
                //}
                var model = new InstaMeetingVM();
                var query = "Select * from InstaInterviewMeetings Where Id=@id";
                var parameters = new DynamicParameters();
                parameters.Add("@id", interviewid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<InstaMeetingVM>(query, parameters);
                    model = result.FirstOrDefault();
                }
                //Fetch Skills
                var querySkill = "Select ti.*,lt.Name as Level,ti.SkillId as SkillName from TransInstaSkills ti join LevelTech lt on lt.id=ti.LevelId Where IRFID=@id";
                var dp = new DynamicParameters();
                dp.Add("@id", model.RequestId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<InstaTechSkill>(querySkill, dp);
                    model.TechSkill = (List<InstaTechSkill>)result;
                }
                return model;
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<bool> AddInstaInterviewReport(InstaMeetingVM reportDetails)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    //check if the data is edited
                    DynamicParameters parameters = new DynamicParameters();
                    DynamicParameters paramx = new DynamicParameters();
                    paramx.Add("@IRFId", reportDetails.RequestId);
                    paramx.Add("@InterviewId", reportDetails.id);
                    paramx.Add("@TechSkillComments", reportDetails.TechSkillComments);
                    paramx.Add("@MeetingStatus", reportDetails.MeetingStatus);
                    paramx.Add("@InterviewStatus", reportDetails.InterviewStatus);
                    paramx.Add("@InterviewerComment", reportDetails.InterviewerComment);
                    paramx.Add("@VideoLink", reportDetails.VideoLink);

                    DynamicParameters parametersPoints = new DynamicParameters();

                    var result = SqlMapper.Query<int>(con, "usp_AddUpdateInstaInterviewReport", paramx,
                                            commandType: CommandType.StoredProcedure);
                    DataTable techReportDt = new DataTable();
                    techReportDt.Columns.Add("IRFId", typeof(int));
                    techReportDt.Columns.Add("LevelId", typeof(int));
                    techReportDt.Columns.Add("SkillId", typeof(string));
                    techReportDt.Columns.Add("ActualLevelId", typeof(int));
                    if (reportDetails.TechSkill != null)
                    {
                        foreach (var item in reportDetails.TechSkill)
                        {
                            techReportDt.Rows.Add(reportDetails.RequestId, item.LevelId, item.SkillId, item.ActualLevelId);
                        }
                        SqlCommand cmdTechReport = new SqlCommand("usp_InstaTechSkillReportBulkInsert", con)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmdTechReport.Parameters.Add(new SqlParameter("@TechSkillReportBulkData", SqlDbType.Structured));
                        cmdTechReport.Parameters["@TechSkillReportBulkData"].Value = techReportDt;
                        con.Open();
                        if (cmdTechReport.ExecuteNonQuery() != 0)
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
    }
}
