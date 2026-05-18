using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Business
{
    /// <summary>
    /// Integrates with the Cashfree Payouts V2 API to disburse funds to an interviewer's bank account.
    /// Flow: Add/verify beneficiary → Async transfer → (optional) status poll.
    /// V2 authentication uses x-client-id + x-client-secret + x-api-version headers on every request.
    /// No separate /authorize call is required.
    /// Sandbox base URL : https://sandbox.cashfree.com
    /// Production base URL: https://api.cashfree.com
    /// </summary>
    public class CashfreePayoutService : ICashfreePayoutService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

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
                // Stable beneId derived from the bank account number so the same
                // interviewer always maps to the same Cashfree beneficiary.
                var beneId = StableBeneId(accountNumber);

                // Add beneficiary; if already registered, fetch the existing beneId.
                var resolvedBeneId = await AddOrFetchBeneficiaryAsync(
                    beneId, accountName, accountNumber, ifscCode, email, phone);

                if (resolvedBeneId == null)
                    return Fail("Failed to register or retrieve beneficiary with Cashfree.");

                return await RequestTransferAsync(resolvedBeneId, amount, transferId, remarks);
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
                return await FetchTransferStatusAsync(transferId);
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
        /// Tries to register a bank beneficiary using the V2 API.
        /// POST {base}/payout/beneficiary
        ///   - Success   : HTTP 201 → returns our generated beneId
        ///   - Duplicate : HTTP 409 → calls GET /payout/beneficiary to retrieve the existing beneId
        ///   - Other     : logs and returns null
        /// </summary>
        private async Task<string> AddOrFetchBeneficiaryAsync(
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
                beneficiary_id   = beneId,
                beneficiary_name = name,
                beneficiary_instrument_details = new
                {
                    bank_account_number = bankAccount,
                    bank_ifsc           = ifsc
                },
                beneficiary_contact_details = new
                {
                    beneficiary_email = email,
                    beneficiary_phone = phone
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/payout/beneficiary");
            AddV2Headers(request);
            request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var body     = await response.Content.ReadAsStringAsync();

            // HTTP 201: beneficiary created successfully
            if ((int)response.StatusCode == 201)
                return beneId;

            // HTTP 409: beneficiary already exists — look up the registered beneId by bank details
            if ((int)response.StatusCode == 409)
                return await FetchBeneIdByBankDetailsAsync(bankAccount, ifsc);

            await _applicationErrorLogger.LogMessage(
                LogLevelInfo.Exception,
                new Exception($"Cashfree POST /payout/beneficiary failed [{(int)response.StatusCode}]: {body}"));
            return null;
        }

        /// <summary>
        /// Looks up an existing beneficiary by bank account + IFSC.
        /// GET {base}/payout/beneficiary?bank_account_number={account}&amp;bank_ifsc={ifsc}
        /// Returns the beneficiary_id on HTTP 200, null on any error.
        /// </summary>
        private async Task<string> FetchBeneIdByBankDetailsAsync(string bankAccount, string ifsc)
        {
            var baseUrl = BaseUrl();
            var url = $"{baseUrl}/payout/beneficiary" +
                      $"?bank_account_number={Uri.EscapeDataString(bankAccount)}" +
                      $"&bank_ifsc={Uri.EscapeDataString(ifsc)}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            AddV2Headers(request);

            var response      = await _httpClient.SendAsync(request);
            var body          = await response.Content.ReadAsStringAsync();
            var json          = JObject.Parse(body);
            var fetchedBeneId = json["beneficiary_id"]?.ToString();

            if (response.IsSuccessStatusCode && !string.IsNullOrEmpty(fetchedBeneId))
                return fetchedBeneId;

            await _applicationErrorLogger.LogMessage(
                LogLevelInfo.Exception,
                new Exception($"Cashfree GET /payout/beneficiary failed [{(int)response.StatusCode}]: {body}"));
            return null;
        }

        /// <summary>
        /// Initiates an async bank transfer using the V2 Transfers endpoint.
        /// POST {base}/payout/transfers
        /// Authentication: x-client-id + x-client-secret + x-api-version headers.
        /// </summary>
        private async Task<CashfreePayoutResult> RequestTransferAsync(
            string beneId,
            decimal amount,
            string transferId,
            string remarks)
        {
            var baseUrl = BaseUrl();

            // v2 valid transfer_mode values: bank, imps, neft, rtgs, upi, …
            var configMode   = _configuration[$"{CfSection}:TransferMode"] ?? "bank";
            var transferMode = configMode.Equals("banktransfer", StringComparison.OrdinalIgnoreCase)
                ? "banktransfer"
                : configMode;

            var payload = new
            {
                transfer_id     = transferId,
                transfer_amount = amount,
                transfer_mode   = transferMode,
                remarks         = Truncate(remarks, 70),
                beneficiary_details = new
                {
                    beneficiary_id = beneId
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/payout/transfers");
            AddV2Headers(request);
            request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var body     = await response.Content.ReadAsStringAsync();
            var json     = JObject.Parse(body);

            var status       = json["status"]?.ToString();
            var statusCode   = json["status_code"]?.ToString();
            var statusDesc   = json["status_description"]?.ToString();
            var cftransferid = json["cf_transfer_id"]?.ToString();
            decimal.TryParse(json["transfer_service_charge"]?.ToString(), out var fees);

            // Terminal success or accepted-for-processing states
            if (status == "RECEIVED" || status == "PENDING" ||
                status == "QUEUED"   || status == "SUCCESS" ||
                status == "APPROVAL_PENDING")
            {
                return new CashfreePayoutResult
                {
                    Success  = true,
                    PayoutId = transferId,
                    Status   = status,
                    Amount   = amount,
                    Fees     = fees,
                    CFPayoutId = cftransferid
                };
            }

            await _applicationErrorLogger.LogMessage(
                LogLevelInfo.Exception,
                new Exception($"Cashfree POST /payout/transfers failed [{(int)response.StatusCode}] " +
                              $"status={status} code={statusCode}: {body}"));

            return new CashfreePayoutResult
            {
                Success      = false,
                ErrorMessage = statusDesc ?? status ?? body,
                ErrorCode    = statusCode
            };
        }

        /// <summary>
        /// Fetches the current status of a previously initiated transfer.
        /// GET {base}/payout/transfers?transfer_id={transferId}
        /// Authentication: x-client-id + x-client-secret + x-api-version headers.
        /// </summary>
        private async Task<CashfreePayoutResult> FetchTransferStatusAsync(string transferId)
        {
            var baseUrl = BaseUrl();
            var url     = $"{baseUrl}/payout/transfers?transfer_id={Uri.EscapeDataString(transferId)}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            AddV2Headers(request);

            var response = await _httpClient.SendAsync(request);
            var body     = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new CashfreePayoutResult
                {
                    Success      = false,
                    PayoutId     = transferId,
                    ErrorMessage = $"Cashfree returned HTTP {(int)response.StatusCode}: {body}",
                    ErrorCode    = ((int)response.StatusCode).ToString()
                };
            }

            var json   = JObject.Parse(body);
            var status = json["status"]?.ToString();
            var utr    = json["transfer_utr"]?.ToString();
            decimal.TryParse(json["transfer_amount"]?.ToString(), out var amt);
            decimal.TryParse(json["transfer_service_charge"]?.ToString(), out var fees);

            return new CashfreePayoutResult
            {
                Success  = true,
                PayoutId = transferId,
                Status   = status,
                Amount   = amt,
                Fees     = fees,
                Utr      = utr
            };
        }

        // ------------------------------------------------------------------ //
        //  Utility                                                             //
        // ------------------------------------------------------------------ //

        /// <summary>Attaches V2 authentication headers to a request.</summary>
        private void AddV2Headers(HttpRequestMessage request)
        {
            var clientId     = _configuration[$"{CfSection}:ClientId"];
            var clientSecret = _configuration[$"{CfSection}:ClientSecret"];
            var apiVersion   = _configuration[$"{CfSection}:ApiVersion"] ?? "2024-01-01";

            request.Headers.Add("x-client-id",     clientId);
            request.Headers.Add("x-client-secret", clientSecret);
            request.Headers.Add("x-api-version",   apiVersion);
        }

        private string BaseUrl()
        {
            var url = _configuration[$"{CfSection}:BaseUrl"];
            return string.IsNullOrWhiteSpace(url)
                ? "https://sandbox.cashfree.com"
                : url.TrimEnd('/');
        }

        private static CashfreePayoutResult Fail(string message) =>
            new CashfreePayoutResult { Success = false, ErrorMessage = message };

        /// <summary>
        /// Builds a stable, per-interviewer beneId from the bank account number.
        /// Format: RB_{sanitized account number}, max 40 chars (Cashfree limit is 50).
        /// </summary>
        private static string StableBeneId(string accountNumber)
        {
            var sb = new StringBuilder("RB_");
            foreach (var c in accountNumber ?? string.Empty)
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
