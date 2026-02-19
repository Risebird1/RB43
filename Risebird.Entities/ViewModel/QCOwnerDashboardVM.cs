using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// One row in the Founder QC Owner Dashboard (metrics for one QC owner).
    /// </summary>
    public class QCOwnerDashboardRowVM
    {
        public string QcOwner { get; set; }
        public int QcDone { get; set; }
        public int ReportsWithFlags { get; set; }
        public int ReportsWithoutFlags { get; set; }
        public int CriticalQueue_NoFlags { get; set; }
        public int TotalFlagsReported { get; set; }
        public int TotalRedoMarked { get; set; }
        public int TotalProxyMarked { get; set; }
        public decimal TotalPenaltyPercent { get; set; }
        public int PenalizedReports { get; set; }
        /// <summary>Total suspicious interviewers marked by this QC owner (QC created By) in the date range.</summary>
        public int TotalSuspiciousInterviewersMarked { get; set; }
        public int TotalPenaltyamount { get; set; }
    }

    /// <summary>
    /// Flag detail row for QC Owner Dashboard popup (flag name, sub value, count).
    /// </summary>
    public class QCOwnerFlagDetailRowVM
    {
        public string FlagName { get; set; }
        public string SubFlagValue { get; set; }
        public int FlagCount { get; set; }
    }

    /// <summary>
    /// Response for GetQCOwnerFlagDetails API (popup when clicking Total Flags Reported).
    /// </summary>
    public class QCOwnerFlagDetailResponseVM
    {
        public string QcOwner { get; set; }
        public string DateFrom { get; set; }
        public string DateTo { get; set; }
        public List<QCOwnerFlagDetailRowVM> FlagDetails { get; set; } = new List<QCOwnerFlagDetailRowVM>();
    }

    /// <summary>
    /// Response for QC Owner Dashboard API: date range + list of rows.
    /// </summary>
    public class QCOwnerDashboardResponseVM
    {
        public string DateFrom { get; set; }
        public string DateTo { get; set; }
        public List<QCOwnerDashboardRowVM> Rows { get; set; } = new List<QCOwnerDashboardRowVM>();
    }
}
