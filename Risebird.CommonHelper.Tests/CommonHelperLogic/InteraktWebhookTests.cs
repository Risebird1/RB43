using Newtonsoft.Json.Linq;
using System;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Risebird.CommonHelper.Tests.CommonHelperLogic
{
    /// <summary>
    /// The Interakt webhook that records WhatsApp "Stop" replies: signature check, payload reading and what counts
    /// as Stop. Payload shape from Interakt's Message Webhook Format.
    /// </summary>
    public class InteraktWebhookTests
    {
        private const string Secret = "test-secret";

        private static string Sign(string body)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Secret));
            return "sha256=" + Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
        }

        private static string Payload(string message, string type = "message_received") =>
            new JObject
            {
                ["version"] = "1.0",
                ["type"] = type,
                ["data"] = new JObject
                {
                    ["customer"] = new JObject { ["channel_phone_number"] = "919876543210" },
                    ["message"] = new JObject { ["chat_message_type"] = "CustomerMessage", ["message_content_type"] = "Text", ["message"] = message }
                }
            }.ToString();

        [Fact]
        public void A_correctly_signed_body_verifies()
        {
            var body = Payload("Stop");
            Assert.True(InteraktWebhook.VerifySignature(body, Sign(body), Secret));
            Assert.True(InteraktWebhook.VerifySignature(body, Sign(body).ToUpperInvariant().Replace("SHA256=", "sha256="), Secret));
        }

        [Fact]
        public void A_wrong_or_missing_signature_or_secret_fails()
        {
            var body = Payload("Stop");
            Assert.False(InteraktWebhook.VerifySignature(body, Sign(body + " "), Secret));
            Assert.False(InteraktWebhook.VerifySignature(body, null, Secret));
            Assert.False(InteraktWebhook.VerifySignature(body, Sign(body), ""));
        }

        [Fact]
        public void A_received_message_gives_the_number_and_text()
        {
            var m = InteraktWebhook.IncomingMessage(JObject.Parse(Payload("Stop")));

            Assert.NotNull(m);
            Assert.Equal("919876543210", m.Value.Phone);
            Assert.Equal("Stop", m.Value.Text);
            Assert.Equal("9876543210", InteraktWebhook.LastDigits(m.Value.Phone));
        }

        [Fact]
        public void Other_events_are_ignored()
        {
            Assert.Null(InteraktWebhook.IncomingMessage(JObject.Parse(Payload("Stop", "message_api_delivered"))));
        }

        [Fact]
        public void A_quick_reply_sent_as_json_is_read_by_its_title()
        {
            Assert.Equal("Stop", InteraktWebhook.ReplyText("{\"type\":\"button_reply\",\"button_reply\":{\"id\":\"1\",\"title\":\"Stop\"}}"));
        }

        [Theory]
        [InlineData("Stop", true)]
        [InlineData(" STOP. ", true)]
        [InlineData("unsubscribe", true)]
        [InlineData("Stop all", true)]
        [InlineData("Please don't stop my interview", false)]
        [InlineData("Yes", false)]
        [InlineData("", false)]
        public void Only_a_stop_reply_counts(string text, bool isStop)
        {
            Assert.Equal(isStop, InteraktWebhook.IsStop(text));
        }
    }
}
