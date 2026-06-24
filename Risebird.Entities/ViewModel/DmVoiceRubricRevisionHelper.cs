using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Risebird.Entities.Data.OpenAIModels;
using Risebird.Entities.Enum;

namespace Risebird.Entities.ViewModel
{
    /// <summary>Voice-to-rubric revision prompt, parsing, diff, and user-facing messages for DM Approval.</summary>
    public static class DmVoiceRubricRevisionHelper
    {
        private static readonly string[] RubricInstructionKeywords =
        {
            "remove", "delete", "drop", "add", "include", "weight", "rebalance",
            "skill", "rubric", "expert", "proficient", "competent", "novice", "advanced",
            "elimination", "mandatory", "replace", "modify", "change", "swap", "increase",
            "decrease", "percent", "%", "level"
        };

        public sealed class RubricRevisionDiff
        {
            public List<string> Removed { get; set; } = new List<string>();
            public List<string> Added { get; set; } = new List<string>();
            public List<string> Modified { get; set; } = new List<string>();
            public List<string> MagWarnings { get; set; } = new List<string>();
            public bool HasChanges =>
                Removed.Count > 0 || Added.Count > 0 || Modified.Count > 0;
        }

        public sealed class VoiceRubricRevisionOutcome
        {
            public List<TransSkill> RevisedSkills { get; set; } = new List<TransSkill>();
            public RubricRevisionDiff Diff { get; set; } = new RubricRevisionDiff();
            public string Message { get; set; }
            public bool Changed { get; set; }
            public bool WeightsAdjusted { get; set; }
        }

        public sealed class WeightNormalizationResult
        {
            public List<TransSkill> Skills { get; set; } = new List<TransSkill>();
            public bool Adjusted { get; set; }
            public int TotalBefore { get; set; }
        }

        public static IEnumerable<TransSkill> GetScoreRubricSkills(IEnumerable<TransSkill> skills) =>
            (skills ?? Enumerable.Empty<TransSkill>())
                .Where(s => s != null && !string.IsNullOrWhiteSpace(s.SkillName))
                .Where(s => s.SkillSet == 0 || s.SkillSet == 1)
                .GroupBy(s => s.SkillName.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

        public static string BuildRevisionUserMessage(
            IRFVm irfVm,
            IEnumerable<TransSkill> currentSkills,
            AssignmentVM magDetails,
            string voiceTranscript)
        {
            var current = GetScoreRubricSkills(currentSkills).ToList();
            var currentJson = SerializeRubricForPrompt(current);
            var magJson = BuildMagJson(irfVm, magDetails);
            var schema = GetOutputSchemaDescription();
            var roleLabel = string.IsNullOrWhiteSpace(irfVm?.IRFId) ? "this role" : irfVm.IRFId.Trim();
            var transcript = (voiceTranscript ?? string.Empty).Trim();
            var rubricEmpty = !current.Any();

            var sb = new StringBuilder();
            sb.AppendLine("You are revising an existing score rubric for a hiring manager.");
            sb.AppendLine("Never generate a fresh rubric from scratch unless the current rubric JSON below is empty.");
            sb.AppendLine("Treat the voice instruction as modification commands (remove, add, rebalance, replace, change levels/weights/elimination flags) — not as raw skill names to append.");
            sb.AppendLine("If the instruction is ambiguous, prefer minimal change over speculative additions.");
            sb.AppendLine("New skills should align to the MAG competency framework when possible.");
            sb.AppendLine("Skill weights must sum to exactly 100 across the rubric.");
            sb.AppendLine("Return ONLY a JSON object in the schema below — no markdown fences, no commentary.");
            sb.AppendLine();
            sb.AppendLine($"Role / skill channel: {roleLabel}");
            sb.AppendLine();
            sb.AppendLine("Below is the CURRENT score rubric for this role:");
            sb.AppendLine(currentJson);
            sb.AppendLine();
            sb.AppendLine("The MAG (Mapped Assessment Guide) for this channel is:");
            sb.AppendLine(magJson);
            sb.AppendLine();
            sb.AppendLine("The hiring manager has given the following voice instruction to modify the rubric:");
            sb.AppendLine(transcript);
            sb.AppendLine();
            if (rubricEmpty)
            {
                sb.AppendLine("The current rubric is empty. Build an initial rubric from the MAG aligned to the voice instruction.");
            }
            else
            {
                sb.AppendLine("Based on the instruction, produce a REVISED rubric as JSON in the same schema as the current one.");
                sb.AppendLine("The revision may include:");
                sb.AppendLine("- Removing skills the instruction asks to remove");
                sb.AppendLine("- Adding new skills the instruction calls out");
                sb.AppendLine("- Modifying weights, proficiency levels, or elimination flags");
                sb.AppendLine("- Replacing skills with better-fitting ones from the MAG");
                sb.AppendLine("If the instruction is off-topic or does not specify any rubric change, return the current rubric unchanged.");
            }
            sb.AppendLine();
            sb.AppendLine("Return ONLY the revised rubric as JSON, in the schema:");
            sb.AppendLine(schema);
            return sb.ToString();
        }

        public static string ExtractJsonFromAssistantText(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson)) return null;
            rawJson = rawJson.Replace("```json", "", StringComparison.OrdinalIgnoreCase)
                .Replace("```", "")
                .Trim();
            var jsonStart = rawJson.IndexOf('{');
            var jsonEnd = rawJson.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
                rawJson = rawJson.Substring(jsonStart, jsonEnd + 1 - jsonStart);
            return rawJson;
        }

