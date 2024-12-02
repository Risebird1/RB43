using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data.OpenAIModels
{
    public class RubricSchema
    {
        [Required]
        public List<RubricSkill> Rubric { get; set; } = new List<RubricSkill>();
    }
    public class RubricSkill
    {
        [Required]
        public string Skill { get; set; } // The name of the skill.

        [Required]
        public string ProficiencyLevel { get; set; } // The competency level of the skill, such as 'Expert', 'Proficient', 'Competent', or 'Advanced Beginner'.

        [Required]
        public int Weight { get; set; } // The score associated with the skill.

        public string KeyExpectations { get; set; } = ""; // Optional field to describe role-specific expectations related to the skill.
        public List<string> CoreConcepts { get; set; } // Optional field 
        public double EvaluationTime { get; set; } // Optional field
        public string Mandatory { get; set; } // Optional field
    }
}
