using System;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Unified cap display/enforcement state for one interviewer row on a channel.
    /// Option B: per-row InterviewCapacity is always stored; assignmentPolicy gates enforcement.
    /// </summary>
    public sealed class DmIvCapRowState
    {
        public bool ShowUnlimited { get; init; }
        public bool IsCapEnforced { get; init; }
        public bool IsExplicitZero { get; init; }
        public bool UsesChannelDefault { get; init; }
        public int EffectiveCap { get; init; }
        public bool IsCapReached { get; init; }
        public bool AssignmentAllowed { get; init; }
    }

    public static class DmIvCapHelper
    {
        public const int DefaultCapFallback = 20;

        /// <summary>True when channel assignment policy is "limited".</summary>
        public static bool IsLimitedPolicy(string assignmentPolicy) =>
            string.Equals(assignmentPolicy?.Trim(), "limited", StringComparison.OrdinalIgnoreCase);

        public static bool IsLimitedPolicy(bool limitedPolicy) => limitedPolicy;

        public static bool IsExplicitZeroCap(int? interviewCapacity) =>
            interviewCapacity.HasValue && interviewCapacity.Value == 0;

        public static bool IsCapConfigured(int? interviewCapacity) =>
            interviewCapacity.HasValue;

        /// <summary>DHI table: show per-interviewer limit column when channel policy is "limited".</summary>
        public static bool ShowCapColumn(bool limitedPolicy) => limitedPolicy;

        /// <summary>
        /// Single decision tree for cap display and assignment enforcement.
        /// (a) Free policy → unlimited / allow.
        /// (b) Limited + null cap → channel default (implicit 20).
        /// (c) Limited + explicit 0 → 0/0 / block.
        /// (d) Limited + cap &gt; 0 and count ≥ cap → warn / block.
        /// (e) Limited + cap &gt; 0 and count &lt; cap → progress / allow.
        /// </summary>
        public static DmIvCapRowState GetCapStateForRow(
            string assignmentPolicy,
            int? interviewCapacity,
            int futureAssigned,
            int channelDefaultCap)
        {
            if (!IsLimitedPolicy(assignmentPolicy))
            {
                // Free mode: stored caps are ignored (Option B shadow state).
                return new DmIvCapRowState
                {
                    ShowUnlimited = true,
                    IsCapEnforced = false,
                    AssignmentAllowed = true
                };
            }

            if (IsExplicitZeroCap(interviewCapacity))
            {
                return new DmIvCapRowState
                {
                    ShowUnlimited = false,
                    IsCapEnforced = true,
                    IsExplicitZero = true,
                    EffectiveCap = 0,
                    IsCapReached = true,
                    AssignmentAllowed = false
                };
            }

            var usesDefault = !interviewCapacity.HasValue;
            var effective = interviewCapacity
                ?? (channelDefaultCap > 0 ? channelDefaultCap : DefaultCapFallback);
            var reached = futureAssigned >= effective;

            return new DmIvCapRowState
            {
                ShowUnlimited = false,
                IsCapEnforced = true,
                UsesChannelDefault = usesDefault,
                EffectiveCap = effective,
                IsCapReached = reached,
                AssignmentAllowed = !reached
            };
        }

        public static DmIvCapRowState GetCapStateForRow(
            bool limitedPolicy,
            int? interviewCapacity,
            int futureAssigned,
            int channelDefaultCap) =>
            GetCapStateForRow(limitedPolicy ? "limited" : "free", interviewCapacity, futureAssigned, channelDefaultCap);

        public static int GetEffectiveCap(bool limitedPolicy, int? interviewCapacity, int channelDefaultCap) =>
            GetCapStateForRow(limitedPolicy, interviewCapacity, 0, channelDefaultCap).EffectiveCap;

        public static bool IsCapEnforced(string assignmentPolicy) =>
            IsLimitedPolicy(assignmentPolicy);

        public static bool IsCapEnforced(bool limitedPolicy) =>
            limitedPolicy;

        public static int GetDisplayCap(int? interviewCapacity, int channelDefaultCap, bool limitedPolicy) =>
            GetEffectiveCap(limitedPolicy, interviewCapacity, channelDefaultCap);

        public static bool IsCapReached(int futureAssigned, int? interviewCapacity, bool limitedPolicy, int channelDefaultCap) =>
            GetCapStateForRow(limitedPolicy, interviewCapacity, futureAssigned, channelDefaultCap).IsCapReached;

        public static bool ShowUnlimited(bool limitedPolicy, int? interviewCapacity) =>
            GetCapStateForRow(limitedPolicy, interviewCapacity, 0, DefaultCapFallback).ShowUnlimited;

        public static bool ShowUnlimited(bool limitedPolicy, int? interviewCapacity, bool lockOn) =>
            ShowUnlimited(limitedPolicy, interviewCapacity);
    }
}
