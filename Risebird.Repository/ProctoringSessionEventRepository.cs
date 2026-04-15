using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    public class ProctoringSessionEventRepository : IProctoringSessionEventRepository
    {
        private readonly string _connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        private const string SqlInsertWithIst = @"
INSERT INTO ProctoringSessionEvents
    (CreatedAtUtc, CreatedAtIst, InterviewId, CandidateEmail, EventKind, Level, Reason, MetadataJson, ScreenshotPayloadLength)
VALUES
    (@CreatedAtUtc, @CreatedAtIst, @InterviewId, @CandidateEmail, @EventKind, @Level, @Reason, @MetadataJson, @ScreenshotPayloadLength);
SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

        /// <summary>Used when <c>CreatedAtIst</c> column is not deployed yet.</summary>
        private const string SqlInsertLegacy = @"
INSERT INTO ProctoringSessionEvents
    (CreatedAtUtc, InterviewId, CandidateEmail, EventKind, Level, Reason, MetadataJson, ScreenshotPayloadLength)
VALUES
    (@CreatedAtUtc, @InterviewId, @CandidateEmail, @EventKind, @Level, @Reason, @MetadataJson, @ScreenshotPayloadLength);
SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

        public ProctoringSessionEventRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _connectionString = configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<long> InsertAsync(ProctoringSessionEvent row)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                await _applicationErrorLogger.LogMessage(
                    LogLevelInfo.Exception,
                    new InvalidOperationException("ProctoringSessionEvents: ConnectionString:DBConnectionString is missing."));
                return 0;
            }

            try
            {
                var utc = DateTime.UtcNow;
                row.CreatedAtUtc = utc;
                row.CreatedAtIst = ToIndiaWallClock(utc);

                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync().ConfigureAwait(false);
                try
                {
                    return await conn.ExecuteScalarAsync<long>(SqlInsertWithIst, row).ConfigureAwait(false);
                }
                catch (SqlException ex) when (ex.Number == 207 && ex.Message.IndexOf("CreatedAtIst", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return await conn.ExecuteScalarAsync<long>(SqlInsertLegacy, row).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        /// <summary>India Standard Time wall clock (Asia/Kolkata). Fallback: UTC+5:30 if OS tz data is unavailable.</summary>
        private static DateTime ToIndiaWallClock(DateTime utc)
        {
            utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            try
            {
                if (TimeZoneInfo.TryFindSystemTimeZoneById("Asia/Kolkata", out var kolkata))
                    return TimeZoneInfo.ConvertTimeFromUtc(utc, kolkata);
                if (TimeZoneInfo.TryFindSystemTimeZoneById("India Standard Time", out var ist))
                    return TimeZoneInfo.ConvertTimeFromUtc(utc, ist);
            }
            catch
            {
                /* use offset fallback */
            }

            return utc.Add(TimeSpan.FromHours(5) + TimeSpan.FromMinutes(30));
        }
    }
}
