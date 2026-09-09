using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.Entities.Data;
using Risebird.Repository.Interface;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using static System.Data.CommandType;

namespace Risebird.Repository
{
    /// <summary>
    /// Deliberately does NOT swallow exceptions (unlike most repositories in this codebase): the
    /// DesktopProctor API contract requires a real DB/network failure to surface as a 5xx (client
    /// retries with backoff), never as a 401/403/410 (client stops permanently) — see
    /// ProctoringApiController, whose outer try/catch turns any exception here into a 500.
    /// </summary>
    public class ProctoringApiRepository : IProctoringApiRepository
    {
        private readonly string _connectionString;

        public ProctoringApiRepository(IConfiguration configuration)
        {
            _connectionString = configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<ProctoringCandidateInterviewValidation> ValidateCandidateInterviewAsync(int interviewId, string candidateEmail)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@InterviewId", interviewId);
            parameters.Add("@CandidateEmail", candidateEmail);

            using var con = new SqlConnection(_connectionString);
            return await con.QuerySingleAsync<ProctoringCandidateInterviewValidation>(
                "usp_ValidateProctoringCandidateInterview", parameters, commandType: StoredProcedure);
        }

        public async Task<bool> RegisterSessionAsync(ProctoringApiSession session)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@SessionToken", session.SessionToken);
            parameters.Add("@InterviewId", session.InterviewId);
            parameters.Add("@CandidateEmail", session.CandidateEmail);
            parameters.Add("@BrowserSessionId", session.BrowserSessionId);
            parameters.Add("@Trust", session.Trust);
            parameters.Add("@ExpiresAtUtc", session.ExpiresAtUtc);

            using var con = new SqlConnection(_connectionString);
            await con.ExecuteAsync("usp_RegisterProctoringSession", parameters, commandType: StoredProcedure);
            return true;
        }

        public async Task<ProctoringApiSession> GetSessionByTokenAsync(string sessionToken)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@SessionToken", sessionToken);

            using var con = new SqlConnection(_connectionString);
            return await con.QueryFirstOrDefaultAsync<ProctoringApiSession>("usp_GetProctoringSessionByToken", parameters, commandType: StoredProcedure);
        }

        public async Task<bool> MarkSessionJoinedAsync(string sessionToken, string candidateEmail)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@SessionToken", sessionToken);
            parameters.Add("@Event", "joined");
            parameters.Add("@CandidateEmail", candidateEmail);

            using var con = new SqlConnection(_connectionString);
            await con.ExecuteAsync("usp_UpdateProctoringSessionLifecycle", parameters, commandType: StoredProcedure);
            return true;
        }

        public async Task<bool> MarkSessionStartedAsync(string sessionToken)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@SessionToken", sessionToken);
            parameters.Add("@Event", "started");

            using var con = new SqlConnection(_connectionString);
            await con.ExecuteAsync("usp_UpdateProctoringSessionLifecycle", parameters, commandType: StoredProcedure);
            return true;
        }

        public async Task<bool> UpdateHeartbeatAsync(string sessionToken, string clientState)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@SessionToken", sessionToken);
            parameters.Add("@ClientState", clientState);

            using var con = new SqlConnection(_connectionString);
            await con.ExecuteAsync("usp_UpdateProctoringSessionHeartbeat", parameters, commandType: StoredProcedure);
            return true;
        }

        public async Task<bool> MarkSessionEndedAsync(string sessionToken)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@SessionToken", sessionToken);
            parameters.Add("@Event", "ended");

            using var con = new SqlConnection(_connectionString);
            await con.ExecuteAsync("usp_UpdateProctoringSessionLifecycle", parameters, commandType: StoredProcedure);
            return true;
        }

        public async Task<bool> InsertLogIfNewAsync(ProctoringApiLog logEntry)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@SessionToken", logEntry.SessionToken);
            parameters.Add("@Sequence", logEntry.Sequence);
            parameters.Add("@Type", logEntry.Type);
            parameters.Add("@Category", logEntry.Category);
            parameters.Add("@Level", logEntry.Level);
            parameters.Add("@Confidence", logEntry.Confidence);
            parameters.Add("@OpenedAtUtc", logEntry.OpenedAtUtc);
            parameters.Add("@ResolvedAtUtc", logEntry.ResolvedAtUtc);
            parameters.Add("@RedactedDetail", logEntry.RedactedDetail);
            parameters.Add("@RawDetail", logEntry.RawDetail);

            using var con = new SqlConnection(_connectionString);
            return await con.QuerySingleAsync<bool>("usp_InsertProctoringLog", parameters, commandType: StoredProcedure);
        }

        public async Task<int> AddRiskScoreAsync(string sessionToken, int delta)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@SessionToken", sessionToken);
            parameters.Add("@Delta", delta);

            using var con = new SqlConnection(_connectionString);
            return await con.QuerySingleAsync<int>("usp_AddProctoringSessionRiskScore", parameters, commandType: StoredProcedure);
        }

        public async Task<IReadOnlyList<ProctoringApiLog>> GetLogsForCandidateAsync(string candidateEmail)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@CandidateEmail", candidateEmail);

            using var con = new SqlConnection(_connectionString);
            var rows = await con.QueryAsync<ProctoringApiLog>("usp_GetProctoringLogsForCandidate", parameters, commandType: StoredProcedure);
            return new List<ProctoringApiLog>(rows);
        }
    }
}
