using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// Talks to the MCQ assessment API on screen.risebird.io.
    ///
    /// Auth is the service-account token already shared between the two systems for
    /// AIInterviewStatusWebhookNotifier, so there is one secret to rotate rather than two.
    ///
    /// Every method swallows transport failures and returns null. That is deliberate: this sits
    /// in the path of a candidate's live interview and of a client reading a report, and neither
    /// should break because the MCQ host is slow or restarting. Callers decide what a null means -
    /// the interview skips the section, the report falls back to its cached copy.
    /// </summary>
    public class McqAssessmentService : IMcqAssessmentService
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _logger;
        private readonly HttpClient _http;

        public McqAssessmentService(IConfiguration configuration, IApplicationErrorLogger logger)
        {
            _configuration = configuration;
            _logger = logger;
            _http = new HttpClient();
        }

        private string BaseUrl => _configuration["McqAssessment:BaseUrl"]?.TrimEnd('/');

        private string ServiceAccountToken =>
            _configuration["McqAssessment:ServiceAccountToken"]
            ?? _configuration["AIInterviewWebhook:ServiceAccountToken"];

        public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl);

        public async Task<McqSectionProvisionResult> ProvisionSectionAsync(string sessionId, McqSectionProvisionRequest request)
        {
            if (!IsConfigured || string.IsNullOrWhiteSpace(sessionId) || request == null)
                return null;

            // skillChannelId is what resolves the channel on the MCQ side (MCQIRF.PortalIRFId), and
            // candidateEmail identifies the attempt. Serializing with NullValueHandling.Ignore drops
            // a null field entirely, so a missing value arrives as a generic 422 that reads like an
            // MCQ-side fault. Saying which field is missing, on this side, is far more useful.
            if (string.IsNullOrWhiteSpace(request.SkillChannelId) || string.IsNullOrWhiteSpace(request.CandidateEmail))
            {
                await LogAsync(new Exception(
                    $"Cannot provision the MCQ section for session {sessionId}: " +
                    $"skillChannelId is '{request.SkillChannelId ?? "(null)"}' and candidateEmail is '{request.CandidateEmail ?? "(null)"}'. " +
                    "Both are required. skillChannelId comes from IRF.IRFId on the interview's skill channel."), null);
                return null;
            }

            // Question generation for a whole sectioned paper is several model calls, so the
            // first provision of a session is slow by nature. Anything past this and the
            // candidate is better served by skipping the section than by staring at a spinner.
            var body = await SendAsync(
                HttpMethod.Post,
                $"{BaseUrl}/api/v1/ai-sessions/{Uri.EscapeDataString(sessionId)}/mcq",
                request,
                TimeSpan.FromSeconds(180),
                // A 404 here is a missing route, not an empty result - let it be logged.
                treatNotFoundAsEmpty: false);

            if (body == null) return null;

            try
            {
                return JsonConvert.DeserializeObject<McqSectionProvisionResult>(body);
            }
            catch (Exception ex)
            {
                await LogAsync(ex, $"Could not read the MCQ provision response for session {sessionId}.");
                return null;
            }
        }

        public async Task<McqAssessmentReport> FetchReportAsync(string sessionId)
        {
            if (!IsConfigured || string.IsNullOrWhiteSpace(sessionId))
                return null;

            // 202 is returned while the candidate has started but not submitted, and it carries the
            // partial result. That is exactly what the report should show for someone who abandoned
            // the section - the block renders with a "did not submit" banner over the figures they
            // did produce. Discarding it left the report with no MCQ section at all, which reads as
            // "this channel has no assessment" rather than "the candidate skipped it".
            var body = await SendAsync(
                HttpMethod.Get,
                $"{BaseUrl}/api/v1/ai-sessions/{Uri.EscapeDataString(sessionId)}/mcq/report",
                null,
                TimeSpan.FromSeconds(30),
                acceptAccepted: true);

            if (body == null) return null;

            try
            {
                return JsonConvert.DeserializeObject<McqAssessmentReport>(body);
            }
            catch (Exception ex)
            {
                await LogAsync(ex, $"Could not read the MCQ report response for session {sessionId}.");
                return null;
            }
        }

        public string ChannelOwnerEmail => _configuration["McqAssessment:ChannelOwnerEmail"];

        public async Task<McqChannelLinkResult> EnsureChannelLinkAsync(McqEnsureChannelLinkRequest request)
        {
            if (!IsConfigured || request == null || string.IsNullOrWhiteSpace(request.SkillChannelId))
                return null;

            // Without an owner there is no MCQ client to create the channel under, and the MCQ side
            // would reject it. Treated as "auto-creation is switched off" rather than an error, so
            // an environment that has not configured it simply keeps the manual workflow.
            if (string.IsNullOrWhiteSpace(request.RequestedBy))
            {
                await LogAsync(new Exception(
                    $"Cannot create the MCQ skill channel for '{request.SkillChannelId}': " +
                    "McqAssessment:ChannelOwnerEmail is not configured, so there is no MCQ client to own it."), null);
                return null;
            }

            // Channel creation writes a row on the MCQ side but does no question generation, so it
            // is quick - unlike provisioning, which this must not be confused with.
            var body = await SendAsync(
                HttpMethod.Post,
                $"{BaseUrl}/api/v1/ai-sessions/channel-link",
                request,
                TimeSpan.FromSeconds(45),
                treatNotFoundAsEmpty: false);



            McqChannelLinkResult deSerializedBody;

            try
            {
                deSerializedBody = JsonConvert.DeserializeObject<McqChannelLinkResult>(body);
            }
            catch (Exception ex)
            {
                await LogAsync(ex, $"Could not read the MCQ channel-link response for skill channel {request.SkillChannelId}.");
                return null;
            }
            //call test creation api

            var testBody = await SendAsync(
                HttpMethod.Post,
                $"{BaseUrl}/api/v1/skill-channels/{deSerializedBody.IrfId}/tests",
                new { RequestedBy = request.RequestedBy, DurationMinutes = 20 },
                TimeSpan.FromSeconds(300),
                treatNotFoundAsEmpty: false);

            if (body == null) return null;

            try
            {
                return deSerializedBody;
            }
            catch (Exception ex)
            {
                await LogAsync(ex, $"Could not read the MCQ channel-link response for skill channel {request.SkillChannelId}.");
                return null;
            }
        }

        public async Task<bool?> IsChannelLinkedAsync(string skillChannelId)
        {
            if (!IsConfigured || string.IsNullOrWhiteSpace(skillChannelId))
                return null;

            var body = await SendAsync(
                HttpMethod.Get,
                $"{BaseUrl}/api/v1/ai-sessions/channel-link?skillChannelId={Uri.EscapeDataString(skillChannelId.Trim())}",
                null,
                TimeSpan.FromSeconds(15),
                // A missing route here means the MCQ app has no channel-link endpoint, which is a
                // deployment gap, not an unlinked channel. Let it be logged and reported as
                // "could not check" rather than as a definite answer.
                treatNotFoundAsEmpty: false);

            if (body == null) return null;

            try
            {
                var parsed = JsonConvert.DeserializeObject<McqChannelLinkResult>(body);
                return parsed?.Linked;
            }
            catch (Exception ex)
            {
                await LogAsync(ex, $"Could not read the MCQ channel-link response for skill channel {skillChannelId}.");
                return null;
            }
        }

        /// <summary>
        /// Returns the response body for 2xx, and null for anything else - including 404 (this
        /// session has no MCQ section) and 202 (the candidate has not finished yet), both of
        /// which are ordinary states rather than faults and so are not logged as errors.
        /// </summary>
        private async Task<string> SendAsync(HttpMethod method, string url, object payload, TimeSpan timeout,
            bool treatNotFoundAsEmpty = true, bool acceptAccepted = false)
        {
            using var cts = new CancellationTokenSource(timeout);

            try
            {
                using var request = new HttpRequestMessage(method, url);

                var token = ServiceAccountToken;
                if (!string.IsNullOrWhiteSpace(token))
                    request.Headers.TryAddWithoutValidation("service-account-token", token);

                if (payload != null)
                {
                    request.Content = new StringContent(
                        JsonConvert.SerializeObject(payload,
                            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
                        Encoding.UTF8,
                        "application/json");
                }

                using var response = await _http.SendAsync(request, cts.Token);
                var body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return body;

                // 202 carries a usable in-progress payload. Provisioning wants a finished result and
                // reads it as "nothing yet"; the report wants the partial, so it can show what the
                // candidate did produce rather than omitting the section entirely.
                if (response.StatusCode == HttpStatusCode.Accepted)
                    return acceptAccepted ? body : null;

                // 404 means two very different things depending on the endpoint. On the report it is
                // a legitimate answer - this session has no MCQ section - and logging it would be
                // noise. On provisioning it means the route is not there at all, i.e. the MCQ app
                // has not been deployed with the ai-sessions controller. That has to be loud: it is
                // otherwise a silent null with nothing anywhere to explain it.
                if (response.StatusCode == HttpStatusCode.NotFound && treatNotFoundAsEmpty)
                    return null;

                var hint = response.StatusCode == HttpStatusCode.NotFound
                    ? " The route was not found - check that screen.risebird.io is deployed with the ai-sessions API."
                    : response.StatusCode == HttpStatusCode.Unauthorized
                        ? " Check that McqAssessment:ServiceAccountToken matches RisebirdPortalApi:ServiceAccountToken on the MCQ side."
                        : string.Empty;

                await LogAsync(
                    new Exception($"MCQ assessment API returned {(int)response.StatusCode} for {method} {url}.{hint} Body: {Truncate(body, 1000)}"),
                    null);
                return null;
            }
            catch (OperationCanceledException)
            {
                await LogAsync(new Exception($"MCQ assessment API timed out after {timeout.TotalSeconds:0}s for {method} {url}."), null);
                return null;
            }
            catch (Exception ex)
            {
                await LogAsync(ex, $"MCQ assessment API call failed for {method} {url}.");
                return null;
            }
        }

        private async Task LogAsync(Exception ex, string context)
        {
            try
            {
                if (_logger == null) return;
                await _logger.LogMessage(LogLevelInfo.Exception,
                    string.IsNullOrWhiteSpace(context) ? ex : new Exception(context, ex));
            }
            catch
            {
                // Logging must never be the thing that breaks an interview.
            }
        }

        private static string Truncate(string value, int max) =>
            string.IsNullOrEmpty(value) || value.Length <= max ? value : value.Substring(0, max) + "…";
    }
}
