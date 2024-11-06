using System;
using System.Net.Mail;

namespace Risebird.Web.Extensions
{
    public static class EmailidValidationExtension
    {
        public static bool IsEmailAddressValid(this string emailaddress)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(emailaddress))
                {
                    MailAddress m = new MailAddress(emailaddress);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
