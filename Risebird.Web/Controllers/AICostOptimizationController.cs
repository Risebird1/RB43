using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper;
using Risebird.Repository.Interface;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    /// <summary>
    /// Where OpenAI spend goes per operation and model, how much was spent repeating an identical
    /// request, and what OpenAI:CostOptimization is currently doing about it. Read-only: the
    /// optimisations themselves are switched in configuration, so a mistake is undone without a deploy.
    /// </summary>
    [Authorize]
    public class AICostOptimizationController : Controller
    {
        private const string DefaultAllowedProfiles = "Admin,SupportAdmin,Finance";
        private const int MaxRangeDays = 31;

        private readonly IOpenAIApiLogRepository _openAIApiLogRepository;
        private readonly IConfiguration _configuration;

        public AICostOptimizationController(IOpenAIApiLogRepository openAIApiLogRepository, IConfiguration configuration)
        {
            _openAIApiLogRepository = openAIApiLogRepository;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? startDate = null, DateTime? endDate = null)
        {
            var profile = HttpContext.Session.GetString("profileName")?.Replace("\"", "").Trim();
            var allowed = (_configuration["AICostOptimization:AllowedProfiles"] ?? DefaultAllowedProfiles)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (HttpContext.Session.GetString("email") == null || !allowed.Contains(profile, StringComparer.OrdinalIgnoreCase))
                return RedirectToAction("SignOut", "Session");

            var end = (endDate ?? DateTime.Today).Date;
            var start = (startDate ?? end.AddDays(-6)).Date;
            if (start > end)
                (start, end) = (end, start);
            if ((end - start).TotalDays + 1 > MaxRangeDays)
            {
                start = end.AddDays(-(MaxRangeDays - 1));
                ViewBag.Warning = $"The report compares request bodies, so it is limited to {MaxRangeDays} days. Showing {start:dd MMM} – {end:dd MMM yyyy}.";
            }

            var rows = await _openAIApiLogRepository.GetCostOptimizationReportAsync(start, end);
            var settings = new OpenAICostOptimization(_configuration);
            foreach (var row in rows ?? Enumerable.Empty<Risebird.Entities.ViewModel.OpenAICostOptimizationRow>())
            {
                row.ConfiguredModelOverride = settings.ModelOverrideFor(row.OperationType);
                row.ConfiguredCacheTtlMinutes = settings.CacheTtlMinutesFor(row.OperationType);
            }

            ViewBag.StartDate = start;
            ViewBag.EndDate = end;
            ViewBag.Error = rows == null ? "The report could not be loaded. The error has been logged; try a shorter date range." : null;
            ViewBag.OptimizationEnabled = settings.Enabled;
            ViewBag.PromptCacheKey = settings.UsePromptCacheKey;
            ViewBag.Fallback = settings.FallbackToDefaultModelOnFailure;
            ViewBag.ResponseCacheEnabled = settings.Enabled && _configuration.GetValue("OpenAI:CostOptimization:ResponseCache:Enabled", false);
            ViewBag.DefaultModel = _configuration["OpenAI:ResponsesModel"];
            ViewBag.UsdToInr = _configuration.GetValue<decimal?>("InterviewProfitability:UsdToInr") ?? 88m;

            return View(rows ?? new System.Collections.Generic.List<Risebird.Entities.ViewModel.OpenAICostOptimizationRow>());
        }
    }
}
