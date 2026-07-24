using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data.PlivoCXModels;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// Triggers outbound AI voice calls via the Plivo CX "API Request" trigger endpoint.
    ///
    /// How Plivo CX works (vs. Plivo Programmable Voice):
    ///   - You build a flow once in the Plivo CX dashboard (cx.plivo.com)
    ///   - The flow contains: API Trigger → Initiate Call node → AI Conversation node (with your instructions) → HTTP Request node (for booking)
    ///   - We POST candidate variables to the trigger URL to start each call
    ///   - Plivo CX handles STT + LLM + TTS internally — no WebSocket bridge needed
    ///   - On call end, Plivo CX POSTs extracted variables to our Hangup URL
    ///
    /// Configuration keys (appsettings.json → "PlivoCX" section):
    ///   TriggerEndpointUrl  — full URL copied from Plivo CX flow trigger settings
    ///   ApiAuthId           — Plivo CX API Auth ID (Settings → Authentication in cx.plivo.com)
    ///   ApiAuthToken        — Plivo CX API Auth Token
    /// </summary>
    public class PlivoCXService : IPlivoCXService
    {
        private readonly HttpClient _httpClient;
        private readonly string     _triggerEndpointUrl;
        private readonly string     _rescheduleTriggerEndpointUrl;
        private readonly string     _onboardingTriggerEndpointUrl;
        private readonly string     _availabilityTriggerEndpointUrl;

        // Plivo Voice REST API credentials (recording + transcription fallback).
        private readonly string     _voiceAuthId;
        private readonly string     _voiceBasicAuth;

        public PlivoCXService(IConfiguration configuration)
        {
            var authId    = configuration["PlivoCX:ApiAuthId"]    ?? string.Empty;
            var authToken = configuration["PlivoCX:ApiAuthToken"]  ?? string.Empty;
            _triggerEndpointUrl           = configuration["PlivoCX:TriggerEndpointUrl"]           ?? string.Empty;
            _rescheduleTriggerEndpointUrl = configuration["PlivoCX:RescheduleTriggerEndpointUrl"] ?? string.Empty;
            _onboardingTriggerEndpointUrl = configuration["PlivoCX:OnboardingTriggerEndpointUrl"] ?? string.Empty;
            _availabilityTriggerEndpointUrl = configuration["PlivoCX:AvailabilityTriggerEndpointUrl"] ?? string.Empty;

            // Voice API uses the account auth id/token (falls back to the CX creds,
            // which share the same auth id on this account).
            _voiceAuthId = configuration["Plivo:AuthId"] ?? authId;
            var voiceToken = configuration["Plivo:AuthToken"] ?? authToken;
            _voiceBasicAuth = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{_voiceAuthId}:{voiceToken}"));

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"{authId}:{authToken}")));
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public Task<PlivoCXTriggerResponse> TriggerOutboundCallAsync(PlivoCXTriggerRequest request)
            => PostTriggerAsync(_triggerEndpointUrl, request, "PlivoCX:TriggerEndpointUrl");

        public Task<PlivoCXTriggerResponse> TriggerRescheduleCallAsync(PlivoCXTriggerRequest request)
            => PostTriggerAsync(_rescheduleTriggerEndpointUrl, request, "PlivoCX:RescheduleTriggerEndpointUrl");

        public Task<PlivoCXTriggerResponse> TriggerOnboardingCallAsync(PlivoCXOnboardingTriggerRequest request)
            => PostTriggerAsync(_onboardingTriggerEndpointUrl, request, "PlivoCX:OnboardingTriggerEndpointUrl");

        public Task<PlivoCXTriggerResponse> TriggerAvailabilityCallAsync(PlivoCXAvailabilityTriggerRequest request)
            => PostTriggerAsync(_availabilityTriggerEndpointUrl, request, "PlivoCX:AvailabilityTriggerEndpointUrl");

        public Task<PlivoCXTriggerResponse> TriggerByUrlAsync(string triggerUrl, object request)
            => PostTriggerAsync(triggerUrl, request, "PlivoAssistantUrl (skill channel)");

        /// <summary>
        /// Resolves recording URL + transcript for a call via Plivo's Voice REST APIs.
        /// Step 1: GET /Recording/?call_uuid=... to find the recording (url, id, duration).
        /// Step 2: GET /Transcription/{recording_id}/ for the transcript text; if the
        ///         transcription has not been generated yet, request its creation
        ///         (POST) so it is available on a later refresh. Never throws.
        /// </summary>
        public async Task<PhoneScreenMediaResult> FetchCallMediaAsync(string callUuid)
        {
            var result = new PhoneScreenMediaResult();
            if (string.IsNullOrWhiteSpace(callUuid) || string.IsNullOrWhiteSpace(_voiceAuthId))
            {
                result.Error = "Missing call UUID or Plivo Voice credentials.";
                return result;
            }

            try
            {
                // ── Recording ────────────────────────────────────────────────
                var recUrl = $"https://api.plivo.com/v1/Account/{_voiceAuthId}/Recording/" +
                             $"?call_uuid={Uri.EscapeDataString(callUuid)}&limit=1";
                var recBody = await SendVoiceApiAsync(HttpMethod.Get, recUrl);
                string recordingId = null;

                if (!string.IsNullOrWhiteSpace(recBody))
                {
                    var recJson = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(recBody);
                    var obj = recJson?["objects"]?.FirstOrDefault();
                    if (obj != null)
                    {
                        result.RecordingUrl = obj["recording_url"]?.ToString();
                        recordingId = obj["recording_id"]?.ToString();
                        if (int.TryParse(obj["duration"]?.ToString(), out var dur))
                            result.DurationSeconds = dur;
                        result.RecordingId = recordingId;
                    }
                }

                // ── Transcript ───────────────────────────────────────────────
                if (!string.IsNullOrWhiteSpace(recordingId))
                {
                    var txUrl = $"https://api.plivo.com/v1/Account/{_voiceAuthId}/Transcription/{recordingId}/?type=diarized";
                    var txBody = await SendVoiceApiAsync(HttpMethod.Get, txUrl);
                    if (!string.IsNullOrWhiteSpace(txBody))
                    {
                        var txJson = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(txBody);
                        var text = txJson?["transcription"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(text) && text.Trim() != "{}")
                            result.Transcript = text;
                    }

                    // Not transcribed yet — request creation so a later refresh succeeds.
                    if (string.IsNullOrWhiteSpace(result.Transcript))
                    {
                        var createUrl = $"https://api.plivo.com/v1/Account/{_voiceAuthId}/Transcription/{recordingId}/";
                        _ = await SendVoiceApiAsync(HttpMethod.Post, createUrl,
                            "{\"transcription_callback_url\":\"\"}");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
            }

            return result;
        }

        private async Task<string> SendVoiceApiAsync(HttpMethod method, string url, string jsonBody = null)
        {
            try
            {
                using (var req = new HttpRequestMessage(method, url))
                {
                    req.Headers.Authorization = new AuthenticationHeaderValue("Basic", _voiceBasicAuth);
                    req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    if (jsonBody != null)
                        req.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                    var resp = await _httpClient.SendAsync(req);
                    if (!resp.IsSuccessStatusCode) return null;
                    return await resp.Content.ReadAsStringAsync();
                }
            }
            catch
            {
                return null;
            }
        }

        private async Task<PlivoCXTriggerResponse> PostTriggerAsync(
            string triggerUrl, object request, string configKeyName)
        {
            if (string.IsNullOrEmpty(triggerUrl))
                throw new InvalidOperationException(
                    $"{configKeyName} is not configured. " +
                    "Copy the trigger URL from the Plivo CX flow (AI Studio → Select flow → Trigger node → API URL) " +
                    "and set it in appsettings.json.");

            var json     = JsonConvert.SerializeObject(request);
            var content  = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(triggerUrl, content);
            var body     = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return new PlivoCXTriggerResponse
                {
                    Error           = $"HTTP {(int)response.StatusCode}: {body}",
                    Message         = "Plivo CX trigger failed",
                    RawResponseBody = body,
                    HttpStatusCode  = (int)response.StatusCode
                };

            var result = JsonConvert.DeserializeObject<PlivoCXTriggerResponse>(body)
                         ?? new PlivoCXTriggerResponse { Message = body };
            result.RawResponseBody = body;
            result.HttpStatusCode  = (int)response.StatusCode;
            return result;
        }
    }
}
