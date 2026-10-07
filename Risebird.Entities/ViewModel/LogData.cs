using Risebird.Entities.Storage;

namespace Risebird.Entities.ViewModel
{
    public class LogData : RbTableEntity
    {
        public LogData(string PartitionKey, string RowKey)
        {
            this.PartitionKey = PartitionKey;
            this.RowKey = RowKey;
        }
        public LogData()
        {

        }
        public string UserEmailId { get; set; }
        public int LogeLevel { get; set; }
        public string LogMessage { get; set; }
        public string InnerException { get; set; }
        public string FileName { get; set; }
        public string MethodName { get; set; }
        public string ClassName { get; set; }
        public int LineNumber { get; set; }
        public int Status { get; set; } //0 for Unresolved, 1 Working, 2 for Resolved        
    }

    public enum LogStatus
    {
        Unresolved = 0,
        Working,
        Resolved
    }
}
