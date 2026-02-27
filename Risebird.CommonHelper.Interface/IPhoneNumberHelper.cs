namespace Risebird.CommonHelper.Interface
{
    public interface IPhoneNumberHelper
    {
        /// <summary>
        /// Sanitizes a raw phone number string into E.164 format (+91 followed by 10 digits).
        /// Returns true on success; false with an error message on failure.
        /// </summary>
        bool TrySanitizeIndianPhoneNumber(string rawPhone, out string sanitized, out string error);
    }
}
