using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIInterview.Data.Models
{
    public class InterviewSession
    {
        public Guid SessionId { get; set; }
        public string ThreadId { get; set; }
        public string AssistantId { get; set; }
        public string CandidateEmail { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
