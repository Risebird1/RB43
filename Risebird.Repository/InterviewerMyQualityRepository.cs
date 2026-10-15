using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    /// <summary>
    /// The interviewer's own My Quality data. Errors propagate to InterviewerMyQualityService, which logs them.
    /// </summary>
    public class InterviewerMyQualityRepository : IInterviewerMyQualityRepository
    {
        private const int CommandTimeoutSeconds = 90;

        private readonly string _connectionString;

        public InterviewerMyQualityRepository(IConfiguration configuration)
        {
            _connectionString = configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<InterviewerMyQualityDataVM> GetAsync(string email)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Email", email?.Trim(), DbType.AnsiString, size: 150);

            using var con = new SqlConnection(_connectionString);
            // Close review requests QC did not answer in time first, so the page never shows a stale "Under review".
            await con.ExecuteAsync(
                "IF OBJECT_ID(N'dbo.usp_ExpireDeductionReviewRequests', N'P') IS NOT NULL EXEC dbo.usp_ExpireDeductionReviewRequests @ReturnUnnotified = 0",
                commandTimeout: CommandTimeoutSeconds);
            using var multi = await con.QueryMultipleAsync(
                "dbo.usp_GetInterviewerMyQuality",
                parameters,
                commandTimeout: CommandTimeoutSeconds,
                commandType: CommandType.StoredProcedure);

            var result = new InterviewerMyQualityDataVM
            {
                Tiles = (await multi.ReadAsync<MyQualityTilesRowVM>()).FirstOrDefault()
            };
            if (!multi.IsConsumed) result.Channels = (await multi.ReadAsync<MyQualityChannelRowVM>()).ToList();
            if (!multi.IsConsumed) result.Issues = (await multi.ReadAsync<ExposureRiskRowVM>()).ToList();
            if (!multi.IsConsumed) result.Deductions = (await multi.ReadAsync<MyQualityDeductionRowVM>()).ToList();
            if (!multi.IsConsumed) result.Coaching = (await multi.ReadAsync<MyQualityCoachingRowVM>()).ToList();
            if (!multi.IsConsumed) result.Wording = (await multi.ReadAsync<InterviewerFeedbackTextVM>()).ToList();
            return result;
        }

        public async Task<int> AcknowledgeCoachingAsync(int sessionId, string email)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@SessionId", sessionId);
            parameters.Add("@Email", email?.Trim(), DbType.AnsiString, size: 150);

            using var con = new SqlConnection(_connectionString);
            return await con.QueryFirstOrDefaultAsync<int?>(
                "dbo.usp_AcknowledgeInterviewerCoachingSession",
                parameters,
                commandTimeout: CommandTimeoutSeconds,
                commandType: CommandType.StoredProcedure) ?? -1;
        }
    }
}
