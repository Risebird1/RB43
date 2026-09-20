using Risebird.Entities.ViewModel;

namespace Risebird.CommonHelper.Tests.EntitiesHelpers
{
    public class TrialFlagClientPriorityFromSeverityTests
    {
        [Theory]
        [InlineData("P0")]
        [InlineData("P1")]
        [InlineData("p0")]
        [InlineData("p1")]
        [InlineData("  P0  ")]
        public void P0_and_P1_map_to_critical(string severity) =>
            Assert.Equal(TrialFlagClientPriority.Critical, TrialFlagClientPriority.FromSeverity(severity));

        [Theory]
        [InlineData("P2")]
        [InlineData("P3")]
        [InlineData("p2")]
        [InlineData("  p3 ")]
        public void P2_and_P3_map_to_medium(string severity) =>
            Assert.Equal(TrialFlagClientPriority.Medium, TrialFlagClientPriority.FromSeverity(severity));

        [Theory]
        [InlineData("P5")]
        [InlineData("p5")]
        public void P5_maps_to_clear(string severity) =>
            Assert.Equal(TrialFlagClientPriority.Clear, TrialFlagClientPriority.FromSeverity(severity));

        [Theory]
        [InlineData("P4")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("P9")]
        [InlineData("unknown")]
        [InlineData("—")]
        public void Everything_else_falls_through_to_low(string severity) =>
            Assert.Equal(TrialFlagClientPriority.Low, TrialFlagClientPriority.FromSeverity(severity));

        [Fact]
        public void Tier_names_are_the_four_documented_labels()
        {
            Assert.Equal("Critical", TrialFlagClientPriority.Critical);
            Assert.Equal("Medium", TrialFlagClientPriority.Medium);
            Assert.Equal("Low", TrialFlagClientPriority.Low);
            Assert.Equal("Clear", TrialFlagClientPriority.Clear);
        }
    }

    public class TrialFlagClientPriorityCssModifierTests
    {
        [Theory]
        [InlineData("P0", "critical")]
        [InlineData("P1", "critical")]
        [InlineData("P2", "medium")]
        [InlineData("P3", "medium")]
        [InlineData("P4", "low")]
        [InlineData("P5", "clear")]
        [InlineData(null, "low")]
        [InlineData("", "low")]
        [InlineData("garbage", "low")]
        public void Emits_a_lowercase_css_modifier(string severity, string expected) =>
            Assert.Equal(expected, TrialFlagClientPriority.CssModifier(severity));

        [Fact]
        public void Modifier_is_always_lowercase_and_non_empty()
        {
            foreach (var severity in new[] { null, "", "P0", "P1", "P2", "P3", "P4", "P5", "PX" })
            {
                var css = TrialFlagClientPriority.CssModifier(severity);
                Assert.False(string.IsNullOrWhiteSpace(css));
                Assert.Equal(css.ToLowerInvariant(), css);
            }
        }
    }

    public class TrialFlagClientPriorityBadgeLabelTests
    {
        [Theory]
        [InlineData("P0", "Critical · P0")]
        [InlineData("P1", "Critical · P1")]
        [InlineData("P2", "Medium · P2")]
        [InlineData("P3", "Medium · P3")]
        [InlineData("P4", "Low · P4")]
        [InlineData("P5", "Clear · P5")]
        [InlineData("p0", "Critical · P0")]
        [InlineData("  p2  ", "Medium · P2")]
        public void Combines_the_tier_and_the_raw_severity(string severity, string expected) =>
            Assert.Equal(expected, TrialFlagClientPriority.BadgeLabel(severity));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("—")]
        [InlineData("-")]
        [InlineData("–")]
        public void Collapses_blank_and_dash_placeholders_to_a_bare_low_label(string severity) =>
            Assert.Equal(TrialFlagClientPriority.Low, TrialFlagClientPriority.BadgeLabel(severity));

