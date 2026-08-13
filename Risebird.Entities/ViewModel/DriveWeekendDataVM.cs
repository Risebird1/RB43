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

        /// <summary>D4/D6 — shared high-priority pins at the top of the volume section.</summary>
        public List<IRFAttentionRowVM> HighPrioritySpotlight { get; set; } = new List<IRFAttentionRowVM>();
        /// <summary>D3 — IRFs sorted by all-future scheduled volume (excludes active pins).</summary>
        public List<IRFAttentionRowVM> HighVolumeRows { get; set; } = new List<IRFAttentionRowVM>();
        public List<AssignmentVM> MagList { get; set; } = new List<AssignmentVM>();
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
