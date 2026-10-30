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
    public class EmailLookupRepository : IEmailLookupRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public EmailLookupRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<EmailLookupResultVM> GetEmailLookupData(string emailId)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@EmailId", emailId);

                using var con = new SqlConnection(connectionString);
                using var multi = await con.QueryMultipleAsync(
                    "usp_GetEmailLookupData",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                var userInfo = (await multi.ReadAsync<EmailLookupUserInfoVM>()).FirstOrDefault();
                if (userInfo == null)
                {
                    return new EmailLookupResultVM { SearchEmail = emailId, IsFound = false };
                }

                var interviewerProfile = (await multi.ReadAsync<EmailLookupInterviewerProfileVM>()).FirstOrDefault();
                var clientProfile      = (await multi.ReadAsync<EmailLookupClientProfileVM>()).FirstOrDefault();
                var clientPOs          = (await multi.ReadAsync<EmailLookupClientPOVM>()).ToList();
                var candidateStats     = (await multi.ReadAsync<EmailLookupInterviewStatsVM>()).FirstOrDefault() ?? new EmailLookupInterviewStatsVM();
                var interviewerStats   = (await multi.ReadAsync<EmailLookupInterviewStatsVM>()).FirstOrDefault() ?? new EmailLookupInterviewStatsVM();
                var paymentStats       = (await multi.ReadAsync<EmailLookupPaymentStatsVM>()).FirstOrDefault() ?? new EmailLookupPaymentStatsVM();
                var meetingsAsCandidate   = (await multi.ReadAsync<EmailLookupMeetingVM>()).ToList();
                var meetingsAsInterviewer = (await multi.ReadAsync<EmailLookupMeetingVM>()).ToList();
                var recentPayments     = (await multi.ReadAsync<EmailLookupPaymentVM>()).ToList();
                var tdsQuarters        = (await multi.ReadAsync<EmailLookupTdsQuarterVM>()).ToList();
                var historyTotal       = (await multi.ReadAsync<EmailLookupHistoryTotalVM>()).FirstOrDefault();

                var result = new EmailLookupResultVM
                {
                    SearchEmail = emailId,
                    IsFound = true,
                    UserId = userInfo.UserId,
                    ProfileType = userInfo.ProfileType,
                    FirstName = userInfo.FirstName,
                    LastName = userInfo.LastName,
                    EmailId = userInfo.EmailId,
                    MobileNumber = userInfo.MobileNumber,
                    IsActive = userInfo.IsActive,

                    PrimarySkill = interviewerProfile?.PrimarySkill,
                    IsApproved = interviewerProfile?.IsApproved ?? false,
                    CurrentRole = interviewerProfile?.CurrentRole,
                    Experience = interviewerProfile?.Experience ?? 0,
                    City = interviewerProfile?.City,
                    Country = interviewerProfile?.Country,
                    CompanyName = interviewerProfile?.CompanyName,
                    IsLeadInterviewer = interviewerProfile?.IsLeadInterviewer ?? false,
                    NickName = interviewerProfile?.NickName,
                    IsHoldPayment = interviewerProfile?.IsHoldPayment ?? false,
                    HoldPaymentReason = interviewerProfile?.HoldPaymentReason,
                    IsInterviewerAccess = interviewerProfile?.IsInterviewerAccess ?? false,
                    IsBlockedProfile = interviewerProfile?.IsDeleted ?? false,
                    PAN = interviewerProfile?.PAN,
                    AccountName = interviewerProfile?.AccountName,
                    AccountNumber = interviewerProfile?.AccountNumber,
                    IFSCCode = interviewerProfile?.IFSCCode,
                    UPIId = interviewerProfile?.UPIId,
                    IsBankVerified = interviewerProfile?.IsBankVerified ?? false,
                    BankVerifiedOn = interviewerProfile?.BankVerifiedOn,
                    VerifiedAccountNumber = interviewerProfile?.VerifiedAccountNumber,
                    VerifiedAccountName = interviewerProfile?.VerifiedAccountName,
                    NameAtBank = interviewerProfile?.NameAtBank,
                    IsPanVerified = interviewerProfile?.IsPanVerified ?? false,
                    PanVerifiedOn = interviewerProfile?.PanVerifiedOn,
                    VerifiedPAN = interviewerProfile?.VerifiedPAN,
                    NameAtPan = interviewerProfile?.NameAtPan,

                    CandidateTotalCount = candidateStats.TotalCount,
                    CandidateTotalCompleted = candidateStats.TotalCompleted,
                    CandidateTotalCancelled = candidateStats.TotalCancelled,
                    CandidateTotalScheduled = candidateStats.TotalScheduled,

                    InterviewerTotalCount = interviewerStats.TotalCount,
                    InterviewerTotalCompleted = interviewerStats.TotalCompleted,
                    InterviewerTotalCancelled = interviewerStats.TotalCancelled,
                    InterviewerTotalScheduled = interviewerStats.TotalScheduled,

                    TotalPaymentAmount = paymentStats.TotalPaymentAmount,
                    TotalPaidAmount = paymentStats.TotalPaidAmount,
                    TotalPendingPayment = paymentStats.TotalPendingPayment,

                    ClientId          = clientProfile?.ClientId ?? 0,
                    ClientName        = clientProfile?.ClientName,
                    BillingType       = clientProfile?.BillingType ?? 0,
                    BillingModel      = clientProfile?.BillingModel ?? 0,
                    GSTApplicable     = clientProfile?.GSTApplicable ?? false,
                    GST               = clientProfile?.GST,
                    PerInterviewRate  = clientProfile?.PerInterviewRate ?? 0,
                    AIPerInterviewRate = clientProfile?.AIPerInterviewRate ?? 0,
                    PaymentTerm       = clientProfile?.PaymentTerm ?? 0,
                    ClientType        = clientProfile?.ClientType ?? 0,
                    CountryType       = clientProfile?.CountryType ?? 0,
                    BillingCurrency   = clientProfile?.BillingCurrency ?? "INR",
                    ActiveUserCount   = clientProfile?.ActiveUserCount ?? 0,
                    TotalUserCount    = clientProfile?.TotalUserCount ?? 0,
                    TotalCredits      = clientProfile?.TotalCredits ?? 0,
                    TotalDebits       = clientProfile?.TotalDebits ?? 0,
                    CashCreditAdded   = clientProfile?.CashCreditAdded ?? 0,
                    TotalPOAmount     = clientProfile?.TotalPOAmount ?? 0,
                    TotalPOReceived   = clientProfile?.TotalPOReceived ?? 0,
                    ClientPOs         = clientPOs,

                    MeetingsAsCandidate   = meetingsAsCandidate,
                    MeetingsAsInterviewer = meetingsAsInterviewer,
                    RecentPayments        = recentPayments,
                    TdsQuarters           = tdsQuarters,
                    TotalHistoryPaid      = historyTotal?.TotalHistoryPaid ?? 0
                };

                return result;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new EmailLookupResultVM { SearchEmail = emailId, IsFound = false };
            }
        }

        public async Task<List<OpsToolkitEmergencyUnassignVM>> GetEmergencyUnassignments(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@FromDate", fromDate.Date, DbType.Date);
                parameters.Add("@ToDate", toDate.Date, DbType.Date);

                using var con = new SqlConnection(connectionString);
                var rows = await con.QueryAsync<OpsToolkitEmergencyUnassignVM>(
                    "usp_OpsToolkit_GetEmergencyUnassignments",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120);
                return rows.ToList();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<OpsToolkitEmergencyUnassignVM>();
            }
        }

        public async Task<OpsToolkitInterviewHistoryVM> GetInterviewPointsHistory(int interviewId)
        {
            var result = new OpsToolkitInterviewHistoryVM { InterviewId = interviewId };
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@InterviewId", interviewId);

                using var con = new SqlConnection(connectionString);
                using var multi = await con.QueryMultipleAsync(
                    "usp_OpsToolkit_GetInterviewPointsHistory",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120);

                result.Points      = (await multi.ReadAsync<OpsToolkitPointsEntryVM>()).ToList();
                result.Assignments = (await multi.ReadAsync<OpsToolkitAssignmentChangeVM>()).ToList();
                result.Actions     = (await multi.ReadAsync<OpsToolkitActionLogVM>()).ToList();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return result;
        }

        // Scheduler links are built on HostDetails:baseUrl, e.g. https://portal.risebird.io/InterviewScheduler/ScheduleEvent?id=...
        private string SchedulerBaseUrl =>
            (_configuration["HostDetails:baseUrl"] ?? "https://portal.risebird.io").TrimEnd('/');

        public async Task<List<OpsToolkitIRFScheduleLinkVM>> GetIRFMissingScheduleLink()
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@BaseUrl", SchedulerBaseUrl);

                using var con = new SqlConnection(connectionString);
                var rows = await con.QueryAsync<OpsToolkitIRFScheduleLinkVM>(
                    "usp_OpsToolkit_GetIRFMissingScheduleLink",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120);
                return rows.ToList();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<OpsToolkitIRFScheduleLinkVM>();
            }
        }

        public async Task<OpsToolkitActionResultVM> AddIRFScheduleLink(int? irfIntId, string performedBy)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@IRFIntId", irfIntId);
                parameters.Add("@BaseUrl", SchedulerBaseUrl);
                parameters.Add("@PerformedBy", performedBy);

                using var con = new SqlConnection(connectionString);
                var result = await con.QueryFirstOrDefaultAsync<OpsToolkitActionResultVM>(
                    "usp_OpsToolkit_AddIRFScheduleLink",
                    parameters,
                    commandType: CommandType.StoredProcedure);
                return result ?? new OpsToolkitActionResultVM { Success = false, Message = "No response from database." };
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new OpsToolkitActionResultVM { Success = false, Message = "Could not add the scheduler link." };
            }
        }

        public async Task<OpsToolkitActionResultVM> RemoveUnassignPenalty(int interviewPointsId, string reason, string performedBy)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@InterviewPointsId", interviewPointsId);
                parameters.Add("@Reason", reason);
                parameters.Add("@PerformedBy", performedBy);

                using var con = new SqlConnection(connectionString);
                var result = await con.QueryFirstOrDefaultAsync<OpsToolkitActionResultVM>(
                    "usp_OpsToolkit_RemoveUnassignPenalty",
                    parameters,
                    commandType: CommandType.StoredProcedure);
                return result ?? new OpsToolkitActionResultVM { Success = false, Message = "No response from database." };
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new OpsToolkitActionResultVM { Success = false, Message = "Could not remove the penalty." };
            }
        }
    }
}
