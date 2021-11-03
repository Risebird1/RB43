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
    public class EvaluatorRepository : IEvaluatorRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
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
                    parameters.Add("@emailID", emailID);
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

        public async Task<IEnumerable<TransSelectability>> GetTransSelectability(int id)
        {
            try
            {
                var query = "Select ts.*, sm.Name as Name from TransSelectability ts join SelectabilityMatrix sm on ts.type = sm.id where IRFId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<TransSelectability>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<Candidate> GetCandidate(int id)
        {
            try
            {
                var query = "select cm.id, CASE WHEN ul.LastName is not null THEN ul.FirstName + ' ' + ul.LastName ELSE ul.FirstName END as [Name], ca.ResumeLink, ul.EmailId, ca.CurrentEmployer, ca.[Location], IRF.IRFId as IRFName from CandidatesMaster cm join UserList ul on ul.id = cm.UserId join Candidate ca on ca.CandidateMasterId = cm.id join IRF on IRF.id = ca.IRFId where cm.id = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<Candidate>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<string> GetSelectabilityComments(int irfId, string emailId)
        {
            try
            {
                var query = "select SelectabilityComments from Candidate join CandidatesMaster cm on cm.id= Candidate.CandidateMasterId join UserList ul on ul.id=cm.UserId  where IRFId= @IRFId and Ul.EmailId=@EmailId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", irfId);
                dynamicParameters.Add("@EmailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<string>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<string> GetJoinabilityComments(int irfId, string emailId)
        {
            try
            {
                var query = "select JoinabilityComments from Candidate join CandidatesMaster cm on cm.id= Candidate.CandidateMasterId join UserList ul on ul.id=cm.UserId  where IRFId= @IRFId and Ul.EmailId=@EmailId";

                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", irfId);
                dynamicParameters.Add("@EmailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<string>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<string> GetTechComments(int irfId, int candidateId)
        {
            try
            {
                var query = "select TechSkillComments from Candidate where IRFId= @IRFId and CandidateMasterId = @candidateId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", irfId);
                dynamicParameters.Add("@candidateId", candidateId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<string>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<string> GetGeneralSkillComments(int irfId, int candidateId)
        {
            try
            {
                var query = "select SkillGeneralComments from Candidate where IRFId= @IRFId and CandidateMasterId = @candidateId";

                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", irfId);
                dynamicParameters.Add("@candidateId", candidateId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<string>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }


        public async Task<IEnumerable<LevelTech>> GetLevelTech()
        {
            try
            {
                var query = "select * from LevelTech";

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<LevelTech>(query);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                //_errorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<IEnumerable<SelectabilityEvaluatorReport>> GetSelectabilityReport(int id, int irfId)
        {
            try
            {
                var query = "select SelectabilityReport.*,SelectabilityReport.Type as Name, [Level].[Name] as RequiredLevel from SelectabilityReport inner join [Level] on SelectabilityReport.RequiredLevelId=[Level].id where CandidateId = @id and SelectabilityReport.IRFId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                dynamicParameters.Add("@IRFId", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<SelectabilityEvaluatorReport>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<JoinabilityHRReport>> GetJoinabilityReport(int id, int irfId)
        {
            try
            {
                var query = "select JoinabilityReport.*,JoinabilityReport.Type as Name, [Level].[Name] as RequiredLevel from JoinabilityReport inner join [Level] on JoinabilityReport.RequiredLevelId=[Level].id where CandidateId = @id and JoinabilityReport.IRFId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                dynamicParameters.Add("@IRFId", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<JoinabilityHRReport>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<TechSkillEvaluatorReport>> GetTechReport(int candidateId, int irfId)
        {
            try
            {
                var query = "select a.*, Skill.[Name] as SkillName from (select TechSkillEvaluatorReport.*, [LevelTech].[Name] as RequiredLevel from TechSkillEvaluatorReport inner join [LevelTech] on TechSkillEvaluatorReport.RequiredLevelId=[LevelTech].id) as a inner join Skill on a.SkillId = Skill.id where CandidateId = @CandidateId and IRFId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@CandidateId", candidateId);
                dynamicParameters.Add("@IRFId", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<TechSkillEvaluatorReport>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<SkillGeneralEvaluatorReport>> GetSkillGeneralReport(int candidateId, int irfId)
        {
            try
            {
                var query = "select SkillGeneralEvaluatorReport.*,SkillGeneralEvaluatorReport.[Type] as [Name], [LevelTech].[Name] as RequiredLevel from SkillGeneralEvaluatorReport inner join [LevelTech] on SkillGeneralEvaluatorReport.RequiredLevelId=[LevelTech].id where CandidateId = @CandidateId and IRFId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@CandidateId", candidateId);
                dynamicParameters.Add("@IRFId", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<SkillGeneralEvaluatorReport>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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

                    if (reportDetails.SelectabilityEvaluatorReportCollection != null)
                    {


                        string sqlSelectableReport = "delete from SelectabilityReport where CandidateId = @CandidateId and IRFId =@IRFId";
                        var result3 = con.Execute(sqlSelectableReport, new
                        {
                            reportDetails.CandidateId,
                            reportDetails.IRFId
                        });

                        DataTable selectabilityReportDt = new DataTable();
                        selectabilityReportDt.Columns.Add("RequiredLevelId", typeof(int));
                        selectabilityReportDt.Columns.Add("Type", typeof(string));
                        selectabilityReportDt.Columns.Add("ActualLevelId", typeof(int));
                        selectabilityReportDt.Columns.Add("WeightageRequired", typeof(int));
                        selectabilityReportDt.Columns.Add("WeightageActual", typeof(int));
                        selectabilityReportDt.Columns.Add("Value", typeof(string));
                        selectabilityReportDt.Columns.Add("CandidateId", typeof(int));
                        selectabilityReportDt.Columns.Add("IRFId", typeof(int));


                        foreach (var item in reportDetails.SelectabilityEvaluatorReportCollection)
                        {
                            selectabilityReportDt.Rows.Add(item.RequiredLevelId, item.Name, item.ActualLevelId, item.WeightageRequired, item.WeightageActual, item.Value, reportDetails.CandidateId, reportDetails.IRFId);
                        }

                        SqlCommand cmdselectabilityReport = new SqlCommand("usp_SelectabilityReportBulkInsert", con)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmdselectabilityReport.Parameters.Add(new SqlParameter("@SelectabilityReportBulkData", SqlDbType.Structured));
                        cmdselectabilityReport.Parameters["@SelectabilityReportBulkData"].Value = selectabilityReportDt;
                        con.Open();
                        if (cmdselectabilityReport.ExecuteNonQuery() != 0)
                        {
                            con.Close();
                        }

                        result = true;
                    }


                    var query = "Update Candidate set SelectabilityComments = @SelectabilityComments, SelectabilityScore =@SelectabilityScore, TechSkillComments = @TechSkillComments, TechSkillScore=@TechSkillScore, SkillGeneralComments=@SkillGeneralComments, SkillGeneralScore=@SkillGeneralScore where IRFId = @IRFId and CandidateMasterId = @CandidateId";
                    var dynamicParameters = new DynamicParameters();
                    dynamicParameters.Add("@IRFId", reportDetails.IRFId);
                    dynamicParameters.Add("@CandidateId", reportDetails.CandidateId);
                    dynamicParameters.Add("@SelectabilityComments", reportDetails.SelectabilityComments);
                    dynamicParameters.Add("@SelectabilityScore", reportDetails.SelectabilityScore);
                    dynamicParameters.Add("@TechSkillComments", reportDetails.TechSkillComments);
                    dynamicParameters.Add("@TechSkillScore", reportDetails.TechSkillScore);
                    dynamicParameters.Add("@SkillGeneralComments", reportDetails.SkillGeneralComments);
                    dynamicParameters.Add("@SkillGeneralScore", reportDetails.SkillGeneralScore);
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


                    if (reportDetails.JoinabilityHRReportCollection != null)
                    {
                        //update Selectability Report table

                        string sqlSelectableReport = "delete from JoinabilityReport where CandidateId = @CandidateId and IRFId = @IRFId";
                        var result3 = con.Execute(sqlSelectableReport, new
                        {
                            reportDetails.CandidateId,
                            reportDetails.IRFId
                        });

                        DataTable joinabilityReportDt = new DataTable();
                        joinabilityReportDt.Columns.Add("RequiredLevelId", typeof(int));
                        joinabilityReportDt.Columns.Add("Type", typeof(string));
                        joinabilityReportDt.Columns.Add("ActualLevelId", typeof(int));
                        joinabilityReportDt.Columns.Add("WeightageRequired", typeof(int));
                        joinabilityReportDt.Columns.Add("WeightageActual", typeof(int));
                        joinabilityReportDt.Columns.Add("Value", typeof(string));
                        joinabilityReportDt.Columns.Add("CandidateId", typeof(int));
                        joinabilityReportDt.Columns.Add("IRFId", typeof(int));


                        foreach (var item in reportDetails.JoinabilityHRReportCollection)
                        {
                            joinabilityReportDt.Rows.Add(item.RequiredLevelId, item.Name, item.ActualLevelId, item.WeightageRequired, item.WeightageActual, item.Value, reportDetails.CandidateId, reportDetails.IRFId);
                        }

                        SqlCommand cmdjoinabilityReport = new SqlCommand("usp_JoinabilityReportBulkInsert", con)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmdjoinabilityReport.Parameters.Add(new SqlParameter("@JoinabilityReportBulkData", SqlDbType.Structured));
                        cmdjoinabilityReport.Parameters["@JoinabilityReportBulkData"].Value = joinabilityReportDt;
                        con.Open();
                        if (cmdjoinabilityReport.ExecuteNonQuery() != 0)
                        {
                            con.Close();
                        }

                        result = true;
                    }


                    var query = "Update Candidate set JoinabilityComments = @JoinabilityComments, JoinabilityScore = @JoinabilityScore where IRFId = @IRFId and CandidateMasterId = @CandidateId";
                    var dynamicParameters = new DynamicParameters();
                    dynamicParameters.Add("@IRFId", reportDetails.IRFId);
                    dynamicParameters.Add("@CandidateId", reportDetails.CandidateId);
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

        public async Task<bool> AddTechReport(IList<TechSkillEvaluatorReport> reportDetails, int candidateId, int irfId)
        {
            try
            {
                bool result = false;
                using (SqlConnection con = new SqlConnection(connectionString))
                {


                    if (reportDetails != null)
                    {

                        string sqlDelete = "delete from TechSkillEvaluatorReport where CandidateId = @CandidateId and IRFId = @IRFId";
                        var result3 = con.Execute(sqlDelete, new
                        {
                            CandidateId = candidateId,
                            IRFId = irfId
                        });

                        DataTable techReportDt = new DataTable();
                        techReportDt.Columns.Add("RequiredLevelId", typeof(int));
                        techReportDt.Columns.Add("SkillId", typeof(int));
                        techReportDt.Columns.Add("ActualLevelId", typeof(int));
                        techReportDt.Columns.Add("WeightageRequired", typeof(int));
                        techReportDt.Columns.Add("WeightageActual", typeof(int));
                        techReportDt.Columns.Add("CandidateId", typeof(int));
                        techReportDt.Columns.Add("IRFId", typeof(int));


                        foreach (var item in reportDetails)
                        {
                            techReportDt.Rows.Add(item.RequiredLevelId, item.SkillId, item.ActualLevelId, item.WeightageRequired, item.WeightageActual, candidateId, irfId);
                        }

                        SqlCommand cmdtechReport = new SqlCommand("usp_TechSkillEvaluatorReportBulkInsert", con)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmdtechReport.Parameters.Add(new SqlParameter("@TechSkillEvaluatorReportBulkData", SqlDbType.Structured));
                        cmdtechReport.Parameters["@TechSkillEvaluatorReportBulkData"].Value = techReportDt;
                        con.Open();
                        if (cmdtechReport.ExecuteNonQuery() != 0)
                        {
                            con.Close();
                        }

                        result = true;
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
        public async Task<bool> AddSkillGeneralReport(IList<SkillGeneralEvaluatorReport> reportDetails, int candidateId, int irfId)
        {
            try
            {
                bool result = false;
                using (SqlConnection con = new SqlConnection(connectionString))
                {


                    if (reportDetails != null)
                    {

                        string sqlDelete = "delete from SkillGeneralEvaluatorReport where CandidateId = @CandidateId and IRFId = @IRFId";
                        var result3 = con.Execute(sqlDelete, new
                        {
                            CandidateId = candidateId,
                            IRFId = irfId
                        });


                        DataTable skillGeneralReportDt = new DataTable();
                        skillGeneralReportDt.Columns.Add("RequiredLevelId", typeof(int));
                        skillGeneralReportDt.Columns.Add("Type", typeof(string));
                        skillGeneralReportDt.Columns.Add("ActualLevelId", typeof(int));
                        skillGeneralReportDt.Columns.Add("WeightageRequired", typeof(int));
                        skillGeneralReportDt.Columns.Add("WeightageActual", typeof(int));
                        skillGeneralReportDt.Columns.Add("Value", typeof(string));
                        skillGeneralReportDt.Columns.Add("CandidateId", typeof(int));
                        skillGeneralReportDt.Columns.Add("IRFId", typeof(int));


                        foreach (var item in reportDetails)
                        {
                            skillGeneralReportDt.Rows.Add(item.RequiredLevelId, item.Name, item.ActualLevelId, item.WeightageRequired, item.WeightageActual, item.Value, candidateId, irfId);
                        }

                        SqlCommand cmdskillGeneralReport = new SqlCommand("usp_SkillGeneralEvaluatorReportBulkInsert", con)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmdskillGeneralReport.Parameters.Add(new SqlParameter("@SkillGeneralEvaluatorReportBulkData", SqlDbType.Structured));
                        cmdskillGeneralReport.Parameters["@SkillGeneralEvaluatorReportBulkData"].Value = skillGeneralReportDt;
                        con.Open();
                        if (cmdskillGeneralReport.ExecuteNonQuery() != 0)
                        {
                            con.Close();
                        }
                        result = true;
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

        public int GetUserId(string emailId, string profile)
        {
            int result = 0;
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@EmailId", emailId);
                parameters.Add("@Profile", profile);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    result = SqlMapper.QuerySingle<int>(con, "usp_GetUserId", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result;
                }
            }

            catch (SqlException e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return result;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return result;
            }
        }

        public async Task<IEnumerable<CandidateRecruiter>> GetRecruiterCandidates(int userId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@userId", userId);
                    return await SqlMapper.QueryAsync<CandidateRecruiter>(con, "usp_MyRecruiterEvaluations", parameters,
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

        public async Task<CandidateAudio> GetCandidate(int id, int irfId)
        {
            try
            {
                var query = "select cm.id as CandidateId,ul.FirstName as CandidateName,IRF.IRFId as IRFName, IRF.id as IRFId from CandidatesMaster cm join Candidate ca on cm.id=ca.CandidateMasterId join IRF on ca.IRFId=IRF.id join UserList ul on ul.id=cm.UserId where cm.id = @id and IRF.id=@IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", irfId);
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<CandidateAudio>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> AddCandidateAudio(CandidateAudio obj)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", obj.IRFId);
                dynamicParameters.Add("@CandidateId", obj.CandidateId);
                dynamicParameters.Add("@UploadedByUserId", obj.UploadedByUserId);
                dynamicParameters.Add("@AudioFileLink", obj.AudioFileLink);
                dynamicParameters.Add("@PhraseDetail", obj.PhraseDetail);
                dynamicParameters.Add("@IsVoiceBiometric", obj.IsVoiceBiometric);
                dynamicParameters.Add("@Description", obj.Description);
                if (!obj.IsVoiceBiometric)
                {
                    var query = "insert into CandidateAudio (IRFId, CandidateId, UploadedByUserId, AudioFileLink, PhraseDetail, IsVoiceBiometric, Description ) values (@IRFId, @CandidateId, @UploadedByUserId, @AudioFileLink, @PhraseDetail, @IsVoiceBiometric, @Description)";
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var result = con.Query(query, dynamicParameters);
                        return true;
                    }
                }
                else
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        SqlMapper.Query(con, "usp_AddUpdateBiometricAudio", dynamicParameters, commandType: CommandType.StoredProcedure);
                        return true;
                    }
                }


            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<IEnumerable<CandidateAudio>> GetCandidateAudios(int candidateId, int irfId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@CandidateId", candidateId);
                    parameters.Add("@IRFId", irfId);
                    return await SqlMapper.QueryAsync<CandidateAudio>(con, "usp_GetCandidateAudios", parameters,
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

        public bool UpdateCandidateVoiceUserId(string userId, int candidateMasterId, int irfId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@IRFId", irfId);
                    parameters.Add("@CandidateId", candidateMasterId);
                    parameters.Add("@VoiceItUserId", userId);
                    var result = SqlMapper.Query(con, "usp_UpdateCandidateVoiceUserId", parameters,
                         commandType: CommandType.StoredProcedure);
                    return true;
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
            return false; ;

        }
        public bool UpdateEnrollmentStatus(int id, bool isEnrolled, string message)
        {
            try
            {
                var query = "update CandidateAudio set IsEnrolled=@IsEnrolled, EnrollmentMessage=@EnrollmentMessage where id=@id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IsEnrolled", isEnrolled);
                dynamicParameters.Add("@EnrollmentMessage", message);
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query(query, dynamicParameters);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<IEnumerable<Phrase>> GetPhrases()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {

                    return await SqlMapper.QueryAsync<Phrase>(con, "usp_GetPhrases", commandType: CommandType.StoredProcedure);

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
