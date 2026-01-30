using Microsoft.AspNetCore.Mvc;
using Risebird.Repository.Interface;
using System;
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

        public async Task<IActionResult> Index(DateTime? startDate = null, DateTime? endDate = null)
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            ViewBag.StartDate = start.ToString("yyyy-MM-dd");
            ViewBag.EndDate = end.ToString("yyyy-MM-dd");
            ViewBag.TotalCost = await _openAIApiLogRepository.GetTotalCostAsync(start, end);
            ViewBag.TotalCalls = await _openAIApiLogRepository.GetTotalCallsAsync(start, end);

            var logs = await _openAIApiLogRepository.GetLogsByDateRangeAsync(start, end);
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
    }
}
