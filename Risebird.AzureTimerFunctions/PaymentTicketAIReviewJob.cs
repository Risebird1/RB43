using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.AzureTimerFunctions;

/// <summary>
/// Timer function that runs every 30 minutes and reviews every open
/// Payment-type raised ticket using the AI assistant
/// asst_kjUOaWfVxQOymoAN1m29MrR8.
///
/// Input to the assistant (per ticket):
///   – Raw transcript text  (downloaded from RawTranscriptUrl)
///   – Transcript AI analysis blob
///   – Contradiction AI analysis blob
///   – Report AI analysis blob
///   – MergedAIQCDetails  (full payment deduction breakdown JSON)
///   – Interview date / Interviewer join datetime
///   – Interviewer's own rebuttal comment
///
/// The assistant returns a rich JSON verdict (PaymentTicketAIVerdict).
///
/// "reduce" / "approve" (decision_in_favor_of_interviewer=true)
///   → QCPenalty updated in DB  →  ticket closed  →  AI response saved.
/// "reject" (decision_in_favor_of_interviewer=false or final_decision="reject")
///   →  ticket stays open       →  AI response saved  →  history note added.
/// </summary>
public class PaymentTicketAIReviewJob
{
    private readonly ILogger _logger;
    private readonly IApplicationErrorLogger _applicationErrorLogger;
    private readonly ISupportService _supportService;
    private readonly IAssistantServiceHelper _assistantServiceHelper;

    private readonly IHttpClientFactory _httpClientFactory;

    public PaymentTicketAIReviewJob(
        ILoggerFactory loggerFactory,
        IApplicationErrorLogger applicationErrorLogger,
        ISupportService supportService,
        IAssistantServiceHelper assistantServiceHelper, IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _logger = loggerFactory.CreateLogger<PaymentTicketAIReviewJob>();
        _applicationErrorLogger = applicationErrorLogger;
        _supportService = supportService;
        _assistantServiceHelper = assistantServiceHelper;
    }

    [Function("PaymentTicketAIReviewJob")]
    public async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("PaymentTicketAIReviewJob started at {time}", DateTime.Now);

