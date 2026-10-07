using Newtonsoft.Json.Linq;
using Risebird.Entities.ViewModel;

namespace Risebird.CommonHelper.Tests.CommonHelperLogic
{
    /// <summary>
    /// Advanced resume screening speaks to stored OpenAI prompts shared with screen.risebird.io, so
    /// the payload keys are a contract and the response keys are read by name. These pin both,
    /// along with the verdict scale every list and report relies on.
    /// </summary>
    public class AdvancedResumeScreeningTests
    {
        private static ResumeScreeningCriteriaVM FullRubric()
        {
            var c = new ResumeScreeningCriteriaVM { IRFId = 7, IRFName = "IRF-7", JDTiltle = "Java Developer" };
            foreach (var row in AdvancedResumeScreening.BuildCriteriaRows(c))
            {
                typeof(ResumeScreeningCriteriaVM).GetProperty(row.TextField)!.SetValue(c, row.Label + " text");
                typeof(ResumeScreeningCriteriaVM).GetProperty(row.ScoreField)!.SetValue(c, 10);
            }
            return c;
        }

        // ------------------------------------------------------------------ verdict

        [Theory]
        [InlineData("Strongly Recommended", "Strongly Recommended")]
        [InlineData("strongly recommend", "Strongly Recommended")]
        [InlineData("Not Recommended", "Not Recommended")]
        [InlineData("Recommended", "Recommended")]
        [InlineData("Consider", "Consider")]
        [InlineData("Excellent", "Strongly Recommended")]
        [InlineData("Good", "Recommended")]
        [InlineData("Average", "Consider")]
        [InlineData("Below Average", "Not Recommended")]
        [InlineData("Poor", "Not Recommended")]
        [InlineData("Maybe", "Unclassified")]
        [InlineData("", "Unclassified")]
        [InlineData(null, "Unclassified")]
        public void Verdicts_from_either_scale_land_on_one_vocabulary(string raw, string expected)
        {
            Assert.Equal(expected, AdvancedResumeScreening.NormalizeVerdict(raw));
        }

        // ---------------------------------------------------------------- payload

        [Fact]
        public void The_payload_carries_every_criterion_under_the_prompt_keys()
        {
            var json = JObject.FromObject(AdvancedResumeScreening.BuildScreeningInput(FullRubric()));

            // The quirky mappings are the contract with the stored prompt.
            Assert.Equal("Target Company Percentile text", (string)json["Work_Experience"]!["targetCompanies"]);
            Assert.Equal("Current Company Credentials Match text", (string)json["Work_Experience"]!["currentCompanyCredentialsMatch"]);
            Assert.Equal("Achievement in Reputed Public Exam text", (string)json["Education"]!["achievementReputedPublicExam"]);
            Assert.Equal("Additional Academic Qualifications text", (string)json["Education"]!["additionalAcademicQualifications"]);
            Assert.Equal("Industry Certifications text", (string)json["Education"]!["industryCertifications"]);

            // The three MCQ only sends on its complete rubric are always sent here.
            Assert.Equal("Seniority Level text", (string)json["Skills_Section"]!["seniorityLevel"]);
            Assert.Equal(10, (int)json["Skills_Section"]!["seniorityLevel_Score"]!);
            Assert.Equal("Company Genuinity text", (string)json["Work_Experience"]!["Current_Company_Genuinity"]);
            Assert.Equal("Employment Type text", (string)json["Work_Experience"]!["Employment_Type"]);

            // Every one of the 26 weights reaches the payload.
            var weights = json.Properties()
                .SelectMany(section => ((JObject)section.Value).Properties())
                .Where(p => p.Name.EndsWith("_Score"))
                .Sum(p => (int)p.Value);
            Assert.Equal(26 * 10, weights);
        }

        [Fact]
        public void A_weighted_criterion_without_a_description_is_sent_switched_off()
        {
            var c = FullRubric();
            c.KPIs = "   ";
            c.KPIsScore = 25;
            c.EducationCertification = null;
            c.EducationCertificateScore = 15;

            var input = AdvancedResumeScreening.BuildScreeningInput(c);

            Assert.Equal("", input.Work_Experience.KPIs);
            Assert.Equal(0, input.Work_Experience.KPIs_Score);
            Assert.Equal("", input.Education.industryCertifications);
            Assert.Equal(0, input.Education.industryCertifications_Score);
        }

        [Fact]
        public void A_negative_weight_is_never_sent()
        {
            var c = FullRubric();
            c.RelevantToolsScore = -5;

            Assert.Equal(0, AdvancedResumeScreening.BuildScreeningInput(c).Work_Experience.relevantTools_Score);
        }

        // ------------------------------------------------------- generated rubric

        [Fact]
        public void A_generated_rubric_round_trips_through_the_review_record()
        {
            var original = AdvancedResumeScreening.BuildScreeningInput(FullRubric());

            var target = new ResumeScreeningCriteriaVM();
            AdvancedResumeScreening.ApplyGeneratedCriteria(original, target);
            var again = AdvancedResumeScreening.BuildScreeningInput(target);

            Assert.True(JToken.DeepEquals(JObject.FromObject(original), JObject.FromObject(again)));
        }

