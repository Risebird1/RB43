using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class ApplicationLogDaySummaryVM
    {
        public DateTime LogDate { get; set; }
        public int WarningCount { get; set; }
        public int ErrorCount { get; set; }
        public int ExceptionCount { get; set; }
    }

    public class ApplicationLogGroupVM
    {
        public DateTimeOffset LatestTimestamp { get; set; }
        public int LogeLevel { get; set; }
        public string UserEmailId { get; set; }
        public string ClassName { get; set; }
        public string MethodName { get; set; }
        public int LineNumber { get; set; }
        public string LogMessage { get; set; }
        public string InnerException { get; set; }
        public int Count { get; set; }
    }

    public class ApplicationLogDetailsPageVM
    {
        public DateTime LogDate { get; set; }
        public int? LogLevel { get; set; }
        public IList<ApplicationLogGroupVM> Groups { get; set; } = new List<ApplicationLogGroupVM>();
        public Dictionary<int, int> LevelCounts { get; set; } = new Dictionary<int, int>();

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 100;
        public int TotalGroups { get; set; }
        public int TotalOccurrences { get; set; }
        public bool HasNext { get; set; }
        public bool HasPrevious => Page > 1;
    }

    public class AzureTablePageResult<T>
    {
        public IList<T> Items { get; set; } = new List<T>();
        public string ContinuationToken { get; set; }
        public bool HasMore => !string.IsNullOrEmpty(ContinuationToken);
    }
}
