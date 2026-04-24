using System;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace Risebird.Web.Services.Proctoring
{
    public sealed class ProctoringTokenService : IProctoringTokenService
    {
        private readonly IDataProtector _protector;
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public ProctoringTokenService(IDataProtectionProvider dataProtectionProvider)
        {
            _protector = dataProtectionProvider.CreateProtector("Risebird.Proctoring.Session.v1");
        }

        public string CreateToken(string email, int interviewId, string aiSessionId)
        {
            var payload = new ProctoringTokenPayload
            {
                Email = email ?? string.Empty,
                InterviewId = interviewId,
                AiSessionId = aiSessionId ?? string.Empty,
                ExpiresUtc = DateTime.UtcNow.AddHours(8)
            };
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            return _protector.Protect(json);
        }

        public ProctoringTokenPayload? ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;
            try
            {
                var json = _protector.Unprotect(token);
                var payload = JsonSerializer.Deserialize<ProctoringTokenPayload>(json, JsonOptions);
                if (payload == null || payload.ExpiresUtc < DateTime.UtcNow)
                    return null;
                return payload;
            }
            catch
            {
                return null;
            }
        }
    }
}
