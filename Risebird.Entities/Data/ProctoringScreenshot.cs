using System;

namespace Risebird.Entities.Data
{
    /// <summary>Row in Screenshots — a reference to one blob-stored DesktopProctor screenshot, not the image itself.</summary>
    public class ProctoringScreenshot
    {
        public long Id { get; set; }
        public string SessionToken { get; set; }
        public int Sequence { get; set; }
        public string BlobUrl { get; set; }
        public string ContentType { get; set; }
        public DateTime CapturedAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
