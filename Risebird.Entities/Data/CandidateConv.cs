using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIInterview.Data.Models
{
    public class CandidateConv
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public Guid SessionId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
