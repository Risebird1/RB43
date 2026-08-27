using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    public class PlivoCallsController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PlivoCallsController> _logger;

        public PlivoCallsController(IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<PlivoCallsController> logger)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Generate()
        {
            if (HttpContext.Session.GetString("email") == null)
            {
                return Unauthorized();
            }

            var authId = FirstNonEmpty(_configuration["Plivo:AuthId"], _configuration["Plivo:AUTH_ID"]);
            var authToken = FirstNonEmpty(_configuration["Plivo:AuthToken"], _configuration["Plivo:AUTH_TOKEN"]);
            var applicationId = FirstNonEmpty(_configuration["Plivo:ApplicationId"], _configuration["Plivo:APPLICATION_ID"]);
            var endpointUsername = FirstNonEmpty(_configuration["Plivo:EndpointUsername"], _configuration["Plivo:ENDPOINT_USERNAME"]);

            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(authId)) missing.Add("Plivo:AuthId");
            if (string.IsNullOrWhiteSpace(authToken)) missing.Add("Plivo:AuthToken");
            if (string.IsNullOrWhiteSpace(applicationId)) missing.Add("Plivo:ApplicationId");
            if (string.IsNullOrWhiteSpace(endpointUsername)) missing.Add("Plivo:EndpointUsername");

            if (missing.Count > 0)
            {
                var configError = "Plivo browser calling is not configured. Missing: " + string.Join(", ", missing);
                _logger.LogError("[Plivo] {Error}", configError);
                return BadRequest(new { error = configError });
            }

            var result = await CreateTokenFromPlivoApiAsync(authId, authToken, endpointUsername, applicationId);
            if (string.IsNullOrWhiteSpace(result.Token))
            {
                // No local fallback: a hand-rolled JWT would be rejected by Plivo at login
                // and would only hide whatever the API is actually complaining about.
                _logger.LogError("[Plivo] Access token request failed: {Error}", result.Error);
                return StatusCode(StatusCodes.Status502BadGateway, new { error = result.Error });
            }

            return Json(new { token = result.Token });
        }

        private async Task<(string Token, string Error)> CreateTokenFromPlivoApiAsync(
            string authId, string authToken, string endpointUsername, string applicationId)
        {
            var url = "https://api.plivo.com/v1/Account/" + authId + "/JWT/Token/";

            try
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var body = new JObject
                {
                    ["iss"] = authId,
                    ["sub"] = endpointUsername,
                    ["nbf"] = now - 30,
                    ["exp"] = now + 3600,
                    ["app"] = applicationId,
                    ["incoming_allow"] = true,
                    ["outgoing_allow"] = true,
                    ["per"] = new JObject
                    {
                        ["voice"] = new JObject
                        {
                            ["incoming_allow"] = true,
                            ["outgoing_allow"] = true
                        }
                    }
                };

                var client = _httpClientFactory.CreateClient();
                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(authId + ":" + authToken));
                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                    request.Content = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json");

                    var response = await client.SendAsync(request);
                    var json = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        return (null, $"Plivo returned {(int)response.StatusCode} {response.ReasonPhrase} for POST {url}. Response: {Truncate(json)}");
                    }

                    var parsed = JObject.Parse(json);
                    var token = parsed.Value<string>("token") ?? parsed.Value<string>("Token");
                    if (string.IsNullOrWhiteSpace(token))
                    {
                        return (null, $"Plivo returned 200 but no token field. Response: {Truncate(json)}");
                    }

                    return (token, null);
                }
            }
            catch (Exception ex)
            {
                return (null, $"Plivo access token request threw {ex.GetType().Name}: {ex.Message}");
            }
        }

        private static string Truncate(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "(empty)";
            }
            return value.Length <= 500 ? value : value.Substring(0, 500) + "...";
        }

        private static string FirstNonEmpty(params string[] values)
        {
            if (values == null)
            {
                return null;
            }

            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
        }
    }
}
