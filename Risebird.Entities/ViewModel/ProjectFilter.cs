using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class ProjectFilter
    {
        public string[] ProjectID { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string[] ClientName { get; set; }
        public string[] Location { get; set; }
        public string[] ArchiveStatus { get; set; }
    }
}
