using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace Risebird.Entities.Enum
{
        public enum GraduationType
        {
            [Description("Graduation")]
            Graduation = 1,
            [Description("Post Graduation")]
            PostGraduation = 2
        }

        public enum CollegeType
        {
            [Description("Tier1")]
            TierOne = 1,
            [Description("Tier2")]
            TierTwo = 2,
            [Description("Tier3")]
            TierThree = 3
        }

        public enum DayTimeSlot
        {
            [Description("Morning")]
            Morning = 1,
            [Description("Evening")]
            Evening = 2,
            [Description("All")]
            All = 3
        }

        public enum DaySlot
        {
            [Description("Monday")]
            Monday = 1,
            [Description("Tuesday")]
            Tuesday = 2,
            [Description("Wednesday")]
            Wednesday = 3,
            [Description("Thursday")]
            Thursday = 4,
            [Description("Friday")]
            Friday = 5,
            [Description("Saturday")]
            Saturday = 6,
            [Description("Sunday")]
            Sunday = 7,
            [Description("All")]
            All = 8,

        }

}

