using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class TdsComplianceService : ITdsComplianceService
    {
        private readonly ITdsRepository _tdsRepo;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly int[] _reminderOffsets;

        public TdsComplianceService(
            ITdsRepository tdsRepo,
            IApplicationErrorLogger applicationErrorLogger,
            int[] reminderOffsets = null)
        {
            _tdsRepo                = tdsRepo;
            _applicationErrorLogger = applicationErrorLogger;
            _reminderOffsets        = reminderOffsets ?? new[] { 3, 7, 15, 30, 45, 60, 75, 82, 87, 89 };
        }

        // ------------------------------------------------------------------ //
        //  Quarter date resolution                                             //
        // ------------------------------------------------------------------ //

        public (DateTime Start, DateTime End) ResolveQuarterDates(string financialYear, string quarter)
        {
            var fyStart = int.Parse(financialYear.Split('-')[0]);
            return quarter switch
            {
                "Quarter 1" => (new DateTime(fyStart,     4,  1), new DateTime(fyStart,     6, 30)),
                "Quarter 2" => (new DateTime(fyStart,     7,  1), new DateTime(fyStart,     9, 30)),
                "Quarter 3" => (new DateTime(fyStart,    10,  1), new DateTime(fyStart,    12, 31)),
                "Quarter 4" => (new DateTime(fyStart + 1, 1,  1), new DateTime(fyStart + 1, 3, 31)),
                _           => throw new ArgumentException($"Unknown quarter: {quarter}")
            };
        }

        public (List<SelectListItem> Years, List<SelectListItem> Quarters)
            BuildSelectLists(string selectedYear, string selectedQuarter)
        {
            var currentYear = DateTime.Today.Month >= 4
                ? DateTime.Today.Year
                : DateTime.Today.Year - 1;

            // Six financial years ending at the current one. Range(currentYear - 4, 6) used to run
            // one year past it, offering an FY that has not started and has nothing to file.
            var years = Enumerable.Range(currentYear - 5, 6)
                .Select(y => new SelectListItem
                {
                    Value    = $"{y}-{y + 1}",
                    Text     = $"FY {y}-{y + 1}",
                    Selected = $"{y}-{y + 1}" == selectedYear
                }).ToList();

            var quarters = new[] { "Quarter 1", "Quarter 2", "Quarter 3", "Quarter 4" }
                .Select(q => new SelectListItem
                {
                    Value    = q,
                    Text     = q,
                    Selected = q == selectedQuarter
                }).ToList();

            return (years, quarters);
        }

        // ------------------------------------------------------------------ //
        //  Master compliance VM                                                //
        // ------------------------------------------------------------------ //

        public async Task<TdsComplianceVM> GetComplianceAsync(string financialYear, string quarter)
        {
            try
            {
                var (start, end)    = ResolveQuarterDates(financialYear, quarter);
                var (years, quarters) = BuildSelectLists(financialYear, quarter);

                var allRowsTask      = _tdsRepo.FetchInterviewerQuarterlyTdsAsync(start, end);
                var challansTask     = _tdsRepo.FetchTdsChallansAsync(financialYear, quarter);
                var weeklyTask       = _tdsRepo.FetchWeeklyPayoutsAsync(financialYear, quarter);
                var pendingTask      = _tdsRepo.FetchPendingTdsByWeekAsync(financialYear, quarter);
                var filingTask       = _tdsRepo.FetchTdsReturnFilingAsync(financialYear, quarter);
                var challanLinksTask = _tdsRepo.FetchInterviewerChallanNumbersAsync(financialYear, quarter);
                var paymentItemsTask = _tdsRepo.FetchInterviewerPaymentItemsAsync(start, end, financialYear, quarter);

                await Task.WhenAll(allRowsTask, challansTask, weeklyTask, pendingTask, filingTask, challanLinksTask, paymentItemsTask);

                var allRows      = allRowsTask.Result      ?? new List<InterviewerQuarterlyTdsRow>();
                var challans     = challansTask.Result     ?? new List<TdsChallanVM>();
                var weekly       = weeklyTask.Result       ?? new List<WeeklyPayoutVM>();
                var pending      = pendingTask.Result      ?? new List<PendingTdsWeekVM>();
                var filing       = filingTask.Result       ?? new TdsReturnFilingVM { FinancialYear = financialYear, Quarter = quarter };
                var challanLinks = challanLinksTask.Result ?? new List<InterviewerChallanLink>();
                var paymentItems = paymentItemsTask.Result ?? new List<PaymentHistoryItem>();

                // Ensure one row per week; sum TDS deposited when multiple challan lines exist
                pending = pending
                    .GroupBy(p => p.WeekNumber)
                    .Select(g =>
                    {
                        var first = g.First();
                        var tdsDue = g.Max(x => x.TdsDue);
                        var tdsDeposited = g.Sum(x => x.TdsDeposited);
                        return new PendingTdsWeekVM
                        {
                            WeekNumber   = g.Key,
                            WeekRange    = first.WeekRange,
                            TdsDue       = tdsDue,
                            TdsDeposited = tdsDeposited,
                            Outstanding  = tdsDue - tdsDeposited,
                            IsPaid       = g.All(x => x.IsPaid)
                        };
                    })
                    .OrderBy(p => p.WeekNumber)
                    .ToList();

                var challanByUser = challanLinks
                    .GroupBy(l => l.Userid)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(l => l.ChallanSerialNumber)
                              .Where(s => !string.IsNullOrWhiteSpace(s))
                              .Distinct()
                              .ToList());

                var paymentsByUser = paymentItems
                    .GroupBy(p => p.Userid)
                    .ToDictionary(g => g.Key, g => g.OrderBy(p => p.PaymentDate).ToList());

                foreach (var row in allRows)
                {
                    row.ChallanNumbers = challanByUser.TryGetValue(row.Userid, out var nums) ? nums : new List<string>();
                    row.PaymentItems   = paymentsByUser.TryGetValue(row.Userid, out var items) ? items : new List<PaymentHistoryItem>();
                }

                foreach (var challan in challans)
                {
                    var interviewers = await _tdsRepo.FetchChallanInterviewersAsync(challan.id);
                    challan.Interviewers = interviewers?.ToList() ?? new List<ChallanInterviewerVM>();
                }

                var deductees       = allRows.Where(r => r.IncludeInFiling).ToList();
                var lapsedDeductees = allRows.Where(r => !r.IncludeInFiling).ToList();

                return new TdsComplianceVM
                {
                    SelectedYear    = financialYear,
                    SelectedQuarter = quarter,
                    QuarterRange    = $"{start:dd MMM yyyy} – {end:dd MMM yyyy}",
                    FinancialYears  = years,
                    Quarters        = quarters,
                    Deductees       = deductees,
                    LapsedDeductees = lapsedDeductees,
                    Challans        = challans.ToList(),
                    WeeklyPayouts   = weekly.ToList(),
                    PendingByWeek   = pending.ToList(),
                    Filing          = filing,
                    InterviewerPaidTotal = deductees.Sum(d => d.TotalPaid),
                    InterviewerTdsTotal  = deductees.Sum(d => d.Tds),
                    ChallanTdsTotal      = challans.Sum(c => c.TdsAmount),
                    ChallanCount         = challans.Count
                };
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        // ------------------------------------------------------------------ //
        //  Challan CRUD                                                        //
        // ------------------------------------------------------------------ //

        public async Task<int> SaveChallanAsync(TdsChallanVM vm, string operatorEmail)
        {
            try
            {
                vm.SectionCode = TdsConstants.SectionCode;
                vm.MinorHead   = TdsConstants.MinorHead;
                if (string.IsNullOrWhiteSpace(vm.TANNumber)) vm.TANNumber = TdsConstants.CompanyTAN;
                if (string.IsNullOrWhiteSpace(vm.Deductor))  vm.Deductor  = TdsConstants.DeductorName;

                return await _tdsRepo.AddOrUpdateTdsChallanAsync(vm, operatorEmail);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task DeleteChallanAsync(int id)
        {
            try
            {
                await _tdsRepo.DeleteTdsChallanAsync(id);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task ToggleChallanPaidAsync(
            int id, bool isPaid, string financialYear, string quarter, string operatorEmail)
        {
            try
            {
                var challans = await _tdsRepo.FetchTdsChallansAsync(financialYear, quarter);
                var challan  = challans?.FirstOrDefault(c => c.id == id);
                if (challan == null) return;
                challan.IsPaid = isPaid;
                await _tdsRepo.AddOrUpdateTdsChallanAsync(challan, operatorEmail);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        // ------------------------------------------------------------------ //
        //  Return filing                                                       //
        // ------------------------------------------------------------------ //

        public async Task SaveFilingAsync(TdsReturnFilingVM vm, string operatorEmail)
        {
            try
            {
                vm.ReturnType = TdsConstants.ReturnType;
                await _tdsRepo.AddOrUpdateTdsReturnFilingAsync(vm, operatorEmail);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        // ------------------------------------------------------------------ //
        //  Form 16A PDF auto-map                                              //
        // ------------------------------------------------------------------ //

        public async Task<Form16UploadResultVM> ProcessForm16DumpAsync(
            string financialYear, string quarter,
            IList<IFormFile> files,
            Func<IFormFile, Task<string>> uploadBlob)
        {
            var result = new Form16UploadResultVM();
            try
            {
                var panRegex = new Regex(@"[A-Z]{5}[0-9]{4}[A-Z]", RegexOptions.IgnoreCase);

                foreach (var file in files)
                {
                    try
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file.FileName).ToUpperInvariant();
                        var match    = panRegex.Match(fileName);

                        if (!match.Success)
                        {
                            result.Skipped.Add(file.FileName + " (no PAN in filename)");
                            continue;
                        }

                        var pan         = match.Value.ToUpperInvariant();
                        var interviewer = await _tdsRepo.FetchInterviewerByPanAsync(pan);

                        if (interviewer == null)
                        {
                            result.Unmatched.Add($"{file.FileName} (PAN {pan} – interviewer not found)");
                            continue;
                        }

                        var blobUrl = await uploadBlob(file);

                        await _tdsRepo.AddForm16CertificateAsync(
                            interviewer.Email, financialYear, quarter, blobUrl);

                        result.Matched.Add($"{file.FileName} → {interviewer.FirstName} {interviewer.LastName} ({pan})");
                    }
                    catch (Exception ex)
                    {
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        result.Skipped.Add(file.FileName + " (error during processing)");
                    }
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return result;
        }

        // ------------------------------------------------------------------ //
        //  Export 26Q xlsx                                                     //
        // ------------------------------------------------------------------ //

        public async Task<byte[]> BuildExport26QAsync(string financialYear, string quarter)
        {
            try
            {
                var (start, end) = ResolveQuarterDates(financialYear, quarter);

                var challansTask  = _tdsRepo.FetchTdsChallansAsync(financialYear, quarter);
                var deducteesTask = _tdsRepo.FetchInterviewerQuarterlyTdsAsync(start, end);
                var linksTask     = _tdsRepo.FetchInterviewerChallanNumbersAsync(financialYear, quarter);

                await Task.WhenAll(challansTask, deducteesTask, linksTask);

                var challans  = challansTask.Result  ?? new List<TdsChallanVM>();
                var allRows   = deducteesTask.Result ?? new List<InterviewerQuarterlyTdsRow>();
                var links     = linksTask.Result     ?? new List<InterviewerChallanLink>();

                var deductees = allRows.Where(r => r.IncludeInFiling).ToList();

                var challanByUser = links
                    .GroupBy(l => l.Userid)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(l => l.ChallanSerialNumber)
                              .Where(s => !string.IsNullOrWhiteSpace(s))
                              .Distinct()
                              .ToList());

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var ep = new ExcelPackage())
                {
                    // ── Sheet 1: Challan ──────────────────────────────────────────
                    var ws1 = ep.Workbook.Worksheets.Add("Challan");
                    var challanHeaders = new[]
                    {
                        "Week", "Sr No", "Section", "TAN", "Deductor", "TDS Amount (₹)",
                        "BSR Code", "Deposit Date", "Challan Serial No", "Minor Head", "Reference No"
                    };
                    for (int i = 0; i < challanHeaders.Length; i++)
                    {
                        ws1.Cells[1, i + 1].Value = challanHeaders[i];
                        ws1.Cells[1, i + 1].Style.Font.Bold = true;
                        ws1.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        ws1.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(0, 70, 127));
                        ws1.Cells[1, i + 1].Style.Font.Color.SetColor(Color.White);
                    }

                    int r1 = 2;
                    foreach (var c in challans.OrderBy(c => c.WeekNumber).ThenBy(c => c.SrNo))
                    {
                        ws1.Cells[r1, 1].Value  = c.WeekNumber;
                        ws1.Cells[r1, 2].Value  = c.SrNo;
                        ws1.Cells[r1, 3].Value  = c.SectionCode;
                        ws1.Cells[r1, 4].Value  = c.TANNumber;
                        ws1.Cells[r1, 5].Value  = c.Deductor;
                        ws1.Cells[r1, 6].Value  = c.TdsAmount;
                        ws1.Cells[r1, 6].Style.Numberformat.Format = "#,##0.00";
                        ws1.Cells[r1, 7].Value  = c.BSRCode;
                        ws1.Cells[r1, 8].Value  = c.DepositDate?.ToString("dd-MM-yyyy");
                        ws1.Cells[r1, 9].Value  = c.ChallanSerialNumber;
                        ws1.Cells[r1, 10].Value = c.MinorHead;
                        ws1.Cells[r1, 11].Value = c.ChallanReferenceNumber;
                        r1++;
                    }
                    ws1.Cells[ws1.Dimension?.Address ?? "A1"].AutoFitColumns();

                    // ── Sheet 2: Annexure ─────────────────────────────────────────
                    var ws2 = ep.Workbook.Worksheets.Add("Annexure");
                    var annexHeaders = new[]
                    {
                        "Deductee Code", "PAN", "First Name", "Middle Name", "Last Name",
                        "Amount Paid (₹)", "Section", "Rate %", "TDS (₹)", "Date Paid", "Challan Serial No(s)"
                    };
                    for (int i = 0; i < annexHeaders.Length; i++)
                    {
                        ws2.Cells[1, i + 1].Value = annexHeaders[i];
                        ws2.Cells[1, i + 1].Style.Font.Bold = true;
                        ws2.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        ws2.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(0, 70, 127));
                        ws2.Cells[1, i + 1].Style.Font.Color.SetColor(Color.White);
                    }

                    int r2 = 2;
                    foreach (var d in deductees)
                    {
                        var challanNums = challanByUser.TryGetValue(d.Userid, out var nums)
                            ? string.Join(", ", nums)
                            : "";

                        ws2.Cells[r2, 1].Value  = d.DeducteeCode;
                        ws2.Cells[r2, 2].Value  = d.PAN;
                        ws2.Cells[r2, 3].Value  = d.FirstName;
                        ws2.Cells[r2, 4].Value  = d.MiddleName;
                        ws2.Cells[r2, 5].Value  = d.LastName;
                        ws2.Cells[r2, 6].Value  = d.TotalPaid;
                        ws2.Cells[r2, 6].Style.Numberformat.Format = "#,##0";
                        ws2.Cells[r2, 7].Value  = d.SectionCode;
                        ws2.Cells[r2, 8].Value  = d.Rate;
                        ws2.Cells[r2, 9].Value  = d.Tds;
                        ws2.Cells[r2, 9].Style.Numberformat.Format = "#,##0";
                        ws2.Cells[r2, 10].Value = d.DatePaid?.ToString("dd-MM-yyyy");
                        ws2.Cells[r2, 11].Value = challanNums;
                        r2++;
                    }
                    ws2.Cells[ws2.Dimension?.Address ?? "A1"].AutoFitColumns();

                    return ep.GetAsByteArray();
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        // ------------------------------------------------------------------ //
        //  Lapse reminder helpers                                              //
        // ------------------------------------------------------------------ //

        public async Task<IList<UnverifiedWalletInterviewer>> GetReminderTargetsAsync(DateTime asOfDate)
        {
            try
            {
                return await _tdsRepo.FetchUnverifiedWalletInterviewersAsync(asOfDate);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> MarkReminderSentAsync(
            int userid, string paymentReference, int reminderIndex, string emailTo)
        {
            try
            {
                return await _tdsRepo.LogLapseReminderAsync(userid, paymentReference, reminderIndex, emailTo);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public int GetDueReminderIndex(int daysElapsed)
        {
            for (int i = _reminderOffsets.Length - 1; i >= 0; i--)
                if (daysElapsed >= _reminderOffsets[i])
                    return i;
            return -1;
        }
    }
}
