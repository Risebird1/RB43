using Dapper;
using Microsoft.Extensions.Configuration;
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
    /// <summary>Deduction review requests and the QC queue. Errors propagate to DeductionReviewService, which logs them.</summary>
    public class DeductionReviewRepository : IDeductionReviewRepository
    {
        private const int CommandTimeoutSeconds = 90;

        private readonly string _connectionString;

        public DeductionReviewRepository(IConfiguration configuration)
        {
            _connectionString = configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<(int Result, int? RequestId)> SaveRequestAsync(string email, int interviewId, string reasonCategory, string note,
            DeductionReviewSettings settings, DateTime windowStart, DateTime dueAtUtc)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Email", email?.Trim(), DbType.AnsiString, size: 150);
            parameters.Add("@InterviewId", interviewId);
            parameters.Add("@ReasonCategory", reasonCategory, DbType.AnsiString, size: 30);
            parameters.Add("@Note", note, DbType.String, size: 500);
            parameters.Add("@MinPct", settings.MinDeductionPct);
            parameters.Add("@WindowStart", windowStart);
            parameters.Add("@MonthlyCap", settings.MonthlyCap);
            parameters.Add("@NoteMinLength", settings.NoteMinLength);
            parameters.Add("@DueAtUtc", dueAtUtc);

            using var con = new SqlConnection(_connectionString);
            var row = await con.QueryFirstOrDefaultAsync<SaveRow>(
                "dbo.usp_SaveDeductionReviewRequest", parameters,
                commandTimeout: CommandTimeoutSeconds, commandType: CommandType.StoredProcedure);
            return row == null ? (-7, null) : (row.Result, row.RequestId);
        }

        public async Task<IReadOnlyList<DeductionReviewExpiredRowVM>> ExpireAsync(bool returnUnnotified)
        {
            using var con = new SqlConnection(_connectionString);
            if (!returnUnnotified)
            {
                await con.ExecuteAsync("dbo.usp_ExpireDeductionReviewRequests", new { ReturnUnnotified = false },
                    commandTimeout: CommandTimeoutSeconds, commandType: CommandType.StoredProcedure);
                return Array.Empty<DeductionReviewExpiredRowVM>();
            }
            return (await con.QueryAsync<DeductionReviewExpiredRowVM>("dbo.usp_ExpireDeductionReviewRequests",
                new { ReturnUnnotified = true }, commandTimeout: CommandTimeoutSeconds,
                commandType: CommandType.StoredProcedure)).ToList();
        }

        public async Task MarkLeadNotifiedAsync(IEnumerable<int> ids)
        {
            var list = ids?.Where(i => i > 0).Distinct().ToList();
            if (list == null || list.Count == 0) return;
            using var con = new SqlConnection(_connectionString);
            await con.ExecuteAsync("dbo.usp_MarkDeductionReviewLeadNotified", new { Ids = string.Join(",", list) },
                commandTimeout: CommandTimeoutSeconds, commandType: CommandType.StoredProcedure);
        }

        public async Task<(IReadOnlyList<DeductionReviewQueueRowVM> Open, IReadOnlyList<DeductionReviewQueueRowVM> Decided, IReadOnlyList<DeductionReviewCheckStatVM> Checks)>
            GetQueueAsync(int recentDays)
        {
            using var con = new SqlConnection(_connectionString);
            using var multi = await con.QueryMultipleAsync("dbo.usp_GetDeductionReviewQueue", new { RecentDays = recentDays },
                commandTimeout: CommandTimeoutSeconds, commandType: CommandType.StoredProcedure);
            var open = (await multi.ReadAsync<DeductionReviewQueueRowVM>()).ToList();
            var decided = multi.IsConsumed ? new List<DeductionReviewQueueRowVM>() : (await multi.ReadAsync<DeductionReviewQueueRowVM>()).ToList();
            var checks = multi.IsConsumed ? new List<DeductionReviewCheckStatVM>() : (await multi.ReadAsync<DeductionReviewCheckStatVM>()).ToList();
            return (open, decided, checks);
        }

        public async Task<DeductionReviewDecideResultVM> DecideAsync(int requestId, string decision, int? newPct, string note, string qcEmail)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@RequestId", requestId);
            parameters.Add("@Decision", decision, DbType.AnsiString, size: 10);
            parameters.Add("@NewPct", newPct);
            parameters.Add("@DecisionNote", note, DbType.String, size: 500);
            parameters.Add("@QcEmail", qcEmail?.Trim(), DbType.AnsiString, size: 150);

            using var con = new SqlConnection(_connectionString);
            return await con.QueryFirstOrDefaultAsync<DeductionReviewDecideResultVM>(
                       "dbo.usp_DecideDeductionReviewRequest", parameters,
                       commandTimeout: CommandTimeoutSeconds, commandType: CommandType.StoredProcedure)
                   ?? new DeductionReviewDecideResultVM { Result = -4 };
        }

        public async Task SetCheckPolicyAsync(string aiCheck, bool exclude, string modifiedBy)
        {
            using var con = new SqlConnection(_connectionString);
            await con.ExecuteAsync("dbo.usp_SetDeductionReviewCheckPolicy",
                new { AiCheck = aiCheck, Exclude = exclude, ModifiedBy = modifiedBy },
                commandTimeout: CommandTimeoutSeconds, commandType: CommandType.StoredProcedure);
        }

        private sealed class SaveRow
        {
            public int Result { get; set; }
            public int? RequestId { get; set; }
        }
    }
}
