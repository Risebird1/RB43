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

namespace Risebird.Repository
{
    public class InterviewRequestRepository : IInterviewRequestRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public InterviewRequestRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }
        public async Task<bool> CreateInerviewRequest(InterviewRequestVM meeting)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", meeting.id);
                parameters.Add("@JdTitle", meeting.JdTitle);
                parameters.Add("@PrimarySkill", meeting.PrimarySkill);
                parameters.Add("@JDLink", meeting.JDLink);
                parameters.Add("@JobLevel", meeting.JobLevel);
                parameters.Add("@RequestedBy", meeting.RequestedBy);
                parameters.Add("@ExpectedInterviews", meeting.ExpectedInterviews);
                parameters.Add("@Country", meeting.Country);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<int>(con, "usp_AddInterviewRequest", parameters,
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
                        techSkillDt.Rows.Add(meeting.id, item.LevelId, item.SkillId, item.Weightage);
                    }
                    SqlCommand cmdTechSkill = new SqlCommand("usp_InterviewRequestTechSkillBulkInsert", con);
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
        public async Task<IEnumerable<InterviewRequestVM>> GetAllInterviewRequests(string emailID, string profileName)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@EmailId", emailID);
                    parameters.Add("@Profile", profileName);
                    return await SqlMapper.QueryAsync<InterviewRequestVM>(con, "usp_GetAllInterviewRequests", parameters,
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
        public async Task<InterviewRequestVM> FetchInterviewRequestById(int id)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                var model = new InterviewRequestVM();
                var query = "Select ir.*,ul.EmailId as Assignto,IRF.CalendlyLink from InterviewRequests ir left join UserList ul on ul.id=ir.Assignto left join IRF on IRF.IRFId=ir.IRFID Where ir.id=@id";
                var parameters = new DynamicParameters();
                parameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<InterviewRequestVM>(query, parameters);
                    model = result.FirstOrDefault();
                }
                //Fetch Skills
                var querySkill = "Select IRFId,LevelId,SkillId,ActualLevelId as Weightage from TransInterviewRequestSkills Where IRFID=@id";
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
            return new InterviewRequestVM();
        }
        public async Task<bool> AssignInerviewRequest(int userid, int id)
        {
            try
            {
                var query = "Update InterviewRequests set Assignto=@Assignto Where Id=@id";
                var parameters = new DynamicParameters();
                parameters.Add("@id", id);
                parameters.Add("@Assignto", userid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query(query, parameters);
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
        public async Task<bool> SetIRFCreated(int id, string IRFId)
        {
            try
            {
                var query = "Update InterviewRequests set IsIRFCreated=1 , IRFId =@IRFId Where Id=@id";
                var parameters = new DynamicParameters();
                parameters.Add("@id", id);
                parameters.Add("@IRFId", IRFId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query(query, parameters);
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
        public async Task<string> FetchInterviewerNameByEmailid(string emailid)
        {
            try
            {
                var query = "Select FirstName from UserList where Emailid=@id";
                var parameters = new DynamicParameters();
                parameters.Add("@id", emailid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<string>(query, parameters);
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
            return "";
        }
    }
}
