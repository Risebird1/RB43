using Risebird.Entities.ViewModel;

namespace Risebird.CommonHelper.Tests.EntitiesHelpers
{
    public class DmIvCapHelperPolicyTests
    {
        [Theory]
        [InlineData("limited")]
        [InlineData("Limited")]
        [InlineData("LIMITED")]
        [InlineData("  limited  ")]
        public void Recognises_the_limited_policy_string(string policy) =>
            Assert.True(DmIvCapHelper.IsLimitedPolicy(policy));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("free")]
        [InlineData("unlimited")]
        [InlineData("limit")]
        [InlineData("limitedd")]
        public void Treats_everything_else_as_free(string policy) =>
            Assert.False(DmIvCapHelper.IsLimitedPolicy(policy));

        [Fact]
        public void Boolean_overload_passes_the_flag_through()
        {
            Assert.True(DmIvCapHelper.IsLimitedPolicy(true));
            Assert.False(DmIvCapHelper.IsLimitedPolicy(false));
        }

        [Fact]
        public void IsCapEnforced_mirrors_the_policy()
        {
            Assert.True(DmIvCapHelper.IsCapEnforced("limited"));
            Assert.False(DmIvCapHelper.IsCapEnforced("free"));
            Assert.True(DmIvCapHelper.IsCapEnforced(true));
            Assert.False(DmIvCapHelper.IsCapEnforced(false));
        }

        [Fact]
        public void Cap_column_is_shown_only_under_a_limited_policy()
        {
            Assert.True(DmIvCapHelper.ShowCapColumn(true));
            Assert.False(DmIvCapHelper.ShowCapColumn(false));
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData(0, true)]
        [InlineData(1, false)]
        [InlineData(-1, false)]
        public void Explicit_zero_is_distinguished_from_null_and_positive(int? cap, bool expected) =>
            Assert.Equal(expected, DmIvCapHelper.IsExplicitZeroCap(cap));

        [Theory]
        [InlineData(null, false)]
        [InlineData(0, true)]
        [InlineData(20, true)]
        public void Cap_is_configured_whenever_a_value_is_present(int? cap, bool expected) =>
            Assert.Equal(expected, DmIvCapHelper.IsCapConfigured(cap));

