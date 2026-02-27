using Risebird.CommonHelper.Interface;
using System.Text.RegularExpressions;

namespace Risebird.CommonHelper
{
    public class PhoneNumberHelper : IPhoneNumberHelper
    {
        /// <summary>
        /// Sanitizes a raw phone number string into E.164 format (+91 followed by 10 digits).
        ///
        /// Handles all common input forms:
        ///   +91XXXXXXXXXX   →  +91XXXXXXXXXX  (already correct, normalise spacing)
        ///   91XXXXXXXXXX    →  +91XXXXXXXXXX  (country code without +)
        ///   0091XXXXXXXXXX  →  +91XXXXXXXXXX  (IDD prefix)
        ///   0XXXXXXXXXX     →  +91XXXXXXXXXX  (trunk prefix)
        ///   XXXXXXXXXX      →  +91XXXXXXXXXX  (bare 10-digit number)
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

            // Strip everything except digits
            var digitsOnly = Regex.Replace(rawPhone.Trim(), @"[^\d]", "");

            if (string.IsNullOrEmpty(digitsOnly))
            {
                error = $"Phone number '{rawPhone}' contains no digits.";
                return false;
            }

            // Normalise to bare 10-digit mobile number
            string tenDigits;

            if (digitsOnly.Length == 10)
            {
                tenDigits = digitsOnly;
            }
            else if (digitsOnly.Length == 11 && digitsOnly.StartsWith("0"))
            {
                // Trunk prefix: 0XXXXXXXXXX
                tenDigits = digitsOnly.Substring(1);
            }
            else if (digitsOnly.Length == 12 && digitsOnly.StartsWith("91"))
            {
                // Country code without +: 91XXXXXXXXXX
                tenDigits = digitsOnly.Substring(2);
            }
            else if (digitsOnly.Length == 14 && digitsOnly.StartsWith("0091"))
            {
                // IDD prefix: 0091XXXXXXXXXX
                tenDigits = digitsOnly.Substring(4);
            }
            else
            {
                error = $"Phone number '{rawPhone}' could not be normalised to a 10-digit Indian mobile number (got {digitsOnly.Length} digits after stripping: '{digitsOnly}').";
                return false;
            }

            // Indian mobile numbers start with 6, 7, 8, or 9
            if (!Regex.IsMatch(tenDigits, @"^[6-9]\d{9}$"))
            {
                error = $"Phone number '{rawPhone}' does not look like a valid Indian mobile number (normalised: '{tenDigits}').";
                return false;
            }

            sanitized = "+91" + tenDigits;
            return true;
        }
    }
}
