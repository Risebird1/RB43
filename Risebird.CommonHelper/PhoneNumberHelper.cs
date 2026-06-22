using System.Text.RegularExpressions;
using Risebird.CommonHelper.Interface;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// Helpers for validating and normalizing phone numbers.
    /// </summary>
    public class PhoneNumberHelper : IPhoneNumberHelper
    {
        /// <summary>
        /// Sanitizes a raw phone number string into E.164 format (+91 followed by 10 digits).
        /// Returns true on success; false with an error message on failure.
        /// </summary>
        public bool TrySanitizeIndianPhoneNumber(string rawPhone, out string sanitized, out string error)
        {
            sanitized = null;
            error = null;

            if (string.IsNullOrWhiteSpace(rawPhone))
            {
                error = "Phone number is empty.";
                return false;
            }

            // Keep digits only (drops '+', spaces, dashes, parentheses, etc.).
            string digits = Regex.Replace(rawPhone, @"\D", "");

            if (string.IsNullOrEmpty(digits))
            {
                error = $"Invalid phone number: '{rawPhone}'.";
                return false;
            }

            // Strip the country code ("91"/"0091") or a leading trunk '0' so that we are
            // left with the bare 10-digit national number.
            if (digits.Length > 10)
                digits = digits.Substring(digits.Length - 10);

            if (digits.Length != 10)
            {
                error = $"Invalid Indian phone number: '{rawPhone}'. Expected a 10-digit mobile number.";
                return false;
            }

            // Indian mobile numbers start with 6, 7, 8 or 9.
            if (digits[0] < '6')
            {
                error = $"Invalid Indian mobile number: '{rawPhone}'. Mobile numbers must start with 6-9.";
                return false;
            }

            sanitized = "+91" + digits;
            return true;
        }

        /// <summary>
        /// Strips the country code (and any formatting) from an Indian mobile number,
        /// returning the bare national 10-digit number.
        /// Signzy's Employment Verification API rejects numbers that carry a country
        /// code prefix (e.g. "+91"), so callers must normalize before sending.
        /// Handles inputs like "+91 98765-43210", "0091 9876543210", "919876543210",
        /// "09876543210" -> "9876543210". Inputs that are already 10 digits (or that
        /// cannot be confidently normalized) are returned with only formatting removed.
        /// </summary>
        public static string StripCountryCode(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return phone;

            // Drop everything except digits (removes '+', spaces, dashes, parentheses, etc.).
            string digits = Regex.Replace(phone, @"\D", "");

            if (string.IsNullOrEmpty(digits))
                return phone;

            // Indian mobile numbers are 10 digits. Anything longer carries a country
            // code (91 / 0091) or a leading trunk '0', so keep only the last 10 digits.
            if (digits.Length > 10)
                digits = digits.Substring(digits.Length - 10);

            return digits;
        }
    }
}
