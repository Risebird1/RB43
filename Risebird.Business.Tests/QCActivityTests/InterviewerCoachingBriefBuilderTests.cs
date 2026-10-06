using Risebird.Entities.ViewModel;

namespace Risebird.Business.Tests.QCActivityTests
{
    public class InterviewerCoachingBriefBuilderTests
    {
        private static readonly DateTime From = new DateTime(2026, 9, 25);
        private static readonly DateTime To = new DateTime(2026, 10, 1);
        private static readonly DateTime NowUtc = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

        private static ExposureProfileVM Profile(DateTime? lastEmailUtc = null) => new ExposureProfileVM
        {
            InterviewerUserId = 7,
            InterviewerEmail = "ayesha@example.com",
            InterviewerName = "Ayesha Khan",
            FirstName = "Ayesha",
            Mobile = "9943398718",
            IsActive = true,
            LastSummaryEmailAtUtc = lastEmailUtc
        };

        private static InterviewerExposureVM LateJoinEverywhere() => new InterviewerExposureVM
        {
            Profile = Profile(),
            Risks = new[]
            {
                new ExposureRiskRowVM { InterviewId = 709500, IrfName = "APEIOSMOBD", InterviewDateIst = new DateTime(2026, 9, 27),
                                        SeverityBreakdown = "InterviewerLateJoin:P2,CandidateRating:P3" }
            },
            Payments = new[]
            {
                new ExposurePaymentRowVM { InterviewId = 709300, IrfName = "APEIOSMOBD", InterviewDateIst = new DateTime(2026, 9, 30),
                                           PaymentDeduction = 10, Inaccuratedecisionmaking = "Select despite low scores",
                                           AttendanceJoinCompliance = "Yes", PoorFeedbackComments = "No" }
            },
            Trials = new[]
            {
                new ExposureTrialRowVM { IrfName = "APEIOSMOBD", TriggeringInterviewId = 709500, TriggeringInterviewDate = new DateTime(2026, 9, 27),
                                         JoinTimePass = false, JoinTimeMinutes = 11, InterviewDurationPass = true, ReviewStatus = "pending",
                                         OverallVerdict = "bad_trial" }
            },
            Standing = new[]
            {
                new ExposureStandingRowVM { Kind = "RedMarker", IrfName = "INFIPOWAPP", Detail = "2 red markers, not reviewed yet", IsOpen = true }
            }
        };

        [Fact]
        public void A_late_join_from_three_sources_lands_under_one_theme_with_all_three_as_evidence()
        {
            var brief = InterviewerCoachingBriefBuilder.Build(LateJoinEverywhere(), From, To, NowUtc);

            var join = Assert.Single(brief.Themes, t => t.Key == InterviewerCoachingBriefBuilder.ThemeJoinOnTime);
            Assert.Equal(3, join.ItemCount);
            Assert.Equal(new[] { "Joined the interview late", "Joining on time (payment deduction 10%)", "Joined late by 11 min on your first interview for this channel" }
                .OrderBy(x => x), join.Items.Select(i => i.Text).OrderBy(x => x));
            Assert.Contains(InterviewerCoachingBriefBuilder.SourceRiskCheck, join.Sources);
            Assert.Contains(InterviewerCoachingBriefBuilder.SourceQcReview, join.Sources);
            Assert.Contains(InterviewerCoachingBriefBuilder.SourceTrial, join.Sources);
            Assert.Equal("P2", join.WorstSeverity);
        }

        [Fact]
        public void Counts_issues_areas_and_open_flags()
        {
            var brief = InterviewerCoachingBriefBuilder.Build(LateJoinEverywhere(), From, To, NowUtc);

            // join x3, candidate rating, decision accuracy, pending trial + red markers (channel standing)
            Assert.Equal(7, brief.IssueCount);
            Assert.Equal(4, brief.AreaCount);
            Assert.Equal(2, brief.OpenCount);
            Assert.True(brief.Themes.Last().IsInternal);
        }

        [Fact]
        public void Risk_check_codes_are_translated_into_plain_words()
        {
            Assert.Equal("Joined the interview late", InterviewerCoachingBriefBuilder.DescribeRiskCheck("InterviewerLateJoin"));
            Assert.Equal("Selected despite low skill scores", InterviewerCoachingBriefBuilder.DescribeRiskCheck(" Selectdeficit "));
            Assert.Equal("SomethingNew", InterviewerCoachingBriefBuilder.DescribeRiskCheck("SomethingNew"));
        }

        [Fact]
        public void Parses_breakdown_with_severities_and_falls_back_to_the_description()
        {
            Assert.Equal(new[] { ("CheckA", "P0"), ("CheckB", "P3") },
                InterviewerCoachingBriefBuilder.ParseChecks("CheckA:P0, CheckB:p3, CheckA:P2", null).ToArray());
            Assert.Equal(new[] { ("CheckA", (string)null), ("CheckB", (string)null) },
                InterviewerCoachingBriefBuilder.ParseChecks(null, "CheckA,CheckB").ToArray());
            Assert.Empty(InterviewerCoachingBriefBuilder.ParseChecks(" ", null));
        }

