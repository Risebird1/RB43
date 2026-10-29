using Risebird.Entities.ViewModel;

namespace Risebird.Business.Tests.AIScreeningTests
{
    /// <summary>
    /// The report's Hands-on Skill Evaluation tiles showed the evaluator's raw overall scores with a
    /// "%" appended: a 3 on the 1-5 communication scale read "3 %", and an overall_technical_score of
    /// 0 read "—" even though all 18 questions were rated.
    /// </summary>
    public class AIInterviewEvaluationScorePercentTests
    {
        // Shape of the evaluation saved for session 06fe3e20-f8ec-4b10-a43e-10d874ca2e80 (trimmed).
        private const string ReportWithZeroOverallTechnical = @"{
  ""question_ratings"": [
    { ""question"": ""Q1"", ""score"": 2, ""communication_score"": 3 },
    { ""question"": ""Q2"", ""score"": 1, ""communication_score"": 2 },
    { ""question"": ""Q3"", ""score"": 5, ""communication_score"": 4 }
  ],
  ""rubric_score_summary"": [
    { ""rubric_item"": ""Lead Generation"", ""score"": 2, ""max_score"": 5 }
  ],
  ""overall_technical_score"": 0,
  ""final_verdict"": ""Marginally Rejected"",
  ""overall_communication_score"": 3
}";

        [Fact]
        public void Percentages_come_from_the_question_ratings_not_the_raw_overall_fields()
        {
            var eval = AIInterviewEvaluation.TryParse(ReportWithZeroOverallTechnical);

            Assert.NotNull(eval.TechnicalScorePercent);
            Assert.Equal(53.3f, eval.TechnicalScorePercent.Value, 1);      // avg 8/3 of 5
            Assert.Equal(60f, eval.CommunicationScorePercent.Value, 1);    // avg 9/3 of 5
        }

        [Fact]
        public void Rubric_is_used_when_no_question_is_rated()
        {
            var eval = AIInterviewEvaluation.TryParse(@"{
  ""rubric_score_summary"": [
    { ""rubric_item"": ""A"", ""score"": 2, ""max_score"": 5 },
    { ""rubric_item"": ""B"", ""score"": 3, ""max_score"": 5 }
  ],
  ""overall_technical_score"": 0,
  ""final_verdict"": ""Marginally Rejected""
}");

            Assert.Equal(50f, eval.TechnicalScorePercent.Value, 1);
            Assert.Null(eval.CommunicationScorePercent);
        }

        [Theory]
        [InlineData(3f, 60f)]     // 1-5 scale
        [InlineData(7f, 70f)]     // 0-10 scale
        [InlineData(72f, 72f)]    // already a percentage
        public void Overall_field_is_scaled_when_it_is_all_there_is(float overall, float expected)
        {
            var eval = new AIInterviewEvaluation { OverallTechnicalScore = overall, OverallCommunicationScore = overall };

            Assert.Equal(expected, eval.TechnicalScorePercent.Value, 1);
            Assert.Equal(expected, eval.CommunicationScorePercent.Value, 1);
        }

        [Fact]
        public void Nothing_rated_shows_no_percentage()
        {
            var eval = new AIInterviewEvaluation();

            Assert.Null(eval.TechnicalScorePercent);
            Assert.Null(eval.CommunicationScorePercent);
        }
    }
}
