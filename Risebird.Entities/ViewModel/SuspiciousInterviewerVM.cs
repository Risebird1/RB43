using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.ViewModel
{
    public class SuspiciousInterviewerVM
    {
        public string EmailId { get; set; }
        public string Comment { get; set; }
        public DateTime Createddate { get; set; }
        public string CreatedBy { get; set; }
        public bool SuspiciousStatus { get; set; }
    }
}
