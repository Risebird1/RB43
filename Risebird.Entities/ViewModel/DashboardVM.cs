using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class DashboardVM
    {
        public IEnumerable<Meeting> Meetings { get; set; }
    }
}