        try
        {
            var tickets = await _supportService.GetOpenPaymentTicketsForReview();
            if (tickets == null || !tickets.Any())
            {
                _logger.LogInformation("PaymentTicketAIReviewJob: no open Payment tickets found.");
                return;
            }

            foreach (var ticket in tickets)
            {
                try
                {
                    _logger.LogInformation(
                        "PaymentTicketAIReviewJob: processing TicketId={TicketId}, InterviewId={InterviewId}",
                        ticket.TicketId, ticket.interviewId);

                    // ── 1. Download raw transcript ─────────────────────────────
                    string rawTranscript = string.Empty;
                    if (!string.IsNullOrWhiteSpace(ticket.RawTranscriptUrl))
                    {
                        try
                        {
                            using var http = _httpClientFactory.CreateClient();
                            rawTranscript = await http.GetStringAsync(ticket.RawTranscriptUrl);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(
                                "PaymentTicketAIReviewJob: could not download transcript for TicketId={TicketId}: {err}",
                                ticket.TicketId, ex.Message);
                        }
                    }

                    // ── 2. Compose the message for the AI assistant ────────────
                    string composedMessage = BuildAssistantMessage(ticket, rawTranscript);

                    // ── 3. Call AI assistant ───────────────────────────────────
                    var aiResult = await _assistantServiceHelper.GeneratePaymentTicketReview(composedMessage);
                    if (aiResult?.Data == null || !aiResult.Data.Any())
                    {
                        _logger.LogWarning(
                            "PaymentTicketAIReviewJob: AI returned no data for TicketId={TicketId}",
                            ticket.TicketId);
                        continue;
                    }

                    string aiRawJson = aiResult.Data.FirstOrDefault()?.Content?.FirstOrDefault()?.Text?.Value?.Replace("```json", "").Replace("```", "");

                    _logger.LogInformation(
                        "PaymentTicketAIReviewJob: AI response for TicketId={TicketId}: {response}",
                        ticket.TicketId, aiRawJson);

                    // ── 4. Parse rich verdict ──────────────────────────────────
                    PaymentTicketAIVerdict verdict = null;
                    try
                    {
                        var cleaned = aiRawJson;
                        verdict = JsonConvert.DeserializeObject<PaymentTicketAIVerdict>(cleaned);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            "PaymentTicketAIReviewJob: could not parse verdict JSON for TicketId={TicketId}: {err}",
                            ticket.TicketId, ex.Message);
                        continue;
                    }

                    if (verdict?.DecisionSummary == null)
                        continue;

                    string statusComment = verdict.BuildStatusComment();
                    var now = DateTime.Now;

                    // ── 5a. Update QC Penalty when AI reduced or approved ──────
                    if (verdict.ShouldUpdateQCPenalty && verdict.RevisedPenaltyAmount != null)
                    {
                        bool qcUpdated = await _supportService.UpdateQCPenaltyFromAIReview(
                            interviewId: ticket.interviewId,
                            revisedPenaltyPct: verdict.RevisedPenaltyAmount.RevisedPercentage,
                            updatedMergedDetails: aiRawJson,
                            reasoningComment: verdict.UpdatedReasoningComment ?? statusComment);

                        _logger.LogInformation(
                            "PaymentTicketAIReviewJob: QC penalty updated for InterviewId={InterviewId} " +
                            "({old}% → {new}%). Success={ok}",
                            ticket.interviewId,
                            verdict.RevisedPenaltyAmount.OriginalPercentage,
                            verdict.RevisedPenaltyAmount.RevisedPercentage,
                            qcUpdated);
                    }

                    // ── 5b. Save AI response JSON and always close the ticket ──
                    bool saved = await _supportService.SaveRaisedIssueAIResponse(
                        ticketId:       ticket.TicketId,
                        aiResponseJson: aiRawJson,
                        statusComment:  statusComment,
                        closedBy:       "AI-PaymentTicketReview",
                        closedOn:       now,
                        closeTicket:    true);   // always close regardless of verdict

                    _logger.LogInformation(
                        "PaymentTicketAIReviewJob: TicketId={TicketId} closed (decision={decision}, " +
                        "inFavor={favor}). AI response saved. Success={ok}",
                        ticket.TicketId,
                        verdict.DecisionSummary.FinalDecision,
                        verdict.DecisionSummary.DecisionInFavorOfInterviewer,
                        saved);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        "PaymentTicketAIReviewJob: error processing TicketId={TicketId}: {err}",
                        ticket.TicketId, ex.Message);
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                }
            }
        }
        catch (Exception ex)
        {
            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        }

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("PaymentTicketAIReviewJob next run at: {next}",
                myTimer.ScheduleStatus.Next);
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Helper: compose the prompt sent to the AI assistant
    // ────────────────────────────────────────────────────────────────────────

    private static string BuildAssistantMessage(PaymentTicketReviewVM ticket, string rawTranscript)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are reviewing a payment penalty revert request raised by an interviewer on the Risebird platform.");
        sb.AppendLine("Based on all the evidence below, evaluate the red flags and decide whether the penalty should be reduced, fully approved for revert, or rejected.");
        sb.AppendLine("Return ONLY valid JSON matching the schema described. Include all fields: metadata, decision_summary, updated_red_flag_table, revised_penalty_amount, ai_agent_problem_analysis, updated_reasoning_comment.");
        sb.AppendLine();

        sb.AppendLine("=== TICKET INFO ===");
        sb.AppendLine($"Ticket ID               : {ticket.TicketId}");
        sb.AppendLine($"Interview ID            : {ticket.interviewId}");
        sb.AppendLine($"Interview Date          : {ticket.interviewDate:dd MMM yyyy HH:mm}");
        sb.AppendLine($"Interviewer email ID    : {ticket.Interviewer}");
        sb.AppendLine($"Interviewer Join DateTime: {(ticket.InterviewerJoinDateTime.HasValue ? ticket.InterviewerJoinDateTime.Value.ToString("dd MMM yyyy HH:mm") : "N/A")}");
        sb.AppendLine();

        sb.AppendLine("=== INTERVIEWER REBUTTAL ===");
        sb.AppendLine(ticket.InterviewerComment);
        sb.AppendLine();

        sb.AppendLine("=== RAW TRANSCRIPT ===");
        sb.AppendLine(rawTranscript);
        sb.AppendLine();

        sb.AppendLine("=== TRANSCRIPT AI ANALYSIS ===");
        sb.AppendLine(ticket.TranscriptAI);
        sb.AppendLine();

        sb.AppendLine("=== CONTRADICTION AI ANALYSIS ===");
        sb.AppendLine(ticket.ContradictionAI);
        sb.AppendLine();

        sb.AppendLine("=== REPORT AI ANALYSIS ===");
        sb.AppendLine(ticket.ReportAI);
        sb.AppendLine();

        sb.AppendLine("=== PAYMENT DEDUCTION DETAILS (MergedAIQCDetails) ===");
        sb.AppendLine(ticket.MergedAIQCDetails);

        return sb.ToString();
    }
}
