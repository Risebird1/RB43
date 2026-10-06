using System.ComponentModel;
using ModelContextProtocol.Server;
using Risebird.Mcp.BugTriage.Services;

namespace Risebird.Mcp.BugTriage.Tools;

/// <summary>
/// Read-only Application Insights queries. These are the entry point for "something is broken in
/// production" triage: find the exception, then pivot to the operation and the source file.
/// </summary>
[McpServerToolType]
public static class TelemetryTools
{
    [McpServerTool(Name = "query_exceptions")]
    [Description(
        "List the most frequent exceptions from Application Insights, grouped by type and message. " +
        "Start here when triaging a production error with no other detail. Returns a problemId and a " +
        "sample operationId for each group, which get_exception_detail and get_operation_timeline consume.")]
    public static async Task<string> QueryExceptionsAsync(
        AppInsightsService appInsights,
        [Description("How many hours to look back. Defaults to the configured lookback (24h).")]
        int? lookbackHours = null,
        [Description("Optional cloud role name filter, e.g. 'Risebird.Web' or 'Risebird.MicroService'.")]
        string? roleName = null,
        [Description("Optional case-insensitive substring filter on the exception type or message.")]
        string? contains = null,
        [Description("Maximum exception groups to return. Capped by the server's MaxRows setting.")]
        int maxResults = 20,
        CancellationToken cancellationToken = default)
    {
        var hours = lookbackHours ?? appInsights.DefaultLookbackHours;
        var limit = Math.Clamp(maxResults, 1, appInsights.MaxRows);

        var filters = string.Empty;
        if (!string.IsNullOrWhiteSpace(roleName))
        {
            filters += $"\n| where cloud_RoleName =~ \"{AppInsightsService.EscapeKqlString(roleName)}\"";
        }

        if (!string.IsNullOrWhiteSpace(contains))
        {
            var needle = AppInsightsService.EscapeKqlString(contains);
            filters += $"\n| where type contains \"{needle}\" or outerMessage contains \"{needle}\"";
        }

        var kql = $"""
            exceptions
            | where timestamp > ago({hours}h){filters}
            | summarize
                occurrences = count(),
                firstSeen = min(timestamp),
                lastSeen = max(timestamp),
                affectedUsers = dcount(user_Id),
                sampleOperationId = any(operation_Id),
                operations = make_set(operation_Name, 5)
                by type, problemId, cloud_RoleName, outerMessage
            | top {limit} by occurrences desc
            """;

        var rows = await appInsights.RunQueryAsync(kql, TimeSpan.FromHours(hours), cancellationToken);
        return Render(rows, kql, $"exceptions in the last {hours}h");
    }

    [McpServerTool(Name = "get_exception_detail")]
    [Description(
        "Full detail for one exception group: innermost message, the parsed stack trace, the failing " +
        "operation and the custom dimensions. Feed the returned stack trace into resolve_stack_trace " +
        "to get the actual source files.")]
    public static async Task<string> GetExceptionDetailAsync(
        AppInsightsService appInsights,
        [Description("The problemId from query_exceptions. Either this or operationId is required.")]
        string? problemId = null,
        [Description("An operation_Id to pull the exception for. Either this or problemId is required.")]
        string? operationId = null,
        [Description("How many hours to look back. Defaults to the configured lookback (24h).")]
        int? lookbackHours = null,
        [Description("How many sample occurrences to return.")]
        int samples = 3,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(problemId) && string.IsNullOrWhiteSpace(operationId))
        {
            return "Provide either problemId (from query_exceptions) or operationId.";
        }

        var hours = lookbackHours ?? appInsights.DefaultLookbackHours;
        var limit = Math.Clamp(samples, 1, 10);

        var predicate = !string.IsNullOrWhiteSpace(problemId)
            ? $"problemId == \"{AppInsightsService.EscapeKqlString(problemId)}\""
            : $"operation_Id == \"{AppInsightsService.EscapeKqlString(operationId!)}\"";

        var kql = $"""
            exceptions
            | where timestamp > ago({hours}h)
            | where {predicate}
            | top {limit} by timestamp desc
            | project
                timestamp,
                type,
                method,
                assembly,
                outerMessage,
                innermostMessage,
                cloud_RoleName,
                operation_Id,
                operation_Name,
                user_Id,
                client_IP,
                severityLevel,
                stack = tostring(details),
                dimensions = tostring(customDimensions)
            """;

