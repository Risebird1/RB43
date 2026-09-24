using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        /// Step 2: GET /Transcription/{recording_id}/ for the transcript text. If no
        ///         transcription resource exists yet (404), request one asynchronously
        ///         via POST with our webhook as the callback so Plivo posts the finished
        ///         transcript back to us later (its payload carries the call UUID).
        ///         A transcription that already exists but is still processing is left
        ///         alone to avoid re-transcribe charges. Never throws.
        /// </summary>
        public async Task<PhoneScreenMediaResult> FetchCallMediaAsync(string callUuid, string transcriptionCallbackUrl = null)
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
                    var recJson = JsonConvert.DeserializeObject<JObject>(recBody);
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
                    var txUrl = $"https://api.plivo.com/v1/Account/{_voiceAuthId}/Transcription/{recordingId}/";
                    var txBody = await SendVoiceApiAsync(HttpMethod.Get, txUrl);

                    if (!string.IsNullOrWhiteSpace(txBody))
                    {
                        // A transcription resource exists — extract the text if ready.
                        var txJson = JsonConvert.DeserializeObject<JObject>(txBody);
                        var txTok = txJson?["transcription"];
                        string text = null;
                        if (txTok != null && txTok.Type != JTokenType.Null)
                            text = txTok.Type == JTokenType.String
                                ? txTok.ToString()
                                : txTok.ToString(Formatting.None);

                        if (!string.IsNullOrWhiteSpace(text) &&
                            text.Trim() != "{}" && text.Trim() != "[]")
                            result.Transcript = text;
                        // else: it exists but is still processing — leave it, do not recreate.
                    }
                    else
                    {
                        // No transcription yet (404). Kick one off; when it's ready Plivo
                        // posts it to our webhook, which stores it (resolved by call UUID).
                        var createUrl = $"https://api.plivo.com/v1/Account/{_voiceAuthId}/Transcription/{recordingId}/";
                        var createBody = string.IsNullOrWhiteSpace(transcriptionCallbackUrl)
                            ? "{}"
                            : JsonConvert.SerializeObject(new { transcription_callback_url = transcriptionCallbackUrl });
                        _ = await SendVoiceApiAsync(HttpMethod.Post, createUrl, createBody);
                    }
                }
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Recording-only: GET /Recording/?call_uuid=... and return the recording_url (or null).
        /// No Transcription API call, so no transcription charges. Never throws.
        /// </summary>
        public async Task<string> GetRecordingUrlAsync(string callUuid)
            => (await GetRecordingInfoAsync(callUuid)).RecordingUrl;

        /// <summary>
        /// Recording-only: recording URL + id + duration (seconds) for a call UUID. Same MP3-first
        /// choice as <see cref="GetRecordingUrlAsync"/>; no Transcription API call. Never throws.
        /// </summary>
        public async Task<PhoneScreenMediaResult> GetRecordingInfoAsync(string callUuid)
        {
            var result = new PhoneScreenMediaResult();
            if (string.IsNullOrWhiteSpace(callUuid) || string.IsNullOrWhiteSpace(_voiceAuthId))
                return result;

            try
            {
                // Plivo stores each call as both MP3 and WAV; prefer the MP3 (smaller, plays in every browser).
                var recUrl = $"https://api.plivo.com/v1/Account/{_voiceAuthId}/Recording/" +
                             $"?call_uuid={Uri.EscapeDataString(callUuid.Trim())}&limit=5";
                var recBody = await SendVoiceApiAsync(HttpMethod.Get, recUrl);
                if (string.IsNullOrWhiteSpace(recBody))
                    return result;

                var recordings = JsonConvert.DeserializeObject<JObject>(recBody)?["objects"]?.ToList();
                if (recordings == null || recordings.Count == 0)
                    return result;

                var chosen = recordings.FirstOrDefault(r =>
                                 string.Equals(r["recording_format"]?.ToString(), "mp3", StringComparison.OrdinalIgnoreCase))
                             ?? recordings.First();
                var url = chosen["recording_url"]?.ToString();
                result.RecordingUrl = string.IsNullOrWhiteSpace(url) ? null : url;
                result.RecordingId  = chosen["recording_id"]?.ToString();
                if (double.TryParse(chosen["recording_duration_ms"]?.ToString(),
                        System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var ms) && ms > 0)
                    result.DurationSeconds = (int)Math.Round(ms / 1000.0);
            }
            catch (Exception ex)
            {
                result.RecordingUrl = null;
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
