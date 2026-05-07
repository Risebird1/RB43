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
        /// <summary>Blob-backed captures (screenshots + screen chunks) with public URLs for preview.</summary>
        public IReadOnlyList<ProctoringBlobPreviewVm> BlobMediaItems { get; set; } = Array.Empty<ProctoringBlobPreviewVm>();
    }
}
