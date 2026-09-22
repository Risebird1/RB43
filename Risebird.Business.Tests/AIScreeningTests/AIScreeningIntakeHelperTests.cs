using System.Collections.Generic;
using System.Linq;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;

namespace Risebird.Business.Tests.AIScreeningTests
{
    /// <summary>
    /// The four behaviours the AI screening intake is easy to get wrong: per-section weighting,
    /// off-topic revisions, the section dimension surviving the shared rubric helpers, and the
    /// overall figure not being section one's.
    /// </summary>
    public class AIScreeningIntakeHelperTests
    {
        private static TransSkill Param(string name, int weight, AIScreeningSectionEnum section) =>
            new TransSkill
            {
                SkillName = name,
                Weightage = weight,
                SkillSet = (int)section,
                LevelId = (int)LevelTechEnum.Competent,
                Timelimit = 3
            };

        private static AIScreeningPlan Plan(
            IEnumerable<TransSkill> roleFitment,
            IEnumerable<TransSkill> motivation)
        {
            return new AIScreeningPlan
            {
                RoleLabel = "Senior Backend Engineer - Bangalore",
                DurationMinutes = 20,
                Sections = new List<AIScreeningPlanSection>
                {
                    new AIScreeningPlanSection
                    {
                        Section = AIScreeningSectionEnum.RoleFitment,
                        Title = AIScreeningIntakeHelper.GetSectionTitle(AIScreeningSectionEnum.RoleFitment),
                        Parameters = roleFitment.ToList()
                    },
                    new AIScreeningPlanSection
                    {
                        Section = AIScreeningSectionEnum.MotivationToJoinFitment,
                        Title = AIScreeningIntakeHelper.GetSectionTitle(AIScreeningSectionEnum.MotivationToJoinFitment),
                        Parameters = motivation.ToList()
                    }
                }
            };
        }

        #region Weights total 100 per section, independently

        [Fact]
        public void NormalizeSectionWeights_brings_each_section_to_100_independently_not_100_across_both()
        {
            var plan = Plan(
                new[]
                {
                    Param("Core Tech Stack", 20, AIScreeningSectionEnum.RoleFitment),
                    Param("Relevant Experience", 20, AIScreeningSectionEnum.RoleFitment)
                },
                new[]
                {
                    Param("Compensation Feasibility", 10, AIScreeningSectionEnum.MotivationToJoinFitment),
                    Param("Location Fit", 10, AIScreeningSectionEnum.MotivationToJoinFitment)
                });

            var adjusted = AIScreeningIntakeHelper.NormalizeSectionWeights(plan);

            Assert.True(adjusted);
            Assert.Equal(100, plan.GetSection(AIScreeningSectionEnum.RoleFitment).WeightTotal);
            Assert.Equal(100, plan.GetSection(AIScreeningSectionEnum.MotivationToJoinFitment).WeightTotal);
        }

        [Fact]
        public void NormalizeSectionWeights_leaves_a_section_that_already_totals_100_alone()
        {
            var plan = Plan(
                new[]
                {
                    Param("Core Tech Stack", 60, AIScreeningSectionEnum.RoleFitment),
                    Param("Relevant Experience", 40, AIScreeningSectionEnum.RoleFitment)
                },
                new[]
                {
                    Param("Compensation Feasibility", 55, AIScreeningSectionEnum.MotivationToJoinFitment),
                    Param("Location Fit", 45, AIScreeningSectionEnum.MotivationToJoinFitment)
                });

            var adjusted = AIScreeningIntakeHelper.NormalizeSectionWeights(plan);

            Assert.False(adjusted);
            Assert.Equal(60, plan.GetSection(AIScreeningSectionEnum.RoleFitment).Parameters[0].Weightage);
            Assert.Equal(55, plan.GetSection(AIScreeningSectionEnum.MotivationToJoinFitment).Parameters[0].Weightage);
        }

        [Fact]
        public void NormalizeSectionWeights_keeps_the_motivation_section_which_the_shared_helpers_filter_out()
        {
            // DmVoiceRubricRevisionHelper only looks at SkillSet 0 or 1. Motivation rows are
            // SkillSet 2 and would be dropped without the masking in AIScreeningIntakeHelper.
            var plan = Plan(
                new[] { Param("Core Tech Stack", 100, AIScreeningSectionEnum.RoleFitment) },
                new[]
                {
                    Param("Compensation Feasibility", 30, AIScreeningSectionEnum.MotivationToJoinFitment),
                    Param("Notice Period", 30, AIScreeningSectionEnum.MotivationToJoinFitment)
                });

            AIScreeningIntakeHelper.NormalizeSectionWeights(plan);

            var motivation = plan.GetSection(AIScreeningSectionEnum.MotivationToJoinFitment);
            Assert.Equal(2, motivation.Parameters.Count);
            Assert.Equal(100, motivation.WeightTotal);

            // The section stamp must survive the round trip through the shared helpers.
            Assert.All(motivation.Parameters,
                p => Assert.Equal((int)AIScreeningSectionEnum.MotivationToJoinFitment, p.SkillSet));
        }

