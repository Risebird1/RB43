using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;

namespace Risebird.CommonHelper
{
    public sealed class ProctoringLaunchTokenService : IProctoringLaunchTokenService, IDisposable
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private readonly ECDsa _ecdsa;
        private readonly object _signLock = new object();

        // Registered as a Singleton (the dev key must stay the same for the process's lifetime,
        // not regenerate per request) — so this constructor must not take a Scoped dependency like
        // IApplicationErrorLogger (that's a captive-dependency DI validation failure). The one-time
        // dev-key warning below goes to Trace instead, which needs nothing from the container.
        public ProctoringLaunchTokenService(IConfiguration configuration)
        {
            var d = configuration["ProctoringApiSettings:SigningKey:D"];
            var x = configuration["ProctoringApiSettings:SigningKey:X"];
            var y = configuration["ProctoringApiSettings:SigningKey:Y"];

            if (!string.IsNullOrWhiteSpace(d) && !string.IsNullOrWhiteSpace(x) && !string.IsNullOrWhiteSpace(y))
            {
                var ecParameters = new ECParameters
                {
                    Curve = ECCurve.NamedCurves.nistP256,
                    D = Base64UrlDecode(d),
                    Q = new ECPoint { X = Base64UrlDecode(x), Y = Base64UrlDecode(y) }
                };
                _ecdsa = ECDsa.Create(ecParameters);
            }
            else
            {
                // Dev fallback only — regenerated every process start, so every previously minted
                // launch token stops validating on restart. Configure ProctoringApiSettings:SigningKey
                // with a real production keypair before relying on this in a live environment.
                _ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                var pub = _ecdsa.ExportParameters(includePrivateParameters: false);
                Trace.TraceWarning(
                    "ProctoringApiSettings:SigningKey is not configured — DesktopProctor is using an ephemeral, " +
                    "process-local ECDSA P-256 dev key. Public key (base64url) X=" + Base64UrlEncode(pub.Q.X) +
                    " Y=" + Base64UrlEncode(pub.Q.Y) + ". Do not use this in production.");
            }
        }

        public (string LaunchToken, DateTime ExpiresAtUtc) MintLaunchToken(
            string apiBaseUrl,
            string sessionToken,
            string interviewId,
            string candidateEmail,
            string browserSessionId,
            TimeSpan ttl)
        {
            var expiresAtUtc = DateTime.UtcNow.Add(ttl);
            var payload = new ProctoringLaunchTokenPayload
            {
                ApiBaseUrl = apiBaseUrl,
                SessionToken = sessionToken,
                InterviewId = interviewId,
                CandidateEmail = candidateEmail,
                BrowserSessionId = browserSessionId,
                Exp = new DateTimeOffset(expiresAtUtc).ToUnixTimeSeconds()
            };

            var payloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, JsonOptions)));
            var signingInput = "v1." + payloadB64;

            byte[] signature;
            lock (_signLock)
            {
                signature = _ecdsa.SignData(Encoding.UTF8.GetBytes(signingInput), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
            }

            return (signingInput + "." + Base64UrlEncode(signature), expiresAtUtc);
        }

        public void Dispose() => _ecdsa?.Dispose();

        private static string Base64UrlEncode(byte[] bytes) =>
            Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static byte[] Base64UrlDecode(string value)
        {
            var padded = value.Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
            }
            return Convert.FromBase64String(padded);
        }

        private sealed class ProctoringLaunchTokenPayload
        {
            public string ApiBaseUrl { get; set; }
            public string SessionToken { get; set; }
            public string InterviewId { get; set; }
            public string CandidateEmail { get; set; }
            public string BrowserSessionId { get; set; }
            public long Exp { get; set; }
        }
    }
}
