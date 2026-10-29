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

        private readonly IHttpClientFactory _httpClientFactory;

        public ElevenLabsAgentService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _apiKey = configuration["ElevenLabs:ApiKey"] ?? string.Empty;
            _baseUrl = (configuration["ElevenLabs:BaseUrl"] ?? "https://api.elevenlabs.io").TrimEnd('/');
            _whatsAppPhoneNumberId = configuration["ElevenLabs:WhatsAppPhoneNumberId"] ?? string.Empty;
            _templateName = configuration["ElevenLabs:WhatsAppTemplateName"] ?? string.Empty;
            _templateLanguage = string.IsNullOrWhiteSpace(configuration["ElevenLabs:WhatsAppTemplateLanguage"])
                ? "en" : configuration["ElevenLabs:WhatsAppTemplateLanguage"];
            _webhookSecret = configuration["ElevenLabs:WebhookSecret"] ?? string.Empty;

            _httpClient = _httpClientFactory.CreateClient();
            if (!string.IsNullOrEmpty(_apiKey))
                _httpClient.DefaultRequestHeaders.Add("xi-api-key", _apiKey);

            // Voice campaigns may run on another workspace (e.g. an India data-residency one, where
            // the SIP-trunk caller number is imported). Blank values fall back to the main account.
            var campaignKey = configuration["ElevenLabs:Campaigns:ApiKey"]?.Trim();
            _campaignApiKey = string.IsNullOrWhiteSpace(campaignKey) ? _apiKey : campaignKey;
            var campaignBase = configuration["ElevenLabs:Campaigns:BaseUrl"]?.Trim().TrimEnd('/');
            _campaignBaseUrl = !string.IsNullOrWhiteSpace(campaignBase) ? campaignBase
                             : ResidencyBaseUrl(_campaignApiKey) ?? _baseUrl;
            var telephony = configuration["ElevenLabs:Campaigns:Telephony"]?.Trim();
            _campaignTelephony = string.IsNullOrWhiteSpace(telephony) ? "PlivoSip" : telephony;
            _campaignPhoneNumberId = configuration["ElevenLabs:Campaigns:PhoneNumberId"]?.Trim() ?? string.Empty;
            _campaignWebhookSecret = configuration["ElevenLabs:Campaigns:WebhookSecret"]?.Trim() ?? string.Empty;
        }

        private readonly string _campaignApiKey;
        private readonly string _campaignBaseUrl;
        private readonly string _campaignTelephony;
        private readonly string _campaignPhoneNumberId;
        private readonly string _campaignWebhookSecret;

        /// <summary>Outbound voice-call path per telephony line; the body is the same for all three.</summary>
        private static readonly Dictionary<string, string> OutboundCallPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Twilio"]   = "/v1/convai/twilio/outbound-call",
            ["Exotel"]   = "/v1/convai/exotel/outbound-call",
            // Plivo (or any SIP provider) through a SIP trunk imported in ElevenLabs.
            ["PlivoSip"] = "/v1/convai/sip-trunk/outbound-call"
        };

        public string CampaignCallingProblem =>
            string.IsNullOrWhiteSpace(_campaignApiKey) ? "ElevenLabs API key is not configured (ElevenLabs:Campaigns:ApiKey or ElevenLabs:ApiKey)."
            : !OutboundCallPaths.ContainsKey(_campaignTelephony) ? $"ElevenLabs:Campaigns:Telephony \"{_campaignTelephony}\" is not supported - use PlivoSip, Twilio or Exotel."
            : string.IsNullOrWhiteSpace(_campaignPhoneNumberId) ? "ElevenLabs caller number is not configured (ElevenLabs:Campaigns:PhoneNumberId - the phnum_... id from ElevenLabs > Agents > Phone Numbers)."
            : null;

        public async Task<ElevenLabsOutboundCallResponse> PlaceCampaignCallAsync(
            string agentId, string toNumber, IDictionary<string, object> dynamicVariables)
        {
            var problem = CampaignCallingProblem;
            if (problem != null) return new ElevenLabsOutboundCallResponse { Error = problem };
            if (string.IsNullOrWhiteSpace(agentId))
                return new ElevenLabsOutboundCallResponse { Error = "The campaign's agent has no ElevenLabs agent id." };
            if (string.IsNullOrWhiteSpace(toNumber))
                return new ElevenLabsOutboundCallResponse { Error = "Destination phone number is required." };

            var request = new ElevenLabsOutboundCallRequest
            {
                AgentId = agentId.Trim(),
                AgentPhoneNumberId = _campaignPhoneNumberId,
                ToNumber = toNumber.Trim(),
                ConversationInitiationClientData = new ElevenLabsConversationInitiationClientData
                {
                    DynamicVariables = dynamicVariables != null
                        ? new Dictionary<string, object>(dynamicVariables)
                        : new Dictionary<string, object>()
                }
            };

            try
            {
                var (status, body) = await SendCampaignAsync(HttpMethod.Post, OutboundCallPaths[_campaignTelephony], request);
                if (status < 200 || status > 299)
                    return new ElevenLabsOutboundCallResponse { Error = $"HTTP {status}: {body}", HttpStatusCode = status, RawResponseBody = body };

                var result = JsonConvert.DeserializeObject<ElevenLabsOutboundCallResponse>(body) ?? new ElevenLabsOutboundCallResponse();
                result.CallSid ??= result.SipCallId;
                result.HttpStatusCode = status;
                result.RawResponseBody = body;
                if (!result.Success && string.IsNullOrEmpty(result.Error))
                    result.Error = result.Message ?? "ElevenLabs returned success=false";
                return result;
            }
            catch (Exception ex)
            {
                return new ElevenLabsOutboundCallResponse { Error = ex.Message };
            }
        }

        public Task<ElevenLabsCreateAgentResponse> CreateCampaignAgentAsync(ElevenLabsCreateAgentRequest request)
            => SendCampaignAgentAsync(HttpMethod.Post, "/v1/convai/agents/create", request, null);

        public async Task<string> FindCampaignAgentIdByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(_campaignApiKey)) return null;
            try
            {
                using var message = new HttpRequestMessage(HttpMethod.Get,
                    $"{_campaignBaseUrl}/v1/convai/agents?page_size=100&search={Uri.EscapeDataString(name.Trim())}");
                message.Headers.Add("xi-api-key", _campaignApiKey);
                using var response = await _httpClientFactory.CreateClient().SendAsync(message);
                if (!response.IsSuccessStatusCode) return null;

                var agents = Newtonsoft.Json.Linq.JObject.Parse(await response.Content.ReadAsStringAsync())["agents"] as Newtonsoft.Json.Linq.JArray;
                // Exact name only; the newest when there are several (an earlier run made duplicates).
                return agents?
                    .Where(a => string.Equals((string)a["name"], name.Trim(), StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(a => (long?)a["created_at_unix_secs"] ?? 0)
                    .Select(a => (string)a["agent_id"])
                    .FirstOrDefault(id => !string.IsNullOrWhiteSpace(id));
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Task<ElevenLabsCreateAgentResponse> UpdateCampaignAgentAsync(string agentId, ElevenLabsCreateAgentRequest request)
            => string.IsNullOrWhiteSpace(agentId)
                ? Task.FromResult(new ElevenLabsCreateAgentResponse { Error = "agent_id is required to update an agent." })
                : SendCampaignAgentAsync(HttpMethod.Patch, "/v1/convai/agents/" + Uri.EscapeDataString(agentId.Trim()), request, agentId.Trim());

        private async Task<ElevenLabsCreateAgentResponse> SendCampaignAgentAsync(
            HttpMethod method, string path, ElevenLabsCreateAgentRequest request, string knownAgentId)
        {
            if (string.IsNullOrWhiteSpace(_campaignApiKey))
                return new ElevenLabsCreateAgentResponse { Error = "ElevenLabs API key is not configured." };
            try
            {
                var (status, body) = await SendCampaignAsync(method, path, request);
                if (status < 200 || status > 299)
                    return new ElevenLabsCreateAgentResponse { Error = $"HTTP {status}: {body}", HttpStatusCode = status, RawResponseBody = body };

                var result = JsonConvert.DeserializeObject<ElevenLabsCreateAgentResponse>(body) ?? new ElevenLabsCreateAgentResponse();
                // An update is addressed by id, so a 2xx confirms it even when the body omits agent_id.
                if (string.IsNullOrWhiteSpace(result.AgentId)) result.AgentId = knownAgentId;
                result.Success = !string.IsNullOrWhiteSpace(result.AgentId);
                result.HttpStatusCode = status;
                result.RawResponseBody = body;
                if (!result.Success) result.Error = "ElevenLabs did not return an agent_id";
                return result;
            }
            catch (Exception ex)
            {
                return new ElevenLabsCreateAgentResponse { Error = ex.Message };
            }
        }

        /// <summary>A request to the campaign account; its key is set per request, not on the shared client.</summary>
        private async Task<(int Status, string Body)> SendCampaignAsync(HttpMethod method, string path, object payload)
        {
            using var message = new HttpRequestMessage(method, _campaignBaseUrl + path);
            message.Headers.Add("xi-api-key", _campaignApiKey);
            message.Content = new StringContent(
                JsonConvert.SerializeObject(payload, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
                Encoding.UTF8, "application/json");
            var client = _httpClientFactory.CreateClient();
            using var response = await client.SendAsync(message);
            return ((int)response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// A data-residency key (sk_..._residency_in / _eu / _sg) only works against its own
        /// regional API; null for a normal key.
        /// </summary>
        private static string ResidencyBaseUrl(string apiKey)
        {
            var key = apiKey?.Trim() ?? string.Empty;
            if (key.EndsWith("_residency_in", StringComparison.OrdinalIgnoreCase)) return "https://api.in.residency.elevenlabs.io";
            if (key.EndsWith("_residency_eu", StringComparison.OrdinalIgnoreCase)) return "https://api.eu.residency.elevenlabs.io";
            if (key.EndsWith("_residency_sg", StringComparison.OrdinalIgnoreCase)) return "https://api.sg.residency.elevenlabs.io";
            return null;
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
            // The main account and the campaign account (when separate) each sign with their own secret.
            var secrets = new[] { _webhookSecret, _campaignWebhookSecret }
                .Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.Ordinal).ToList();

            // No secret configured yet -> accept (lets the feature run before setup).
            if (secrets.Count == 0)
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
            var expected = (provided.StartsWith("v0=") ? provided.Substring(3) : provided).ToLowerInvariant();
            foreach (var secret in secrets)
            {
                using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
                {
                    var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
                    var computed = BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLowerInvariant();
                    if (FixedTimeEquals(computed, expected)) return true;
                }
            }
            return false;
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
