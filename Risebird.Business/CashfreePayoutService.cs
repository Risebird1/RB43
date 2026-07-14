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

        public async Task<CashfreePayoutResult> CreatePayoutToBankAsync(string accountName, string accountNumber, string ifscCode, string email, string phone, decimal amount, string transferId, string remarks)
        {
            try
            {
                // Stable beneId derived from the bank account number so the same
                // interviewer always maps to the same Cashfree beneficiary.
                var beneId = StableBeneId(accountNumber);

                // Add beneficiary; if already registered, fetch the existing beneId.
                var resolvedBeneId = await AddOrFetchBeneficiaryAsync(beneId, accountName, accountNumber, ifscCode, email, phone);

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

        public async Task<CashfreePanVerificationResult> VerifyPanAsync(string pan, string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(pan))
                    return PanVerifyFail("PAN number is required.");

                var panUpper = pan.Trim().ToUpperInvariant();
                var baseUrl = VerificationBaseUrl();
                var payload = new
                {
                    pan = panUpper,
                    name = string.IsNullOrWhiteSpace(name) ? null : name.Trim()
                };

                var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/pan");
                AddVerificationHeaders(request);
                request.Content = new StringContent(
                    JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var err = TryParseCashfreeError(body);
                    await _applicationErrorLogger.LogMessage(
                        LogLevelInfo.Exception,
                        new Exception($"Cashfree PAN verification failed [{(int)response.StatusCode}]: {body}"));
                    return PanVerifyFail(err ?? $"Cashfree returned HTTP {(int)response.StatusCode}.");
                }

                var json = JObject.Parse(body);
                var panStatus = json["pan_status"]?.ToString();
                var isValid = string.Equals(panStatus, "VALID", StringComparison.OrdinalIgnoreCase);
                var nameMatchResult = json["name_match_result"]?.ToString();

                var result = new CashfreePanVerificationResult
                {
                    Success = isValid,
                    IsValid = isValid,
                    PanStatus = panStatus,
                    NameAtPan = json["name_pan_card"]?.ToString(),
                    NameMatchResult = nameMatchResult,
                    NameMatchScore = json["name_match_score"]?.ToString(),
                    ReferenceId = json["reference_id"]?.Value<long?>()
                };

                if (!isValid)
                {
                    result.ErrorMessage = "The PAN number could not be verified. Please check and try again.";
                    return result;
                }

                if (!string.IsNullOrWhiteSpace(name) &&
                    !string.IsNullOrWhiteSpace(nameMatchResult) &&
                    (nameMatchResult == "NO_MATCH" || nameMatchResult == "POOR_PARTIAL_MATCH"))
                {
                    result.ErrorMessage =
                        $"PAN is valid but the name on record ({result.NameAtPan}) does not closely match the name you entered ({nameMatchResult}).";
                }

                return result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return PanVerifyFail($"Unexpected error verifying PAN: {e.Message}");
            }
        }

        public async Task<CashfreeBankVerificationResult> VerifyBankAccountAsync(string accountName, string accountNumber, string ifscCode, string phone)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(accountNumber) || string.IsNullOrWhiteSpace(ifscCode))
                    return VerifyFail("Account number and IFSC are required.");

                var baseUrl = VerificationBaseUrl();
                var payload = new
                {
                    bank_account = accountNumber.Trim(),
                    ifsc = ifscCode.Trim().ToUpperInvariant(),
                    name = string.IsNullOrWhiteSpace(accountName) ? null : accountName.Trim(),
                    phone = SanitizePhone(phone)
                };

                var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/bank-account/sync");
                AddVerificationHeaders(request);
                request.Content = new StringContent(
                    JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var err = TryParseCashfreeError(body);
                    await _applicationErrorLogger.LogMessage(
                        LogLevelInfo.Exception,
                        new Exception($"Cashfree BAV sync failed [{(int)response.StatusCode}]: {body}"));
                    return VerifyFail(err ?? $"Cashfree returned HTTP {(int)response.StatusCode}.");
                }

                var json = JObject.Parse(body);
                var accountStatus = json["account_status"]?.ToString();
                var accountStatusCode = json["account_status_code"]?.ToString();
                var isValid = string.Equals(accountStatus, "VALID", StringComparison.OrdinalIgnoreCase);
                var nameMatchResult = json["name_match_result"]?.ToString();

                var result = new CashfreeBankVerificationResult
                {
                    Success = isValid,
                    IsValid = isValid,
                    AccountStatus = accountStatus,
                    AccountStatusCode = accountStatusCode,
                    NameAtBank = json["name_at_bank"]?.ToString(),
                    NameMatchResult = nameMatchResult,
                    NameMatchScore = json["name_match_score"]?.ToString(),
                    BankName = json["bank_name"]?.ToString(),
                    Branch = json["branch"]?.ToString(),
                    ReferenceId = json["reference_id"]?.Value<long?>()
                };

                if (!isValid)
                {
                    result.ErrorMessage = FormatInvalidAccountMessage(accountStatusCode);
                    return result;
                }

                if (!string.IsNullOrWhiteSpace(accountName) &&
                    !string.IsNullOrWhiteSpace(nameMatchResult) &&
                    (nameMatchResult == "NO_MATCH" || nameMatchResult == "POOR_PARTIAL_MATCH"))
                {
                    result.ErrorMessage =
                        $"Account is valid but the name on record ({result.NameAtBank}) does not closely match the name you entered ({nameMatchResult}).";
                }

                return result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return VerifyFail($"Unexpected error verifying bank account: {e.Message}");
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
        private async Task<string> AddOrFetchBeneficiaryAsync(string beneId, string name, string bankAccount, string ifsc, string email, string phone)
        {
            var baseUrl = BaseUrl();
            var payload = new
            {
                beneficiary_id = beneId,
                beneficiary_name = name,
                beneficiary_instrument_details = new
                {
                    bank_account_number = bankAccount,
                    bank_ifsc = ifsc
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
            var body = await response.Content.ReadAsStringAsync();

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

            var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(body);
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
        private async Task<CashfreePayoutResult> RequestTransferAsync(string beneId,decimal amount,string transferId,string remarks)
        {
            var baseUrl = BaseUrl();

            // v2 valid transfer_mode values: bank, imps, neft, rtgs, upi, …
            var configMode = _configuration[$"{CfSection}:TransferMode"] ?? "bank";
            var transferMode = configMode.Equals("banktransfer", StringComparison.OrdinalIgnoreCase)
                ? "banktransfer"
                : configMode;

            var payload = new
            {
                transfer_id = transferId,
                transfer_amount = amount,
                transfer_mode = transferMode,
                transfer_remarks = Truncate(remarks, 70),
                beneficiary_details = new
                {
                    beneficiary_id = beneId
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/payout/transfers");
            AddV2Headers(request);
            request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(body);

            var status = json["status"]?.ToString();
            var statusCode = json["status_code"]?.ToString();
            var statusDesc = json["status_description"]?.ToString();
            var cftransferid = json["cf_transfer_id"]?.ToString();
            decimal.TryParse(json["transfer_service_charge"]?.ToString(), out var fees);

            // Terminal success or accepted-for-processing states
            if (status == "RECEIVED" || status == "PENDING" ||
                status == "QUEUED" || status == "SUCCESS" ||
                status == "APPROVAL_PENDING")
            {
                return new CashfreePayoutResult
                {
                    Success = true,
                    PayoutId = transferId,
                    Status = status,
                    Amount = amount,
                    Fees = fees,
                    CFPayoutId = cftransferid
                };
            }

            await _applicationErrorLogger.LogMessage(
                LogLevelInfo.Exception,
                new Exception($"Cashfree POST /payout/transfers failed [{(int)response.StatusCode}] " +
                              $"status={status} code={statusCode}: {body}"));

            return new CashfreePayoutResult
            {
                Success = false,
                ErrorMessage = statusDesc ?? status ?? body,
                ErrorCode = statusCode
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
            var url = $"{baseUrl}/payout/transfers?transfer_id={Uri.EscapeDataString(transferId)}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            AddV2Headers(request);

            var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new CashfreePayoutResult
                {
                    Success = false,
                    PayoutId = transferId,
                    ErrorMessage = $"Cashfree returned HTTP {(int)response.StatusCode}: {body}",
                    ErrorCode = ((int)response.StatusCode).ToString()
                };
            }

            var json = JObject.Parse(body);
            var status = json["status"]?.ToString();
            var utr = json["transfer_utr"]?.ToString();
            decimal.TryParse(json["transfer_amount"]?.ToString(), out var amt);
            decimal.TryParse(json["transfer_service_charge"]?.ToString(), out var fees);

            return new CashfreePayoutResult
            {
                Success = true,
                PayoutId = transferId,
                Status = status,
                Amount = amt,
                Fees = fees,
                Utr = utr
            };
        }

        // ------------------------------------------------------------------ //
        //  Utility                                                             //
        // ------------------------------------------------------------------ //

        /// <summary>Attaches V2 authentication headers to a request.</summary>
        private void AddV2Headers(HttpRequestMessage request)
        {
            var clientId = _configuration[$"{CfSection}:ClientId"];
            var clientSecret = _configuration[$"{CfSection}:ClientSecret"];
            var apiVersion = _configuration[$"{CfSection}:ApiVersion"] ?? "2024-01-01";

            request.Headers.Add("x-client-id", clientId);
            request.Headers.Add("x-client-secret", clientSecret);
            request.Headers.Add("x-api-version", apiVersion);
        }

        private void AddVerificationHeaders(HttpRequestMessage request)
        {
            var clientId = _configuration[$"{CfSection}:ClientId"];
            var clientSecret = _configuration[$"{CfSection}:ClientSecret"];
            var apiVersion = _configuration[$"{CfSection}:VerificationApiVersion"] ?? "2023-12-18";

            request.Headers.Add("x-client-id", clientId);
            request.Headers.Add("x-client-secret", clientSecret);
            request.Headers.Add("x-api-version", apiVersion);
        }

        private string VerificationBaseUrl()
        {
            var url = _configuration[$"{CfSection}:VerificationBaseUrl"];
            if (!string.IsNullOrWhiteSpace(url))
                return url.TrimEnd('/');

            return BaseUrl().Contains("sandbox", StringComparison.OrdinalIgnoreCase)
                ? "https://sandbox.cashfree.com/verification"
                : "https://api.cashfree.com/verification";
        }

        private static CashfreeBankVerificationResult VerifyFail(string message) =>
            new CashfreeBankVerificationResult { Success = false, IsValid = false, ErrorMessage = message };

        private static CashfreePanVerificationResult PanVerifyFail(string message) =>
            new CashfreePanVerificationResult { Success = false, IsValid = false, ErrorMessage = message };

        private static string TryParseCashfreeError(string body)
        {
            try
            {
                var json = JObject.Parse(body);
                return json["message"]?.ToString() ?? json["status_description"]?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static string FormatInvalidAccountMessage(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return "Bank account could not be verified.";
            return code switch
            {
                "INVALID_ACCOUNT_FAIL" => "The account number is invalid.",
                "INVALID_IFSC_FAIL" => "The IFSC code is invalid.",
                "ACCOUNT_BLOCKED" => "This bank account is blocked.",
                "NRE_ACCOUNT_FAIL" => "NRE accounts cannot be verified for payouts.",
                _ => $"Bank account verification failed ({code})."
            };
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
