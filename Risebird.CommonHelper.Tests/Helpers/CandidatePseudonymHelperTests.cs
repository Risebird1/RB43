namespace Risebird.CommonHelper.Tests.Helpers
{
    public class CandidatePseudonymHelperBuildNickNameTests
    {
        private static ISet<string> Taken(params string[] values) =>
            new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);

        // ── Positive ──────────────────────────────────────────────────────────

        [Fact]
        public void Builds_two_initials_from_each_name_plus_a_four_digit_suffix()
        {
            var nick = CandidatePseudonymHelper.BuildNickName("Ada", "Lovelace", 1234, Taken());

            Assert.Equal("AdLo2234", nick);
        }

        [Fact]
        public void Capitalizes_the_first_letter_and_lowercases_the_second()
        {
            var nick = CandidatePseudonymHelper.BuildNickName("aDA", "lOVELACE", 0, Taken());

            Assert.Equal("AdLo1000", nick);
        }

        [Fact]
        public void Is_deterministic_for_the_same_inputs()
        {
            var first = CandidatePseudonymHelper.BuildNickName("Grace", "Hopper", 77, Taken());
            var second = CandidatePseudonymHelper.BuildNickName("Grace", "Hopper", 77, Taken());

            Assert.Equal(first, second);
        }

        [Fact]
        public void Suffix_always_lands_in_the_one_thousand_to_nine_thousand_nine_hundred_ninety_nine_band()
        {
            foreach (var seed in new[] { 0, 1, 8999, 9000, 9001, 123456, int.MaxValue })
            {
                var nick = CandidatePseudonymHelper.BuildNickName("Ada", "Lovelace", seed, Taken());
                var suffix = int.Parse(nick.Substring(4));

                Assert.InRange(suffix, 1000, 9999);
            }
        }

        [Fact]
        public void Wraps_the_suffix_at_nine_thousand()
        {
            // 9000 % 9000 == 0 → 1000, i.e. the same suffix as seed 0.
            Assert.Equal(
                CandidatePseudonymHelper.BuildNickName("Ada", "Lovelace", 0, Taken()),
                CandidatePseudonymHelper.BuildNickName("Ada", "Lovelace", 9000, Taken()));
        }

        [Fact]
        public void Treats_a_negative_seed_the_same_as_its_positive_counterpart()
        {
            Assert.Equal(
                CandidatePseudonymHelper.BuildNickName("Ada", "Lovelace", 1234, Taken()),
                CandidatePseudonymHelper.BuildNickName("Ada", "Lovelace", -1234, Taken()));
        }

        [Fact]
        public void Pads_a_single_character_name_with_x()
        {
            var nick = CandidatePseudonymHelper.BuildNickName("A", "B", 0, Taken());

            Assert.Equal("AxBx1000", nick);
        }

        [Fact]
        public void Strips_punctuation_and_spaces_before_taking_initials()
        {
            var nick = CandidatePseudonymHelper.BuildNickName("  o'br ien ", "d-souza", 0, Taken());

            // "o'br ien" → "obrien" → "Ob";  "d-souza" → "dsouza" → "Ds"
            Assert.Equal("ObDs1000", nick);
        }

        [Fact]
        public void Keeps_digits_that_survive_the_letter_or_digit_filter()
        {
            var nick = CandidatePseudonymHelper.BuildNickName("42nd", "9lives", 0, Taken());

            Assert.Equal("429l1000", nick);
        }

        // ── Fallbacks ─────────────────────────────────────────────────────────

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("   ", "   ")]
        [InlineData("!!!", "@@@")]
        public void Falls_back_to_Ri_and_se_when_a_name_yields_no_usable_characters(string first, string last)
        {
            var nick = CandidatePseudonymHelper.BuildNickName(first, last, 0, Taken());

            Assert.Equal("Rise1000", nick);
        }

        [Fact]
        public void Falls_back_per_part_independently()
        {
            Assert.Equal("Adse1000", CandidatePseudonymHelper.BuildNickName("Ada", null, 0, Taken()));
            Assert.Equal("RiLo1000", CandidatePseudonymHelper.BuildNickName(null, "Lovelace", 0, Taken()));
        }

        [Fact]
        public void Tolerates_a_null_taken_set()
        {
            var nick = CandidatePseudonymHelper.BuildNickName("Ada", "Lovelace", 1234, null);

            Assert.Equal("AdLo2234", nick);
        }

        // ── Collision handling ────────────────────────────────────────────────

        [Fact]
        public void Bumps_the_suffix_once_on_a_single_collision()
        {
            var nick = CandidatePseudonymHelper.BuildNickName("Ada", "Lovelace", 1234, Taken("AdLo2234"));

            Assert.Equal("AdLo2235", nick);
        }

        [Fact]
        public void Collision_check_is_case_insensitive_when_the_caller_supplies_that_comparer()
        {
            var nick = CandidatePseudonymHelper.BuildNickName("Ada", "Lovelace", 1234, Taken("adlo2234"));

            Assert.NotEqual("AdLo2234", nick);
        }

        [Fact]
        public void Walks_forward_through_several_consecutive_collisions()
        {
            var taken = Taken("AdLo2234", "AdLo2235", "AdLo2236");

            var nick = CandidatePseudonymHelper.BuildNickName("Ada", "Lovelace", 1234, taken);

            Assert.Equal("AdLo2237", nick);
        }

        [Fact]
        public void Falls_back_to_a_guid_fragment_after_ten_failed_attempts()
        {
            // Seed the set with every candidate the deterministic walk can produce (attempt 0..10).
            var taken = Taken(Enumerable.Range(0, 11).Select(i => $"AdLo{1234 + i + 1000}").ToArray());

            var nick = CandidatePseudonymHelper.BuildNickName("Ada", "Lovelace", 1234, taken);

            Assert.StartsWith("AdLo", nick);
            Assert.Equal(8, nick.Length);
            Assert.DoesNotContain(nick, taken);
        }

        [Fact]
        public void Guid_fallback_produces_a_different_value_each_call()
        {
            var taken = Taken(Enumerable.Range(0, 11).Select(i => $"AdLo{1234 + i + 1000}").ToArray());

            var first = CandidatePseudonymHelper.BuildNickName("Ada", "Lovelace", 1234, taken);
            var second = CandidatePseudonymHelper.BuildNickName("Ada", "Lovelace", 1234, taken);

            Assert.NotEqual(first, second);
        }

        // ── Documented edge case ──────────────────────────────────────────────

        [Fact]
        public void Int_min_value_seed_overflows_because_Math_Abs_cannot_negate_it()
        {
            // Pinned deliberately: Math.Abs(int.MinValue) throws. Callers must never pass
            // int.MinValue as a seed id (real identity columns never reach it).
            Assert.Throws<OverflowException>(() =>
                CandidatePseudonymHelper.BuildNickName("Ada", "Lovelace", int.MinValue, Taken()));
        }
    }

    public class CandidatePseudonymHelperBuildNickNameFromEmailTests
    {
        private static ISet<string> Taken(params string[] values) =>
            new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);

        [Theory]
        [InlineData("ada.lovelace@risebird.io")]
        [InlineData("ada_lovelace@risebird.io")]
        [InlineData("ada-lovelace@risebird.io")]
        [InlineData("ada lovelace@risebird.io")]
        public void Splits_the_local_part_on_dot_underscore_hyphen_and_space(string email)
        {
            var nick = CandidatePseudonymHelper.BuildNickNameFromEmail(email, 1234, Taken());

            Assert.Equal("AdLo2234", nick);
        }

        [Fact]
        public void Uses_the_whole_local_part_as_the_first_name_when_there_is_no_separator()
        {
            var nick = CandidatePseudonymHelper.BuildNickNameFromEmail("adalovelace@risebird.io", 0, Taken());

            Assert.Equal("Adse1000", nick);
        }

        [Fact]
        public void Ignores_tokens_beyond_the_second()
        {
            var nick = CandidatePseudonymHelper.BuildNickNameFromEmail("ada.lovelace.byron@risebird.io", 1234, Taken());

            Assert.Equal("AdLo2234", nick);
        }

        [Fact]
        public void Ignores_the_domain_entirely()
        {
            Assert.Equal(
                CandidatePseudonymHelper.BuildNickNameFromEmail("ada.lovelace@risebird.io", 5, Taken()),
                CandidatePseudonymHelper.BuildNickNameFromEmail("ada.lovelace@example.org", 5, Taken()));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Falls_back_to_the_anonymous_pseudonym_for_a_blank_email(string email)
        {
            var nick = CandidatePseudonymHelper.BuildNickNameFromEmail(email, 0, Taken());

            Assert.Equal("Rise1000", nick);
        }

        [Fact]
        public void Handles_an_address_with_no_at_sign_by_treating_it_all_as_the_local_part()
        {
            var nick = CandidatePseudonymHelper.BuildNickNameFromEmail("ada.lovelace", 1234, Taken());

            Assert.Equal("AdLo2234", nick);
        }

        [Fact]
        public void Handles_a_local_part_made_only_of_separators()
        {
            var nick = CandidatePseudonymHelper.BuildNickNameFromEmail("...@risebird.io", 0, Taken());

            // No tokens survive → first falls back to the raw local part "...", which has
            // no letters/digits → "Ri"; last is empty → "se".
            Assert.Equal("Rise1000", nick);
        }

        [Fact]
        public void Respects_the_taken_set_it_is_handed()
        {
            var nick = CandidatePseudonymHelper.BuildNickNameFromEmail(
                "ada.lovelace@risebird.io", 1234, Taken("AdLo2234"));

            Assert.Equal("AdLo2235", nick);
        }
    }
}
