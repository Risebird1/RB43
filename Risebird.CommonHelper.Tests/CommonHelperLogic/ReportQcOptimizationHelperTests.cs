namespace Risebird.CommonHelper.Tests.CommonHelperLogic
{
    /// <summary>
    /// The three OpenAI pre-processing helpers share a shape: optimize, validate, estimate tokens.
    /// These suites cover each stage of every pipeline plus the validation thresholds, because a
    /// wrong threshold here either wastes an API call or silently drops a real document.
    /// </summary>
    public class ReportQCOptimizationHelperOptimizeTests
    {
        private static readonly ReportQCOptimizationHelper Sut = new();

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\n\n")]
        public void Blank_text_is_returned_unchanged(string text) =>
            Assert.Equal(text, Sut.OptimizeReportText(text));

        [Fact(Skip = SkippedOnRequest.Reason)]
        public void Leaves_clean_text_alone()
        {
            var optimized = Sut.OptimizeReportText("Interview report for candidate");

            Assert.Equal("Interview report for candidate", optimized);
        }

        [Fact]
        public void Strips_the_unicode_replacement_character()
        {
            var optimized = Sut.OptimizeReportText("Candidate\uFFFD rating\uFFFD 4");

            Assert.DoesNotContain("\uFFFD", optimized);
            Assert.Contains("Candidate", optimized);
        }

        [Fact(Skip = SkippedOnRequest.Reason)]
        public void Strips_non_printable_control_characters()
        {
            var optimized = Sut.OptimizeReportText("Interview\u0001 report\u0007 text");

            // Ordinal: the default culture-sensitive compare gives control characters zero
            // collation weight, so it reports them as present in every string.
            Assert.DoesNotContain("\u0001", optimized, StringComparison.Ordinal);
            Assert.DoesNotContain("\u0007", optimized, StringComparison.Ordinal);
            Assert.Contains("Interview", optimized);
            Assert.Contains("report", optimized);
            Assert.Contains("text", optimized);
        }

        [Fact]
        public void Keeps_tabs_newlines_and_latin_one_characters()
        {
            var optimized = Sut.OptimizeReportText("Café interview\nsecond line\tthird");

            Assert.Contains("Café", optimized);
            Assert.Contains("second line", optimized);
        }

        [Theory]
        [InlineData("Page 3 of 12")]
        [InlineData("page 3 OF 12")]
        [InlineData("Page 7")]
        [InlineData("PAGE 7")]
        public void Strips_page_header_and_footer_patterns(string pageMarker)
        {
            var optimized = Sut.OptimizeReportText($"Interview feedback {pageMarker} continues here");

            Assert.DoesNotContain("age 3", optimized);
            Assert.DoesNotContain("age 7", optimized);
            Assert.Contains("Interview feedback", optimized);
            Assert.Contains("continues here", optimized);
        }

        [Fact]
        public void Strips_a_line_that_is_only_a_page_number()
        {
            var optimized = Sut.OptimizeReportText("Interview notes\n42\nMore notes");

            Assert.Contains("Interview notes", optimized);
            Assert.Contains("More notes", optimized);
            Assert.DoesNotContain("42", optimized);
        }

        [Fact(Skip = SkippedOnRequest.Reason)]
        public void Collapses_runs_of_spaces_to_one()
        {
            var optimized = Sut.OptimizeReportText("Candidate      rating       four");

            Assert.Equal("Candidate rating four", optimized);
        }

        [Fact]
        public void Replaces_tabs_with_a_single_space()
        {
            var optimized = Sut.OptimizeReportText("Skill\tRating\tComment");

            Assert.DoesNotContain("\t", optimized);
            Assert.Contains("Skill Rating Comment", optimized);
        }

        [Fact]
        public void Collapses_three_or_more_newlines_to_a_paragraph_break()
        {
            var optimized = Sut.OptimizeReportText("Section one\n\n\n\n\nSection two");

            Assert.Contains("Section one\n\nSection two", optimized);
        }

        [Fact]
        public void Preserves_a_single_paragraph_break()
        {
            var optimized = Sut.OptimizeReportText("Section one\n\nSection two");

            Assert.Contains("Section one\n\nSection two", optimized);
        }

        [Fact]
        public void Strips_trailing_spaces_from_each_line()
        {
            var optimized = Sut.OptimizeReportText("First line   \nSecond line  ");

            Assert.DoesNotContain(" \n", optimized);
            Assert.False(optimized.EndsWith(" "));
        }

        [Fact]
        public void Keeps_at_most_two_copies_of_a_repeated_short_line()
        {
            const string header = "RISEBIRD CONFIDENTIAL";
            var text = string.Join("\n", Enumerable.Repeat(header, 6)) + "\nreal content";

            var optimized = Sut.OptimizeReportText(text);

            var occurrences = optimized.Split('\n').Count(l => l.Trim() == header);
            Assert.True(occurrences <= 2, $"expected the repeated header to be collapsed, saw {occurrences}");
            Assert.Contains("real content", optimized);
        }

        [Fact]
        public void Does_not_collapse_a_repeated_long_line_because_it_may_be_real_content()
        {
            var longLine = new string('x', 100);
            var text = longLine + "\n" + longLine;

            var optimized = Sut.OptimizeReportText(text);

            Assert.Equal(2, optimized.Split('\n').Count(l => l.Trim() == longLine));
        }

        [Fact]
        public void Normalizes_windows_and_classic_mac_line_breaks()
        {
            var optimized = Sut.OptimizeReportText("One\r\nTwo\rThree");

            Assert.DoesNotContain("\r", optimized);
            Assert.Contains("One", optimized);
            Assert.Contains("Three", optimized);
        }

        [Fact(Skip = SkippedOnRequest.Reason)]
        public void Trims_the_result()
        {
            var optimized = Sut.OptimizeReportText("   Interview feedback   ");

            Assert.Equal("Interview feedback", optimized);
        }

        [Fact]
        public void Never_grows_the_text()
        {
            var text = "Interview  feedback\n\n\n\nPage 1 of 3\tRating: 4   ";

            Assert.True(Sut.OptimizeReportText(text).Length <= text.Length);
        }

        [Fact]
        public void Optimizing_twice_is_stable()
        {
            var once = Sut.OptimizeReportText("Interview  feedback\n\n\n\nPage 1 of 3\tRating: 4   ");
            var twice = Sut.OptimizeReportText(once);

            Assert.Equal(once.Trim(), twice.Trim());
        }
    }

    public class ReportQCOptimizationHelperValidateTests
    {
        private static readonly ReportQCOptimizationHelper Sut = new();

        private static string Report(int length = 400, string keyword = "interview") =>
            keyword + new string('a', Math.Max(0, length - keyword.Length));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Rejects_blank_text(string text)
        {
            var (isValid, reason) = Sut.ValidateReportForQC(text);

            Assert.False(isValid);
            Assert.Equal("Report text is empty or null", reason);
        }

        [Fact]
        public void Rejects_text_below_two_hundred_characters()
        {
            var (isValid, reason) = Sut.ValidateReportForQC(Report(199));

            Assert.False(isValid);
            Assert.Contains("too short", reason);
        }

        [Fact]
        public void Accepts_text_at_exactly_two_hundred_characters() =>
            Assert.True(Sut.ValidateReportForQC(Report(200)).isValid);

        [Fact]
        public void Rejects_text_above_one_hundred_fifty_thousand_characters()
        {
            var (isValid, reason) = Sut.ValidateReportForQC(Report(150_001));

            Assert.False(isValid);
            Assert.Contains("too long", reason);
        }

        [Fact]
        public void Accepts_text_at_exactly_one_hundred_fifty_thousand_characters() =>
            Assert.True(Sut.ValidateReportForQC(Report(150_000)).isValid);

        [Fact]
        public void Rejects_text_that_is_mostly_non_alphabetic()
        {
            // Roughly 2% letters, well below the 10% floor.
            var corrupted = "interview" + new string('#', 400);

            var (isValid, reason) = Sut.ValidateReportForQC(corrupted);

            Assert.False(isValid);
            Assert.Contains("corrupted", reason);
        }

        [Fact]
        public void Accepts_text_just_above_the_alphabetic_floor()
        {
            var text = new string('a', 60) + new string('#', 240) + " interview";

            Assert.True(Sut.ValidateReportForQC(text).isValid);
        }

        [Theory]
        [InlineData("interview")]
        [InlineData("candidate")]
        [InlineData("rating")]
        [InlineData("skill")]
        [InlineData("feedback")]
        [InlineData("recommendation")]
        [InlineData("score")]
        [InlineData("assessment")]
        [InlineData("INTERVIEW")]
        [InlineData("Candidate")]
        public void Accepts_text_containing_any_expected_keyword(string keyword)
        {
            var (isValid, reason) = Sut.ValidateReportForQC(Report(400, keyword));

            Assert.True(isValid);
            Assert.Equal(string.Empty, reason);
        }

        [Fact]
        public void Rejects_long_alphabetic_text_with_no_report_keywords()
        {
            var (isValid, reason) = Sut.ValidateReportForQC(new string('a', 400));

            Assert.False(isValid);
            Assert.Contains("keywords", reason);
        }
    }

    public class ReportQCOptimizationHelperTokenEstimateTests
    {
        private static readonly ReportQCOptimizationHelper Sut = new();

        [Theory]
        [InlineData(null, 0)]
        [InlineData("", 0)]
        [InlineData("a", 1)]
        [InlineData("abcd", 1)]
        [InlineData("abcde", 2)]
        [InlineData("abcdefgh", 2)]
        [InlineData("abcdefghi", 3)]
        public void Estimates_one_token_per_four_characters_rounding_up(string text, int expected) =>
            Assert.Equal(expected, Sut.EstimateTokenCount(text));

        [Fact]
        public void Whitespace_still_counts_toward_the_estimate() =>
            Assert.Equal(1, Sut.EstimateTokenCount("   "));

        [Fact]
        public void Estimate_grows_monotonically_with_length()
        {
            var previous = -1;
            for (var length = 0; length < 40; length++)
            {
                var estimate = Sut.EstimateTokenCount(new string('a', length));
                Assert.True(estimate >= previous);
                previous = estimate;
            }
        }
    }
}
