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

        public async Task<IEnumerable<SkillChannelTemplateVM>> GetAllTemplates()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<SkillChannelTemplateVM>(con, "usp_GetAllTemplates",
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

        public async Task<bool> AddTemplate(SkillChannelTemplateVM templateDetails, string createdByEmailId/*, out int templateId*/)
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
    }
}