        [Fact]
        public void Candidate_no_show_is_not_held_against_the_interviewer_and_unknown_checks_go_to_other()
        {
            var exposure = new InterviewerExposureVM
            {
                Profile = Profile(),
                Risks = new[] { new ExposureRiskRowVM { InterviewId = 1, SeverityBreakdown = "Candidate No Show:P0,BrandNewCheck:P4" } }
            };

            var brief = InterviewerCoachingBriefBuilder.Build(exposure, From, To, NowUtc);

            var theme = Assert.Single(brief.Themes);
            Assert.Equal(InterviewerCoachingBriefBuilder.ThemeOther, theme.Key);
            Assert.Equal("BrandNewCheck", Assert.Single(theme.Items).Text);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData("No", null)]
        [InlineData("N/A", null)]
        [InlineData("Video Off When Mandatory", null)]
        [InlineData("Yes", "")]
        [InlineData("Interviewer Video off", "Interviewer Video off")]
        [InlineData("Video Off When Mandatory,Left Call Without Notice", "Left Call Without Notice")]
        public void Payment_reason_values_drop_blanks_and_compliant_checklist_text(string value, string expected)
        {
            var parts = InterviewerCoachingBriefBuilder.ReasonParts(value);
            if (expected == null)
                Assert.Null(parts);
            else
                Assert.Equal(expected, string.Join(",", parts));
        }

        [Fact]
        public void A_deduction_without_any_reason_is_still_raised()
        {
            var exposure = new InterviewerExposureVM
            {
                Profile = Profile(),
                Payments = new[] { new ExposurePaymentRowVM { InterviewId = 5, PaymentDeduction = 40, IrfName = "X" } }
            };

            var brief = InterviewerCoachingBriefBuilder.Build(exposure, From, To, NowUtc);

            var item = Assert.Single(Assert.Single(brief.Themes).Items);
            Assert.Equal("Report penalised in QC review (payment deduction 40%)", item.Text);
        }

        [Fact]
        public void Email_is_blocked_for_twelve_hours_after_the_last_summary()
        {
            var exposure = LateJoinEverywhere();
            exposure.Profile = Profile(lastEmailUtc: NowUtc.AddHours(-2));

            var brief = InterviewerCoachingBriefBuilder.Build(exposure, From, To, NowUtc);

            Assert.False(brief.CanEmail);
            Assert.Equal(NowUtc.AddHours(10), brief.EmailAvailableAtUtc);

            exposure.Profile = Profile(lastEmailUtc: NowUtc.AddHours(-13));
            Assert.True(InterviewerCoachingBriefBuilder.Build(exposure, From, To, NowUtc).CanEmail);
        }

        [Fact]
        public void Email_leaves_out_channel_standing_severity_and_unselected_themes_and_encodes_values()
        {
            var exposure = LateJoinEverywhere();
            exposure.Payments = new[]
            {
                new ExposurePaymentRowVM { InterviewId = 1, IrfName = "IRF<script>", PaymentDeduction = 15, PoorFeedbackComments = "<b>generic</b> comments" }
            };
            var brief = InterviewerCoachingBriefBuilder.Build(exposure, From, To, NowUtc);
            var selected = brief.Themes.Select(t => t.Key).Where(k => k != InterviewerCoachingBriefBuilder.ThemeCandidateExperience).ToList();

            var (subject, html, count) = InterviewerCoachingBriefBuilder.BuildEmail(brief, selected, "Thanks for your time <today>");

            Assert.Equal(2, count); // join on time + report quality; candidate experience unselected, standing internal
            Assert.Equal("Your Risebird interview review: 2 areas to work on", subject);
            Assert.DoesNotContain("Channel standing", html);
            Assert.DoesNotContain("red markers", html);
            Assert.DoesNotContain("P2", html);
            Assert.DoesNotContain("Give candidates a good experience", html);
            Assert.DoesNotContain("<script>", html);
            Assert.DoesNotContain("<b>generic</b>", html);
            Assert.Contains("&lt;b&gt;generic&lt;/b&gt;", html);
            Assert.Contains("Thanks for your time &lt;today&gt;", html);
            Assert.Contains("25 Sep 2026", html);
            Assert.Contains("Hi Ayesha Khan,", html);
        }

        [Fact]
        public void Inactive_interviewers_are_never_emailed()
        {
            var exposure = LateJoinEverywhere();
            exposure.Profile.IsActive = false;

            var brief = InterviewerCoachingBriefBuilder.Build(exposure, From, To, NowUtc);

            Assert.False(brief.CanEmail);
            Assert.Null(brief.EmailAvailableAtUtc);
        }

        [Fact]
        public void Deductions_saved_by_the_ai_review_are_labelled_as_ai()
        {
            var exposure = new InterviewerExposureVM
            {
                Profile = Profile(),
                Payments = new[]
                {
                    new ExposurePaymentRowVM { InterviewId = 1, PaymentDeduction = 40, LastSavedBy = "AISystem" },
                    new ExposurePaymentRowVM { InterviewId = 2, PaymentDeduction = 15, LastSavedBy = "qc@risebird.io", PoorFeedbackComments = "Generic" }
                }
            };

            var items = InterviewerCoachingBriefBuilder.Build(exposure, From, To, NowUtc).Themes.SelectMany(t => t.Items).ToList();

            var ai = Assert.Single(items, i => i.InterviewId == 1);
            Assert.Equal(InterviewerCoachingBriefBuilder.SourceAiReview, ai.Source);
            Assert.Equal("Report penalised by the AI review (payment deduction 40%)", ai.Text);
            Assert.Equal(InterviewerCoachingBriefBuilder.SourceQcReview, Assert.Single(items, i => i.InterviewId == 2).Source);
        }

        [Fact]
        public void Email_with_nothing_selected_has_no_areas()
        {
            var brief = InterviewerCoachingBriefBuilder.Build(LateJoinEverywhere(), From, To, NowUtc);

            var (subject, _, count) = InterviewerCoachingBriefBuilder.BuildEmail(brief, new[] { InterviewerCoachingBriefBuilder.ThemeChannelStanding }, null);

            Assert.Equal(0, count);
            Assert.Equal("Your Risebird interview review: 0 areas to work on", subject);
        }
    }
}
