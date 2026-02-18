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
    /// Response for QC Owner Dashboard API: date range + list of rows.
    /// </summary>
    public class QCOwnerDashboardResponseVM
    {
        public string DateFrom { get; set; }
        public string DateTo { get; set; }
        public List<QCOwnerDashboardRowVM> Rows { get; set; } = new List<QCOwnerDashboardRowVM>();
    }
}
