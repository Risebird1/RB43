using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Helpers;
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
    /// <inheritdoc cref="IAIScreeningRepository"/>
    public class AIScreeningRepository : IAIScreeningRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public AIScreeningRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<int> CreateAIAudioTemplate(
            string templateName,
            int assessmentGroupId,
            IList<TransSkill> rubric,
            string createdBy,
            string meetingPlatform = "Zoho",
            bool isAadhaarVerificationRequired = false)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var cmd = new SqlCommand("usp_CreateAIAudioTemplate", con)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.Add(new SqlParameter("@TemplateName", SqlDbType.VarChar) { Value = templateName });
                    // NULL rather than 0 when nothing resolved: ClonableSkillChannel.AssessmentGroupId
                    // is nullable and 0 is never a real id.
                    cmd.Parameters.Add(new SqlParameter("@AssessmentGroupId", SqlDbType.Int)
                    {
                        Value = assessmentGroupId > 0 ? (object)assessmentGroupId : DBNull.Value
                    });
                    cmd.Parameters.Add(new SqlParameter("@CreatedOn", SqlDbType.DateTime) { Value = DateTime.Now });
                    cmd.Parameters.Add(new SqlParameter("@CreatedBy", SqlDbType.VarChar)
                    {
                        Value = (object)createdBy ?? DBNull.Value
                    });
                    cmd.Parameters.Add(new SqlParameter("@MeetingPlatform", SqlDbType.VarChar)
                    {
                        Value = string.IsNullOrWhiteSpace(meetingPlatform) ? "Zoho" : meetingPlatform
                    });
                    cmd.Parameters.Add(new SqlParameter("@IsAadhaarVerificationRequired", SqlDbType.Bit)
                    {
                        Value = isAadhaarVerificationRequired
                    });
                    cmd.Parameters.Add(new SqlParameter("@TechSkillBulkData", SqlDbType.Structured)
                    {
                        Value = BuildTechSkillTable(rubric)
                    });

                    var templateIdParam = new SqlParameter("@ClonableIRFId", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(templateIdParam);

                    con.Open();
                    await cmd.ExecuteNonQueryAsync();
                    con.Close();

                    return templateIdParam.Value == null || templateIdParam.Value == DBNull.Value
                        ? 0
                        : Convert.ToInt32(templateIdParam.Value);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(Risebird.Entities.Enum.LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<bool> SaveAIScreeningProvisioning(
            int irfIntId,
            int skillSetsEvaluate,
            string openAIPromptId,
            string openAIEvaluatorPromptId,
            string idealCandidateProfile,
            IList<TransSkill> rubric,
            string jdTitleTwo = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var cmd = new SqlCommand("usp_SaveAIScreeningProvisioning", con)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.Add(new SqlParameter("@IRFId", SqlDbType.Int) { Value = irfIntId });
                    cmd.Parameters.Add(new SqlParameter("@SkillSetsevaluate", SqlDbType.Int) { Value = skillSetsEvaluate });
                    cmd.Parameters.Add(new SqlParameter("@OpenAIPromptId", SqlDbType.VarChar)
                    {
                        Value = string.IsNullOrWhiteSpace(openAIPromptId) ? (object)DBNull.Value : openAIPromptId.Trim()
                    });
                    cmd.Parameters.Add(new SqlParameter("@OpenAIEvaluatorPromptId", SqlDbType.VarChar)
                    {
                        Value = string.IsNullOrWhiteSpace(openAIEvaluatorPromptId)
                            ? (object)DBNull.Value
                            : openAIEvaluatorPromptId.Trim()
                    });
                    cmd.Parameters.Add(new SqlParameter("@IdealCandidateProfile", SqlDbType.VarChar, -1)
                    {
                        Value = string.IsNullOrWhiteSpace(idealCandidateProfile)
                            ? (object)DBNull.Value
                            : idealCandidateProfile.Trim()
                    });
                    cmd.Parameters.Add(new SqlParameter("@JDTitleTwo", SqlDbType.VarChar, 150)
                    {
                        Value = string.IsNullOrWhiteSpace(jdTitleTwo) ? (object)DBNull.Value : jdTitleTwo.Trim()
                    });
                    cmd.Parameters.Add(new SqlParameter("@TechSkillBulkData", SqlDbType.Structured)
                    {
                        Value = BuildTechSkillTable(rubric)
                    });

                    con.Open();
                    await cmd.ExecuteNonQueryAsync();
                    con.Close();
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(Risebird.Entities.Enum.LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> IsTemplateNameTaken(string templateName)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@value", templateName);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<string>(
                        con, "usp_GetClonableSkillChannelNameUniqueness", dynamicParameters,
                        commandType: CommandType.StoredProcedure);
                    return result != null && result.Any();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(Risebird.Entities.Enum.LogLevelInfo.Exception, ex);
                // Treat an unreadable answer as "taken" so the caller picks a different name
                // rather than failing on the unique constraint.
                return true;
            }
        }

        public async Task<int> ResolveAssessmentGroupIdByJobTitle(string jobTitle, double minimumScore)
        {
            if (string.IsNullOrWhiteSpace(jobTitle)) return 0;

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    // Matched on the Assessment Group's own name, and on the nearest match rather
                    // than an exact one: a job title lifted off an arbitrary JD ("Sr. Backend
                    // Engineer (Kafka)") almost never equals an AG name character for character.
                    //
                    // No MasterAssessmentGroup join. The MAG gate exists to guarantee a staffable
                    // interviewer panel, which an AI Audio channel does not have — requiring one
                    // here would only push matchable roles onto the catch-all.
                    var candidates = (await SqlMapper.QueryAsync<AssessmentGroupMatcher.Candidate>(
                        con,
                        @"SELECT ag.id AS Id, ag.AssessmentGroup AS Name
                          FROM AssessmentGroup ag
                          WHERE ag.AssessmentGroup IS NOT NULL
                            AND LTRIM(RTRIM(ag.AssessmentGroup)) <> ''"))?.ToList();

                    if (candidates == null || candidates.Count == 0) return 0;

                    var match = AssessmentGroupMatcher.FindBest(jobTitle, candidates, minimumScore);
                    if (match == null)
                    {
                        // Not an error: no match is the normal case for an arbitrary job
                        // description, and the caller stamps the catch-all instead.
                        return 0;
                    }

                    await _applicationErrorLogger.LogMessage(
                        Risebird.Entities.Enum.LogLevelInfo.Info,
                        $"AI screening resolved job title '{jobTitle.Trim()}' to Assessment Group " +
                        $"'{match.Name}' (id {match.Id}) with score {match.Score:0.00}.");

                    return match.Id;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(Risebird.Entities.Enum.LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<bool> IsAIScreeningChannel(int irfIntId)
        {
            if (irfIntId <= 0) return false;

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var cmd = new SqlCommand(
                        "SELECT TOP 1 ISNULL(IsAIAudioScreening, 0) FROM IRF WHERE id = @id", con);
                    cmd.Parameters.AddWithValue("@id", irfIntId);

                    con.Open();
                    var scalar = await cmd.ExecuteScalarAsync();
                    con.Close();

                    return scalar != null && scalar != DBNull.Value && Convert.ToBoolean(scalar);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(Risebird.Entities.Enum.LogLevelInfo.Exception, ex);
                // Treat an unreadable answer as "not a screening channel" so the report falls back
                // to exactly what it renders today rather than switching behaviour on a failure.
                return false;
            }
        }

        public async Task<int> GetAssessmentGroupIdByName(string assessmentGroupName)
        {
            if (string.IsNullOrWhiteSpace(assessmentGroupName)) return 0;

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var cmd = new SqlCommand(
                        @"SELECT TOP 1 ag.id
                          FROM AssessmentGroup ag
                          WHERE ag.AssessmentGroup = @name
                          ORDER BY ag.id", con);
                    cmd.Parameters.AddWithValue("@name", assessmentGroupName.Trim());

                    con.Open();
                    var scalar = await cmd.ExecuteScalarAsync();
                    con.Close();

                    return scalar == null || scalar == DBNull.Value ? 0 : Convert.ToInt32(scalar);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(Risebird.Entities.Enum.LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        /// <summary>Builds the TransTechSkill_TableTypeV5 payload, column order as declared.</summary>
        private static DataTable BuildTechSkillTable(IList<TransSkill> rubric)
        {
            var table = new DataTable();
            table.Columns.Add("IRFId", typeof(int));
            table.Columns.Add("LevelId", typeof(int));
            table.Columns.Add("SkillId", typeof(int));
            table.Columns.Add("Weightage", typeof(int));
            table.Columns.Add("Concepts", typeof(string));
            table.Columns.Add("Natureofquestion", typeof(string));
            table.Columns.Add("Timelimit", typeof(int));
            table.Columns.Add("isEliminationskill", typeof(bool));
            table.Columns.Add("SkillSet", typeof(int));
            table.Columns.Add("SkillName", typeof(string));

            foreach (var item in rubric ?? new List<TransSkill>())
            {
                if (item == null || string.IsNullOrWhiteSpace(item.SkillName)) continue;

                table.Rows.Add(
                    item.IRFId,
                    item.LevelId,
                    item.SkillId,
                    item.Weightage,
                    item.Concepts ?? string.Empty,
                    item.Natureofquestion ?? string.Empty,
                    item.Timelimit > 0 ? item.Timelimit : 3,
                    item.isEliminationskill,
                    item.SkillSet,
                    item.SkillName);
            }

            return table;
        }
    }
}
