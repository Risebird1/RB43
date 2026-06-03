using System;

namespace Risebird.Entities.ViewModel
{
    public static class DmIvCapHelper
    {
        public const int DefaultCapFallback = 20;

        /// <summary>Per-row cap when set; otherwise channel default (min 1) on locked channels.</summary>
        public static int GetEffectiveCap(bool lockOn, int? interviewCapacity, int channelDefaultCap)
        {
            if (!lockOn) return 0;
            if (interviewCapacity.HasValue) return interviewCapacity.Value;
            return channelDefaultCap > 0 ? channelDefaultCap : DefaultCapFallback;
        }

        /// <summary>Cap enforcement for open-interview assign applies when the channel is locked.</summary>
        public static bool IsCapEnforced(bool lockOn) => lockOn;

        public static bool IsCapEnforced(bool lockOn, int? interviewCapacity) => lockOn;

        public static int GetDisplayCap(int? interviewCapacity, int channelDefaultCap, bool lockOn) =>
            GetEffectiveCap(lockOn, interviewCapacity, channelDefaultCap);

        public static int GetDisplayCap(int? interviewCapacity) =>
            interviewCapacity ?? 0;

        public static bool IsCapReached(int futureAssigned, int? interviewCapacity, bool lockOn, int channelDefaultCap) =>
            lockOn && futureAssigned >= GetEffectiveCap(lockOn, interviewCapacity, channelDefaultCap);

        public static bool IsCapReached(int futureAssigned, int? interviewCapacity, bool lockOn) =>
            IsCapReached(futureAssigned, interviewCapacity, lockOn, DefaultCapFallback);

        public static bool IsCapConfigured(int? interviewCapacity) =>
            interviewCapacity.HasValue;

        /// <summary>DHI table: show per-interviewer limit UI only when channel policy is "limited".</summary>
        public static bool ShowCapColumn(bool limitedPolicy) =>
            limitedPolicy;

        /// <summary>DHI cell label when no effective cap applies (free policy, or limited without lock and no row cap).</summary>
        public static bool ShowUnlimited(bool limitedPolicy, int? interviewCapacity, bool lockOn) =>
            !limitedPolicy || (!lockOn && !interviewCapacity.HasValue);

        public static bool ShowUnlimited(bool limitedPolicy, int? interviewCapacity) =>
            ShowUnlimited(limitedPolicy, interviewCapacity, false);

        public static bool IsExplicitZeroCap(int? interviewCapacity) =>
            interviewCapacity.HasValue && interviewCapacity.Value == 0;
    }
}
