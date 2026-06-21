using System.Collections.Generic;
using Risebird.Entities.Enum;

namespace Risebird.Web.Helpers
{
    /// <summary>DM Approval — Synthesia AI question video gating (forward-compatible whitelist).</summary>
    public static class DmSynthesiaVideoViewMode
    {
        /// <summary>AI interview types that use Synthesia-generated preset question videos.</summary>
        public static readonly IReadOnlyCollection<int> SynthesiaAiInterviewTypeIds = new[]
        {
            (int)AIInterviewTypeEnum.Preset
        };

        public static bool UsesSynthesiaVideos(bool isAiInterview, int aiInterviewType)
        {
            if (!isAiInterview) return false;
            foreach (var typeId in SynthesiaAiInterviewTypeIds)
            {
                if (typeId == aiInterviewType) return true;
            }
            return false;
        }
    }
}