        [Fact]
        public void Default_fallback_cap_is_twenty() =>
            Assert.Equal(20, DmIvCapHelper.DefaultCapFallback);
    }

    public class DmIvCapHelperGetCapStateForRowTests
    {
        // (a) Free policy → unlimited / allow, stored caps ignored.
        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(1000)]
        public void Free_policy_ignores_the_stored_cap_entirely(int? cap)
        {
            var state = DmIvCapHelper.GetCapStateForRow("free", cap, futureAssigned: 999, channelDefaultCap: 20);

            Assert.True(state.ShowUnlimited);
            Assert.False(state.IsCapEnforced);
            Assert.True(state.AssignmentAllowed);
            Assert.False(state.IsCapReached);
            Assert.False(state.IsExplicitZero);
            Assert.False(state.UsesChannelDefault);
            Assert.Equal(0, state.EffectiveCap);
        }

        // (b) Limited + null cap → channel default.
        [Fact]
        public void Limited_with_no_cap_falls_back_to_the_channel_default()
        {
            var state = DmIvCapHelper.GetCapStateForRow("limited", null, futureAssigned: 3, channelDefaultCap: 12);

            Assert.False(state.ShowUnlimited);
            Assert.True(state.IsCapEnforced);
            Assert.True(state.UsesChannelDefault);
            Assert.Equal(12, state.EffectiveCap);
            Assert.False(state.IsCapReached);
            Assert.True(state.AssignmentAllowed);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void Limited_with_no_cap_and_no_usable_channel_default_uses_the_hard_fallback(int channelDefaultCap)
        {
            var state = DmIvCapHelper.GetCapStateForRow("limited", null, 0, channelDefaultCap);

            Assert.Equal(DmIvCapHelper.DefaultCapFallback, state.EffectiveCap);
            Assert.True(state.UsesChannelDefault);
        }

        // (c) Limited + explicit 0 → 0/0, block.
        [Fact]
        public void Limited_with_an_explicit_zero_cap_blocks_all_assignment()
        {
            var state = DmIvCapHelper.GetCapStateForRow("limited", 0, futureAssigned: 0, channelDefaultCap: 20);

            Assert.False(state.ShowUnlimited);
            Assert.True(state.IsCapEnforced);
            Assert.True(state.IsExplicitZero);
            Assert.False(state.UsesChannelDefault);
            Assert.Equal(0, state.EffectiveCap);
            Assert.True(state.IsCapReached);
            Assert.False(state.AssignmentAllowed);
        }

        [Fact]
        public void Explicit_zero_blocks_regardless_of_the_channel_default()
        {
            var state = DmIvCapHelper.GetCapStateForRow("limited", 0, 0, channelDefaultCap: 500);

            Assert.Equal(0, state.EffectiveCap);
            Assert.False(state.AssignmentAllowed);
        }

        // (d)/(e) Limited + positive cap.
        [Theory]
        [InlineData(0, 5, false)]
        [InlineData(4, 5, false)]
        [InlineData(5, 5, true)]
        [InlineData(6, 5, true)]
        [InlineData(500, 5, true)]
        public void Cap_is_reached_once_future_assignments_meet_or_exceed_it(
            int futureAssigned, int cap, bool expectedReached)
        {
            var state = DmIvCapHelper.GetCapStateForRow("limited", cap, futureAssigned, 20);

            Assert.Equal(expectedReached, state.IsCapReached);
            Assert.Equal(!expectedReached, state.AssignmentAllowed);
            Assert.Equal(cap, state.EffectiveCap);
            Assert.False(state.UsesChannelDefault);
        }

        [Fact]
        public void A_negative_future_count_never_reaches_a_positive_cap()
        {
            var state = DmIvCapHelper.GetCapStateForRow("limited", 5, futureAssigned: -3, channelDefaultCap: 20);

            Assert.False(state.IsCapReached);
            Assert.True(state.AssignmentAllowed);
        }

        [Fact]
        public void A_negative_stored_cap_is_treated_as_already_reached()
        {
            // Not an explicit zero, so it flows through as an effective cap of -1 that
            // any non-negative future count already meets.
            var state = DmIvCapHelper.GetCapStateForRow("limited", -1, futureAssigned: 0, channelDefaultCap: 20);

            Assert.Equal(-1, state.EffectiveCap);
            Assert.True(state.IsCapReached);
            Assert.False(state.AssignmentAllowed);
        }

        [Fact]
        public void Assignment_allowed_is_always_the_inverse_of_cap_reached_under_a_limited_policy()
        {
            foreach (int? cap in new int?[] { null, 0, 1, 5, 20 })
                foreach (var future in new[] { 0, 1, 5, 20, 100 })
                {
                    var state = DmIvCapHelper.GetCapStateForRow("limited", cap, future, 20);
                    Assert.Equal(!state.IsCapReached, state.AssignmentAllowed);
                }
        }

        [Fact]
        public void Boolean_overload_matches_the_string_overload()
        {
            foreach (int? cap in new int?[] { null, 0, 7 })
                foreach (var future in new[] { 0, 7, 100 })
                {
                    var fromBool = DmIvCapHelper.GetCapStateForRow(true, cap, future, 20);
                    var fromString = DmIvCapHelper.GetCapStateForRow("limited", cap, future, 20);

                    Assert.Equal(fromString.EffectiveCap, fromBool.EffectiveCap);
                    Assert.Equal(fromString.IsCapReached, fromBool.IsCapReached);
                    Assert.Equal(fromString.AssignmentAllowed, fromBool.AssignmentAllowed);
                    Assert.Equal(fromString.ShowUnlimited, fromBool.ShowUnlimited);
                }
        }
    }

    public class DmIvCapHelperConvenienceWrapperTests
    {
        [Fact]
        public void Effective_cap_under_a_free_policy_is_zero_because_no_cap_applies()
        {
            // Pinned so callers never render "0 of 0" for a free channel — check
            // ShowUnlimited (or IsCapEnforced) before showing EffectiveCap.
            Assert.Equal(0, DmIvCapHelper.GetEffectiveCap(limitedPolicy: false, interviewCapacity: 99, channelDefaultCap: 20));
        }

        [Theory]
        [InlineData(7, 20, 7)]
        [InlineData(null, 12, 12)]
        [InlineData(null, 0, 20)]
        [InlineData(0, 20, 0)]
        public void Effective_cap_under_a_limited_policy_resolves_the_configured_value(
            int? cap, int channelDefaultCap, int expected) =>
            Assert.Equal(expected, DmIvCapHelper.GetEffectiveCap(true, cap, channelDefaultCap));

        [Fact]
        public void GetDisplayCap_is_an_alias_for_GetEffectiveCap()
        {
            foreach (int? cap in new int?[] { null, 0, 9 })
                foreach (var limited in new[] { true, false })
                    Assert.Equal(
                        DmIvCapHelper.GetEffectiveCap(limited, cap, 20),
                        DmIvCapHelper.GetDisplayCap(cap, 20, limited));
        }

        [Theory]
        [InlineData(3, 5, true, false)]
        [InlineData(5, 5, true, true)]
        [InlineData(500, 5, false, false)]   // free policy → never reached
        [InlineData(0, 0, true, true)]       // explicit zero
        public void IsCapReached_wrapper_matches_the_full_state(
            int futureAssigned, int? cap, bool limited, bool expected) =>
            Assert.Equal(expected, DmIvCapHelper.IsCapReached(futureAssigned, cap, limited, 20));

        [Theory]
        [InlineData(false, null, true)]
        [InlineData(false, 5, true)]
        [InlineData(true, null, false)]
        [InlineData(true, 0, false)]
        [InlineData(true, 5, false)]
        public void ShowUnlimited_is_true_exactly_when_the_policy_is_free(
            bool limited, int? cap, bool expected) =>
            Assert.Equal(expected, DmIvCapHelper.ShowUnlimited(limited, cap));

        [Fact]
        public void The_lock_aware_ShowUnlimited_overload_ignores_the_lock_flag()
        {
            foreach (var limited in new[] { true, false })
                foreach (int? cap in new int?[] { null, 0, 5 })
                {
                    Assert.Equal(
                        DmIvCapHelper.ShowUnlimited(limited, cap),
                        DmIvCapHelper.ShowUnlimited(limited, cap, lockOn: true));
                    Assert.Equal(
                        DmIvCapHelper.ShowUnlimited(limited, cap),
                        DmIvCapHelper.ShowUnlimited(limited, cap, lockOn: false));
                }
        }
    }
}
