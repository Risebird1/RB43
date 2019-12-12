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

        public IEnumerable<SelectabilityEvaluatorReport> GetSelectabilityReport(int id)
        {
            try
            {
                var query = "select SelectabilityReport.*,SelectabilityReport.Type as Name, [Level].[Name] as RequiredLevel from SelectabilityReport inner join [Level] on SelectabilityReport.RequiredLevelId=[Level].id where CandidateId = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
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
                                string sqlSelectableReport = "update SelectabilityReport set ActualLevelId=@ActualLevelId, WeightageActual = @WeightageActual where CandidateId = @CandidateId";
                                var result3 = con.Execute(sqlSelectableReport, new
                                {
                                    CandidateId = reportDetails.CandidateId,
                                    ActualLevelId = item.ActualLevelId,
                                    WeightageActual = item.WeightageActual,
                                });
                            }
                            result = true;
                        }


                    }
                    else
                    {
                        //add to interviewReport table
                        if (reportDetails.SelectabilityEvaluatorReportCollection != null)
                        {
                            //update Selectability Report table
                            foreach (var item in reportDetails.SelectabilityEvaluatorReportCollection)
                            {
                                string sqlSelectableReport = "INSERT INTO SelectabilityReport (CandidateId, RequiredLevelId ,Type,Value, ActualLevelId, WeightageRequired,WeightageActual) Values (@CandidateId, @RequiredLevelId, @Type,@Value, @ActualLevelId, @WeightageRequired, @WeightageActual);";
                                var result3 = con.Execute(sqlSelectableReport, new
                                {
                                    CandidateId = reportDetails.CandidateId,
                                    RequiredLevelId = item.RequiredLevelId,
                                    Type = item.Name,
                                    Value = item.Value,
                                    ActualLevelId = item.ActualLevelId,
                                    WeightageRequired = item.WeightageRequired,
                                    WeightageActual = item.WeightageActual
                                });
                            }
                            result = true;
                        }

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

    }
}
