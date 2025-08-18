using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.ViewModel
{
    public class InterviewMessageVM
    {
        public Guid SessionId { get; set; }
        public string Message { get; set; }
        public string AssistantId { get; set; }
    }

    public class AIReportVM
    {
        public Guid SessionId { get; set; }
        public string Message { get; set; }
    }
}
