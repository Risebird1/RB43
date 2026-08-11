using Microsoft.AspNetCore.Http;
using System;
using System.Globalization;

namespace Risebird.Web.Helpers
{
    /// <summary>
    /// Shared plumbing for the inbound automation webhooks — Relay.app while workflows are
    /// still being migrated, n8n afterwards. Keeps secret validation, timestamp handling and
    /// platform attribution identical across every workflow endpoint.
    /// </summary>
    public static class AutomationWebhookHelper
    {
        // 'X-Relay-Secret' is the original Relay.app header and stays supported so both
        // platforms can post to the same endpoint while workflows move over one at a time.
        private static readonly string[] SecretHeaders =
            { "X-Webhook-Secret", "X-Relay-Secret", "X-N8N-Secret" };

        public static bool IsSecretValid(IHeaderDictionary headers, string expectedSecret)
        {
            if (string.IsNullOrWhiteSpace(expectedSecret))
                return true;   // validation switched off by configuration

            if (headers == null)
                return false;

            foreach (var header in SecretHeaders)
            {
                var incoming = headers[header].ToString();
                if (!string.IsNullOrEmpty(incoming) && incoming == expectedSecret)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Parses a webhook timestamp to UTC. Relay sent ISO-8601; n8n forwards Gmail's
        /// RFC 2822 date, so both have to normalize to a value comparable with UtcNow.
        /// </summary>
        public static DateTime? ParseTimestamp(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
                       DateTimeStyles.AssumeUniversal, out var offset)
                ? offset.UtcDateTime
                : (DateTime?)null;
        }

        /// <summary>
        /// Normalizes the automation platform reported in the payload. Payloads that carry no
        /// platform are legacy Relay.app calls.
        /// </summary>
        public static string ResolvePlatform(string source)
        {
            return string.Equals((source ?? "").Trim(), "n8n", StringComparison.OrdinalIgnoreCase)
                ? "n8n"
                : "Relay";
        }
    }
}
