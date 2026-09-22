using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Enum
{
    /// <summary>
    /// The two scored sections of an AI Audio Adaptive HR screening interview.
    /// The sections are fixed; only the parameters inside each are generated per role.
    /// Values are stored on TransTechSkill.SkillSet.
    /// </summary>
    public enum AIScreeningSectionEnum
    {
        /// <summary>Can they do the job.</summary>
        RoleFitment = 1,

        /// <summary>Will they take it.</summary>
        MotivationToJoinFitment = 2
    }
}
