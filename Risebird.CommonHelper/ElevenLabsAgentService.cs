using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data.ElevenLabsModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// ElevenLabs Agents Platform client for the WhatsApp phone screener.
    ///
    /// Outbound is a single REST call to /v1/convai/whatsapp/outbound-message: it sends
    /// an approved WhatsApp template and attaches the agent + dynamic variables. When the
    /// conversation ends, ElevenLabs POSTs a post_call_transcription webhook (transcript,
    /// analysis, data collection) to our endpoint, HMAC-signed with the webhook secret.
    ///
    /// Configuration keys (appsettings.json -> "ElevenLabs"):
    ///   ApiKey                    xi-api-key used for all requests
    ///   BaseUrl                   API base (defaults to https://api.elevenlabs.io)
    ///   WhatsAppPhoneNumberId     the connected WhatsApp sender's phone number id
    ///   WhatsAppTemplateName      approved Meta template used to open the conversation
    ///   WhatsAppTemplateLanguage  template language code (e.g. "en")
    ///   WebhookSecret             shared secret for verifying post-call webhooks
    /// </summary>
    public class ElevenLabsAgentService : IElevenLabsAgentService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly string _whatsAppPhoneNumberId;
        private readonly string _templateName;
        private readonly string _templateLanguage;
        private readonly string _webhookSecret;

        public ElevenLabsAgentService(IConfiguration configuration)
        {
            _apiKey = configuration["ElevenLabs:ApiKey"] ?? string.Empty;
            _baseUrl = (configuration["ElevenLabs:BaseUrl"] ?? "https://api.elevenlabs.io").TrimEnd('/');
            _whatsAppPhoneNumberId = configuration["ElevenLabs:WhatsAppPhoneNumberId"] ?? string.Empty;
            _templateName = configuration["ElevenLabs:WhatsAppTemplateName"] ?? string.Empty;
            _templateLanguage = string.IsNullOrWhiteSpace(configuration["ElevenLabs:WhatsAppTemplateLanguage"])
                ? "en" : configuration["ElevenLabs:WhatsAppTemplateLanguage"];
            _webhookSecret = configuration["ElevenLabs:WebhookSecret"] ?? string.Empty;

            _httpClient = new HttpClient();
            if (!string.IsNullOrEmpty(_apiKey))
                _httpClient.DefaultRequestHeaders.Add("xi-api-key", _apiKey);
        }

        public async Task<ElevenLabsWhatsAppOutboundResponse> SendWhatsAppScreenAsync(
            string agentId,
            string candidatePhone,
            IDictionary<string, object> dynamicVariables,
            IList<string> templateBodyParams)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                return new ElevenLabsWhatsAppOutboundResponse { Error = "ElevenLabs API key is not configured (ElevenLabs:ApiKey)." };

            if (string.IsNullOrWhiteSpace(_whatsAppPhoneNumberId))
                return new ElevenLabsWhatsAppOutboundResponse { Error = "ElevenLabs WhatsApp phone number id is not configured (ElevenLabs:WhatsAppPhoneNumberId)." };

            if (string.IsNullOrWhiteSpace(agentId))
                return new ElevenLabsWhatsAppOutboundResponse { Error = "No ElevenLabs agent id is mapped to this skill channel." };

            var userId = SanitizeWhatsAppUserId(candidatePhone);
            if (string.IsNullOrWhiteSpace(userId))
                return new ElevenLabsWhatsAppOutboundResponse { Error = "Candidate phone number is missing or invalid." };

            var bodyParams = (templateBodyParams ?? new List<string>())
                .Select(p => new ElevenLabsTemplateTextParam { Text = p ?? string.Empty })
                .ToList();

            var request = new ElevenLabsWhatsAppOutboundRequest
            {
                WhatsAppPhoneNumberId = _whatsAppPhoneNumberId,
                WhatsAppUserId = userId,
                TemplateName = _templateName,
                TemplateLanguageCode = _templateLanguage,
                TemplateParams = new List<ElevenLabsTemplateComponent>
                {
                    new ElevenLabsTemplateComponent { Type = "body", Parameters = bodyParams }
                },
                AgentId = agentId,
                ConversationInitiationClientData = new ElevenLabsConversationInitiationClientData
                {
                    DynamicVariables = dynamicVariables != null
                        ? new Dictionary<string, object>(dynamicVariables)
                        : new Dictionary<string, object>()
                }
            };

            var json = JsonConvert.SerializeObject(request, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var httpResponse = await _httpClient.PostAsync($"{_baseUrl}/v1/convai/whatsapp/outbound-message", content);
                var responseBody = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                    return new ElevenLabsWhatsAppOutboundResponse
                    {
                        Error = $"HTTP {(int)httpResponse.StatusCode}: {responseBody}",
                        HttpStatusCode = (int)httpResponse.StatusCode,
                        RawResponseBody = responseBody
                    };

                var result = JsonConvert.DeserializeObject<ElevenLabsWhatsAppOutboundResponse>(responseBody)
                             ?? new ElevenLabsWhatsAppOutboundResponse();
                result.HttpStatusCode = (int)httpResponse.StatusCode;
                result.RawResponseBody = responseBody;
                return result;
            }
            catch (Exception ex)
            {
                return new ElevenLabsWhatsAppOutboundResponse { Error = ex.Message };
            }
        }

        public bool VerifyWebhookSignature(string rawBody, string signatureHeader)
        {
            // No secret configured yet -> accept (lets the feature run before setup).
            if (string.IsNullOrWhiteSpace(_webhookSecret))
                return true;

            if (string.IsNullOrWhiteSpace(signatureHeader) || string.IsNullOrEmpty(rawBody))
                return false;

            // Header format: "t=<unix_ts>,v0=<hex_hmac_sha256>".
            string timestamp = null, provided = null;
            foreach (var part in signatureHeader.Split(','))
            {
                var kv = part.Split(new[] { '=' }, 2);
                if (kv.Length != 2) continue;
                var key = kv[0].Trim();
                var val = kv[1].Trim();
                if (key == "t") timestamp = val;
                else if (key == "v0") provided = val;
            }

            if (string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(provided))
                return false;

            // Reject stale signatures (guards against replay) — 30 minute window.
            if (long.TryParse(timestamp, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ts))
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (Math.Abs(now - ts) > 60 * 30) return false;
            }

            var signedPayload = $"{timestamp}.{rawBody}";
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_webhookSecret)))
            {
                var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
                var computed = BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLowerInvariant();
                var expected = provided.StartsWith("v0=") ? provided.Substring(3) : provided;
                return FixedTimeEquals(computed, expected.ToLowerInvariant());
            }
        }

        /// <summary>Digits-only international WhatsApp id; assumes India (91) for bare 10-digit numbers.</summary>
        private static string SanitizeWhatsAppUserId(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return null;
            var digits = new string(phone.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(digits)) return null;

            if (digits.Length == 10) return "91" + digits;                 // bare local number
            if (digits.Length == 11 && digits.StartsWith("0")) return "91" + digits.Substring(1);
            if (digits.Length == 12 && digits.StartsWith("91")) return digits; // already +91
            return digits;                                                  // trust provided country code
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            var diff = 0;
            for (var i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
