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
using Risebird.Entities.Data.PlivoCXModels;

namespace Risebird.Repository
{
    public class InterviewerEmailAvailabilityRepository : IInterviewerEmailAvailabilityRepository
    {
        private readonly string _connectionString;
        private readonly IApplicationErrorLogger _logger;

        public InterviewerEmailAvailabilityRepository(IConfiguration configuration, IApplicationErrorLogger logger)
        {
            _connectionString = configuration["ConnectionString:DBConnectionString"];
            _logger = logger;
        }

        public async Task<int> InsertEmailAvailabilityAsync(InterviewerEmailAvailability record)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@SenderEmail", record.SenderEmail);
                parameters.Add("@Subject", record.Subject);
                parameters.Add("@MailContent", record.MailContent);
                parameters.Add("@ReceivedAt", record.ReceivedAt);
                parameters.Add("@JobTitle", record.JobTitle);
                parameters.Add("@InterviewerName", record.InterviewerName);
                parameters.Add("@Phone", record.Phone);
                parameters.Add("@MagName", record.MagName);
                parameters.Add("@IRFReference", record.IRFReference);
                parameters.Add("@HasLoginCredentials", record.HasLoginCredentials);
                parameters.Add("@SlotsPerDay", record.SlotsPerDay);
                parameters.Add("@TimeFrame", record.TimeFrame);
                parameters.Add("@AvailabilityIntent", record.AvailabilityIntent);
                parameters.Add("@MailBodyIntent", record.MailBodyIntent);
                parameters.Add("@ConcernsOrNotInterested", record.ConcernsOrNotInterested);
                parameters.Add("@ResumeFitment", record.ResumeFitment);
                parameters.Add("@ResumeLink", record.ResumeLink);
                parameters.Add("@HasAttachment", record.HasAttachment);
                parameters.Add("@NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await con.ExecuteAsync("usp_InsertInterviewerEmailAvailability", parameters,
                    commandType: CommandType.StoredProcedure, commandTimeout: 60);

                return parameters.Get<int>("@NewId");
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<List<InterviewerEmailAvailabilityVM>> GetEmailAvailabilityAsync(
            string status, string senderEmail, string resumeFitment,
            DateTime? dateFrom, DateTime? dateTo, int page, int pageSize)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@Status", string.IsNullOrWhiteSpace(status) ? null : status);
                parameters.Add("@SenderEmail", string.IsNullOrWhiteSpace(senderEmail) ? null : senderEmail);
                parameters.Add("@ResumeFitment", string.IsNullOrWhiteSpace(resumeFitment) ? null : resumeFitment);
                parameters.Add("@DateFrom", dateFrom);
                parameters.Add("@DateTo", dateTo);
                parameters.Add("@Page", page);
                parameters.Add("@PageSize", pageSize);

                var result = await con.QueryAsync<InterviewerEmailAvailabilityVM>(
                    "usp_GetInterviewerEmailAvailability", parameters,
                    commandType: CommandType.StoredProcedure, commandTimeout: 120);

