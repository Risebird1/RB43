using System;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Per-interviewer surcharge on DM Approval: whole-number INR amount, max = base interview rate (100% of cost).
    /// </summary>
    public static class DmIvSurchargeHelper
    {
        public static int GetMaxSurcharge(int perSuccessRate) =>
            Math.Max(0, perSuccessRate);

        public static bool TryValidate(int surcharge, int perSuccessRate, out string errorMessage)
        {
            errorMessage = null;
            if (surcharge < 0)
            {
                errorMessage = "Surcharge cannot be negative.";
                return false;
            }

            var max = GetMaxSurcharge(perSuccessRate);
            if (perSuccessRate <= 0 && surcharge > 0)
            {
                errorMessage = "Set a per-interview rate before applying a surcharge.";
                return false;
            }

            if (surcharge > max)
            {
                errorMessage = $"Surcharge cannot exceed 100% of the interview cost (max ₹{max:N0}).";
                return false;
            }

            return true;
        }
    }
}
