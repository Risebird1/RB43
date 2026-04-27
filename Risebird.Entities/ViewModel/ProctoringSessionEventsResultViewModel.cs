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
    }
}
