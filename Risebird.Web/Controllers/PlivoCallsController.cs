using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Risebird.Web.Controllers
{
    /// <summary>
    /// Plivo browser calling: issues the access token used by PlivoCallMaker.js (client.loginWithAccessToken).
    /// </summary>
    public class PlivoCallsController : Controller
    {
        private const int TokenLifetimeSeconds = 3600;
        private readonly IConfiguration _configuration;

        public PlivoCallsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // POST: PlivoCalls/Generate
        [HttpPost]
        public IActionResult Generate()
        {
            // Only signed-in portal users may obtain a calling token.
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("email")))
            {
                return Unauthorized();
            }

            var authId = _configuration["PlivoSettings:AUTH_ID"];
            var authToken = _configuration["PlivoSettings:AUTH_TOKEN"];
            var applicationId = _configuration["PlivoSettings:APPLICATION_ID"];
            var endpointUsername = _configuration["PlivoSettings:EndpointUsername"];

            if (string.IsNullOrWhiteSpace(authId) || string.IsNullOrWhiteSpace(authToken)
                || string.IsNullOrWhiteSpace(applicationId) || string.IsNullOrWhiteSpace(endpointUsername))
            {
                return Json(new { token = (string)null });
            }

            var token = GenerateToken(authId, authToken, applicationId, endpointUsername);
            return Json(new { token });
        }

        /// <summary>
        /// Plivo voice access token: HS256 JWT signed with the Auth Token.
        /// </summary>
        private static string GenerateToken(string authId, string authToken, string applicationId, string endpointUsername)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var header = new Dictionary<string, object>
            {
                { "typ", "JWT" },
                { "alg", "HS256" },
                { "cty", "plivo;v=1" }
            };

            var payload = new Dictionary<string, object>
            {
                { "jti", Guid.NewGuid().ToString() },
                { "iss", authId },
                { "sub", endpointUsername },
                { "nbf", now },
                { "exp", now + TokenLifetimeSeconds },
                { "app", applicationId },
                { "per", new Dictionary<string, object>
                    {
                        { "voice", new Dictionary<string, object>
                            {
                                { "incoming_allow", true },
                                { "outgoing_allow", true }
                            }
                        }
                    }
                }
            };

            var unsigned = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(header)))
                         + "."
                         + Base64UrlEncode(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)));

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(authToken)))
            {
                var signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(unsigned));
                return unsigned + "." + Base64UrlEncode(signature);
            }
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
