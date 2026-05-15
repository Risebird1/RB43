using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Business
{
    /// <summary>
    /// Integrates with the Cashfree Payouts v1 API to disburse funds to an interviewer's bank account.
    /// Flow: Authorize → Add/verify beneficiary → Async transfer → (optional) status poll.
    /// </summary>
    public class CashfreePayoutService : ICashfreePayoutService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        // Config section key
        private const string CfSection = "Cashfree";

        public CashfreePayoutService(
            HttpClient httpClient,
            IConfiguration configuration,
            IApplicationErrorLogger applicationErrorLogger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
        }

        // ------------------------------------------------------------------ //
        //  Public API                                                          //
        // ------------------------------------------------------------------ //

        public async Task<CashfreePayoutResult> CreatePayoutToBankAsync(
            string accountName,
            string accountNumber,
            string ifscCode,
            string email,
            string phone,
            decimal amount,
            string transferId,
            string remarks)
        {
            try
            {
                var token = await AuthorizeAsync();
                if (token == null)
                    return Fail("Cashfree authorisation failed. Check ClientId/ClientSecret configuration.");

                // Beneficiary id: alphanumeric + underscore, ≤ 50 chars.
                // Derive a stable id from the transferId so re-tries reuse the same beneId.
                var beneId = SanitizeBeneId(transferId);

                var addResult = await AddBeneficiaryAsync(token, beneId, accountName, accountNumber, ifscCode, email, phone);
                if (!addResult)
                    return Fail("Failed to register beneficiary with Cashfree.");

                return await RequestTransferAsync(token, beneId, amount, transferId, remarks);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return Fail($"Unexpected error initiating Cashfree payout: {e.Message}");
            }
        }

        public async Task<CashfreePayoutResult> GetPayoutStatusAsync(string transferId)
        {
            try
            {
                var token = await AuthorizeAsync();
                if (token == null)
                    return Fail("Cashfree authorisation failed when fetching status.");

                return await FetchTransferStatusAsync(token, transferId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return Fail($"Unexpected error fetching Cashfree payout status: {e.Message}");
            }
        }

        // ------------------------------------------------------------------ //
        //  Private helpers                                                     //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Obtains a short-lived Bearer token from Cashfree (valid ~6 minutes).
        /// POST /payout/v1/authorize  headers: X-Client-Id, X-Client-Secret
        /// </summary>
        private async Task<string> AuthorizeAsync()
        {
            var clientId     = _configuration[$"{CfSection}:ClientId"];
            var clientSecret = _configuration[$"{CfSection}:ClientSecret"];
            var baseUrl      = BaseUrl();

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/payout/v1/authorize");
            request.Headers.Add("X-Client-Id", clientId);
            request.Headers.Add("X-Client-Secret", clientSecret);
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var body     = await response.Content.ReadAsStringAsync();

            var json = JObject.Parse(body);
            var status = json["status"]?.ToString();
            if (status != "SUCCESS")
            {
                await _applicationErrorLogger.LogMessage(
                    LogLevelInfo.Exception,
                    new Exception($"Cashfree authorize failed: {body}"));
                return null;
            }

            return json["data"]?["token"]?.ToString();
        }

        /// <summary>
        /// Adds a bank beneficiary. Returns true if successfully added or already exists (409).
        /// POST /payout/v1/addBeneficiary
        /// </summary>
        private async Task<bool> AddBeneficiaryAsync(
            string token,
            string beneId,
            string name,
            string bankAccount,
            string ifsc,
            string email,
            string phone)
        {
            var baseUrl = BaseUrl();
            var payload = new
            {
                beneId,
                name,
                email,
                phone    = SanitizePhone(phone),
                bankAccount,
                ifsc,
                address1 = "India"
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/payout/v1/addBeneficiary");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            // 409 = "Beneficiary Id already exists" → treat as success
            if ((int)response.StatusCode == 409)
                return true;

            var body = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(body);
            var status = json["status"]?.ToString();
            if (status == "SUCCESS")
                return true;

            await _applicationErrorLogger.LogMessage(
                LogLevelInfo.Exception,
                new Exception($"Cashfree addBeneficiary failed [{response.StatusCode}]: {body}"));
            return false;
        }

        /// <summary>
        /// Initiates an async bank transfer.
        /// POST /payout/v1/requestAsyncTransfer
        /// </summary>
        private async Task<CashfreePayoutResult> RequestTransferAsync(
            string token,
            string beneId,
            decimal amount,
            string transferId,
            string remarks)
        {
            var mode    = _configuration[$"{CfSection}:TransferMode"] ?? "banktransfer";
            var baseUrl = BaseUrl();

            var payload = new
            {
                beneId,
                amount,
                transferId,
                transferMode = mode,
                remarks      = Truncate(remarks, 70)
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/payout/v1/requestAsyncTransfer");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var body     = await response.Content.ReadAsStringAsync();
            var json     = JObject.Parse(body);

            var status  = json["status"]?.ToString();
            var subCode = json["subCode"]?.ToString();
            var message = json["message"]?.ToString();

            if (status == "ACCEPTED" || subCode == "201")
            {
                return new CashfreePayoutResult
                {
                    Success  = true,
                    PayoutId = transferId,
                    Status   = "ACCEPTED",
                    Amount   = amount
                };
            }

            await _applicationErrorLogger.LogMessage(
                LogLevelInfo.Exception,
                new Exception($"Cashfree requestAsyncTransfer failed [{response.StatusCode}]: {body}"));

            return new CashfreePayoutResult
            {
                Success      = false,
                ErrorMessage = message ?? body,
                ErrorCode    = subCode
            };
        }

        /// <summary>
        /// Fetches transfer status.
        /// GET /payout/v1.1/getTransferStatus?transferId=...
        /// </summary>
        private async Task<CashfreePayoutResult> FetchTransferStatusAsync(string token, string transferId)
        {
            var baseUrl = BaseUrl();
            var url     = $"{baseUrl}/payout/v1.1/getTransferStatus?transferId={Uri.EscapeDataString(transferId)}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Content-Type", "application/json");

            var response = await _httpClient.SendAsync(request);
            var body     = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new CashfreePayoutResult
                {
                    Success      = false,
                    PayoutId     = transferId,
                    ErrorMessage = $"Cashfree returned {(int)response.StatusCode}: {body}",
                    ErrorCode    = ((int)response.StatusCode).ToString()
                };
            }

            var json   = JObject.Parse(body);
            var data   = json["data"]?["transfer"];
            var status = data?["status"]?.ToString() ?? json["status"]?.ToString();
            var utr    = data?["utr"]?.ToString();
            decimal.TryParse(data?["amount"]?.ToString(), out var amt);

            return new CashfreePayoutResult
            {
                Success  = true,
                PayoutId = transferId,
                Status   = status,
                Amount   = amt,
                Utr      = utr
            };
        }

        // ------------------------------------------------------------------ //
        //  Utility                                                             //
        // ------------------------------------------------------------------ //

        private string BaseUrl()
        {
            var url = _configuration[$"{CfSection}:BaseUrl"];
            return string.IsNullOrWhiteSpace(url)
                ? "https://payout-gamma.cashfree.com"   // default to sandbox
                : url.TrimEnd('/');
        }

        private static CashfreePayoutResult Fail(string message) =>
            new CashfreePayoutResult { Success = false, ErrorMessage = message };

        /// <summary>Keeps only alphanumeric + underscore chars and trims to 40 chars.</summary>
        private static string SanitizeBeneId(string raw)
        {
            var sb = new StringBuilder();
            foreach (var c in raw)
                if (char.IsLetterOrDigit(c) || c == '_')
                    sb.Append(c);
            var result = sb.ToString();
            return result.Length > 40 ? result.Substring(0, 40) : result;
        }

        /// <summary>Strip non-digit characters; Cashfree expects 8-12 digit Indian phone.</summary>
        private static string SanitizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return "0000000000";
            var sb = new StringBuilder();
            foreach (var c in phone)
                if (char.IsDigit(c))
                    sb.Append(c);
            var digits = sb.ToString();
            // Strip leading 91 if present and result is 12 digits
            if (digits.Length == 12 && digits.StartsWith("91"))
                digits = digits.Substring(2);
            return digits.Length >= 8 ? digits : "0000000000";
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Length <= max ? s : s.Substring(0, max);
        }
    }
}
