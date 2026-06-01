using System;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Entities.ViewModel
{
    /// <summary>Validates AI-generated sample questions against rubric skill names (Group D item 8).</summary>
    public static class DmIvQuestionRubricHelper
    {
        public const string SkillNameInstruction =
            "You must use ONLY these exact skill names in the skill_or_competency field of each question. Do not invent new skill names, do not paraphrase, do not combine skills. If a question covers multiple rubric skills, pick the single best match from the list provided.";

        public static string BuildRubricPromptSection(IEnumerable<TransSkill> skills)
        {
            var lines = new List<string> { "Score Rubric (use these exact skill names only):", "" };
            foreach (var s in skills ?? Enumerable.Empty<TransSkill>())
            {
                if (string.IsNullOrWhiteSpace(s.SkillName)) continue;
                var level = string.IsNullOrWhiteSpace(s.LevelName) ? "N/A" : s.LevelName.Trim();
                lines.Add($"- Skill: {s.SkillName.Trim()} | Level: {level} | Weight: {s.Weightage}%");
            }
            lines.Add("");
            lines.Add(SkillNameInstruction);
            return string.Join("\n", lines);
        }

        public static HashSet<string> BuildValidSkillNameSet(IEnumerable<TransSkill> skills) =>
            new HashSet<string>(
                (skills ?? Enumerable.Empty<TransSkill>())
                    .Select(s => s.SkillName?.Trim())
                    .Where(s => !string.IsNullOrEmpty(s)),
                StringComparer.OrdinalIgnoreCase);

        public static bool TryMatchSkill(string returnedSkill, HashSet<string> validNames, out string matchedCanonical)
        {
            matchedCanonical = null;
            if (string.IsNullOrWhiteSpace(returnedSkill) || validNames == null || validNames.Count == 0)
                return false;

            var trimmed = returnedSkill.Trim();
            foreach (var name in validNames)
            {
                if (string.Equals(name, trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    matchedCanonical = name;
                    return true;
                }
            }
            return false;
        }
    }
}
