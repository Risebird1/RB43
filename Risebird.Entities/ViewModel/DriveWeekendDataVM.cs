using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class DriveWeekendDataVM
    {
        public List<DriveIRFVM> Rows { get; set; } = new List<DriveIRFVM>();
        public List<DriveIRFVM> BelowThresholdRows { get; set; } = new List<DriveIRFVM>();
        public DateTime WeekendStart { get; set; }
        public DateTime WeekendEnd => WeekendStart.AddDays(1);
        public DriveWeekendLoadStats Stats { get; set; } = new DriveWeekendLoadStats();
    }
}
