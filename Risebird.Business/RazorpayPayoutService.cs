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
    /// Razorpay X Payouts API - uses standard flow: Create Contact → Create Fund Account → Create Payout.
    /// See: https://razorpay.com/docs/api/x/contacts/create/
    ///      https://razorpay.com/docs/api/x/fund-accounts/create/bank-account/
    ///      https://razorpay.com/docs/api/x/payouts/create/bank-account/
    /// </summary>
    public class RazorpayPayoutService : IRazorpayPayoutService
    {
        private const string RazorpayPayoutsBaseUrl = "https://api.razorpay.com/v1/";
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public RazorpayPayoutService(HttpClient httpClient, IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _applicationErrorLogger = applicationErrorLogger ?? throw new ArgumentNullException(nameof(applicationErrorLogger));

            var keyId = _configuration["Razorpay:KeyId"];
            var keySecret = _configuration["Razorpay:KeySecret"];
            if (!string.IsNullOrEmpty(keyId) && !string.IsNullOrEmpty(keySecret))
            {
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{keyId}:{keySecret}"));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            }
            _httpClient.BaseAddress = new Uri(RazorpayPayoutsBaseUrl);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<RazorpayPayoutResult> CreatePayoutToBankAsync(
            string beneficiaryName,
            string beneficiaryAccountNumber,
            string ifscCode,
            string contactEmail,
            long amountInPaise,
            string referenceId,
            string narration,
            string contactPhone = null)
        {
            var result = new RazorpayPayoutResult { Success = false };

            var accountNumber = _configuration["Razorpay:PayoutAccountNumber"];
            if (string.IsNullOrWhiteSpace(accountNumber))
            {
                result.ErrorMessage = "Razorpay PayoutAccountNumber is not configured.";
                return result;
            }

            if (amountInPaise < 100)
            {
                result.ErrorMessage = "Minimum payout amount is 100 paise (₹1).";
                return result;
            }

            var idempotencyKey = Guid.NewGuid().ToString("N");
            var refId = (referenceId ?? idempotencyKey).Length > 40 ? (referenceId ?? idempotencyKey).Substring(0, 40) : (referenceId ?? idempotencyKey);
            var mode = _configuration["Razorpay:PayoutMode"] ?? "IMPS";
            var purpose = _configuration["Razorpay:PayoutPurpose"] ?? "payout";
            var queueIfLowBalance = string.Equals(_configuration["Razorpay:QueueIfLowBalance"], "true", StringComparison.OrdinalIgnoreCase);

            try
            {
                // Step 1: Create Contact (POST /v1/contacts)
                var contactPayload = new
                {
                    name = (beneficiaryName ?? "Interviewer").Length > 50 ? (beneficiaryName ?? "Interviewer").Substring(0, 50) : (beneficiaryName ?? "Interviewer"),
                    email = contactEmail ?? "noreply@risebird.io",
                    contact = (contactPhone ?? "0000000000").Replace(" ", "").Length >= 10 ? (contactPhone ?? "0000000000").Replace(" ", "").Substring(0, 10) : "0000000000",
                    type = "vendor",
                    reference_id = refId
                };

                var contactResponse = await PostAsync("contacts", contactPayload, null).ConfigureAwait(false);
                if (!contactResponse.IsSuccessStatusCode)
                {
                    result.ErrorMessage = await ParseErrorAsync(contactResponse, "Contact");
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, new Exception($"Razorpay create contact failed: {result.ErrorMessage}"));
                    return result;
                }

                var contactBody = await contactResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                var contactId = JObject.Parse(contactBody)["id"]?.ToString();
                if (string.IsNullOrEmpty(contactId))
                {
                    result.ErrorMessage = "Razorpay did not return contact id.";
                    return result;
                }

                // Step 2: Create Fund Account (POST /v1/fund_accounts)
                var fundAccountPayload = new
                {
                    contact_id = contactId,
                    account_type = "bank_account",
                    bank_account = new
                    {
                        name = (beneficiaryName ?? "Interviewer").Length > 50 ? (beneficiaryName ?? "Interviewer").Substring(0, 50) : (beneficiaryName ?? "Interviewer"),
                        ifsc = (ifscCode ?? "").Trim().Replace(" ", "").Length >= 11 ? (ifscCode ?? "").Trim().Replace(" ", "").Substring(0, 11) : (ifscCode ?? "").Trim().Replace(" ", ""),
                        account_number = (beneficiaryAccountNumber ?? "").Trim().Replace(" ", "")
                    }
                };

                var fundResponse = await PostAsync("fund_accounts", fundAccountPayload, null).ConfigureAwait(false);
                if (!fundResponse.IsSuccessStatusCode)
                {
                    result.ErrorMessage = await ParseErrorAsync(fundResponse, "Fund Account");
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, new Exception($"Razorpay create fund account failed: {result.ErrorMessage}"));
                    return result;
                }

                var fundBody = await fundResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                var fundAccountId = JObject.Parse(fundBody)["id"]?.ToString();
                if (string.IsNullOrEmpty(fundAccountId))
                {
                    result.ErrorMessage = "Razorpay did not return fund_account id.";
                    return result;
                }

                // Step 3: Create Payout (POST /v1/payouts) - per https://razorpay.com/docs/api/x/payouts/create/bank-account/
                var payoutPayload = new
                {
                    account_number = accountNumber.Trim(),
                    fund_account_id = fundAccountId,
                    amount = amountInPaise,
                    currency = "INR",
                    mode = mode.ToUpperInvariant(),
                    purpose = purpose,
                    queue_if_low_balance = queueIfLowBalance,
                    reference_id = refId,
                    narration = (narration ?? "Risebird payout").Length > 30 ? (narration ?? "Risebird payout").Substring(0, 30) : (narration ?? "Risebird payout"),
                    notes = new { source = "risebird_interviewer_payout" }
                };

                var payoutResponse = await PostAsync("payouts", payoutPayload, idempotencyKey).ConfigureAwait(false);
                var payoutBody = await payoutResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (payoutResponse.IsSuccessStatusCode)
                {
                    var jobj = JObject.Parse(payoutBody);
                    result.Success = true;
                    result.PayoutId = jobj["id"]?.ToString();
                    result.Status = jobj["status"]?.ToString();
                    result.Fees = jobj["fees"]?.Value<long>();
                    return result;
                }

                result.ErrorMessage = await ParseErrorAsync(payoutResponse, "Payout", payoutBody);
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, new Exception($"Razorpay payout failed: {result.ErrorMessage}"));
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            return result;
        }

        /// <summary>
        /// Fetch payout status by payout ID. GET /v1/payouts/{id}
        /// </summary>
        public async Task<RazorpayPayoutResult> GetPayoutStatusAsync(string payoutId)
        {
            var result = new RazorpayPayoutResult { Success = false };
            if (string.IsNullOrWhiteSpace(payoutId))
            {
                result.ErrorMessage = "Payout ID is required.";
                return result;
            }

            try
            {
                var response = await _httpClient.GetAsync($"payouts/{payoutId.Trim()}").ConfigureAwait(false);
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var jobj = JObject.Parse(body);
                    result.Success = true;
                    result.PayoutId = jobj["id"]?.ToString();
                    result.Status = jobj["status"]?.ToString();
                    result.Amount = jobj["amount"]?.Value<long>();
                    result.Utr = jobj["utr"]?.ToString();
                    return result;
                }

                result.ErrorMessage = await ParseErrorAsync(response, "Fetch Payout", body);
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, new Exception($"Razorpay get payout status failed: {result.ErrorMessage}"));
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            return result;
        }

        /// <summary>
        /// Validate bank account: Create Contact → Create Fund Account → POST /v1/fund_accounts/validations.
        /// </summary>
        public async Task<RazorpayValidationResult> ValidateBankAccountAsync(
            string beneficiaryName,
            string beneficiaryAccountNumber,
            string ifscCode,
            string contactEmail,
            string contactPhone = null)
        {
            var result = new RazorpayValidationResult { Success = false };
            var accountNumber = _configuration["Razorpay:PayoutAccountNumber"];
            if (string.IsNullOrWhiteSpace(accountNumber))
            {
                result.ErrorMessage = "Razorpay PayoutAccountNumber is not configured.";
                return result;
            }
            var trimmedAccount = (beneficiaryAccountNumber ?? "").Trim().Replace(" ", "");
            var trimmedIfsc = (ifscCode ?? "").Trim().Replace(" ", "");
            if (string.IsNullOrEmpty(trimmedAccount) || string.IsNullOrEmpty(trimmedIfsc))
            {
                result.ErrorMessage = "Bank account number and IFSC are required.";
                return result;
            }

            try
            {
                var refId = $"RB_V_{DateTime.UtcNow:yyyyMMddHHmmss}";
                var name = (beneficiaryName ?? "Interviewer").Length > 50 ? (beneficiaryName ?? "Interviewer").Substring(0, 50) : (beneficiaryName ?? "Interviewer");

                var contactPayload = new
                {
                    name,
                    email = contactEmail ?? "noreply@risebird.io",
                    contact = (contactPhone ?? "0000000000").Replace(" ", "").Length >= 10 ? (contactPhone ?? "0000000000").Replace(" ", "").Substring(0, 10) : "0000000000",
                    type = "vendor",
                    reference_id = refId
                };
                var contactResponse = await PostAsync("contacts", contactPayload, null).ConfigureAwait(false);
                if (!contactResponse.IsSuccessStatusCode)
                {
                    result.ErrorMessage = await ParseErrorAsync(contactResponse, "Contact");
                    return result;
                }
                var contactBody = await contactResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                var contactId = JObject.Parse(contactBody)["id"]?.ToString();
                if (string.IsNullOrEmpty(contactId))
                {
                    result.ErrorMessage = "Razorpay did not return contact id.";
                    return result;
                }

                var fundAccountPayload = new
                {
                    contact_id = contactId,
                    account_type = "bank_account",
                    bank_account = new
                    {
                        name,
                        ifsc = trimmedIfsc.Length >= 11 ? trimmedIfsc.Substring(0, 11) : trimmedIfsc,
                        account_number = trimmedAccount
                    }
                };
                var fundResponse = await PostAsync("fund_accounts", fundAccountPayload, null).ConfigureAwait(false);
                if (!fundResponse.IsSuccessStatusCode)
                {
                    result.ErrorMessage = await ParseErrorAsync(fundResponse, "Fund Account");
                    return result;
                }
                var fundBody = await fundResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                var fundAccountId = JObject.Parse(fundBody)["id"]?.ToString();
                if (string.IsNullOrEmpty(fundAccountId))
                {
                    result.ErrorMessage = "Razorpay did not return fund_account id.";
                    return result;
                }

                var validationPayload = new
                {
                    account_number = accountNumber.Trim(),
                    fund_account = new { id = fundAccountId },
                    amount = 100,
                    currency = "INR",
                    notes = new { source = "risebird_bank_verification" }
                };
                var validationResponse = await PostAsync("fund_accounts/validations", validationPayload, null).ConfigureAwait(false);
                var validationBody = await validationResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!validationResponse.IsSuccessStatusCode)
                {
                    result.ErrorMessage = await ParseErrorAsync(validationResponse, "Validation", validationBody);
                    return result;
                }

                var jobj = JObject.Parse(validationBody);
                result.Success = true;
                result.ValidationId = jobj["id"]?.ToString();
                result.Status = jobj["status"]?.ToString();
                var resultsObj = jobj["results"];
                if (resultsObj != null)
                {
                    result.RegisteredName = resultsObj["registered_name"]?.ToString();
                    result.AccountStatus = resultsObj["account_status"]?.ToString();
                }
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return result;
        }

        private async Task<HttpResponseMessage> PostAsync(string path, object payload, string idempotencyKey)
        {
            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = content };
            if (!string.IsNullOrEmpty(idempotencyKey))
                request.Headers.Add("X-Payout-Idempotency", idempotencyKey);
            return await _httpClient.SendAsync(request).ConfigureAwait(false);
        }

        private static async Task<string> ParseErrorAsync(HttpResponseMessage response, string step, string responseBody = null)
        {
            var body = responseBody ?? await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            try
            {
                var errObj = JObject.Parse(body);
                var desc = errObj["error"]?["description"]?.ToString();
                if (!string.IsNullOrEmpty(desc)) return $"{step}: {desc}";
            }
            catch { }
            return $"Razorpay {step} error ({(int)response.StatusCode}): {body}";
        }
    }
}
