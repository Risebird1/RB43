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
    public class ClonableSkillChannelTemplateRepository : IClonableSkillChannelTemplateRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public ClonableSkillChannelTemplateRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<IEnumerable<ClonableSkillChannelVM>> GetAllClonableSkillChannels()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<ClonableSkillChannelVM>(con, "usp_GetAllClonableSkillChannels", commandType: CommandType.StoredProcedure);
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
        public async Task<bool> CreateClonableSkillChannel(ClonableSkillChannelVM templateDetails)
        {
            try
            {

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DataTable joinabilityDt = new DataTable();
                    if (templateDetails.Joinability != null && templateDetails.Joinability.Count() > 0)
                    {
                        //DataTable joinabilityDt = new DataTable();
                        joinabilityDt.Columns.Add("IRFId", typeof(int));
                        joinabilityDt.Columns.Add("LevelId", typeof(int));
                        joinabilityDt.Columns.Add("Type", typeof(string));
                        joinabilityDt.Columns.Add("Value", typeof(string));
                        joinabilityDt.Columns.Add("Weightage", typeof(int));


                        foreach (var item in templateDetails.Joinability)
                        {
                            joinabilityDt.Rows.Add(templateDetails.id, item.LevelId, item.Type, item.Value, item.Weightage);
                        }

                    }

                    DataTable selectabilityDt = new DataTable();
                    if (templateDetails.Selectability != null && templateDetails.Selectability.Count() > 0 )
                    {
                        selectabilityDt.Columns.Add("IRFId", typeof(int));
                        selectabilityDt.Columns.Add("LevelId", typeof(int));
                        selectabilityDt.Columns.Add("Type", typeof(string));
                        selectabilityDt.Columns.Add("Value", typeof(string));
                        selectabilityDt.Columns.Add("Weightage", typeof(int));

                        foreach (var item in templateDetails.Selectability)
                        {
                            selectabilityDt.Rows.Add(templateDetails.id, item.LevelId, item.Type, item.Value, item.Weightage);
                        }

                    }

                    DataTable skillGeneralDt = new DataTable();
                    if (templateDetails.SkillGeneral != null && templateDetails.SkillGeneral.Count() > 0 )
                    {
                        skillGeneralDt.Columns.Add("IRFId", typeof(int));
                        skillGeneralDt.Columns.Add("LevelId", typeof(int));
                        skillGeneralDt.Columns.Add("Type", typeof(string));
                        skillGeneralDt.Columns.Add("Value", typeof(string));
                        skillGeneralDt.Columns.Add("Weightage", typeof(int));

                        foreach (var item in templateDetails.SkillGeneral)
                        {
                            skillGeneralDt.Rows.Add(templateDetails.id, item.LevelId, item.Type, item.Value, item.Weightage);
                        }

                    }

                    DataTable techSkillDt = new DataTable();
                    techSkillDt.Columns.Add("IRFId", typeof(int));
                    techSkillDt.Columns.Add("LevelId", typeof(int));
                    techSkillDt.Columns.Add("SkillId", typeof(int));
                    techSkillDt.Columns.Add("Weightage", typeof(int));
                    techSkillDt.Columns.Add("Concepts", typeof(string));
                    techSkillDt.Columns.Add("Natureofquestion", typeof(string));
                    techSkillDt.Columns.Add("Timelimit", typeof(int));

                    foreach (var item in templateDetails.SkillCollection)
                    {
                        if (String.IsNullOrEmpty(item.Concepts))
                        {
                            item.Concepts = "";
                        }
                        if (String.IsNullOrEmpty(item.Natureofquestion))
                        {
                            item.Natureofquestion = "";
                        }
                        techSkillDt.Rows.Add(templateDetails.id, item.LevelId, item.SkillId, item.Weightage, item.Concepts, item.Natureofquestion, item.Timelimit);
                    }

                    DataTable interviewerDt = new DataTable();

                    if (templateDetails.InterviewerCollection != null && templateDetails.InterviewerCollection.Count() != 0 && !(templateDetails.InterviewerCollection.Where(x => x.UserId == 0).Any()))
                    {
                        interviewerDt.Columns.Add("IRFId", typeof(int));
                        interviewerDt.Columns.Add("UserId", typeof(int));
                        interviewerDt.Columns.Add("PerSuccessRate", typeof(int));
                        interviewerDt.Columns.Add("IsActive", typeof(bool));
                        interviewerDt.Columns.Add("IsAvailable", typeof(bool));

                        foreach (var item in templateDetails.InterviewerCollection)
                        {
                            interviewerDt.Rows.Add(templateDetails.id, item.UserId, item.PerSuccessRate, item.IsActive, item.IsAvailable);
                        }
                    }

                    DataTable questionsDt = new DataTable();

                    if (templateDetails.ClonableTemplateQuestionsCollection != null && templateDetails.ClonableTemplateQuestionsCollection.Count() != 0)
                    {
                        questionsDt.Columns.Add("Skill", typeof(int));
                        questionsDt.Columns.Add("Question", typeof(string));
                        questionsDt.Columns.Add("UserId", typeof(int));

                        foreach (var item in templateDetails.ClonableTemplateQuestionsCollection)
                        {
                            questionsDt.Rows.Add(item.SkillId, item.Question, templateDetails.UserId);
                        }
                    }


                    SqlCommand cmdUpdateIRF = new SqlCommand("usp_CreateClonableSkillChannel", con);
                    cmdUpdateIRF.CommandType = CommandType.StoredProcedure;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@Name", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@Name"].Value = templateDetails.TemplateName;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@CreatedFromIRFId", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@CreatedFromIRFId"].Value = templateDetails.id;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@CreatedOn", SqlDbType.DateTime));
                    cmdUpdateIRF.Parameters["@CreatedOn"].Value = DateTime.Now;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationStatus", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@RecommendationStatus"].Value = templateDetails.IRFRecommendations.Status;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationAssessmentGroupId", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@RecommendationAssessmentGroupId"].Value = templateDetails.IRFRecommendations.Status == true ? templateDetails.IRFRecommendations.AssessmentGroupId : null;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationCategoryId", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@RecommendationCategoryId"].Value = templateDetails.IRFRecommendations.Status == true ? templateDetails.IRFRecommendations.CategoryId : null;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationCompetencyLevelId", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@RecommendationCompetencyLevelId"].Value = templateDetails.IRFRecommendations.Status == true ? templateDetails.IRFRecommendations.CompetencyLevelId : null;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationSeniorityLevelId", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@RecommendationSeniorityLevelId"].Value = templateDetails.IRFRecommendations.Status == true ? templateDetails.IRFRecommendations.SeniorityLevelId : null;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationCountryType", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@RecommendationCountryType"].Value = templateDetails.IRFRecommendations.Status == true ? templateDetails.IRFRecommendations.CountryType : null;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationCompanyType", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@RecommendationCompanyType"].Value = templateDetails.IRFRecommendations.Status == true ? templateDetails.IRFRecommendations.CompanyType : null;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationCreatedOn", SqlDbType.DateTime));
                    cmdUpdateIRF.Parameters["@RecommendationCreatedOn"].Value = DateTime.Now;

                    if (joinabilityDt != null && joinabilityDt.Rows.Count > 0)
                    {
                        cmdUpdateIRF.Parameters.Add(new SqlParameter("@JoinabilityBulkData", SqlDbType.Structured));
                        cmdUpdateIRF.Parameters["@JoinabilityBulkData"].Value = joinabilityDt;
                    }
                    if (selectabilityDt != null && selectabilityDt.Rows.Count > 0)
                    {
                        cmdUpdateIRF.Parameters.Add(new SqlParameter("@SelectabilityBulkData", SqlDbType.Structured));
                        cmdUpdateIRF.Parameters["@SelectabilityBulkData"].Value = selectabilityDt;
                    }
                    if (skillGeneralDt != null && skillGeneralDt.Rows.Count > 0)
                    {
                        cmdUpdateIRF.Parameters.Add(new SqlParameter("@SkillGeneralBulkData", SqlDbType.Structured));
                        cmdUpdateIRF.Parameters["@SkillGeneralBulkData"].Value = skillGeneralDt;
                    }
                    if (techSkillDt != null && techSkillDt.Rows.Count > 0)
                    {
                        cmdUpdateIRF.Parameters.Add(new SqlParameter("@TechSkillBulkData", SqlDbType.Structured));
                        cmdUpdateIRF.Parameters["@TechSkillBulkData"].Value = techSkillDt;
                    }
                    if (interviewerDt != null && interviewerDt.Rows.Count > 0)
                    {
                        cmdUpdateIRF.Parameters.Add(new SqlParameter("@InterviewerBulkData", SqlDbType.Structured));
                        cmdUpdateIRF.Parameters["@InterviewerBulkData"].Value = interviewerDt;
                    }

                    if (questionsDt != null && questionsDt.Rows.Count > 0)
                    {
                        cmdUpdateIRF.Parameters.Add(new SqlParameter("@QuestionsBulkData", SqlDbType.Structured));
                        cmdUpdateIRF.Parameters["@QuestionsBulkData"].Value = questionsDt;
                    }
                    con.Open();

                    if (await cmdUpdateIRF.ExecuteNonQueryAsync() != 0)
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
        public async Task<ClonableSkillChannelVM> GetClonableSkillChannelById(int id)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync<ClonableSkillChannelVM>(con, "usp_GetClonableSkillChannelById", dynamicParameters,
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

        public async Task<bool> UpdateClonableSkillChannel(ClonableSkillChannelVM templateDetails)
        {
            try
            {

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DataTable joinabilityDt = new DataTable();
                    if (templateDetails.Joinability != null && templateDetails.Joinability.Count() > 0)
                    {
                        //DataTable joinabilityDt = new DataTable();
                        joinabilityDt.Columns.Add("IRFId", typeof(int));
                        joinabilityDt.Columns.Add("LevelId", typeof(int));
                        joinabilityDt.Columns.Add("Type", typeof(string));
                        joinabilityDt.Columns.Add("Value", typeof(string));
                        joinabilityDt.Columns.Add("Weightage", typeof(int));


                        foreach (var item in templateDetails.Joinability)
                        {
                            joinabilityDt.Rows.Add(templateDetails.id, item.LevelId, item.Type, item.Value, item.Weightage);
                        }

                    }

                    DataTable selectabilityDt = new DataTable();
                    if (templateDetails.Selectability != null && templateDetails.Selectability.Count() > 0)
                    {
                        selectabilityDt.Columns.Add("IRFId", typeof(int));
                        selectabilityDt.Columns.Add("LevelId", typeof(int));
                        selectabilityDt.Columns.Add("Type", typeof(string));
                        selectabilityDt.Columns.Add("Value", typeof(string));
                        selectabilityDt.Columns.Add("Weightage", typeof(int));

                        foreach (var item in templateDetails.Selectability)
                        {
                            selectabilityDt.Rows.Add(templateDetails.id, item.LevelId, item.Type, item.Value, item.Weightage);
                        }

                    }

                    DataTable skillGeneralDt = new DataTable();
                    if (templateDetails.SkillGeneral != null && templateDetails.SkillGeneral.Count() > 0)
                    {
                        skillGeneralDt.Columns.Add("IRFId", typeof(int));
                        skillGeneralDt.Columns.Add("LevelId", typeof(int));
                        skillGeneralDt.Columns.Add("Type", typeof(string));
                        skillGeneralDt.Columns.Add("Value", typeof(string));
                        skillGeneralDt.Columns.Add("Weightage", typeof(int));

                        foreach (var item in templateDetails.SkillGeneral)
                        {
                            skillGeneralDt.Rows.Add(templateDetails.id, item.LevelId, item.Type, item.Value, item.Weightage);
                        }

                    }

                    DataTable techSkillDt = new DataTable();
                    techSkillDt.Columns.Add("IRFId", typeof(int));
                    techSkillDt.Columns.Add("LevelId", typeof(int));
                    techSkillDt.Columns.Add("SkillId", typeof(int));
                    techSkillDt.Columns.Add("Weightage", typeof(int));
                    techSkillDt.Columns.Add("Concepts", typeof(string));
                    techSkillDt.Columns.Add("Natureofquestion", typeof(string));
                    techSkillDt.Columns.Add("Timelimit", typeof(int));

                    foreach (var item in templateDetails.SkillCollection)
                    {
                        if (String.IsNullOrEmpty(item.Concepts))
                        {
                            item.Concepts = "";
                        }
                        if (String.IsNullOrEmpty(item.Natureofquestion))
                        {
                            item.Natureofquestion = "";
                        }
                        techSkillDt.Rows.Add(templateDetails.id, item.LevelId, item.SkillId, item.Weightage, item.Concepts, item.Natureofquestion, item.Timelimit);
                    }

                    DataTable interviewerDt = new DataTable();

                    if (templateDetails.InterviewerCollection != null && templateDetails.InterviewerCollection.Count() != 0 && !(templateDetails.InterviewerCollection.Where(x => x.UserId == 0).Any()))
                    {
                        interviewerDt.Columns.Add("IRFId", typeof(int));
                        interviewerDt.Columns.Add("UserId", typeof(int));
                        interviewerDt.Columns.Add("PerSuccessRate", typeof(int));
                        interviewerDt.Columns.Add("IsActive", typeof(bool));
                        interviewerDt.Columns.Add("IsAvailable", typeof(bool));

                        foreach (var item in templateDetails.InterviewerCollection)
                        {
                            interviewerDt.Rows.Add(templateDetails.id, item.UserId, item.PerSuccessRate, item.IsActive, item.IsAvailable);
                        }
                    }

                    DataTable questionsDt = new DataTable();

                    if (templateDetails.ClonableTemplateQuestionsCollection != null && templateDetails.ClonableTemplateQuestionsCollection.Count() != 0)
                    {
                        questionsDt.Columns.Add("Skill", typeof(int));
                        questionsDt.Columns.Add("Question", typeof(string));
                        questionsDt.Columns.Add("UserId", typeof(int));

                        foreach (var item in templateDetails.ClonableTemplateQuestionsCollection)
                        {
                            questionsDt.Rows.Add(item.SkillId, item.Question, templateDetails.UserId);
                        }
                    }


                    SqlCommand cmdUpdateIRF = new SqlCommand("usp_UpdateClonableSkillChannel", con);
                    cmdUpdateIRF.CommandType = CommandType.StoredProcedure;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@id", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@id"].Value = templateDetails.ClonableSkillChannelId;

                    //cmdUpdateIRF.Parameters.Add(new SqlParameter("@Name", SqlDbType.VarChar));
                    //cmdUpdateIRF.Parameters["@Name"].Value = templateDetails.TemplateName;

                    //cmdUpdateIRF.Parameters.Add(new SqlParameter("@CreatedFromIRFId", SqlDbType.Int));
                    //cmdUpdateIRF.Parameters["@CreatedFromIRFId"].Value = templateDetails.id;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@ModifiedOn", SqlDbType.DateTime));
                    cmdUpdateIRF.Parameters["@ModifiedOn"].Value = DateTime.Now;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationStatus", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@RecommendationStatus"].Value = templateDetails.IRFRecommendations.Status;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationAssessmentGroupId", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@RecommendationAssessmentGroupId"].Value = templateDetails.IRFRecommendations.Status == true ? templateDetails.IRFRecommendations.AssessmentGroupId : null;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationCategoryId", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@RecommendationCategoryId"].Value = templateDetails.IRFRecommendations.Status == true ? templateDetails.IRFRecommendations.CategoryId : null;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationCompetencyLevelId", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@RecommendationCompetencyLevelId"].Value = templateDetails.IRFRecommendations.Status == true ? templateDetails.IRFRecommendations.CompetencyLevelId : null;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationSeniorityLevelId", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@RecommendationSeniorityLevelId"].Value = templateDetails.IRFRecommendations.Status == true ? templateDetails.IRFRecommendations.SeniorityLevelId : null;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationCountryType", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@RecommendationCountryType"].Value = templateDetails.IRFRecommendations.Status == true ? templateDetails.IRFRecommendations.CountryType : null;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationCompanyType", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@RecommendationCompanyType"].Value = templateDetails.IRFRecommendations.Status == true ? templateDetails.IRFRecommendations.CompanyType : null;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationModifiedOn", SqlDbType.DateTime));
                    cmdUpdateIRF.Parameters["@RecommendationModifiedOn"].Value = DateTime.Now;

                    if (joinabilityDt != null && joinabilityDt.Rows.Count > 0)
                    {
                        cmdUpdateIRF.Parameters.Add(new SqlParameter("@JoinabilityBulkData", SqlDbType.Structured));
                        cmdUpdateIRF.Parameters["@JoinabilityBulkData"].Value = joinabilityDt;
                    }
                    if (selectabilityDt != null && selectabilityDt.Rows.Count > 0)
                    {
                        cmdUpdateIRF.Parameters.Add(new SqlParameter("@SelectabilityBulkData", SqlDbType.Structured));
                        cmdUpdateIRF.Parameters["@SelectabilityBulkData"].Value = selectabilityDt;
                    }
                    if (skillGeneralDt != null && skillGeneralDt.Rows.Count > 0)
                    {
                        cmdUpdateIRF.Parameters.Add(new SqlParameter("@SkillGeneralBulkData", SqlDbType.Structured));
                        cmdUpdateIRF.Parameters["@SkillGeneralBulkData"].Value = skillGeneralDt;
                    }
                    if (techSkillDt != null && techSkillDt.Rows.Count > 0)
                    {
                        cmdUpdateIRF.Parameters.Add(new SqlParameter("@TechSkillBulkData", SqlDbType.Structured));
                        cmdUpdateIRF.Parameters["@TechSkillBulkData"].Value = techSkillDt;
                    }
                    if (interviewerDt != null && interviewerDt.Rows.Count > 0)
                    {
                        cmdUpdateIRF.Parameters.Add(new SqlParameter("@InterviewerBulkData", SqlDbType.Structured));
                        cmdUpdateIRF.Parameters["@InterviewerBulkData"].Value = interviewerDt;
                    }

                    if (questionsDt != null && questionsDt.Rows.Count > 0)
                    {
                        cmdUpdateIRF.Parameters.Add(new SqlParameter("@QuestionsBulkData", SqlDbType.Structured));
                        cmdUpdateIRF.Parameters["@QuestionsBulkData"].Value = questionsDt;
                    }
                    con.Open();

                    if (await cmdUpdateIRF.ExecuteNonQueryAsync() != 0)
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

    }
}
