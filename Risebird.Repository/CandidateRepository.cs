using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.RbAPIDataModels;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using static Dapper.SqlMapper;

namespace Risebird.Repository
{
    public class CandidateRepository : ICandidateRepository
    {

        private readonly IConfiguration _configuration;
        private string connectionString;
        private readonly string connectionStringForTesting;
        private readonly string connectionStringForMCQDb;
        private IApplicationErrorLogger _applicationErrorLogger;
        public CandidateRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
            connectionStringForTesting = "Server=tcp:risebirdtest.database.windows.net,1433;Initial Catalog=productTestProdDB;Persist Security Info=False;User ID=risebirdtestadmin;Password=Ri$£bird@567;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
            connectionStringForMCQDb = _configuration["ConnectionString:EmploymentVerificationConnection"];
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

        public async Task<IEnumerable<CandidateExcelData>> GetCandidateStatusDataWithoutColdDate(DateTime startDate, DateTime endDate, int clientId)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@clientId", clientId);
                parameters.Add("@startDate", startDate);
                parameters.Add("@endDate", endDate);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<CandidateExcelData>(con, "usp_GetCandidateStatusDataWithoutCold", parameters,
                        commandType: CommandType.StoredProcedure, commandTimeout: 600);
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Error, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Error, ex);
            }
            return null;
        }
        public async Task<IEnumerable<CandidateExcelData>> GetCandidateStatusDataWithColdDate(DateTime startDate, DateTime endDate, int clientId)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@clientId", clientId);
                parameters.Add("@startDate", startDate);
                parameters.Add("@endDate", endDate);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<CandidateExcelData>(con, "usp_GetCandidateStatusDataWithCold", parameters,
                        commandType: CommandType.StoredProcedure, commandTimeout: 600);
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Error, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Error, ex);
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
                    var result = await SqlMapper.QueryAsync<CandidateLastStatus>(con, "usp_GetCandidateLastStatusV2", parameters,//"usp_GetCandidateLastStatus"
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
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@interviewid", interviewid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = (List<CandidateDetailsForWhatsApp>)await SqlMapper.QueryAsync<CandidateDetailsForWhatsApp>(
                        con, "usp_FetchInterviewDetailsForWhatsApp", dynamicParameters,
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
        public async Task<string> CancelInterviewDetails(int interviewid, string CandidateEmail)
        {
            try
            {
                int isvalidintervierid = 0;
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@interviewid", interviewid);
                dynamicParameters.Add("@CandidateEmail", CandidateEmail);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<int>(con, "usp_ValidateInterviewForCancel", dynamicParameters,
                        commandType: CommandType.StoredProcedure);
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
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@interviewid", interviewid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<InterviewUsersDetails>(con, "usp_FetchChannelOwnerandInterviewer", dynamicParameters,
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
        public async Task<InterviewUsersDetails> FetchInterviewerDetails(int interviewid)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@interviewid", interviewid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<InterviewUsersDetails>(con, "usp_FetchInterviewerDetailsByInterviewId", dynamicParameters,
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
                    resumeListDt.Columns.Add("ClientHREmailId", typeof(string));


                    foreach (var item in resumeList)
                    {
                        resumeListDt.Rows.Add(item.IRFId, item.ResumeLink, item.AddedBy, DateTime.Now, item.ClientHREmailId);
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

        public async Task<bool> UpdateCandidateResumeInBulk(List<CandidateResume> resumeList, bool ismcqdb = false)
        {
            try
            {
                if (ismcqdb)
                {
                    connectionString = connectionStringForMCQDb;
                }
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DataTable resumeListDt = new DataTable();
                    resumeListDt.Columns.Add("id", typeof(int));
                    resumeListDt.Columns.Add("IsProcessed", typeof(bool));
                    resumeListDt.Columns.Add("IsResumeParsed", typeof(bool));
                    resumeListDt.Columns.Add("IsResumeParsingFailed", typeof(bool));
                    resumeListDt.Columns.Add("IsEmailMissing", typeof(bool));
                    if (!ismcqdb)
                    {
                        resumeListDt.Columns.Add("ReasonBehindNotProcessed", typeof(string));
                    }

                    foreach (var item in resumeList)
                    {
                        if (ismcqdb)
                        {
                            resumeListDt.Rows.Add(item.id, item.IsProcessed, item.IsResumeParsed, item.IsResumeParsingFailed, item.IsEmailMissing);
                        }
                        else
                        {
                            resumeListDt.Rows.Add(item.id, item.IsProcessed, item.IsResumeParsed, item.IsResumeParsingFailed, item.IsEmailMissing, item.ReasonBehindNotProcessed);
                        }
                    }

                    SqlCommand cmd = new SqlCommand("usp_CandidateResumeBulkUpdate", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@CandidateResumeBulkUpdateData", SqlDbType.Structured));
                    cmd.Parameters["@CandidateResumeBulkUpdateData"].Value = resumeListDt;
                    con.Open();
                    if (await cmd.ExecuteNonQueryAsync() != 0)
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

        public async Task<IEnumerable<CandidateResume>> GetCandidatesResumes(int irfId, string emailid = null)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", irfId);
                if (string.IsNullOrWhiteSpace(emailid))
                {
                    emailid = "";
                }
                dynamicParameters.Add("@emailid", emailid);
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
        public async Task<IEnumerable<CandidateTimelineVM>> GetCandidateTimelinebyDateStatus(DateTime startDate, DateTime endDate, int datatype, string emailId = null)
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
                    return await SqlMapper.QueryAsync<CandidateTimelineVM>(con, "usp_GetCandidateTimelineByDateStatus", parameters,
                        commandType: CommandType.StoredProcedure, commandTimeout: 600);
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
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync(con, "usp_ProcessCandidateResume", dynamicParameters,
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
        public async Task<string> FetchCandidateResumeById(int id)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_FetchCandidateResumeLinkById", dynamicParameters,
                        commandType: CommandType.StoredProcedure);
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
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_FetchBillingOwnerEmailByIRFId", dynamicParameters,
                        commandType: CommandType.StoredProcedure);
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
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@interviewid", interviewid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<InterviewUsersDetails>(con, "usp_FetchChannelOwnerByInterviewId", dynamicParameters,
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

        public async Task<IEnumerable<CandidateCommunicationDetailsVM>> GetCandidateCommunicationDetails(int communicationMedium, int? interviewId = null, int? irfId = null, string emailId = null)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@interviewId", interviewId);
                dynamicParameters.Add("@irfId", irfId);
                dynamicParameters.Add("@emailId", emailId);
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
                        _resultVM.CandidateDetails.ClientHRAttemptContactMobile = dt.Rows[0]["ClientHRAttemptContactMobile"] != null ? dt.Rows[0]["ClientHRAttemptContactMobile"].ToString() : null;
                        _resultVM.CandidateDetails.LogoUrl = dt.Rows[0]["LogoLink"] != null ? dt.Rows[0]["LogoLink"].ToString() : null;
                        _resultVM.CandidateDetails.CandidateMasterId = dt.Rows[0]["CandidateMasterId"] != DBNull.Value ? (int)dt.Rows[0]["CandidateMasterId"] : 0;
                        _resultVM.CandidateDetails.CandidateIdverification = !string.IsNullOrEmpty(dt.Rows[0]["CandidateIdverification"].ToString()) ? Convert.ToBoolean(dt.Rows[0]["CandidateIdverification"]) : false;
                        _resultVM.CandidateDetails.CandidateVideo = !string.IsNullOrEmpty(dt.Rows[0]["CandidateVideo"].ToString()) ? dt.Rows[0]["CandidateVideo"].ToString() : null;
                        _resultVM.CandidateDetails.ConsentForm = !string.IsNullOrEmpty(dt.Rows[0]["ConsentForm"].ToString()) ? dt.Rows[0]["ConsentForm"].ToString() : null;
                        _resultVM.CandidateDetails.IsAIInterview = !string.IsNullOrEmpty(dt.Rows[0]["IsAIInterview"].ToString()) ? Convert.ToBoolean(dt.Rows[0]["IsAIInterview"]) : false;
                        _resultVM.CandidateDetails.IsProctoring = !string.IsNullOrEmpty(dt.Rows[0]["IsProctoring"].ToString()) ? Convert.ToBoolean(dt.Rows[0]["IsProctoring"]) : false;
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
                                    InterviewId = !string.IsNullOrEmpty(dt.Rows[i]["Interviewid"].ToString()) ? Convert.ToInt32(dt.Rows[i]["Interviewid"]) : 0,
                                    resumeLink = dt.Rows[i]["resumeLink"] != null ? dt.Rows[i]["resumeLink"].ToString() : null,
                                    zoomLink = dt.Rows[i]["zoomLink"] != null ? dt.Rows[i]["zoomLink"].ToString() : null,
                                    interviewDate = dt.Rows[i]["interviewDate"] != null ? Convert.ToDateTime(dt.Rows[i]["interviewDate"].ToString()) : new DateTime(),
                                    candidateEmail = dt.Rows[i]["candidateEmail"] != null ? dt.Rows[i]["candidateEmail"].ToString() : null,
                                    interviewSlot = dt.Rows[i]["interviewSlot"] != null ? dt.Rows[i]["interviewSlot"].ToString() : null,
                                    confirmationStatus = dt.Rows[i]["ConfirmationStatus"] != null ? dt.Rows[i]["ConfirmationStatus"].ToString() : null,
                                    MeetingStatus = dt.Rows[i]["MeetingStatus"] != null ? !string.IsNullOrWhiteSpace(dt.Rows[i]["MeetingStatus"].ToString()) ? Convert.ToInt32(dt.Rows[i]["MeetingStatus"]) : null : null,
                                    InterviewStatus = dt.Rows[i]["InterviewStatus"] != null ? !string.IsNullOrWhiteSpace(dt.Rows[i]["InterviewStatus"].ToString()) ? Convert.ToInt32(dt.Rows[i]["InterviewStatus"]) : null : null,
                                    MobileNumber = dt.Rows[i]["MobileNumber"] != null ? dt.Rows[i]["MobileNumber"].ToString() : null,
                                    TimeZoneoffset = dt.Rows[i]["TimeZoneoffset"] != null ? Convert.ToInt32(dt.Rows[i]["TimeZoneoffset"]) : 0,
                                    TimeZonesValue = dt.Rows[i]["TimeZonesValue"] != null ? dt.Rows[i]["TimeZonesValue"].ToString() : null,
                                    SessionId = dt.Rows[i]["SessionId"] != null ? dt.Rows[i]["SessionId"].ToString() : null,
                                    AadharVerificationId = dt.Rows[i]["AadharVerificationId"] != null ? Convert.ToInt32(dt.Rows[i]["AadharVerificationId"]) : 0

                                });
                            }
                            _resultVM.Meetings = listMeetings;
                        }

                    }
                    if (dataset.Tables.Count > 2)
                    {
                        dt = dataset.Tables[2];
                        if (dt.Rows.Count > 0)
                        {
                            _resultVM.Uniqueid = dt.Rows[0]["Uniqueid"] != null ? dt.Rows[0]["Uniqueid"].ToString() : null;
                        }
                    }
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

        public async Task<IEnumerable<CandidateResume>> GetCandidateResumesForParsing(bool ismcqdb = false)
        {
            try
            {
                if (ismcqdb)
                {
                    connectionString = connectionStringForMCQDb;
                }
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<CandidateResume>(con, "usp_GetCandidateResumeForParsing", null,//usp_GetCandidateResumeForParsingTesting
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
        public async Task<IEnumerable<IRFCandidateReport>> FetchSkillChannelCandidateReprots(int irfId)
        {
            try
            {

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var _resultVM = new List<IRFCandidateReport>();
                    var skillLists = new List<string>();
                    DataSet dataset = new DataSet();
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    adapter.SelectCommand = new SqlCommand("usp_FetchSkillChannelCandidateReprots", con);
                    adapter.SelectCommand.CommandType = CommandType.StoredProcedure;
                    adapter.SelectCommand.Parameters.AddWithValue("@irfId", irfId);
                    adapter.Fill(dataset);
                    DataTable dt = new DataTable();
                    if (dataset.Tables.Count > 0)
                    {
                        dt = dataset.Tables[0];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {
                            //_resultVM.Meetings = new List<MeetingVM>();
                            var listMeetings = new List<IRFCandidateReport>();
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                listMeetings.Add(new IRFCandidateReport
                                {
                                    id = !string.IsNullOrEmpty(dt.Rows[i]["id"].ToString()) ? Convert.ToInt32(dt.Rows[i]["id"]) : 0,
                                    candidateName = dt.Rows[i]["candidateName"] != null ? dt.Rows[i]["candidateName"].ToString() : null,
                                    candidateEmail = dt.Rows[i]["candidateEmail"] != null ? dt.Rows[i]["candidateEmail"].ToString() : null,
                                    interviewDate = dt.Rows[i]["interviewDate"] != null ? Convert.ToDateTime(dt.Rows[i]["interviewDate"].ToString()) : new DateTime(),
                                    IRFId = dt.Rows[i]["IRFId"] != null ? dt.Rows[i]["IRFId"].ToString() : null,
                                    JDTiltle = dt.Rows[i]["JDTiltle"] != null ? dt.Rows[i]["JDTiltle"].ToString() : null,
                                    InterviewerComment = dt.Rows[i]["InterviewerComment"] != null ? dt.Rows[i]["InterviewerComment"].ToString() : null,
                                    MeetingStatus = dt.Rows[i]["MeetingStatus"] != null ? dt.Rows[i]["MeetingStatus"].ToString() : null,
                                    InterviewStatus = dt.Rows[i]["InterviewStatus"] != null ? dt.Rows[i]["InterviewStatus"].ToString() : null,
                                    MobileNumber = dt.Rows[i]["MobileNumber"] != null ? dt.Rows[i]["MobileNumber"].ToString() : null,
                                    ClientHrContact = dt.Rows[i]["ClientHrContact"] != null ? dt.Rows[i]["ClientHrContact"].ToString() : null,
                                    VideoLink = dt.Rows[i]["VideoLink"] != null ? dt.Rows[i]["VideoLink"].ToString() : null,
                                    confirmationStatus = dt.Rows[i]["confirmationStatus"] != null ? dt.Rows[i]["confirmationStatus"].ToString() : null,
                                    CommunicationalProficiency = !string.IsNullOrEmpty(dt.Rows[i]["CommunicationalProficiency"].ToString()) ? Convert.ToInt32(dt.Rows[i]["CommunicationalProficiency"]) : 0
                                });
                            }
                            _resultVM = listMeetings;
                        }
                    }
                    if (dataset.Tables.Count > 1)
                    {
                        dt = dataset.Tables[1];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {
                            var interviewid = 0;
                            string skill = string.Empty;
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                interviewid = !string.IsNullOrEmpty(dt.Rows[i]["InterviewId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["InterviewId"]) : 0;
                                skill = dt.Rows[i]["SkillName"] != null ? dt.Rows[i]["SkillName"].ToString() : null;
                                if (_resultVM.Where(x => x.id == interviewid).Count() > 0)
                                {
                                    _resultVM.Where(x => x.id == interviewid).FirstOrDefault().TechSkillReportCollection.Add(new TechSkillReport
                                    {
                                        WeightageActual = (float)(!string.IsNullOrEmpty(dt.Rows[i]["WeightageActual"].ToString()) ? Convert.ToDecimal(dt.Rows[i]["WeightageActual"]) : 0),
                                        WeightageRequired = (float)(!string.IsNullOrEmpty(dt.Rows[i]["WeightageRequired"].ToString()) ? Convert.ToDecimal(dt.Rows[i]["WeightageRequired"]) : 0),
                                        SkillName = dt.Rows[i]["SkillName"] != null ? dt.Rows[i]["SkillName"].ToString() : null,
                                        InterviewId = !string.IsNullOrEmpty(dt.Rows[i]["InterviewId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["InterviewId"]) : 0
                                    });
                                    if (!skillLists.Contains(skill))
                                    {
                                        skillLists.Add(skill);
                                    }
                                }
                            }
                        }
                    }
                    if (_resultVM.Count() > 0)
                        _resultVM.FirstOrDefault().SkillList = skillLists;
                    return _resultVM;
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

        public async Task<bool> SaveCandidateCodedResume(Candidate model)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@id", model.id);
                    parameters.Add("@CodedResumeLink", model.CodedResumeLink);
                    parameters.Add("@IsResumeRedactAttempted", model.IsResumeRedactAttempted);
                    parameters.Add("@IsResumeRedactFailed", model.IsResumeRedactFailed);
                    var results = await SqlMapper.QueryAsync(con, "usp_SaveCandidateCodedResume", parameters,
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
        public async Task<List<MeetingVM>> GetColdCandidatesByClientRepository(string Email)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Email", Email);
                    var result = await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetMarkedColdCandidateByClient", parameters, commandTimeout: 600,
                        commandType: CommandType.StoredProcedure);
                    return result.ToList();
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<List<CandidateResume>> GetNotProcessedResumeByClientRepository(string Email)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@clientEmailId", Email);
                    var result = await SqlMapper.QueryAsync<CandidateResume>(con, "usp_GetNotProcessedResumesForClient", parameters, commandTimeout: 600,
                        commandType: CommandType.StoredProcedure);
                    return result.ToList();
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<CandidateExcelData>> GetIRFCandidateStatsForExcelDownLoad(int irfId, string emailid, int projectid)
        {
            try
            {

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var _resultVM = new List<CandidateExcelData>();
                    var skillLists = new List<string>();
                    var skillListsTwo = new List<string>();
                    DataSet dataset = new DataSet();
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    adapter.SelectCommand = new SqlCommand("usp_GetIRFCandidateStatsForExcelDownLoadV2", con);//usp_GetIRFCandidateStatsForExcelDownLoadV2//usp_GetIRFCandidateStatsForExcelDownLoad
                    adapter.SelectCommand.CommandType = CommandType.StoredProcedure;
                    adapter.SelectCommand.CommandTimeout = 600;
                    adapter.SelectCommand.Parameters.AddWithValue("@irfId", irfId);
                    adapter.SelectCommand.Parameters.AddWithValue("@timeNow", DateTime.Now);
                    adapter.SelectCommand.Parameters.AddWithValue("@projectid", projectid);
                    if (string.IsNullOrWhiteSpace(emailid))
                    {
                        emailid = "";
                    }
                    adapter.SelectCommand.Parameters.AddWithValue("@emailid", emailid);
                    SqlParameter outputParam = new SqlParameter("@numbertOFTechStacks", SqlDbType.Int);
                    outputParam.Direction = ParameterDirection.Output;
                    adapter.SelectCommand.Parameters.Add(outputParam);
                    adapter.Fill(dataset);

                    //find the value of number of tech stacks heres
                    int numberOfTechStacks = (int)adapter.SelectCommand.Parameters["@numbertOfTechStacks"].Value;
                    DataTable dt = new DataTable();
                    if (dataset.Tables.Count > 0)
                    {
                        dt = dataset.Tables[0];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {
                            //_resultVM.Meetings = new List<MeetingVM>();
                            var listCandidates = new List<CandidateExcelData>();
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                listCandidates.Add(new CandidateExcelData
                                {
                                    id = !string.IsNullOrEmpty(dt.Rows[i]["id"].ToString()) ? Convert.ToInt32(dt.Rows[i]["id"]) : 0,
                                    Irfintid = !string.IsNullOrEmpty(dt.Rows[i]["Irfintid"].ToString()) ? Convert.ToInt32(dt.Rows[i]["Irfintid"]) : 0,
                                    ClientId = !string.IsNullOrEmpty(dt.Rows[i]["ClientId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["ClientId"]) : 0,
                                    candidateName = dt.Rows[i]["candidateName"] != null ? dt.Rows[i]["candidateName"].ToString() : null,
                                    candidateEmail = dt.Rows[i]["candidateEmail"] != null ? dt.Rows[i]["candidateEmail"].ToString() : null,
                                    interviewDate = !string.IsNullOrEmpty(dt.Rows[i]["interviewDate"].ToString()) ? Convert.ToDateTime(dt.Rows[i]["interviewDate"].ToString()) : new DateTime(),
                                    IRFId = dt.Rows[i]["IRFId"] != null ? dt.Rows[i]["IRFId"].ToString() : null,
                                    JDTiltle = dt.Rows[i]["JDTiltle"] != null ? dt.Rows[i]["JDTiltle"].ToString() : null,
                                    AddedOnDate = !string.IsNullOrEmpty(dt.Rows[i]["AddedOnDate"].ToString()) ? Convert.ToDateTime(dt.Rows[i]["AddedOnDate"].ToString()) : new DateTime(),
                                    InvitationSentOnDate = !string.IsNullOrEmpty(dt.Rows[i]["InvitationSentOnDate"].ToString()) ? Convert.ToDateTime(dt.Rows[i]["InvitationSentOnDate"].ToString()) : new DateTime(),
                                    FirstScheduledOnDate = !string.IsNullOrEmpty(dt.Rows[i]["FirstScheduledOnDate"].ToString()) ? Convert.ToDateTime(dt.Rows[i]["FirstScheduledOnDate"].ToString()) : new DateTime(),
                                    completedDate = !string.IsNullOrEmpty(dt.Rows[i]["completedDate"].ToString()) ? Convert.ToDateTime(dt.Rows[i]["completedDate"].ToString()) : new DateTime(),
                                    isQcDone = dt.Rows[i]["isQcDone"] != null && dt.Rows[i]["isQcDone"] != DBNull.Value ? Convert.ToBoolean(dt.Rows[i]["isQcDone"]) : false,
                                    IsDrive = dt.Rows[i]["IsDrive"] != null && dt.Rows[i]["IsDrive"] != DBNull.Value ? Convert.ToBoolean(dt.Rows[i]["IsDrive"]) : false,
                                    RedoInterview = dt.Rows[i]["RedoInterview"] != null && dt.Rows[i]["RedoInterview"] != DBNull.Value ? Convert.ToBoolean(dt.Rows[i]["RedoInterview"]) : false,

                                    InterviewerComment = dt.Rows[i]["InterviewerComment"] != null ? dt.Rows[i]["InterviewerComment"].ToString() : null,
                                    MeetingStatus = dt.Rows[i]["MeetingStatus"] != null ? dt.Rows[i]["MeetingStatus"].ToString() : null,
                                    //InterviewStatus = dt.Rows[i]["InterviewStatus"] != null ? dt.Rows[i]["InterviewStatus"].ToString() : null,
                                    CandidateLatestStatus = dt.Rows[i]["CandidateLatestStatus"] != null ? dt.Rows[i]["CandidateLatestStatus"].ToString() : null,
                                    InterviewerCodedName = dt.Rows[i]["InterviewerCodedName"] != null ? dt.Rows[i]["InterviewerCodedName"].ToString() : null,
                                    MobileNumber = dt.Rows[i]["MobileNumber"] != null ? dt.Rows[i]["MobileNumber"].ToString() : null,
                                    ClientHrContact = dt.Rows[i]["ClientHrContact"] != null ? dt.Rows[i]["ClientHrContact"].ToString() : null,
                                    staffingAgency = dt.Rows[i]["staffingAgency"] != null ? dt.Rows[i]["staffingAgency"].ToString() : null,
                                    ResumeLink = dt.Rows[i]["ResumeLink"] != null ? dt.Rows[i]["ResumeLink"].ToString() : null,
                                    VideoLink = dt.Rows[i]["VideoLink"] != null ? dt.Rows[i]["VideoLink"].ToString() : null,
                                    confirmationStatus = dt.Rows[i]["confirmationStatus"] != null ? dt.Rows[i]["confirmationStatus"].ToString() : null,
                                    CommunicationalProficiency = !string.IsNullOrEmpty(dt.Rows[i]["CommunicationalProficiency"].ToString()) ? Convert.ToInt32(dt.Rows[i]["CommunicationalProficiency"]) : 0,

                                    MeetingStatusTwo = !string.IsNullOrEmpty(dt.Rows[i]["MeetingStatusTwo"].ToString()) ? Convert.ToInt32(dt.Rows[i]["MeetingStatusTwo"]) : null,
                                    InterviewStatusTwo = !string.IsNullOrEmpty(dt.Rows[i]["InterviewStatusTwo"].ToString()) ? Convert.ToInt32(dt.Rows[i]["InterviewStatusTwo"]) : null,
                                    InterviewerCommentTwo = !string.IsNullOrEmpty(dt.Rows[i]["InterviewerCommentTwo"].ToString()) ? dt.Rows[i]["InterviewerCommentTwo"].ToString() : null,
                                    PreReportLink = dt.Rows[i]["PreReportLink"] != null ? dt.Rows[i]["PreReportLink"].ToString() : null,
                                    PreReportUsername = dt.Rows[i]["PreReportUsername"] != null ? dt.Rows[i]["PreReportUsername"].ToString() : null,
                                    PreReportPassword = dt.Rows[i]["PreReportPassword"] != null ? dt.Rows[i]["PreReportPassword"].ToString() : null,
                                    CandidateType = CandidateSingleSourceTruthTypes.Completed
                                });
                            }
                            _resultVM = listCandidates;
                        }
                    }
                    if (dataset.Tables.Count > 1)
                    {
                        dt = dataset.Tables[1];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {
                            var interviewid = 0;
                            string skill = string.Empty;
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                interviewid = !string.IsNullOrEmpty(dt.Rows[i]["InterviewId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["InterviewId"]) : 0;
                                skill = dt.Rows[i]["SkillName"] != null ? dt.Rows[i]["SkillName"].ToString() : null;
                                if (_resultVM.Where(x => x.id == interviewid).Count() > 0)
                                {
                                    _resultVM.Where(x => x.id == interviewid).FirstOrDefault().TechSkillReportCollection.Add(new TechSkillReport
                                    {
                                        WeightageActual = (float)(!string.IsNullOrEmpty(dt.Rows[i]["WeightageActual"].ToString()) ? Convert.ToDecimal(dt.Rows[i]["WeightageActual"]) : 0),
                                        WeightageRequired = (float)(!string.IsNullOrEmpty(dt.Rows[i]["WeightageRequired"].ToString()) ? Convert.ToDecimal(dt.Rows[i]["WeightageRequired"]) : 0),
                                        SkillName = dt.Rows[i]["SkillName"] != null ? dt.Rows[i]["SkillName"].ToString() : null,
                                        InterviewId = !string.IsNullOrEmpty(dt.Rows[i]["InterviewId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["InterviewId"]) : 0
                                    });
                                    if (!skillLists.Contains(skill))
                                    {
                                        skillLists.Add(skill);
                                    }
                                }
                            }
                        }
                    }
                    if (_resultVM.Count() > 0)
                        _resultVM.FirstOrDefault().SkillList = skillLists;

                    if (dataset.Tables.Count > 2)
                    {
                        dt = dataset.Tables[2];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {
                            //_resultVM.Meetings = new List<MeetingVM>();
                            var listCandidates = new List<CandidateExcelData>();
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                listCandidates.Add(new CandidateExcelData
                                {
                                    id = !string.IsNullOrEmpty(dt.Rows[i]["id"].ToString()) ? Convert.ToInt32(dt.Rows[i]["id"]) : 0,
                                    Irfintid = !string.IsNullOrEmpty(dt.Rows[i]["Irfintid"].ToString()) ? Convert.ToInt32(dt.Rows[i]["Irfintid"]) : 0,
                                    ClientId = !string.IsNullOrEmpty(dt.Rows[i]["ClientId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["ClientId"]) : 0,
                                    candidateName = dt.Rows[i]["candidateName"] != null ? dt.Rows[i]["candidateName"].ToString() : null,
                                    candidateEmail = dt.Rows[i]["candidateEmail"] != null ? dt.Rows[i]["candidateEmail"].ToString() : null,
                                    interviewDate = dt.Rows[i]["interviewDate"] != null ? Convert.ToDateTime(dt.Rows[i]["interviewDate"].ToString()) : new DateTime(),
                                    IRFId = dt.Rows[i]["IRFId"] != null ? dt.Rows[i]["IRFId"].ToString() : null,
                                    JDTiltle = dt.Rows[i]["JDTiltle"] != null ? dt.Rows[i]["JDTiltle"].ToString() : null,
                                    AddedOnDate = dt.Rows[i]["AddedOnDate"] != null ? Convert.ToDateTime(dt.Rows[i]["AddedOnDate"].ToString()) : new DateTime(),
                                    InvitationSentOnDate = dt.Rows[i]["InvitationSentOnDate"] != null ? Convert.ToDateTime(dt.Rows[i]["InvitationSentOnDate"].ToString()) : new DateTime(),
                                    FirstScheduledOnDate = dt.Rows[i]["FirstScheduledOnDate"] != null ? Convert.ToDateTime(dt.Rows[i]["FirstScheduledOnDate"].ToString()) : new DateTime(),

                                    InterviewerComment = dt.Rows[i]["InterviewerComment"] != null ? dt.Rows[i]["InterviewerComment"].ToString() : null,
                                    //MeetingStatus = dt.Rows[i]["MeetingStatus"] != null ? dt.Rows[i]["MeetingStatus"].ToString() : null,
                                    //InterviewStatus = dt.Rows[i]["InterviewStatus"] != null ? dt.Rows[i]["InterviewStatus"].ToString() : null,
                                    CandidateLatestStatus = dt.Rows[i]["CandidateLatestStatus"] != null ? dt.Rows[i]["CandidateLatestStatus"].ToString() : null,
                                    InterviewerCodedName = dt.Rows[i]["InterviewerCodedName"] != null ? dt.Rows[i]["InterviewerCodedName"].ToString() : null,
                                    MobileNumber = dt.Rows[i]["MobileNumber"] != null ? dt.Rows[i]["MobileNumber"].ToString() : null,
                                    ClientHrContact = dt.Rows[i]["ClientHrContact"] != null ? dt.Rows[i]["ClientHrContact"].ToString() : null,
                                    staffingAgency = dt.Rows[i]["staffingAgency"] != null ? dt.Rows[i]["staffingAgency"].ToString() : null,
                                    ResumeLink = dt.Rows[i]["ResumeLink"] != null ? dt.Rows[i]["ResumeLink"].ToString() : null,
                                    VideoLink = dt.Rows[i]["VideoLink"] != null ? dt.Rows[i]["VideoLink"].ToString() : null,
                                    confirmationStatus = dt.Rows[i]["confirmationStatus"] != null ? dt.Rows[i]["confirmationStatus"].ToString() : null,
                                    CommunicationalProficiency = !string.IsNullOrEmpty(dt.Rows[i]["CommunicationalProficiency"].ToString()) ? Convert.ToInt32(dt.Rows[i]["CommunicationalProficiency"]) : 0,
                                    PreReportLink = dt.Rows[i]["PreReportLink"] != null ? dt.Rows[i]["PreReportLink"].ToString() : null,
                                    PreReportUsername = dt.Rows[i]["PreReportUsername"] != null ? dt.Rows[i]["PreReportUsername"].ToString() : null,
                                    PreReportPassword = dt.Rows[i]["PreReportPassword"] != null ? dt.Rows[i]["PreReportPassword"].ToString() : null,
                                    isQcDone = dt.Rows[i]["isQcDone"] != null && dt.Rows[i]["isQcDone"] != DBNull.Value ? Convert.ToBoolean(dt.Rows[i]["isQcDone"]) : false,
                                    RedoInterview = dt.Rows[i]["RedoInterview"] != null && dt.Rows[i]["RedoInterview"] != DBNull.Value ? Convert.ToBoolean(dt.Rows[i]["RedoInterview"]) : false,

                                    CandidateType = CandidateSingleSourceTruthTypes.Pending
                                });
                            }
                            _resultVM.AddRange(listCandidates);
                        }
                    }

                    if (dataset.Tables.Count > 3)
                    {
                        dt = dataset.Tables[3];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {
                            //_resultVM.Meetings = new List<MeetingVM>();
                            var listCandidates = new List<CandidateExcelData>();
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                listCandidates.Add(new CandidateExcelData
                                {
                                    id = !string.IsNullOrEmpty(dt.Rows[i]["id"].ToString()) ? Convert.ToInt32(dt.Rows[i]["id"]) : 0,
                                    Irfintid = !string.IsNullOrEmpty(dt.Rows[i]["Irfintid"].ToString()) ? Convert.ToInt32(dt.Rows[i]["Irfintid"]) : 0,
                                    ClientId = !string.IsNullOrEmpty(dt.Rows[i]["ClientId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["ClientId"]) : 0,
                                    candidateName = dt.Rows[i]["candidateName"] != null ? dt.Rows[i]["candidateName"].ToString() : null,
                                    candidateEmail = dt.Rows[i]["candidateEmail"] != null ? dt.Rows[i]["candidateEmail"].ToString() : null,
                                    interviewDate = dt.Rows[i]["interviewDate"] != null ? Convert.ToDateTime(dt.Rows[i]["interviewDate"].ToString()) : new DateTime(),
                                    IRFId = dt.Rows[i]["IRFId"] != null ? dt.Rows[i]["IRFId"].ToString() : null,
                                    JDTiltle = dt.Rows[i]["JDTiltle"] != null ? dt.Rows[i]["JDTiltle"].ToString() : null,

                                    AddedOnDate = dt.Rows[i]["AddedOnDate"] != null ? Convert.ToDateTime(dt.Rows[i]["AddedOnDate"].ToString()) : new DateTime(),
                                    InvitationSentOnDate = dt.Rows[i]["InvitationSentOnDate"] != null ? Convert.ToDateTime(dt.Rows[i]["InvitationSentOnDate"].ToString()) : new DateTime(),
                                    FirstScheduledOnDate = dt.Rows[i]["FirstScheduledOnDate"] != null ? Convert.ToDateTime(dt.Rows[i]["FirstScheduledOnDate"].ToString()) : new DateTime(),

                                    //InterviewerComment = dt.Rows[i]["InterviewerComment"] != null ? dt.Rows[i]["InterviewerComment"].ToString() : null,
                                    //MeetingStatus = dt.Rows[i]["MeetingStatus"] != null ? dt.Rows[i]["MeetingStatus"].ToString() : null,
                                    //InterviewStatus = dt.Rows[i]["InterviewStatus"] != null ? dt.Rows[i]["InterviewStatus"].ToString() : null,
                                    CandidateLatestStatus = dt.Rows[i]["CandidateLatestStatus"] != null ? dt.Rows[i]["CandidateLatestStatus"].ToString() : null,
                                    InterviewerCodedName = dt.Rows[i]["InterviewerCodedName"] != null ? dt.Rows[i]["InterviewerCodedName"].ToString() : null,
                                    MobileNumber = dt.Rows[i]["MobileNumber"] != null ? dt.Rows[i]["MobileNumber"].ToString() : null,
                                    ClientHrContact = dt.Rows[i]["ClientHrContact"] != null ? dt.Rows[i]["ClientHrContact"].ToString() : null,
                                    staffingAgency = dt.Rows[i]["staffingAgency"] != null ? dt.Rows[i]["staffingAgency"].ToString() : null,
                                    ResumeLink = dt.Rows[i]["ResumeLink"] != null ? dt.Rows[i]["ResumeLink"].ToString() : null,
                                    //VideoLink = dt.Rows[i]["VideoLink"] != null ? dt.Rows[i]["VideoLink"].ToString() : null,
                                    confirmationStatus = dt.Rows[i]["confirmationStatus"] != null ? dt.Rows[i]["confirmationStatus"].ToString() : null,
                                    //CommunicationalProficiency = !string.IsNullOrEmpty(dt.Rows[i]["CommunicationalProficiency"].ToString()) ? Convert.ToInt32(dt.Rows[i]["CommunicationalProficiency"]) : 0,
                                    PreReportLink = dt.Rows[i]["PreReportLink"] != null ? dt.Rows[i]["PreReportLink"].ToString() : null,
                                    PreReportUsername = dt.Rows[i]["PreReportUsername"] != null ? dt.Rows[i]["PreReportUsername"].ToString() : null,
                                    PreReportPassword = dt.Rows[i]["PreReportPassword"] != null ? dt.Rows[i]["PreReportPassword"].ToString() : null,
                                    CandidateType = CandidateSingleSourceTruthTypes.FutureScheduled
                                });
                            }
                            _resultVM.AddRange(listCandidates);
                        }
                    }
                    if (dataset.Tables.Count > 4)
                    {
                        dt = dataset.Tables[4];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {
                            //_resultVM.Meetings = new List<MeetingVM>();
                            var listCandidates = new List<CandidateExcelData>();
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                listCandidates.Add(new CandidateExcelData
                                {
                                    id = !string.IsNullOrEmpty(dt.Rows[i]["id"].ToString()) ? Convert.ToInt32(dt.Rows[i]["id"]) : 0,
                                    Irfintid = !string.IsNullOrEmpty(dt.Rows[i]["Irfintid"].ToString()) ? Convert.ToInt32(dt.Rows[i]["Irfintid"]) : 0,
                                    ClientId = !string.IsNullOrEmpty(dt.Rows[i]["ClientId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["ClientId"]) : 0,
                                    candidateName = dt.Rows[i]["candidateName"] != null ? dt.Rows[i]["candidateName"].ToString() : null,
                                    candidateEmail = dt.Rows[i]["candidateEmail"] != null ? dt.Rows[i]["candidateEmail"].ToString() : null,
                                    interviewDate = !string.IsNullOrEmpty(dt.Rows[i]["interviewDate"].ToString()) ? Convert.ToDateTime(dt.Rows[i]["interviewDate"].ToString()) : new DateTime(),
                                    IRFId = dt.Rows[i]["IRFId"] != null ? dt.Rows[i]["IRFId"].ToString() : null,
                                    JDTiltle = dt.Rows[i]["JDTiltle"] != null ? dt.Rows[i]["JDTiltle"].ToString() : null,

                                    AddedOnDate = !string.IsNullOrEmpty(dt.Rows[i]["AddedOnDate"].ToString()) ? Convert.ToDateTime(dt.Rows[i]["AddedOnDate"].ToString()) : new DateTime(),
                                    InvitationSentOnDate = !string.IsNullOrEmpty(dt.Rows[i]["InvitationSentOnDate"].ToString()) ? Convert.ToDateTime(dt.Rows[i]["InvitationSentOnDate"].ToString()) : new DateTime(),
                                    FirstScheduledOnDate = !string.IsNullOrEmpty(dt.Rows[i]["FirstScheduledOnDate"].ToString()) ? Convert.ToDateTime(dt.Rows[i]["FirstScheduledOnDate"].ToString()) : new DateTime(),


                                    //InterviewerComment = dt.Rows[i]["InterviewerComment"] != null ? dt.Rows[i]["InterviewerComment"].ToString() : null,

                                    MeetingStatus = dt.Rows[i]["MeetingStatus"] != null ? dt.Rows[i]["MeetingStatus"].ToString() : null,
                                    InterviewStatus = dt.Rows[i]["InterviewStatus"] != null ? dt.Rows[i]["InterviewStatus"].ToString() : null,
                                    CandidateLatestStatus = dt.Rows[i]["CandidateLatestStatus"] != null ? dt.Rows[i]["CandidateLatestStatus"].ToString() : null,
                                    InterviewerCodedName = dt.Rows[i]["InterviewerCodedName"] != null ? dt.Rows[i]["InterviewerCodedName"].ToString() : null,
                                    MobileNumber = dt.Rows[i]["MobileNumber"] != null ? dt.Rows[i]["MobileNumber"].ToString() : null,
                                    ClientHrContact = dt.Rows[i]["ClientHrContact"] != null ? dt.Rows[i]["ClientHrContact"].ToString() : null,
                                    staffingAgency = dt.Rows[i]["staffingAgency"] != null ? dt.Rows[i]["staffingAgency"].ToString() : null,
                                    ResumeLink = dt.Rows[i]["ResumeLink"] != null ? dt.Rows[i]["ResumeLink"].ToString() : null,
                                    VideoLink = dt.Rows[i]["VideoLink"] != null ? dt.Rows[i]["VideoLink"].ToString() : null,
                                    confirmationStatus = dt.Rows[i]["confirmationStatus"] != null ? dt.Rows[i]["confirmationStatus"].ToString() : null,
                                    //CommunicationalProficiency = !string.IsNullOrEmpty(dt.Rows[i]["CommunicationalProficiency"].ToString()) ? Convert.ToInt32(dt.Rows[i]["CommunicationalProficiency"]) : 0,
                                    PreReportLink = dt.Rows[i]["PreReportLink"] != null ? dt.Rows[i]["PreReportLink"].ToString() : null,
                                    PreReportUsername = dt.Rows[i]["PreReportUsername"] != null ? dt.Rows[i]["PreReportUsername"].ToString() : null,
                                    PreReportPassword = dt.Rows[i]["PreReportPassword"] != null ? dt.Rows[i]["PreReportPassword"].ToString() : null,
                                    isQcDone = dt.Rows[i]["isQcDone"] != null && dt.Rows[i]["isQcDone"] != DBNull.Value ? Convert.ToBoolean(dt.Rows[i]["isQcDone"]) : false,
                                    RedoInterview = dt.Rows[i]["RedoInterview"] != null && dt.Rows[i]["RedoInterview"] != DBNull.Value ? Convert.ToBoolean(dt.Rows[i]["RedoInterview"]) : false,
                                    CandidateType = CandidateSingleSourceTruthTypes.Reschedule
                                });
                            }
                            _resultVM.AddRange(listCandidates);
                        }
                    }
                    if (dataset.Tables.Count > 5)
                    {
                        dt = dataset.Tables[5];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {
                            //_resultVM.Meetings = new List<MeetingVM>();
                            var listCandidates = new List<CandidateExcelData>();
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                listCandidates.Add(new CandidateExcelData
                                {
                                    //id = !string.IsNullOrEmpty(dt.Rows[i]["id"].ToString()) ? Convert.ToInt32(dt.Rows[i]["id"]) : 0,
                                    Irfintid = !string.IsNullOrEmpty(dt.Rows[i]["Irfintid"].ToString()) ? Convert.ToInt32(dt.Rows[i]["Irfintid"]) : 0,
                                    ClientId = !string.IsNullOrEmpty(dt.Rows[i]["ClientId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["ClientId"]) : 0,
                                    candidateName = dt.Rows[i]["candidateName"] != null ? dt.Rows[i]["candidateName"].ToString() : null,
                                    candidateEmail = dt.Rows[i]["candidateEmail"] != null ? dt.Rows[i]["candidateEmail"].ToString() : null,
                                    //interviewDate = dt.Rows[i]["interviewDate"] != null ? Convert.ToDateTime(dt.Rows[i]["interviewDate"].ToString()) : new DateTime(),
                                    IRFId = dt.Rows[i]["IRFId"] != null ? dt.Rows[i]["IRFId"].ToString() : null,
                                    JDTiltle = dt.Rows[i]["JDTiltle"] != null ? dt.Rows[i]["JDTiltle"].ToString() : null,
                                    AddedOnDate = dt.Rows[i]["AddedOnDate"] != null ? Convert.ToDateTime(dt.Rows[i]["AddedOnDate"].ToString()) : new DateTime(),
                                    InvitationSentOnDate = dt.Rows[i]["InvitationSentOnDate"] != null ? Convert.ToDateTime(dt.Rows[i]["InvitationSentOnDate"].ToString()) : new DateTime(),
                                    //FirstScheduledOnDate = dt.Rows[i]["FirstScheduledOnDate"] != null ? Convert.ToDateTime(dt.Rows[i]["FirstScheduledOnDate"].ToString()) : new DateTime(),


                                    //InterviewerComment = dt.Rows[i]["InterviewerComment"] != null ? dt.Rows[i]["InterviewerComment"].ToString() : null,
                                    //MeetingStatus = dt.Rows[i]["MeetingStatus"] != null ? dt.Rows[i]["MeetingStatus"].ToString() : null,
                                    //InterviewStatus = dt.Rows[i]["InterviewStatus"] != null ? dt.Rows[i]["InterviewStatus"].ToString() : null,
                                    CandidateLatestStatus = dt.Rows[i]["CandidateLatestStatus"] != null ? dt.Rows[i]["CandidateLatestStatus"].ToString() : null,
                                    MobileNumber = dt.Rows[i]["MobileNumber"] != null ? dt.Rows[i]["MobileNumber"].ToString() : null,
                                    ClientHrContact = dt.Rows[i]["ClientHrContact"] != null ? dt.Rows[i]["ClientHrContact"].ToString() : null,
                                    //staffingAgency = dt.Rows[i]["staffingAgency"] != null ? dt.Rows[i]["staffingAgency"].ToString() : null,
                                    ResumeLink = dt.Rows[i]["ResumeLink"] != null ? dt.Rows[i]["ResumeLink"].ToString() : null,
                                    //VideoLink = dt.Rows[i]["VideoLink"] != null ? dt.Rows[i]["VideoLink"].ToString() : null,
                                    //confirmationStatus = dt.Rows[i]["confirmationStatus"] != null ? dt.Rows[i]["confirmationStatus"].ToString() : null,
                                    //CommunicationalProficiency = !string.IsNullOrEmpty(dt.Rows[i]["CommunicationalProficiency"].ToString()) ? Convert.ToInt32(dt.Rows[i]["CommunicationalProficiency"]) : 0,
                                    PreReportLink = dt.Rows[i]["PreReportLink"] != null ? dt.Rows[i]["PreReportLink"].ToString() : null,
                                    PreReportUsername = dt.Rows[i]["PreReportUsername"] != null ? dt.Rows[i]["PreReportUsername"].ToString() : null,
                                    PreReportPassword = dt.Rows[i]["PreReportPassword"] != null ? dt.Rows[i]["PreReportPassword"].ToString() : null,
                                    CandidateType = (dt.Rows[i]["CandidateLatestStatus"] != null && dt.Rows[i]["CandidateLatestStatus"].ToString() == "NOTSCHEDULED") ? CandidateSingleSourceTruthTypes.NotScheduled : CandidateSingleSourceTruthTypes.Cold
                                });
                            }
                            _resultVM.AddRange(listCandidates);
                        }
                    }
                    if (dataset.Tables.Count > 6)
                    {
                        dt = dataset.Tables[6];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {
                            int interviewid = 0;
                            string skill = string.Empty;
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                interviewid = !string.IsNullOrEmpty(dt.Rows[i]["InterviewId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["InterviewId"]) : 0;
                                skill = dt.Rows[i]["SkillName"] != null ? dt.Rows[i]["SkillName"].ToString() : null;
                                if (_resultVM.Where(x => x.id == interviewid) != null && _resultVM.Where(x => x.id == interviewid).Count() > 0)
                                {
                                    _resultVM.Where(x => x.id == interviewid).FirstOrDefault().TechSkillReportTwoCollection.Add(new TechSkillReport
                                    {
                                        WeightageActual = (float)(!string.IsNullOrEmpty(dt.Rows[i]["WeightageActual"].ToString()) ? Convert.ToDecimal(dt.Rows[i]["WeightageActual"]) : 0),
                                        WeightageRequired = (float)(!string.IsNullOrEmpty(dt.Rows[i]["WeightageRequired"].ToString()) ? Convert.ToDecimal(dt.Rows[i]["WeightageRequired"]) : 0),
                                        SkillName = dt.Rows[i]["SkillName"] != null ? dt.Rows[i]["SkillName"].ToString() : null,
                                        InterviewId = !string.IsNullOrEmpty(dt.Rows[i]["InterviewId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["InterviewId"]) : 0
                                    });
                                    if (!skillListsTwo.Contains(skill))
                                    {
                                        skillListsTwo.Add(skill);
                                    }
                                }
                            }
                        }
                    }
                    if (_resultVM.Count() > 0)
                        _resultVM.FirstOrDefault().SkillListTwo = skillListsTwo;
                    return _resultVM;
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

        public async Task<Candidate> GetCandidateOpenAIScreeningResults(int irfId, int candidateMasterId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@irfId", irfId);
                    parameters.Add("@candidateMasterId", candidateMasterId);
                    var result = await SqlMapper.QueryAsync<Candidate>(con, "usp_GetCandidateOpenAIScreeningResults", parameters, commandTimeout: 600,
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

        public async Task<bool> SaveCandidateOpenAIScreeningResults(int irfId, int candidateMasterId, string screeningResponseJson)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@irfId", irfId);
                    parameters.Add("@candidateMasterId", candidateMasterId);
                    parameters.Add("@screeningResponseJson", screeningResponseJson);
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_SaveCandidateOpenAIScreeningResults", parameters, commandTimeout: 600,  //usp_SaveCandidateOpenAIScreeningResults
                        commandType: CommandType.StoredProcedure);
                    return true;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }

        public async Task<bool> SaveCandidateOpenAIScreeningResultsV2(int irfId, int candidateMasterId, string screeningResponseJson, string verdict, int resumeScreeningScore)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@irfId", irfId);
                    parameters.Add("@candidateMasterId", candidateMasterId);
                    parameters.Add("@screeningResponseJson", screeningResponseJson);
                    parameters.Add("@verdict", verdict);
                    parameters.Add("@resumeScreeningScore", resumeScreeningScore);
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_SaveCandidateResumeAIScreeningResultsV2", parameters, commandTimeout: 600,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }

        //public async Task<CandidateStatus> GetCandidateLatestStatusForApi(string skillChannelName, string candidateEmailId, string requestedByClientUser)
        //{
        //    try
        //    {
        //        var dynamicParameters = new DynamicParameters();
        //        dynamicParameters.Add("@skillChannelName", skillChannelName);
        //        dynamicParameters.Add("@candidateEmailId", candidateEmailId);
        //        dynamicParameters.Add("@requestedByClientUser", requestedByClientUser);
        //        using (SqlConnection con = new SqlConnection(connectionString))
        //        {
        //            var results = await SqlMapper.QueryAsync<CandidateStatus>(con, "usp_GetCandidateLatestStatusForApi", dynamicParameters,
        //               commandType: CommandType.StoredProcedure);
        //            return results?.FirstOrDefault();
        //        }
        //    }
        //    catch (SqlException ex)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        return null;
        //    }
        //}
        public async Task<IEnumerable<InterviewChannelDetails>> GetCandidateLatestStatusForApi(string skillChannelName, string candidateEmailId, string requestedByClientUser, string jobId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var _resultVM = new List<InterviewChannelDetails>();
                    var skillLists = new List<string>();
                    var skillListsTwo = new List<string>();
                    DataSet dataset = new DataSet();
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    //Both skillChannelName and jobId are optional (the caller supplies at least one).
                    //Passing a C# null via AddWithValue makes SQL Server treat the parameter as "not
                    //supplied" and throw, so normalize missing values to empty string, which the SP
                    //already interprets as "not provided".
                    if (string.IsNullOrWhiteSpace(skillChannelName))
                    {
                        skillChannelName = "";
                    }
                    if (string.IsNullOrWhiteSpace(jobId))
                    {
                        jobId = "";
                    }
                    adapter.SelectCommand = new SqlCommand("usp_GetCandidateLatestStatusForApi", con);
                    adapter.SelectCommand.CommandType = CommandType.StoredProcedure;
                    adapter.SelectCommand.CommandTimeout = 600;
                    adapter.SelectCommand.Parameters.AddWithValue("@skillChannelName", skillChannelName);
                    adapter.SelectCommand.Parameters.AddWithValue("@candidateEmailId", candidateEmailId);
                    adapter.SelectCommand.Parameters.AddWithValue("@requestedByClientUser", requestedByClientUser);
                    adapter.SelectCommand.Parameters.AddWithValue("@jobId", jobId);
                    adapter.SelectCommand.Parameters.AddWithValue("@timeNow", DateTime.Now);

                    adapter.Fill(dataset);

                    //find the value of number of tech stacks heres
                    DataTable dt = new DataTable();
                    if (dataset.Tables.Count > 0)
                    {
                        dt = dataset.Tables[0];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {
                            //_resultVM.Meetings = new List<MeetingVM>();
                            var listCandidates = new List<InterviewChannelDetails>();
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                listCandidates.Add(new InterviewChannelDetails
                                {
                                    id = !string.IsNullOrEmpty(dt.Rows[i]["id"].ToString()) ? Convert.ToInt32(dt.Rows[i]["id"]) : 0,
                                    irfintid = !string.IsNullOrEmpty(dt.Rows[i]["irfintid"].ToString()) ? Convert.ToInt32(dt.Rows[i]["irfintid"]) : 0,
                                    candidateEmail = dt.Rows[i]["candidateEmail"] != null ? dt.Rows[i]["candidateEmail"].ToString() : null,
                                    interviewDate = !string.IsNullOrEmpty(dt.Rows[i]["interviewDate"].ToString()) ? Convert.ToDateTime(dt.Rows[i]["interviewDate"].ToString()) : null,
                                    IRFId = dt.Rows[i]["IRFId"] != null ? dt.Rows[i]["IRFId"].ToString() : null,
                                    JDTiltle = dt.Rows[i]["JDTiltle"] != null ? dt.Rows[i]["JDTiltle"].ToString() : null,

                                    InterviewerComment = dt.Rows[i]["InterviewerComment"] != null ? dt.Rows[i]["InterviewerComment"].ToString() : null,
                                    MeetingStatus = !string.IsNullOrEmpty(dt.Rows[i]["MeetingStatus"].ToString()) ? Convert.ToInt32(dt.Rows[i]["MeetingStatus"]) : null,
                                    InterviewStatus = !string.IsNullOrEmpty(dt.Rows[i]["InterviewStatus"].ToString()) ? Convert.ToInt32(dt.Rows[i]["InterviewStatus"]) : null,
                                    CandidateLatestStatus = dt.Rows[i]["CandidateLatestStatus"] != null ? dt.Rows[i]["CandidateLatestStatus"].ToString() : null,
                                    InterviewerCodedName = dt.Rows[i]["InterviewerCodedName"] != null ? dt.Rows[i]["InterviewerCodedName"].ToString() : null,
                                    VideoLink = dt.Rows[i]["VideoLink"] != null ? dt.Rows[i]["VideoLink"].ToString() : null,
                                    confirmationStatus = dt.Rows[i]["confirmationStatus"] != null ? dt.Rows[i]["confirmationStatus"].ToString() : null,
                                    CommunicationalProficiency = !string.IsNullOrEmpty(dt.Rows[i]["CommunicationalProficiency"].ToString()) ? Convert.ToInt32(dt.Rows[i]["CommunicationalProficiency"]) : 0,
                                    TechScoreone = !string.IsNullOrEmpty(dt.Rows[i]["TechScore"].ToString()) ? Convert.ToInt32(dt.Rows[i]["TechScore"]) : 0,
                                    TechScoretwo = !string.IsNullOrEmpty(dt.Rows[i]["TechScoreTwo"].ToString()) ? Convert.ToInt32(dt.Rows[i]["TechScoreTwo"]) : 0,

                                    JDTitleTwo = dt.Rows[i]["JDTitleTwo"] != null ? dt.Rows[i]["JDTitleTwo"].ToString() : null,
                                    MeetingStatusTwo = !string.IsNullOrEmpty(dt.Rows[i]["MeetingStatusTwo"].ToString()) ? Convert.ToInt32(dt.Rows[i]["MeetingStatusTwo"]) : null,
                                    InterviewStatusTwo = !string.IsNullOrEmpty(dt.Rows[i]["InterviewStatusTwo"].ToString()) ? Convert.ToInt32(dt.Rows[i]["InterviewStatusTwo"]) : null,
                                    numberOfTechStacks = !string.IsNullOrEmpty(dt.Rows[i]["SkillSetsevaluate"].ToString()) ? Convert.ToInt32(dt.Rows[i]["SkillSetsevaluate"]) : 1,
                                    InterviewerCommentTwo = !string.IsNullOrEmpty(dt.Rows[i]["InterviewerCommentTwo"].ToString()) ? dt.Rows[i]["InterviewerCommentTwo"].ToString() : null,
                                    createdOn = !string.IsNullOrEmpty(dt.Rows[i]["addedOn"].ToString()) ? Convert.ToDateTime(dt.Rows[i]["addedOn"].ToString()) : new DateTime(),
                                    currentStatusUpdatedAt = !string.IsNullOrEmpty(dt.Rows[i]["statusUpdatedAt"].ToString()) ? Convert.ToDateTime(dt.Rows[i]["statusUpdatedAt"].ToString()) : null,
                                    jobId = dt.Rows[i]["jobId"] != null ? dt.Rows[i]["jobId"].ToString() : null,
                                    TranscriptLink = dt.Rows[i]["AudioTranscriptFileLink"] != null ? dt.Rows[i]["AudioTranscriptFileLink"].ToString() : string.Empty,
                                    CodeSnippet = dt.Rows[i]["CodeSnippet"] != null ? dt.Rows[i]["CodeSnippet"].ToString() : string.Empty,
                                    CodeSnippetTwo = dt.Rows[i]["CodeSnippetTwo"] != null ? dt.Rows[i]["CodeSnippetTwo"].ToString() : string.Empty,
                                    Cancellationsource = dt.Rows[i]["Cancellationsource"] != null ? dt.Rows[i]["Cancellationsource"].ToString() : string.Empty,
                                    Cancellationreason = dt.Rows[i]["Cancellationreason"] != null ? dt.Rows[i]["Cancellationreason"].ToString() : string.Empty,
                                    ClientHRAttemptContact = dt.Rows[i]["ClientHRAttemptContact"] != null ? dt.Rows[i]["ClientHRAttemptContact"].ToString() : string.Empty,
                                    CandidateId = !string.IsNullOrEmpty(dt.Rows[i]["CandidateId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["CandidateId"]) : 0,
                                    IsAIInterview = dt.Columns.Contains("IsAIInterview") && !string.IsNullOrEmpty(dt.Rows[i]["IsAIInterview"].ToString()) ? Convert.ToBoolean(dt.Rows[i]["IsAIInterview"]) : false,
                                    SessionId = dt.Columns.Contains("SessionId") && dt.Rows[i]["SessionId"] != null ? dt.Rows[i]["SessionId"].ToString() : string.Empty,
                                });
                            }
                            _resultVM = listCandidates;
                        }
                    }
                    if (dataset.Tables.Count > 1)
                    {
                        dt = dataset.Tables[1];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {
                            var interviewid = 0;
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                interviewid = !string.IsNullOrEmpty(dt.Rows[i]["InterviewId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["InterviewId"]) : 0;
                                if (_resultVM.Where(x => x.id == interviewid).Count() > 0)
                                {
                                    _resultVM.Where(x => x.id == interviewid).FirstOrDefault().questions.Add(new QuestionDetailsSet
                                    {
                                        rating = !string.IsNullOrEmpty(dt.Rows[i]["RatingId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["RatingId"]) : 0,
                                        question = !string.IsNullOrEmpty(dt.Rows[i]["Comment"].ToString()) ? dt.Rows[i]["Comment"].ToString() : "",
                                        skillName = dt.Rows[i]["SkillName"] != null ? dt.Rows[i]["SkillName"].ToString() : "",
                                        skillSet = !string.IsNullOrEmpty(dt.Rows[i]["SkillSet"].ToString()) ? Convert.ToInt32(dt.Rows[i]["SkillSet"]) : 1
                                    });
                                }
                            }
                        }
                    }
                    if (dataset.Tables.Count > 2)
                    {
                        dt = dataset.Tables[2];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {
                            var interviewid = 0;
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                interviewid = !string.IsNullOrEmpty(dt.Rows[i]["InterviewId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["InterviewId"]) : 0;
                                if (_resultVM.Where(x => x.id == interviewid).Count() > 0)
                                {
                                    _resultVM.Where(x => x.id == interviewid).FirstOrDefault().skillset.Add(new SkillsSets
                                    {
                                        WeightageActual = !string.IsNullOrEmpty(dt.Rows[i]["WeightageActual"].ToString()) ? Convert.ToInt32(dt.Rows[i]["WeightageActual"]) : 0,
                                        WeightageRequired = !string.IsNullOrEmpty(dt.Rows[i]["WeightageRequired"].ToString()) ? Convert.ToInt32(dt.Rows[i]["WeightageRequired"]) : 0,
                                        EvaluatedAt = dt.Rows[i]["EvaluatedAt"] != null ? dt.Rows[i]["EvaluatedAt"].ToString() : "",
                                        skillName = dt.Rows[i]["SkillName"] != null ? dt.Rows[i]["SkillName"].ToString() : "",
                                        skillSet = !string.IsNullOrEmpty(dt.Rows[i]["SkillSet"].ToString()) ? Convert.ToInt32(dt.Rows[i]["SkillSet"]) : 1
                                    });
                                }
                            }
                        }
                    }
                    return _resultVM;//?.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<IEnumerable<InterviewChannelDetails>> FetchInterviewChannelCandidateInformation(int irfid, int id, string requestedBy, DateTime? startDate, DateTime? endDate, string jobId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var _resultVM = new List<InterviewChannelDetails>();
                    var skillLists = new List<string>();
                    var skillListsTwo = new List<string>();
                    DataSet dataset = new DataSet();
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    adapter.SelectCommand = new SqlCommand("usp_FetchInterviewChannelCandidateInformation", con);
                    adapter.SelectCommand.CommandType = CommandType.StoredProcedure;
                    adapter.SelectCommand.CommandTimeout = 600;
                    adapter.SelectCommand.Parameters.AddWithValue("@irfId", irfid);
                    adapter.SelectCommand.Parameters.AddWithValue("@clientid", id);
                    adapter.SelectCommand.Parameters.AddWithValue("@ClientUser", requestedBy);
                    adapter.SelectCommand.Parameters.AddWithValue("@timeNow", DateTime.Now);
                    if (string.IsNullOrWhiteSpace(jobId))
                    {
                        jobId = "";
                    }
                    adapter.SelectCommand.Parameters.AddWithValue("@jobId", jobId);

                    if (startDate != null && endDate != null)
                    {
                        adapter.SelectCommand.Parameters.AddWithValue("@startdate", startDate);
                        adapter.SelectCommand.Parameters.AddWithValue("@enddate", endDate);
                    }
                    else
                    {
                        adapter.SelectCommand.Parameters.AddWithValue("@startdate", null);
                        adapter.SelectCommand.Parameters.AddWithValue("@enddate", null);
                    }
                    adapter.Fill(dataset);

                    //find the value of number of tech stacks heres
                    DataTable dt = new DataTable();
                    if (dataset.Tables.Count > 0)
                    {
                        dt = dataset.Tables[0];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {
                            //_resultVM.Meetings = new List<MeetingVM>();
                            var listCandidates = new List<InterviewChannelDetails>();
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                listCandidates.Add(new InterviewChannelDetails
                                {
                                    id = !string.IsNullOrEmpty(dt.Rows[i]["id"].ToString()) ? Convert.ToInt32(dt.Rows[i]["id"]) : 0,
                                    irfintid = !string.IsNullOrEmpty(dt.Rows[i]["irfintid"].ToString()) ? Convert.ToInt32(dt.Rows[i]["irfintid"]) : 0,
                                    candidateEmail = dt.Rows[i]["candidateEmail"] != null ? dt.Rows[i]["candidateEmail"].ToString() : null,
                                    interviewDate = !string.IsNullOrEmpty(dt.Rows[i]["interviewDate"].ToString()) ? Convert.ToDateTime(dt.Rows[i]["interviewDate"].ToString()) : new DateTime(),
                                    IRFId = dt.Rows[i]["IRFId"] != null ? dt.Rows[i]["IRFId"].ToString() : null,
                                    JDTiltle = dt.Rows[i]["JDTiltle"] != null ? dt.Rows[i]["JDTiltle"].ToString() : null,

                                    InterviewerComment = dt.Rows[i]["InterviewerComment"] != null ? dt.Rows[i]["InterviewerComment"].ToString() : null,
                                    MeetingStatus = !string.IsNullOrEmpty(dt.Rows[i]["MeetingStatus"].ToString()) ? Convert.ToInt32(dt.Rows[i]["MeetingStatus"]) : null,
                                    InterviewStatus = !string.IsNullOrEmpty(dt.Rows[i]["InterviewStatus"].ToString()) ? Convert.ToInt32(dt.Rows[i]["InterviewStatus"]) : null,
                                    CandidateLatestStatus = dt.Rows[i]["CandidateLatestStatus"] != null ? dt.Rows[i]["CandidateLatestStatus"].ToString() : null,
                                    InterviewerCodedName = dt.Rows[i]["InterviewerCodedName"] != null ? dt.Rows[i]["InterviewerCodedName"].ToString() : null,
                                    VideoLink = dt.Rows[i]["VideoLink"] != null ? dt.Rows[i]["VideoLink"].ToString() : null,
                                    confirmationStatus = dt.Rows[i]["confirmationStatus"] != null ? dt.Rows[i]["confirmationStatus"].ToString() : null,
                                    CommunicationalProficiency = !string.IsNullOrEmpty(dt.Rows[i]["CommunicationalProficiency"].ToString()) ? Convert.ToInt32(dt.Rows[i]["CommunicationalProficiency"]) : 0,
                                    TechScoreone = !string.IsNullOrEmpty(dt.Rows[i]["TechScore"].ToString()) ? Convert.ToInt32(dt.Rows[i]["TechScore"]) : 0,
                                    TechScoretwo = !string.IsNullOrEmpty(dt.Rows[i]["TechScoreTwo"].ToString()) ? Convert.ToInt32(dt.Rows[i]["TechScoreTwo"]) : 0,

                                    JDTitleTwo = dt.Rows[i]["JDTitleTwo"] != null ? dt.Rows[i]["JDTitleTwo"].ToString() : null,
                                    MeetingStatusTwo = !string.IsNullOrEmpty(dt.Rows[i]["MeetingStatusTwo"].ToString()) ? Convert.ToInt32(dt.Rows[i]["MeetingStatusTwo"]) : null,
                                    InterviewStatusTwo = !string.IsNullOrEmpty(dt.Rows[i]["InterviewStatusTwo"].ToString()) ? Convert.ToInt32(dt.Rows[i]["InterviewStatusTwo"]) : null,
                                    numberOfTechStacks = !string.IsNullOrEmpty(dt.Rows[i]["SkillSetsevaluate"].ToString()) ? Convert.ToInt32(dt.Rows[i]["SkillSetsevaluate"]) : 1,
                                    InterviewerCommentTwo = !string.IsNullOrEmpty(dt.Rows[i]["InterviewerCommentTwo"].ToString()) ? dt.Rows[i]["InterviewerCommentTwo"].ToString() : null,

                                    createdOn = !string.IsNullOrEmpty(dt.Rows[i]["addedOn"].ToString()) ? Convert.ToDateTime(dt.Rows[i]["addedOn"].ToString()) : null,
                                    currentStatusUpdatedAt = !string.IsNullOrEmpty(dt.Rows[i]["statusUpdatedAt"].ToString()) ? Convert.ToDateTime(dt.Rows[i]["statusUpdatedAt"].ToString()) : null,
                                    jobId = dt.Rows[i]["jobId"] != null ? dt.Rows[i]["jobId"].ToString() : null,
                                    TranscriptLink = dt.Rows[i]["AudioTranscriptFileLink"] != null ? dt.Rows[i]["AudioTranscriptFileLink"].ToString() : string.Empty,

                                    CodeSnippet = dt.Rows[i]["CodeSnippet"] != null ? dt.Rows[i]["CodeSnippet"].ToString() : string.Empty,
                                    CodeSnippetTwo = dt.Rows[i]["CodeSnippetTwo"] != null ? dt.Rows[i]["CodeSnippetTwo"].ToString() : string.Empty,
                                    ClientHRAttemptContact = dt.Rows[i]["ClientHRAttemptContact"] != null ? dt.Rows[i]["ClientHRAttemptContact"].ToString() : string.Empty,

                                    CandidateId = !string.IsNullOrEmpty(dt.Rows[i]["CandidateId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["CandidateId"]) : 0,
                                    IsAIInterview = dt.Columns.Contains("IsAIInterview") && !string.IsNullOrEmpty(dt.Rows[i]["IsAIInterview"].ToString()) ? Convert.ToBoolean(dt.Rows[i]["IsAIInterview"]) : false,
                                    SessionId = dt.Columns.Contains("SessionId") && dt.Rows[i]["SessionId"] != null ? dt.Rows[i]["SessionId"].ToString() : string.Empty,
                                });
                            }
                            _resultVM = listCandidates;
                        }
                    }
                    if (dataset.Tables.Count > 1)
                    {
                        dt = dataset.Tables[1];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {
                            var interviewid = 0;
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                interviewid = !string.IsNullOrEmpty(dt.Rows[i]["InterviewId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["InterviewId"]) : 0;
                                if (_resultVM.Where(x => x.id == interviewid).Count() > 0)
                                {
                                    _resultVM.Where(x => x.id == interviewid).FirstOrDefault().questions.Add(new QuestionDetailsSet
                                    {
                                        rating = !string.IsNullOrEmpty(dt.Rows[i]["RatingId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["RatingId"]) : 0,
                                        question = !string.IsNullOrEmpty(dt.Rows[i]["Comment"].ToString()) ? dt.Rows[i]["Comment"].ToString() : "",
                                        skillName = dt.Rows[i]["SkillName"] != null ? dt.Rows[i]["SkillName"].ToString() : "",
                                        skillSet = !string.IsNullOrEmpty(dt.Rows[i]["SkillSet"].ToString()) ? Convert.ToInt32(dt.Rows[i]["SkillSet"]) : 1
                                    });
                                }
                            }
                        }
                    }
                    if (dataset.Tables.Count > 2)
                    {
                        dt = dataset.Tables[2];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {
                            var interviewid = 0;
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                interviewid = !string.IsNullOrEmpty(dt.Rows[i]["InterviewId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["InterviewId"]) : 0;
                                if (_resultVM.Where(x => x.id == interviewid).Count() > 0)
                                {
                                    _resultVM.Where(x => x.id == interviewid).FirstOrDefault().skillset.Add(new SkillsSets
                                    {
                                        WeightageActual = !string.IsNullOrEmpty(dt.Rows[i]["WeightageActual"].ToString()) ? Convert.ToInt32(dt.Rows[i]["WeightageActual"]) : 0,
                                        WeightageRequired = !string.IsNullOrEmpty(dt.Rows[i]["WeightageRequired"].ToString()) ? Convert.ToInt32(dt.Rows[i]["WeightageRequired"]) : 0,
                                        EvaluatedAt = dt.Rows[i]["EvaluatedAt"] != null ? dt.Rows[i]["EvaluatedAt"].ToString() : "",
                                        skillName = dt.Rows[i]["SkillName"] != null ? dt.Rows[i]["SkillName"].ToString() : "",
                                        skillSet = !string.IsNullOrEmpty(dt.Rows[i]["SkillSet"].ToString()) ? Convert.ToInt32(dt.Rows[i]["SkillSet"]) : 1
                                    });
                                }
                            }
                        }
                    }
                    return _resultVM;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<IEnumerable<CandidateStatusWebHookData>> GetCandidateStatusForWebHook(bool isStaging = false)
        {
            try
            {
                if (isStaging)
                {
                    connectionString = connectionStringForTesting;
                }
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@timeNow", DateTime.Now);
                    var result = await SqlMapper.QueryAsync<CandidateStatusWebHookData>(con, "usp_GetCandidateStatusWebHookData", parameters, commandTimeout: 600,
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
        public async Task<bool> DeleteDeliveredCandidateStatusForWebHook(List<int> data, bool isStaging = false)
        {
            try
            {
                if (isStaging)
                {
                    connectionString = connectionStringForTesting;
                }
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DataTable dataDt = new DataTable();
                    dataDt.Columns.Add("id", typeof(int));

                    foreach (var item in data)
                    {
                        dataDt.Rows.Add(item);
                    }

                    SqlCommand cmd = new SqlCommand("usp_CandidateWebHookDataBulkDelete", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@CandidateWebHookBulkData", SqlDbType.Structured));
                    cmd.Parameters["@CandidateWebHookBulkData"].Value = dataDt;
                    con.Open();
                    if (await cmd.ExecuteNonQueryAsync() != 0)
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
        public async Task<IEnumerable<AIInterviewStatusWebHookData>> GetAIInterviewStatusForWebHook(bool isStaging = false)
        {
            try
            {
                if (isStaging)
                {
                    connectionString = connectionStringForTesting;
                }
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@timeNow", DateTime.Now);
                    var result = await SqlMapper.QueryAsync<AIInterviewStatusWebHookData>(con, "usp_GetAIInterviewStatusWebHookData", parameters, commandTimeout: 600,
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
        public async Task<bool> DeleteDeliveredAIInterviewStatusForWebHook(List<int> data, bool isStaging = false)
        {
            try
            {
                if (isStaging)
                {
                    connectionString = connectionStringForTesting;
                }
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DataTable dataDt = new DataTable();
                    dataDt.Columns.Add("id", typeof(int));

                    foreach (var item in data)
                    {
                        dataDt.Rows.Add(item);
                    }

                    SqlCommand cmd = new SqlCommand("usp_AIInterviewStatusWebHookDataBulkDelete", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@AIInterviewWebHookBulkData", SqlDbType.Structured));
                    cmd.Parameters["@AIInterviewWebHookBulkData"].Value = dataDt;
                    con.Open();
                    if (await cmd.ExecuteNonQueryAsync() != 0)
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
        public async Task<IEnumerable<CandidateTimelineVM>> GetAllCandidateSearchbyDateStatus(DateTime startDate, DateTime endDate, int datatype, string emailId = null)
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
                    return await SqlMapper.QueryAsync<CandidateTimelineVM>(con, "usp_GetAllCandidateSearchbyDateStatus", parameters,
                        commandType: CommandType.StoredProcedure, commandTimeout: 600);
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
        public async Task<IEnumerable<CountryCodeDetails>> GetAllCountryCodeDetails()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<CountryCodeDetails>(con, "usp_GetAllCountryCodeDetails", null,
                        commandType: CommandType.StoredProcedure, commandTimeout: 600);
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
        public async Task<bool> UpdateCandidateAcknowledge(int interviewid, string interviewchecks)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    try
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@interviewid", interviewid);
                        parameters.Add("@interviewchecks", interviewchecks);
                        var result = await SqlMapper.QueryAsync<IRF>(con, "dbo.usp_UpdateCandidateAcknowledge", parameters, null,
                            commandType: CommandType.StoredProcedure);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    }
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
        public async Task<AddCandidatesResponse> AddMCQSkillChannelCandidateList(List<CandidateQuickAddModel> candidateList)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionStringForMCQDb))
                {
                    DataTable _candidateList = new DataTable();
                    _candidateList.Columns.Add("IRFId", typeof(int));
                    _candidateList.Columns.Add("ResumeLink", typeof(string));
                    _candidateList.Columns.Add("CandidateEmail", typeof(string));
                    _candidateList.Columns.Add("AddedBy", typeof(string));
                    _candidateList.Columns.Add("AddedOn", typeof(DateTime));
                    _candidateList.Columns.Add("Location", typeof(string));
                    _candidateList.Columns.Add("CurrentEmployer", typeof(string));
                    _candidateList.Columns.Add("CandidateName", typeof(string));
                    foreach (var item in candidateList)
                    {
                        _candidateList.Rows.Add(item.IRFId, item.ResumeLink, item.EmailId, item.ClientHREmail, DateTime.Now, "", "", item.Name);
                    }
                    SqlCommand cmd = new SqlCommand("usp_AddSkillChannelCandidateList", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@IRFCandidateList", SqlDbType.Structured));
                    cmd.Parameters["@IRFCandidateList"].Value = _candidateList;
                    con.Open();
                    var _candidateresponse = new AddCandidatesResponse();
                    var _listresponse = new List<CandidateOnboardingStatus>();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _listresponse.Add(new CandidateOnboardingStatus
                            {
                                emailId = reader["CandidateEmail"]?.ToString(),
                                onboardingStatus = reader["AddedStatus"]?.ToString()
                            });
                        }
                    }
                    _candidateresponse.list = _listresponse;
                    //con.Close();
                    return _candidateresponse;
                }
            }
            catch (SqlException e)
            {
            }
            catch (Exception e)
            {
            }
            return null;
        }

        public async Task<Candidate> GetCandidateResumeSummaryById(int candidateId)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@candidateId", candidateId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QuerySingleAsync<Candidate>(con, "[usp_GetCandidateResumeSummaryById]", parameters,
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

        public async Task<bool> SaveCandidateResumeSummaryAsJsonStringById(int candidateId, string resumeSummaryAsJsonString)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@candidateId", candidateId);
                parameters.Add("@resumeSummaryAsJsonString", resumeSummaryAsJsonString);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync(con, "[usp_SaveCandidateResumeSummaryAsJsonStringById]", parameters,
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

        public async Task<Candidate> GetDetailsForCandidateResumeScreeningByAI(int irfId, int candidateId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", irfId);
                dynamicParameters.Add("@CandidateId", candidateId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync<Candidate>(con, "usp_GetDetailsForCandidateResumeScreeningByAI", dynamicParameters,
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

        public async Task<CandidateResume> GetCandidateResumeById(int candidateResumeId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await con.QueryFirstOrDefaultAsync<CandidateResume>(
                        @"SELECT cr.*, irf.IRFId AS SkillChannelName, irf.IsAffindaIndexCreated
                          FROM dbo.CandidateResume cr
                          LEFT JOIN dbo.IRF irf ON irf.id = cr.IRFId
                          WHERE cr.id = @Id",
                        new { Id = candidateResumeId });
                    return result;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<CandidateResumeInfo> GetCandidateByResumeId(int candidateResumeId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await con.QueryFirstOrDefaultAsync<CandidateResumeInfo>(
                        @"SELECT 
                            cr.id AS CandidateResumeId,
                            c.id AS CandidateId,
                            c.CandidateMasterId,
                            ul.FirstName + ' ' + ul.LastName AS CandidateName,
                            ul.EmailId AS CandidateEmail,
                            c.Phone AS CandidatePhone
                          FROM dbo.CandidateResume cr
                          LEFT JOIN dbo.Candidate c ON c.IRFId = cr.IRFId AND c.ResumeLink = cr.ResumeLink
                          LEFT JOIN dbo.CandidatesMaster cm ON cm.id = c.CandidateMasterId
                          LEFT JOIN dbo.UserList ul ON ul.id = cm.UserId
                          WHERE cr.id = @Id",
                        new { Id = candidateResumeId });
                    return result;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<IDictionary<string, string>> EnsureCandidatePseudonymsAsync(IEnumerable<string> candidateEmails, string createdBy = null)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var emails = candidateEmails?
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            if (emails.Count == 0)
            {
                return result;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await con.OpenAsync();

                    var existingRows = await con.QueryAsync<(string CandidateEmail, string NickName)>(
                        "SELECT CandidateEmail, NickName FROM dbo.CandidatePseudonym WHERE CandidateEmail IN @emails",
                        new { emails });

                    foreach (var row in existingRows)
                    {
                        if (!string.IsNullOrWhiteSpace(row.CandidateEmail))
                        {
                            result[row.CandidateEmail.Trim()] = row.NickName;
                        }
                    }

                    var takenNickNames = new HashSet<string>(
                        (await con.QueryAsync<string>("SELECT NickName FROM dbo.CandidatePseudonym"))
                            .Where(n => !string.IsNullOrWhiteSpace(n)),
                        StringComparer.OrdinalIgnoreCase);

                    foreach (var email in emails)
                    {
                        if (result.Keys.Any(k => string.Equals(k, email, StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }

                        var nameRow = await con.QueryFirstOrDefaultAsync<CandidatePseudonymNameRow>(
                            @"SELECT ul.FirstName, ul.LastName, cm.id AS CandidateMasterId, cm.Name AS MasterName
                              FROM dbo.UserList ul
                              LEFT JOIN dbo.CandidatesMaster cm ON cm.UserId = ul.id
                              WHERE LOWER(LTRIM(RTRIM(ul.EmailId))) = LOWER(LTRIM(RTRIM(@email)))",
                            new { email });

                        var seedId = nameRow?.CandidateMasterId
                            ?? Math.Abs(StringComparer.OrdinalIgnoreCase.GetHashCode(email));
                        string nickName;
                        if (nameRow != null && (!string.IsNullOrWhiteSpace(nameRow.FirstName) || !string.IsNullOrWhiteSpace(nameRow.LastName)))
                        {
                            nickName = CandidatePseudonymHelper.BuildNickName(nameRow.FirstName, nameRow.LastName, seedId, takenNickNames);
                        }
                        else if (nameRow != null && !string.IsNullOrWhiteSpace(nameRow.MasterName))
                        {
                            var parts = nameRow.MasterName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            nickName = CandidatePseudonymHelper.BuildNickName(
                                parts.Length > 0 ? parts[0] : null,
                                parts.Length > 1 ? parts[1] : null,
                                seedId,
                                takenNickNames);
                        }
                        else
                        {
                            nickName = CandidatePseudonymHelper.BuildNickNameFromEmail(email, seedId, takenNickNames);
                        }

                        takenNickNames.Add(nickName);

                        await con.ExecuteAsync(
                            @"INSERT INTO dbo.CandidatePseudonym (CandidateMasterId, CandidateEmail, NickName, CreatedBy)
                              VALUES (@CandidateMasterId, @CandidateEmail, @NickName, @CreatedBy)",
                            new
                            {
                                CandidateMasterId = nameRow?.CandidateMasterId,
                                CandidateEmail = email,
                                NickName = nickName,
                                CreatedBy = createdBy
                            });

                        result[email] = nickName;
                    }
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            return result;
        }

        public async Task<CandidatePseudonymResolveVM> ResolveCandidatePseudonymAsync(string nickName)
        {
            if (string.IsNullOrWhiteSpace(nickName))
            {
                return null;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await con.QueryFirstOrDefaultAsync<CandidatePseudonymResolveVM>(
                        @"SELECT cp.NickName,
                                 cp.CandidateEmail,
                                 COALESCE(NULLIF(LTRIM(RTRIM(cm.Name)), ''), NULLIF(LTRIM(RTRIM(ul.FirstName + ' ' + ul.LastName)), ''), cp.CandidateEmail) AS CandidateName,
                                 cp.CandidateMasterId
                          FROM dbo.CandidatePseudonym cp
                          LEFT JOIN dbo.UserList ul ON LOWER(LTRIM(RTRIM(ul.EmailId))) = LOWER(LTRIM(RTRIM(cp.CandidateEmail)))
                          LEFT JOIN dbo.CandidatesMaster cm ON cm.id = cp.CandidateMasterId OR cm.UserId = ul.id
                          WHERE cp.NickName = @nickName",
                        new { nickName = nickName.Trim() });
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        private sealed class CandidatePseudonymNameRow
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public int? CandidateMasterId { get; set; }
            public string MasterName { get; set; }
        }

    }
}