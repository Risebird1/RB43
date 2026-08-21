using System;

namespace Risebird.Entities.ViewModel
{
    public static class PanVerificationHelper
    {
        public static bool CorePanChanged(TransInterviewerAccDetails previous, TransInterviewerAccDetails current)
        {
            if (previous == null || current == null)
                return true;

            return !string.Equals(NormalizePan(previous.PAN), NormalizePan(current.PAN), StringComparison.OrdinalIgnoreCase);
        }

        public static string NormalizePan(string value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
    }
}
