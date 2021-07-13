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
    public class CandidateRepository : ICandidateRepository
    {

        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private IApplicationErrorLogger _applicationErrorLogger;
        public CandidateRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<IEnumerable<CandidateTimelineData>> GetCandidateTimeline(string emailId = null)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@EmailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<CandidateTimelineData>(con, "usp_GetCandidateTimeline", parameters,
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
        public async Task<IEnumerable<CandidateTimelineData>> GetCandidateTimelinebyDate(DateTime startDate, DateTime endDate, string emailId = null)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@EmailId", emailId);
                parameters.Add("@startDate", startDate);
                parameters.Add("@endDate", endDate);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<CandidateTimelineData>(con, "usp_GetCandidateTimelineByDate", parameters,
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


        public async Task<IEnumerable<CandidateLastStatus>> GetCandidateLastStatus()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@dateValue", DateTime.Now.AddDays(-1));
                    var result = SqlMapper.Query<CandidateLastStatus>(con, "usp_GetCandidateLastStatus", parameters,
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
        public async Task<CandidateDetailsForWhatsApp> FetchInterviewDetails(int interviewid)
        {
            try
            {
                var query = "select id,candidateEmail,interviewDate,CandidateName from InterviewMeetings where id=@interviewid and interviewdate>dateadd(minute,330,getdate()) and confirmationStatus=1";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@interviewid", interviewid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = (List<CandidateDetailsForWhatsApp>)await con.QueryAsync<CandidateDetailsForWhatsApp>(query, dynamicParameters);
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
        public async Task<string> CancelInterviewDetails(int interviewid, string CandidateEmail)
        {
            try
            {
                int isvalidintervierid = 0;
                var query = "select id from InterviewMeetings where id=@interviewid and CandidateEmail=@CandidateEmail and confirmationStatus=1";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@interviewid", interviewid);
                dynamicParameters.Add("@CandidateEmail", CandidateEmail);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await con.QueryAsync<int>(query, dynamicParameters);
                    isvalidintervierid = result.FirstOrDefault();
                }
                if (isvalidintervierid > 0)
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@interviewid", interviewid);
                        parameters.Add("@CandidateEmail", CandidateEmail);
                        var result = SqlMapper.Query<CandidateLastStatus>(con, "usp_CancelInterviewUpdate", parameters,
                            commandType: CommandType.StoredProcedure);
                        return "Interview Cancelled Successfully.";
                    }
                }
                else
                {
                    return "Email Address is not correct, Please enter correct Email Address.";
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
            return "Fail to Cancel the interview, Please try again.";
        }
        public async Task<InterviewUsersDetails> FetchChannelOwnerandInterviewer(int interviewid)
        {
            try
            {
                var query = "Select im.interviewer,ul.EmailId,im.interviewDate,Cl.Name as ClientName,IRF.CalendlyLink,IRF.JDTiltle from InterviewMeetings im join IRF left join Client cl on IRF.ClientId=cl.id on im.IRFId=irf.IRFId left join Userlist ul on ul.id=irf.RisebirdHr where im.id=@interviewid and confirmationStatus=2";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@interviewid", interviewid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await con.QueryAsync<InterviewUsersDetails>(query, dynamicParameters);
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
        public async Task<InterviewUsersDetails> FetchInterviewerDetails(int interviewid)
        {
            try
            {
                var query = "Select im.interviewer,ul.EmailId,im.interviewDate,Cl.Name as ClientName,IRF.CalendlyLink,IRF.JDTiltle,(Isnull(uli.FirstName,'')+Isnull(uli.LastName,'')) as CandidateName from InterviewMeetings im join IRF left join Client cl on IRF.ClientId=cl.id on im.IRFId=irf.IRFId left join Userlist ul on ul.id=irf.RisebirdHr left join Userlist uli on uli.EmailId=im.candidateEmail  where im.id=@interviewid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@interviewid", interviewid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await con.QueryAsync<InterviewUsersDetails>(query, dynamicParameters);
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

        public async Task<Candidate> GetCandidatesDetails(int irfId, int candidateId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", irfId);
                dynamicParameters.Add("@CandidateId", candidateId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync<Candidate>(con, "usp_GetCandidateDetailsByCandidateId", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> AddResume(List<CandidateResume> resumeList)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DataTable resumeListDt = new DataTable();
                    resumeListDt.Columns.Add("IRFId", typeof(int));
                    resumeListDt.Columns.Add("CandidateId", typeof(int));
                    resumeListDt.Columns.Add("ResumeLink", typeof(string));
                    resumeListDt.Columns.Add("AddedBy", typeof(int));
                    resumeListDt.Columns.Add("AddedOn", typeof(DateTime));


                    foreach (var item in resumeList)
                    {
                        resumeListDt.Rows.Add(item.CandidateId, item.IRFId, item.ResumeLink, item.AddedBy, DateTime.Now);
                    }

                    SqlCommand cmdJoinability = new SqlCommand("usp_CandidateResumeBulkInsert", con);
                    cmdJoinability.CommandType = CommandType.StoredProcedure;
                    cmdJoinability.Parameters.Add(new SqlParameter("@CandidateResumeBulkData", SqlDbType.Structured));
                    cmdJoinability.Parameters["@CandidateResumeBulkData"].Value = resumeListDt;
                    con.Open();
                    if (cmdJoinability.ExecuteNonQuery() != 0)
                    {
                        con.Close();
                    }
                    return true;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<IEnumerable<CandidateResume>> GetCandidatesResumes(int irfId, int candidateId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", irfId);
                dynamicParameters.Add("@CandidateId", candidateId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync<CandidateResume>(con, "usp_GetCandidatesResumes", dynamicParameters,
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

        public async Task<bool> DeleteCandidateResume(int id)
        {
            try
            {

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<int>(con, "usp_DeleteCandidateResume", parameters,
                        commandType: CommandType.StoredProcedure);
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
