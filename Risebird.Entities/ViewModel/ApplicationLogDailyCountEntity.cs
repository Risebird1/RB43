using Risebird.Entities.Storage;
using System;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Pre-aggregated daily log counts for Operation → Logs index.
    /// PartitionKey = yyyy-MM-dd (UTC), RowKey = "counts".
    /// </summary>
    public class ApplicationLogDailyCountEntity : RbTableEntity
    {
        public const string TableName = "Risebirdlogdailycounts";
        public const string CountsRowKey = "counts";

        public ApplicationLogDailyCountEntity()
        {
        }

        public ApplicationLogDailyCountEntity(DateTime utcDate)
        {
            PartitionKey = utcDate.Date.ToString("yyyy-MM-dd");
            RowKey = CountsRowKey;
        }

        public int ExceptionCount { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public int InfoCount { get; set; }
        public int DebugCount { get; set; }
        public DateTime AggregatedAtUtc { get; set; }

        public DateTime GetUtcDate()
        {
            return DateTime.ParseExact(PartitionKey, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
