using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Globalization;
using Microsoft.Extensions.Options;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using Risebird.Entities.ViewModel;
using RestSharp;
using System.Linq;
using Risebird.Business.Interface;


namespace Risebird.CommonHelper
{
    public class ZohoMeetingHelper : IZohoMeetingHelper
    {
        private readonly ZohoOptions _options;
        private readonly IInterviewSchedulerService _interviewSchedulerservice;
        private string? _cachedAccessToken;
        private DateTime _accessTokenExpiry;

        public ZohoMeetingHelper(IOptions<ZohoOptions> options, IInterviewSchedulerService interviewSchedulerservice)
        {
            _options = options.Value;
            _interviewSchedulerservice = interviewSchedulerservice;
        }
        private async Task<string> GetAccessTokenAsync()
        {
            var zohodetails = await _interviewSchedulerservice.FetchZohoAccessToken(ZooTokenTypes.Meeting);
            if (zohodetails == null)
            {
                zohodetails = new ZohoAccessToken();
            }
            _cachedAccessToken = zohodetails.AccessToken;
            _accessTokenExpiry = zohodetails.TokenExpiry;
            if (!string.IsNullOrWhiteSpace(_cachedAccessToken) && _accessTokenExpiry > DateTime.Now.AddMinutes(1))
            {
                return _cachedAccessToken!;
            }

            if (string.IsNullOrWhiteSpace(_options.RefreshToken))
            {
                throw new InvalidOperationException("Zoho refresh token is not available. Connect Zoho to acquire a refresh token.");
            }

            var client = new RestClient(new RestClientOptions(_options.AccountsBaseUrl.TrimEnd('/')));
            var req = new RestRequest("/oauth/v2/token", Method.Post);
            req.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            req.AddParameter("refresh_token", _options.RefreshToken);
            req.AddParameter("client_id", _options.ClientId);
            req.AddParameter("client_secret", _options.ClientSecret);
            req.AddParameter("grant_type", "refresh_token");
            var resp = await client.ExecuteAsync(req);
            if (!resp.IsSuccessful || string.IsNullOrEmpty(resp.Content))
            {
                throw new InvalidOperationException($"Failed to acquire access token: {resp.StatusCode} {resp.ErrorMessage}");
            }
            using var tokenDoc = JsonDocument.Parse(resp.Content);
            var accessToken = tokenDoc.RootElement.GetProperty("access_token").GetString();
            // Zoho may return expires_in_sec (seconds) or expires_in (milliseconds)
            int expiresInSeconds = 3600;
            if (tokenDoc.RootElement.TryGetProperty("expires_in_sec", out var expSecEl) && expSecEl.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                expiresInSeconds = expSecEl.GetInt32();
            }
            else if (tokenDoc.RootElement.TryGetProperty("expires_in", out var expEl) && expEl.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                var exp = expEl.GetInt64();
                // Heuristic: if greater than 10000, treat as ms, else seconds
                expiresInSeconds = exp > 10000 ? (int)(exp / 1000) : (int)exp;
            }
            _cachedAccessToken = accessToken;
            _accessTokenExpiry = DateTime.Now.AddSeconds(expiresInSeconds);
            zohodetails.AccessToken = _cachedAccessToken;
            zohodetails.TokenExpiry = _accessTokenExpiry;
            zohodetails.TokenType = ZooTokenTypes.Meeting;
            await _interviewSchedulerservice.SaveZohoAccessToken(zohodetails);
            return accessToken!;
        }
        public async Task<string> GetRecordingAccessTokenAsync()
        {
            var zohodetails = await _interviewSchedulerservice.FetchZohoAccessToken(ZooTokenTypes.Recording);
            if (zohodetails == null)
            {
                zohodetails = new ZohoAccessToken();
            }
            _cachedAccessToken = zohodetails.AccessToken;
            _accessTokenExpiry = zohodetails.TokenExpiry;
            if (!string.IsNullOrWhiteSpace(_cachedAccessToken) && _accessTokenExpiry > DateTime.Now.AddMinutes(1))
            {
                return _cachedAccessToken!;
            }

            if (string.IsNullOrWhiteSpace(_options.RecordingRefreshToken))
            {
                throw new InvalidOperationException("Zoho refresh token is not available. Connect Zoho to acquire a refresh token.");
            }

            var client = new RestClient(new RestClientOptions(_options.AccountsBaseUrl.TrimEnd('/')));
            var req = new RestRequest("/oauth/v2/token", Method.Post);
            req.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            req.AddParameter("refresh_token", _options.RecordingRefreshToken);
            req.AddParameter("client_id", _options.ClientId);
            req.AddParameter("client_secret", _options.ClientSecret);
            req.AddParameter("grant_type", "refresh_token");
            var resp = await client.ExecuteAsync(req);
            if (!resp.IsSuccessful || string.IsNullOrEmpty(resp.Content))
            {
                throw new InvalidOperationException($"Failed to acquire access token: {resp.StatusCode} {resp.ErrorMessage}");
            }
            using var tokenDoc = JsonDocument.Parse(resp.Content);
            var accessToken = tokenDoc.RootElement.GetProperty("access_token").GetString();
            // Zoho may return expires_in_sec (seconds) or expires_in (milliseconds)
            int expiresInSeconds = 3600;
            if (tokenDoc.RootElement.TryGetProperty("expires_in_sec", out var expSecEl) && expSecEl.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                expiresInSeconds = expSecEl.GetInt32();
            }
            else if (tokenDoc.RootElement.TryGetProperty("expires_in", out var expEl) && expEl.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                var exp = expEl.GetInt64();
                // Heuristic: if greater than 10000, treat as ms, else seconds
                expiresInSeconds = exp > 10000 ? (int)(exp / 1000) : (int)exp;
            }
            _cachedAccessToken = accessToken;
            _accessTokenExpiry = DateTime.Now.AddSeconds(expiresInSeconds);
            zohodetails.AccessToken = _cachedAccessToken;
            zohodetails.TokenExpiry = _accessTokenExpiry;
            zohodetails.TokenType = ZooTokenTypes.Recording;
            await _interviewSchedulerservice.SaveZohoAccessToken(zohodetails);
            return accessToken!;
        }
        private RestClient CreateMeetingApiClient() => new RestClient(new RestClientOptions(_options.MeetingApiBaseUrl.TrimEnd('/')));

