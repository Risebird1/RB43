using System;

namespace Risebird.Entities.ViewModel
{
    /// <summary>Azure blob discovered for a proctoring interview (DB row or storage listing).</summary>
    public sealed class ProctoringCapturedBlobViewModel
    {
        public string BlobUrl { get; set; } = string.Empty;
        public DateTime? CapturedAtUtc { get; set; }
        public string Kind { get; set; } = string.Empty;
    }
}
