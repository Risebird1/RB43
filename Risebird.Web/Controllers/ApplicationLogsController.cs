using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class ApplicationLogsController : Controller
    {
        private const string LogTableName = "Risebirdlogs";
        private const int MaxRangeDays = 7;
        private const int MaxLookbackDays = 30;
        private const int DetailPageSize = 100;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

        private static readonly IList<string> DetailSelectColumns = new List<string>
        {
            "LogeLevel", "UserEmailId", "ClassName", "MethodName", "LineNumber", "LogMessage", "InnerException"
        };

        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IApplicationLogDailyCountService _dailyCountService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IMemoryCache _memoryCache;

        public ApplicationLogsController(
            IAzureStorageHelper azureStorageHelper,
            IApplicationLogDailyCountService dailyCountService,
            IApplicationErrorLogger applicationErrorLogger,
            IMemoryCache memoryCache)
        {
            _azureStorageHelper = azureStorageHelper;
            _dailyCountService = dailyCountService;
            _applicationErrorLogger = applicationErrorLogger;
            _memoryCache = memoryCache;
        }

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                    return RedirectToAction("SignOut", "Session");

                var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
                if (profileName != "Admin")
                    return RedirectToAction("SignOut", "Session");

                ViewBag.Profile = profileName;
                ViewBag.UserProfile = profileName;

                var todayUtc = DateTime.UtcNow.Date;
                var earliestAllowed = todayUtc.AddDays(-(MaxLookbackDays - 1));
                var errors = new List<string>();

                var endUtc = (endDate ?? todayUtc).Date;
                var startUtc = (startDate ?? endUtc.AddDays(-(MaxRangeDays - 1))).Date;

                if (endUtc > todayUtc)
                {
                    endUtc = todayUtc;
                    errors.Add("Future dates are not allowed.");
                }
                if (startUtc > todayUtc)
                {
                    startUtc = todayUtc;
                    errors.Add("Future dates are not allowed.");
                }

                if (startUtc < earliestAllowed)
                {
                    startUtc = earliestAllowed;
                    errors.Add($"Data older than {MaxLookbackDays} days is not available.");
                }
                if (endUtc < earliestAllowed)
                {
                    endUtc = earliestAllowed;
                    errors.Add($"Data older than {MaxLookbackDays} days is not available.");
                }

                if (startUtc > endUtc)
                {
                    startUtc = endUtc;
                    errors.Add("Start date must be less than or equal to end date.");
                }

                if ((endUtc - startUtc).TotalDays > MaxRangeDays - 1)
                {
                    startUtc = endUtc.AddDays(-(MaxRangeDays - 1));
                    if (startUtc < earliestAllowed)
                        startUtc = earliestAllowed;
                    errors.Add($"Date range is limited to {MaxRangeDays} days.");
                }

                ViewBag.StartDate = startUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                ViewBag.EndDate = endUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                ViewBag.MinAllowedDate = earliestAllowed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                ViewBag.MaxAllowedDate = todayUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                ViewBag.MaxRangeDays = MaxRangeDays;
                ViewBag.MaxLookbackDays = MaxLookbackDays;
                ViewBag.PageHeader = "Application Logs";
                if (errors.Count > 0)
                    ViewBag.DateRangeError = string.Join(" ", errors.Distinct());

                var cacheKey = $"applogs:index:{startUtc:yyyy-MM-dd}:{endUtc:yyyy-MM-dd}";
                if (!_memoryCache.TryGetValue(cacheKey, out IList<ApplicationLogDaySummaryVM> summaries))
                {
                    summaries = await _dailyCountService.GetSummariesAsync(startUtc, endUtc);
                    _memoryCache.Set(cacheKey, summaries, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = CacheDuration
                    });
                }

                return View(summaries);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(string logDate, int? logLevel = null, int page = 1)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                    return RedirectToAction("SignOut", "Session");

                var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
                if (profileName != "Admin")
                    return RedirectToAction("SignOut", "Session");

                ViewBag.Profile = profileName;
                ViewBag.UserProfile = profileName;

                if (!TryParseUtcDate(logDate, out var dayUtc))
                {
                    TempData["LogsError"] = "Invalid date. Please pick a day from the list.";
                    return RedirectToAction("Index");
                }

                var todayUtc = DateTime.UtcNow.Date;
                var earliestAllowed = todayUtc.AddDays(-(MaxLookbackDays - 1));
                if (dayUtc > todayUtc || dayUtc < earliestAllowed)
                {
                    TempData["LogsError"] = $"Logs are available for the last {MaxLookbackDays} days only (no future dates).";
                    return RedirectToAction("Index");
                }

                if (page < 1)
                    page = 1;

                var selectedLevel = logLevel ?? (int)LogLevelInfo.Exception;
                ViewBag.PageHeader = $"Logs — {dayUtc:dd MMM yyyy} (UTC)";

                var cache = await GetOrLoadDayLevelCacheAsync(dayUtc, selectedLevel);

                var totalGroups = cache.Groups.Count;
                var pagedGroups = cache.Groups
                    .Skip((page - 1) * DetailPageSize)
                    .Take(DetailPageSize)
                    .ToList();

                var model = new ApplicationLogDetailsPageVM
                {
                    LogDate = dayUtc,
                    LogLevel = selectedLevel,
                    Groups = pagedGroups,
                    LevelCounts = cache.LevelCounts,
                    Page = page,
                    PageSize = DetailPageSize,
                    TotalGroups = totalGroups,
                    TotalOccurrences = cache.TotalOccurrences,
                    HasNext = (page * DetailPageSize) < totalGroups
                };

                return View(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["LogsError"] = "Unable to load log details for that date. Please try again.";
                return RedirectToAction("Index");
            }
        }

        private async Task<CachedDayLevelLogs> GetOrLoadDayLevelCacheAsync(DateTime dayUtc, int selectedLevel)
        {
            var cacheKey = $"applogs:details:{dayUtc:yyyy-MM-dd}:{selectedLevel}";
            if (_memoryCache.TryGetValue(cacheKey, out CachedDayLevelLogs cached) && cached != null)
                return cached;

            var startUtc = DateTime.SpecifyKind(dayUtc, DateTimeKind.Utc);
            var endUtcExclusive = startUtc.AddDays(1);

            var rowsTask = _azureStorageHelper.GetLogEntitiesByDateRangeAsync<LogData>(
                LogTableName, startUtc, endUtcExclusive, selectedLevel, DetailSelectColumns);
            var summaryTask = GetOrLoadDaySummaryAsync(dayUtc);

            await Task.WhenAll(rowsTask, summaryTask);

            var rows = rowsTask.Result;
            var groups = GroupSimilarLogs(rows);
            var summary = summaryTask.Result;
            var levelCounts = new Dictionary<int, int>
            {
                [(int)LogLevelInfo.Exception] = summary?.ExceptionCount ?? 0,
                [(int)LogLevelInfo.Error] = summary?.ErrorCount ?? 0,
                [(int)LogLevelInfo.Warning] = summary?.WarningCount ?? 0
            };
            levelCounts[selectedLevel] = rows.Count;

            cached = new CachedDayLevelLogs
            {
                Groups = groups,
                TotalOccurrences = rows.Count,
                LevelCounts = levelCounts
            };

            _memoryCache.Set(cacheKey, cached, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheDuration
            });

            return cached;
        }

        private async Task<ApplicationLogDaySummaryVM> GetOrLoadDaySummaryAsync(DateTime dayUtc)
        {
            var cacheKey = $"applogs:summary:{dayUtc:yyyy-MM-dd}";
            if (_memoryCache.TryGetValue(cacheKey, out ApplicationLogDaySummaryVM summary) && summary != null)
                return summary;

            var list = await _dailyCountService.GetSummariesAsync(dayUtc, dayUtc);
            summary = list?.FirstOrDefault() ?? new ApplicationLogDaySummaryVM { LogDate = dayUtc };

            _memoryCache.Set(cacheKey, summary, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheDuration
            });

            return summary;
        }

        private static IList<ApplicationLogGroupVM> GroupSimilarLogs(IList<LogData> rows)
        {
            if (rows == null || rows.Count == 0)
                return new List<ApplicationLogGroupVM>();

            return rows
                .GroupBy(r => BuildGroupKey(r))
                .Select(g =>
                {
                    var latest = g.OrderByDescending(x => x.Timestamp).First();
                    return new ApplicationLogGroupVM
                    {
                        LatestTimestamp = latest.Timestamp,
                        LogeLevel = latest.LogeLevel,
                        UserEmailId = latest.UserEmailId,
                        ClassName = latest.ClassName,
                        MethodName = latest.MethodName,
                        LineNumber = latest.LineNumber,
                        LogMessage = latest.LogMessage,
                        InnerException = latest.InnerException,
                        Count = g.Count()
                    };
                })
                .OrderByDescending(g => g.Count)
                .ThenByDescending(g => g.LatestTimestamp)
                .ToList();
        }

        private static string BuildGroupKey(LogData row)
        {
            var inner = string.IsNullOrWhiteSpace(row.InnerException) || row.InnerException == "NA"
                ? string.Empty
                : row.InnerException.Trim();

            return string.Join("\u001f",
                row.LogeLevel.ToString(CultureInfo.InvariantCulture),
                (row.ClassName ?? string.Empty).Trim(),
                (row.MethodName ?? string.Empty).Trim(),
                (row.LogMessage ?? string.Empty).Trim(),
                inner);
        }

        private static bool TryParseUtcDate(string value, out DateTime dayUtc)
        {
            dayUtc = default;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (DateTime.TryParseExact(value.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var parsed))
            {
                dayUtc = parsed.Date;
                return true;
            }

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out parsed))
            {
                dayUtc = parsed.Date;
                return true;
            }

            return false;
        }

        private sealed class CachedDayLevelLogs
        {
            public IList<ApplicationLogGroupVM> Groups { get; set; } = new List<ApplicationLogGroupVM>();
            public int TotalOccurrences { get; set; }
            public Dictionary<int, int> LevelCounts { get; set; } = new Dictionary<int, int>();
        }
    }
}