        public static RubricSchema DeserializeRubricSchema(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson)) return null;

            var model = JsonConvert.DeserializeObject<RubricSchema>(rawJson);
            if (model?.Rubric != null && model.Rubric.Count > 0)
                return model;

            try
            {
                var root = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(rawJson);
                var rubricToken = root?["Rubric"] ?? root?["rubric"];
                if (rubricToken == null) return model;

                var skills = rubricToken.ToObject<List<RubricSkill>>();
                if (skills == null || skills.Count == 0) return model;

                return new RubricSchema { Rubric = skills };
            }
            catch
            {
                return model;
            }
        }

        public static List<TransSkill> ToTransSkills(RubricSchema model, int irfId)
        {
            var techSkills = new List<TransSkill>();
            if (model?.Rubric == null) return techSkills;

            foreach (var skill in model.Rubric)
            {
                if (string.IsNullOrWhiteSpace(skill?.Skill)) continue;
                var techSkill = new TransSkill
                {
                    SkillName = skill.Skill.Trim(),
                    Weightage = skill.Weight > 0 ? skill.Weight : 10,
                    Concepts = skill.CoreConcepts != null ? string.Join(",", skill.CoreConcepts) : null,
                    Natureofquestion = skill.KeyExpectations,
                    Timelimit = skill.EvaluationTime > 0 ? Convert.ToInt16(skill.EvaluationTime) : (short)10,
                    isEliminationskill = skill.Mandatory != null &&
                        skill.Mandatory.Equals("yes", StringComparison.OrdinalIgnoreCase),
                    IRFId = irfId,
                    SkillSet = 1
                };
                if (System.Enum.TryParse(typeof(LevelTechEnum), skill.ProficiencyLevel, true, out var parsedLevel))
                    techSkill.LevelId = (int)parsedLevel;
                techSkills.Add(techSkill);
            }
            return techSkills;
        }

        public static HashSet<string> BuildMagSkillNameSet(AssignmentVM magDetails)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (magDetails == null) return names;
            AddMagTokens(names, magDetails.PrimarySkills);
            AddMagTokens(names, magDetails.TestedSkill);
            AddMagTokens(names, magDetails.AssessmentGroup);
            AddMagTokens(names, magDetails.MAGName);
            return names;
        }

        public static RubricRevisionDiff CompareRubrics(
            IEnumerable<TransSkill> before,
            IEnumerable<TransSkill> after,
            HashSet<string> magSkillNames)
        {
            var diff = new RubricRevisionDiff();
            var beforeMap = GetScoreRubricSkills(before)
                .ToDictionary(s => s.SkillName.Trim(), StringComparer.OrdinalIgnoreCase);
            var afterMap = GetScoreRubricSkills(after)
                .ToDictionary(s => s.SkillName.Trim(), StringComparer.OrdinalIgnoreCase);

            foreach (var name in beforeMap.Keys)
            {
                if (!afterMap.ContainsKey(name))
                    diff.Removed.Add(name);
            }

            foreach (var kvp in afterMap)
            {
                if (!beforeMap.ContainsKey(kvp.Key))
                {
                    diff.Added.Add(kvp.Key);
                    if (magSkillNames.Count > 0 && !MagContainsSkill(magSkillNames, kvp.Key))
                    {
                        diff.MagWarnings.Add(
                            $"Note: '{kvp.Key}' is not in the channel MAG — added anyway per your instruction.");
                    }
                    continue;
                }

                var prev = beforeMap[kvp.Key];
                var next = kvp.Value;
                var changes = DescribeSkillChanges(prev, next);
                if (!string.IsNullOrEmpty(changes))
                    diff.Modified.Add($"{kvp.Key}: {changes}");
            }

            foreach (var name in diff.Removed)
            {
                if (magSkillNames.Count > 0 && MagContainsSkill(magSkillNames, name))
                {
                    diff.MagWarnings.Add(
                        $"Note: '{name}' was removed; this skill is part of the MAG for this channel.");
                }
            }

            return diff;
        }

        public static WeightNormalizationResult NormalizeWeightageTo100(List<TransSkill> skills)
        {
            var list = GetScoreRubricSkills(skills).ToList();
            var result = new WeightNormalizationResult { Skills = list };
            if (!list.Any())
                return result;

            result.TotalBefore = list.Sum(s => Math.Max(0, s.Weightage));
            if (result.TotalBefore == 100)
                return result;

            if (result.TotalBefore <= 0)
            {
                DistributeEvenWeights(list);
                result.Adjusted = true;
                return result;
            }

            var scaled = list
                .Select(s => Math.Max(1, (int)Math.Round(s.Weightage * 100.0 / result.TotalBefore)))
                .ToList();
            FixWeightRounding(scaled);

            for (var i = 0; i < list.Count; i++)
                list[i].Weightage = scaled[i];

            result.Adjusted = true;
            return result;
        }

        public static object[] ToClientSkillPayload(IEnumerable<TransSkill> skills) =>
            GetScoreRubricSkills(skills)
                .Select(s => new
                {
                    skillName = s.SkillName,
                    levelId = s.LevelId,
                    levelName = ResolveLevelName(s),
                    weightage = s.Weightage,
                    timelimit = s.Timelimit > 0 ? s.Timelimit : 10,
                    isEliminationskill = s.isEliminationskill,
                    concepts = s.Concepts ?? string.Empty,
                    natureofquestion = s.Natureofquestion ?? string.Empty
                })
                .ToArray();

        public static VoiceRubricRevisionOutcome BuildOutcome(
            IEnumerable<TransSkill> currentSkills,
            List<TransSkill> revisedSkills,
            RubricRevisionDiff diff,
            string voiceTranscript,
            WeightNormalizationResult weightNormalization = null)
        {
            var current = GetScoreRubricSkills(currentSkills).ToList();
            var revised = GetScoreRubricSkills(revisedSkills).ToList();
            diff ??= new RubricRevisionDiff();

            var structurallyEqual = AreRubricsSemanticallyEqual(current, revised);
            var changed = diff.HasChanges || !structurallyEqual;

            if (changed && !diff.HasChanges)
                diff.Modified.Add("Rubric details updated from voice instruction.");

            var outcome = new VoiceRubricRevisionOutcome
            {
                RevisedSkills = revisedSkills ?? new List<TransSkill>(),
                Diff = diff,
                Changed = changed,
                WeightsAdjusted = weightNormalization?.Adjusted == true
            };

            if (!outcome.Changed)
            {
                outcome.Message = BuildNoChangeMessage(voiceTranscript);
                outcome.RevisedSkills = current;
                foreach (var s in outcome.RevisedSkills)
                {
                    s.IRFId = revisedSkills?.FirstOrDefault()?.IRFId ?? s.IRFId;
                    if (s.SkillSet == 0) s.SkillSet = 1;
                }
                return outcome;
            }

            outcome.Message = BuildChangedMessage(diff, outcome.WeightsAdjusted, weightNormalization?.TotalBefore ?? 0);
            return outcome;
        }

        public static bool AreRubricsSemanticallyEqual(
            IEnumerable<TransSkill> before,
            IEnumerable<TransSkill> after)
        {
            return string.Equals(
                BuildRubricFingerprint(before),
                BuildRubricFingerprint(after),
                StringComparison.Ordinal);
        }

        public static string BuildNoChangeMessage(string voiceTranscript)
        {
            var t = (voiceTranscript ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(t))
                return "No changes applied. Save a voice transcript first.";

            if (!LooksLikeRubricInstruction(t))
                return "No rubric-relevant instructions detected in the voice brief.";

            return "No changes were needed — the rubric already matches your voice instruction.";
        }

        public static string BuildChangedMessage(
            RubricRevisionDiff diff,
            bool weightsAdjusted = false,
            int totalWeightBefore = 0)
        {
            var parts = new List<string> { "Rubric revised." };

            foreach (var name in diff.Removed)
                parts.Add($"{name} removed.");

            foreach (var name in diff.Added)
                parts.Add($"'{name}' added.");

            foreach (var mod in diff.Modified)
                parts.Add(mod.EndsWith(".", StringComparison.Ordinal) ? mod : mod + ".");

            if (weightsAdjusted && totalWeightBefore != 100)
            {
                parts.Add(totalWeightBefore > 100
                    ? $"Weights totaled {totalWeightBefore}% and were adjusted to 100%."
                    : "Weights were adjusted to total 100%.");
            }

            if (diff.MagWarnings.Count > 0)
                parts.AddRange(diff.MagWarnings);

            parts.Add("Click Save score rubric to persist changes.");

            return string.Join(" ", parts);
        }

        public static bool LooksLikeRubricInstruction(string transcript)
        {
            var t = (transcript ?? string.Empty).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(t)) return false;
            return RubricInstructionKeywords.Any(k => t.Contains(k, StringComparison.Ordinal));
        }

        private static string SerializeRubricForPrompt(IEnumerable<TransSkill> skills)
        {
            var rubric = (skills ?? Enumerable.Empty<TransSkill>()).Select(s => new
            {
                skill = s.SkillName?.Trim(),
                proficiencyLevel = ResolveLevelName(s),
                weight = s.Weightage,
                keyExpectations = s.Natureofquestion ?? "",
                coreConcepts = SplitConcepts(s.Concepts),
                evaluationTime = s.Timelimit > 0 ? (double)s.Timelimit : 10d,
                mandatory = s.isEliminationskill ? "yes" : "no"
            }).ToList();

            return JsonConvert.SerializeObject(new { rubric }, Formatting.Indented);
        }

        private static string BuildMagJson(IRFVm irfVm, AssignmentVM magDetails)
        {
            if (magDetails != null)
            {
                return JsonConvert.SerializeObject(new
                {
                    assessmentGroup = magDetails.AssessmentGroup,
                    magName = magDetails.MAGName,
                    primarySkills = magDetails.PrimarySkills,
                    testedSkills = magDetails.TestedSkill,
                    overview = magDetails.Overview,
                    seniorityLevel = magDetails.SeniorityLevel ?? magDetails.Seniority,
                    yearsOfExperience = magDetails.YearsOfExperience ?? magDetails.YearsofExprience?.ToString(),
                    competencyLevel = magDetails.CompetencyLevel,
                    categoryName = magDetails.CategoryName
                }, Formatting.Indented);
            }

            return JsonConvert.SerializeObject(new
            {
                assessmentGroup = irfVm?.AssessmentGroupName,
                assessmentGroupId = irfVm?.AssessmentGroupId,
                note = "MAG details unavailable — use assessment group name as competency reference."
            }, Formatting.Indented);
        }

        private static string GetOutputSchemaDescription() =>
            @"{
  ""Rubric"": [
    {
      ""Skill"": ""string — skill name"",
      ""ProficiencyLevel"": ""Novice | Advanced | Competent | Proficient | Expert"",
      ""Weight"": 0,
      ""KeyExpectations"": ""string"",
      ""CoreConcepts"": [""string""],
      ""EvaluationTime"": 0,
      ""Mandatory"": ""yes | no""
    }
  ]
}";

        private static string ResolveLevelName(TransSkill skill)
        {
            if (!string.IsNullOrWhiteSpace(skill?.LevelName))
                return skill.LevelName.Trim();
            if (skill != null && skill.LevelId > 0 && System.Enum.IsDefined(typeof(LevelTechEnum), skill.LevelId))
                return System.Enum.GetName(typeof(LevelTechEnum), skill.LevelId);
            return "Competent";
        }

        private static List<string> SplitConcepts(string concepts)
        {
            if (string.IsNullOrWhiteSpace(concepts)) return new List<string>();
            return concepts.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim())
                .Where(c => c.Length > 0)
                .ToList();
        }

        private static void AddMagTokens(HashSet<string> names, string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;
            foreach (var token in raw.Split(new[] { ',', ';', '|', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var t = token.Trim();
                if (t.Length > 1) names.Add(t);
            }
        }

        private static bool MagContainsSkill(HashSet<string> magSkillNames, string skillName)
        {
            if (magSkillNames == null || magSkillNames.Count == 0 || string.IsNullOrWhiteSpace(skillName))
                return true;

            var target = skillName.Trim();
            if (magSkillNames.Contains(target)) return true;

            foreach (var mag in magSkillNames)
            {
                if (target.Contains(mag, StringComparison.OrdinalIgnoreCase) ||
                    mag.Contains(target, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static void DistributeEvenWeights(List<TransSkill> skills)
        {
            if (skills == null || skills.Count == 0) return;
            var baseWeight = 100 / skills.Count;
            var remainder = 100 - (baseWeight * skills.Count);
            for (var i = 0; i < skills.Count; i++)
                skills[i].Weightage = baseWeight + (i < remainder ? 1 : 0);
        }

        private static void FixWeightRounding(List<int> weights)
        {
            if (weights == null || weights.Count == 0) return;
            var delta = 100 - weights.Sum();
            if (delta == 0) return;

            var index = 0;
            for (var i = 1; i < weights.Count; i++)
            {
                if (weights[i] > weights[index])
                    index = i;
            }

            weights[index] = Math.Max(1, weights[index] + delta);
            if (weights.Sum() != 100 && weights.Count > 1)
            {
                var secondPass = 100 - weights.Sum();
                weights[0] = Math.Max(1, weights[0] + secondPass);
            }
        }

        private static string DescribeSkillChanges(TransSkill prev, TransSkill next)
        {
            var parts = new List<string>();
            var prevLevel = ResolveLevelKey(prev);
            var nextLevel = ResolveLevelKey(next);
            if (!string.Equals(prevLevel, nextLevel, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(prevLevel))
                    parts.Add($"level set to {nextLevel}");
                else if (string.IsNullOrEmpty(nextLevel))
                    parts.Add($"level cleared from {prevLevel}");
                else
                    parts.Add($"level {prevLevel} → {nextLevel}");
            }

            if (prev.Weightage != next.Weightage)
                parts.Add($"weight {prev.Weightage}% → {next.Weightage}%");
            if (prev.isEliminationskill != next.isEliminationskill)
                parts.Add(next.isEliminationskill ? "marked elimination" : "elimination removed");
            if (EffectiveTimelimit(prev) != EffectiveTimelimit(next))
                parts.Add($"time {EffectiveTimelimit(prev)}m → {EffectiveTimelimit(next)}m");
            if (!string.Equals(NormalizeConcepts(prev.Concepts), NormalizeConcepts(next.Concepts), StringComparison.Ordinal))
                parts.Add("concepts updated");
            if (!string.Equals(NormalizeFreeText(prev.Natureofquestion), NormalizeFreeText(next.Natureofquestion), StringComparison.Ordinal))
                parts.Add("expectations updated");
            return string.Join(", ", parts);
        }

        private static string ResolveLevelKey(TransSkill skill)
        {
            if (skill != null && skill.LevelId > 0 && System.Enum.IsDefined(typeof(LevelTechEnum), skill.LevelId))
                return System.Enum.GetName(typeof(LevelTechEnum), skill.LevelId);
            if (!string.IsNullOrWhiteSpace(skill?.LevelName))
                return skill.LevelName.Trim();
            return string.Empty;
        }

        private static int EffectiveTimelimit(TransSkill skill) =>
            skill != null && skill.Timelimit > 0 ? skill.Timelimit : 10;

        private static string NormalizeConcepts(string concepts)
        {
            if (string.IsNullOrWhiteSpace(concepts)) return string.Empty;
            return string.Join("|", SplitConcepts(concepts)
                .Select(c => c.ToLowerInvariant())
                .OrderBy(c => c, StringComparer.Ordinal));
        }

        private static string NormalizeFreeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            return string.Join(" ", text.Trim()
                    .Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
                .ToLowerInvariant();
        }

        private static string BuildRubricFingerprint(IEnumerable<TransSkill> skills)
        {
            var payload = GetScoreRubricSkills(skills)
                .OrderBy(s => s.SkillName?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .Select(s => new
                {
                    name = (s.SkillName ?? string.Empty).Trim().ToLowerInvariant(),
                    level = ResolveLevelKey(s).ToLowerInvariant(),
                    weight = s.Weightage,
                    elim = s.isEliminationskill,
                    time = EffectiveTimelimit(s),
                    concepts = NormalizeConcepts(s.Concepts),
                    expectations = NormalizeFreeText(s.Natureofquestion)
                });

            return JsonConvert.SerializeObject(payload);
        }
    }
}
