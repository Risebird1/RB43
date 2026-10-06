using System.ComponentModel;
using System.Text.RegularExpressions;
using ModelContextProtocol.Server;
using Risebird.Mcp.BugTriage.Services;

namespace Risebird.Mcp.BugTriage.Tools;

/// <summary>
/// Turns free-form human bug reports - a comment on a run, a support email, a Teams message - into
/// the structured identifiers the other tools consume. Extraction is deterministic pattern matching,
/// not inference: the agent still does the reasoning, this just saves it from eyeballing the text
/// for GUIDs and file names.
/// </summary>
[McpServerToolType]
public static partial class BugReportTools
{
    [GeneratedRegex(@"\b(?<type>[A-Z][A-Za-z0-9_]*(?:Exception|Error))\b")]
    private static partial Regex ExceptionTypeRegex { get; }

    [GeneratedRegex(@"\b(?<file>[A-Za-z0-9_.]+\.(?:cs|cshtml|js|sql|razor))\b", RegexOptions.IgnoreCase)]
    private static partial Regex FileNameRegex { get; }

    [GeneratedRegex(@"\bhttps?://[^\s<>""')\]]+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex { get; }

    [GeneratedRegex(@"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b")]
    private static partial Regex GuidRegex { get; }

    /// <summary>App Insights operation ids are 32 hex characters with no dashes.</summary>
    [GeneratedRegex(@"\b[0-9a-f]{32}\b")]
    private static partial Regex OperationIdRegex { get; }

    [GeneratedRegex(@"\b(?:status(?:\s*code)?|http|returns?|got|threw)\D{0,10}(?<code>[45]\d{2})\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex StatusCodeRegex { get; }

    [GeneratedRegex(@"(?:^|\s)(?:AB)?#(?<id>\d{1,7})\b")]
    private static partial Regex WorkItemRefRegex { get; }

    /// <summary>Types that are almost certainly ours: the layered suffixes this solution uses.</summary>
    [GeneratedRegex(@"\b(?<type>[A-Z][A-Za-z0-9_]*(?:Controller|Service|Repository|Helper|Manager|Provider|Handler|Job|Function))\b")]
    private static partial Regex ComponentTypeRegex { get; }

    [McpServerTool(Name = "analyze_bug_report")]
    [Description(
        "Extract the actionable identifiers from a free-form human bug report - the kind of text " +
        "someone types into a run comment, a support email, or a Teams message. Returns exception " +
        "types, stack frames, file and component names, URLs, operation ids, GUIDs, HTTP status codes " +
        "and work item references, plus a suggested sequence of follow-up tool calls. Use this first " +
        "when all you have is prose; then follow the suggested calls to get real evidence. " +
        "This tool only pattern-matches the text - it does not diagnose anything on its own.")]
    public static string AnalyzeBugReport(
        [Description("The raw human-written report text.")] string reportText)
    {
        if (string.IsNullOrWhiteSpace(reportText))
        {
            return "Report text was empty.";
        }

        var exceptionTypes = Distinct(ExceptionTypeRegex, reportText, "type");
        var componentTypes = Distinct(ComponentTypeRegex, reportText, "type");
        var fileNames = Distinct(FileNameRegex, reportText, "file");
        var urls = Distinct(UrlRegex, reportText, group: null);
        var guids = Distinct(GuidRegex, reportText, group: null);
        var operationIds = Distinct(OperationIdRegex, reportText, group: null);
        var statusCodes = Distinct(StatusCodeRegex, reportText, "code");
        var workItemRefs = Distinct(WorkItemRefRegex, reportText, "id");

        var frames = RepositoryService.ParseStackTrace(reportText);

        var suggestions = BuildSuggestions(
            exceptionTypes, componentTypes, fileNames, urls, operationIds, statusCodes, workItemRefs, frames.Count);

        return AppInsightsService.ToJson(new
        {
            extracted = new
            {
                exceptionTypes,
                componentTypes,
                fileNames,
                urls,
                operationIds,
                otherGuids = guids.Except(operationIds, StringComparer.OrdinalIgnoreCase).ToList(),
                httpStatusCodes = statusCodes,
                workItemOrIssueReferences = workItemRefs,
                stackFrameCount = frames.Count,
                stackFrames = frames.Take(12).Select(f => new { f.Method, f.BuildFilePath, f.Line }),
            },
            suggestedNextSteps = suggestions,
            caveat = "These are literal pattern matches from the text. Numbers that look like work item " +
                     "ids may be anything; confirm against the source before acting on them.",
        });
    }

    private static List<string> BuildSuggestions(
        List<string> exceptionTypes,
        List<string> componentTypes,
        List<string> fileNames,
        List<string> urls,
        List<string> operationIds,
        List<string> statusCodes,
        List<string> workItemRefs,
        int frameCount)
    {
        var suggestions = new List<string>();

        if (frameCount > 0)
        {
            suggestions.Add(
                "The report contains a stack trace - call resolve_stack_trace with the raw text to get " +
                "the source files, lines and recent commits. Do this first; it is the strongest signal here.");
        }

        foreach (var operationId in operationIds.Take(3))
        {
            suggestions.Add($"Call get_operation_timeline with operationId '{operationId}' to see the full request.");
        }

        foreach (var exceptionType in exceptionTypes.Take(3))
        {
            suggestions.Add(
                $"Call query_exceptions with contains='{exceptionType}' to confirm it is happening in " +
                "production and get a problemId and sample operationId.");
        }

        if (statusCodes.Count > 0 && frameCount == 0)
        {
            suggestions.Add(
                $"HTTP {string.Join("/", statusCodes)} mentioned with no stack trace - call " +
                "query_failed_requests to find the failing operation, then get_operation_timeline on a sample.");
        }

        foreach (var url in urls.Take(2))
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var parsed) && parsed.AbsolutePath.Length > 1)
            {
                suggestions.Add(
                    $"Call query_failed_requests with contains='{parsed.AbsolutePath}' to find failures on that route.");
            }
        }

        foreach (var reference in workItemRefs.Take(3))
        {
            suggestions.Add(
                $"'{reference}' may be an Azure DevOps work item or a GitHub issue - try get_work_item " +
                $"with id={reference} and get_github_issue with number={reference}, and discard whichever misses.");
        }

        foreach (var file in fileNames.Take(3))
        {
            suggestions.Add($"Call get_file_history for '{file}' to see whether it changed recently.");
        }

        foreach (var component in componentTypes.Take(3))
        {
            suggestions.Add(
                $"'{component}' looks like one of our components - search the repository for it, then " +
                "call get_file_history on the file that defines it.");
        }

        if (suggestions.Count == 0)
        {
            suggestions.Add(
                "Nothing machine-actionable was found in this text. Ask the reporter for the exact error " +
                "message, the URL or screen, the approximate time, and the account used - then call " +
                "query_exceptions or query_failed_requests over that time window.");
        }

        return suggestions;
    }

    private static List<string> Distinct(Regex regex, string input, string? group)
    {
        return regex.Matches(input)
            .Select(match => group is null ? match.Value : match.Groups[group].Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(25)
            .ToList();
    }
}
