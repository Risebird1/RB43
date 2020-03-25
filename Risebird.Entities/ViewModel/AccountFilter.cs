using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class AccountFilter
    {
        public string[] AccountId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string[] ClientName { get; set; }
        public string[] Location { get; set; }
        public string[] ArchiveStatus { get; set; }
    }
}
