using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class ProctoringMediaDiagnosticsViewModel
    {
        public int InterviewId { get; set; }
        public DateTime ScheduledStartLocal { get; set; }
        public DateTime ScheduledEndLocal { get; set; }
        public int? MeetingStatusId { get; set; }
        public int TotalEvents { get; set; }
        public int JoinEvents { get; set; }
        public int StartEvents { get; set; }
        public int StopEvents { get; set; }
        public int ScreenshotEvents { get; set; }
        public int ScreenshotWithBlob { get; set; }
        public int ScreenshotUploadFailed { get; set; }
        public int ChunkEvents { get; set; }
        public int ChunkWithBlob { get; set; }
        public int ChunkUploadFailed { get; set; }
        public int AzureScreenshotFallback { get; set; }
        public int AzureChunkFallback { get; set; }
        public IReadOnlyList<string> LikelyCauses { get; set; } = Array.Empty<string>();
    }
}
