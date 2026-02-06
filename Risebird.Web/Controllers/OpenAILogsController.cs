using Microsoft.AspNetCore.Mvc;
using Risebird.Repository.Interface;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    public class OpenAILogsController : Controller
    {
        private readonly IOpenAIApiLogRepository _openAIApiLogRepository;

        public OpenAILogsController(IOpenAIApiLogRepository openAIApiLogRepository)
        {
            _openAIApiLogRepository = openAIApiLogRepository;
        }

        public async Task<IActionResult> Index(DateTime? startDate = null, DateTime? endDate = null, int pageNumber = 1, int pageSize = 50, string sortBy = "LogDate", string sortOrder = "desc")
        {
            // Reduced default range from 30 days to 7 days for better performance
            var start = startDate ?? DateTime.UtcNow.AddDays(-7);
            var end = endDate ?? DateTime.UtcNow;

            // Validate page size (min 10, max 200)
            pageSize = Math.Max(10, Math.Min(200, pageSize));
            pageNumber = Math.Max(1, pageNumber);

            ViewBag.StartDate = start.ToString("yyyy-MM-dd");
            ViewBag.EndDate = end.ToString("yyyy-MM-dd");
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;

            // Get summary data
            ViewBag.TotalCost = await _openAIApiLogRepository.GetTotalCostAsync(start, end);
            ViewBag.TotalCalls = await _openAIApiLogRepository.GetTotalCallsAsync(start, end);

            // Get paged logs
            var (logs, totalCount) = await _openAIApiLogRepository.GetLogsByDateRangePagedAsync(start, end, pageNumber, pageSize, sortBy, sortOrder);
            
            // Calculate pagination data
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.HasPreviousPage = pageNumber > 1;
            ViewBag.HasNextPage = pageNumber < ViewBag.TotalPages;
            
            // Success and failed counts from server
            ViewBag.SuccessCount = logs.Count(x => x.IsSuccess);
            ViewBag.FailedCount = logs.Count(x => !x.IsSuccess);

            return View(logs);
        }

        public async Task<IActionResult> Summary(DateTime? startDate = null, DateTime? endDate = null)
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            ViewBag.StartDate = start.ToString("yyyy-MM-dd");
            ViewBag.EndDate = end.ToString("yyyy-MM-dd");

            var dailySummary = await _openAIApiLogRepository.GetDailySummaryAsync(start, end);
            var operationTypeSummary = await _openAIApiLogRepository.GetSummaryByOperationTypeAsync(start, end);
            var modelSummary = await _openAIApiLogRepository.GetSummaryByModelAsync(start, end);

            ViewBag.DailySummary = dailySummary;
            ViewBag.OperationTypeSummary = operationTypeSummary;
            ViewBag.ModelSummary = modelSummary;
            ViewBag.TotalCost = await _openAIApiLogRepository.GetTotalCostAsync(start, end);
            ViewBag.TotalCalls = await _openAIApiLogRepository.GetTotalCallsAsync(start, end);

            return View();
        }

        public async Task<IActionResult> ByOperationType(string operationType, DateTime? startDate = null, DateTime? endDate = null)
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            ViewBag.StartDate = start.ToString("yyyy-MM-dd");
            ViewBag.EndDate = end.ToString("yyyy-MM-dd");
            ViewBag.OperationType = operationType;

            var logs = await _openAIApiLogRepository.GetLogsByOperationTypeAsync(operationType, start, end);
            return View(logs);
        }

        public async Task<IActionResult> Failed(DateTime? startDate = null, DateTime? endDate = null)
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-7);
            var end = endDate ?? DateTime.UtcNow;

            ViewBag.StartDate = start.ToString("yyyy-MM-dd");
            ViewBag.EndDate = end.ToString("yyyy-MM-dd");

            var logs = await _openAIApiLogRepository.GetFailedCallsAsync(start, end);
            return View(logs);
        }

        public async Task<IActionResult> Details(long id)
        {
            var log = await _openAIApiLogRepository.GetLogByIdAsync(id);
            if (log == null)
            {
                return NotFound();
            }
            return View(log);
        }

        public async Task<IActionResult> BySession(Guid sessionId)
        {
            ViewBag.SessionId = sessionId;
            var logs = await _openAIApiLogRepository.GetLogsBySessionIdAsync(sessionId);
            return View(logs);
        }

        public async Task<IActionResult> ByCandidate(int candidateId)
        {
            ViewBag.CandidateId = candidateId;
            var logs = await _openAIApiLogRepository.GetLogsByCandidateIdAsync(candidateId);
            return View(logs);
        }

        public async Task<IActionResult> ByInterviewer(int interviewerId)
        {
            ViewBag.InterviewerId = interviewerId;
            var logs = await _openAIApiLogRepository.GetLogsByInterviewerIdAsync(interviewerId);
            return View(logs);
        }

        public async Task<IActionResult> ByIRF(int irfId)
        {
            ViewBag.IRFId = irfId;
            var logs = await _openAIApiLogRepository.GetLogsByIRFIdAsync(irfId);
            return View(logs);
        }

        public async Task<IActionResult> ByMeeting(int meetingId)
        {
            ViewBag.MeetingId = meetingId;
            var logs = await _openAIApiLogRepository.GetLogsByMeetingIdAsync(meetingId);
            return View(logs);
        }

        [HttpGet]
        public async Task<JsonResult> GetCostData(DateTime? startDate = null, DateTime? endDate = null)
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var dailySummary = await _openAIApiLogRepository.GetDailySummaryAsync(start, end);
            var operationTypeSummary = await _openAIApiLogRepository.GetSummaryByOperationTypeAsync(start, end);
            var modelSummary = await _openAIApiLogRepository.GetSummaryByModelAsync(start, end);

            return Json(new
            {
                dailySummary,
                operationTypeSummary,
                modelSummary
            });
        }

        // Assistant Cost Tracking Actions
        public async Task<IActionResult> AssistantRanking(int? topN = null, decimal? minCost = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            ViewBag.TopN = topN;
            ViewBag.MinCost = minCost;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            var ranking = await _openAIApiLogRepository.GetAssistantCostRankingAsync(topN, minCost, startDate, endDate);
            return View(ranking);
        }

        public async Task<IActionResult> AssistantDetails(string assistantId, DateTime? startDate = null, DateTime? endDate = null)
        {
            if (string.IsNullOrEmpty(assistantId))
            {
                return RedirectToAction(nameof(AssistantRanking));
            }

            ViewBag.AssistantId = assistantId;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            var details = await _openAIApiLogRepository.GetAssistantCostDetailsAsync(assistantId, startDate, endDate);
            
            if (details == null)
            {
                return NotFound();
            }

            return View(details);
        }

        [HttpPost]
        public async Task<IActionResult> RefreshAssistantSummary()
        {
            try
            {
                await _openAIApiLogRepository.RefreshAssistantCostSummaryAsync();
                TempData["SuccessMessage"] = "Assistant cost summary refreshed successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error refreshing summary: {ex.Message}";
            }

            return RedirectToAction(nameof(AssistantRanking));
        }
    }
}
