using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IApplicationLogDailyCountService
    {
        Task<IList<ApplicationLogDaySummaryVM>> GetSummariesAsync(DateTime startUtcDate, DateTime endUtcDate);
        Task<ApplicationLogDailyCountEntity> AggregateAndUpsertDayAsync(DateTime utcDate);
        Task<int> AggregateRangeAsync(DateTime startUtcDate, DateTime endUtcDate);
    }
}
