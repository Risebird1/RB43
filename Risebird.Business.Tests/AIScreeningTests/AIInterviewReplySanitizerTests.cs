using Risebird.Entities.ViewModel;

namespace Risebird.Business.Tests.AIScreeningTests
{
    /// <summary>
    /// A candidate must never hear the report read aloud. These cover the shapes actually seen in
    /// production: the report JSON followed by a closing line, and a turn that is nothing but JSON.
    /// </summary>
    public class AIInterviewReplySanitizerTests
    {
        private const string ReportJson =
            "{ \"question_ratings\": [ { \"question\": \"Tell me about a time you acquired a client.\", " +
            "\"verdict\": \"Incorrect\", \"score\": 0, \"comment\": \"No relevant experience.\" } ], " +
            "\"rubric_score_summary\": [ { \"rubric_item\": \"Client Acquisition\", \"score\": 0 } ], " +
            "\"overall_technical_score\": 0, \"final_verdict\": \"Not Recommended\" }";

        [Fact]
        public void A_normal_question_is_left_completely_alone()
        {
            const string question = "Can you walk me through how you handled a difficult stakeholder?";

            var result = AIInterviewReplySanitizer.Strip(question, out var stripped);

            Assert.False(stripped);
            Assert.Equal(question, result);
        }

        [Fact]
        public void Prose_mentioning_a_score_conversationally_is_not_treated_as_a_payload()
        {
            const string question = "Your final verdict on that architecture — what score would you give it?";

            var result = AIInterviewReplySanitizer.Strip(question, out var stripped);

            Assert.False(stripped);
            Assert.Equal(question, result);
        }

        [Fact]
        public void The_report_json_is_removed_and_the_closing_line_is_kept()
        {
            // Exactly what was rendered on the report page: the blob, then the spoken line.
            var reply = ReportJson + "\n\nI'll move to the next question.";

            var result = AIInterviewReplySanitizer.Strip(reply, out var stripped);

            Assert.True(stripped);
            Assert.Equal("I'll move to the next question.", result);
            Assert.DoesNotContain("question_ratings", result);
        }

        [Fact]
        public void Prose_before_the_json_is_kept_too()
        {
            var reply = "Thanks for that. " + ReportJson;

            var result = AIInterviewReplySanitizer.Strip(reply, out var stripped);

            Assert.True(stripped);
            Assert.Equal("Thanks for that.", result);
        }

        [Fact]
        public void A_turn_that_is_nothing_but_json_falls_back_to_a_neutral_line()
        {
            var result = AIInterviewReplySanitizer.StripOrFallback(ReportJson, out var stripped);

            Assert.True(stripped);
            Assert.Equal(AIInterviewReplySanitizer.FallbackReply, result);
            Assert.DoesNotContain("{", result);
        }

        [Fact]
        public void A_brace_inside_a_quoted_comment_does_not_cut_the_object_short()
        {
            var reply =
                "{ \"question_ratings\": [ { \"comment\": \"They wrote a closing brace } in the answer\" } ], " +
                "\"final_verdict\": \"Recommended\" } Now, my next question.";

            var result = AIInterviewReplySanitizer.Strip(reply, out var stripped);

            Assert.True(stripped);
            Assert.Equal("Now, my next question.", result);
        }

        [Fact]
        public void An_escaped_quote_does_not_break_the_scan()
        {
            var reply =
                "{ \"final_verdict\": \"He said \\\"no idea\\\" outright\", \"llm_detection\": {} } Next question please.";

            var result = AIInterviewReplySanitizer.Strip(reply, out var stripped);

            Assert.True(stripped);
            Assert.Equal("Next question please.", result);
        }

        [Fact]
        public void Truncated_json_is_dropped_rather_than_spoken()
        {
            var reply = "Right. { \"question_ratings\": [ { \"score\": 0, \"comment\": \"cut off mid";

            var result = AIInterviewReplySanitizer.Strip(reply, out var stripped);

            Assert.True(stripped);
            Assert.Equal("Right.", result);
            Assert.DoesNotContain("question_ratings", result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Empty_turns_pass_through_untouched(string reply)
        {
            var result = AIInterviewReplySanitizer.Strip(reply, out var stripped);

            Assert.False(stripped);
            Assert.Equal(reply, result);
        }

        [Fact]
        public void Report_shaped_json_without_evaluation_keys_is_left_alone()
        {
            // Only evaluation-only keys trigger this. Anything else is none of its business.
            const string reply = "Here is a config example: { \"timeout\": 30, \"retries\": 2 }";

            var result = AIInterviewReplySanitizer.Strip(reply, out var stripped);

            Assert.False(stripped);
            Assert.Equal(reply, result);
        }
    }
}
