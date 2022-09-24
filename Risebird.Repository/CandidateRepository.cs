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
                var query = "Select im.interviewer,ul.EmailId,im.interviewDate,Cl.Name as ClientName,IRF.CalendlyLink,IRF.JDTiltle,IRF.IRFId,IRF.id as irfIntId,im.candidateEmail,im.candidateName as CandidateName,im.ZoomMeetingId   from InterviewMeetings im join IRF left join Client cl on IRF.ClientId=cl.id on im.IRFId=irf.IRFId left join Userlist ul on ul.id=irf.RisebirdHr   where im.id=@interviewid and confirmationStatus=2";
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
                    resumeListDt.Columns.Add("ResumeLink", typeof(string));
                    resumeListDt.Columns.Add("AddedBy", typeof(int));
                    resumeListDt.Columns.Add("AddedOn", typeof(DateTime));


                    foreach (var item in resumeList)
                    {
                        resumeListDt.Rows.Add(item.IRFId, item.ResumeLink, item.AddedBy, DateTime.Now);
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
        public async Task<IEnumerable<CandidateResume>> GetCandidatesResumes(int irfId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", irfId);
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


        //public async Task<IEnumerable<string>> GetIRFOwnerAndRestrictedOwnerClients(int interviewId)
        //{
        //    try
        //    {
        //        var dynamicParameters = new DynamicParameters();
        //        dynamicParameters.Add("@IRFId", null);
        //        dynamicParameters.Add("@interviewId", interviewId);
        //        using (SqlConnection con = new SqlConnection(connectionString))
        //        {
        //            var results = await SqlMapper.QueryAsync<string>(con, "usp_ListOfIRFClients", dynamicParameters,
        //               commandType: CommandType.StoredProcedure);
        //            return results;
        //        }
        //    }
        //    catch (SqlException ex)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        return null;
        //    }
        //}

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

        public async Task<IEnumerable<string>> GetCandidateStaffingAgencyAndClientHRByInterviewId(int interviewId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@interviewId", interviewId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync<string>(con, "usp_GetCandidateStaffingAgencyAndClientHRByInterviewId", dynamicParameters,
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

        public async Task<string> GetCandidateStaffingAgency(int candidateId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@candidateId", candidateId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync<string>(con, "usp_GetCandidateStaffingAgency", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return results?.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<IEnumerable<CandidateTimelineData>> GetCandidateTimelinebyDateStatus(DateTime startDate, DateTime endDate, int datatype, string emailId = null)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@EmailId", emailId);
                parameters.Add("@startDate", startDate);
                parameters.Add("@endDate", endDate);
                parameters.Add("@datatype", datatype);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<CandidateTimelineData>(con, "usp_GetCandidateTimelineByDateStatus", parameters,
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

        public async Task<bool> AddEditFitmentReport(FitmentReportModel model)
        {
            try
            {

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DataTable joinabilityReportDt = new DataTable();
                    joinabilityReportDt.Columns.Add("RequiredLevelId", typeof(int));
                    joinabilityReportDt.Columns.Add("Type", typeof(string));
                    joinabilityReportDt.Columns.Add("ActualLevelId", typeof(int));
                    joinabilityReportDt.Columns.Add("WeightageRequired", typeof(int));
                    joinabilityReportDt.Columns.Add("WeightageActual", typeof(int));
                    joinabilityReportDt.Columns.Add("Value", typeof(string));
                    joinabilityReportDt.Columns.Add("CandidateId", typeof(int));
                    joinabilityReportDt.Columns.Add("IRFId", typeof(int));


                    foreach (var item in model.JoinabilityReportCollection)
                    {
                        joinabilityReportDt.Rows.Add(item.RequiredLevelId, item.Name, item.ActualLevelId, item.WeightageRequired, item.WeightageActual, item.Value, model.CandidateId, model.IRFId);
                    }

                    DataTable selectabilityReportDt = new DataTable();
                    selectabilityReportDt.Columns.Add("RequiredLevelId", typeof(int));
                    selectabilityReportDt.Columns.Add("Type", typeof(string));
                    selectabilityReportDt.Columns.Add("ActualLevelId", typeof(int));
                    selectabilityReportDt.Columns.Add("WeightageRequired", typeof(int));
                    selectabilityReportDt.Columns.Add("WeightageActual", typeof(int));
                    selectabilityReportDt.Columns.Add("Value", typeof(string));
                    selectabilityReportDt.Columns.Add("CandidateId", typeof(int));
                    selectabilityReportDt.Columns.Add("IRFId", typeof(int));


                    foreach (var item in model.SelectabilityReportCollection)
                    {
                        selectabilityReportDt.Rows.Add(item.RequiredLevelId, item.Name, item.ActualLevelId, item.WeightageRequired, item.WeightageActual, item.Value, model.CandidateId, model.IRFId);
                    }

                    SqlCommand cmdAddTemplate = new SqlCommand("usp_AddEditFitmentReport", con);
                    cmdAddTemplate.CommandType = CommandType.StoredProcedure;
                    cmdAddTemplate.Parameters.Add(new SqlParameter("@JoinabilityReportBulkData", SqlDbType.Structured));
                    cmdAddTemplate.Parameters["@JoinabilityReportBulkData"].Value = joinabilityReportDt;
                    cmdAddTemplate.Parameters.Add(new SqlParameter("@SelectabilityReportBulkData", SqlDbType.Structured));
                    cmdAddTemplate.Parameters["@SelectabilityReportBulkData"].Value = selectabilityReportDt;
                    cmdAddTemplate.Parameters.Add(new SqlParameter("@FitmentStatus", SqlDbType.Int));
                    cmdAddTemplate.Parameters["@FitmentStatus"].Value = model.FitmentStatus;
                    cmdAddTemplate.Parameters.Add(new SqlParameter("@PriorityStatus", SqlDbType.Int));
                    cmdAddTemplate.Parameters["@PriorityStatus"].Value = model.PriorityStatus;
                    cmdAddTemplate.Parameters.Add(new SqlParameter("@CompletionStatus", SqlDbType.Int));
                    cmdAddTemplate.Parameters["@CompletionStatus"].Value = model.CompletionStatus;
                    cmdAddTemplate.Parameters.Add(new SqlParameter("@IRFId", SqlDbType.Int));
                    cmdAddTemplate.Parameters["@IRFId"].Value = model.IRFId;
                    cmdAddTemplate.Parameters.Add(new SqlParameter("@CandidateId", SqlDbType.Int));
                    cmdAddTemplate.Parameters["@CandidateId"].Value = model.CandidateId;
                    cmdAddTemplate.Parameters.Add(new SqlParameter("@JoinabilityScore", SqlDbType.Int));
                    cmdAddTemplate.Parameters["@JoinabilityScore"].Value = model.JoinabilityScore;
                    cmdAddTemplate.Parameters.Add(new SqlParameter("@SelectabilityScore", SqlDbType.Int));
                    cmdAddTemplate.Parameters["@SelectabilityScore"].Value = model.SelectabilityScore;
                    cmdAddTemplate.Parameters.Add(new SqlParameter("@JoinabilityComments", SqlDbType.VarChar));
                    cmdAddTemplate.Parameters["@JoinabilityComments"].Value = model.JoinabilityComments;
                    cmdAddTemplate.Parameters.Add(new SqlParameter("@SelectabilityComments", SqlDbType.VarChar));
                    cmdAddTemplate.Parameters["@SelectabilityComments"].Value = model.SelectabilityComments;
                    con.Open();
                    if (await cmdAddTemplate.ExecuteNonQueryAsync() != 0)
                    {
                        con.Close();
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
        public async Task<Candidate> GetCandidateForFitmentReport(int candidateMasterId, int irfId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@candidateMasterId", candidateMasterId);
                    parameters.Add("@irfId", irfId);
                    var result = await SqlMapper.QueryAsync<Candidate>(con, "usp_GetCandidateForFitmentReport", parameters,
                         commandType: CommandType.StoredProcedure);
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
        public async Task<bool> ProcessCandidateResume(int id)
        {
            try
            {
                var query = "Update CandidateResume set IsProcessed=1 where id=@id  ";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await con.QueryAsync(query, dynamicParameters);
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
        public async Task<string> FetchCandidateResumeById(int id)
        {
            try
            {
                var query = "Select ResumeLink from  CandidateResume where id=@id  ";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await con.QueryAsync<string>(query, dynamicParameters);
                    return result.FirstOrDefault();
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }
        public async Task<string> FetchBillingownerById(int id)
        {
            try
            {
                var query = "Select ul.EmailId From IRF join UserList ul on ul.id=IRF.BillingOwner where IRF.id=@id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await con.QueryAsync<string>(query, dynamicParameters);
                    return result.FirstOrDefault();
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }
        public async Task<InterviewUsersDetails> FetchChannelOwnerByInterviewid(int interviewid)
        {
            try
            {
                var query = "Select ClientHrContact as EmailId From InterviewMeetings where im.id=@interviewid";
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
    }
}
