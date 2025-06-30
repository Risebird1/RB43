using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIInterview.Data.Models
{
    public class EvaluationConv
    {
        public int Id { get; set; }
        public Guid SessionId { get; set; }
        public string Evaluator { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
