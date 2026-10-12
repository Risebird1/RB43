using System;
using System.Collections.Generic;
using System.Linq;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;

namespace Risebird.Business.Tests.AIScreeningTests
{
    /// <summary>
    /// The operations team pastes this text into OpenAI unread. Anything the composer leaves out or
    /// gets wrong becomes a published prompt that nobody notices until the reports come back empty,
    /// so the tests that matter are the ones about what must appear in the text verbatim.
    /// </summary>
    public class AIScreeningPromptComposerTests
    {
        private static TransSkill Param(
            string name, int weight, int skillSet, string level = "Intermediate",
            string concepts = null, bool mandatory = false) =>
            new TransSkill
            {
                SkillName = name,
                Weightage = weight,
                SkillSet = skillSet,
                LevelName = level,
                Concepts = concepts,
                isEliminationskill = mandatory
            };

        private static List<TransSkill> Rubric() => new List<TransSkill>
        {
            Param("Backend depth (Java/Go)", 60, 1, "Advanced", "service design, concurrency", mandatory: true),
            Param("Communication", 40, 1),
            Param("Relocation to Bangalore", 70, 2, "Confirmed", mandatory: true),
            Param("Notice period", 30, 2, "60 days or less")
        };

        private static AIScreeningPromptInputs Inputs(List<TransSkill> rubric = null) =>
            new AIScreeningPromptInputs
            {
                ChannelCode = "HRSCR-ERO-FA59E",
                JobTitle = "Senior Backend Engineer",
                IdealCandidateProfile = "Six years of production Go.",
                Instruction = "Push on relocation.",
                DurationMinutes = 15,
                Sections = AIScreeningPromptInputs.SectionsFrom(rubric ?? Rubric())
            };

        private static AIScreeningPromptPair Compose(AIScreeningPromptInputs inputs = null) =>
            AIScreeningPromptComposer.Compose(inputs ?? Inputs());

        #region The evaluator must name every parameter exactly

