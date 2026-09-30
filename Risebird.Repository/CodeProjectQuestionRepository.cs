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
using System.Text.Json;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    public class CodeProjectQuestionRepository : ICodeProjectQuestionRepository
    {
        private readonly string _connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public CodeProjectQuestionRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _connectionString = configuration["ConnectionString:DBConnectionString"];
            _applicationErrorLogger = applicationErrorLogger;
        }

        public async Task<List<StackImage>> GetActiveStackImages()
        {
            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    var rows = await DapperStoredProcedureHelper.QueryAsync<StackImage>(con, "usp_GetActiveStackImages");
                    return rows.ToList();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<StackImage>();
            }
        }

        public async Task<int> AddCodeProjectQuestion(int irfId, string skillName, CodeProjectGeneration g, int timeLimitMinutes,
            string sanitizedPayload, string createdBy)
        {
            try
            {
                var p = new DynamicParameters();
                p.Add("@IRFId", irfId);
                p.Add("@SkillName", skillName);
                p.Add("@Comment", g.QuestionText);
                p.Add("@ExpectedAnswer", g.ExpectedAnswer);
                p.Add("@QuestionGist", g.QuestionGist);
                p.Add("@TimeLimitInMinutes", timeLimitMinutes);
                p.Add("@GeneratedPayload", sanitizedPayload);
                p.Add("@CreatedBy", createdBy);
                p.Add("@Language", g.Language ?? "");
                p.Add("@Framework", g.Framework);
                p.Add("@StackImageCode", g.StackImageCode);
                p.Add("@EntryPointFile", g.EntryPointFile);
                p.Add("@ReadOnlyPathsJson", JsonSerializer.Serialize(g.ReadOnlyPaths));
                p.Add("@BuildCommand", g.BuildCommand);
                p.Add("@RunCommand", g.RunCommand);
                p.Add("@TestCommand", g.TestCommand);
                p.Add("@HasWebserver", g.HasWebserver);
                p.Add("@PortsJson", JsonSerializer.Serialize(g.Ports));
                p.Add("@RequirementsJson", JsonSerializer.Serialize(g.Requirements));
                p.Add("@EstimatedMinutes", g.EstimatedMinutes);
                using (var con = new SqlConnection(_connectionString))
                {
                    var id = await DapperStoredProcedureHelper.QueryFirstOrDefaultAsync<int?>(con, "usp_AddCodeProjectQuestion", p);
                    return id ?? 0;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<bool> UpdateValidation(int questionId, string status, string validationJson, string projectBlobUri = null, string secretsBlobUri = null)
        {
            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    var affected = await DapperStoredProcedureHelper.QueryFirstOrDefaultAsync<int>(con, "usp_UpdateCodeProjectValidation", new
                    {
                        QuestionId = questionId,
                        ValidationStatus = status,
                        ValidationJson = validationJson,
                        ProjectBlobUri = projectBlobUri,
                        SecretsBlobUri = secretsBlobUri
                    });
                    return affected > 0;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<CodeProjectDetails> GetCodeProjectDetails(int questionId)
        {
            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    return await DapperStoredProcedureHelper.QueryFirstOrDefaultAsync<CodeProjectDetails>(con, "usp_GetCodeProjectDetails", new { QuestionId = questionId });
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<List<int>> GetUnfinishedValidations()
        {
            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    var rows = await DapperStoredProcedureHelper.QueryAsync<int>(con, "usp_GetUnfinishedCodeProjectValidations");
                    return rows.ToList();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<int>();
            }
        }

        public async Task<List<CodeProjectStatusRow>> GetStatusesByIRF(int irfId)
        {
            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    var rows = await DapperStoredProcedureHelper.QueryAsync<CodeProjectStatusRow>(con, "usp_GetCodeProjectStatusesByIRF", new { IRFId = irfId });
                    return rows.ToList();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<CodeProjectStatusRow>();
            }
        }

        public async Task<(int QuestionId, string StackImageCode)?> PeekNextCodeProjectQuestion(int interviewId, int irfId, int currentQuestionId)
        {
            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    var row = await DapperStoredProcedureHelper.QueryFirstOrDefaultAsync<CodeProjectStatusRow>(con,
                        "usp_PeekNextCodeProjectQuestionForSession", new { interviewId, irfId, currentQuestionId });
                    return row == null || row.QuestionId <= 0 ? null : (row.QuestionId, row.StackImageCode);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> ClaimEnvironment(string sessionId, int questionId, string stackImageCode)
        {
            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    return await DapperStoredProcedureHelper.QueryFirstOrDefaultAsync<bool>(con, "usp_ClaimCodeProjectEnvironment",
                        new { SessionId = sessionId, QuestionId = questionId, StackImageCode = stackImageCode });
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> UpdateEnvironment(string sessionId, int questionId, string state, string environmentHandle = null,
            string connectUrl = null, int? vmPropertyId = null, string message = null)
        {
            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    var affected = await DapperStoredProcedureHelper.QueryFirstOrDefaultAsync<int>(con, "usp_UpdateCodeProjectEnvironment", new
                    {
                        SessionId = sessionId,
                        QuestionId = questionId,
                        State = state,
                        EnvironmentHandle = environmentHandle,
                        ConnectUrl = connectUrl,
                        VMPropertyId = vmPropertyId,
                        Message = message
                    });
                    return affected > 0;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<CodeProjectEnvironment> GetEnvironment(string sessionId, int questionId)
        {
            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    return await DapperStoredProcedureHelper.QueryFirstOrDefaultAsync<CodeProjectEnvironment>(con, "usp_GetCodeProjectEnvironment",
                        new { SessionId = sessionId, QuestionId = questionId });
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<int> SaveCodeProjectAssessment(CodeProjectAssessment a)
        {
            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    var id = await DapperStoredProcedureHelper.QueryFirstOrDefaultAsync<int?>(con, "usp_SaveCodeProjectAssessment", new
                    {
                        a.SessionId,
                        a.QuestionId,
                        a.QuestionText,
                        a.CodeLanguage,
                        a.SubmittedCode,
                        a.TimeLimitInMinutes,
                        a.TimeStarted,
                        a.TimeCompleted,
                        a.TimeTakenInSeconds,
                        IsTimeExpired = a.IsTimeExpired ?? false,
                        a.VMPropertyId,
                        a.RequirementsPassed,
                        a.RequirementsTotal,
                        a.ResultsJson,
                        a.CreatedBy
                    });
                    return id ?? 0;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<CodeProjectAssessment> GetCodeProjectAssessment(string sessionId, int questionId)
        {
            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    return await DapperStoredProcedureHelper.QueryFirstOrDefaultAsync<CodeProjectAssessment>(con, "usp_GetCodeProjectAssessment",
                        new { SessionId = sessionId, QuestionId = questionId });
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<List<CodeProjectAssessment>> GetCodeProjectAssessmentsBySessionId(string sessionId)
        {
            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    var rows = await DapperStoredProcedureHelper.QueryAsync<CodeProjectAssessment>(con, "usp_GetCodeProjectAssessmentsBySessionId",
                        new { SessionId = sessionId });
                    return rows.ToList();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<CodeProjectAssessment>();
            }
        }
    }
}