        [Fact]
        public void An_unknown_severity_still_shows_its_raw_token() =>
            Assert.Equal("Low · PX", TrialFlagClientPriority.BadgeLabel("PX"));
    }

    public class TrialFlagClientPriorityBuildJsMapTests
    {
        [Fact]
        public void Covers_P0_through_P5_plus_the_blank_key()
        {
            var map = TrialFlagClientPriority.BuildJsMap();

            Assert.Equal(7, map.Count);
            foreach (var key in new[] { "P0", "P1", "P2", "P3", "P4", "P5", "" })
                Assert.True(map.ContainsKey(key), $"missing key '{key}'");
        }

        [Fact]
        public void Lookup_is_case_insensitive()
        {
            var map = TrialFlagClientPriority.BuildJsMap();

            Assert.True(map.ContainsKey("p0"));
            Assert.True(map.ContainsKey("P0"));
        }

        [Fact]
        public void Every_entry_agrees_with_the_scalar_helpers()
        {
            var map = TrialFlagClientPriority.BuildJsMap();

            foreach (var kv in map)
            {
                Assert.Equal(TrialFlagClientPriority.FromSeverity(kv.Key), kv.Value.Tier);
                Assert.Equal(TrialFlagClientPriority.CssModifier(kv.Key), kv.Value.Css);
                Assert.Equal(TrialFlagClientPriority.BadgeLabel(kv.Key), kv.Value.Badge);
            }
        }

        [Fact]
        public void The_blank_key_carries_the_bare_low_badge()
        {
            var map = TrialFlagClientPriority.BuildJsMap();

            Assert.Equal(TrialFlagClientPriority.Low, map[""].Badge);
            Assert.Equal("low", map[""].Css);
        }

        [Fact]
        public void Rebuilding_produces_an_equivalent_map()
        {
            var first = TrialFlagClientPriority.BuildJsMap();
            var second = TrialFlagClientPriority.BuildJsMap();

            Assert.Equal(first.Count, second.Count);
            foreach (var kv in first)
                Assert.Equal(kv.Value.Badge, second[kv.Key].Badge);
        }
    }

    public class TrialFlagEvidenceBadgeHelperIsPositiveEvidenceTests
    {
        [Theory]
        [InlineData("Yes")]
        [InlineData("1")]
        [InlineData("true")]
        [InlineData("some reason")]
        [InlineData("P0")]
        [InlineData("0.5")]
        [InlineData("nonetheless")]
        public void Treats_meaningful_values_as_evidence(string value) =>
            Assert.True(TrialFlagEvidenceBadgeHelper.IsPositiveEvidence(value));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("0")]
        [InlineData("no")]
        [InlineData("No")]
        [InlineData("N")]
        [InlineData("false")]
        [InlineData("FALSE")]
        [InlineData("na")]
        [InlineData("n/a")]
        [InlineData("N/A")]
        [InlineData("none")]
        [InlineData("null")]
        [InlineData("  NULL  ")]
        public void Filters_out_every_noise_token(string value) =>
            Assert.False(TrialFlagEvidenceBadgeHelper.IsPositiveEvidence(value));
    }

    public class TrialFlagEvidenceBadgeHelperPaymentReasonTests
    {
        [Fact]
        public void Emits_a_label_only_for_columns_that_carry_evidence()
        {
            var badges = TrialFlagEvidenceBadgeHelper.CollectPaymentReasonBadges(
                limitedHandoncoding: "Yes",
                wrongStatusUpdates: null,
                weakInfrastructure: "0",
                proctoringIgnorance: "   ",
                unethicalPractices: "confirmed",
                poorCommunicationSkills: "no",
                attendanceJoinCompliance: "late join");

            Assert.Equal(
                new[] { "LimitedHandoncoding", "UnethicalPractices", "AttendanceJoinCompliance" },
                badges);
        }

        [Fact]
        public void Returns_an_empty_list_when_nothing_is_set()
        {
            var badges = TrialFlagEvidenceBadgeHelper.CollectPaymentReasonBadges(
                null, null, null, null, null, null, null);

            Assert.Empty(badges);
        }

        [Fact]
        public void Returns_every_label_when_all_columns_carry_evidence()
        {
            var badges = TrialFlagEvidenceBadgeHelper.CollectPaymentReasonBadges(
                "a", "b", "c", "d", "e", "f", "g");

            Assert.Equal(7, badges.Count);
        }

        [Fact]
        public void Labels_are_emitted_in_a_stable_declaration_order()
        {
            var badges = TrialFlagEvidenceBadgeHelper.CollectPaymentReasonBadges(
                "a", "b", "c", "d", "e", "f", "g");

            Assert.Equal("LimitedHandoncoding", badges[0]);
            Assert.Equal("AttendanceJoinCompliance", badges[^1]);
        }
    }

    public class TrialFlagEvidenceBadgeHelperJsonTests
    {
        [Fact(Skip = SkippedOnRequest.Reason)]
        public void Parses_payment_reason_values_from_json()
        {
            var json = """
                {"LimitedHandoncoding":"Yes","WeakInfrastructure":null,"UnethicalPractices":"0",
                 "ProctoringIgnorance":"camera off","WrongStatusUpdates":"","PoorCommunicationSkills":"n/a",
                 "AttendanceJoinCompliance":"15 min late"}
                """;

            var badges = TrialFlagEvidenceBadgeHelper.CollectPaymentReasonBadgesFromJson(json);

            // "Yes" is evidence; only null, "0", "" and "n/a" are filtered as noise.
            Assert.Equal(
                new[] { "LimitedHandoncoding", "ProctoringIgnorance", "AttendanceJoinCompliance" },
                badges);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not json")]
        [InlineData("[1,2,3]")]
        [InlineData("{")]
        public void Returns_empty_for_blank_or_unparseable_json(string json) =>
            Assert.Empty(TrialFlagEvidenceBadgeHelper.CollectPaymentReasonBadgesFromJson(json));

        [Fact]
        public void Returns_empty_when_the_json_object_has_none_of_the_known_columns() =>
            Assert.Empty(TrialFlagEvidenceBadgeHelper.CollectPaymentReasonBadgesFromJson("""{"Other":"Yes"}"""));

        [Fact]
        public void Collects_merge_flag_badges_from_a_bare_array()
        {
            var json = """
                [{"category":"Rubric","flag":"skipped scoring"},
                 {"category":"Timing","flag":"ran 12 minutes"}]
                """;

            var badges = TrialFlagEvidenceBadgeHelper.CollectMergeFlagBadges(json);

            Assert.Equal(new[] { "Rubric: skipped scoring", "Timing: ran 12 minutes" }, badges);
        }

        [Fact]
        public void Collects_merge_flag_badges_from_a_wrapped_object()
        {
            var json = """{"flags":[{"category":"Rubric","flag":"skipped scoring"}]}""";

            Assert.Equal(new[] { "Rubric: skipped scoring" },
                TrialFlagEvidenceBadgeHelper.CollectMergeFlagBadges(json));
        }

        [Fact]
        public void Accepts_pascal_case_property_names()
        {
            var json = """[{"Category":"Rubric","Flag":"skipped scoring"}]""";

            Assert.Equal(new[] { "Rubric: skipped scoring" },
                TrialFlagEvidenceBadgeHelper.CollectMergeFlagBadges(json));
        }

        [Fact]
        public void Drops_the_category_prefix_when_the_category_is_noise()
        {
            var json = """[{"category":"none","flag":"skipped scoring"},{"category":null,"flag":"another"}]""";

            Assert.Equal(new[] { "skipped scoring", "another" },
                TrialFlagEvidenceBadgeHelper.CollectMergeFlagBadges(json));
        }

        [Fact]
        public void Skips_entries_whose_flag_is_missing_or_noise()
        {
            var json = """
                [{"category":"Rubric","flag":null},
                 {"category":"Rubric","flag":""},
                 {"category":"Rubric","flag":"0"},
                 {"category":"Rubric"},
                 null,
                 {"category":"Rubric","flag":"real problem"}]
                """;

            Assert.Equal(new[] { "Rubric: real problem" },
                TrialFlagEvidenceBadgeHelper.CollectMergeFlagBadges(json));
        }

        [Fact]
        public void De_duplicates_identical_badges_case_insensitively()
        {
            var json = """
                [{"category":"Rubric","flag":"same"},
                 {"category":"rubric","flag":"SAME"},
                 {"category":"Rubric","flag":"same"}]
                """;

            Assert.Single(TrialFlagEvidenceBadgeHelper.CollectMergeFlagBadges(json));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not json")]
        [InlineData("[]")]
        [InlineData("{}")]
        [InlineData("""{"flags":[]}""")]
        [InlineData("""{"flags":null}""")]
        public void Returns_empty_for_blank_empty_or_unparseable_merge_json(string json) =>
            Assert.Empty(TrialFlagEvidenceBadgeHelper.CollectMergeFlagBadges(json));

        [Fact]
        public void Filters_a_legacy_label_csv()
        {
            var badges = TrialFlagEvidenceBadgeHelper.FilterLabelCsv(
                "WeakInfrastructure, 0 , none,  ProctoringIgnorance ,n/a,WeakInfrastructure");

            Assert.Equal(new[] { "WeakInfrastructure", "ProctoringIgnorance" }, badges);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("0,no,none,n/a,null,false")]
        [InlineData(",,,")]
        public void Returns_empty_for_a_csv_with_no_real_labels(string csv) =>
            Assert.Empty(TrialFlagEvidenceBadgeHelper.FilterLabelCsv(csv));
    }
}
