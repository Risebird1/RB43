using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// Interakt webhooks (Developer Settings → Webhook URL + secret). Every call carries
    /// <c>Interakt-Signature: sha256=&lt;hex HMAC-SHA256 of the raw body with the secret&gt;</c>; an incoming WhatsApp message is
    /// <c>type = "message_received"</c> with the sender in <c>data.customer.channel_phone_number</c> (country code included,
    /// no '+') and the text in <c>data.message.message</c> (a button / quick-reply arrives as stringified JSON).
    /// </summary>
    public static class InteraktWebhook
    {
        public const string SignatureHeader = "Interakt-Signature";

        // Words that mean "stop messaging me" - the C3 template's "Stop" quick reply and typed replies.
        private static readonly string[] StopWords = { "stop", "unsubscribe", "stop all", "opt out", "optout" };

        public static bool VerifySignature(string rawBody, string header, string secret)
        {
            if (string.IsNullOrEmpty(rawBody) || string.IsNullOrWhiteSpace(header) || string.IsNullOrWhiteSpace(secret))
                return false;
            var received = header.Trim();
            if (received.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
                received = received.Substring("sha256=".Length);

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody))).ToLowerInvariant();
            return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(received.ToLowerInvariant()));
        }

        /// <summary>The sender's number and message text of a message_received payload; null when it is another event.</summary>
        public static (string Phone, string Text)? IncomingMessage(JObject root)
        {
            if (root == null || !string.Equals((string)root["type"], "message_received", StringComparison.OrdinalIgnoreCase))
                return null;
            var phone = (string)root.SelectToken("data.customer.channel_phone_number");
            var text = root.SelectToken("data.message.message")?.ToString();
            return (Digits(phone), ReplyText(text));
        }

        /// <summary>The visible text of a reply: plain text as is; a button / quick reply's title or payload from its JSON.</summary>
        public static string ReplyText(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return "";
            var trimmed = message.Trim();
            if (trimmed.StartsWith("{"))
            {
                try
                {
                    var json = JObject.Parse(trimmed);
                    foreach (var key in new[] { "title", "text", "body", "payload", "button_text" })
                    {
                        var value = json.SelectTokens("$.." + key).Select(t => t.ToString()).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
                        if (value != null) return value.Trim();
                    }
                }
                catch (Exception) { }
            }
            return trimmed;
        }

        public static bool IsStop(string text)
        {
            var normalized = Regex.Replace((text ?? "").ToLowerInvariant(), @"[^a-z ]", " ");
            normalized = Regex.Replace(normalized, @"\s+", " ").Trim();
            return StopWords.Contains(normalized);
        }

        public static string Digits(string phone) => string.IsNullOrEmpty(phone) ? "" : Regex.Replace(phone, @"\D", "");

        /// <summary>The national number used to find the portal user: the last 10 digits (Indian numbers), or all of a shorter one.</summary>
        public static string LastDigits(string digits) =>
            string.IsNullOrEmpty(digits) ? "" : digits.Length > 10 ? digits.Substring(digits.Length - 10) : digits;
    }
}
