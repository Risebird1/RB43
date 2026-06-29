using Microsoft.AspNetCore.Http;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface ITdsComplianceService
    {
        /// <summary>Resolves quarter date range from FY and quarter name.</summary>
        (DateTime Start, DateTime End) ResolveQuarterDates(string financialYear, string quarter);

        /// <summary>Builds the FY and Quarter select-list items.</summary>
        (List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Years,
         List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Quarters)
            BuildSelectLists(string selectedYear, string selectedQuarter);

        /// <summary>Orchestrates all repo calls and assembles TdsComplianceVM.</summary>
        Task<TdsComplianceVM> GetComplianceAsync(string financialYear, string quarter);

        /// <summary>Save or update a challan (handles PDF path separately).</summary>
        Task<int> SaveChallanAsync(TdsChallanVM vm, string operatorEmail);

        /// <summary>Hard-delete challan and cascade link rows.</summary>
        Task DeleteChallanAsync(int id);

        /// <summary>Toggle IsPaid on a single challan.</summary>
        Task ToggleChallanPaidAsync(int id, bool isPaid, string financialYear, string quarter, string operatorEmail);

        /// <summary>Save or update quarterly return filing.</summary>
        Task SaveFilingAsync(TdsReturnFilingVM vm, string operatorEmail);

        /// <summary>
        /// Process Form16A PDF dump: extract PAN from filename, look up interviewer,
        /// upload to Azure Blob, write to InterviewerCertificates, return match summary.
        /// </summary>
        Task<Form16UploadResultVM> ProcessForm16DumpAsync(
            string financialYear, string quarter,
            IList<IFormFile> files,
            Func<IFormFile, Task<string>> uploadBlob);

        /// <summary>Build the 26Q xlsx export as a byte array.</summary>
        Task<byte[]> BuildExport26QAsync(string financialYear, string quarter);

        /// <summary>Returns interviewers due for a lapse reminder today.</summary>
        Task<IList<UnverifiedWalletInterviewer>> GetReminderTargetsAsync(DateTime asOfDate);

        /// <summary>Logs a reminder as sent; returns false if already logged (idempotent).</summary>
        Task<bool> MarkReminderSentAsync(int userid, string paymentReference, int reminderIndex, string emailTo);
    }
}
