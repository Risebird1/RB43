using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Risebird.Entities.Data.OpenAIModels;
using Risebird.Entities.Enum;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Prompts, parsing, per-section weighting and scoring for the AI Audio Adaptive HR screening
    /// intake.
    ///
    /// The interview always has exactly two scored sections — Role Fitment and Motivation to Join
    /// Fitment. The sections are fixed; the parameters inside each are generated per role from the
    /// JD, the recruiter's instruction and the ideal-candidate profile.
    ///
    /// The diff, weight normalization and off-topic handling are deliberately delegated to
    /// <see cref="DmVoiceRubricRevisionHelper"/> rather than reimplemented — this type only adds
    /// the section dimension on top. Those helpers filter to SkillSet 0/1, so a section's rows are
    /// presented to them as SkillSet 1 and restored afterwards (see <see cref="WithSectionMasked"/>).
    /// </summary>
    public static class AIScreeningIntakeHelper
    {
        public const int SectionWeightTotal = 100;
        public const int MaxParameterScore = 5;
        public const int DefaultDurationMinutes = 20;

        public static string GetSectionTitle(AIScreeningSectionEnum section) =>
            section == AIScreeningSectionEnum.RoleFitment
                ? "Role Fitment"
                : "Motivation to Join Fitment";

        private static string GetSectionIntent(AIScreeningSectionEnum section) =>
            section == AIScreeningSectionEnum.RoleFitment
                ? "whether the candidate can do the job"
                : "whether the candidate will actually take the job if offered";

        #region Prompts

        /// <summary>
        /// Proposes an ideal-candidate profile from the job description and the recruiter's
        /// instruction. The result is one line of free text the recruiter reads and corrects.
        /// </summary>
        public static string BuildIdealCandidatePrompt(string jobTitle, string jdText, string instruction)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are helping a recruiter set up a telephone HR screening interview.");
            sb.AppendLine("From the job description and the recruiter's instruction, describe the ideal candidate.");
            sb.AppendLine();
            sb.AppendLine("Cover only what the sources support, in this order, separated by ' | ':");
            sb.AppendLine("core skills, years of experience, domain exposure, employment stability expectation,");
            sb.AppendLine("current and expected compensation, notice period, location and work mode.");
            sb.AppendLine("Omit anything the sources do not support. Do not invent figures.");
            sb.AppendLine("Where the recruiter's instruction and the job description disagree, the instruction wins.");
            sb.AppendLine();
            sb.AppendLine("Return ONLY a JSON object — no markdown fences, no commentary:");
            sb.AppendLine(@"{ ""idealCandidateProfile"": ""string — one line, compact, using ' | ' between facts"" }");
            sb.AppendLine();
            sb.AppendLine($"Job title: {Blank(jobTitle, "not stated")}");
            sb.AppendLine();
            sb.AppendLine("Job description:");
            sb.AppendLine(Blank(jdText, "(none supplied)"));
            sb.AppendLine();
            sb.AppendLine("Recruiter's instruction:");
            sb.AppendLine(Blank(instruction, "(none supplied)"));
            return sb.ToString();
        }

        /// <summary>
        /// Drafts both sections in one call. Weights must total 100 within each section
        /// independently — not 100 across both.
        /// </summary>
        public static string BuildPlanPrompt(
            string jobTitle,
            string jdText,
            string instruction,
            string idealCandidateProfile,
            int durationMinutes)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are drafting the scoring rubric for a telephone HR screening interview.");
            sb.AppendLine();
            sb.AppendLine("The interview has EXACTLY TWO scored sections. The sections are fixed — never rename,");
            sb.AppendLine("merge, drop or add a section:");
            sb.AppendLine($"  1. Role Fitment — {GetSectionIntent(AIScreeningSectionEnum.RoleFitment)}.");
            sb.AppendLine($"  2. Motivation to Join Fitment — {GetSectionIntent(AIScreeningSectionEnum.MotivationToJoinFitment)}.");
            sb.AppendLine();
            sb.AppendLine("Rules:");
            sb.AppendLine("- Generate 4 to 6 parameters per section, drawn from the job description, the recruiter's");
            sb.AppendLine("  instruction and the ideal candidate profile.");
            sb.AppendLine($"- Within EACH section the weights must total exactly {SectionWeightTotal}.");
            sb.AppendLine("  The two sections are weighted independently — do NOT make the two sections total 100 together.");
            sb.AppendLine("- Compensation, notice period, location, work mode and reason for change belong to");
            sb.AppendLine("  Motivation to Join Fitment. Skills, experience, domain and stability belong to Role Fitment.");
            sb.AppendLine("- Mark a parameter mandatory only when the instruction makes it a hard filter");
            sb.AppendLine("  (for example a budget ceiling or a required location).");
            sb.AppendLine("- Name each parameter as the thing being judged, not as a question.");
            sb.AppendLine($"- The whole interview should fit {durationMinutes} minutes; EvaluationTime is minutes per parameter.");
            sb.AppendLine();
            sb.AppendLine("Return ONLY a JSON object in this schema — no markdown fences, no commentary:");
            sb.AppendLine(GetPlanSchemaDescription());
            sb.AppendLine();
            sb.AppendLine($"Job title: {Blank(jobTitle, "not stated")}");
            sb.AppendLine($"Interview duration (minutes): {durationMinutes}");
            sb.AppendLine();
            sb.AppendLine("Job description:");
            sb.AppendLine(Blank(jdText, "(none supplied)"));
            sb.AppendLine();
            sb.AppendLine("Recruiter's instruction:");
            sb.AppendLine(Blank(instruction, "(none supplied)"));
            sb.AppendLine();
            sb.AppendLine("Confirmed ideal candidate profile:");
            sb.AppendLine(Blank(idealCandidateProfile, "(none supplied)"));
            return sb.ToString();
        }

        /// <summary>
        /// Revises an already-drafted plan from a spoken or typed instruction. When the instruction
        /// does not specify a rubric change the model is told to return the plan unchanged — that is
        /// a valid response and is surfaced as a message, not an error.
        /// </summary>
        public static string BuildPlanRevisionPrompt(
            AIScreeningPlan currentPlan,
            string transcript,
            string idealCandidateProfile)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are revising an existing two-section screening rubric for a recruiter.");
            sb.AppendLine("Never generate a fresh rubric from scratch.");
            sb.AppendLine("Treat the instruction as modification commands (remove, add, rebalance, change levels,");
            sb.AppendLine("weights or mandatory flags) — not as raw parameter names to append.");
            sb.AppendLine("If the instruction is ambiguous, prefer minimal change over speculative additions.");
            sb.AppendLine();
            sb.AppendLine("The two sections are fixed — never rename, merge, drop or add a section.");
            sb.AppendLine($"Within EACH section the weights must total exactly {SectionWeightTotal}, independently of the other.");
            sb.AppendLine("When the instruction moves weight away from a parameter, give that weight to the other");
            sb.AppendLine("parameters in the SAME section.");
            sb.AppendLine();
            sb.AppendLine("If the instruction is off-topic, or does not specify any rubric change, return the current");
            sb.AppendLine("rubric completely unchanged. That is a valid answer.");
            sb.AppendLine();
            sb.AppendLine("Return ONLY a JSON object in this schema — no markdown fences, no commentary:");
            sb.AppendLine(GetPlanSchemaDescription());
            sb.AppendLine();
            sb.AppendLine("Confirmed ideal candidate profile:");
            sb.AppendLine(Blank(idealCandidateProfile, "(none supplied)"));
            sb.AppendLine();
            sb.AppendLine("The CURRENT rubric is:");
            sb.AppendLine(SerializePlanForPrompt(currentPlan));
            sb.AppendLine();
            sb.AppendLine("The recruiter's instruction to modify it:");
            sb.AppendLine(Blank(transcript, "(none supplied)"));
            return sb.ToString();
        }

        private static string GetPlanSchemaDescription() =>
            @"{
  ""roleLabel"": ""string — role and location, e.g. 'Senior Backend Engineer — Bangalore'"",
  ""durationMinutes"": 0,
  ""sections"": [
    {
      ""section"": ""RoleFitment"",
      ""parameters"": [
        {
          ""Skill"": ""string — the parameter being judged"",
          ""ProficiencyLevel"": ""Novice | Advanced | Competent | Proficient | Expert"",
          ""Weight"": 0,
          ""KeyExpectations"": ""string"",
          ""CoreConcepts"": [""string""],
          ""EvaluationTime"": 0,
          ""Mandatory"": ""yes | no""
        }
      ]
    },
    {
      ""section"": ""MotivationToJoinFitment"",
      ""parameters"": [ ]
    }
  ]
}";

        private static string SerializePlanForPrompt(AIScreeningPlan plan)
        {
            var payload = new
            {
                roleLabel = plan?.RoleLabel,
                durationMinutes = plan?.DurationMinutes ?? DefaultDurationMinutes,
                sections = (plan?.Sections ?? new List<AIScreeningPlanSection>()).Select(s => new
                {
                    section = s.Section.ToString(),
                    parameters = (s.Parameters ?? new List<TransSkill>()).Select(p => new
                    {
                        Skill = p.SkillName,
                        ProficiencyLevel = ResolveLevelName(p),
                        Weight = p.Weightage,
                        KeyExpectations = p.Natureofquestion ?? string.Empty,
                        CoreConcepts = SplitConcepts(p.Concepts),
                        EvaluationTime = p.Timelimit > 0 ? (double)p.Timelimit : 3d,
                        Mandatory = p.isEliminationskill ? "yes" : "no"
                    })
                })
            };
            return JsonConvert.SerializeObject(payload, Formatting.Indented);
        }

        #endregion

        #region Parsing

        /// <summary>Reads the ideal-candidate profile out of the assistant's JSON response.</summary>
        public static string ParseIdealCandidateProfile(string rawText)
        {
            var json = DmVoiceRubricRevisionHelper.ExtractJsonFromAssistantText(rawText);
            if (string.IsNullOrWhiteSpace(json)) return null;

            try
            {
                var root = JsonConvert.DeserializeObject<JObject>(json);
                var value = (string)(root?["idealCandidateProfile"] ?? root?["IdealCandidateProfile"]
                            ?? root?["profile"] ?? root?["Profile"]);
                return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Reads a two-section plan out of the assistant's JSON response. Both sections are always
        /// present in the result, empty if the model omitted one.
        /// </summary>
        public static AIScreeningPlan ParsePlan(string rawText, int fallbackDurationMinutes = DefaultDurationMinutes)
        {
            var json = DmVoiceRubricRevisionHelper.ExtractJsonFromAssistantText(rawText);
            if (string.IsNullOrWhiteSpace(json)) return null;

            JObject root;
            try
            {
                root = JsonConvert.DeserializeObject<JObject>(json);
            }
            catch
            {
                return null;
            }
            if (root == null) return null;

            var plan = new AIScreeningPlan
            {
                RoleLabel = (string)(root["roleLabel"] ?? root["RoleLabel"]),
                DurationMinutes = ReadInt(root["durationMinutes"] ?? root["DurationMinutes"], fallbackDurationMinutes)
            };

            var sectionsToken = root["sections"] ?? root["Sections"];
            foreach (AIScreeningSectionEnum section in System.Enum.GetValues(typeof(AIScreeningSectionEnum)))
            {
                var token = FindSectionToken(sectionsToken, section);
                plan.Sections.Add(new AIScreeningPlanSection
                {
                    Section = section,
                    Title = GetSectionTitle(section),
                    Parameters = ReadParameters(token, section)
                });
            }

            if (plan.DurationMinutes <= 0) plan.DurationMinutes = fallbackDurationMinutes;
            return plan;
        }

        private static JToken FindSectionToken(JToken sectionsToken, AIScreeningSectionEnum section)
        {
            if (sectionsToken == null) return null;

            // Array form: [{ "section": "RoleFitment", "parameters": [...] }, ...]
            if (sectionsToken is JArray array)
            {
                foreach (var entry in array)
                {
                    var name = (string)(entry["section"] ?? entry["Section"] ?? entry["name"] ?? entry["Title"]);
                    if (MatchesSection(name, section))
                        return entry["parameters"] ?? entry["Parameters"] ?? entry["rubric"] ?? entry["Rubric"];
                }
                return null;
            }

            // Object form: { "RoleFitment": [...], "MotivationToJoinFitment": [...] }
            if (sectionsToken is JObject obj)
            {
                foreach (var prop in obj.Properties())
                {
                    if (MatchesSection(prop.Name, section))
                        return prop.Value is JArray ? prop.Value : (prop.Value["parameters"] ?? prop.Value["Parameters"]);
                }
            }

            return null;
        }

        private static bool MatchesSection(string name, AIScreeningSectionEnum section)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            var key = new string(name.Where(char.IsLetterOrDigit).ToArray());

            if (section == AIScreeningSectionEnum.RoleFitment)
                return key.StartsWith("Role", StringComparison.OrdinalIgnoreCase) || key == "1";

            return key.StartsWith("Motivation", StringComparison.OrdinalIgnoreCase) || key == "2";
        }

        private static List<TransSkill> ReadParameters(JToken token, AIScreeningSectionEnum section)
        {
            var parameters = new List<TransSkill>();
            if (token is not JArray array) return parameters;

            List<RubricSkill> parsed;
            try
            {
                parsed = array.ToObject<List<RubricSkill>>();
            }
            catch
            {
                return parameters;
            }
            if (parsed == null) return parameters;

            // ToTransSkills owns the RubricSkill -> TransSkill mapping (level parsing, defaults,
            // concepts joining). Only the section stamp is added here.
            var mapped = DmVoiceRubricRevisionHelper.ToTransSkills(new RubricSchema { Rubric = parsed }, 0);
            foreach (var skill in mapped)
            {
                skill.SkillSet = (int)section;
                parameters.Add(skill);
            }
            return parameters;
        }

        private static int ReadInt(JToken token, int fallback)
        {
            if (token == null) return fallback;
            return int.TryParse(token.ToString(), out var value) && value > 0 ? value : fallback;
        }

        #endregion

        #region Weights and diffing

        /// <summary>
        /// Normalizes each section's weights to 100 independently. Returns true when any section
        /// was adjusted, so the UI can say so.
        /// </summary>
        public static bool NormalizeSectionWeights(AIScreeningPlan plan)
        {
            if (plan?.Sections == null) return false;

            var adjusted = false;
            foreach (var section in plan.Sections)
            {
                if (section?.Parameters == null || section.Parameters.Count == 0) continue;

                var result = WithSectionMasked(section, () =>
                    DmVoiceRubricRevisionHelper.NormalizeWeightageTo100(section.Parameters));

                if (result.Adjusted) adjusted = true;
            }
            return adjusted;
        }

        /// <summary>
        /// Diffs two plans section by section, reusing the existing rubric differ so the intake and
        /// the DHI screen describe a change the same way.
        /// </summary>
        public static AIScreeningPlanRevision BuildRevision(
            AIScreeningPlan before,
            AIScreeningPlan after,
            string transcript,
            bool weightsAdjusted)
        {
            var revision = new AIScreeningPlanRevision
            {
                Plan = after,
                WeightsAdjusted = weightsAdjusted
            };

            foreach (AIScreeningSectionEnum section in System.Enum.GetValues(typeof(AIScreeningSectionEnum)))
            {
                var beforeSection = before?.GetSection(section);
                var afterSection = after?.GetSection(section);
                var title = GetSectionTitle(section);

                var diff = WithSectionMasked(beforeSection, afterSection, () =>
                    DmVoiceRubricRevisionHelper.CompareRubrics(
                        beforeSection?.Parameters ?? new List<TransSkill>(),
                        afterSection?.Parameters ?? new List<TransSkill>(),
                        new HashSet<string>(StringComparer.OrdinalIgnoreCase)));

                revision.Added.AddRange(diff.Added.Select(x => $"{title}: {x}"));
                revision.Removed.AddRange(diff.Removed.Select(x => $"{title}: {x}"));
                revision.Modified.AddRange(diff.Modified.Select(x => $"{title}: {x}"));

                if (afterSection != null && !afterSection.IsBalanced && afterSection.Parameters.Count > 0)
                {
                    revision.Warnings.Add(
                        $"{title} weights total {afterSection.WeightTotal}, not {SectionWeightTotal}.");
                }
            }

            revision.Changed = revision.Added.Count > 0 || revision.Removed.Count > 0 || revision.Modified.Count > 0;

            revision.Message = revision.Changed
                ? BuildChangedMessage(revision)
                : DmVoiceRubricRevisionHelper.BuildNoChangeMessage(transcript);

            if (after != null) after.Message = revision.Message;
            return revision;
        }

        private static string BuildChangedMessage(AIScreeningPlanRevision revision)
        {
            var parts = new List<string> { "Plan revised." };
            parts.AddRange(revision.Removed.Select(x => $"{x} removed."));
            parts.AddRange(revision.Added.Select(x => $"'{x}' added."));
            parts.AddRange(revision.Modified.Select(x => x.EndsWith(".", StringComparison.Ordinal) ? x : x + "."));

            if (revision.WeightsAdjusted)
                parts.Add($"Weights were adjusted to total {SectionWeightTotal} in each section.");

            parts.AddRange(revision.Warnings);
            return string.Join(" ", parts);
        }

        /// <summary>
        /// Runs <paramref name="action"/> with a section's rows temporarily presented as SkillSet 1.
        /// <see cref="DmVoiceRubricRevisionHelper"/> filters to SkillSet 0/1, so Motivation rows
        /// (SkillSet 2) would otherwise be silently dropped by every one of its helpers.
        /// </summary>
        private static T WithSectionMasked<T>(AIScreeningPlanSection section, Func<T> action)
        {
            var restore = MaskSection(section);
            try { return action(); }
            finally { restore(); }
        }

        private static T WithSectionMasked<T>(
            AIScreeningPlanSection first, AIScreeningPlanSection second, Func<T> action)
        {
            var restoreFirst = MaskSection(first);
            var restoreSecond = MaskSection(second);
            try { return action(); }
            finally { restoreFirst(); restoreSecond(); }
        }

        private static Action MaskSection(AIScreeningPlanSection section)
        {
            if (section?.Parameters == null || section.Parameters.Count == 0)
                return () => { };

            var original = section.Parameters.Select(p => p?.SkillSet ?? 0).ToArray();
            foreach (var p in section.Parameters)
            {
                if (p != null) p.SkillSet = 1;
            }

            return () =>
            {
                for (var i = 0; i < section.Parameters.Count; i++)
                {
                    if (section.Parameters[i] != null)
                        section.Parameters[i].SkillSet = original[i];
                }
            };
        }

        #endregion

        #region Scoring

        /// <summary>
        /// Section score from the model's 1-5 per-parameter ratings: each parameter earns
        /// weight x (score / 5), and the section totals 100.
        /// </summary>
        public static decimal ComputeSectionScore(IEnumerable<AIScreeningParameterRating> ratings)
        {
            var list = (ratings ?? Enumerable.Empty<AIScreeningParameterRating>())
                .Where(r => r != null)
                .ToList();
            if (list.Count == 0) return 0m;

            var total = list.Sum(r => r.Weight * ClampScore(r.Score) / (decimal)MaxParameterScore);
            return Math.Round(total, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// The headline figure is the mean of both sections, because the two sections carry equal
        /// weight. It is not section one's score: a candidate who fits the role perfectly but will
        /// not join must not read as a pass.
        /// </summary>
        public static decimal ComputeOverallScore(decimal roleFitmentScore, decimal motivationScore) =>
            Math.Round((roleFitmentScore + motivationScore) / 2m, 2, MidpointRounding.AwayFromZero);

        private static decimal ClampScore(decimal score) =>
            score < 0 ? 0 : (score > MaxParameterScore ? MaxParameterScore : score);

        #endregion

        #region Small shared bits

        private static string Blank(string value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

        private static string ResolveLevelName(TransSkill skill)
        {
            if (!string.IsNullOrWhiteSpace(skill?.LevelName)) return skill.LevelName.Trim();
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

        #endregion
    }

    /// <summary>One parameter's 1-5 rating, as returned by the evaluator, plus its rubric weight.</summary>
    public class AIScreeningParameterRating
    {
        public string ParameterName { get; set; }
        public AIScreeningSectionEnum Section { get; set; }

        /// <summary>Rubric weight within its section. The section's weights total 100.</summary>
        public int Weight { get; set; }

        /// <summary>The model's rating, 1-5.</summary>
        public decimal Score { get; set; }

        /// <summary>weight x (score / 5) — the parameter's contribution to its section score.</summary>
        public decimal ParameterScore =>
            Math.Round(Weight * (Score < 0 ? 0 : Math.Min(Score, AIScreeningIntakeHelper.MaxParameterScore))
                       / AIScreeningIntakeHelper.MaxParameterScore, 2, MidpointRounding.AwayFromZero);
    }
}
