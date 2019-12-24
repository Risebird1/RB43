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
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    public class EvaluatorRepository: IEvaluatorRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private IApplicationErrorLogger _applicationErrorLogger;
        public EvaluatorRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<IEnumerable<CandidateResumeEvaluation>> GetOpenEvaluations(string emailID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("emailID", emailID);
                    return await SqlMapper.QueryAsync<CandidateResumeEvaluation>(con, "usp_OpenResumeEvaluations", parameters,
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

        /// <summary>
        /// Assign Resume evaluation for selectability to Resume Evaluator
        /// </summary>
        /// <param name="id"></param>
        /// <param name="mailId"></param>
        /// <returns></returns>
        public string AssignYourselfSelectability(int id, string mailId)
        {
            try
            {
                var query = "update [dbo].[TransHREvaluatorIRF] set ResumeEvaluatorEmailId=@value where id = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                dynamicParameters.Add("@value", mailId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<int>(query, dynamicParameters);
                    return "Resume evaluation assigned successfully.";
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to assign Resume Evaluation. Please contact Risebird HR.";
            }

        }
        /// <summary>
        /// Resume evaluation for joinability assignment to HR
        /// </summary>
        /// <param name="id"></param>
        /// <param name="mailId"></param>
        /// <returns></returns>
        public string AssignYourselfJoinability(int id, string mailId)
        {
            try
            {
                var query = "update [dbo].[TransHREvaluatorIRF] set HREmailId=@value where id = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                dynamicParameters.Add("@value", mailId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<int>(query, dynamicParameters);
                    return "Resume evaluation assigned successfully.";
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to assign Resume Evaluation. Please contact Risebird HR.";
            }

        }

        /// <summary>
        /// Get all assigned evaluations for selectability for Resume Evaluator
        /// </summary>
        /// <param name="emailID"></param>
        /// <returns></returns>
        public async Task<IEnumerable<CandidateResumeEvaluation>> GetAssignedSelectabilityEvaluations(string emailID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("emailID", emailID);
                    return await SqlMapper.QueryAsync<CandidateResumeEvaluation>(con, "usp_MyResumeEvaluations", parameters,
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

        public bool UnAssignSelectability(int id)
        {
            try
            {
                var query = "update TransHREvaluatorIRF set ResumeEvaluatorEmailId=null where id=@id";//select * from TransClientUser where IRFId = (select id from IRF where IRFId=@IRFId)";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<int>(query, dynamicParameters);
                    return true;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }

        }

        public IEnumerable<TransSelectability> GetTransSelectability(int id)
        {
            try
            {
                var query = "Select ts.*, sm.Name as Name from TransSelectability ts join SelectabilityMatrix sm on ts.type = sm.id where IRFId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TransSelectability>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public Candidate GetCandidate(int id)
        {
            try
            {
                var query = "select * from CandidatesMaster where id = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<Candidate>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public string GetSelectabilityComments(int irfId, string emailId)
        {
            try
            {
                var query = "select SelectabilityComments from Candidate where IRFId= @IRFId and EmailId=@EmailId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", irfId);
                dynamicParameters.Add("@EmailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<string>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public string GetJoinabilityComments(int irfId, string emailId)
        {
            try
            {
                var query = "select JoinabilityComments from Candidate where IRFId= @IRFId and EmailId=@EmailId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", irfId);
                dynamicParameters.Add("@EmailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<string>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public IEnumerable<SelectabilityEvaluatorReport> GetSelectabilityReport(int id, int irfId)
        {
            try
            {
                var query = "select SelectabilityReport.*,SelectabilityReport.Type as Name, [Level].[Name] as RequiredLevel from SelectabilityReport inner join [Level] on SelectabilityReport.RequiredLevelId=[Level].id where CandidateId = @id and SelectabilityReport.IRFId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                dynamicParameters.Add("@IRFId", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<SelectabilityEvaluatorReport>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<JoinabilityHRReport> GetJoinabilityReport(int id, int irfId)
        {
            try
            {
                var query = "select JoinabilityReport.*,JoinabilityReport.Type as Name, [Level].[Name] as RequiredLevel from JoinabilityReport inner join [Level] on JoinabilityReport.RequiredLevelId=[Level].id where CandidateId = @id and JoinabilityReport.IRFId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                dynamicParameters.Add("@IRFId", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<JoinabilityHRReport>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> AddSelectabilityReport(EvaluatorReport reportDetails)
        {
            try
            {
                bool result = false;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    //check if the data is edited
                    if (reportDetails.IsEdited)
                    {
                        if (reportDetails.SelectabilityEvaluatorReportCollection != null)
                        {
                            //update Selectability Report table
                            foreach (var item in reportDetails.SelectabilityEvaluatorReportCollection)
                            {
                                string sqlSelectableReport = "delete from SelectabilityReport where CandidateId = @CandidateId and IRFId =@IRFId";
                                var result3 = con.Execute(sqlSelectableReport, new
                                {
                                    CandidateId = reportDetails.CandidateId,
                                    ActualLevelId = item.ActualLevelId,
                                    WeightageActual = item.WeightageActual,
                                    IRFId = reportDetails.IRFId
                                });
                            }
                            result = true;
                        }


                    }
                    if (reportDetails.SelectabilityEvaluatorReportCollection != null)
                    {
                        //update Selectability Report table
                        foreach (var item in reportDetails.SelectabilityEvaluatorReportCollection)
                        {
                            string sqlSelectableReport = "INSERT INTO SelectabilityReport (CandidateId, RequiredLevelId ,Type,Value, ActualLevelId, WeightageRequired,WeightageActual, IRFId) Values (@CandidateId, @RequiredLevelId, @Type,@Value, @ActualLevelId, @WeightageRequired, @WeightageActual, @IRFId);";
                            var result3 = con.Execute(sqlSelectableReport, new
                            {
                                CandidateId = reportDetails.CandidateId,
                                RequiredLevelId = item.RequiredLevelId,
                                Type = item.Name,
                                Value = item.Value,
                                ActualLevelId = item.ActualLevelId,
                                WeightageRequired = item.WeightageRequired,
                                WeightageActual = item.WeightageActual,
                                IRFId = reportDetails.IRFId
                            });
                        }
                        result = true;
                    }
                    
                    var query = "Update Candidate set SelectabilityComments = @SelectabilityComments, SelectabilityScore =@SelectabilityScore where IRFId =@IRFId and EmailId=@EmailId ";
                    var dynamicParameters = new DynamicParameters();
                    dynamicParameters.Add("@IRFId", reportDetails.IRFId);
                    dynamicParameters.Add("@EmailId", reportDetails.CandidateEmailId);
                    dynamicParameters.Add("@SelectabilityComments", reportDetails.SelectabilityComments);
                    dynamicParameters.Add("@SelectabilityScore", reportDetails.SelectabilityScore);
                    try
                    {
                        con.Query<int>(query, dynamicParameters);
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                    return result;
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

        public async Task<IEnumerable<CandidateHREvaluation>> GetEvaluationsHR(string emailID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("emailID", emailID);
                    return await SqlMapper.QueryAsync<CandidateHREvaluation>(con, "usp_OpenHREvaluations", parameters,
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


        public async Task<IEnumerable<CandidateHREvaluation>> GetAssignedJoinabilityEvaluations(string emailID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("emailID", emailID);
                    return await SqlMapper.QueryAsync<CandidateHREvaluation>(con, "usp_MyHREvaluations", parameters,
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
        public bool UnAssignJoinability(int id)
        {
            try
            {
                var query = "update TransHREvaluatorIRF set HREmailId=null where id=@id";//select * from TransClientUser where IRFId = (select id from IRF where IRFId=@IRFId)";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);


                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<int>(query, dynamicParameters);
                    return true;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }

        }

        public async Task<bool> AddJoinabilityReport(HRReport reportDetails)
        {
            try
            {
                bool result = false;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    //check if the data is edited
                    if (reportDetails.IsEdited)
                    {
                        if (reportDetails.JoinabilityHRReportCollection != null)
                        {
                            //update Selectability Report table
                            foreach (var item in reportDetails.JoinabilityHRReportCollection)
                            {
                                string sqlSelectableReport = "delete from JoinabilityReport where CandidateId = @CandidateId and IRFId = @IRFId";
                                var result3 = con.Execute(sqlSelectableReport, new
                                {
                                    CandidateId = reportDetails.CandidateId,
                                    IRFId = reportDetails.IRFId
                                });
                            }
                            result = true;
                        }


                    }
                    if (reportDetails.JoinabilityHRReportCollection != null)
                    {
                        //update Selectability Report table
                        foreach (var item in reportDetails.JoinabilityHRReportCollection)
                        {
                            string sqlSelectableReport = "INSERT INTO JoinabilityReport (CandidateId, RequiredLevelId ,Type,Value, ActualLevelId, WeightageRequired,WeightageActual, IRFId) Values (@CandidateId, @RequiredLevelId, @Type,@Value, @ActualLevelId, @WeightageRequired, @WeightageActual, @IRFId);";
                            var result3 = con.Execute(sqlSelectableReport, new
                            {
                                CandidateId = reportDetails.CandidateId,
                                RequiredLevelId = item.RequiredLevelId,
                                Type = item.Name,
                                Value = item.Value,
                                ActualLevelId = item.ActualLevelId,
                                WeightageRequired = item.WeightageRequired,
                                WeightageActual = item.WeightageActual,
                                IRFId = reportDetails.IRFId
                            });
                        }
                        result = true;
                    }
                    
                    var query = "Update Candidate set JoinabilityComments = @JoinabilityComments, JoinabilityScore = @JoinabilityScore where IRFId =@IRFId and EmailId=@EmailId";
                    var dynamicParameters = new DynamicParameters();
                    dynamicParameters.Add("@IRFId", reportDetails.IRFId);
                    dynamicParameters.Add("@EmailId", reportDetails.CandidateEmailId);
                    dynamicParameters.Add("@JoinabilityComments", reportDetails.JoinabilityComments);
                    dynamicParameters.Add("@JoinabilityScore", reportDetails.JoinabilityScore);

                    try
                    {
                        con.Query<int>(query, dynamicParameters);
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                    return result;
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
