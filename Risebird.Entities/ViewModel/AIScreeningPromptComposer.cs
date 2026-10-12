using System;
using Risebird.Entities.Enum;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Builds the two prompt texts an operator pastes into the OpenAI account for one screening
    /// channel, and nothing else.
    ///
    /// OpenAI has no API that creates a stored prompt and hands back a pmpt_ id, so the loop stays
    /// manual: this composes the text, a person publishes it, and the id they get back is typed
    /// into the form. What it removes is the part that was actually going wrong - a human writing
    /// prompt text from scratch for every channel, and the rubric inside the prompt drifting away
    /// from the rubric in the database.
    ///
    /// Deterministic and offline. The same inputs always produce the same text, which is what
    /// makes <see cref="AIScreeningPromptPair.Fingerprint"/> mean anything.
    /// </summary>
    public static class AIScreeningPromptComposer
    {
        private const int WrapColumn = 95;

        public static AIScreeningPromptPair Compose(AIScreeningPromptInputs inputs)
        {
            inputs = inputs ?? new AIScreeningPromptInputs();

            var fingerprint = ComputeFingerprint(inputs);

            return new AIScreeningPromptPair
            {
                Fingerprint = fingerprint,
                PrimaryInstructions = BuildPrimary(inputs, fingerprint),
                EvaluatorInstructions = BuildEvaluator(inputs, fingerprint)
            };
        }

        // ------------------------------------------------------------------ primary

        private static string BuildPrimary(AIScreeningPromptInputs inputs, string fingerprint)
        {
            var sb = new StringBuilder();

            Header(sb, inputs, fingerprint, "INTERVIEWER PROMPT (Primary Prompt Id)");

            var role = Describe(inputs.JobTitle, "the role");

            // Wrapped after substitution, not hand-wrapped: the job title is arbitrary length and
            // would otherwise push the line well past the column in the OpenAI editor.
            sb.AppendLine(Wrap(
                "You are conducting a short telephone HR screening interview for the role of " + role +
                ". You speak to the candidate directly; everything you say is converted to speech and " +
                "played to them."));
            sb.AppendLine();

            sb.AppendLine("RULE 1 - NEVER REVEAL ASSESSMENT");
            sb.AppendLine("Never state, read out, summarise or hint at a score, rating, verdict, comment or evaluation");
            sb.AppendLine("of any answer. Never output JSON, key/value data, field names or code during the interview.");
            sb.AppendLine("Do not say whether an answer was good, bad, correct, incorrect, sufficient or insufficient.");
            sb.AppendLine("Keep your own assessment entirely to yourself until you are explicitly asked for the final");
            sb.AppendLine("report. If you catch yourself about to produce a rating, ask the next question instead.");
            sb.AppendLine();

            sb.AppendLine("RULE 2 - ONE TURN, ONE QUESTION");
            sb.AppendLine("Each turn is a brief acknowledgement (at most a few words) followed by exactly one");
            sb.AppendLine("question. Nothing else. No preamble, no recap of what they said, no numbering, no");
            sb.AppendLine("meta-commentary about the interview or the rubric.");
            sb.AppendLine();

            sb.AppendLine("RULE 3 - SPEAKABLE LANGUAGE");
            sb.AppendLine("Plain conversational sentences. No markdown, bullets, headings, emoji, brackets or symbols");
            sb.AppendLine("that cannot be read aloud naturally. Keep each turn under about 40 words.");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(inputs.IdealCandidateProfile))
            {
                sb.AppendLine("WHO YOU ARE LOOKING FOR");
                sb.AppendLine(Wrap(inputs.IdealCandidateProfile.Trim()));
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(inputs.Instruction))
            {
                sb.AppendLine("WHAT THE RECRUITER ASKED FOR");
                sb.AppendLine(Wrap(inputs.Instruction.Trim()));
                sb.AppendLine();
            }

            sb.AppendLine("WHAT YOU ARE ASSESSING");
            sb.AppendLine("Two scored sections. They are weighted independently, so each totals 100 on its own.");
            sb.AppendLine("Spend roughly the first half of the interview on the first section and the second half on");
            sb.AppendLine("the second, giving more time to the higher-weighted parameters.");
            sb.AppendLine();
            AppendSections(sb, inputs, includeConcepts: true);

            var mandatory = AllParameters(inputs).Where(p => p.IsMandatory).Select(p => p.Name).ToList();
            if (mandatory.Count > 0)
            {
                sb.AppendLine("Must be covered before the interview ends, whatever time is left:");
                foreach (var name in mandatory)
                {
                    sb.AppendLine("  - " + name);
                }
                sb.AppendLine();
            }

            sb.AppendLine("HOW TO INTERVIEW");
            sb.AppendLine(Bullet(
                "Open by greeting the candidate by name, saying this is a short screening call for the " +
                role + " role, and asking your first question. Do not list what you will cover."));
            sb.AppendLine("- Ask about real experience and concrete specifics, not definitions. Prefer \"walk me");
            sb.AppendLine("  through a time when...\" over \"what is...\".");
            sb.AppendLine("- Follow up once when an answer is vague or missing a specific, then move on. Do not");
            sb.AppendLine("  interrogate.");
            sb.AppendLine("- If the candidate says they do not know or have no experience, accept it briefly and move");
            sb.AppendLine("  to the next parameter. Never correct them, never explain what a better answer would");
            sb.AppendLine("  have been.");
            sb.AppendLine("- Compensation, notice period, location and work mode are asked plainly and neutrally as");
            sb.AppendLine("  facts to establish, never as tests to pass.");

            sb.AppendLine(Bullet(inputs.DurationMinutes > 0
                ? "Keep the whole interview within about " + inputs.DurationMinutes + " minutes, and close " +
                  "by thanking them and telling them the team will be in touch. Do not summarise their " +
                  "performance."
                : "Close by thanking them and telling them the team will be in touch. Do not summarise " +
                  "their performance."));
            sb.AppendLine();

            sb.AppendLine("IF ASKED");
            sb.AppendLine("- If the candidate asks how they did, say the team will review and get back to them. Give");
            sb.AppendLine("  no assessment.");
            sb.AppendLine("- If they ask about the role, answer briefly and return to your question.");
            sb.AppendLine("- If they go off topic, acknowledge in a few words and steer back.");
            sb.AppendLine();

            sb.AppendLine("THE FINAL REPORT");
            sb.AppendLine("Only when you are explicitly asked for the evaluation or report - after the interview has");
            sb.AppendLine("ended - produce the structured output in the schema you are given, and only then. That");
            sb.AppendLine("request arrives as a separate instruction at the end of the conversation. Until it does,");
            sb.AppendLine("everything you produce is spoken aloud to the candidate.");

            return sb.ToString().TrimEnd() + Environment.NewLine;
        }

        // ---------------------------------------------------------------- evaluator

        private static string BuildEvaluator(AIScreeningPromptInputs inputs, string fingerprint)
        {
            var sb = new StringBuilder();

            Header(sb, inputs, fingerprint, "EVALUATOR PROMPT (Interview Evaluator Prompt Id)");

            var role = Describe(inputs.JobTitle, "the role");

            sb.AppendLine(Wrap(
                "You are evaluating a completed HR screening interview for the role of " + role +
                ". You are given the full transcript. Return ONLY a single JSON object - no prose " +
                "before or after, no markdown fence, no explanation."));
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(inputs.IdealCandidateProfile))
            {
                sb.AppendLine("WHO THE ROLE NEEDS");
                sb.AppendLine(Wrap(inputs.IdealCandidateProfile.Trim()));
                sb.AppendLine();
            }

            sb.AppendLine("RULE A - RUBRIC ITEM NAMES MUST MATCH EXACTLY");
            sb.AppendLine("Emit one rubric_score_summary entry for every parameter listed below, with rubric_item set");
            sb.AppendLine("to that parameter's name copied character for character from this list. Do not rephrase,");
            sb.AppendLine("expand, abbreviate, title-case or pluralise it. The report matches your entry to the");
            sb.AppendLine("rubric by this string; a renamed item scores zero and drags the section average down.");
            sb.AppendLine("Emit an entry even for a parameter the interview never reached - score it 1 and say so in");
            sb.AppendLine("observed_level.");
            sb.AppendLine();
            AppendSections(sb, inputs, includeConcepts: false);

            sb.AppendLine("RULE B - SCORE 1 TO 5, AND DO NO ARITHMETIC");
            sb.AppendLine("Every \"score\" is an integer from 1 (far below expectation) to 5 (clearly exceeds");
            sb.AppendLine("expectation). Never emit percentages, weighted values, section totals or an overall");
            sb.AppendLine("percentage. The weights shown above, both section totals and the headline percentage are");
            sb.AppendLine("all computed by Risebird from these 1-5 ratings. A score above 5 is clamped to 5, so");
            sb.AppendLine("emitting 80 reads as 5.");
            sb.AppendLine();

            sb.AppendLine("RULE C - ONE JOINT VERDICT");
            sb.AppendLine("final_verdict is a single judgement across BOTH sections. A candidate who fits the role");
            sb.AppendLine("perfectly but will not join is not a pass. Use exactly one of:");
            sb.AppendLine("  Strongly Selected | Marginally Selected | Marginally Rejected | Strongly Rejected");
            sb.AppendLine();

            sb.AppendLine("RULE D - EVIDENCE, NOT ASSERTION");
            sb.AppendLine("Every comment cites what the candidate actually said. Do not infer from the job");
            sb.AppendLine("description what they must have meant. If an answer was absent, evasive or off-topic, say");
            sb.AppendLine("that plainly.");
            sb.AppendLine();

            sb.AppendLine("OUTPUT SCHEMA");
            sb.AppendLine("{");
            sb.AppendLine("  \"question_ratings\": [");
            sb.AppendLine("    {");
            sb.AppendLine("      \"question\": \"the question as asked\",");
            sb.AppendLine("      \"verdict\": \"one of the four verdicts\",");
            sb.AppendLine("      \"score\": 1,");
            sb.AppendLine("      \"comment\": \"evidence from the answer\",");
            sb.AppendLine("      \"communication_score\": 1,");
            sb.AppendLine("      \"communication_comment\": \"clarity, structure, fluency\"");
            sb.AppendLine("    }");
            sb.AppendLine("  ],");
            sb.AppendLine("  \"rubric_score_summary\": [");
            sb.AppendLine("    {");
            sb.AppendLine("      \"rubric_item\": \"EXACT parameter name from the list above\",");
            sb.AppendLine("      \"expected_level\": \"the level the rubric asked for\",");
            sb.AppendLine("      \"observed_level\": \"the level actually demonstrated\",");
            sb.AppendLine("      \"score\": 1,");
            sb.AppendLine("      \"max_score\": 5");
            sb.AppendLine("    }");
            sb.AppendLine("  ],");
            sb.AppendLine("  \"overall_technical_score\": 0,");
            sb.AppendLine("  \"final_verdict\": \"one of the four verdicts\",");
            sb.AppendLine("  \"final_comment\": \"2-4 sentences covering BOTH sections\",");
            sb.AppendLine("  \"overall_communication_score\": 1,");
            sb.AppendLine("  \"overall_communication_score reasoning\": \"why that communication score\",");
            sb.AppendLine("  \"llm_detection\": {");
            sb.AppendLine("    \"llm_response_suspected\": false,");
            sb.AppendLine("    \"evidence_comment\": \"why, or empty\",");
            sb.AppendLine("    \"suspicious_instances\": []");
            sb.AppendLine("  }");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("Note the space, not an underscore, in \"overall_communication_score reasoning\". That is");
            sb.AppendLine("the field name Risebird reads; correcting it would drop the reasoning from every report.");

            return sb.ToString().TrimEnd() + Environment.NewLine;
        }

        // ------------------------------------------------------------------- shared

        private static void Header(StringBuilder sb, AIScreeningPromptInputs inputs, string fingerprint, string which)
        {
            sb.AppendLine("# RISEBIRD AI AUDIO SCREENING - " + which);

            var channel = string.IsNullOrWhiteSpace(inputs.ChannelCode)
                ? "(not yet created)"
                : inputs.ChannelCode.Trim();

            sb.AppendLine("# Channel: " + channel);
            sb.AppendLine("# Role: " + Describe(inputs.JobTitle, "(not set)"));

            // Stamped into the text itself so the published prompt carries it. When a channel's
            // parameters are edited later this value changes, and the one showing in the OpenAI
            // account no longer matches the one on the form - which is the signal that the prompt
            // needs republishing.
            sb.AppendLine("# Parameters version: " + fingerprint);
            sb.AppendLine("# Generated by Risebird. Re-copy and republish whenever the parameters change.");
            sb.AppendLine();
            sb.AppendLine(new string('-', WrapColumn));
            sb.AppendLine();
        }

        private static void AppendSections(StringBuilder sb, AIScreeningPromptInputs inputs, bool includeConcepts)
        {
            var sections = inputs.Sections ?? new List<AIScreeningPromptSection>();

            if (sections.Count == 0 || sections.All(s => (s.Parameters?.Count ?? 0) == 0))
            {
                sb.AppendLine("  (No parameters have been drafted yet. Finish the rubric before publishing this");
                sb.AppendLine("  prompt - without it the interview has nothing to cover.)");
                sb.AppendLine();
                return;
            }

            foreach (var section in sections)
            {
                var parameters = section.Parameters ?? new List<AIScreeningPromptParameter>();
                var total = parameters.Sum(p => Math.Max(0, p.Weight));

                sb.AppendLine(section.Title + " (weights total " + total + ")");

                if (parameters.Count == 0)
                {
                    sb.AppendLine("  (none)");
                }

                foreach (var p in parameters)
                {
                    var line = new StringBuilder("  - " + p.Name + " - weight " + Math.Max(0, p.Weight));

                    if (!string.IsNullOrWhiteSpace(p.ExpectedLevel))
                    {
                        line.Append(", expected level " + p.ExpectedLevel.Trim());
                    }

                    if (p.IsMandatory)
                    {
                        line.Append(", mandatory");
                    }

                    sb.AppendLine(line.ToString());

                    if (includeConcepts && !string.IsNullOrWhiteSpace(p.Concepts))
                    {
                        sb.AppendLine(Indent(Wrap("explore: " + p.Concepts.Trim(), WrapColumn - 6), "      "));
                    }
                }

                sb.AppendLine();
            }
        }

        private static IEnumerable<AIScreeningPromptParameter> AllParameters(AIScreeningPromptInputs inputs) =>
            (inputs.Sections ?? new List<AIScreeningPromptSection>())
                .SelectMany(s => s.Parameters ?? new List<AIScreeningPromptParameter>())
                .Where(p => p != null && !string.IsNullOrWhiteSpace(p.Name));

        private static string Describe(string value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

        /// <summary>
        /// Short, stable hash of everything the text is built from. Not a security boundary - it
        /// exists so a person can see at a glance whether the prompt published in OpenAI was built
        /// from the parameters the channel carries now.
        /// </summary>
        private static string ComputeFingerprint(AIScreeningPromptInputs inputs)
        {
            var sb = new StringBuilder();
            sb.Append(inputs.JobTitle?.Trim()).Append('\u001f');
            sb.Append(inputs.IdealCandidateProfile?.Trim()).Append('\u001f');
            sb.Append(inputs.Instruction?.Trim()).Append('\u001f');
            sb.Append(inputs.DurationMinutes.ToString(CultureInfo.InvariantCulture)).Append('\u001f');

            foreach (var section in inputs.Sections ?? new List<AIScreeningPromptSection>())
            {
                sb.Append(section.Title?.Trim()).Append('\u001e');

                foreach (var p in section.Parameters ?? new List<AIScreeningPromptParameter>())
                {
                    sb.Append(p.Name?.Trim()).Append('\u001d')
                      .Append(p.Weight.ToString(CultureInfo.InvariantCulture)).Append('\u001d')
                      .Append(p.ExpectedLevel?.Trim()).Append('\u001d')
                      .Append(p.Concepts?.Trim()).Append('\u001d')
                      .Append(p.IsMandatory ? "1" : "0").Append('\u001e');
                }
            }

            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
                return BitConverter.ToString(hash, 0, 4).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        /// <summary>
        /// A "- " bullet whose continuation lines align under the text. Used wherever an injected
        /// value of unpredictable length (a job title, a duration) sits inside the sentence, since
        /// a hand-wrapped line would overrun once that value is substituted in.
        /// </summary>
        private static string Bullet(string text)
        {
            var wrapped = Wrap(text, WrapColumn - 2)
                .Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            var sb = new StringBuilder("- " + wrapped[0]);
            for (var i = 1; i < wrapped.Length; i++)
            {
                sb.Append(Environment.NewLine).Append("  ").Append(wrapped[i]);
            }

            return sb.ToString();
        }

        private static string Indent(string text, string indent) =>
            string.Join(Environment.NewLine,
                (text ?? string.Empty).Split('\n').Select(l => indent + l.TrimEnd('\r')));

        /// <summary>Wraps to a fixed column so the pasted prompt reads the same in the OpenAI editor.</summary>
        private static string Wrap(string text, int column = WrapColumn)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var lines = new List<string>();

            foreach (var paragraph in text.Replace("\r\n", "\n").Split('\n'))
            {
                if (paragraph.Trim().Length == 0)
                {
                    lines.Add(string.Empty);
                    continue;
                }

                var line = new StringBuilder();

                foreach (var word in paragraph.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (line.Length > 0 && line.Length + 1 + word.Length > column)
                    {
                        lines.Add(line.ToString());
                        line.Clear();
                    }

                    if (line.Length > 0) line.Append(' ');
                    line.Append(word);
                }

                if (line.Length > 0) lines.Add(line.ToString());
            }

            return string.Join(Environment.NewLine, lines);
        }
    }

    /// <summary>Everything the two prompt texts are built from, independent of where it was read.</summary>
    public class AIScreeningPromptInputs
    {
        public string ChannelCode { get; set; }
        public string JobTitle { get; set; }
        public string IdealCandidateProfile { get; set; }
        public string Instruction { get; set; }
        public int DurationMinutes { get; set; }
        public List<AIScreeningPromptSection> Sections { get; set; } = new List<AIScreeningPromptSection>();

        /// <summary>
        /// Composer inputs for an intake still in progress. Takes the drafted plan's own sections
        /// rather than re-deriving them from SkillSet, so the text reflects what the recruiter is
        /// looking at on screen, including any revision they have just made.
        /// </summary>
        public static AIScreeningPromptInputs FromDraft(AIScreeningDraft draft)
        {
            if (draft == null) return new AIScreeningPromptInputs();

            return new AIScreeningPromptInputs
            {
                ChannelCode = draft.CreatedIrfId,
                JobTitle = draft.JobTitle,
                IdealCandidateProfile = string.IsNullOrWhiteSpace(draft.ConfirmedIdealCandidateProfile)
                    ? draft.ProposedIdealCandidateProfile
                    : draft.ConfirmedIdealCandidateProfile,
                Instruction = draft.Instruction,
                DurationMinutes = draft.Plan?.DurationMinutes ?? 0,
                Sections = (draft.Plan?.Sections ?? new List<AIScreeningPlanSection>())
                    .Where(s => s != null)
                    .Select(s => new AIScreeningPromptSection
                    {
                        Title = string.IsNullOrWhiteSpace(s.Title)
                            ? AIScreeningIntakeHelper.GetSectionTitle(s.Section)
                            : s.Title,
                        Parameters = (s.Parameters ?? new List<TransSkill>())
                            .Where(p => p != null && !string.IsNullOrWhiteSpace(p.SkillName))
                            .Select(p => new AIScreeningPromptParameter
                            {
                                Name = p.SkillName,
                                Weight = p.Weightage,
                                ExpectedLevel = p.LevelName,
                                Concepts = p.Concepts,
                                IsMandatory = p.isEliminationskill
                            }).ToList()
                    }).ToList()
            };
        }

        /// <summary>
        /// Reads a sectioned rubric into composer sections, in section order. Rows carrying no
        /// SkillSet are left out rather than guessed at: an unsectioned rubric is the broken shape
        /// the repair migration exists for, and inventing a section for it would put the wrong
        /// parameters in front of the interviewer.
        /// </summary>
        public static List<AIScreeningPromptSection> SectionsFrom(IEnumerable<TransSkill> rubric)
        {
            var sections = new List<AIScreeningPromptSection>();
            var rows = (rubric ?? Enumerable.Empty<TransSkill>()).Where(r => r != null).ToList();

            foreach (var section in new[]
                     {
                         AIScreeningSectionEnum.RoleFitment,
                         AIScreeningSectionEnum.MotivationToJoinFitment
                     })
            {
                var inSection = rows.Where(r => r.SkillSet == (int)section).ToList();
                if (inSection.Count == 0) continue;

                sections.Add(new AIScreeningPromptSection
                {
                    Title = AIScreeningIntakeHelper.GetSectionTitle(section),
                    Parameters = inSection.Select(r => new AIScreeningPromptParameter
                    {
                        Name = r.SkillName,
                        Weight = r.Weightage,
                        ExpectedLevel = r.LevelName,
                        Concepts = r.Concepts,
                        IsMandatory = r.isEliminationskill
                    }).ToList()
                });
            }

            return sections;
        }
    }

    public class AIScreeningPromptSection
    {
        public string Title { get; set; }
        public List<AIScreeningPromptParameter> Parameters { get; set; } = new List<AIScreeningPromptParameter>();
    }

    public class AIScreeningPromptParameter
    {
        public string Name { get; set; }
        public int Weight { get; set; }
        public string ExpectedLevel { get; set; }
        public string Concepts { get; set; }
        public bool IsMandatory { get; set; }
    }

    public class AIScreeningPromptPair
    {
        public string PrimaryInstructions { get; set; }
        public string EvaluatorInstructions { get; set; }

        /// <summary>
        /// Short hash of the parameters both texts were built from, also stamped into each text.
        /// Lets an operator tell whether what is published in OpenAI still matches the channel.
        /// </summary>
        public string Fingerprint { get; set; }
    }
}
