using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.Entities.ViewModel;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    /// <summary>
    /// Interview → Profitability tab: revenue, every cost and margin per interview, and the running
    /// costs (cloud, email, licences, ...) that are shared across interviews. Margin data is
    /// commercially sensitive, so it is limited to InterviewProfitability:AllowedProfiles.
    /// </summary>
    [Authorize]
    public class InterviewProfitabilityController : Controller
    {
        private const string DefaultAllowedProfiles = "Admin,Finance";

        private readonly IInterviewProfitabilityService _profitabilityService;
        private readonly IConfiguration _configuration;

        public InterviewProfitabilityController(IInterviewProfitabilityService profitabilityService, IConfiguration configuration)
        {
            _profitabilityService = profitabilityService;
            _configuration = configuration;
        }

        /// <summary>Whether the signed-in profile may see margins; also used by the Interview page to show the tab.</summary>
        public static bool IsAllowed(string profileName, IConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(profileName))
                return false;

            var allowed = (configuration["InterviewProfitability:AllowedProfiles"] ?? DefaultAllowedProfiles)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return allowed.Contains(profileName.Replace("\"", "").Trim(), StringComparer.OrdinalIgnoreCase);
        }

        private string ProfileName => HttpContext.Session.GetString("profileName")?.Replace("\"", "").Trim();
        private string Email => HttpContext.Session.GetString("email")?.Replace("\"", "").Trim();

        private bool Allowed => Email != null && IsAllowed(ProfileName, _configuration);

        [HttpGet]
        public async Task<IActionResult> Index(InterviewProfitabilityFilter filter)
        {
            if (!Allowed)
                return RedirectToAction("SignOut", "Session");

            var report = await _profitabilityService.GetReportAsync(filter);
            ViewBag.profile = ProfileName;
            ViewBag.FilterOptions = await _profitabilityService.GetFilterOptionsAsync(report.Filter.ClientId);
            ViewBag.CostFactors = await _profitabilityService.GetCostFactorsAsync();
            if (TempData["ProfitabilityMessage"] != null)
                ViewBag.Message = TempData["ProfitabilityMessage"].ToString();

            return View(report);
        }

        [HttpGet]
        public async Task<IActionResult> IRFs(int clientId)
        {
            if (!Allowed)
                return Unauthorized();

            var options = await _profitabilityService.GetFilterOptionsAsync(clientId);
            return Json(options.IRFs.Select(i => new { id = i.Id, code = i.Code, title = i.Title }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCostFactor(OperationalCostFactor factor, string returnUrl)
        {
            if (!Allowed)
                return RedirectToAction("SignOut", "Session");

            var error = await _profitabilityService.SaveCostFactorAsync(factor, Email);
            TempData["ProfitabilityMessage"] = error ?? $"Saved \"{factor.Name}\".";

            return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
        }

        /// <summary>The grid as shown (same filters and grouping), one column per cost, as CSV for Excel.</summary>
        [HttpGet]
        public async Task<IActionResult> Export(InterviewProfitabilityFilter filter)
        {
            if (!Allowed)
                return RedirectToAction("SignOut", "Session");

            var report = await _profitabilityService.GetReportAsync(filter);
            if (report.Error != null)
            {
                TempData["ProfitabilityMessage"] = report.Error;
                return RedirectToAction(nameof(Index), filter);
            }

            var csv = new StringBuilder();
            var header = new[]
            {
                "Row", "Date", "Interviews", "Client", "IRF", "Candidate", "Interviewer", "Type", "Status",
                "Billing basis", "Revenue (INR)", "Interviewer payout (INR)", "AI - this interview (INR)",
                "AI - IRF share (INR)", "AI - shared (INR)", "Voice & telephony - this interview (INR)",
                "Voice & telephony - IRF share (INR)"
            }.Concat(report.FactorColumns.Select(f => $"{f.Name} (INR)"))
             .Concat(new[] { "Total cost (INR)", "Margin (INR)", "Margin %", "Note" });
            csv.AppendLine(string.Join(",", header.Select(Csv)));

            foreach (var line in report.Lines.Append(report.Totals))
            {
                var cells = new[]
                {
                    line.Label, line.InterviewDate?.ToString("yyyy-MM-dd"), line.Interviews.ToString(),
                    line.ClientName, line.IRFCode, line.CandidateEmail, line.InterviewerName, line.InterviewType,
                    line.Status, line.BillingBasis, Money(line.Revenue), Money(line.InterviewerPayout),
                    Money(line.AIDirect), Money(line.AIIrfShare), Money(line.AISharedShare),
                    Money(line.VoiceDirect), Money(line.VoiceIrfShare)
                }.Concat(report.FactorColumns.Select(f => Money(line.Factors.TryGetValue(f.Id, out var v) ? v : 0)))
                 .Concat(new[]
                 {
                     Money(line.TotalCost), Money(line.Margin),
                     line.MarginPercent?.ToString("0.0", CultureInfo.InvariantCulture), line.Note
                 });
                csv.AppendLine(string.Join(",", cells.Select(Csv)));
            }

            // The BOM makes Excel open the file as UTF-8, so rupee-adjacent names and en dashes survive.
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            return File(bytes, "text/csv",
                $"interview-profitability-{report.StartDate:yyyyMMdd}-{report.EndDate:yyyyMMdd}.csv");
        }

        private static string Money(decimal value) => Math.Round(value, 2).ToString("0.00", CultureInfo.InvariantCulture);

        private static string Csv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            // A leading =, +, - or @ makes Excel evaluate the cell as a formula.
            if ("=+@".IndexOf(value[0]) >= 0 || (value[0] == '-' && !decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _)))
                value = "'" + value;

            return value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
        }
    }
}
