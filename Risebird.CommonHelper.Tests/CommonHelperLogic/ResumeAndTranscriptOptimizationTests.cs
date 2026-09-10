namespace Risebird.CommonHelper.Tests.CommonHelperLogic
{
    public class ResumeOptimizationHelperOptimizeTests
    {
        private static readonly ResumeOptimizationHelper Sut = new();

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Blank_text_is_returned_unchanged(string text) =>
            Assert.Equal(text, Sut.OptimizeResumeText(text));

        [Theory]
        [InlineData("References available upon request")]
        [InlineData("References available on request")]
        [InlineData("References upon request")]
        [InlineData("Available upon request")]
        [InlineData("Confidential Resume")]
        [InlineData("Curriculum Vitae")]
        [InlineData("REFERENCES AVAILABLE UPON REQUEST")]
        [InlineData("curriculum vitae")]
        public void Strips_boilerplate_phrases(string boilerplate)
        {
            var optimized = Sut.OptimizeResumeText($"Ada Lovelace\n{boilerplate}\nSkills: C#");

            Assert.DoesNotContain(boilerplate, optimized, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Ada Lovelace", optimized);
            Assert.Contains("Skills: C#", optimized);
        }

        [Fact]
        public void Strips_page_markers()
        {
            var optimized = Sut.OptimizeResumeText("Ada Lovelace\nPage 2 of 4\nSkills: C#");

            Assert.DoesNotContain("Page 2 of 4", optimized);
            Assert.Contains("Skills: C#", optimized);
        }

        [Fact]
        public void Strips_repeated_header_dates()
        {
            var optimized = Sut.OptimizeResumeText("Ada Lovelace\n01/02/2026\nSkills: C#");

            Assert.DoesNotContain("01/02/2026", optimized);
            Assert.Contains("Skills: C#", optimized);
        }

        [Fact(Skip = SkippedOnRequest.Reason)]
        public void Collapses_runs_of_spaces()
        {
            var optimized = Sut.OptimizeResumeText("Ada     Lovelace");

            Assert.Equal("Ada Lovelace", optimized);
        }

        [Fact]
        public void Replaces_tabs_with_a_single_space()
        {
            var optimized = Sut.OptimizeResumeText("Skill\tYears\tLevel");

            Assert.DoesNotContain("\t", optimized);
            Assert.Contains("Skill Years Level", optimized);
        }

        [Fact]
        public void Collapses_three_or_more_newlines_to_a_paragraph_break()
        {
            var optimized = Sut.OptimizeResumeText("Experience\n\n\n\n\nEducation");

            Assert.Contains("Experience\n\nEducation", optimized);
        }

        [Fact(Skip = SkippedOnRequest.Reason)]
        public void Trims_the_result()
        {
            Assert.Equal("Ada Lovelace", Sut.OptimizeResumeText("   Ada Lovelace   "));
        }

        [Fact]
        public void Collapses_a_repeated_short_header_line()
        {
            var text = string.Join("\n", Enumerable.Repeat("CONFIDENTIAL", 5)) + "\nExperience: 5 years";

            var optimized = Sut.OptimizeResumeText(text);

            Assert.True(optimized.Split('\n').Count(l => l.Trim() == "CONFIDENTIAL") <= 2);
            Assert.Contains("Experience: 5 years", optimized);
        }

        [Fact]
        public void Does_not_collapse_a_repeated_line_of_fifty_characters_or_more()
        {
            var longLine = new string('x', 60);

            var optimized = Sut.OptimizeResumeText(longLine + "\n" + longLine);

            Assert.Equal(2, optimized.Split('\n').Count(l => l.Trim() == longLine));
        }

        [Fact]
        public void Normalizes_line_breaks()
        {
            var optimized = Sut.OptimizeResumeText("Ada\r\nLovelace\rSkills");

            Assert.DoesNotContain("\r", optimized);
            Assert.Contains("Ada", optimized);
            Assert.Contains("Skills", optimized);
        }

        [Fact]
        public void Never_grows_the_text()
        {
            var text = "Ada   Lovelace\n\n\n\nReferences available upon request\tSkills   ";

            Assert.True(Sut.OptimizeResumeText(text).Length <= text.Length);
        }

        [Fact]
        public void Optimizing_twice_is_stable()
        {
            var once = Sut.OptimizeResumeText("Ada   Lovelace\n\n\n\nCurriculum Vitae\tSkills   ");

            Assert.Equal(once.Trim(), Sut.OptimizeResumeText(once).Trim());
        }
    }

    public class ResumeOptimizationHelperValidateTests
    {
        private static readonly ResumeOptimizationHelper Sut = new();

        private static string Resume(int length = 300, string keyword = "experience") =>
            keyword + new string('a', Math.Max(0, length - keyword.Length));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Rejects_blank_resume_text(string text)
        {
            var (isValid, reason) = Sut.ValidateResumeForParsing(text);

            Assert.False(isValid);
            Assert.Equal("Resume text is empty or null", reason);
        }

        [Fact]
        public void Rejects_a_resume_below_one_hundred_characters()
        {
            var (isValid, reason) = Sut.ValidateResumeForParsing(Resume(99));

            Assert.False(isValid);
            Assert.Contains("too short", reason);
        }

        [Fact]
        public void Accepts_a_resume_at_exactly_one_hundred_characters() =>
            Assert.True(Sut.ValidateResumeForParsing(Resume(100)).isValid);

        [Fact]
        public void Rejects_a_resume_above_fifty_thousand_characters()
        {
            var (isValid, reason) = Sut.ValidateResumeForParsing(Resume(50_001));

            Assert.False(isValid);
            Assert.Contains("too long", reason);
        }

        [Fact]
        public void Accepts_a_resume_at_exactly_fifty_thousand_characters() =>
            Assert.True(Sut.ValidateResumeForParsing(Resume(50_000)).isValid);

        [Fact]
        public void Rejects_a_resume_that_is_mostly_non_alphabetic()
        {
            var (isValid, reason) = Sut.ValidateResumeForParsing("experience" + new string('#', 400));

            Assert.False(isValid);
            Assert.Contains("corrupted", reason);
        }

        [Fact]
        public void Accepts_a_resume_just_above_the_ten_percent_alphabetic_floor()
        {
            var text = new string('a', 60) + new string('#', 240) + " experience";

            Assert.True(Sut.ValidateResumeForParsing(text).isValid);
        }

        [Theory]
        [InlineData("experience")]
        [InlineData("education")]
        [InlineData("skills")]
        [InlineData("work")]
        [InlineData("university")]
        [InlineData("college")]
        [InlineData("email")]
        [InlineData("phone")]
        [InlineData("EXPERIENCE")]
        [InlineData("Education")]
        public void Accepts_a_resume_containing_any_expected_keyword(string keyword)
        {
            var (isValid, reason) = Sut.ValidateResumeForParsing(Resume(300, keyword));

            Assert.True(isValid);
            Assert.Equal(string.Empty, reason);
        }

        [Fact]
        public void Rejects_a_resume_with_no_recognisable_keywords()
        {
            var (isValid, reason) = Sut.ValidateResumeForParsing(new string('a', 300));

            Assert.False(isValid);
            Assert.Contains("keywords", reason);
        }

        [Fact]
        public void Uses_a_lower_length_floor_than_the_report_helper()
        {
            // 150 chars: valid as a resume (100 floor), too short as a QC report (200 floor).
            var text = Resume(150);

            Assert.True(Sut.ValidateResumeForParsing(text).isValid);
            Assert.False(new ReportQCOptimizationHelper().ValidateReportForQC(text).isValid);
        }
    }

    public class ResumeOptimizationHelperCheapModelRoutingTests
    {
        private static readonly ResumeOptimizationHelper Sut = new();

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void A_blank_resume_is_not_simple(string text) =>
            Assert.False(Sut.IsSimpleResume(text));

        [Fact]
        public void A_short_junior_resume_is_simple() =>
            Assert.True(Sut.IsSimpleResume("Ada Lovelace. Skills: C#. 2 years at Risebird."));

        [Fact]
        public void A_resume_over_three_thousand_characters_is_not_simple() =>
            Assert.False(Sut.IsSimpleResume(new string('a', 3001)));

        [Fact]
        public void A_resume_at_exactly_three_thousand_characters_is_still_eligible() =>
            Assert.True(Sut.IsSimpleResume(new string('a', 3000)));

        [Theory]
        [InlineData("phd")]
        [InlineData("PhD")]
        [InlineData("PHD")]
        [InlineData("Ph.D")]
        [InlineData("doctorate")]
        [InlineData("Doctoral")]
        [InlineData("DOCTORATE")]
        public void A_doctoral_resume_is_never_simple(string marker) =>
            Assert.False(Sut.IsSimpleResume($"Ada Lovelace. {marker} in Mathematics."));

        [Fact]
        public void Length_is_checked_before_the_doctorate_keyword()
        {
            // Both disqualify, so the result is the same either way — this pins that a long
            // doctoral resume is rejected rather than throwing on the keyword scan.
            Assert.False(Sut.IsSimpleResume(new string('a', 4000) + " PhD"));
        }
    }

    public class TranscriptOptimizationHelperOptimizeTests
    {
        private static readonly TranscriptOptimizationHelper Sut = new();

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Blank_text_is_returned_unchanged(string text) =>
            Assert.Equal(text, Sut.OptimizeTranscriptText(text));

        [Theory]
        [InlineData("[00:01:23]")]
        [InlineData("[01:23]")]
        [InlineData("(00:01:23)")]
        [InlineData("(01:23)")]
        public void Strips_bracketed_and_parenthesised_timestamps(string timestamp)
        {
            var optimized = Sut.OptimizeTranscriptText($"{timestamp} Tell me about your history please");

            Assert.DoesNotContain(":", optimized);
            Assert.Contains("history", optimized);
        }

        [Fact]
        public void Strips_a_leading_bare_timestamp_from_each_line()
        {
            var optimized = Sut.OptimizeTranscriptText("00:01 First answer here\n00:02 Second answer here");

            Assert.DoesNotContain("00:01", optimized);
            Assert.DoesNotContain("00:02", optimized);
            Assert.Contains("First answer here", optimized);
            Assert.Contains("Second answer here", optimized);
        }

        [Fact]
        public void Strips_millisecond_timestamps()
        {
            var optimized = Sut.OptimizeTranscriptText("Answer at 00:01:23.456 in the middle");

            Assert.DoesNotContain("00:01:23.456", optimized);
            Assert.Contains("in the middle", optimized);
        }

        [Theory]
        [InlineData("um")]
        [InlineData("uh")]
        [InlineData("umm")]
        [InlineData("you know")]
        [InlineData("I mean")]
        [InlineData("kind of")]
        [InlineData("sort of")]
        [InlineData("basically")]
        public void Strips_filler_words(string filler)
        {
            var optimized = Sut.OptimizeTranscriptText($"So {filler} the answer is distributed caching");

            Assert.DoesNotContain(filler, optimized, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("distributed caching", optimized);
        }

        [Fact]
        public void Preserves_the_substantive_answer_around_the_fillers()
        {
            var optimized = Sut.OptimizeTranscriptText(
                "Um, so basically I uh built a sharded write-through cache, you know.");

            Assert.Contains("sharded write-through cache", optimized);
        }

        [Fact]
        public void Does_not_strip_a_filler_word_that_is_part_of_a_longer_word()
        {
            // Word-boundary anchored, so "umbrella" and "summary" must survive intact.
            var optimized = Sut.OptimizeTranscriptText("The umbrella summary was uhh fine");

            Assert.Contains("umbrella", optimized);
            Assert.Contains("summary", optimized);
        }

        [Fact]
        public void Collapses_whitespace_and_normalizes_line_breaks()
        {
            var optimized = Sut.OptimizeTranscriptText("Answer   one\r\n\r\n\r\n\r\nAnswer\ttwo");

            Assert.DoesNotContain("\r", optimized);
            Assert.DoesNotContain("\t", optimized);
            Assert.DoesNotContain("   ", optimized);
        }

        [Fact]
        public void Never_grows_the_text()
        {
            var text = "[00:01] Um, so   basically the answer\r\n\r\n\r\nis caching, you know.\t";

            Assert.True(Sut.OptimizeTranscriptText(text).Length <= text.Length);
        }

        [Fact]
        public void Optimizing_never_throws_on_awkward_input()
        {
            foreach (var text in new[]
                     {
                         "[[[", "((()))", "::::", "00:00:00", "\n\n\n", "um um um um",
                         "Interviewer:", "A", new string('x', 5000)
                     })
            {
                Assert.Null(Record.Exception(() => Sut.OptimizeTranscriptText(text)));
            }
        }
    }

    public class TranscriptOptimizationHelperValidateTests
    {
        private static readonly TranscriptOptimizationHelper Sut = new();

        private static string Transcript(int length = 400, string keyword = "interview") =>
            keyword + " " + new string('a', Math.Max(0, length - keyword.Length - 1));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Rejects_blank_transcript_text(string text)
        {
            var (isValid, reason) = Sut.ValidateTranscriptForAnalysis(text);

            Assert.False(isValid);
            Assert.Equal("Transcript text is empty or null", reason);
        }

        [Fact]
        public void Rejects_a_transcript_below_two_hundred_characters()
        {
            var (isValid, reason) = Sut.ValidateTranscriptForAnalysis(Transcript(199));

            Assert.False(isValid);
            Assert.Contains("too short", reason);
        }

        [Fact]
        public void Accepts_a_transcript_at_exactly_two_hundred_characters() =>
            Assert.True(Sut.ValidateTranscriptForAnalysis(Transcript(200)).isValid);

        [Fact]
        public void Rejects_a_transcript_above_one_hundred_thousand_characters()
        {
            var (isValid, reason) = Sut.ValidateTranscriptForAnalysis(Transcript(100_001));

            Assert.False(isValid);
            Assert.Contains("too long", reason);
        }

        [Fact]
        public void Accepts_a_transcript_at_exactly_one_hundred_thousand_characters() =>
            Assert.True(Sut.ValidateTranscriptForAnalysis(Transcript(100_000)).isValid);

        [Fact]
        public void Uses_a_stricter_fifteen_percent_alphabetic_floor_than_the_report_helper()
        {
            // 12% letters: above the report helper's 10% floor, below the transcript's 15%.
            var text = new string('a', 48) + new string('#', 352);

            Assert.False(Sut.ValidateTranscriptForAnalysis(text).isValid);
        }

        [Fact]
        public void Accepts_a_transcript_just_above_its_alphabetic_floor()
        {
            var text = "what " + new string('a', 80) + new string('#', 315);

            Assert.True(Sut.ValidateTranscriptForAnalysis(text).isValid);
        }

        [Theory]
        [InlineData("interview")]
        [InlineData("question")]
        [InlineData("answer")]
        [InlineData("tell me")]
        [InlineData("what")]
        [InlineData("how")]
        [InlineData("why")]
        [InlineData("experience")]
        [InlineData("think")]
        [InlineData("work")]
        [InlineData("INTERVIEW")]
        public void Accepts_a_transcript_containing_any_conversational_keyword(string keyword)
        {
            var (isValid, reason) = Sut.ValidateTranscriptForAnalysis(Transcript(400, keyword));

            Assert.True(isValid);
            Assert.Equal(string.Empty, reason);
        }

        [Fact]
        public void Rejects_a_transcript_with_no_conversational_keywords()
        {
            var (isValid, reason) = Sut.ValidateTranscriptForAnalysis(new string('a', 400));

            Assert.False(isValid);
            Assert.Contains("conversational", reason);
        }
    }

    public class TranscriptOptimizationHelperCheapModelRoutingTests
    {
        private static readonly TranscriptOptimizationHelper Sut = new();

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void A_blank_transcript_is_not_simple(string text) =>
            Assert.False(Sut.IsSimpleTranscript(text));

        [Fact]
        public void A_short_non_technical_transcript_is_simple() =>
            Assert.True(Sut.IsSimpleTranscript("Tell me about yourself. I was in support for two years."));

        [Fact]
        public void A_transcript_over_five_thousand_characters_is_not_simple() =>
            Assert.False(Sut.IsSimpleTranscript(new string('a', 5001)));

        [Fact]
        public void A_transcript_at_exactly_five_thousand_characters_is_still_eligible() =>
            Assert.True(Sut.IsSimpleTranscript(new string('a', 5000)));

        [Theory]
        [InlineData("architecture")]
        [InlineData("algorithm")]
        [InlineData("design pattern")]
        [InlineData("technical")]
        [InlineData("system design")]
        [InlineData("ARCHITECTURE")]
        [InlineData("Algorithm")]
        public void A_technical_transcript_is_never_simple(string topic) =>
            Assert.False(Sut.IsSimpleTranscript($"We discussed {topic} at length."));

        [Fact]
        public void A_transcript_with_many_labelled_speakers_is_not_simple()
        {
            var text = "Interviewer: hello\nCandidate: hi\nObserver: noted\nRecruiter: welcome\nPanelist: hello";

            Assert.False(Sut.IsSimpleTranscript(text));
        }

        [Fact]
        public void A_two_speaker_transcript_stays_simple()
        {
            var text = "Interviewer: tell me about yourself\nCandidate: I was in support for two years";

            Assert.True(Sut.IsSimpleTranscript(text));
        }
    }

    public class TextOptimizationHelperConsistencyTests
    {
        [Fact]
        public void All_three_helpers_estimate_tokens_identically()
        {
            var report = new ReportQCOptimizationHelper();
            var resume = new ResumeOptimizationHelper();
            var transcript = new TranscriptOptimizationHelper();

            foreach (var text in new[] { null, "", "a", "abcd", "abcde", new string('x', 1000) })
            {
                var expected = report.EstimateTokenCount(text);
                Assert.Equal(expected, resume.EstimateTokenCount(text));
                Assert.Equal(expected, transcript.EstimateTokenCount(text));
            }
        }

        [Fact]
        public void All_three_optimizers_pass_blank_input_straight_through()
        {
            var report = new ReportQCOptimizationHelper();
            var resume = new ResumeOptimizationHelper();
            var transcript = new TranscriptOptimizationHelper();

            foreach (var text in new[] { null, "", "   " })
            {
                Assert.Equal(text, report.OptimizeReportText(text));
                Assert.Equal(text, resume.OptimizeResumeText(text));
                Assert.Equal(text, transcript.OptimizeTranscriptText(text));
            }
        }

        [Fact]
        public void The_three_length_floors_are_deliberately_different()
        {
            // Resume 100, report 200, transcript 200 — a shared constant would be wrong here,
            // so this pins the intent rather than the implementation detail.
            var resume = new ResumeOptimizationHelper();
            var report = new ReportQCOptimizationHelper();
            var transcript = new TranscriptOptimizationHelper();

            var oneFifty = "experience interview " + new string('a', 130);

            Assert.True(resume.ValidateResumeForParsing(oneFifty).isValid);
            Assert.False(report.ValidateReportForQC(oneFifty).isValid);
            Assert.False(transcript.ValidateTranscriptForAnalysis(oneFifty).isValid);
        }
    }
}
