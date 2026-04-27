using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
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
                row.CreatedAtIst = IndiaTimeHelper.UtcToIndiaWallClock(utc);

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

        private const string SqlCount = @"
SELECT COUNT(1) FROM ProctoringSessionEvents WHERE InterviewId = @InterviewId;";

        private const string SqlList = @"
SELECT
    [Id], [CreatedAtUtc], [CreatedAtIst], [InterviewId], [CandidateEmail], [EventKind], [Level], [Reason], [MetadataJson], [ScreenshotPayloadLength]
FROM [dbo].[ProctoringSessionEvents]
WHERE [InterviewId] = @InterviewId
ORDER BY [CreatedAtUtc] ASC, [Id] ASC;";

        /// <summary>DBs deployed before <c>CreatedAtIst</c> was added: same as <see cref="SqlInsertLegacy"/> — do not list that column (invalid column 207 on primary query).</summary>
        private const string SqlListLegacy = @"
SELECT
    [Id], [CreatedAtUtc], [InterviewId], [CandidateEmail], [EventKind], [Level], [Reason], [MetadataJson], [ScreenshotPayloadLength]
FROM [dbo].[ProctoringSessionEvents]
WHERE [InterviewId] = @InterviewId
ORDER BY [CreatedAtUtc] ASC, [Id] ASC;";

        public async Task<int> GetCountByInterviewIdAsync(int interviewId)
        {
            if (interviewId <= 0 || string.IsNullOrWhiteSpace(_connectionString))
                return 0;
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync().ConfigureAwait(false);
                var c = await conn.QuerySingleAsync<int>(SqlCount, new { InterviewId = interviewId }).ConfigureAwait(false);
                return c;
            }
            catch
            {
                return 0;
            }
        }

        public async Task<IReadOnlyList<ProctoringSessionEvent>> GetByInterviewIdOrderedAsync(int interviewId)
        {
            if (interviewId <= 0 || string.IsNullOrWhiteSpace(_connectionString))
                return Array.Empty<ProctoringSessionEvent>();
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync().ConfigureAwait(false);
                try
                {
                    return await ReadEventRowsAsync(conn, SqlList, interviewId).ConfigureAwait(false);
                }
                catch (SqlException ex) when (ex.Number == 207 && ex.Message.IndexOf("CreatedAtIst", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Table exists without CreatedAtIst; inserts use SqlInsertLegacy, list must match
                    return await ReadEventRowsAsync(conn, SqlListLegacy, interviewId).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex).ConfigureAwait(false);
                return Array.Empty<ProctoringSessionEvent>();
            }
        }

        private static async Task<IReadOnlyList<ProctoringSessionEvent>> ReadEventRowsAsync(SqlConnection conn, string sql, int interviewId)
        {
            var list = new List<ProctoringSessionEvent>();
            // Explicit mapping: some environments fail to map [Reason] / [Level] with QueryAsync<ProctoringSessionEvent>
            foreach (var d in await conn.QueryAsync<dynamic>(sql, new { InterviewId = interviewId }).ConfigureAwait(false))
            {
                var r = d as System.Collections.Generic.IDictionary<string, object>
                    ?? throw new InvalidOperationException("Proctoring list row is not a readable key/value row.");

                var row = new ProctoringSessionEvent
                {
                    Id = AsLong(r, "Id"),
                    CreatedAtUtc = AsDateTime(r, "CreatedAtUtc"),
                    CreatedAtIst = AsNullableDateTime(r, "CreatedAtIst"),
                    InterviewId = (int)AsLong(r, "InterviewId"),
                    CandidateEmail = AsString(r, "CandidateEmail"),
                    EventKind = AsString(r, "EventKind") ?? string.Empty,
                    Level = AsString(r, "Level"),
                    Reason = AsString(r, "Reason"),
                    MetadataJson = AsString(r, "MetadataJson"),
                    ScreenshotPayloadLength = AsIntNullable(r, "ScreenshotPayloadLength"),
                };
                list.Add(row);
            }
            return list;
        }

        private static bool GetColumn(System.Collections.Generic.IDictionary<string, object> r, string k, out object o)
        {
            o = null;
            if (r == null) return false;
            if (r.TryGetValue(k, out o)) return true;
            return TryGetIgnoreCase(r, k, out o);
        }

        private static long AsLong(System.Collections.Generic.IDictionary<string, object> r, string k)
        {
            if (!GetColumn(r, k, out var o) || o == null || o is System.DBNull) return 0L;
            if (o is int i) return i;
            if (o is long l) return l;
            if (o is short s) return s;
            return System.Convert.ToInt64(o, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static bool TryGetIgnoreCase(System.Collections.Generic.IDictionary<string, object> r, string key, out object value)
        {
            foreach (var p in r)
            {
                if (string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    value = p.Value;
                    return true;
                }
            }
            value = null;
            return false;
        }

        private static string AsString(System.Collections.Generic.IDictionary<string, object> r, string k)
        {
            if (!GetColumn(r, k, out var o) || o == null || o is System.DBNull) return null;
            return o.ToString();
        }

        private static DateTime AsDateTime(System.Collections.Generic.IDictionary<string, object> r, string k)
        {
            if (!GetColumn(r, k, out var o) || o == null || o is System.DBNull) return default;
            if (o is DateTime d) return d;
            return System.Convert.ToDateTime(o, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static DateTime? AsNullableDateTime(System.Collections.Generic.IDictionary<string, object> r, string k)
        {
            if (!GetColumn(r, k, out var o) || o == null || o is System.DBNull) return null;
            if (o is DateTime d) return d;
            return System.Convert.ToDateTime(o, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static int? AsIntNullable(System.Collections.Generic.IDictionary<string, object> r, string k)
        {
            if (!GetColumn(r, k, out var o) || o == null || o is System.DBNull) return null;
            if (o is int i) return i;
            return System.Convert.ToInt32(o, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
