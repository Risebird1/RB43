using Risebird.Entities.Constants;
using Risebird.Entities.Helpers;

namespace Risebird.CommonHelper.Tests.EntitiesHelpers
{
    /// <summary>
    /// The Plivo log_id token is the only carrier for the data needed to create an availability
    /// record after a call completes. A round-trip bug here silently loses the interviewer's
    /// confirmed availability, so every branch of build/parse is covered.
    /// </summary>
    public class AvailabilityCallTokenBuildTests
    {
        [Fact]
        public void Builds_a_prefixed_tilde_delimited_token()
        {
            var token = AvailabilityCallToken.Build(55, "Backend MAG", "naukri");

            Assert.StartsWith("PROF~55~", token);
            Assert.Equal(4, token.Split('~').Length);
        }

        [Fact]
        public void Encodes_the_payload_so_it_never_contains_the_delimiter()
        {
            var token = AvailabilityCallToken.Build(55, "MAG~with~tildes", "source~too");

            // Only the three structural delimiters remain.
            Assert.Equal(3, token.Count(c => c == '~'));
        }

        [Fact]
        public void Encodes_the_payload_url_safely()
        {
            var token = AvailabilityCallToken.Build(55, "MAG ??? +/= name", "source +/=");

            Assert.DoesNotContain("+", token);
            Assert.DoesNotContain("/", token);
            Assert.DoesNotContain("=", token);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void A_blank_payload_segment_encodes_to_nothing(string blank)
        {
            var token = AvailabilityCallToken.Build(55, blank, blank);

            Assert.Equal("PROF~55~~", token);
        }

        [Fact]
        public void Is_deterministic()
        {
            Assert.Equal(
                AvailabilityCallToken.Build(55, "Backend MAG", "naukri"),
                AvailabilityCallToken.Build(55, "Backend MAG", "naukri"));
        }

        [Fact]
        public void Different_profile_ids_produce_different_tokens()
        {
            Assert.NotEqual(
                AvailabilityCallToken.Build(55, "MAG", "naukri"),
                AvailabilityCallToken.Build(56, "MAG", "naukri"));
        }
    }

    public class AvailabilityCallTokenRoundTripTests
    {
        [Theory]
        [InlineData(1, "Backend MAG", "naukri")]
        [InlineData(999999, "Data Engineering", "linkedin")]
        [InlineData(55, "MAG with spaces and punctuation!", "campaign-2026")]
        [InlineData(55, "MAG~with~tildes", "source~too")]
        [InlineData(55, "unicode MAG", "source")]
        public void Round_trips_every_payload(int profileId, string magName, string source)
        {
            var token = AvailabilityCallToken.Build(profileId, magName, source);

            var parsed = AvailabilityCallToken.TryParse(token, out var id, out var mag, out var src);

            Assert.True(parsed);
            Assert.Equal(profileId, id);
            Assert.Equal(magName, mag);
            Assert.Equal(source, src);
        }

        [Fact]
        public void Round_trips_blank_segments_as_empty_strings()
        {
            var token = AvailabilityCallToken.Build(55, null, null);

            var parsed = AvailabilityCallToken.TryParse(token, out var id, out var mag, out var src);

            Assert.True(parsed);
            Assert.Equal(55, id);
            Assert.Equal(string.Empty, mag);
            Assert.Equal(string.Empty, src);
        }
    }

    public class AvailabilityCallTokenParseTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Rejects_blank_input(string token)
        {
            var parsed = AvailabilityCallToken.TryParse(token, out var id, out var mag, out var src);

            Assert.False(parsed);
            Assert.Equal(0, id);
            Assert.Null(mag);
            Assert.Null(src);
        }

        [Theory]
        [InlineData("12345")]
        [InlineData("0")]
        [InlineData("prof~55~a~b")]
        [InlineData("PROFILE~55~a~b")]
        [InlineData("~55~a~b")]
        [InlineData(" PROF~55~a~b")]
        public void Rejects_anything_that_is_not_a_prefixed_token(string token)
        {
            // A plain integer log_id maps to an existing record and must NOT be claimed here.
            Assert.False(AvailabilityCallToken.TryParse(token, out _, out _, out _));
        }

        [Theory]
        [InlineData("PROF~55")]
        [InlineData("PROF~55~onlymag")]
        [InlineData("PROF~")]
        [InlineData("PROF~~")]
        public void Rejects_a_token_with_too_few_segments(string token) =>
            Assert.False(AvailabilityCallToken.TryParse(token, out _, out _, out _));

        [Theory]
        [InlineData("PROF~abc~a~b")]
        [InlineData("PROF~~a~b")]
        [InlineData("PROF~1.5~a~b")]
        public void Rejects_a_non_integer_profile_id(string token) =>
            Assert.False(AvailabilityCallToken.TryParse(token, out _, out _, out _));

        [Theory]
        [InlineData("PROF~0~a~b")]
        [InlineData("PROF~-1~a~b")]
        public void Rejects_a_non_positive_profile_id(string token) =>
            Assert.False(AvailabilityCallToken.TryParse(token, out _, out _, out _));

