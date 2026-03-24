using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.AzureTimerFunctions
{
    /// <summary>
    /// For QcInterviewAIevaluation rows where QC is not done and both Report AI and Transcript AI JSON exist,
    /// calls the merge assistant and stores a combined JSON array in MergedAIQCDetails.
    /// </summary>
    public class QcInterviewAIMergeJob
    {
        private const string DefaultMergeAssistantId = "asst_VJ5EYhocpfuzhwjwZmMcfblr";

        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IConfiguration _configuration;
        private readonly IAssistantServiceHelper _assistantServiceHelper;
        private readonly IMeetingService _meetingService;
        private readonly ILogger _log;

        public QcInterviewAIMergeJob(
            ILoggerFactory loggerFactory,
            IApplicationErrorLogger applicationErrorLogger,
            IConfiguration configuration,
            IAssistantServiceHelper assistantServiceHelper,
            IMeetingService meetingService)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _configuration = configuration;
            _assistantServiceHelper = assistantServiceHelper;
            _meetingService = meetingService;
            _log = loggerFactory.CreateLogger<QcInterviewAIMergeJob>();
        }

        [Function("QcInterviewAIMergeJob")]
        public async Task Run([TimerTrigger("0 */15 * * * *")] TimerInfo myTimer)
        {
            _log.LogInformation("QcInterviewAIMergeJob started at {Time}", DateTime.Now);

            var assistantId = _configuration["OpenAI:QcInterviewAIMergeAssistantId"];
            if (string.IsNullOrWhiteSpace(assistantId))
                assistantId = DefaultMergeAssistantId;

            try
            {
                var pending = await _meetingService.GetPendingQcInterviewAIMerge();
                if (pending == null || !pending.Any())
                {
                    _log.LogInformation("QcInterviewAIMergeJob: no pending rows.");
                    return;
                }

                foreach (QcInterviewAIMergePendingRow row in pending)
                {
                    try
                    {
                        var userMessage = BuildMergeUserMessage(row.ReportAIDetails, row.TranscritAiDetails);
                        var runResult = await _assistantServiceHelper.RunAssistantWithUserMessage(userMessage, assistantId, useJsonResponseFormat: true);
                        var rawText = runResult?.Data?.FirstOrDefault()?.Content?.FirstOrDefault()?.Text?.Value.Replace("```json", "").Replace("```", "");
                        if (string.IsNullOrWhiteSpace(rawText))
                        {
                            _log.LogWarning("QcInterviewAIMergeJob: empty assistant response for qcEvaluationId={Id} interviewId={InterviewId}", row.id, row.InterviewId);
                            continue;
                        }

                        var items = TryParseMergedItems(rawText);
                        if (items == null || items.Count == 0)
                        {
                            _log.LogWarning("QcInterviewAIMergeJob: could not parse JSON for qcEvaluationId={Id} interviewId={InterviewId}", row.id, row.InterviewId);
                            continue;
                        }

                        var combined = CombineCategoryWise(items);
                        //var mergedJson = JsonConvert.SerializeObject(combined);
                        await _meetingService.SaveQcInterviewAIMergedDetails(row.id, rawText);
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex, "QcInterviewAIMergeJob: error for qcEvaluationId={Id} interviewId={InterviewId}", row.id, row.InterviewId);
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    }
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            _log.LogInformation("QcInterviewAIMergeJob finished at {Time}", DateTime.Now);
        }

        private static string BuildMergeUserMessage(string reportJson, string transcriptJson)
        {
            return "You are given two JSON evaluations for the same interview: Report AI and Transcript AI. " +
                   "Reconcile them and output one JSON array only (no markdown). " +
                   "Each element must have only: category, flag (use consistent category values; flag is the parameter name or code).\n\n" +
                   "Report AI JSON:\n" + (reportJson ?? "") + "\n\n" +
                   "Transcript AI JSON:\n" + (transcriptJson ?? "");
        }

        private static string NormalizeAssistantJson(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;
            var t = raw.Trim();
            if (t.StartsWith("```", StringComparison.Ordinal))
            {
                var firstNl = t.IndexOf('\n');
                if (firstNl >= 0)
                    t = t.Substring(firstNl + 1);
                var end = t.LastIndexOf("```", StringComparison.Ordinal);
                if (end > 0)
                    t = t.Substring(0, end);
            }
            return t.Trim();
        }

        private static List<QcInterviewAIMergedFindingItem> TryParseMergedItems(string rawText)
        {
            var normalized = NormalizeAssistantJson(rawText);
            if (string.IsNullOrEmpty(normalized))
                return null;

            try
            {
                var token = JToken.Parse(normalized);
                if (token.Type == JTokenType.Array)
                    return token.ToObject<List<QcInterviewAIMergedFindingItem>>();

                if (token.Type == JTokenType.Object)
                {
                    foreach (var prop in ((JObject)token).Properties())
                    {
                        if (prop.Value.Type == JTokenType.Array)
                            return prop.Value.ToObject<List<QcInterviewAIMergedFindingItem>>();
                    }
                }
            }
            catch
            {
                // fall through
            }

            try
            {
                return JsonConvert.DeserializeObject<List<QcInterviewAIMergedFindingItem>>(normalized);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Unique category per row: duplicate categories merge all flag tokens (comma/semicolon split) into one list.</summary>
        private static List<QcInterviewAIMergedFindingItem> CombineCategoryWise(List<QcInterviewAIMergedFindingItem> items)
        {
            return items
                .GroupBy(
                    x => (x.category ?? "").Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var categoryDisplay = string.IsNullOrEmpty(g.Key)
                        ? null
                        : (g.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.category))?.category?.Trim() ?? g.Key);

                    var flagTokens = g.SelectMany(x => SplitFlagTokens(x.flag))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    return new QcInterviewAIMergedFindingItem
                    {
                        category = categoryDisplay,
                        flag = flagTokens.Count == 0 ? null : string.Join(", ", flagTokens)
                    };
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.category) || !string.IsNullOrWhiteSpace(x.flag))
                .ToList();
        }

        private static IEnumerable<string> SplitFlagTokens(string flag)
        {
            if (string.IsNullOrWhiteSpace(flag))
                yield break;
            foreach (var part in flag.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var t = part.Trim();
                if (t.Length > 0)
                    yield return t;
            }
        }
    }
}