        /// <summary>
        /// AIScreeningReportMapper matches rubric_score_summary entries to the rubric by name. If a
        /// parameter is missing from the evaluator prompt the model has no way to know it exists,
        /// it scores zero, and the section average silently drops.
        /// </summary>
        [Fact]
        public void Every_rubric_parameter_appears_in_the_evaluator_prompt()
        {
            var text = Compose().EvaluatorInstructions;

            foreach (var name in Rubric().Select(r => r.SkillName))
            {
                Assert.Contains(name, text, StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Case and punctuation must survive verbatim - "Backend depth (Java/Go)" title-cased or
        /// stripped of its brackets would no longer match the rubric row.
        /// </summary>
        [Fact]
        public void Parameter_names_are_reproduced_character_for_character()
        {
            var awkward = "C++ / .NET interop (legacy)";
            var text = Compose(Inputs(new List<TransSkill> { Param(awkward, 100, 1) })).EvaluatorInstructions;

            Assert.Contains(awkward, text, StringComparison.Ordinal);
        }

        [Fact]
        public void Both_sections_are_named_in_both_prompts()
        {
            var pair = Compose();

            foreach (var text in new[] { pair.PrimaryInstructions, pair.EvaluatorInstructions })
            {
                Assert.Contains("Role Fitment", text, StringComparison.Ordinal);
                Assert.Contains("Motivation to Join Fitment", text, StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// The report computes section totals and the headline percentage itself from 1-5 ratings.
        /// An evaluator that also totals them produces a second, conflicting set of numbers.
        /// </summary>
        [Fact]
        public void The_evaluator_is_told_to_score_one_to_five_and_do_no_arithmetic()
        {
            var text = Compose().EvaluatorInstructions;

            Assert.Contains("RULE B - SCORE 1 TO 5, AND DO NO ARITHMETIC", text, StringComparison.Ordinal);
            Assert.Contains("clamped to 5", text, StringComparison.Ordinal);
        }

        /// <summary>The space is the field name the parser expects; an underscore drops the field.</summary>
        [Fact]
        public void The_schema_keeps_the_spaced_communication_reasoning_field()
        {
            var text = Compose().EvaluatorInstructions;

            Assert.Contains("\"overall_communication_score reasoning\"", text, StringComparison.Ordinal);
            Assert.DoesNotContain("\"overall_communication_score_reasoning\"", text, StringComparison.Ordinal);
        }

        [Fact]
        public void The_four_canonical_verdicts_are_spelled_out_for_the_evaluator()
        {
            var text = Compose().EvaluatorInstructions;

            foreach (var verdict in new[]
                     { "Strongly Selected", "Marginally Selected", "Marginally Rejected", "Strongly Rejected" })
            {
                Assert.Contains(verdict, text, StringComparison.Ordinal);
                Assert.Equal(verdict, AIInterviewVerdict.Normalize(verdict));
            }
        }

        #endregion

        #region The interviewer must not leak assessment

        /// <summary>
        /// The whole reason screening channels needed their own prompt: the shared one emitted its
        /// evaluation mid-interview and text-to-speech read the candidate their own score aloud.
        /// </summary>
        [Fact]
        public void The_interviewer_prompt_leads_with_the_do_not_reveal_rule()
        {
            var text = Compose().PrimaryInstructions;

            Assert.Contains("RULE 1 - NEVER REVEAL ASSESSMENT", text, StringComparison.Ordinal);
            Assert.True(
                text.IndexOf("RULE 1 - NEVER REVEAL ASSESSMENT", StringComparison.Ordinal)
                < text.IndexOf("WHAT YOU ARE ASSESSING", StringComparison.Ordinal),
                "The do-not-reveal rule has to come before the rubric it applies to.");
        }

        /// <summary>The interviewer prompt is spoken aloud, so it must never carry the report schema.</summary>
        [Fact]
        public void The_interviewer_prompt_does_not_contain_the_report_schema()
        {
            var text = Compose().PrimaryInstructions;

            Assert.DoesNotContain("rubric_score_summary", text, StringComparison.Ordinal);
            Assert.DoesNotContain("final_verdict", text, StringComparison.Ordinal);
        }

        [Fact]
        public void Mandatory_parameters_are_called_out_to_the_interviewer()
        {
            var text = Compose().PrimaryInstructions;

            Assert.Contains("Must be covered before the interview ends", text, StringComparison.Ordinal);
            Assert.Contains("Relocation to Bangalore", text, StringComparison.Ordinal);
        }

        /// <summary>
        /// Concepts guide the questions, so the interviewer gets them; the evaluator scores what was
        /// actually said and does not need them cluttering its rubric list.
        /// </summary>
        [Fact]
        public void Concepts_reach_the_interviewer_and_not_the_evaluator()
        {
            var pair = Compose();

            Assert.Contains("service design, concurrency", pair.PrimaryInstructions, StringComparison.Ordinal);
            Assert.DoesNotContain("service design, concurrency", pair.EvaluatorInstructions, StringComparison.Ordinal);
        }

        #endregion

        #region The fingerprint tracks the parameters

        [Fact]
        public void The_same_parameters_always_produce_the_same_text_and_fingerprint()
        {
            var first = Compose();
            var second = Compose();

            Assert.Equal(first.Fingerprint, second.Fingerprint);
            Assert.Equal(first.PrimaryInstructions, second.PrimaryInstructions);
            Assert.Equal(first.EvaluatorInstructions, second.EvaluatorInstructions);
        }

        /// <summary>
        /// Each of these is a reason to republish. If any stopped moving the fingerprint, an edited
        /// channel would keep showing the version stamp of a prompt built from the old parameters.
        /// </summary>
        [Fact]
        public void Changing_any_prompt_bearing_parameter_moves_the_fingerprint()
        {
            var baseline = Compose().Fingerprint;

            var retitled = Inputs(); retitled.JobTitle = "Staff Backend Engineer";
            var reprofiled = Inputs(); reprofiled.IdealCandidateProfile = "Ten years of Rust.";
            var reinstructed = Inputs(); reinstructed.Instruction = "Push on compensation instead.";
            var retimed = Inputs(); retimed.DurationMinutes = 30;

            var reweighted = Inputs(new List<TransSkill>
            {
                Param("Backend depth (Java/Go)", 70, 1, "Advanced", "service design, concurrency", mandatory: true),
                Param("Communication", 30, 1),
                Param("Relocation to Bangalore", 70, 2, "Confirmed", mandatory: true),
                Param("Notice period", 30, 2, "60 days or less")
            });

            var renamed = Inputs(new List<TransSkill>
            {
                Param("Backend depth (Java or Go)", 60, 1, "Advanced", "service design, concurrency", mandatory: true),
                Param("Communication", 40, 1),
                Param("Relocation to Bangalore", 70, 2, "Confirmed", mandatory: true),
                Param("Notice period", 30, 2, "60 days or less")
            });

            foreach (var changed in new[] { retitled, reprofiled, reinstructed, retimed, reweighted, renamed })
            {
                Assert.NotEqual(baseline, AIScreeningPromptComposer.Compose(changed).Fingerprint);
            }
        }

        /// <summary>
        /// The stamp has to travel into OpenAI with the text, or an operator cannot tell whether the
        /// published prompt was built from the parameters the channel carries now.
        /// </summary>
        [Fact]
        public void The_fingerprint_is_stamped_into_both_texts()
        {
            var pair = Compose();

            Assert.Contains("# Parameters version: " + pair.Fingerprint, pair.PrimaryInstructions, StringComparison.Ordinal);
            Assert.Contains("# Parameters version: " + pair.Fingerprint, pair.EvaluatorInstructions, StringComparison.Ordinal);
        }

        #endregion

        #region Degenerate inputs

        [Fact]
        public void A_null_input_still_produces_both_texts_rather_than_throwing()
        {
            var pair = AIScreeningPromptComposer.Compose(null);

            Assert.False(string.IsNullOrWhiteSpace(pair.PrimaryInstructions));
            Assert.False(string.IsNullOrWhiteSpace(pair.EvaluatorInstructions));
        }

        /// <summary>
        /// A rubric that has not been drafted yet must say so in the text. Silently emitting a prompt
        /// with no parameters is how a channel ends up published against an empty rubric.
        /// </summary>
        [Fact]
        public void An_empty_rubric_is_called_out_in_the_text()
        {
            var pair = Compose(new AIScreeningPromptInputs { JobTitle = "Analyst" });

            Assert.Contains("No parameters have been drafted yet", pair.PrimaryInstructions, StringComparison.Ordinal);
            Assert.Contains("No parameters have been drafted yet", pair.EvaluatorInstructions, StringComparison.Ordinal);
        }

        /// <summary>
        /// An unsectioned rubric is the damaged shape Repair_AIScreeningChannels_MissingProvisioning
        /// exists for. Guessing a section would put the wrong parameters in front of the interviewer.
        /// </summary>
        [Fact]
        public void Rubric_rows_with_no_section_are_left_out_rather_than_guessed()
        {
            var sections = AIScreeningPromptInputs.SectionsFrom(new List<TransSkill>
            {
                Param("Unsectioned parameter", 100, 0)
            });

            Assert.Empty(sections);
        }

        [Fact]
        public void A_missing_job_title_does_not_leave_a_dangling_sentence()
        {
            var pair = Compose(new AIScreeningPromptInputs { Sections = AIScreeningPromptInputs.SectionsFrom(Rubric()) });

            Assert.Contains("for the role of the role.", pair.PrimaryInstructions, StringComparison.Ordinal);
            Assert.Contains("# Role: (not set)", pair.PrimaryInstructions, StringComparison.Ordinal);
        }

        [Fact]
        public void A_channel_that_does_not_exist_yet_is_labelled_as_such() =>
            Assert.Contains(
                "# Channel: (not yet created)",
                Compose(new AIScreeningPromptInputs { JobTitle = "Analyst" }).PrimaryInstructions,
                StringComparison.Ordinal);

        /// <summary>
        /// The text is pasted into a fixed-width editor. A long job title substituted into a
        /// hand-wrapped line used to push it well past the column.
        /// </summary>
        [Fact]
        public void No_line_overruns_the_wrap_column_even_with_a_very_long_job_title()
        {
            var inputs = Inputs();
            inputs.JobTitle = "Senior Principal Distributed Systems and Platform Reliability Engineer, "
                              + "Payments Infrastructure, Bangalore and Hyderabad";

            var pair = AIScreeningPromptComposer.Compose(inputs);

            foreach (var text in new[] { pair.PrimaryInstructions, pair.EvaluatorInstructions })
            {
                foreach (var line in text.Split('\n').Select(l => l.TrimEnd('\r')))
                {
                    // The header's Role line and the rubric list carry single unbreakable values and
                    // are allowed to run; the prose must not.
                    if (line.StartsWith("#", StringComparison.Ordinal)) continue;
                    if (line.StartsWith("  - ", StringComparison.Ordinal)) continue;

                    Assert.True(line.Length <= 96, $"Line of {line.Length} chars: {line}");
                }
            }
        }

        #endregion

        #region Reading a draft

        [Fact]
        public void A_draft_prefers_the_confirmed_profile_over_the_proposed_one()
        {
            var inputs = AIScreeningPromptInputs.FromDraft(new AIScreeningDraft
            {
                JobTitle = "Analyst",
                ProposedIdealCandidateProfile = "PROPOSED",
                ConfirmedIdealCandidateProfile = "CONFIRMED"
            });

            Assert.Equal("CONFIRMED", inputs.IdealCandidateProfile);
        }

        [Fact]
        public void A_draft_falls_back_to_the_proposed_profile_before_it_is_confirmed()
        {
            var inputs = AIScreeningPromptInputs.FromDraft(new AIScreeningDraft
            {
                JobTitle = "Analyst",
                ProposedIdealCandidateProfile = "PROPOSED"
            });

            Assert.Equal("PROPOSED", inputs.IdealCandidateProfile);
        }

        /// <summary>
        /// Taken from the plan's own sections, so a revision made at step 3 is in the text by the
        /// time the operator reaches step 4.
        /// </summary>
        [Fact]
        public void A_draft_carries_the_drafted_plan_into_the_prompt()
        {
            var inputs = AIScreeningPromptInputs.FromDraft(new AIScreeningDraft
            {
                JobTitle = "Analyst",
                Plan = new AIScreeningPlan
                {
                    DurationMinutes = 20,
                    Sections = new List<AIScreeningPlanSection>
                    {
                        new AIScreeningPlanSection
                        {
                            Section = AIScreeningSectionEnum.RoleFitment,
                            Title = "Role Fitment",
                            Parameters = new List<TransSkill> { Param("Revised parameter", 100, 1) }
                        }
                    }
                }
            });

            Assert.Equal(20, inputs.DurationMinutes);
            Assert.Contains("Revised parameter",
                AIScreeningPromptComposer.Compose(inputs).EvaluatorInstructions, StringComparison.Ordinal);
        }

        [Fact]
        public void A_null_draft_does_not_throw() =>
            Assert.NotNull(AIScreeningPromptInputs.FromDraft(null));

        #endregion
    }
}