        public async Task<ZohoMeetingResponse> CreateMeetingAsync(ZohoCreateMeetingRequest request)
        {
            // Per Zoho docs, schedule via sessions.json with wrapper { session: { ... } }
            var friendlyStart = request.StartTime.ToString("MMM dd, yyyy hh:mm tt", CultureInfo.InvariantCulture);
            var userid = await _interviewSchedulerservice.GetAvailableZohoAccount(request.StartTime.AddMinutes(-30), request.StartTime.AddMinutes(30));
            if (string.IsNullOrWhiteSpace(userid))
            {
                userid = "60005813892";
            }
            var payload = new
            {
                session = new
                {
                    topic = request.Topic,
                    agenda = request.Agenda,
                    presenter = userid,
                    startTime = friendlyStart,
                    duration = request.DurationMinutes * 60000,
                    timezone = "Asia/Calcutta"
                }
            };
            var token = await GetAccessTokenAsync();
            var client = CreateMeetingApiClient();
            var path = $"/{_options.OrganizationId}/sessions.json";
            var req = new RestRequest(path, Method.Post);
            req.AddHeader("Authorization", $"Zoho-oauthtoken {token}");
            req.AddHeader("Accept", "application/json");
            req.AddJsonBody(payload);
            var resp = await client.ExecuteAsync(req);
            if (!resp.IsSuccessful || string.IsNullOrEmpty(resp.Content))
            {
                throw new InvalidOperationException($"Failed to create meeting: {resp.StatusCode} {resp.ErrorMessage}");
            }
            using var doc = JsonDocument.Parse(resp.Content);
            var root = doc.RootElement;
            var session = root.TryGetProperty("session", out var sObj) ? sObj : root;
            var meetingKey = session.TryGetProperty("meetingKey", out var mk) ? (mk.GetRawText().Trim('"')) : string.Empty;
            var startLink = session.TryGetProperty("startLink", out var sl) ? sl.GetString() ?? string.Empty : string.Empty;
            var joinLink = session.TryGetProperty("joinLink", out var jl) ? jl.GetString() ?? string.Empty : string.Empty;
            return new ZohoMeetingResponse { meetingKey = meetingKey, startLink = startLink, joinLink = joinLink, accountEmail = userid };
        }

        public async Task<bool> DeleteMeetingAsync(string meetingKey)
        {
            var token = await GetAccessTokenAsync();
            var client = CreateMeetingApiClient();
            var path = $"/{_options.OrganizationId}/sessions/{meetingKey}.json";
            var req = new RestRequest(path, Method.Delete);
            req.AddHeader("Authorization", $"Zoho-oauthtoken {token}");
            req.AddHeader("Accept", "application/json");
            var resp = await client.ExecuteAsync(req);
            if (!resp.IsSuccessful)
            {
                return false;
            }
            return true;
        }

