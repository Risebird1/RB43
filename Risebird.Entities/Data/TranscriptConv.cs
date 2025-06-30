using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data
{
    public class TranscriptConv
    {
        public int Id { get; set; }
        public Guid SessionId { get; set; }
        public string Role { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
