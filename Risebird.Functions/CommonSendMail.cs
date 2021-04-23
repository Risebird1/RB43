using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Net;

namespace Risebird.Functions
{
    public class CommonSendMail
    {
        private readonly string _sendGridClientKey = "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";
        public string SendMail(SendGridMessage msg)
        {
            try
            {
                var mailFrom = "notification@risebird.io";
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                var client = new SendGridClient(_sendGridClientKey);

                var response = client.SendEmailAsync(msg).Result;
                if (response.StatusCode != HttpStatusCode.Accepted)
                {
                    return "Failed to send mail. Please check all fields and try again.";

                }
                return "Mail sent successfully.";
            }
            catch (Exception ex)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
    }
}
