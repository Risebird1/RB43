using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class DriveWeekendDataVM
    {
        public List<DriveIRFVM> Rows { get; set; } = new List<DriveIRFVM>();
        /// <summary>At/above threshold but not yet in spotlight — shown as suggestions to add.</summary>
        public List<DriveIRFVM> SuggestedThresholdRows { get; set; } = new List<DriveIRFVM>();
        public List<DriveIRFVM> BelowThresholdRows { get; set; } = new List<DriveIRFVM>();
        public DateTime WeekendStart { get; set; }
        public DateTime WeekendEnd => WeekendStart.AddDays(1);
        public DriveWeekendLoadStats Stats { get; set; } = new DriveWeekendLoadStats();
        /// <summary>'This' | 'Next'</summary>
        public string WeekendBucket { get; set; } = "This";
        /// <summary>IRF int ids currently in WeekendDriveSpotlight for this bucket.</summary>
        public List<int> SelectedSpotlightIds { get; set; } = new List<int>();
        /// <summary>Options for the spotlight multiselect (weekend IRFs + manual skill channels).</summary>
        public List<DriveSpotlightOptionVM> SpotlightOptions { get; set; } = new List<DriveSpotlightOptionVM>();
    }

    public class DriveSpotlightOptionVM
    {
        public int IrfIntId { get; set; }
        public string IRFId { get; set; }
        public string Label { get; set; }
        public int Demand { get; set; }
        public bool BelowThreshold { get; set; }
    }
}