        [Fact]
        public void Review_rows_bind_to_real_properties_and_overlay_keeps_the_context()
        {
            var edited = FullRubric();
            var stored = new ResumeScreeningCriteriaVM { IRFId = 7, IRFName = "IRF-7", JDLink = "https://x/jd.pdf" };

            AdvancedResumeScreening.OverlayEditedCriteria(edited, stored);

            Assert.Equal("https://x/jd.pdf", stored.JDLink);
            foreach (var row in AdvancedResumeScreening.BuildCriteriaRows(stored))
            {
                Assert.NotNull(typeof(ResumeScreeningCriteriaVM).GetProperty(row.TextField));
                Assert.Equal(row.Label + " text", row.Text);
                Assert.Equal(10, row.Score);
            }
        }

        [Fact]
        public void Overlong_text_is_reported_and_clamped_to_the_column()
        {
            var c = FullRubric();
            c.Title = new string('t', 301);
            c.KRAs = new string('k', 2001);

            var overlong = AdvancedResumeScreening.OverlongCriteria(c);
            Assert.Equal(2, overlong.Count);

            AdvancedResumeScreening.ClampToColumnWidths(c);
            Assert.Equal(300, c.Title.Length);
            Assert.Equal(2000, c.KRAs.Length);
            Assert.Empty(AdvancedResumeScreening.OverlongCriteria(c));
        }

        // ----------------------------------------------------------------- result

        private const string ScreenerReply = """
            ```json
            {
              "overallverdict": "Recommended",
              "overallfitmentpercentage": "72.6%",
              "overallFitmentScore": "190/260",
              "overallFitmentAnalysis": "Solid Java background.",
              "skillsSection": {
                "primarySkillsMatch": { "matched": true, "actualscore": "28", "maxscore": "30", "reason": "Spring Boot in two roles." },
                "skillsSectionScore": "60", "skillsSectionEffectiveMax": "80"
              },
              "workExperienceSection": {
                "currentProjectRelevanceCount": { "matched": true, "actualscore": "8", "maxscore": "10", "observation": "Current project is relevant." },
                "careerGraphCount": { "matched": false, "actualscore": "2", "maxscore": "10", "reason": "Flat career graph." }
              },
              "educationSection": {
                "educationType": { "matched": false, "actualscore": "0", "maxscore": "5" }
              }
            }
            ```
            """;

        [Fact]
        public void A_fenced_screener_reply_parses()
        {
            var r = AdvancedResumeScreening.ParseModelJson<AdvancedScreeningResponse>(ScreenerReply);

            Assert.NotNull(r);
            Assert.Equal("Recommended", r!.overallverdict);
            Assert.Equal("Current project is relevant.", r.workExperienceSection.currentProjectRelevanceCount.reasonOrObservation);
        }

        [Fact]
        public void Unreadable_replies_parse_to_null_rather_than_throwing()
        {
            Assert.Null(AdvancedResumeScreening.ParseModelJson<AdvancedScreeningResponse>("not json"));
            Assert.Null(AdvancedResumeScreening.ParseModelJson<AdvancedScreeningResponse>(null));
        }

        [Theory]
        [InlineData("72.6%", 73)]
        [InlineData("72", 72)]
        [InlineData(" 100 ", 100)]
        [InlineData("140", 100)]
        [InlineData("-3", 0)]
        [InlineData("n/a", 0)]
        [InlineData(null, 0)]
        public void Fitment_percentage_is_a_whole_number_from_0_to_100(string raw, int expected)
        {
            Assert.Equal(expected, AdvancedResumeScreening.ParsePercentage(raw));
        }

        [Fact]
        public void Career_graph_is_read_from_its_own_key()
        {
            // MCQ's results page rendered Career Graph from currentProjectRelevanceCount.
            var r = AdvancedResumeScreening.ParseModelJson<AdvancedScreeningResponse>(ScreenerReply);

            var career = AdvancedResumeScreening.ScoredCriteria(r).Single(c => c.Label == "Career Graph");

            Assert.Equal("Flat career graph.", career.Info.reasonOrObservation);
        }

        [Fact]
        public void The_report_shape_carries_percentage_verdict_and_one_row_per_scored_criterion()
        {
            var r = AdvancedResumeScreening.ParseModelJson<AdvancedScreeningResponse>(ScreenerReply);

            var report = AdvancedResumeScreening.ToResumeEvaluation(r)!;

            Assert.Equal(73, report.TotalScore);
            Assert.Equal(100, report.MaxScore);
            Assert.Equal("Recommended", report.Verdict);
            Assert.Equal("Solid Java background.", report.FitmentSummary);
            Assert.Equal(4, report.EvaluationCriteriaScores.Count);
            var primary = report.EvaluationCriteriaScores.Single(s => s.CriterionId == "Primary Skills");
            Assert.Equal(28, primary.Score);
            Assert.Equal(30, primary.MaxScore);
            Assert.Equal("Spring Boot in two roles.", primary.Reason);
        }

        [Fact]
        public void No_result_maps_to_no_report_section()
        {
            Assert.Null(AdvancedResumeScreening.ToResumeEvaluation(null));
        }
    }
}