        [Fact]
        public void Accepts_extra_trailing_segments_and_ignores_them()
        {
            // Length >= 3 is the contract, so a future added segment stays backward compatible.
            var parsed = AvailabilityCallToken.TryParse("PROF~55~~~extra", out var id, out var mag, out var src);

            Assert.True(parsed);
            Assert.Equal(55, id);
            Assert.Equal(string.Empty, mag);
            Assert.Equal(string.Empty, src);
        }

        [Fact]
        public void Undecodable_payload_segments_degrade_to_empty_rather_than_throwing()
        {
            var parsed = AvailabilityCallToken.TryParse("PROF~55~!!!~???", out var id, out var mag, out var src);

            Assert.True(parsed);
            Assert.Equal(55, id);
            Assert.Equal(string.Empty, mag);
            Assert.Equal(string.Empty, src);
        }

        [Fact]
        public void Clears_out_parameters_before_parsing()
        {
            var id = 99;
            var mag = "stale";
            var src = "stale";

            AvailabilityCallToken.TryParse("not-a-token", out id, out mag, out src);

            Assert.Equal(0, id);
            Assert.Null(mag);
            Assert.Null(src);
        }
    }

    public class DriveRowActionsTests
    {
        [Fact]
        public void Offers_the_seven_drive_row_actions() =>
            Assert.Equal(7, DriveRowActions.Options.Count);

        [Fact]
        public void Every_action_has_a_key_and_a_label()
        {
            foreach (var option in DriveRowActions.Options)
            {
                Assert.False(string.IsNullOrWhiteSpace(option.Key));
                Assert.False(string.IsNullOrWhiteSpace(option.Value));
            }
        }

        [Fact]
        public void Action_keys_are_unique()
        {
            var keys = DriveRowActions.Options.Select(o => o.Key).ToList();

            Assert.Equal(keys.Count, keys.Distinct(StringComparer.Ordinal).Count());
        }

        [Fact]
        public void Action_labels_are_unique()
        {
            var labels = DriveRowActions.Options.Select(o => o.Value).ToList();

            Assert.Equal(labels.Count, labels.Distinct(StringComparer.Ordinal).Count());
        }

        [Theory]
        [InlineData("magBatchCall")]
        [InlineData("magEmail")]
        [InlineData("campaignEmail")]
        [InlineData("campaignBatchCall")]
        [InlineData("processStuckApplicants")]
        [InlineData("inviteFresh")]
        [InlineData("openInterviewNotification")]
        public void Contains_each_expected_action_key(string key) =>
            Assert.Contains(DriveRowActions.Options, o => o.Key == key);

        [Fact]
        public void Keys_are_camel_case_identifiers_safe_for_a_form_post()
        {
            foreach (var option in DriveRowActions.Options)
            {
                Assert.True(char.IsLower(option.Key[0]), $"'{option.Key}' should start lower-case");
                Assert.All(option.Key, c => Assert.True(char.IsLetterOrDigit(c), $"'{option.Key}' must be alphanumeric"));
            }
        }
    }

    public class DriveWeekendActionRefsTests
    {
        [Fact]
        public void Ref_type_is_the_stable_discriminator() =>
            Assert.Equal("DriveWeekend", DriveWeekendActionRefs.RefType);

        [Fact]
        public void Builds_a_pipe_delimited_ref_id()
        {
            var refId = DriveWeekendActionRefs.BuildRefId("1234", new DateTime(2026, 09, 12));

            Assert.Equal("1234|2026-09-12", refId);
        }

        [Fact]
        public void Formats_the_weekend_start_as_a_sortable_date()
        {
            var refId = DriveWeekendActionRefs.BuildRefId("1", new DateTime(2026, 01, 05));

            Assert.EndsWith("2026-01-05", refId);
        }

        [Fact]
        public void Drops_the_time_component()
        {
            var withTime = DriveWeekendActionRefs.BuildRefId("1", new DateTime(2026, 09, 12, 23, 59, 58));
            var withoutTime = DriveWeekendActionRefs.BuildRefId("1", new DateTime(2026, 09, 12));

            Assert.Equal(withoutTime, withTime);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Tolerates_a_blank_irf_id(string irfId)
        {
            var refId = DriveWeekendActionRefs.BuildRefId(irfId, new DateTime(2026, 09, 12));

            Assert.Equal("|2026-09-12", refId);
        }

        [Fact]
        public void Is_deterministic()
        {
            Assert.Equal(
                DriveWeekendActionRefs.BuildRefId("1234", new DateTime(2026, 09, 12)),
                DriveWeekendActionRefs.BuildRefId("1234", new DateTime(2026, 09, 12)));
        }

        [Fact]
        public void Different_weekends_produce_different_ref_ids()
        {
            Assert.NotEqual(
                DriveWeekendActionRefs.BuildRefId("1234", new DateTime(2026, 09, 12)),
                DriveWeekendActionRefs.BuildRefId("1234", new DateTime(2026, 09, 19)));
        }
    }
}
