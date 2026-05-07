using System;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Proctoring media saved to blob storage (tab screenshots and screen-share chunks) for report display.
    /// </summary>
    public sealed class ProctoringBlobPreviewVm
    {
        public string BlobUrl { get; set; }
        public string EventKind { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string MimeType { get; set; }
        public long? ChunkSeq { get; set; }
        /// <summary>Captured-at string from extension payload metadata when present.</summary>
        public string CapturedAtRaw { get; set; }

        public bool IsVideo =>
            !string.IsNullOrEmpty(MimeType) &&
            MimeType.IndexOf("video", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
