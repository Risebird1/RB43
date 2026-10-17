using System;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Entities.ViewModel
{
    /// <summary>Count of one Potential Risk tab: distinct interviewers (the badge) and report rows (the tooltip).</summary>
    public class PotentialRiskTabCountVM
    {
        public string Tab { get; set; }
        public string Label { get; set; }
        public int Interviewers { get; set; }
        public int Rows { get; set; }
    }

    /// <summary>
    /// Status tabs on Potential Risk by Candidate (All · Pending · Under Review · Reviewed · Removed).
    /// Rows are reports; the tab follows the interviewer (their latest Potential Risk decision), so all of an
    /// interviewer's rows move together. Computed in memory from the rows the page already loads.
    /// </summary>
    public static class PotentialRiskTabs
    {
        public const string All = "All";
        public const string Pending = "Pending";
        public const string UnderReview = "UnderReview";
        public const string Reviewed = "Reviewed";
        public const string Removed = "Removed";

        public static readonly IReadOnlyList<string> Order = new[] { All, Pending, UnderReview, Reviewed, Removed };

        private static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [All] = "All",
            [Pending] = "Pending",
            [UnderReview] = "Under Review",
            [Reviewed] = "Reviewed",
            [Removed] = "Removed"
        };

        /// <summary>The canonical tab key; an empty or unknown value falls back to All.</summary>
        public static string Normalize(string tab)
        {
            if (string.IsNullOrWhiteSpace(tab)) return All;
            var t = tab.Trim();
            return Order.FirstOrDefault(k => string.Equals(k, t, StringComparison.OrdinalIgnoreCase)) ?? All;
        }

        public static string Label(string tab) => Labels.TryGetValue(Normalize(tab), out var l) ? l : tab;

        /// <summary>
        /// The tab a row belongs to (never All): Removed when the decision is Removed (3) or the interviewer is
        /// inactive; otherwise Under Review (1), Reviewed (2) or Pending (no decision).
        /// </summary>
        public static string TabOf(MeetingVM row)
        {
            if (row == null) return Pending;
            if (row.PotentialRiskStatus == 3 || row.InterviewerIsActive == false) return Removed;
            if (row.PotentialRiskStatus == 1) return UnderReview;
            if (row.PotentialRiskStatus == 2) return Reviewed;
            return Pending;
        }

        public static bool InTab(MeetingVM row, string tab)
        {
            var t = Normalize(tab);
            return t == All || TabOf(row) == t;
        }

        /// <summary>Rows of one tab (All = every row), in the original order.</summary>
        public static IEnumerable<MeetingVM> Filter(IEnumerable<MeetingVM> rows, string tab)
        {
            var t = Normalize(tab);
            return (rows ?? Enumerable.Empty<MeetingVM>()).Where(r => r != null && (t == All || TabOf(r) == t));
        }

        /// <summary>Per-tab counts in tab order: distinct interviewers (by user id, else email) and rows.</summary>
        public static IReadOnlyList<PotentialRiskTabCountVM> Counts(IEnumerable<MeetingVM> rows)
        {
            var list = (rows ?? Enumerable.Empty<MeetingVM>()).Where(r => r != null).ToList();
            return Order.Select(tab =>
            {
                var inTab = tab == All ? list : list.Where(r => TabOf(r) == tab).ToList();
                return new PotentialRiskTabCountVM
                {
                    Tab = tab,
                    Label = Labels[tab],
                    Rows = inTab.Count,
                    Interviewers = inTab.Select(InterviewerKey).Where(k => k != null).Distinct(StringComparer.OrdinalIgnoreCase).Count()
                };
            }).ToList();
        }

        /// <summary>Readable status for the badge and the export: Pending, Under Review, Reviewed, Removed.</summary>
        public static string StatusText(MeetingVM row) => Labels[TabOf(row)];

        private static string InterviewerKey(MeetingVM r)
        {
            if (r.InterviewerUserId > 0) return "id:" + r.InterviewerUserId;
            return string.IsNullOrWhiteSpace(r.interviewer) ? null : "em:" + r.interviewer.Trim();
        }
    }
}
