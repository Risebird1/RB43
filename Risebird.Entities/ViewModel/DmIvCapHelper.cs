using System;

namespace Risebird.Entities.ViewModel
{
    public static class DmIvCapHelper
    {
        /// <summary>Cap applies only on locked channels when an admin has explicitly set InterviewCapacity (including 0).</summary>
        public static bool IsCapConfigured(int? interviewCapacity) =>
            interviewCapacity.HasValue;

        public static bool IsCapEnforced(bool lockOn, int? interviewCapacity) =>
            lockOn && interviewCapacity.HasValue;

        public static int GetDisplayCap(int? interviewCapacity) =>
            interviewCapacity ?? 0;

        public static bool IsCapReached(int futureAssigned, int? interviewCapacity, bool lockOn) =>
            lockOn && interviewCapacity.HasValue && futureAssigned >= interviewCapacity.Value;

        /// <summary>DHI table: show per-interviewer limit UI only when channel policy is "limited".</summary>
        public static bool ShowCapColumn(bool limitedPolicy) =>
            limitedPolicy;

        /// <summary>DHI cell label when no explicit cap is configured, or policy is free assignment.</summary>
        public static bool ShowUnlimited(bool limitedPolicy, int? interviewCapacity) =>
            !limitedPolicy || !interviewCapacity.HasValue;

        public static bool IsExplicitZeroCap(int? interviewCapacity) =>
            interviewCapacity.HasValue && interviewCapacity.Value == 0;
    }
}
