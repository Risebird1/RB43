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
    public class NaukriOnboardingRepository : INaukriOnboardingRepository
    {
        private readonly string _connectionString;
        private readonly IApplicationErrorLogger _logger;

        public NaukriOnboardingRepository(IConfiguration configuration, IApplicationErrorLogger logger)
        {
            _connectionString = configuration["ConnectionString:DBConnectionString"];
            _logger = logger;
        }

        public async Task<int> InsertAsync(NaukriProspectiveInterviewer record)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var p = new DynamicParameters();
                p.Add("@SenderEmail", record.SenderEmail);
                p.Add("@Subject", record.Subject);
                p.Add("@MailContent", record.MailContent);
                p.Add("@ReceivedAt", record.ReceivedAt);
                p.Add("@CandidateName", record.CandidateName);
                p.Add("@Location", record.Location);
                p.Add("@PastExperience", record.PastExperience);
                p.Add("@NoticePeriod", record.NoticePeriod);
                p.Add("@Education", record.Education);
                p.Add("@KeySkills", record.KeySkills);
                p.Add("@CurrentRole", record.CurrentRole);
                p.Add("@ExperienceYears", record.ExperienceYears);
                p.Add("@Salary", record.Salary);
                p.Add("@MagName", record.MagName);
                p.Add("@ResumeLink", record.ResumeLink);
                p.Add("@HasAttachment", record.HasAttachment);
                p.Add("@NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await con.ExecuteAsync("usp_InsertNaukriProspectiveInterviewer", p,
                    commandType: CommandType.StoredProcedure, commandTimeout: 60);

                return p.Get<int>("@NewId");
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<List<NaukriProspectiveInterviewerVM>> GetListAsync(
            string status, string candidateName, string magName,
            DateTime? dateFrom, DateTime? dateTo, int page, int pageSize)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var p = new DynamicParameters();
                p.Add("@Status", string.IsNullOrWhiteSpace(status) ? null : status);
                p.Add("@CandidateName", string.IsNullOrWhiteSpace(candidateName) ? null : candidateName);
                p.Add("@MagName", string.IsNullOrWhiteSpace(magName) ? null : magName);
                p.Add("@DateFrom", dateFrom);
                p.Add("@DateTo", dateTo);
                p.Add("@Page", page);
                p.Add("@PageSize", pageSize);

                var result = await con.QueryAsync<NaukriProspectiveInterviewerVM>(
                    "usp_GetNaukriProspectiveInterviewers", p,
                    commandType: CommandType.StoredProcedure, commandTimeout: 60);

                return result.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<NaukriProspectiveInterviewerVM>();
            }
        }

        public async Task<NaukriProspectiveInterviewerVM> GetByIdAsync(int id)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var p = new DynamicParameters();
                p.Add("@Id", id);

                return await con.QueryFirstOrDefaultAsync<NaukriProspectiveInterviewerVM>(
                    "usp_GetNaukriProspectiveInterviewerById", p,
                    commandType: CommandType.StoredProcedure, commandTimeout: 60);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> UpdateStatusAsync(int id, string status, string changedBy)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var p = new DynamicParameters();
                p.Add("@Id", id);
                p.Add("@Status", status);
                p.Add("@ChangedBy", changedBy);

                var rows = await con.QueryFirstOrDefaultAsync<int>(
                    "usp_UpdateNaukriProspectiveStatus", p,
                    commandType: CommandType.StoredProcedure, commandTimeout: 60);
                return rows > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<(bool isExisting, int? existingUserId, int? existingProfileId)> UpdateResumeParseResultAsync(
            int id, string parsedEmail, string parsedPhone)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var p = new DynamicParameters();
                p.Add("@Id", id);
                p.Add("@ParsedEmail", parsedEmail);
                p.Add("@ParsedPhone", parsedPhone);

                var result = await con.QueryFirstOrDefaultAsync<dynamic>(
                    "usp_UpdateNaukriResumeParseResult", p,
                    commandType: CommandType.StoredProcedure, commandTimeout: 60);

                if (result == null) return (false, null, null);
                return ((bool)result.IsExisting, (int?)result.ExistingUserId, (int?)result.ExistingProfileId);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return (false, null, null);
            }
        }

        public async Task<bool> UpdateEmploymentVerificationAsync(
            int id, bool? verified, string companyName, bool? isEmployed, string details)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var p = new DynamicParameters();
                p.Add("@Id", id);
                p.Add("@EmploymentVerified", verified);
                p.Add("@EmploymentCompanyName", companyName);
                p.Add("@EmploymentIsEmployed", isEmployed);
                p.Add("@EmploymentDetails", details);

                var rows = await con.QueryFirstOrDefaultAsync<int>(
                    "usp_UpdateNaukriEmploymentVerification", p,
                    commandType: CommandType.StoredProcedure, commandTimeout: 60);
                return rows > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> UpdateAIAnalysisAsync(
            int id, bool completed, string analysisResult, string verdict, int? score, string newStatus)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var p = new DynamicParameters();
                p.Add("@Id", id);
                p.Add("@ResumeAIAnalysisCompleted", completed);
                p.Add("@ResumeAIAnalysisResult", analysisResult);
                p.Add("@AIVerdict", verdict);
                p.Add("@AIFitmentScore", score);
                p.Add("@NewStatus", newStatus);

                var rows = await con.QueryFirstOrDefaultAsync<int>(
                    "usp_UpdateNaukriAIAnalysis", p,
                    commandType: CommandType.StoredProcedure, commandTimeout: 60);
                return rows > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> UpdateOnboardingResultAsync(
            int id, int? userId, int? profileId, int? assessmentGroupId, bool ackSent)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var p = new DynamicParameters();
                p.Add("@Id", id);
                p.Add("@OnboardedUserId", userId);
                p.Add("@OnboardedProfileId", profileId);
                p.Add("@AssessmentGroupId", assessmentGroupId);
                p.Add("@AcknowledgementEmailSent", ackSent);

                var rows = await con.QueryFirstOrDefaultAsync<int>(
                    "usp_UpdateNaukriOnboardingResult", p,
                    commandType: CommandType.StoredProcedure, commandTimeout: 60);
                return rows > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> UpdateAckEmailSentAsync(int id)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var p = new DynamicParameters();
                p.Add("@Id", id);

                var rows = await con.QueryFirstOrDefaultAsync<int>(
                    "usp_UpdateNaukriAckEmailSent", p,
                    commandType: CommandType.StoredProcedure, commandTimeout: 60);
                return rows > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var p = new DynamicParameters();
                p.Add("@Id", id);

                var rows = await con.QueryFirstOrDefaultAsync<int>(
                    "usp_DeleteNaukriProspectiveInterviewer", p,
                    commandType: CommandType.StoredProcedure, commandTimeout: 60);
                return rows > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
    }
}
