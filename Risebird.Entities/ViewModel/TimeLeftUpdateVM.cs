using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.ViewModel
{
    public class TimeLeftUpdateVM
    {
        public Guid SessionId { get; set; }
        public int TimeLeftInSeconds { get; set; }
        public int interviewId { get; set; }
    }
}
