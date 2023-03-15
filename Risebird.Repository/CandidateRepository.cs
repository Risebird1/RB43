using Dapper;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Risebird.CommonHelper;
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
                    var result = await SqlMapper.QueryAsync<CandidateLastStatus>(con, "usp_GetCandidateLastStatus", parameters,
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
                        var result = await SqlMapper.QueryAsync<CandidateLastStatus>(con, "usp_CancelInterviewUpdate", parameters,
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
                    if (await cmdJoinability.ExecuteNonQueryAsync() != 0)
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

        public async Task<IEnumerable<CandidateCommunicationDetailsVM>> GetCandidateCommunicationDetails(int interviewId, int communicationMedium)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@interviewId", interviewId);
                dynamicParameters.Add("@CommunicationMedium", ((CommunicationMedium)communicationMedium).GetDescription());
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync<CandidateCommunicationDetailsVM>(con, "usp_GetCandidateCommunicationDetails", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return results;
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
        public async Task<Candidate> GetCandidateIRFMailDetails(string EmailId, int IRFid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("Email", EmailId);
                    parameters.Add("IRFId", IRFid);
                    var result = await SqlMapper.QueryAsync<Candidate>(con, "sp_CandidatelinkDetail", parameters,
                         commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<string> GetCandidateUniqueId(string emailId)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@emailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QuerySingleAsync<string>(con, "[usp_GetCandidateUniqueId]", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result;
                }
            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<int> GetuniqueCandidateByEmailId(string emailId, string id)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@emailId", emailId);
                parameters.Add("@uniqueid", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<int>(con, "[usp_GetuniqueCandidateByEmailId]", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }
            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return 0;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return 0;
            }
        }

        public async Task<CandidateIRFDetailsVM> GetIRFDetailsForCandidate(string emailId, int irfId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    CandidateIRFDetailsVM _resultVM = new CandidateIRFDetailsVM();
                    DataSet dataset = new DataSet();
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    adapter.SelectCommand = new SqlCommand("usp_GetIRFDetailsForCandidate", con);
                    adapter.SelectCommand.CommandType = CommandType.StoredProcedure;
                    adapter.SelectCommand.Parameters.AddWithValue("@Email", emailId);
                    adapter.SelectCommand.Parameters.AddWithValue("@irfId", irfId);
                    adapter.Fill(dataset);
                    DataTable dt = new DataTable();
                    if (dataset.Tables.Count > 0)
                    {
                        dt = dataset.Tables[0];
                        _resultVM.CandidateDetails.Name = dt.Rows[0]["Name"] != null ? dt.Rows[0]["Name"].ToString() : null;
                        _resultVM.CandidateDetails.IRFName = dt.Rows[0]["IRFName"] != null ? dt.Rows[0]["IRFName"].ToString() : null;
                        _resultVM.CandidateDetails.EmailId = dt.Rows[0]["EmailId"] != null ? dt.Rows[0]["EmailId"].ToString() : null;
                        _resultVM.CandidateDetails.Phone = dt.Rows[0]["MobileNumber"] != null ? dt.Rows[0]["MobileNumber"].ToString() : null;
                        _resultVM.CandidateDetails.JDTiltle = dt.Rows[0]["JDTiltle"] != null ? dt.Rows[0]["JDTiltle"].ToString() : null;
                        _resultVM.CandidateDetails.JDLink = dt.Rows[0]["JDLink"] != null ? dt.Rows[0]["JDLink"].ToString() : null;
                        _resultVM.CandidateDetails.ClientName = dt.Rows[0]["ClientName"] != null ? dt.Rows[0]["ClientName"].ToString() : null;
                        _resultVM.CandidateDetails.CalendlyLink = dt.Rows[0]["CalendlyLink"] != null ? dt.Rows[0]["CalendlyLink"].ToString() : null;
                        _resultVM.CandidateDetails.PrimarySkillInvite = dt.Rows[0]["PrimarySkillInvite"] != null ? dt.Rows[0]["PrimarySkillInvite"].ToString() : null;
                        _resultVM.CandidateDetails.CompanyLink = dt.Rows[0]["CompanyLink"] != null ? dt.Rows[0]["CompanyLink"].ToString() : null;
                        _resultVM.CandidateDetails.CompanyVideoLink = dt.Rows[0]["CompanyVideoLink"] != null ? dt.Rows[0]["CompanyVideoLink"].ToString() : null;
                        _resultVM.CandidateDetails.ChannelOwnerEmail = dt.Rows[0]["ChannelOwnerEmail"] != null ? dt.Rows[0]["ChannelOwnerEmail"].ToString() : null;
                        _resultVM.CandidateDetails.ChannelOwnerMobile = dt.Rows[0]["ChannelOwnerMobile"] != null ? dt.Rows[0]["ChannelOwnerMobile"].ToString() : null;
                        _resultVM.CandidateDetails.ClientHRAttemptContact = dt.Rows[0]["ClientHRAttemptContact"] != null ? dt.Rows[0]["ClientHRAttemptContact"].ToString() : null;
                        //_resultVM.CandidateDetails = !string.IsNullOrEmpty(dt.Rows[0][0].ToString()) ? Convert.ToInt32(dt.Rows[0][0]) : 0;
                    }
                    if (dataset.Tables.Count > 1)
                    {
                        dt = dataset.Tables[1];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {
                            //_resultVM.Meetings = new List<MeetingVM>();
                            var listMeetings = new List<MeetingVM>();
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                listMeetings.Add(new MeetingVM
                                {
                                    Interviewid = !string.IsNullOrEmpty(dt.Rows[i]["Interviewid"].ToString()) ? Convert.ToInt32(dt.Rows[i]["Interviewid"]) : 0,
                                    resumeLink = dt.Rows[i]["resumeLink"] != null ? dt.Rows[i]["resumeLink"].ToString() : null,
                                    zoomLink = dt.Rows[i]["zoomLink"] != null ? dt.Rows[i]["zoomLink"].ToString() : null,
                                    interviewDate = dt.Rows[i]["interviewDate"] != null ? Convert.ToDateTime(dt.Rows[i]["interviewDate"].ToString()) : new DateTime(),
                                    candidateEmail = dt.Rows[i]["candidateEmail"] != null ? dt.Rows[i]["candidateEmail"].ToString() : null,
                                    interviewSlot = dt.Rows[i]["interviewSlot"] != null ? dt.Rows[i]["interviewSlot"].ToString() : null,
                                    confirmationStatus = dt.Rows[i]["ConfirmationStatus"] != null ? dt.Rows[i]["ConfirmationStatus"].ToString() : null,
                                    MeetingStatus = dt.Rows[i]["MeetingStatus"] != null ? !string.IsNullOrWhiteSpace(dt.Rows[i]["MeetingStatus"].ToString()) ? Convert.ToInt32(dt.Rows[i]["MeetingStatus"]) : null : null,
                                    InterviewStatus = dt.Rows[i]["InterviewStatus"] != null ? !string.IsNullOrWhiteSpace(dt.Rows[i]["InterviewStatus"].ToString()) ? Convert.ToInt32(dt.Rows[i]["InterviewStatus"]) : null : null
                                });
                            }
                            _resultVM.Meetings = listMeetings;
                        }

                    }
                    //if (dataset.Tables.Count > 2)
                    //{
                    //    dt = dataset.Tables[2];
                    //    _resultVM.CodeInviteDetails.programmingLanguage = dt.Rows[0][0] != null ? dt.Rows[0][0].ToString() : null;
                    //    _resultVM.CodeInviteDetails.score = dt.Rows[0][1] != null ? Convert.ToInt32(dt.Rows[0][1].ToString()) : new int();
                    //    _resultVM.CandidateDetails.EmailId = dt.Rows[0][2] != null ? dt.Rows[0][2].ToString() : null;
                    //}
                    //if (dataset.Tables.Count > 3)
                    //{
                    //    dt = dataset.Tables[3];
                    //    _resultVM.MCQVMShare.MCQSet = dt.Rows[0][0] != null ? dt.Rows[0][0].ToString() : null;
                    //    _resultVM.MCQVMShare.MCQId = dt.Rows[0][1] != null ? dt.Rows[0][1].ToString() : null;
                    //}

                    return _resultVM;
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
