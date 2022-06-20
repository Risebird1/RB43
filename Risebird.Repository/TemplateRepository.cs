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
using System.Threading.Tasks;

namespace Risebird.Repository
{
    public class TemplateRepository : ITemplateRepository
    {

        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private IApplicationErrorLogger _applicationErrorLogger;
        public TemplateRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<IEnumerable<SkillChannelTemplateVM>> GetAllTemplates(string emailId/*, DateTime startDate, DateTime endDate*/)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@emailId", emailId);
                    //parameters.Add("@startDate", startDate);
                    //parameters.Add("@endDate", endDate);
                    var result = await SqlMapper.QueryAsync<SkillChannelTemplateVM>(con, "usp_GetAllTemplates", parameters,
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

        public async Task<bool> AddTemplate(SkillChannelTemplateVM templateDetails, string createdByEmailId)
        {
            try
            {

                using (SqlConnection con = new SqlConnection(connectionString))
                {


                    DataTable techSkillDt = new DataTable();
                    techSkillDt.Columns.Add("SkillId", typeof(int));
                    techSkillDt.Columns.Add("LevelId", typeof(int));
                    techSkillDt.Columns.Add("Weightage", typeof(int));
                    techSkillDt.Columns.Add("IsSelected", typeof(bool));


                    foreach (var item in templateDetails.TechSkillCollection)
                    {
                        techSkillDt.Rows.Add(item.SkillId, item.LevelId, item.Weightage, item.IsSelected);
                    }

                    DataTable interviewerDt = new DataTable();
                    interviewerDt.Columns.Add("UserId", typeof(int));
                    interviewerDt.Columns.Add("IsSelected", typeof(bool));

                    foreach (var item in templateDetails.InterviewerCollection)
                    {
                        interviewerDt.Rows.Add(item.UserId, item.IsSelected);
                    }



                    SqlCommand cmdAddTemplate = new SqlCommand("usp_AddTemplate", con);
                    cmdAddTemplate.CommandType = CommandType.StoredProcedure;
                    cmdAddTemplate.Parameters.Add(new SqlParameter("@TechSkillBulkData", SqlDbType.Structured));
                    cmdAddTemplate.Parameters["@TechSkillBulkData"].Value = techSkillDt;
                    cmdAddTemplate.Parameters.Add(new SqlParameter("@InterviewerBulkData", SqlDbType.Structured));
                    cmdAddTemplate.Parameters["@InterviewerBulkData"].Value = interviewerDt;
                    cmdAddTemplate.Parameters.Add(new SqlParameter("@TemplateName", SqlDbType.VarChar));
                    cmdAddTemplate.Parameters["@TemplateName"].Value = templateDetails.TemplateName;
                    cmdAddTemplate.Parameters.Add(new SqlParameter("@AssessmentGroupId", SqlDbType.Int));
                    cmdAddTemplate.Parameters["@AssessmentGroupId"].Value = templateDetails.AssessmentGroupId;
                    cmdAddTemplate.Parameters.Add(new SqlParameter("@createdByEmailId", SqlDbType.VarChar));
                    cmdAddTemplate.Parameters["@createdByEmailId"].Value = createdByEmailId;
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

        public async Task<bool> IsTemplateNameInUse(string templateName)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@templateName", templateName);
                    var result = (await SqlMapper.QueryAsync<bool>(con, "usp_IsTemplateNameInUse", parameters,
                        commandType: CommandType.StoredProcedure)).FirstOrDefault();
                    return result;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<EditSkillChannelTemplateVM> GetTemplateById(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@id", id);
                    var result = await SqlMapper.QueryAsync<EditSkillChannelTemplateVM>(con, "usp_GetTemplateById", parameters,
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

        public async Task<IEnumerable<TransTemplateInterviewer>> GetTemplateInterviewers(int templateId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@templateId", templateId);
                    return await SqlMapper.QueryAsync<TransTemplateInterviewer>(con, "usp_GetTemplateInterviewers", parameters,
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

        public async Task<IEnumerable<TransTemplateTechSkill>> GetTemplateTechSkills(int templateId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@templateId", templateId);
                    return await SqlMapper.QueryAsync<TransTemplateTechSkill>(con, "usp_GetTemplateTechSkills", parameters,
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

        public async Task<bool> UpdateTemplate(EditSkillChannelTemplateVM templateDetails, string modifiedByEmailId)
        {
            try
            {

                using (SqlConnection con = new SqlConnection(connectionString))
                {


                    DataTable techSkillDt = new DataTable();
                    techSkillDt.Columns.Add("SkillId", typeof(int));
                    techSkillDt.Columns.Add("LevelId", typeof(int));
                    techSkillDt.Columns.Add("Weightage", typeof(int));
                    techSkillDt.Columns.Add("IsSelected", typeof(bool));


                    foreach (var item in templateDetails.TechSkillCollection)
                    {
                        techSkillDt.Rows.Add(item.SkillId, item.LevelId, item.Weightage, item.IsSelected);
                    }

                    DataTable interviewerDt = new DataTable();
                    interviewerDt.Columns.Add("UserId", typeof(int));
                    interviewerDt.Columns.Add("IsSelected", typeof(bool));

                    foreach (var item in templateDetails.InterviewerCollection)
                    {
                        interviewerDt.Rows.Add(item.UserId, item.IsSelected);
                    }



                    SqlCommand cmdAddTemplate = new SqlCommand("usp_UpdateTemplate", con);
                    cmdAddTemplate.CommandType = CommandType.StoredProcedure;
                    cmdAddTemplate.Parameters.Add(new SqlParameter("@TechSkillBulkData", SqlDbType.Structured));
                    cmdAddTemplate.Parameters["@TechSkillBulkData"].Value = techSkillDt;
                    cmdAddTemplate.Parameters.Add(new SqlParameter("@InterviewerBulkData", SqlDbType.Structured));
                    cmdAddTemplate.Parameters["@InterviewerBulkData"].Value = interviewerDt;
                    //cmdAddTemplate.Parameters.Add(new SqlParameter("@TemplateName", SqlDbType.VarChar));
                    //cmdAddTemplate.Parameters["@TemplateName"].Value = templateDetails.TemplateName;
                    cmdAddTemplate.Parameters.Add(new SqlParameter("@AssessmentGroupId", SqlDbType.Int));
                    cmdAddTemplate.Parameters["@AssessmentGroupId"].Value = templateDetails.AssessmentGroupId;
                    cmdAddTemplate.Parameters.Add(new SqlParameter("@modifiedByEmailId", SqlDbType.VarChar));
                    cmdAddTemplate.Parameters["@modifiedByEmailId"].Value = modifiedByEmailId;
                    cmdAddTemplate.Parameters.Add(new SqlParameter("@templateId", SqlDbType.Int));
                    cmdAddTemplate.Parameters["@templateId"].Value = templateDetails.id;
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

        public async Task<bool> DeleteTemplate(int id)
        {
            try
            {

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<int>(con, "usp_DeleteTemplate", parameters,
                        commandType: CommandType.StoredProcedure);
                    if (result != null && result.FirstOrDefault() == 0)
                    {
                        return true;
                    }
                    return false;

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

        public async Task<bool> RestoreTemplate(int id)
        {
            try
            {

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<int>(con, "usp_RestoreTemplate", parameters,
                        commandType: CommandType.StoredProcedure);
                    if (result != null && result.FirstOrDefault() == 0)
                    {
                        return true;
                    }
                    return false;

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
