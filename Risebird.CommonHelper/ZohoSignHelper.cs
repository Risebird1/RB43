using Microsoft.Extensions.Options;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// Zoho Sign API (https://www.zoho.com/sign/api/). The agreement PDF is uploaded for every
    /// interviewer and the fields are placed from the document's layout file, so the agreement
    /// itself is owned by Risebird (local folder / Azure Blob), not by a Zoho Sign template.
    /// </summary>
    public class ZohoSignHelper : IZohoSignHelper
    {
        private const string InterviewerSigner = "Interviewer";
        private const string CompanySigner = "Company";

        private readonly HttpClient _httpClient;
        private readonly ZohoSignOptions _options;
        private readonly IInterviewSchedulerService _interviewSchedulerService;

        public ZohoSignHelper(HttpClient httpClient, IOptions<ZohoSignOptions> options, IInterviewSchedulerService interviewSchedulerService)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _interviewSchedulerService = interviewSchedulerService;
        }

        public async Task<ZohoSignRequestResult> SendInterviewerAgreementAsync(ZohoSignAgreementRequest request, AgreementDocument document)
        {
            var hasCompanySigner = !string.IsNullOrWhiteSpace(_options.CompanySignerEmail);

            // 1. Create the request with the prepared PDF and the recipients (interviewer signs first).
            var actions = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    ["action_type"] = "SIGN",
                    ["recipient_name"] = request.RecipientName,
                    ["recipient_email"] = request.RecipientEmail,
                    ["signing_order"] = 1,
                    ["verify_recipient"] = false,
                    ["is_embedded"] = _options.UseEmbeddedSigning
                }
            };
            if (hasCompanySigner)
            {
                actions.Add(new Dictionary<string, object>
                {
                    ["action_type"] = "SIGN",
                    ["recipient_name"] = _options.CompanySignerName,
                    ["recipient_email"] = _options.CompanySignerEmail,
                    ["signing_order"] = 2,
                    ["verify_recipient"] = false
                });
            }

            var requestPayload = new Dictionary<string, object>
            {
                ["request_name"] = $"{_options.RequestName} - {request.RecipientName}",
                ["is_sequential"] = true,
                ["expiration_days"] = _options.ExpirationDays,
                ["email_reminders"] = true,
                ["reminder_period"] = 5,
                ["actions"] = actions
            };
            if (_options.UseEmbeddedSigning && !string.IsNullOrWhiteSpace(request.ReturnUrl))
            {
                requestPayload["redirect_pages"] = new Dictionary<string, string>
                {
                    ["sign_success"] = request.ReturnUrl,
                    ["sign_completed"] = request.ReturnUrl,
                    ["sign_declined"] = request.ReturnUrl,
                    ["sign_later"] = request.ReturnUrl
                };
            }

            var createBody = new MultipartFormDataContent();
            var file = new ByteArrayContent(document.Pdf);
            file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            createBody.Add(file, "file", document.FileName);
            createBody.Add(new StringContent(JsonSerializer.Serialize(new Dictionary<string, object> { ["requests"] = requestPayload })), "data");

            string requestId, documentId;
            var actionIds = new Dictionary<string, string>();
            using (var created = await SendAsync(HttpMethod.Post, "requests", createBody))
            {
                var requests = created.RootElement.GetProperty("requests");
                requestId = GetString(requests, "request_id");
                documentId = requests.TryGetProperty("document_ids", out var docs) && docs.ValueKind == JsonValueKind.Array && docs.GetArrayLength() > 0
                    ? GetString(docs[0], "document_id")
                    : null;
                if (requests.TryGetProperty("actions", out var createdActions) && createdActions.ValueKind == JsonValueKind.Array)
                {
                    foreach (var a in createdActions.EnumerateArray())
                    {
                        var signer = string.Equals(GetString(a, "recipient_email"), request.RecipientEmail, StringComparison.OrdinalIgnoreCase)
                            ? InterviewerSigner
                            : CompanySigner;
                        actionIds.TryAdd(signer, GetString(a, "action_id"));
                    }
                }
            }
            if (string.IsNullOrEmpty(requestId) || string.IsNullOrEmpty(documentId) || !actionIds.ContainsKey(InterviewerSigner))
                throw new InvalidOperationException("Zoho Sign did not return a request id / document id / action id for the agreement.");

            // 2. Place the fields from the layout and send the request out for signing.
            var submitActions = new List<Dictionary<string, object>>
            {
                BuildSubmitAction(actionIds[InterviewerSigner], request.RecipientName, request.RecipientEmail, InterviewerSigner, documentId, document)
            };
            if (hasCompanySigner && actionIds.TryGetValue(CompanySigner, out var companyActionId))
                submitActions.Add(BuildSubmitAction(companyActionId, _options.CompanySignerName, _options.CompanySignerEmail, CompanySigner, documentId, document));

            var submitBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("data", JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["requests"] = new Dictionary<string, object> { ["actions"] = submitActions }
                }))
            });
            using var submitted = await SendAsync(HttpMethod.Post, $"requests/{Uri.EscapeDataString(requestId)}/submit", submitBody);

            return new ZohoSignRequestResult
            {
                RequestId = requestId,
                ActionId = actionIds[InterviewerSigner],
                RequestStatus = GetString(submitted.RootElement.GetProperty("requests"), "request_status")
            };
        }

        private static Dictionary<string, object> BuildSubmitAction(string actionId, string name, string email, string signer, string documentId, AgreementDocument document)
        {
            var fields = document.Layout.Fields
                .Where(f => string.Equals(f.Signer, signer, StringComparison.OrdinalIgnoreCase))
                .Select(f => BuildField(f, documentId, document))
                .ToList();
            if (!fields.Any(f => (string)f["field_type_name"] == "Signature"))
                throw new InvalidOperationException($"Agreement layout has no Signature field for signer '{signer}'.");

            return new Dictionary<string, object>
            {
                ["action_id"] = actionId,
                ["action_type"] = "SIGN",
                ["recipient_name"] = name,
                ["recipient_email"] = email,
                ["fields"] = fields
            };
        }

        // Layout positions are points from the top-left; Zoho Sign takes them as a percentage of the page.
        private static Dictionary<string, object> BuildField(AgreementLayoutField field, string documentId, AgreementDocument document)
        {
            var (pageWidth, pageHeight) = document.PageSizes[field.Page - 1];
            string Percent(float value, float total) => Math.Round(value / total * 100, 4).ToString(CultureInfo.InvariantCulture);

            var (typeName, category) = field.Type?.ToLowerInvariant() switch
            {
                "signature" => ("Signature", "image"),
                "date" => ("Date", "datefield"),
                "name" => ("Name", "textfield"),
                "textfield" => ("Textfield", "textfield"),
                _ => throw new InvalidOperationException($"Unsupported agreement field type '{field.Type}'.")
            };

            var result = new Dictionary<string, object>
            {
                ["document_id"] = documentId,
                ["field_type_name"] = typeName,
                ["field_category"] = category,
                ["field_name"] = field.Label,
                ["field_label"] = field.Label,
                ["is_mandatory"] = field.Mandatory,
                ["page_no"] = field.Page - 1,
                ["x_value"] = Percent(field.X, pageWidth),
                ["y_value"] = Percent(field.Y, pageHeight),
                ["width"] = Percent(field.Width, pageWidth),
                ["height"] = Percent(field.Height, pageHeight)
            };
            if (category == "textfield")
                result["text_property"] = new Dictionary<string, object> { ["font_size"] = field.FontSize };
            if (category == "datefield")
                result["date_format"] = "dd MMM yyyy";
            return result;
        }

        public async Task<string> GetEmbeddedSignUrlAsync(string requestId, string actionId, string host)
        {
            var path = $"requests/{Uri.EscapeDataString(requestId)}/actions/{Uri.EscapeDataString(actionId)}/embedtoken?host={Uri.EscapeDataString(host)}";
            using var doc = await SendAsync(HttpMethod.Post, path, null);
            return GetString(doc.RootElement, "sign_url");
        }

        public async Task SendReminderAsync(string requestId)
        {
            using var doc = await SendAsync(HttpMethod.Post, $"requests/{Uri.EscapeDataString(requestId)}/remind", null);
        }

        public async Task<ZohoSignRequestDetails> GetRequestDetailsAsync(string requestId, string actionId)
        {
            using var doc = await SendAsync(HttpMethod.Get, $"requests/{Uri.EscapeDataString(requestId)}", null);
            var requests = doc.RootElement.GetProperty("requests");
            var details = new ZohoSignRequestDetails
            {
                RequestId = GetString(requests, "request_id"),
                RequestStatus = GetString(requests, "request_status"),
                DeclineReason = GetString(requests, "decline_reason")
            };

            if (requests.TryGetProperty("actions", out var actions) && actions.ValueKind == JsonValueKind.Array)
            {
                var list = actions.EnumerateArray().ToList();
                // The interviewer's action; if the id is unknown, the first signer (signing order 1).
                var action = list.FirstOrDefault(a => GetString(a, "action_id") == actionId);
                if (action.ValueKind != JsonValueKind.Object)
                    action = list.FirstOrDefault(a => GetString(a, "signing_order") == "1");
                if (action.ValueKind == JsonValueKind.Object)
                {
                    details.ActionStatus = GetString(action, "action_status");
                    details.SignedOn = GetEpochMillis(action, "signed_time") ?? GetEpochMillis(action, "action_time");
                    if (string.IsNullOrEmpty(details.DeclineReason))
                        details.DeclineReason = GetString(action, "decline_reason");
                }
            }
            return details;
        }

        public async Task<byte[]> DownloadSignedDocumentAsync(string requestId)
        {
            var token = await GetAccessTokenAsync();
            using var req = new HttpRequestMessage(HttpMethod.Get, BuildUrl($"requests/{Uri.EscapeDataString(requestId)}/pdf"));
            req.Headers.Authorization = new AuthenticationHeaderValue("Zoho-oauthtoken", token);
            using var resp = await _httpClient.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                var error = await resp.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Zoho Sign PDF download failed: {(int)resp.StatusCode} {error}");
            }
            return await resp.Content.ReadAsByteArrayAsync();
        }

        public async Task<IReadOnlyDictionary<string, string>> GetFieldValuesAsync(string requestId)
        {
            using var doc = await SendAsync(HttpMethod.Get, $"requests/{Uri.EscapeDataString(requestId)}/fielddata", null);
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            CollectFieldValues(doc.RootElement, values);
            return values;
        }

        // The field data response nests fields under documents / actions; collect every
        // { field_label | field_name, field_value } pair wherever it appears.
        private static void CollectFieldValues(JsonElement element, Dictionary<string, string> values)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                var label = GetString(element, "field_label") ?? GetString(element, "field_name");
                var value = GetString(element, "field_value");
                if (!string.IsNullOrEmpty(label) && value != null && !values.ContainsKey(label))
                    values[label] = value;
                foreach (var property in element.EnumerateObject())
                    CollectFieldValues(property.Value, values);
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                    CollectFieldValues(item, values);
            }
        }

        /// <summary>
        /// Zoho Sign signs webhook payloads with HMAC-SHA256 of the raw body using the
        /// webhook's secret key, sent in the X-ZS-WEBHOOK-SIGNATURE header (base64).
        /// </summary>
        public bool IsWebhookSignatureValid(string payload, string signature)
        {
            if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
                return true;   // validation switched off by configuration
            if (string.IsNullOrWhiteSpace(signature) || payload == null)
                return false;

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.WebhookSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var incoming = signature.Trim();
            return FixedTimeEquals(Convert.ToBase64String(hash), incoming)
                || FixedTimeEquals(Convert.ToHexString(hash).ToLowerInvariant(), incoming.ToLowerInvariant());
        }

        private static bool FixedTimeEquals(string expected, string actual) =>
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(actual));

        private async Task<JsonDocument> SendAsync(HttpMethod method, string path, HttpContent content)
        {
            var token = await GetAccessTokenAsync();
            using var req = new HttpRequestMessage(method, BuildUrl(path)) { Content = content };
            req.Headers.Authorization = new AuthenticationHeaderValue("Zoho-oauthtoken", token);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            using var resp = await _httpClient.SendAsync(req);
            var json = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode || string.IsNullOrEmpty(json))
                throw new InvalidOperationException($"Zoho Sign {method} {path} failed: {(int)resp.StatusCode} {json}");

            var doc = JsonDocument.Parse(json);
            // Zoho Sign reports errors as { code: <non-zero>, message, status: "failure" }.
            if (doc.RootElement.TryGetProperty("code", out var code) && code.ValueKind == JsonValueKind.Number && code.GetInt32() != 0)
            {
                var message = $"Zoho Sign {method} {path} failed: code {code.GetInt32()} {GetString(doc.RootElement, "message")}";
                doc.Dispose();
                throw new InvalidOperationException(message);
            }
            return doc;
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_options.ClientId) && !string.IsNullOrWhiteSpace(_options.ClientSecret) && !string.IsNullOrWhiteSpace(_options.RefreshToken);

        private string BuildUrl(string path) => $"{_options.ApiBaseUrl.TrimEnd('/')}/{path}";

        private async Task<string> GetAccessTokenAsync()
        {
            var zohoDetails = await _interviewSchedulerService.FetchZohoAccessToken(ZooTokenTypes.Sign) ?? new ZohoAccessToken();
            if (!string.IsNullOrEmpty(zohoDetails.AccessToken) && zohoDetails.TokenExpiry > DateTime.Now.AddMinutes(5))
                return zohoDetails.AccessToken;

            if (string.IsNullOrWhiteSpace(_options.RefreshToken))
                throw new InvalidOperationException("ZohoSign:RefreshToken is not configured.");

            var body = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("refresh_token", _options.RefreshToken),
                new KeyValuePair<string, string>("client_id", _options.ClientId),
                new KeyValuePair<string, string>("client_secret", _options.ClientSecret),
                new KeyValuePair<string, string>("grant_type", "refresh_token")
            });
            using var resp = await _httpClient.PostAsync($"{_options.AccountsBaseUrl.TrimEnd('/')}/oauth/v2/token", body);
            var json = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode || string.IsNullOrEmpty(json))
                throw new InvalidOperationException($"Failed to acquire Zoho Sign access token: {(int)resp.StatusCode}");

            using var tokenDoc = JsonDocument.Parse(json);
            var accessToken = GetString(tokenDoc.RootElement, "access_token");
            if (string.IsNullOrEmpty(accessToken))
                throw new InvalidOperationException($"Failed to acquire Zoho Sign access token: {GetString(tokenDoc.RootElement, "error")}");

            // Zoho may return expires_in_sec (seconds) or expires_in (seconds or milliseconds)
            int expiresInSeconds = 3600;
            if (tokenDoc.RootElement.TryGetProperty("expires_in_sec", out var expSec) && expSec.ValueKind == JsonValueKind.Number)
                expiresInSeconds = expSec.GetInt32();
            else if (tokenDoc.RootElement.TryGetProperty("expires_in", out var exp) && exp.ValueKind == JsonValueKind.Number)
                expiresInSeconds = exp.GetInt64() > 10000 ? (int)(exp.GetInt64() / 1000) : (int)exp.GetInt64();

            zohoDetails.AccessToken = accessToken;
            zohoDetails.TokenExpiry = DateTime.Now.AddSeconds(expiresInSeconds);
            zohoDetails.TokenType = ZooTokenTypes.Sign;
            await _interviewSchedulerService.SaveZohoAccessToken(zohoDetails);
            return accessToken;
        }

        private static string GetString(JsonElement element, string name)
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
                return null;
            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.GetRawText(),
                _ => null
            };
        }

        // Zoho Sign timestamps are epoch milliseconds; stored as IST like the rest of the portal.
        private static DateTime? GetEpochMillis(JsonElement element, string name)
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number)
                return null;
            return DateTimeOffset.FromUnixTimeMilliseconds(value.GetInt64()).UtcDateTime.AddMinutes(330);
        }
    }
}