        [Fact]
        public void ToTransSkills_stamps_each_parameter_with_its_section()
        {
            var plan = Plan(
                new[] { Param("Core Tech Stack", 100, AIScreeningSectionEnum.RoleFitment) },
                new[] { Param("Location Fit", 100, AIScreeningSectionEnum.MotivationToJoinFitment) });

            var skills = plan.ToTransSkills(irfId: 42);

            Assert.Equal(2, skills.Count);
            Assert.All(skills, s => Assert.Equal(42, s.IRFId));
            Assert.Equal((int)AIScreeningSectionEnum.RoleFitment,
                skills.Single(s => s.SkillName == "Core Tech Stack").SkillSet);
            Assert.Equal((int)AIScreeningSectionEnum.MotivationToJoinFitment,
                skills.Single(s => s.SkillName == "Location Fit").SkillSet);
        }

        #endregion

        #region An off-topic revision is a valid response, not an error

        [Fact]
        public void BuildRevision_reports_no_change_with_a_message_when_the_plan_is_identical()
        {
            var before = Plan(
                new[] { Param("Core Tech Stack", 100, AIScreeningSectionEnum.RoleFitment) },
                new[] { Param("Location Fit", 100, AIScreeningSectionEnum.MotivationToJoinFitment) });

            var after = Plan(
                new[] { Param("Core Tech Stack", 100, AIScreeningSectionEnum.RoleFitment) },
                new[] { Param("Location Fit", 100, AIScreeningSectionEnum.MotivationToJoinFitment) });

            var revision = AIScreeningIntakeHelper.BuildRevision(
                before, after, "what is the weather like", weightsAdjusted: false);

            Assert.False(revision.Changed);
            Assert.False(string.IsNullOrWhiteSpace(revision.Message));
            Assert.Empty(revision.Added);
            Assert.Empty(revision.Removed);
        }

        [Fact]
        public void BuildRevision_labels_each_change_with_the_section_it_happened_in()
        {
            var before = Plan(
                new[]
                {
                    Param("Core Tech Stack", 50, AIScreeningSectionEnum.RoleFitment),
                    Param("Career Progression", 50, AIScreeningSectionEnum.RoleFitment)
                },
                new[] { Param("Location Fit", 100, AIScreeningSectionEnum.MotivationToJoinFitment) });

            var after = Plan(
                new[] { Param("Core Tech Stack", 100, AIScreeningSectionEnum.RoleFitment) },
                new[] { Param("Location Fit", 100, AIScreeningSectionEnum.MotivationToJoinFitment) });

            var revision = AIScreeningIntakeHelper.BuildRevision(
                before, after, "drop career progression", weightsAdjusted: false);

            Assert.True(revision.Changed);
            Assert.Contains(revision.Removed, r => r.StartsWith("Role Fitment:") && r.Contains("Career Progression"));
        }

        [Fact]
        public void BuildRevision_warns_when_a_revision_leaves_a_section_off_100()
        {
            var before = Plan(
                new[] { Param("Core Tech Stack", 100, AIScreeningSectionEnum.RoleFitment) },
                new[] { Param("Location Fit", 100, AIScreeningSectionEnum.MotivationToJoinFitment) });

            var after = Plan(
                new[] { Param("Core Tech Stack", 70, AIScreeningSectionEnum.RoleFitment) },
                new[] { Param("Location Fit", 100, AIScreeningSectionEnum.MotivationToJoinFitment) });

            var revision = AIScreeningIntakeHelper.BuildRevision(
                before, after, "lower the tech weight", weightsAdjusted: false);

            Assert.Contains(revision.Warnings, w => w.StartsWith("Role Fitment") && w.Contains("70"));
        }

        #endregion

        #region Scoring

        [Fact]
        public void ComputeSectionScore_gives_each_parameter_its_weight_times_score_over_five()
        {
            var ratings = new[]
            {
                new AIScreeningParameterRating { Weight = 60, Score = 5 },  // 60
                new AIScreeningParameterRating { Weight = 40, Score = 3 }   // 24
            };

            Assert.Equal(84m, AIScreeningIntakeHelper.ComputeSectionScore(ratings));
        }

