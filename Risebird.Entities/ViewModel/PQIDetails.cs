using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.ViewModel
{
    public class PQIDetails
    {
        public int id { get; set; }
        public int UserId { get; set; }
        public string IRFId { get; set; }
        public string interviewer { get; set; }
        public string Client { get; set; }
        public int TotalInterview { get; set; }
        public int TotalCompletedMeetings { get; set; }
        public int TotalSelected { get; set; }
        public int selectionrate { get; set; }
        public int TotalRejected { get; set; }
        public int rejectionrate { get; set; }
        public string ChannelOwner { get; set; }
    }
}
