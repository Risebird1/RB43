using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// Exotel Voice v1 REST client.
    /// Authentication: HTTP Basic Auth with ApiKey:ApiToken.
    /// Base URL: https://{Subdomain}/v1/Accounts/{AccountSid}/
    /// </summary>
    public class ExotelService : IExotelService
    {
        // Candidate / interviewer numbers are often stored as 10-digit Indian numbers without a country code.
        private const string DefaultCountryCode = "91";

        private readonly ExotelSettings _settings;
        private readonly HttpClient _httpClient;

        public ExotelService(IOptions<ExotelSettings> settings, IHttpClientFactory httpClientFactory)
        {
            _settings = settings.Value;
            _httpClient = httpClientFactory.CreateClient();

            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_settings.ApiKey}:{_settings.ApiToken}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }

        private string BaseUrl => $"https://{_settings.Subdomain.Trim()}/v1/Accounts/{_settings.AccountSid.Trim()}";

        /// <inheritdoc/>
        public async Task<ExotelConnectResult> ConnectCallAsync(string from, string to, string statusCallbackUrl)
        {
            if (!_settings.IsConfigured)
                return new ExotelConnectResult { ErrorMessage = "Exotel is not configured" };

            var fromNumber = ToDialNumber(from);
            var toNumber = ToDialNumber(to);
            if (string.IsNullOrEmpty(fromNumber) || string.IsNullOrEmpty(toNumber))
                return new ExotelConnectResult { ErrorMessage = "Invalid phone number" };

            var form = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("From", fromNumber),
                new KeyValuePair<string, string>("To", toNumber),
                new KeyValuePair<string, string>("CallerId", ToExoPhone(_settings.CallerId)),
                new KeyValuePair<string, string>("Record", _settings.Record ? "true" : "false")
            };
            if (!string.IsNullOrWhiteSpace(statusCallbackUrl))
            {
                form.Add(new KeyValuePair<string, string>("StatusCallback", statusCallbackUrl));
                form.Add(new KeyValuePair<string, string>("StatusCallbackEvents[0]", "terminal"));
            }

            var response = await _httpClient.PostAsync($"{BaseUrl}/Calls/connect.json", new FormUrlEncodedContent(form));
            var body = await response.Content.ReadAsStringAsync();
            var json = TryParse(body);

            if (!response.IsSuccessStatusCode)
            {
                // RestException is an array on some endpoints ([{ StatusCode, Message }]) and an object on others.
                var restException = json?["RestException"];
                var message = (restException is JArray list ? list.FirstOrDefault() : restException)?["Message"]?.ToString();
                return new ExotelConnectResult
                {
                    ErrorMessage = $"Exotel call failed ({(int)response.StatusCode}): {(string.IsNullOrEmpty(message) ? body : message)}"
                };
            }

            var call = json?["Call"];
            var sid = call?["Sid"]?.ToString();
            return new ExotelConnectResult
            {
                Success = !string.IsNullOrEmpty(sid),
                Sid = sid,
                Status = call?["Status"]?.ToString(),
                ErrorMessage = string.IsNullOrEmpty(sid) ? "Exotel returned no call Sid" : null
            };
        }

        /// <inheritdoc/>
        public async Task<ExotelCallDetails> GetCallDetailsAsync(string callSid)
        {
            if (!_settings.IsConfigured || string.IsNullOrWhiteSpace(callSid))
                return null;

            var response = await _httpClient.GetAsync($"{BaseUrl}/Calls/{Uri.EscapeDataString(callSid.Trim())}.json");
            if (!response.IsSuccessStatusCode)
                return null;

            var call = TryParse(await response.Content.ReadAsStringAsync())?["Call"];
            if (call == null)
                return null;

            var recordingUrl = call["RecordingUrl"]?.ToString();
            return new ExotelCallDetails
            {
                Sid = call["Sid"]?.ToString(),
                Status = call["Status"]?.ToString(),
                RecordingUrl = string.IsNullOrWhiteSpace(recordingUrl) ? null : recordingUrl.Trim(),
                DurationSeconds = int.TryParse(call["Duration"]?.ToString(), out var d) && d > 0 ? d : (int?)null
            };
        }

        private static JObject TryParse(string body)
        {
            try { return string.IsNullOrWhiteSpace(body) ? null : JObject.Parse(body); }
            catch (Exception) { return null; }
        }

        // ExoPhones are passed as Exotel lists them — national format with the trunk 0, no "+91":
        // "+918064061496" / "918064061496" / "8064061496" → "08064061496". Toll-free (1800…) numbers pass through.
        private static string ToExoPhone(string raw)
        {
            var digits = new string((raw ?? string.Empty).Where(char.IsDigit).ToArray());
            if (digits.Length == 12 && digits.StartsWith(DefaultCountryCode))
                return "0" + digits.Substring(2);
            if (digits.Length == 10)
                return "0" + digits;
            return digits;
        }

        // "+91 98765-43210" / "9876543210" / "09876543210" → "+919876543210".
        // SIP user ids (the in-browser softphone leg) contain letters and are passed through unchanged.
        private static string ToDialNumber(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var value = raw.Trim();
            if (value.Any(char.IsLetter))
                return value;

            var hasPlus = value.StartsWith("+");
            var digits = new string(value.Where(char.IsDigit).ToArray());
            if (digits.Length < 9)
                return null;

            if (!hasPlus)
            {
                if (digits.StartsWith("00"))
                    digits = digits.Substring(2);           // international prefix: 00919876543210
                else if (digits.Length == 11 && digits.StartsWith("0"))
                    digits = digits.Substring(1);           // local trunk prefix: 09876543210
                if (digits.Length == 10)
                    digits = DefaultCountryCode + digits;   // national number without country code
            }
            return "+" + digits;
        }
    }
}
