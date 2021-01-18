using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class UnassignedInterviewsFilter
    {
        public string[] Status { get; set; }
       
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
