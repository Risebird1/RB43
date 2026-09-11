using Risebird.Entities.ViewModel;

namespace Risebird.CommonHelper.Tests.EntitiesHelpers
{
    public class DmIvQuestionRubricHelperBuildRubricPromptSectionTests
    {
        private static TransSkill Skill(string name, string level = "Advanced", int weight = 40) =>
            new() { SkillName = name, LevelName = level, Weightage = weight };

        [Fact]
        public void Lists_each_skill_with_its_level_and_weight()
        {
            var section = DmIvQuestionRubricHelper.BuildRubricPromptSection(new[]
            {
                Skill("C#", "Advanced", 60),
                Skill("SQL", "Intermediate", 40)
            });

            Assert.Contains("- Skill: C# | Level: Advanced | Weight: 60%", section);
            Assert.Contains("- Skill: SQL | Level: Intermediate | Weight: 40%", section);
        }

        [Fact]
        public void Starts_with_the_rubric_header_and_ends_with_the_instruction()
        {
            var section = DmIvQuestionRubricHelper.BuildRubricPromptSection(new[] { Skill("C#") });

            Assert.StartsWith("Score Rubric (use these exact skill names only):", section);
            Assert.EndsWith(DmIvQuestionRubricHelper.SkillNameInstruction, section);
        }

        [Fact]
        public void Renders_a_missing_level_as_not_applicable()
        {
            foreach (var level in new[] { null, "", "   " })
            {
                var section = DmIvQuestionRubricHelper.BuildRubricPromptSection(new[] { Skill("C#", level) });
                Assert.Contains("Level: N/A", section);
            }
        }

        [Fact]
        public void Trims_skill_and_level_names()
        {
            var section = DmIvQuestionRubricHelper.BuildRubricPromptSection(new[]
            {
                Skill("  C#  ", "  Advanced  ")
            });

            Assert.Contains("- Skill: C# | Level: Advanced |", section);
        }

        [Fact]
        public void Skips_skills_with_a_blank_name()
        {
            var section = DmIvQuestionRubricHelper.BuildRubricPromptSection(new[]
            {
                Skill(null), Skill(""), Skill("   "), Skill("C#")
            });

            Assert.Single(section.Split('\n').Where(l => l.StartsWith("- Skill:")));
        }

        [Fact]
        public void Still_emits_the_header_and_instruction_for_an_empty_rubric()
        {
            foreach (var skills in new[] { Array.Empty<TransSkill>(), null })
            {
                var section = DmIvQuestionRubricHelper.BuildRubricPromptSection(skills);

                Assert.Contains("Score Rubric", section);
                Assert.Contains(DmIvQuestionRubricHelper.SkillNameInstruction, section);
                Assert.DoesNotContain("- Skill:", section);
            }
        }

        [Fact]
        public void Renders_a_zero_weight_verbatim()
        {
            var section = DmIvQuestionRubricHelper.BuildRubricPromptSection(new[] { Skill("C#", "Basic", 0) });

            Assert.Contains("Weight: 0%", section);
        }
    }

    public class DmIvQuestionRubricHelperSkillNameSetTests
    {
        private static TransSkill Skill(string name) => new() { SkillName = name };

        [Fact]
        public void Collects_trimmed_skill_names()
        {
            var set = DmIvQuestionRubricHelper.BuildValidSkillNameSet(new[] { Skill("  C#  "), Skill("SQL") });

            Assert.Equal(2, set.Count);
            Assert.Contains("C#", set);
            Assert.Contains("SQL", set);
        }

        [Fact]
        public void Set_is_case_insensitive()
        {
            var set = DmIvQuestionRubricHelper.BuildValidSkillNameSet(new[] { Skill("C#") });

            Assert.Contains("c#", set);
        }

        [Fact]
        public void Drops_blank_names()
        {
            var set = DmIvQuestionRubricHelper.BuildValidSkillNameSet(new[]
            {
                Skill(null), Skill(""), Skill("   "), Skill("C#")
            });

            Assert.Single(set);
        }

        [Fact]
        public void De_duplicates_names_that_differ_only_in_case_or_padding()
        {
            var set = DmIvQuestionRubricHelper.BuildValidSkillNameSet(new[]
            {
                Skill("C#"), Skill("c#"), Skill("  C#  ")
            });

            Assert.Single(set);
        }

        [Fact]
        public void Handles_a_null_or_empty_collection()
        {
            Assert.Empty(DmIvQuestionRubricHelper.BuildValidSkillNameSet(null));
            Assert.Empty(DmIvQuestionRubricHelper.BuildValidSkillNameSet(Array.Empty<TransSkill>()));
        }
    }

    public class DmIvQuestionRubricHelperTryMatchSkillTests
    {
        private static HashSet<string> Valid(params string[] names) =>
            new(names, StringComparer.OrdinalIgnoreCase);