                return result.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<InterviewerEmailAvailabilityVM>();
            }
        }

        public async Task<InterviewerEmailAvailabilityVM> GetEmailAvailabilityByIdAsync(int id)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@Id", id);

                var result = await con.QueryFirstOrDefaultAsync<InterviewerEmailAvailabilityVM>(
                    "usp_GetInterviewerEmailAvailabilityById", parameters,
                    commandType: CommandType.StoredProcedure, commandTimeout: 60);

                return result;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> UpdateEmailAvailabilityStatusAsync(int id, string status, string changedBy)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@Id", id);
                parameters.Add("@Status", status);
                parameters.Add("@ChangedBy", changedBy);

                var rows = await con.QueryFirstOrDefaultAsync<int>(
                    "usp_UpdateInterviewerEmailAvailabilityStatus", parameters,
                    commandType: CommandType.StoredProcedure, commandTimeout: 60);

                return rows > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> UpdateEmailAvailabilityVerificationAsync(
            int id, bool? employmentVerified, bool? resumeAIAnalysisCompleted,
            string resumeAIAnalysisResult, string newStatus)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@Id", id);
                parameters.Add("@EmploymentVerified", employmentVerified);
                parameters.Add("@ResumeAIAnalysisCompleted", resumeAIAnalysisCompleted);
                parameters.Add("@ResumeAIAnalysisResult", resumeAIAnalysisResult);
                parameters.Add("@NewStatus", newStatus);

                var rows = await con.QueryFirstOrDefaultAsync<int>(
                    "usp_UpdateEmailAvailabilityVerification", parameters,
                    commandType: CommandType.StoredProcedure, commandTimeout: 60);

                return rows > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> AssignInterviewerToAssessmentGroupAsync(
            int emailAvailabilityId, int interviewerProfileId,
            int assessmentGroupId, string assignedBy)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@EmailAvailabilityId", emailAvailabilityId);
                parameters.Add("@InterviewerProfileId", interviewerProfileId);
                parameters.Add("@AssessmentGroupId", assessmentGroupId);
                parameters.Add("@AssignedBy", assignedBy);

                var result = await con.QueryFirstOrDefaultAsync<int>(
                    "usp_AssignInterviewerToAssessmentGroup", parameters,
                    commandType: CommandType.StoredProcedure, commandTimeout: 60);

                return result > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> DeleteEmailAvailabilityAsync(int id)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@Id", id);

                var rows = await con.QueryFirstOrDefaultAsync<int>(
                    "usp_DeleteInterviewerEmailAvailability", parameters,
                    commandType: CommandType.StoredProcedure, commandTimeout: 60);

                return rows > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> UpdateResumeLinkAsync(int id, string resumeLink)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@Id", id);
                parameters.Add("@ResumeLink", resumeLink);

                var rows = await con.QueryFirstOrDefaultAsync<int>(
                    "usp_UpdateInterviewerEmailAvailabilityResumeLink", parameters,
                    commandType: CommandType.StoredProcedure, commandTimeout: 60);

                return rows > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> DropInterviewerProfileAsync(int interviewerProfileId, string modifiedBy)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@InterviewerProfileId", interviewerProfileId);
                parameters.Add("@ModifiedBy", modifiedBy);

                var rows = await con.QueryFirstOrDefaultAsync<int>(
                    "usp_DropInterviewerProfileFromAvailability", parameters,
                    commandType: CommandType.StoredProcedure, commandTimeout: 60);

                return rows > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task InsertHistoryAsync(int recordId, string eventType, string fromValue, string toValue, string changedBy)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                await con.ExecuteAsync("usp_InsertInterviewerEmailAvailabilityHistory",
                    new { RecordId = recordId, EventType = eventType, FromValue = fromValue, ToValue = toValue, ChangedBy = changedBy },
                    commandType: CommandType.StoredProcedure, commandTimeout: 30);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task<List<InterviewerEmailAvailabilityHistory>> GetHistoryAsync(int recordId)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var results = await con.QueryAsync<InterviewerEmailAvailabilityHistory>(
                    "usp_GetInterviewerEmailAvailabilityHistory",
                    new { RecordId = recordId },
                    commandType: CommandType.StoredProcedure);
                return results.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<InterviewerEmailAvailabilityHistory>();
            }
        }

        public async Task<bool> UpdateMagNameAsync(int id, string magName)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var rows = await con.QueryFirstOrDefaultAsync<int>(
                    "usp_UpdateInterviewerEmailAvailabilityMagName",
                    new { Id = id, MagName = magName },
                    commandType: CommandType.StoredProcedure, commandTimeout: 30);
                return rows > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<List<MagDropdownItem>> GetAllMasterAssessmentGroupsAsync()
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var result = await con.QueryAsync<MagDropdownItem>(
                    "usp_GetAllMasterAssessmentGroups",
                    commandType: CommandType.StoredProcedure, commandTimeout: 30);
                return result.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<MagDropdownItem>();
            }
        }

        // ── Plivo call ─────────────────────────────────────────────────────────────

        public async Task<bool> UpdatePlivoCallStatusAsync(int id, string callStatus, string conversationId,
            DateTime? callAt, int? duration, string transcript, string summary,
            string intent, bool? assignmentHelp, bool? emailReceived, bool? callbackRequested,
            string notes, string calledBy)
        {
            using var con = new SqlConnection(_connectionString);
            var p = new DynamicParameters();
            p.Add("@Id", id);
            p.Add("@PlivoCallStatus", callStatus);
            p.Add("@PlivoConversationId", conversationId);
            p.Add("@PlivoCallAt", callAt);
            p.Add("@PlivoCallDuration", duration);
            p.Add("@PlivoCallTranscript", transcript);
            p.Add("@PlivoCallSummary", summary);
            p.Add("@PlivoIntent", intent);
            p.Add("@PlivoAssignmentHelp", assignmentHelp);
            p.Add("@PlivoEmailReceived", emailReceived);
            p.Add("@PlivoCallbackRequested", callbackRequested);
            p.Add("@PlivoNotes", notes);
            p.Add("@PlivoCalledBy", calledBy);

            var rows = await con.QueryFirstOrDefaultAsync<int>(
                "usp_UpdateIAEmailAvailabilityPlivoCallStatus", p,
                commandType: CommandType.StoredProcedure, commandTimeout: 60);
            return rows > 0;
        }

        public async Task<int?> FindByPhoneAndCallStatusAsync(string phone, string callStatus)
        {
            using var con = new SqlConnection(_connectionString);
            return await con.QueryFirstOrDefaultAsync<int?>(
                "usp_FindIAByPhoneAndCallStatus",
                new { Phone = phone, CallStatus = callStatus },
                commandType: CommandType.StoredProcedure, commandTimeout: 30);
        }

        // ── Batch call ─────────────────────────────────────────────────────────────

        public async Task<List<IABatchCallMagSummary>> GetMAGsWithCallableInterviewerCountsAsync()
        {
            using var con = new SqlConnection(_connectionString);
            var result = await con.QueryAsync<IABatchCallMagSummary>(
                "usp_GetIABatchCallMAGSummary",
                commandType: CommandType.StoredProcedure, commandTimeout: 120);
            return result.ToList();
        }

        public async Task<List<IACallableInterviewer>> GetCallableInterviewersForMAGAsync(string magName, bool onlyUncalled = true)
        {
            using var con = new SqlConnection(_connectionString);
            var result = await con.QueryAsync<IACallableInterviewer>(
                "usp_GetInterviewersForMAGBatchCall",
                new { MagName = magName, OnlyUncalled = onlyUncalled },
                commandType: CommandType.StoredProcedure, commandTimeout: 60);
            return result.ToList();
        }

        public async Task<int> InsertBatchCallLogAsync(string magName, string initiatedBy, int totalInterviewers)
        {
            using var con = new SqlConnection(_connectionString);
            var p = new DynamicParameters();
            p.Add("@MagName", magName);
            p.Add("@InitiatedBy", initiatedBy);
            p.Add("@TotalInterviewers", totalInterviewers);
            p.Add("@NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await con.ExecuteAsync("usp_InsertIABatchCallLog", p,
                commandType: CommandType.StoredProcedure, commandTimeout: 30);
            return p.Get<int>("@NewId");
        }

        public async Task CompleteBatchCallLogAsync(int id, int succeeded, int failed, int skipped, string status)
        {
            using var con = new SqlConnection(_connectionString);
            await con.ExecuteAsync("usp_CompleteIABatchCallLog",
                new { Id = id, Succeeded = succeeded, Failed = failed, Skipped = skipped, Status = status },
                commandType: CommandType.StoredProcedure, commandTimeout: 30);
        }

        public async Task<List<IABatchCallLogEntry>> GetBatchCallHistoryAsync(string magName)
        {
            using var con = new SqlConnection(_connectionString);
            var result = await con.QueryAsync<IABatchCallLogEntry>(
                "usp_GetIABatchCallHistory",
                new { MagName = magName },
                commandType: CommandType.StoredProcedure, commandTimeout: 30);
            return result.ToList();
        }

        public async Task<List<IAInterviewerCallOutcome>> GetInterviewerCallOutcomesForMAGAsync(string magName)
        {
            using var con = new SqlConnection(_connectionString);
            var result = await con.QueryAsync<IAInterviewerCallOutcome>(
                "usp_GetInterviewerCallOutcomesForMAG",
                new { MagName = magName },
                commandType: CommandType.StoredProcedure, commandTimeout: 60);
            return result.ToList();
        }
    }
}
