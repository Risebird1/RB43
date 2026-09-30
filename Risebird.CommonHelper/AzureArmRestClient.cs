using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Risebird.CommonHelper.Interface;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public sealed class ArmResponse
    {
        public HttpStatusCode StatusCode { get; set; }
        public JObject Body { get; set; }
        /// <summary>Location header of a 201/202 long-running operation.</summary>
        public string Location { get; set; }
        public string AsyncOperation { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }

        public bool IsSuccess => (int)StatusCode >= 200 && (int)StatusCode < 300;
        public bool IsNotFound => StatusCode == HttpStatusCode.NotFound;
        public string ProvisioningState => Body?["properties"]?["provisioningState"]?.ToString();
        public string Error => ErrorCode == null ? $"HTTP {(int)StatusCode}" : $"{ErrorCode}: {ErrorMessage}";
    }

    /// <summary>
    /// Azure Resource Manager over REST, the way the existing VM functions (StartStopVM, GetRDPFile,
    /// RunPowerShellScript) already talk to Azure: subscription-scoped relative URLs and the
    /// service-principal token from IAzureHelper. Unlike those, it is async and parses ARM error bodies.
    /// </summary>
    public class AzureArmRestClient
    {
        private readonly HttpClient _http;
        private readonly IAzureHelper _azureHelper;
        private readonly string _endpoint;

        public AzureArmRestClient(HttpClient http, IAzureHelper azureHelper, IConfiguration configuration)
        {
            _http = http;
            _azureHelper = azureHelper;
            _endpoint = configuration["AzureSettings:endPoint"] ?? "https://management.azure.com/subscriptions/{0}";
            _http.Timeout = TimeSpan.FromSeconds(100);
        }

        /// <summary>Relative paths resolve under the subscription; absolute URLs (LRO Location headers) are used as-is.</summary>
        public Task<ArmResponse> GetAsync(string path, CancellationToken ct = default) => SendAsync(HttpMethod.Get, path, null, ct);
        public Task<ArmResponse> PutAsync(string path, object body, CancellationToken ct = default) => SendAsync(HttpMethod.Put, path, body, ct);
        public Task<ArmResponse> PostAsync(string path, object body, CancellationToken ct = default) => SendAsync(HttpMethod.Post, path, body, ct);
        public Task<ArmResponse> DeleteAsync(string path, CancellationToken ct = default) => SendAsync(HttpMethod.Delete, path, null, ct);

        public string SubscriptionId => _azureHelper.GetAzureCredential().SubscriptionId;

        private async Task<ArmResponse> SendAsync(HttpMethod method, string path, object body, CancellationToken ct)
        {
            var credentials = _azureHelper.GetAzureCredential();
            if (string.IsNullOrEmpty(credentials?.Token))
                return new ArmResponse { StatusCode = HttpStatusCode.Unauthorized, ErrorCode = "NoToken", ErrorMessage = "Could not obtain an Azure token for the service principal." };

            var url = path.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? path
                : string.Format(_endpoint, credentials.SubscriptionId).TrimEnd('/') + "/" + path.TrimStart('/');

            using (var msg = new HttpRequestMessage(method, url))
            {
                msg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", credentials.Token);
                if (body != null)
                    msg.Content = new StringContent(JsonConvert.SerializeObject(body, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
                        Encoding.UTF8, "application/json");

                using (var resp = await _http.SendAsync(msg, ct))
                {
                    var text = resp.Content == null ? null : await resp.Content.ReadAsStringAsync();
                    var result = new ArmResponse
                    {
                        StatusCode = resp.StatusCode,
                        Location = resp.Headers.Location?.ToString(),
                        AsyncOperation = resp.Headers.TryGetValues("Azure-AsyncOperation", out var ao) ? ao.FirstOrDefault() : null
                    };
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        try { result.Body = JObject.Parse(text); }
                        catch (JsonException) { result.ErrorMessage = text.Length > 500 ? text.Substring(0, 500) : text; }
                    }
                    if (!result.IsSuccess)
                    {
                        result.ErrorCode ??= result.Body?["error"]?["code"]?.ToString() ?? ((int)resp.StatusCode).ToString();
                        result.ErrorMessage = result.Body?["error"]?["message"]?.ToString() ?? result.ErrorMessage;
                    }
                    return result;
                }
            }
        }
    }
}
