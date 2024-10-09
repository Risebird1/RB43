using System.Net.Mail;
using System;
using System.Text.RegularExpressions;

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
