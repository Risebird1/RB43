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
    public class InterviewAlignmentRepository : IInterviewAlignmentRepository
    {
        private readonly string _connectionString;
        private readonly IApplicationErrorLogger _logger;

        public InterviewAlignmentRepository(IConfiguration configuration, IApplicationErrorLogger logger)
        {
            _connectionString = configuration["ConnectionString:DBConnectionString"];
            _logger = logger;
        }

        public async Task<bool> IsAlignmentRequiredAsync(int interviewId)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                return await con.QueryFirstOrDefaultAsync<bool?>(
                    "usp_GetInterviewAlignmentRequired",
                    new { InterviewId = interviewId },
                    commandType: CommandType.StoredProcedure) == true;
            }
            catch (SqlException ex)
            {
                // Not deployed yet, or unreachable: treat as off, so nothing answers for the channel.
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool?> SetAlignmentRequiredAsync(int irfId, bool isRequired)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var updated = await con.QueryFirstOrDefaultAsync<int?>(
                    "usp_SetInterviewerAlignmentRequired",
                    new { IrfId = irfId, IsRequired = isRequired },
                    commandType: CommandType.StoredProcedure);
                return updated.GetValueOrDefault() > 0;
            }
            catch (SqlException ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<InterviewAlignmentContext> GetAlignmentContextAsync(int interviewId, string interviewerEmail)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                return await con.QueryFirstOrDefaultAsync<InterviewAlignmentContext>(
                    "usp_GetInterviewAlignmentContext",
                    new { InterviewId = interviewId, InterviewerEmail = interviewerEmail },
                    commandType: CommandType.StoredProcedure);
            }
            catch (SqlException ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<IList<AlignmentFlaggedInterview>> GetFlaggedInterviewsAsync(int interviewId, string interviewerEmail)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var rows = await con.QueryAsync<AlignmentFlaggedInterview>(
                    "usp_GetInterviewAlignmentFlaggedInterviews",
                    new { InterviewId = interviewId, InterviewerEmail = interviewerEmail },
                    commandType: CommandType.StoredProcedure);
                return rows.ToList();
            }
            catch (SqlException ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> AcknowledgeTrialOnboardingAsync(string interviewerEmail, string policyVersion, DateTime now)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                await con.ExecuteAsync(
                    "usp_AcknowledgeTrialOnboarding",
                    new { InterviewerEmail = interviewerEmail, PolicyVersion = policyVersion, CurrentDate = now },
                    commandType: CommandType.StoredProcedure);
                return true;
            }
            catch (SqlException ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> SaveJoinGateOverrideAsync(int interviewId, string interviewerEmail, string outstandingRequirements, DateTime now)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                await con.ExecuteAsync(
                    "usp_SaveJoinGateOverride",
                    new
                    {
                        InterviewId = interviewId,
                        InterviewerEmail = interviewerEmail,
                        OutstandingRequirements = outstandingRequirements,
                        CurrentDate = now
                    },
                    commandType: CommandType.StoredProcedure);
                return true;
            }
            catch (SqlException ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<IList<InterviewerCodeQuestion>> GetInterviewerCodeQuestionsAsync(int interviewId, string interviewerEmail)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var rows = await con.QueryAsync<InterviewerCodeQuestion>(
                    "usp_GetInterviewerCodeQuestions",
                    new { InterviewId = interviewId, InterviewerEmail = interviewerEmail },
                    commandType: CommandType.StoredProcedure);
                return rows.ToList();
            }
            catch (SqlException ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<CreateInterviewCodeSessionResult> CreateCodeSessionAsync(int interviewId, string interviewerEmail, int questionId,
            string questionText, string skillName, int? timeLimitInMinutes, Guid token, DateTime now)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@InterviewId", interviewId);
                parameters.Add("@InterviewerEmail", interviewerEmail);
                parameters.Add("@QuestionId", questionId);
                parameters.Add("@QuestionText", questionText, DbType.String);
                parameters.Add("@SkillName", skillName);
                parameters.Add("@TimeLimit", timeLimitInMinutes);
                parameters.Add("@Token", token);
                parameters.Add("@CurrentDate", now);

                // The status column is always present; the session columns only on success.
                var row = await con.QueryFirstOrDefaultAsync<CreateSessionRow>(
                    "usp_CreateInterviewCodeSession", parameters, commandType: CommandType.StoredProcedure);
                if (row == null)
                {
                    return null;
                }

                var result = new CreateInterviewCodeSessionResult { Status = row.Status };
                if (string.Equals(row.Status, "ok", StringComparison.OrdinalIgnoreCase))
                {
                    result.Session = row;
                }
                return result;
            }
            catch (SqlException ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<IList<InterviewCodeSession>> GetCodeSessionsAsync(int interviewId)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var rows = await con.QueryAsync<InterviewCodeSession>(
                    "usp_GetInterviewCodeSessions",
                    new { InterviewId = interviewId },
                    commandType: CommandType.StoredProcedure);
                return rows.ToList();
            }
            catch (SqlException ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<InterviewCodeSession> GetCodeSessionByTokenAsync(Guid token)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                return await con.QueryFirstOrDefaultAsync<InterviewCodeSession>(
                    "usp_GetInterviewCodeSessionByToken",
                    new { Token = token },
                    commandType: CommandType.StoredProcedure);
            }
            catch (SqlException ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<DateTime?> MarkCodeSessionOpenedAsync(Guid token, DateTime now)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                return await con.QueryFirstOrDefaultAsync<DateTime?>(
                    "usp_MarkInterviewCodeSessionOpened",
                    new { Token = token, CurrentDate = now },
                    commandType: CommandType.StoredProcedure);
            }
            catch (SqlException ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> SaveCodeSessionRunAsync(Guid token, string languageId, string source, string output, string status, DateTime now)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@Token", token);
                parameters.Add("@LanguageId", languageId);
                parameters.Add("@Source", source, DbType.String);
                parameters.Add("@Output", output, DbType.String);
                parameters.Add("@Status", Truncate(status, 40));
                parameters.Add("@CurrentDate", now);
                await con.ExecuteAsync("usp_SaveInterviewCodeSessionRun", parameters, commandType: CommandType.StoredProcedure);
                return true;
            }
            catch (SqlException ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> SubmitCodeSessionAsync(Guid token, string languageId, string source, DateTime now)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@Token", token);
                parameters.Add("@LanguageId", languageId);
                parameters.Add("@Source", source, DbType.String);
                parameters.Add("@CurrentDate", now);
                var row = await con.QueryFirstOrDefaultAsync<SubmitRow>(
                    "usp_SubmitInterviewCodeSession", parameters, commandType: CommandType.StoredProcedure);
                // Accepted is false when the session had already been submitted (the stored code is final). A database
                // still on the older procedure returns no Accepted column; then a SubmittedOn means it was saved.
                return row != null && (row.Accepted ?? row.SubmittedOn.HasValue);
            }
            catch (SqlException ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        private static string Truncate(string value, int length) =>
            string.IsNullOrEmpty(value) || value.Length <= length ? value : value.Substring(0, length);

        private sealed class CreateSessionRow : InterviewCodeSession
        {
            public string Status { get; set; }
        }

        private sealed class SubmitRow
        {
            public DateTime? SubmittedOn { get; set; }
            public bool? Accepted { get; set; }
        }
    }
}