        public async Task<IEnumerable<ZohoRecordingInfo>> GetRecordingAsync(string meetingKey)
        {
            var token = await GetRecordingAccessTokenAsync();
            var client = CreateMeetingApiClient();
            var path = $"/{_options.OrganizationId}/recordings/{meetingKey}.json";
            var req = new RestRequest(path, Method.Get);
            req.AddHeader("Authorization", $"Zoho-oauthtoken {token}");
            req.AddHeader("Accept", "application/json");
            var resp = await client.ExecuteAsync(req);
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            if (!resp.IsSuccessful || string.IsNullOrEmpty(resp.Content))
            {
                throw new InvalidOperationException($"Failed to get recordings: {resp.StatusCode} {resp.ErrorMessage}");
            }
            using var doc = JsonDocument.Parse(resp.Content);
            var root = doc.RootElement;
            JsonElement recordings = root.GetProperty("recordings");
            var _recordings = new List<ZohoRecordingInfo>();
            foreach (JsonElement recording in recordings.EnumerateArray())
            {
                _recordings.Add(new ZohoRecordingInfo
                {
                    isTranscriptGenerated = recording.TryGetProperty("isTranscriptGenerated", out var t) && t.GetBoolean(),
                    downloadUrl = recording.TryGetProperty("downloadUrl", out var d) ? d.GetString() : null,
                    duration = recording.TryGetProperty("duration", out var de) ? de.GetInt32() : 0,
                    transcriptionDownloadUrl = recording.TryGetProperty("transcriptionDownloadUrl", out var tr) ? tr.GetString() : null,
                    erecordingId = recording.TryGetProperty("erecordingId", out var rid) ? rid.GetString() : null
                });
            }
            return _recordings;
        }
        public async Task DeleteRecordingAsync(string recordingid)
        {
            var token = await GetRecordingAccessTokenAsync();
            var client = CreateMeetingApiClient();
            var path = $"/{_options.OrganizationId}/recordings/{recordingid}.json";
            var req = new RestRequest(path, Method.Delete);
            req.AddHeader("Authorization", $"Zoho-oauthtoken {token}");
            req.AddHeader("Accept", "application/json");
            var resp = await client.ExecuteAsync(req);
            if (!resp.IsSuccessful)
            {
                throw new InvalidOperationException($"Failed to delete recording: {resp.StatusCode} {resp.ErrorMessage}");
            }
        }
        public async Task<IReadOnlyList<ZohoParticipantReportItem>> GetParticipantReportAsync(string meetingKey)
        {
            var token = await GetAccessTokenAsync();
            var client = CreateMeetingApiClient();
            // Endpoint may vary by Zoho API version; this targets participant report for a meeting/session
            var path = $"/{_options.OrganizationId}/participant/{meetingKey}.json?index=1&count=10";
            var req = new RestRequest(path, Method.Get);
            req.AddHeader("Authorization", $"Zoho-oauthtoken {token}");
            req.AddHeader("Accept", "application/json");
            var resp = await client.ExecuteAsync(req);
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Array.Empty<ZohoParticipantReportItem>();
            }
            if (!resp.IsSuccessful || string.IsNullOrEmpty(resp.Content))
            {
                throw new InvalidOperationException($"Failed to get participant report: {resp.StatusCode} {resp.ErrorMessage}");
            }
            using var doc = JsonDocument.Parse(resp.Content);
            var root = doc.RootElement;
            var results = new List<ZohoParticipantReportItem>();
            JsonElement participants;
            if (root.TryGetProperty("participants", out participants) && participants.ValueKind == JsonValueKind.Array)
            {
                foreach (var p in participants.EnumerateArray())
                {
                    results.Add(new ZohoParticipantReportItem
                    {
                        participantId = p.TryGetProperty("memberId", out var pid) ? pid.GetString() : null,
                        email = p.TryGetProperty("email", out var e) ? e.GetString() : null,
                        inAndOutTime = p.TryGetProperty("inAndOutTime", out var ia) ? ia.GetString() : null,
                        totalDurationMinutes = p.TryGetProperty("duration", out var td) ? td.GetInt32() / 60000 : 0
                    });
                }
            }
            return results;
        }
    }
}