        [Fact]
        public void ComputeSectionScore_is_100_when_every_parameter_scores_the_maximum()
        {
            var ratings = new[]
            {
                new AIScreeningParameterRating { Weight = 45, Score = 5 },
                new AIScreeningParameterRating { Weight = 35, Score = 5 },
                new AIScreeningParameterRating { Weight = 20, Score = 5 }
            };

            Assert.Equal(100m, AIScreeningIntakeHelper.ComputeSectionScore(ratings));
        }

        [Fact]
        public void ComputeOverallScore_is_the_mean_of_both_sections_not_section_ones_score()
        {
            // A candidate who fits the role perfectly but will not join must not read as a pass.
            Assert.Equal(50m, AIScreeningIntakeHelper.ComputeOverallScore(100m, 0m));
            Assert.Equal(62.5m, AIScreeningIntakeHelper.ComputeOverallScore(90m, 35m));
        }

        #endregion

        #region Parsing

        [Fact]
        public void ParsePlan_reads_both_sections_and_stamps_their_SkillSet()
        {
            const string json = @"{
              ""roleLabel"": ""Senior Backend Engineer - Bangalore"",
              ""durationMinutes"": 20,
              ""sections"": [
                { ""section"": ""RoleFitment"", ""parameters"": [
                    { ""Skill"": ""Core Tech Stack"", ""ProficiencyLevel"": ""Expert"", ""Weight"": 60, ""Mandatory"": ""yes"" },
                    { ""Skill"": ""Relevant Experience"", ""ProficiencyLevel"": ""Competent"", ""Weight"": 40, ""Mandatory"": ""no"" } ] },
                { ""section"": ""MotivationToJoinFitment"", ""parameters"": [
                    { ""Skill"": ""Compensation Feasibility"", ""ProficiencyLevel"": ""Competent"", ""Weight"": 100, ""Mandatory"": ""yes"" } ] }
              ] }";

            var plan = AIScreeningIntakeHelper.ParsePlan(json);

            Assert.NotNull(plan);
            Assert.Equal(20, plan.DurationMinutes);
            Assert.Equal(3, plan.ParameterCount);

            var role = plan.GetSection(AIScreeningSectionEnum.RoleFitment);
            Assert.Equal(100, role.WeightTotal);
            Assert.All(role.Parameters, p => Assert.Equal((int)AIScreeningSectionEnum.RoleFitment, p.SkillSet));
            Assert.True(role.Parameters.Single(p => p.SkillName == "Core Tech Stack").isEliminationskill);

            var motivation = plan.GetSection(AIScreeningSectionEnum.MotivationToJoinFitment);
            Assert.Single(motivation.Parameters);
            Assert.Equal((int)AIScreeningSectionEnum.MotivationToJoinFitment, motivation.Parameters[0].SkillSet);
        }

        [Fact]
        public void ParsePlan_always_returns_both_sections_even_when_the_model_omits_one()
        {
            const string json = @"{ ""sections"": [
                { ""section"": ""RoleFitment"", ""parameters"": [
                    { ""Skill"": ""Core Tech Stack"", ""ProficiencyLevel"": ""Expert"", ""Weight"": 100 } ] } ] }";

            var plan = AIScreeningIntakeHelper.ParsePlan(json);

            Assert.Equal(2, plan.Sections.Count);
            Assert.Empty(plan.GetSection(AIScreeningSectionEnum.MotivationToJoinFitment).Parameters);
        }

        [Fact]
        public void ParsePlan_accepts_the_object_shape_and_a_markdown_fenced_response()
        {
            const string json = @"```json
            { ""sections"": {
                ""Role Fitment"": [ { ""Skill"": ""Core Tech Stack"", ""ProficiencyLevel"": ""Expert"", ""Weight"": 100 } ],
                ""Motivation to Join Fitment"": [ { ""Skill"": ""Location Fit"", ""ProficiencyLevel"": ""Competent"", ""Weight"": 100 } ]
            } }
            ```";

            var plan = AIScreeningIntakeHelper.ParsePlan(json);

            Assert.Equal(2, plan.ParameterCount);
            Assert.Single(plan.GetSection(AIScreeningSectionEnum.MotivationToJoinFitment).Parameters);
        }

        [Fact]
        public void ParseIdealCandidateProfile_reads_the_profile_out_of_the_response()
        {
            const string json = @"{ ""idealCandidateProfile"": ""Java, Spring Boot | 5-8 yrs | Bangalore, hybrid"" }";

            Assert.Equal(
                "Java, Spring Boot | 5-8 yrs | Bangalore, hybrid",
                AIScreeningIntakeHelper.ParseIdealCandidateProfile(json));
        }

        [Fact]
        public void ParsePlan_returns_null_for_an_unparseable_response()
        {
            Assert.Null(AIScreeningIntakeHelper.ParsePlan("the model apologises and returns prose"));
        }

        #endregion
    }
}
