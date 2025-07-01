using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data
{
    public class InterviewStartResult
    {
        public Guid SessionId { get; set; }
        public string ThreadId { get; set; }
        public List<string> Replies { get; set; }
    }
    public class AskResponse
    {
        public List<string> Replies { get; set; }
    }

    public class InterviewReportResult
    {
        public Guid SessionId { get; set; }
        public string ThreadId { get; set; }
        public List<string> Replies { get; set; }
    }
}
