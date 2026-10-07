using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    /// <summary>
    /// Coaching brief evidence and coaching sessions (Interviewer QC View). Errors propagate to
    /// InterviewerCoachingService, which logs them.
    /// </summary>
    public class InterviewerCoachingRepository : IInterviewerCoachingRepository
    {
        private const int CommandTimeoutSeconds = 120;

        private readonly string _connectionString;

        public InterviewerCoachingRepository(IConfiguration configuration)
        {
            _connectionString = configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<InterviewerExposureVM> GetExposureAsync(string interviewerEmail, DateTime fromIst, DateTime toIst)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@InterviewerEmail", interviewerEmail?.Trim(), DbType.AnsiString, size: 150);
            parameters.Add("@FromIst", fromIst.Date, DbType.Date);
            parameters.Add("@ToIst", toIst.Date, DbType.Date);

            using var con = new SqlConnection(_connectionString);
            using var multi = await con.QueryMultipleAsync(
                "dbo.usp_GetInterviewerExposure",
                parameters,
                commandTimeout: CommandTimeoutSeconds,
                commandType: CommandType.StoredProcedure);

            var result = new InterviewerExposureVM
            {
                Profile = (await multi.ReadAsync<ExposureProfileVM>()).FirstOrDefault()
            };
            if (!multi.IsConsumed) result.Risks = (await multi.ReadAsync<ExposureRiskRowVM>()).ToList();
            if (!multi.IsConsumed) result.Payments = (await multi.ReadAsync<ExposurePaymentRowVM>()).ToList();
            if (!multi.IsConsumed) result.Trials = (await multi.ReadAsync<ExposureTrialRowVM>()).ToList();
            if (!multi.IsConsumed) result.Standing = (await multi.ReadAsync<ExposureStandingRowVM>()).ToList();
            return result;
        }

        public async Task<CoachingSaveResultVM> SaveSessionAsync(string interviewerEmail, string coachEmail, string channel, string outcome,
            string themes, string agreed, string emailSubject, int? issueCount, DateTime? fromIst, DateTime? toIst,
            bool linkRecentCall)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@InterviewerEmail", interviewerEmail?.Trim(), DbType.AnsiString, size: 150);
            parameters.Add("@CoachEmail", coachEmail?.Trim(), DbType.AnsiString, size: 150);
            parameters.Add("@Channel", channel, DbType.AnsiString, size: 20);
            parameters.Add("@Outcome", outcome, DbType.AnsiString, size: 30);
            parameters.Add("@Themes", themes, DbType.String, size: 1000);
            parameters.Add("@Agreed", agreed, DbType.String, size: 2000);
            parameters.Add("@EmailSubject", emailSubject, DbType.String, size: 300);
            parameters.Add("@IssueCount", issueCount);
            parameters.Add("@FromIst", fromIst?.Date, DbType.Date);
            parameters.Add("@ToIst", toIst?.Date, DbType.Date);
            // Same app-server clock User/makecall stamps CallRecordingData.DateCreated with.
            parameters.Add("@NowServer", DateTime.Now);
            parameters.Add("@LinkRecentCall", linkRecentCall);
            parameters.Add("@EmailCooldownHours", CoachingConstants.EmailCooldownHours);

            using var con = new SqlConnection(_connectionString);
            var row = await con.QueryFirstOrDefaultAsync<CoachingSaveResultVM>(
                "dbo.usp_SaveInterviewerCoachingSession",
                parameters,
                commandTimeout: CommandTimeoutSeconds,
                commandType: CommandType.StoredProcedure);
            return row ?? new CoachingSaveResultVM { Result = -2 };
        }

        public async Task<bool> CancelEmailSessionAsync(int sessionId)
        {
            const string sql = @"
                DELETE FROM dbo.InterviewerCoachingSession
                WHERE Id = @Id
                  AND Channel = 'Email'
                  AND CreatedAtUtc >= DATEADD(MINUTE, -10, GETUTCDATE())";
            using var con = new SqlConnection(_connectionString);
            return await con.ExecuteAsync(sql, new { Id = sessionId }, commandTimeout: CommandTimeoutSeconds) > 0;
        }
    }
}
