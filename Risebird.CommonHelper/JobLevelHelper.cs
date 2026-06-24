using System.Collections.Generic;
using System.Linq;
using Risebird.Entities.Enum;

namespace Risebird.CommonHelper
{
    /// <summary>Canonical experience-band options for Job Level dropdowns (Group D item 12).</summary>
    public static class JobLevelHelper
    {
        private static readonly JobLevels[] CanonicalBands =
        {
            JobLevels.Years_1_3,
            JobLevels.Years_3_6,
            JobLevels.Years_6_9,
            JobLevels.Years_9_12,
            JobLevels.Years_12_15,
            JobLevels.Years_15_Plus
        };

        public static IEnumerable<(int ID, string Name)> SelectListOptions() =>
            CanonicalBands.Select(s => ((int)s, s.GetDescription()));
    }
}
