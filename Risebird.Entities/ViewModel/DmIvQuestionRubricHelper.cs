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

        public const string QuestionGistInstruction =
            "For every question also return question_gist: 1 to 3 spoken sentences the AI interview avatar will read aloud to the candidate. No source code, no markdown, no bullet lists. Plain conversational English only.";

        /// <summary>
        /// Spoken text for the AI avatar. Prefers an AI-supplied gist, otherwise a code-stripped
        /// version of the question so generated rows still have something to display and narrate.
        /// </summary>
        public static string BuildSpokenQuestionGist(string question, string aiGist = null)
        {
            var fromAi = NormalizeSpokenText(aiGist);
            if (!string.IsNullOrEmpty(fromAi))
                return fromAi;

            var fromQuestion = NormalizeSpokenText(StripCodeForSpeech(question));
            if (!string.IsNullOrEmpty(fromQuestion))
                return fromQuestion;

            var fallback = NormalizeSpokenText(question);
            return string.IsNullOrEmpty(fallback)
                ? null
                : fallback;
        }

        private static string StripCodeForSpeech(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var stripped = System.Text.RegularExpressions.Regex.Replace(
                text, "```[\\s\\S]*?```", " ", System.Text.RegularExpressions.RegexOptions.Multiline);
            stripped = System.Text.RegularExpressions.Regex.Replace(stripped, "`[^`]+`", " ");
            return stripped;
        }

        private static string NormalizeSpokenText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var normalized = System.Text.RegularExpressions.Regex.Replace(text.Trim(), @"\s+", " ");
            if (normalized.Length > 1500)
                normalized = normalized.Substring(0, 1500).TrimEnd() + "…";
            return normalized;
        }
    }
}
