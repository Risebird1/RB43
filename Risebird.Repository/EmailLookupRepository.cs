using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
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
    }
}