        [Theory]
        [InlineData("C#")]
        [InlineData("c#")]
        [InlineData("  C#  ")]
        [InlineData("C#\t")]
        public void Matches_case_insensitively_after_trimming(string returned)
        {
            var matched = DmIvQuestionRubricHelper.TryMatchSkill(returned, Valid("C#", "SQL"), out var canonical);

            Assert.True(matched);
            Assert.Equal("C#", canonical);
        }

        [Fact]
        public void Returns_the_canonical_spelling_not_the_ai_spelling()
        {
            DmIvQuestionRubricHelper.TryMatchSkill("sql", Valid("SQL"), out var canonical);

            Assert.Equal("SQL", canonical);
        }

        [Theory]
        [InlineData("CSharp")]
        [InlineData("C Sharp")]
        [InlineData("C# and SQL")]
        [InlineData("C")]
        public void Rejects_paraphrased_or_combined_skill_names(string returned)
        {
            var matched = DmIvQuestionRubricHelper.TryMatchSkill(returned, Valid("C#", "SQL"), out var canonical);

            Assert.False(matched);
            Assert.Null(canonical);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Rejects_a_blank_returned_skill(string returned)
        {
            var matched = DmIvQuestionRubricHelper.TryMatchSkill(returned, Valid("C#"), out var canonical);

            Assert.False(matched);
            Assert.Null(canonical);
        }

        [Fact]
        public void Rejects_everything_when_the_valid_set_is_null_or_empty()
        {
            Assert.False(DmIvQuestionRubricHelper.TryMatchSkill("C#", null, out _));
            Assert.False(DmIvQuestionRubricHelper.TryMatchSkill("C#", Valid(), out _));
        }

        [Fact]
        public void Clears_a_stale_out_parameter_on_failure()
        {
            var canonical = "stale";

            DmIvQuestionRubricHelper.TryMatchSkill("nope", Valid("C#"), out canonical);

            Assert.Null(canonical);
        }
    }

    public class DmIvQuestionRubricHelperSpokenGistTests
    {
        [Fact]
        public void Prefers_the_ai_supplied_gist()
        {
            var gist = DmIvQuestionRubricHelper.BuildSpokenQuestionGist(
                "Explain `IDisposable` in C#.", "Tell me how you clean up unmanaged resources.");

            Assert.Equal("Tell me how you clean up unmanaged resources.", gist);
        }

        [Fact]
        public void Collapses_whitespace_in_the_ai_gist()
        {
            var gist = DmIvQuestionRubricHelper.BuildSpokenQuestionGist(null, "  Tell   me\n\nabout\tthis.  ");

            Assert.Equal("Tell me about this.", gist);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Falls_back_to_the_question_when_the_ai_gist_is_blank(string aiGist)
        {
            var gist = DmIvQuestionRubricHelper.BuildSpokenQuestionGist("What is a closure?", aiGist);

            Assert.Equal("What is a closure?", gist);
        }

        [Fact]
        public void Strips_fenced_code_blocks_from_the_question()
        {
            var question = "Fix this:\n```csharp\nvar x = 1;\n```\nWhat is wrong?";

            var gist = DmIvQuestionRubricHelper.BuildSpokenQuestionGist(question);

            Assert.DoesNotContain("var x = 1", gist);
            Assert.Contains("Fix this:", gist);
            Assert.Contains("What is wrong?", gist);
        }

        [Fact]
        public void Strips_inline_code_spans_from_the_question()
        {
            var gist = DmIvQuestionRubricHelper.BuildSpokenQuestionGist("Explain `IDisposable` please.");

            Assert.DoesNotContain("IDisposable", gist);
            Assert.Contains("Explain", gist);
            Assert.Contains("please.", gist);
        }

        [Fact]
        public void Falls_back_to_the_raw_question_when_stripping_removes_everything()
        {
            var question = "```\nonly code here\n```";

            var gist = DmIvQuestionRubricHelper.BuildSpokenQuestionGist(question);

            Assert.Equal("``` only code here ```", gist);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Returns_null_when_there_is_nothing_to_say(string question) =>
            Assert.Null(DmIvQuestionRubricHelper.BuildSpokenQuestionGist(question));

        [Fact]
        public void Truncates_very_long_text_with_an_ellipsis()
        {
            var gist = DmIvQuestionRubricHelper.BuildSpokenQuestionGist(new string('a', 2_000));

            Assert.Equal(1501, gist.Length);
            Assert.EndsWith("…", gist);
        }

        [Fact]
        public void Does_not_truncate_text_at_the_limit()
        {
            var gist = DmIvQuestionRubricHelper.BuildSpokenQuestionGist(new string('a', 1_500));

            Assert.Equal(1500, gist.Length);
            Assert.DoesNotContain("…", gist);
        }

        [Fact]
        public void Instruction_constants_are_non_empty_guidance()
        {
            Assert.Contains("skill_or_competency", DmIvQuestionRubricHelper.SkillNameInstruction);
            Assert.Contains("question_gist", DmIvQuestionRubricHelper.QuestionGistInstruction);
        }
    }
}
