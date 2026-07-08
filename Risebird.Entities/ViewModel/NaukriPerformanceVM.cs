using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    // View model backing the NaukriOnboarding operator-performance / supervision page.
    public class NaukriPerformanceVM
    {
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public string Operator { get; set; }
        public bool IncludeSystem { get; set; }

        // Distinct event types present in the window (column order for the matrix).
        public List<string> EventTypes { get; set; } = new List<string>();

        // One row per operator with the per-event-type counts filled in.
        public List<NaukriPerformanceRow> Operators { get; set; } = new List<NaukriPerformanceRow>();

        // Recent detailed activity (capped).
        public List<NaukriPerformanceLogItem> Log { get; set; } = new List<NaukriPerformanceLogItem>();

        // Roll-ups for the summary cards.
        public int TotalActions { get; set; }
        public int TotalOperators { get; set; }
        public int TotalTicketsTouched { get; set; }
    }

    public class NaukriPerformanceRow
    {
        public string Operator { get; set; }
        public int TotalActions { get; set; }
        public int TicketsTouched { get; set; }
        public DateTime? FirstActionAt { get; set; }
        public DateTime? LastActionAt { get; set; }
        public Dictionary<string, int> EventCounts { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public int CountFor(string eventType)
            => EventCounts.TryGetValue(eventType, out var v) ? v : 0;
    }

    public class NaukriPerformanceLogItem
    {
        public DateTime ChangedAt { get; set; }
        public string Operator { get; set; }
        public int RecordId { get; set; }
        public string CandidateName { get; set; }
        public string MagName { get; set; }
        public string EventType { get; set; }
        public string FromValue { get; set; }
        public string ToValue { get; set; }
    }
}
