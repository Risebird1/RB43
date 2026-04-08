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
                            joinabilityDt.Rows.Add(templateDetails.id, item.LevelId, item.Type ?? string.Empty, item.Value ?? string.Empty, item.Weightage);
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
                            selectabilityDt.Rows.Add(templateDetails.id, item.LevelId, item.Type ?? string.Empty, item.Value ?? string.Empty, item.Weightage);
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
                            skillGeneralDt.Rows.Add(templateDetails.id, item.LevelId, item.Type, item.Value ?? string.Empty, item.Weightage);
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
                    techSkillDt.Columns.Add("isEliminationskill", typeof(bool));

                    foreach (var item in templateDetails.SkillCollection)
                    {
                        if (string.IsNullOrEmpty(item.Concepts))
                        {
                            item.Concepts = "";
                        }
                        if (string.IsNullOrEmpty(item.Natureofquestion))
                        {
                            item.Natureofquestion = "";
                        }
                        techSkillDt.Rows.Add(templateDetails.id, item.LevelId, item.SkillId, item.Weightage, item.Concepts, item.Natureofquestion, item.Timelimit, item.isEliminationskill);
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

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@CreatedBy", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@CreatedBy"].Value = templateDetails.CreatedBy;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@MeetingPlatform", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@MeetingPlatform"].Value = templateDetails.MeetingPlatform;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationStatus", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@RecommendationStatus"].Value = templateDetails.IRFRecommendations.Status;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationAssessmentGroupId", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@RecommendationAssessmentGroupId"].Value = templateDetails.IRFRecommendations.Status == true ? templateDetails.IRFRecommendations.AssessmentGroupId : null;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationCategoryId", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@RecommendationCategoryId"].Value = templateDetails.IRFRecommendations.Status == true ? templateDetails.IRFRecommendations.CategoryId : null;

                    //cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationCompetencyLevelId", SqlDbType.Int));
                    //cmdUpdateIRF.Parameters["@RecommendationCompetencyLevelId"].Value = templateDetails.IRFRecommendations.Status == true ? templateDetails.IRFRecommendations.CompetencyLevelId : null;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationSeniorityLevelId", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@RecommendationSeniorityLevelId"].Value = templateDetails.IRFRecommendations.Status == true ? templateDetails.IRFRecommendations.SeniorityLevelId : null;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationCountryType", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@RecommendationCountryType"].Value = templateDetails.IRFRecommendations.Status == true ? templateDetails.IRFRecommendations.CountryType : null;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationCompanyType", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@RecommendationCompanyType"].Value = templateDetails.IRFRecommendations.Status == true ? templateDetails.IRFRecommendations.CompanyType : null;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationCreatedOn", SqlDbType.DateTime));
                    cmdUpdateIRF.Parameters["@RecommendationCreatedOn"].Value = DateTime.Now;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationDifficultyLevel", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@RecommendationDifficultyLevel"].Value = templateDetails.IRFRecommendations.Status == true ? templateDetails.IRFRecommendations.DifficultyLevel : null;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationEmploymentType", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@RecommendationEmploymentType"].Value = templateDetails.IRFRecommendations.Status == true ? templateDetails.IRFRecommendations.EmploymentType : null;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationPerHourRate", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@RecommendationPerHourRate"].Value = templateDetails.IRFRecommendations.Status == true ? templateDetails.IRFRecommendations.PerHourRate : null;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationPrimarySkills", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@RecommendationPrimarySkills"].Value = templateDetails.IRFRecommendations.Status == true ? templateDetails.IRFRecommendations.PrimarySkills : null;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationClients", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@RecommendationClients"].Value = templateDetails.IRFRecommendations.Status == true ? templateDetails.IRFRecommendations.Clients : null;


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
                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsAadhaarVerificationRequired", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsAadhaarVerificationRequired"].Value = templateDetails.IsAadhaarVerificationRequired;
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
                    techSkillDt.Columns.Add("isEliminationskill", typeof(int));

                    foreach (var item in templateDetails.SkillCollection)
                    {
                        if (string.IsNullOrEmpty(item.Concepts))
                        {
                            item.Concepts = "";
                        }
                        if (string.IsNullOrEmpty(item.Natureofquestion))
                        {
                            item.Natureofquestion = "";
                        }
                        techSkillDt.Rows.Add(templateDetails.id, item.LevelId, item.SkillId, item.Weightage, item.Concepts, item.Natureofquestion, item.Timelimit, item.isEliminationskill);
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

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@Name", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@Name"].Value = templateDetails.TemplateName;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@MeetingPlatform", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@MeetingPlatform"].Value = templateDetails.MeetingPlatform;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@ModifiedOn", SqlDbType.DateTime));
                    cmdUpdateIRF.Parameters["@ModifiedOn"].Value = DateTime.Now;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@ModifiedBy", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@ModifiedBy"].Value = templateDetails.ModifiedBy;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationStatus", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@RecommendationStatus"].Value = templateDetails.IRFRecommendations?.Status;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationAssessmentGroupId", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@RecommendationAssessmentGroupId"].Value = templateDetails.IRFRecommendations?.Status == true ? templateDetails.IRFRecommendations.AssessmentGroupId : null;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationCategoryId", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@RecommendationCategoryId"].Value = templateDetails.IRFRecommendations?.Status == true ? templateDetails.IRFRecommendations.CategoryId : null;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationSeniorityLevelId", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@RecommendationSeniorityLevelId"].Value = templateDetails.IRFRecommendations?.Status == true ? templateDetails.IRFRecommendations.SeniorityLevelId : null;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationCountryType", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@RecommendationCountryType"].Value = templateDetails.IRFRecommendations?.Status == true ? templateDetails.IRFRecommendations.CountryType : null;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationCompanyType", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@RecommendationCompanyType"].Value = templateDetails.IRFRecommendations?.Status == true ? templateDetails.IRFRecommendations?.CompanyType : null;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationModifiedOn", SqlDbType.DateTime));
                    cmdUpdateIRF.Parameters["@RecommendationModifiedOn"].Value = DateTime.Now;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationDifficultyLevel", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@RecommendationDifficultyLevel"].Value = templateDetails.IRFRecommendations?.Status == true ? templateDetails.IRFRecommendations.DifficultyLevel : null;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationEmploymentType", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@RecommendationEmploymentType"].Value = templateDetails.IRFRecommendations?.Status == true ? templateDetails.IRFRecommendations.EmploymentType : null;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationPerHourRate", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@RecommendationPerHourRate"].Value = templateDetails.IRFRecommendations?.Status == true ? templateDetails.IRFRecommendations.PerHourRate : null;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationPrimarySkills", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@RecommendationPrimarySkills"].Value = templateDetails.IRFRecommendations?.Status == true ? templateDetails.IRFRecommendations.PrimarySkills : null;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationClients", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@RecommendationClients"].Value = templateDetails.IRFRecommendations?.Status == true ? templateDetails.IRFRecommendations.Clients : null;


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
                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsAadhaarVerificationRequired", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsAadhaarVerificationRequired"].Value = templateDetails.IsAadhaarVerificationRequired;
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
        public async Task<ClonableSkillChannelVM> GetClonableSkillChannelMeetingPlatformById(int id)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QuerySingleAsync<ClonableSkillChannelVM>(con, "usp_GetClonableSkillChannelMeetingPlatformById", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<ClonableSkillChannelVM> GetClonableSkillChannelDetails(int id)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QuerySingleAsync<ClonableSkillChannelVM>(con, "usp_GetClonableSkillChannelDetails", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<IEnumerable<ClonableSkillChannelVM>> GetAllClonableSkillChannelsToBeRenewed()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<ClonableSkillChannelVM>(con, "usp_GetAllClonableSkillChannelsToBeRenewed", commandType: CommandType.StoredProcedure);
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

        public async Task<bool> RenewTemplateRubrics(List<TransSkill> techRubrics, int templateId)
        {
            try
            {

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    
                    DataTable techSkillDt = new DataTable();

                    if (techRubrics != null && techRubrics.Count() > 0)
                    {
                        techSkillDt.Columns.Add("IRFId", typeof(int));
                        techSkillDt.Columns.Add("LevelId", typeof(int));
                        techSkillDt.Columns.Add("SkillId", typeof(int));
                        techSkillDt.Columns.Add("Weightage", typeof(int));
                        techSkillDt.Columns.Add("Concepts", typeof(string));
                        techSkillDt.Columns.Add("Natureofquestion", typeof(string));
                        techSkillDt.Columns.Add("Timelimit", typeof(int));
                        techSkillDt.Columns.Add("isEliminationskill", typeof(bool));
                        techSkillDt.Columns.Add("SkillSet", typeof(int));
                        techSkillDt.Columns.Add("SkillName", typeof(string));

                        foreach (var item in techRubrics)
                        {
                            if (string.IsNullOrEmpty(item.Concepts))//|| irfDetails.TechRubricType == TechRubricTypes.Basic
                            {
                                item.Concepts = "";
                            }
                            if (string.IsNullOrEmpty(item.Natureofquestion))// || irfDetails.TechRubricType == TechRubricTypes.Basic
                            {
                                item.Natureofquestion = "";
                            }
                            
                            techSkillDt.Rows.Add(0, item.LevelId, item.SkillId, item.Weightage, item.Concepts, item.Natureofquestion, item.Timelimit, item.isEliminationskill, item.SkillSet, item.SkillName);
                        }
                    }

                    SqlCommand cmdCreateIRF = new SqlCommand("usp_RenewTemplateRubrics", con);
                    cmdCreateIRF.CommandType = CommandType.StoredProcedure;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@templateId", SqlDbType.Int));
                    cmdCreateIRF.Parameters["@templateId"].Value = templateId;


                    if (techSkillDt != null && techSkillDt.Rows.Count > 0)
                    {
                        cmdCreateIRF.Parameters.Add(new SqlParameter("@TechSkillBulkData", SqlDbType.Structured));
                        cmdCreateIRF.Parameters["@TechSkillBulkData"].Value = techSkillDt;
                    }
                    
                    
                    con.Open();

                    if (await cmdCreateIRF.ExecuteNonQueryAsync() != 0)
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

        public async Task<bool> CreateTemplateFromMAG(List<TransSkill> techRubrics, int assessmentGroupId, string loggedInUser)
        {
            try
            {

                using (SqlConnection con = new SqlConnection(connectionString))
                {

                    DataTable techSkillDt = new DataTable();

                    if (techRubrics != null && techRubrics.Count() > 0)
                    {
                        techSkillDt.Columns.Add("IRFId", typeof(int));
                        techSkillDt.Columns.Add("LevelId", typeof(int));
                        techSkillDt.Columns.Add("SkillId", typeof(int));
                        techSkillDt.Columns.Add("Weightage", typeof(int));
                        techSkillDt.Columns.Add("Concepts", typeof(string));
                        techSkillDt.Columns.Add("Natureofquestion", typeof(string));
                        techSkillDt.Columns.Add("Timelimit", typeof(int));
                        techSkillDt.Columns.Add("isEliminationskill", typeof(bool));
                        techSkillDt.Columns.Add("SkillSet", typeof(int));
                        techSkillDt.Columns.Add("SkillName", typeof(string));

                        foreach (var item in techRubrics)
                        {
                            if (string.IsNullOrEmpty(item.Concepts))//|| irfDetails.TechRubricType == TechRubricTypes.Basic
                            {
                                item.Concepts = "";
                            }
                            if (string.IsNullOrEmpty(item.Natureofquestion))// || irfDetails.TechRubricType == TechRubricTypes.Basic
                            {
                                item.Natureofquestion = "";
                            }

                            techSkillDt.Rows.Add(0, item.LevelId, item.SkillId, item.Weightage, item.Concepts, item.Natureofquestion, item.Timelimit, item.isEliminationskill, item.SkillSet, item.SkillName);
                        }
                    }

                    SqlCommand cmdCreateTemplate= new SqlCommand("usp_CreateTemplateFromMAG", con);
                    cmdCreateTemplate.CommandType = CommandType.StoredProcedure;

                    cmdCreateTemplate.Parameters.Add(new SqlParameter("@assessmentGroupId", SqlDbType.Int));
                    cmdCreateTemplate.Parameters["@assessmentGroupId"].Value = assessmentGroupId;

                    cmdCreateTemplate.Parameters.Add(new SqlParameter("@CreatedBy", SqlDbType.VarChar));
                    cmdCreateTemplate.Parameters["@CreatedBy"].Value = loggedInUser;

                    cmdCreateTemplate.Parameters.Add(new SqlParameter("@CreatedOn", SqlDbType.DateTime));
                    cmdCreateTemplate.Parameters["@CreatedOn"].Value = DateTime.Now;

                    if (techSkillDt != null && techSkillDt.Rows.Count > 0)
                    {
                        cmdCreateTemplate.Parameters.Add(new SqlParameter("@TechSkillBulkData", SqlDbType.Structured));
                        cmdCreateTemplate.Parameters["@TechSkillBulkData"].Value = techSkillDt;
                    }


                    con.Open();

                    if (await cmdCreateTemplate.ExecuteNonQueryAsync() != 0)
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
        public async Task<IEnumerable<SkillComment>> GetDeliveryHeadSampleQuestions(int id, string emailId = null)
        {
            try
            {

                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", id);
                dynamicParameters.Add("@emailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = SqlMapper.Query<SkillComment>(con, "usp_GetDeliveryHeadSampleQuestions", dynamicParameters,
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

        public async Task<IEnumerable<MAGWithoutTemplateVM>> GetMAGsWithoutTemplatesAsync()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<MAGWithoutTemplateVM>(
                        con, 
                        "usp_GetMAGsWithoutTemplates", 
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
            return new List<MAGWithoutTemplateVM>();
        }

    }
}
