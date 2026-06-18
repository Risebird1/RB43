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

        public async Task InsertHistoryAsync(int recordId, string eventType, string fromValue, string toValue, string changedBy)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var p = new DynamicParameters();
                p.Add("@RecordId", recordId);
                p.Add("@EventType", eventType);
                p.Add("@FromValue", fromValue);
                p.Add("@ToValue", toValue);
                p.Add("@ChangedBy", changedBy ?? "System");

                await con.ExecuteAsync("usp_InsertNaukriOnboardingHistory", p,
                    commandType: CommandType.StoredProcedure, commandTimeout: 30);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task<List<NaukriOnboardingHistory>> GetHistoryAsync(int recordId)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var p = new DynamicParameters();
                p.Add("@RecordId", recordId);

                var result = await con.QueryAsync<NaukriOnboardingHistory>(
                    "usp_GetNaukriOnboardingHistory", p,
                    commandType: CommandType.StoredProcedure, commandTimeout: 30);
                return result.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<NaukriOnboardingHistory>();
            }
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
                p.Add("@ParsedEmail", record.ParsedEmail);
                p.Add("@ParsedPhone", record.ParsedPhone);
                p.Add("@ResumeParseCompleted", record.ResumeParseCompleted);
                p.Add("@ResumeLink", record.ResumeLink);
                p.Add("@HasAttachment", record.HasAttachment);
                p.Add("@Source", record.Source ?? "Naukri");
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
            string status, string candidateName, string magName, string source,
            DateTime? dateFrom, DateTime? dateTo, int page, int pageSize,
            string automationStatus = null, string callStatus = null, string disposition = null,
            string funnelStage = null, string funnelMode = null, string onboardingType = null)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var p = new DynamicParameters();
                p.Add("@Status", string.IsNullOrWhiteSpace(status) ? null : status);
                p.Add("@CandidateName", string.IsNullOrWhiteSpace(candidateName) ? null : candidateName);
                p.Add("@MagName", string.IsNullOrWhiteSpace(magName) ? null : magName);
                p.Add("@Source", string.IsNullOrWhiteSpace(source) ? null : source);
                p.Add("@AutomationStatus", string.IsNullOrWhiteSpace(automationStatus) ? null : automationStatus);
                p.Add("@CallStatus", string.IsNullOrWhiteSpace(callStatus) ? null : callStatus);
                p.Add("@Disposition", string.IsNullOrWhiteSpace(disposition) ? null : disposition);
                p.Add("@FunnelStage", string.IsNullOrWhiteSpace(funnelStage) ? null : funnelStage);
                p.Add("@FunnelMode", string.IsNullOrWhiteSpace(funnelMode) ? "at" : funnelMode);
                p.Add("@OnboardingType", string.IsNullOrWhiteSpace(onboardingType) ? null : onboardingType);
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

        public async Task<Dictionary<string, int>> GetFunnelStageCountsAsync(
            string status, string candidateName, string magName, string source,
            DateTime? dateFrom, DateTime? dateTo,
            string automationStatus = null, string callStatus = null, string disposition = null,
            string funnelStage = null, string funnelMode = null, string onboardingType = null)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var p = new DynamicParameters();
                p.Add("@Status", string.IsNullOrWhiteSpace(status) ? null : status);
                p.Add("@CandidateName", string.IsNullOrWhiteSpace(candidateName) ? null : candidateName);
                p.Add("@MagName", string.IsNullOrWhiteSpace(magName) ? null : magName);
                p.Add("@Source", string.IsNullOrWhiteSpace(source) ? null : source);
                p.Add("@AutomationStatus", string.IsNullOrWhiteSpace(automationStatus) ? null : automationStatus);
                p.Add("@CallStatus", string.IsNullOrWhiteSpace(callStatus) ? null : callStatus);
                p.Add("@Disposition", string.IsNullOrWhiteSpace(disposition) ? null : disposition);
                p.Add("@FunnelStage", string.IsNullOrWhiteSpace(funnelStage) ? null : funnelStage);
                p.Add("@FunnelMode", string.IsNullOrWhiteSpace(funnelMode) ? "at" : funnelMode);
                p.Add("@OnboardingType", string.IsNullOrWhiteSpace(onboardingType) ? null : onboardingType);
                p.Add("@DateFrom", dateFrom);
                p.Add("@DateTo", dateTo);

                var rows = await con.QueryAsync(
                    "usp_GetNaukriFunnelStageCounts", p,
                    commandType: CommandType.StoredProcedure, commandTimeout: 60);

                var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var row in rows)
                {
                    string stage = row.FunnelStage as string;
                    var key = string.IsNullOrWhiteSpace(stage) ? "New" : stage;
                    dict[key] = (int)row.Cnt;
                }
                return dict;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
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

        public async Task<bool> UpdateResumeLinkAsync(int id, string resumeLink)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var rows = await con.ExecuteAsync(
                    "UPDATE NaukriProspectiveInterviewer SET ResumeLink = @Link, HasAttachment = 1, UpdatedAt = GETUTCDATE() WHERE Id = @Id",
                    new { Link = resumeLink, Id = id }, commandTimeout: 30);
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

        public async Task<dynamic> SaveManualOverrideAsync(int id, string fieldName, string oldValue, string newValue, string changedBy)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var p = new DynamicParameters();
                p.Add("@Id", id);
                p.Add("@FieldName", fieldName);
                p.Add("@OldValue", oldValue);
                p.Add("@NewValue", newValue);
                p.Add("@ChangedBy", changedBy);

                return await con.QueryFirstOrDefaultAsync<dynamic>(
                    "usp_UpdateNaukriManualOverride", p,
                    commandType: CommandType.StoredProcedure, commandTimeout: 30);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<int?> GetInterviewerProfileIdByUserIdAsync(int userId)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                return await con.QueryFirstOrDefaultAsync<int?>(
                    "SELECT Id FROM InterviewerProfile WHERE UserId = @UserId",
                    new { UserId = userId }, commandTimeout: 30);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> UpdateSelectedIRFAsync(int id, int irfId, string irfName, string applicationLink, int? assessmentGroupId)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var p = new DynamicParameters();
                p.Add("@Id", id);
                p.Add("@SelectedIRFId", irfId);
                p.Add("@SelectedIRFName", irfName);
                p.Add("@ApplicationLink", applicationLink);
                p.Add("@AssessmentGroupId", assessmentGroupId);
                await con.ExecuteAsync("usp_UpdateNaukriSelectedIRF", p, commandType: CommandType.StoredProcedure, commandTimeout: 30);
                return true;
            }
            catch (Exception ex) { await _logger.LogMessage(LogLevelInfo.Exception, ex); return false; }
        }

        public async Task<bool> UpdateInvitationEmailSentAsync(int id)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                await con.ExecuteAsync("usp_UpdateNaukriInvitationEmail", new { Id = id }, commandType: CommandType.StoredProcedure, commandTimeout: 30);
                return true;
            }
            catch (Exception ex) { await _logger.LogMessage(LogLevelInfo.Exception, ex); return false; }
        }

        public async Task<bool> UpdateProspectResponseAsync(int id, string response)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var p = new DynamicParameters();
                p.Add("@Id", id);
                p.Add("@ProspectResponse", response);
                await con.ExecuteAsync("usp_UpdateNaukriProspectResponse", p, commandType: CommandType.StoredProcedure, commandTimeout: 30);
                return true;
            }
            catch (Exception ex) { await _logger.LogMessage(LogLevelInfo.Exception, ex); return false; }
        }

        public async Task<List<ActiveIRFItem>> GetActiveIRFsByAssessmentGroupIdAsync(int assessmentGroupId)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var result = await con.QueryAsync<ActiveIRFItem>(
                    "usp_GetActiveIRFsByAssessmentGroupId",
                    new { AssessmentGroupId = assessmentGroupId },
                    commandType: CommandType.StoredProcedure, commandTimeout: 30);
                return result?.AsList() ?? new List<ActiveIRFItem>();
            }
            catch (Exception ex) { await _logger.LogMessage(LogLevelInfo.Exception, ex); return new List<ActiveIRFItem>(); }
        }

        public async Task<bool> UpdateSelfRegistrationAsync(int id, bool completed, int? userId, int? profileId)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                await con.ExecuteAsync(
                    @"UPDATE NaukriProspectiveInterviewer
                      SET SelfRegistrationCompleted = @Completed,
                          OnboardedUserId = ISNULL(@UserId, OnboardedUserId),
                          OnboardedProfileId = ISNULL(@ProfileId, OnboardedProfileId),
                          OnboardingCompleted = CASE WHEN @Completed = 1 THEN 1 ELSE OnboardingCompleted END,
                          Status = CASE WHEN @Completed = 1 THEN 'Onboarded' ELSE Status END,
                          UpdatedAt = GETUTCDATE()
                      WHERE Id = @Id",
                    new { Id = id, Completed = completed, UserId = userId, ProfileId = profileId },
                    commandTimeout: 30);
                return true;
            }
            catch (Exception ex) { await _logger.LogMessage(LogLevelInfo.Exception, ex); return false; }
        }

        // ── Prospect Onboarding redesign (Phase 2) ──────────────────────────────────

        // Sets the single, unified Intent that both email replies and AI Plivo calls feed.
        public async Task<bool> SetUnifiedIntentAsync(int id, string intent, string intentSource, DateTime? confirmedAt)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                await con.ExecuteAsync(
                    @"UPDATE NaukriProspectiveInterviewer
                      SET Intent            = @Intent,
                          IntentSource      = @IntentSource,
                          IntentConfirmedAt = ISNULL(@ConfirmedAt, IntentConfirmedAt),
                          UpdatedAt         = GETUTCDATE()
                      WHERE Id = @Id",
                    new { Id = id, Intent = intent, IntentSource = intentSource, ConfirmedAt = confirmedAt },
                    commandTimeout: 30);
                return true;
            }
            catch (Exception ex) { await _logger.LogMessage(LogLevelInfo.Exception, ex); return false; }
        }

        // Advances the funnel forward-only (never regresses) and optionally sets the
        // operational ticket status. Ticket status is kept separate from the funnel stage.
        public async Task<bool> AdvanceFunnelStageAsync(int id, string funnelStage, string ticketStatus = null)
        {
            try
            {
                int newRank = FunnelStageRank(funnelStage);
                using var con = new SqlConnection(_connectionString);
                await con.ExecuteAsync(
                    @"UPDATE NaukriProspectiveInterviewer
                      SET FunnelStage = CASE
                              WHEN @NewRank >= (CASE ISNULL(FunnelStage, '')
                                  WHEN 'Parsed'              THEN 1
                                  WHEN 'MAGMapped'           THEN 2
                                  WHEN 'InvitationSent'      THEN 3
                                  WHEN 'IntentCollection'    THEN 4
                                  WHEN 'IntentConfirmed'     THEN 5
                                  WHEN 'EmploymentVerified'  THEN 6
                                  WHEN 'ResumeFitment'       THEN 7
                                  WHEN 'AcknowledgementSent' THEN 8
                                  WHEN 'Onboarded'           THEN 9
                                  ELSE 0 END)
                              THEN @FunnelStage ELSE FunnelStage END,
                          TicketStatus = ISNULL(@TicketStatus, TicketStatus),
                          UpdatedAt    = GETUTCDATE()
                      WHERE Id = @Id",
                    new { Id = id, FunnelStage = funnelStage, NewRank = newRank, TicketStatus = ticketStatus },
                    commandTimeout: 30);
                return true;
            }
            catch (Exception ex) { await _logger.LogMessage(LogLevelInfo.Exception, ex); return false; }
        }

        public async Task<bool> SetOnboardingTypeAsync(int id, string onboardingType)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                await con.ExecuteAsync(
                    @"UPDATE NaukriProspectiveInterviewer
                      SET OnboardingType = @OnboardingType, UpdatedAt = GETUTCDATE()
                      WHERE Id = @Id",
                    new { Id = id, OnboardingType = onboardingType },
                    commandTimeout: 30);
                return true;
            }
            catch (Exception ex) { await _logger.LogMessage(LogLevelInfo.Exception, ex); return false; }
        }

        public async Task<int?> FindProspectIdByOnboardedUserIdAsync(int userId)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                return await con.QueryFirstOrDefaultAsync<int?>(
                    @"SELECT TOP 1 Id FROM NaukriProspectiveInterviewer
                      WHERE OnboardedUserId = @UserId
                      ORDER BY Id DESC",
                    new { UserId = userId },
                    commandTimeout: 30);
            }
            catch (Exception ex) { await _logger.LogMessage(LogLevelInfo.Exception, ex); return null; }
        }

        private static int FunnelStageRank(string stage)
        {
            switch (stage)
            {
                case "Parsed":              return 1;
                case "MAGMapped":           return 2;
                case "InvitationSent":      return 3;
                case "IntentCollection":    return 4;
                case "IntentConfirmed":     return 5;
                case "EmploymentVerified":  return 6;
                case "ResumeFitment":       return 7;
                case "AcknowledgementSent": return 8;
                case "Onboarded":           return 9;
                default:                    return 0;
            }
        }

        public async Task<(int? UserId, int? ProfileId)> FindUserByEmailAsync(string email)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var row = await con.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT TOP 1 ul.id AS UserId, ip.Id AS ProfileId FROM UserList ul LEFT JOIN InterviewerProfile ip ON ip.UserId = ul.id WHERE LOWER(LTRIM(RTRIM(ul.EmailId))) = @Email AND ul.IsActive = 1",
                    new { Email = email.Trim().ToLower() }, commandTimeout: 30);
                if (row == null) return (null, null);
                return ((int?)row.UserId, (int?)row.ProfileId);
            }
            catch (Exception ex) { await _logger.LogMessage(LogLevelInfo.Exception, ex); return (null, null); }
        }

        public async Task<bool> UpdatePlivoCallStatusAsync(int id, string callStatus, string conversationId,
            DateTime? callAt, int? duration, string transcript, string summary,
            string disposition, string onboardingPreference, bool? kycAccepted, string calledBy)
        {
            try
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
                p.Add("@PlivoProspectDisposition", disposition);
                p.Add("@PlivoOnboardingPreference", onboardingPreference);
                p.Add("@PlivoKycAccepted", kycAccepted);
                p.Add("@PlivoCalledBy", calledBy);

                var rows = await con.QueryFirstOrDefaultAsync<int>(
                    "usp_UpdateNaukriPlivoCallStatus", p,
                    commandType: CommandType.StoredProcedure, commandTimeout: 60);
                return rows > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<int?> FindProspectIdByPhoneAndCallStatusAsync(string phone, string callStatus)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var sql = @"SELECT TOP 1 Id FROM NaukriProspectiveInterviewer
                            WHERE ParsedPhone = @Phone AND PlivoCallStatus = @Status
                            ORDER BY Id DESC";
                return await con.QueryFirstOrDefaultAsync<int?>(sql, new { Phone = phone, Status = callStatus });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<List<NaukriProspectiveInterviewerVM>> GetEligibleProspectsForAutomationAsync(int batchSize = 2)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var result = await con.QueryAsync<NaukriProspectiveInterviewerVM>(
                    "usp_GetEligibleProspectsForAutomation",
                    new { BatchSize = batchSize },
                    commandType: CommandType.StoredProcedure, commandTimeout: 60);
                return result?.ToList() ?? new List<NaukriProspectiveInterviewerVM>();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<NaukriProspectiveInterviewerVM>();
            }
        }

        public async Task<bool> UpdateAutomationStatusAsync(int id, string automationStatus, bool incrementCallAttempt = false)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var rows = await con.QueryFirstOrDefaultAsync<int>(
                    "usp_UpdateProspectAutomationStatus",
                    new { Id = id, AutomationStatus = automationStatus, IncrementCallAttempt = incrementCallAttempt },
                    commandType: CommandType.StoredProcedure, commandTimeout: 60);
                return rows > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<int?> GetLatestActiveIRFForAssessmentGroupAsync(int assessmentGroupId)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var sql = @"SELECT TOP 1 ti.id
                            FROM TransInterviewer ti
                            WHERE ti.AssessmentGroupId = @AgId
                              AND ISNULL(ti.IsActive, 0) = 1
                              AND ISNULL(ti.IsDeleted, 0) = 0
                            ORDER BY ti.id DESC";
                return await con.QueryFirstOrDefaultAsync<int?>(sql, new { AgId = assessmentGroupId });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> UpdateMagNameAsync(int id, string magName)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var rows = await con.ExecuteAsync(
                    "UPDATE NaukriProspectiveInterviewer SET MagName = @MagName, UpdatedAt = GETUTCDATE() WHERE Id = @Id",
                    new { Id = id, MagName = magName });
                return rows > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> UpdateParsedEmailAsync(int id, string email)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var rows = await con.ExecuteAsync(
                    "UPDATE NaukriProspectiveInterviewer SET ParsedEmail = @Email, UpdatedAt = GETUTCDATE() WHERE Id = @Id",
                    new { Id = id, Email = email });
                return rows > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> UpdateParsedPhoneAsync(int id, string phone)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var rows = await con.ExecuteAsync(
                    "UPDATE NaukriProspectiveInterviewer SET ParsedPhone = @Phone, UpdatedAt = GETUTCDATE() WHERE Id = @Id",
                    new { Id = id, Phone = phone });
                return rows > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<List<BatchCallMagSummary>> GetMAGsWithCallableProspectCountsAsync()
        {
            using var con = new SqlConnection(_connectionString);
            var result = await con.QueryAsync<BatchCallMagSummary>(
                "usp_GetBatchCallMAGSummary",
                commandType: System.Data.CommandType.StoredProcedure);
            return result.AsList();
        }

        public async Task<List<NaukriProspectiveInterviewerVM>> GetCallableProspectsForMAGAsync(string magName, bool onlyUncalled = true)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var result = await con.QueryAsync<NaukriProspectiveInterviewerVM>(
                    "usp_GetProspectsForMAGBatchCall",
                    new { MagName = magName, OnlyUncalled = onlyUncalled },
                    commandType: System.Data.CommandType.StoredProcedure);
                return result.AsList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<NaukriProspectiveInterviewerVM>();
            }
        }

        public async Task<int> InsertBatchCallLogAsync(string magName, string initiatedBy, int totalProspects)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var p = new DynamicParameters();
                p.Add("@MagName", magName);
                p.Add("@InitiatedBy", initiatedBy);
                p.Add("@TotalProspects", totalProspects);
                p.Add("@NewId", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);
                await con.ExecuteAsync("usp_InsertBatchCallLog", p, commandType: System.Data.CommandType.StoredProcedure);
                return p.Get<int>("@NewId");
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task CompleteBatchCallLogAsync(int id, int succeeded, int failed, int skipped, string status)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                await con.ExecuteAsync("usp_CompleteBatchCallLog",
                    new { Id = id, ProspectsSucceeded = succeeded, ProspectsFailed = failed, ProspectsSkipped = skipped, Status = status },
                    commandType: System.Data.CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task<List<BatchCallLogEntry>> GetBatchCallHistoryAsync(string magName)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var result = await con.QueryAsync<BatchCallLogEntry>(
                    "usp_GetBatchCallHistory",
                    new { MagName = magName },
                    commandType: System.Data.CommandType.StoredProcedure);
                return result.AsList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<BatchCallLogEntry>();
            }
        }

        public async Task<(List<NaukriProspectiveInterviewerVM> Items, int TotalCount)> GetProspectCallOutcomesForMAGAsync(string magName, int page = 1, int pageSize = 25)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 25 : (pageSize > 200 ? 200 : pageSize);

            try
            {
                using var con = new SqlConnection(_connectionString);
                var p = new DynamicParameters();
                p.Add("@MagName", magName);
                p.Add("@Page", page);
                p.Add("@PageSize", pageSize);
                p.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                var result = await con.QueryAsync<NaukriProspectiveInterviewerVM>(
                    "usp_GetProspectCallOutcomesForMAG",
                    p,
                    commandType: System.Data.CommandType.StoredProcedure);
                return (result.AsList(), p.Get<int>("@TotalCount"));
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return (new List<NaukriProspectiveInterviewerVM>(), 0);
            }
        }
    }
}
