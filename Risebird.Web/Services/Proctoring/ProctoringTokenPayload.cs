using System;

namespace Risebird.Web.Services.Proctoring
{
    public sealed class ProctoringTokenPayload
    {
        public string Email { get; set; } = string.Empty;
        public int InterviewId { get; set; }
        public string AiSessionId { get; set; } = string.Empty;
        public DateTime ExpiresUtc { get; set; }
    }
}
