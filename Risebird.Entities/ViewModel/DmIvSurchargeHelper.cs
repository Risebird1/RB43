using System;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Per-interviewer surcharge on DM Approval: whole-number INR amount, hard cap ₹500 (global).
    /// </summary>
    public static class DmIvSurchargeHelper
    {
        public const int MaxSurchargeInr = 500;

        public static int GetMaxSurcharge() => MaxSurchargeInr;

        public static bool TryValidate(int surcharge, out string errorMessage)
        {
            errorMessage = null;
            if (surcharge < 0)
            {
                errorMessage = "Surcharge cannot be negative.";
                return false;
            }

            if (surcharge > MaxSurchargeInr)
            {
                errorMessage = "Surcharge cannot exceed ₹500";
                return false;
            }

            return true;
        }
    }
}
