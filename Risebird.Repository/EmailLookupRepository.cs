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
                var candidateStats = (await multi.ReadAsync<EmailLookupInterviewStatsVM>()).FirstOrDefault() ?? new EmailLookupInterviewStatsVM();
                var interviewerStats = (await multi.ReadAsync<EmailLookupInterviewStatsVM>()).FirstOrDefault() ?? new EmailLookupInterviewStatsVM();
                var paymentStats = (await multi.ReadAsync<EmailLookupPaymentStatsVM>()).FirstOrDefault() ?? new EmailLookupPaymentStatsVM();
                var meetingsAsCandidate = (await multi.ReadAsync<EmailLookupMeetingVM>()).ToList();
                var meetingsAsInterviewer = (await multi.ReadAsync<EmailLookupMeetingVM>()).ToList();
                var recentPayments = (await multi.ReadAsync<EmailLookupPaymentVM>()).ToList();

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

                    MeetingsAsCandidate = meetingsAsCandidate,
                    MeetingsAsInterviewer = meetingsAsInterviewer,
                    RecentPayments = recentPayments
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
