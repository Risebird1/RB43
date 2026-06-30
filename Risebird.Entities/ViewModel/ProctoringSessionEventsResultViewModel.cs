using Risebird.Entities.Data;
using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class ProctoringSessionEventsResultViewModel
    {
        public Guid SessionId { get; set; }
        public int InterviewId { get; set; }
        public string CandidateName { get; set; }
        public string CandidateEmail { get; set; }
        public IReadOnlyList<ProctoringSessionEvent> Events { get; set; } = Array.Empty<ProctoringSessionEvent>();

        /// <summary>Screenshots listed from Azure when telemetry rows lack blobUrl metadata.</summary>
        public IReadOnlyList<ProctoringCapturedBlobViewModel> FallbackScreenshots { get; set; } =
            Array.Empty<ProctoringCapturedBlobViewModel>();

        /// <summary>Video chunks listed from Azure when telemetry rows lack blobUrl metadata.</summary>
        public IReadOnlyList<ProctoringCapturedBlobViewModel> FallbackVideoChunks { get; set; } =
            Array.Empty<ProctoringCapturedBlobViewModel>();

        /// <summary>Why screenshots/video may be missing on this report.</summary>
        public ProctoringMediaDiagnosticsViewModel MediaDiagnostics { get; set; }
    }
}
