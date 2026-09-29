using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// Builds and reads pre-aggregated daily log-level counts for the Operation → Logs UI.
    /// </summary>
    public class ApplicationLogDailyCountService : IApplicationLogDailyCountService
    {
        private const string SourceLogTable = "Risebirdlogs";
        private static readonly IList<string> CountSelectColumns = new List<string> { "LogeLevel" };

        private readonly IAzureStorageHelper _azureStorageHelper;

        public ApplicationLogDailyCountService(IAzureStorageHelper azureStorageHelper)
        {
            _azureStorageHelper = azureStorageHelper;
        }

        public async Task<IList<ApplicationLogDaySummaryVM>> GetSummariesAsync(DateTime startUtcDate, DateTime endUtcDate)
        {
            var start = startUtcDate.Date;
            var end = endUtcDate.Date;
            if (start > end)
                start = end;

            var entities = await _azureStorageHelper.GetEntitiesByPartitionKeyRangeAsync<ApplicationLogDailyCountEntity>(
                ApplicationLogDailyCountEntity.TableName,
                start.ToString("yyyy-MM-dd"),
                end.ToString("yyyy-MM-dd"));

            var map = entities
                .Where(e => e.RowKey == ApplicationLogDailyCountEntity.CountsRowKey)
                .GroupBy(e => e.PartitionKey)
                .ToDictionary(g => g.Key, g => g.First());

            var result = new List<ApplicationLogDaySummaryVM>();
            for (var day = end; day >= start; day = day.AddDays(-1))
            {
                var key = day.ToString("yyyy-MM-dd");
                if (map.TryGetValue(key, out var entity))
                {
                    result.Add(new ApplicationLogDaySummaryVM
                    {
                        LogDate = day,
                        ExceptionCount = entity.ExceptionCount,
                        ErrorCount = entity.ErrorCount,
                        WarningCount = entity.WarningCount
                    });
                }
                else
                {
                    result.Add(new ApplicationLogDaySummaryVM { LogDate = day });
                }
            }

            return result;
        }

        public async Task<ApplicationLogDailyCountEntity> AggregateAndUpsertDayAsync(DateTime utcDate)
        {
            var day = utcDate.Date;
            var startUtc = DateTime.SpecifyKind(day, DateTimeKind.Utc);
            var endUtcExclusive = startUtc.AddDays(1);

            var exceptionTask = _azureStorageHelper.GetLogEntitiesByDateRangeAsync<LogLevelStampEntity>(
                SourceLogTable, startUtc, endUtcExclusive, (int)LogLevelInfo.Exception, CountSelectColumns);
            var errorTask = _azureStorageHelper.GetLogEntitiesByDateRangeAsync<LogLevelStampEntity>(
                SourceLogTable, startUtc, endUtcExclusive, (int)LogLevelInfo.Error, CountSelectColumns);
            var warningTask = _azureStorageHelper.GetLogEntitiesByDateRangeAsync<LogLevelStampEntity>(
                SourceLogTable, startUtc, endUtcExclusive, (int)LogLevelInfo.Warning, CountSelectColumns);

            await Task.WhenAll(exceptionTask, errorTask, warningTask);

            var entity = new ApplicationLogDailyCountEntity(day)
            {
                ExceptionCount = exceptionTask.Result.Count,
                ErrorCount = errorTask.Result.Count,
                WarningCount = warningTask.Result.Count,
                AggregatedAtUtc = DateTime.UtcNow,
                ETag = Azure.ETag.All
            };

            await _azureStorageHelper.InsertEntity(ApplicationLogDailyCountEntity.TableName, entity);
            return entity;
        }

        public async Task<int> AggregateRangeAsync(DateTime startUtcDate, DateTime endUtcDate)
        {
            var start = startUtcDate.Date;
            var end = endUtcDate.Date;
            if (start > end)
                start = end;

            var days = 0;
            for (var day = start; day <= end; day = day.AddDays(1))
            {
                await AggregateAndUpsertDayAsync(day);
                days++;
            }

            return days;
        }
    }
}
