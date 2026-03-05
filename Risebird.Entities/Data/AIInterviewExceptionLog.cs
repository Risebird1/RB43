using System;

namespace Risebird.Entities.Data
{
    public class AIInterviewExceptionLog
    {
        public int Id { get; set; }
        public string SessionId { get; set; }
        public string HttpMethod { get; set; }
        public string ExceptionMessage { get; set; }
        public string InnerExceptionMessage { get; set; }
        public string StackTrace { get; set; }
        public int LogLevel { get; set; }
        public DateTime OccurredAt { get; set; }
        public bool IsResolved { get; set; }
    }
}
