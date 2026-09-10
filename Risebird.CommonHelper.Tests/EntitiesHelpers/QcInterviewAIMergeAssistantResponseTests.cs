using Risebird.Entities.Data;

namespace Risebird.CommonHelper.Tests.EntitiesHelpers
{
    public class QcInterviewAIMergeAssistantResponseTryParseTests
    {
        [Fact]
        public void Parses_a_plain_object()
        {
            var json = """
                {"interviewer_suspicious":"Yes","RedoInterview":true,"total_penalty":15,
                 "total_qcmerge_score":42,"qcmerge_verdict":"fail",
                 "flags":[{"category":"Rubric","flag":"skipped"}]}
                """;

            var ok = QcInterviewAIMergeAssistantResponse.TryParseFromAssistantText(json, out var result);

            Assert.True(ok);
            Assert.Equal("Yes", result.interviewer_suspicious);
            Assert.True(result.RedoInterview);
            Assert.Equal(15, result.total_penalty);
            Assert.Equal(42, result.total_qcmerge_score);
            Assert.Equal("fail", result.qcmerge_verdict);
            Assert.Single(result.flags);
        }

        [Fact]
        public void Strips_a_markdown_code_fence()
        {
            var json = "```json\n{\"interviewer_suspicious\":\"Yes\"}\n```";

            var ok = QcInterviewAIMergeAssistantResponse.TryParseFromAssistantText(json, out var result);

            Assert.True(ok);
            Assert.Equal("Yes", result.interviewer_suspicious);
        }

        [Fact]
        public void Strips_an_unlabelled_code_fence()
        {
            var json = "```\n{\"RedoInterview\":true}\n```";

            var ok = QcInterviewAIMergeAssistantResponse.TryParseFromAssistantText(json, out var result);

            Assert.True(ok);
            Assert.True(result.RedoInterview);
        }

        [Fact]
        public void Tolerates_surrounding_whitespace()
        {
            var ok = QcInterviewAIMergeAssistantResponse.TryParseFromAssistantText(
                "\n\n  {\"qcmerge_verdict\":\"pass\"}  \n\n", out var result);

            Assert.True(ok);
            Assert.Equal("pass", result.qcmerge_verdict);
        }

        [Fact]
        public void Parses_the_legacy_bare_array_of_flags()
        {
            var json = """[{"category":"Rubric","flag":"skipped"},{"category":"Timing","flag":"short"}]""";

            var ok = QcInterviewAIMergeAssistantResponse.TryParseFromAssistantText(json, out var result);

            Assert.True(ok);
            Assert.Equal(2, result.flags.Count);
            Assert.Null(result.interviewer_suspicious);
        }

        [Fact]
        public void An_empty_legacy_array_is_not_a_successful_parse()
        {
            var ok = QcInterviewAIMergeAssistantResponse.TryParseFromAssistantText("[]", out var result);

            Assert.False(ok);
            Assert.NotNull(result);
            Assert.Empty(result.flags);
        }

        [Fact]
        public void An_object_with_no_recognised_field_is_not_a_successful_parse()
        {
            var ok = QcInterviewAIMergeAssistantResponse.TryParseFromAssistantText(
                """{"unrelated":"value"}""", out _);

            Assert.False(ok);
        }

        [Fact]
        public void An_object_carrying_only_a_penalty_is_a_successful_parse()
        {
            var ok = QcInterviewAIMergeAssistantResponse.TryParseFromAssistantText(
                """{"total_penalty":0}""", out var result);

            Assert.True(ok);
            Assert.Equal(0, result.total_penalty);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\n\t")]
        public void Blank_input_fails_and_yields_no_result(string raw)
        {
            var ok = QcInterviewAIMergeAssistantResponse.TryParseFromAssistantText(raw, out var result);

            Assert.False(ok);
            Assert.Null(result);
        }

        [Theory]
        [InlineData("not json at all")]
        [InlineData("{")]
        [InlineData("{\"a\":}")]
        [InlineData("```json\nnot json\n```")]
        public void Malformed_json_fails_without_throwing(string raw)
        {
            var exception = Record.Exception(() =>
            {
                QcInterviewAIMergeAssistantResponse.TryParseFromAssistantText(raw, out _);
            });

            Assert.Null(exception);
        }

        [Fact]
        public void A_bare_json_scalar_is_not_a_successful_object_parse()
        {
            // Neither object nor array — falls through to the second attempt.
            var exception = Record.Exception(() =>
                QcInterviewAIMergeAssistantResponse.TryParseFromAssistantText("\"just a string\"", out _));

            Assert.Null(exception);
        }
    }

    public class QcInterviewAIMergeAssistantResponseFromMergedDetailsJsonTests
    {
        [Fact]
        public void Deserializes_the_canonical_stored_json()
        {
            var result = QcInterviewAIMergeAssistantResponse.FromMergedDetailsJson(
                """{"interviewer_suspicious":"Yes","RedoInterview":false,"total_penalty":10}""");

            Assert.NotNull(result);
            Assert.Equal("Yes", result.interviewer_suspicious);
            Assert.False(result.RedoInterview);
            Assert.Equal(10, result.total_penalty);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Returns_null_for_blank_json(string json) =>
            Assert.Null(QcInterviewAIMergeAssistantResponse.FromMergedDetailsJson(json));

        [Theory]
        [InlineData("not json")]
        [InlineData("{")]
        [InlineData("[]")]
        [InlineData("[1,2,3]")]
        public void Returns_null_for_unparseable_json(string json) =>
            Assert.Null(QcInterviewAIMergeAssistantResponse.FromMergedDetailsJson(json));

        [Fact]
        public void Leaves_missing_fields_null_rather_than_defaulting_them()
        {
            var result = QcInterviewAIMergeAssistantResponse.FromMergedDetailsJson("{}");

            Assert.NotNull(result);
            Assert.Null(result.interviewer_suspicious);
            Assert.Null(result.RedoInterview);
            Assert.Null(result.total_penalty);
            Assert.Null(result.total_qcmerge_score);
            Assert.Null(result.qcmerge_verdict);
            Assert.Null(result.flags);
        }

        [Fact]
        public void Round_trips_with_the_assistant_text_parser()
        {
            var json = """{"interviewer_suspicious":"Yes","RedoInterview":true}""";

            QcInterviewAIMergeAssistantResponse.TryParseFromAssistantText(json, out var fromText);
            var fromStore = QcInterviewAIMergeAssistantResponse.FromMergedDetailsJson(json);

            Assert.Equal(fromText.interviewer_suspicious, fromStore.interviewer_suspicious);
            Assert.Equal(fromText.RedoInterview, fromStore.RedoInterview);
        }
    }
}
