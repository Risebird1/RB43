using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    public class TrialFlagReviewRepository : ITrialFlagReviewRepository
    {
        private readonly string _connectionString;
        private readonly IApplicationErrorLogger _logger;

        public TrialFlagReviewRepository(IConfiguration configuration, IApplicationErrorLogger logger)
        {
            _connectionString = configuration["ConnectionString:DBConnectionString"];
            _logger = logger;
        }

        public async Task<(bool TableExists, int TotalRows)> GetDiagnosticsAsync()
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var tableExists = await con.ExecuteScalarAsync<int>(
                    "SELECT CASE WHEN OBJECT_ID(N'dbo.TrialFlags', N'U') IS NOT NULL THEN 1 ELSE 0 END");
                if (tableExists == 0)
                    return (false, 0);

                var totalRows = await con.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.TrialFlags");
                return (true, totalRows);
            }
            catch (SqlException ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                throw;
            }
        }

        public async Task<IReadOnlyList<TrialFlagReviewRowVm>> GetTrialFlagsAsync(string reviewStatus)
        {
            using var con = new SqlConnection(_connectionString);
            var rows = await con.QueryAsync<TrialFlagReviewRowVm>(
                "usp_GetTrialFlagsForReview",
                new { ReviewStatus = string.IsNullOrWhiteSpace(reviewStatus) ? null : reviewStatus.Trim() },
                commandType: CommandType.StoredProcedure,
                commandTimeout: 60);
            return rows?.ToList() ?? new List<TrialFlagReviewRowVm>();
        }

        public async Task<TrialFlagReviewRowVm> GetTrialFlagByIdAsync(int trialFlagId)
        {
            var rows = await GetTrialFlagsAsync(null);
            return rows.FirstOrDefault(x => x.TrialFlagId == trialFlagId);
        }

        public async Task<bool> UpdateReviewAsync(int trialFlagId, string reviewStatus, string reviewedBy, string reviewerComment)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                return await con.QueryFirstOrDefaultAsync<bool>(
                    "usp_UpdateTrialFlagReview",
                    new
                    {
                        TrialFlagId = trialFlagId,
                        ReviewStatus = reviewStatus,
                        ReviewedBy = reviewedBy,
                        ReviewerComment = reviewerComment
                    },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30);
            }
            catch (SqlException ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
    }
}