        var rows = await appInsights.RunQueryAsync(kql, TimeSpan.FromHours(hours), cancellationToken);
        return Render(rows, kql, "exception detail");
    }

    [McpServerTool(Name = "get_operation_timeline")]
    [Description(
        "Every telemetry item for a single operation_Id in time order - request, dependencies, traces " +
        "and exceptions. This is the best way to see what happened immediately before a failure " +
        "(the slow SQL call, the 500 from a downstream service, the log line with the bad input).")]
    public static async Task<string> GetOperationTimelineAsync(
        AppInsightsService appInsights,
        [Description("The operation_Id correlating one end-to-end request.")]
        string operationId,
        [Description("How many hours to look back. Defaults to the configured lookback (24h).")]
        int? lookbackHours = null,
        CancellationToken cancellationToken = default)
    {
        var hours = lookbackHours ?? appInsights.DefaultLookbackHours;
        var id = AppInsightsService.EscapeKqlString(operationId);

        var kql = $"""
            let op = "{id}";
            let window = ago({hours}h);
            union
                (requests     | where timestamp > window and operation_Id == op
                              | project timestamp, itemType = "request", name, success = tostring(success),
                                        resultCode, duration, detail = tostring(customDimensions)),
                (dependencies | where timestamp > window and operation_Id == op
                              | project timestamp, itemType = "dependency", name, success = tostring(success),
                                        resultCode, duration, detail = strcat(type, " | ", target, " | ", data)),
                (traces       | where timestamp > window and operation_Id == op
                              | project timestamp, itemType = "trace", name = "", success = "",
                                        resultCode = tostring(severityLevel), duration = real(null), detail = message),
                (exceptions   | where timestamp > window and operation_Id == op
                              | project timestamp, itemType = "exception", name = type, success = "false",
                                        resultCode = "", duration = real(null),
                                        detail = strcat(outerMessage, " || ", tostring(details)))
            | sort by timestamp asc
            | take {appInsights.MaxRows}
            """;

        var rows = await appInsights.RunQueryAsync(kql, TimeSpan.FromHours(hours), cancellationToken);
        return Render(rows, kql, $"timeline for operation {operationId}");
    }

    [McpServerTool(Name = "query_failed_requests")]
    [Description(
        "Failed HTTP requests grouped by operation name and result code. Use this when the report is " +
        "'page X is broken' or '500 errors on the portal' rather than a named exception.")]
    public static async Task<string> QueryFailedRequestsAsync(
        AppInsightsService appInsights,
        [Description("How many hours to look back. Defaults to the configured lookback (24h).")]
        int? lookbackHours = null,
        [Description("Optional cloud role name filter, e.g. 'Risebird.Web'.")]
        string? roleName = null,
        [Description("Optional case-insensitive substring filter on the operation name or URL.")]
        string? contains = null,
        [Description("Maximum groups to return.")]
        int maxResults = 20,
        CancellationToken cancellationToken = default)
    {
        var hours = lookbackHours ?? appInsights.DefaultLookbackHours;
        var limit = Math.Clamp(maxResults, 1, appInsights.MaxRows);

        var filters = string.Empty;
        if (!string.IsNullOrWhiteSpace(roleName))
        {
            filters += $"\n| where cloud_RoleName =~ \"{AppInsightsService.EscapeKqlString(roleName)}\"";
        }

        if (!string.IsNullOrWhiteSpace(contains))
        {
            var needle = AppInsightsService.EscapeKqlString(contains);
            filters += $"\n| where name contains \"{needle}\" or url contains \"{needle}\"";
        }

        var kql = $"""
            requests
            | where timestamp > ago({hours}h)
            | where success == false{filters}
            | summarize
                failures = count(),
                firstSeen = min(timestamp),
                lastSeen = max(timestamp),
                affectedUsers = dcount(user_Id),
                p95DurationMs = percentile(duration, 95),
                sampleOperationId = any(operation_Id),
                sampleUrl = any(url)
                by name, resultCode, cloud_RoleName
            | top {limit} by failures desc
            """;

        var rows = await appInsights.RunQueryAsync(kql, TimeSpan.FromHours(hours), cancellationToken);
        return Render(rows, kql, $"failed requests in the last {hours}h");
    }

    [McpServerTool(Name = "run_kql")]
    [Description(
        "Escape hatch: run an arbitrary read-only KQL query against Application Insights when the " +
        "purpose-built tools do not fit. Operators that write to or read from outside the workspace " +
        "(externaldata, http_request, .ingest, .set, .append, .alter, .drop, .create) are rejected. " +
        "Always add an explicit 'take' or 'top' - results are truncated at the server's MaxRows.")]
    public static async Task<string> RunKqlAsync(
        AppInsightsService appInsights,
        [Description("The KQL query. Tables available: requests, exceptions, traces, dependencies, customEvents, pageViews, availabilityResults.")]
        string query,
        [Description("How many hours the query's time range covers. Defaults to the configured lookback (24h).")]
        int? lookbackHours = null,
        CancellationToken cancellationToken = default)
    {
        var hours = lookbackHours ?? appInsights.DefaultLookbackHours;
        var rows = await appInsights.RunQueryAsync(query, TimeSpan.FromHours(hours), cancellationToken);
        return Render(rows, query, "custom KQL query");
    }

    private static string Render(
        IReadOnlyList<Dictionary<string, object?>> rows,
        string kql,
        string description)
    {
        if (rows.Count == 0)
        {
            return $"No results for {description}.\n\nQuery executed:\n{kql}";
        }

        return $"{rows.Count} row(s) for {description}.\n\n{AppInsightsService.ToJson(rows)}\n\nQuery executed:\n{kql}";
    }
}
