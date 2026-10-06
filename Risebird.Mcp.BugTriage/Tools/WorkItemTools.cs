using System.ComponentModel;
using ModelContextProtocol.Server;
using Risebird.Mcp.BugTriage.Services;

namespace Risebird.Mcp.BugTriage.Tools;

/// <summary>Read-only Azure DevOps work item access.</summary>
[McpServerToolType]
public static class WorkItemTools
{
    [McpServerTool(Name = "get_work_item")]
    [Description(
        "Fetch one Azure DevOps work item with all fields and its discussion comments. Use this when " +
        "the user references a bug by number. Repro steps and comments usually contain the stack trace " +
        "or URL you need for resolve_stack_trace and the telemetry tools.")]
    public static async Task<string> GetWorkItemAsync(
        AzureDevOpsService azureDevOps,
        [Description("The work item id.")] int id,
        [Description("Set false to skip the discussion comments.")] bool includeComments = true,
        CancellationToken cancellationToken = default)
    {
        var workItem = await azureDevOps.GetWorkItemAsync(id, cancellationToken);

        if (!includeComments)
        {
            return AppInsightsService.ToJson(new { workItem });
        }

        var comments = await azureDevOps.GetWorkItemCommentsAsync(id, cancellationToken);
        return AppInsightsService.ToJson(new { workItem, comments });
    }

    [McpServerTool(Name = "search_work_items")]
    [Description(
        "Search Azure DevOps work items with WIQL. Pass a full WIQL SELECT statement, or omit it and " +
        "use the filter arguments to build one over the configured project. Read-only: only SELECT " +
        "statements are accepted.")]
    public static async Task<string> SearchWorkItemsAsync(
        AzureDevOpsService azureDevOps,
        [Description("Full WIQL query. When set, the filter arguments below are ignored.")]
        string? wiql = null,
        [Description("Work item type filter, e.g. 'Bug' or 'Task'. Defaults to Bug.")]
        string workItemType = "Bug",
        [Description("State filter, e.g. 'Active', 'New', 'Resolved'. Omit for any state.")]
        string? state = null,
        [Description("Case-insensitive substring to match in the title.")]
        string? titleContains = null,
        [Description("Only items changed in the last N days. Omit for no time bound.")]
        int? changedWithinDays = null,
        [Description("Maximum items to return.")]
        int maxResults = 25,
        CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(maxResults, 1, 100);
        var query = wiql;

        if (string.IsNullOrWhiteSpace(query))
        {
            var clauses = new List<string> { $"[System.WorkItemType] = '{Escape(workItemType)}'" };

            if (!string.IsNullOrWhiteSpace(state))
            {
                clauses.Add($"[System.State] = '{Escape(state)}'");
            }

            if (!string.IsNullOrWhiteSpace(titleContains))
            {
                clauses.Add($"[System.Title] CONTAINS '{Escape(titleContains)}'");
            }

            if (changedWithinDays is > 0)
            {
                clauses.Add($"[System.ChangedDate] >= @Today - {changedWithinDays}");
            }

            query = "SELECT [System.Id] FROM WorkItems WHERE " + string.Join(" AND ", clauses) +
                    " ORDER BY [System.ChangedDate] DESC";
        }
        else if (!query.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            return "Only SELECT WIQL statements are accepted - this server is read-only.";
        }

        var results = await azureDevOps.QueryWorkItemsAsync(query, limit, cancellationToken);
        return AppInsightsService.ToJson(new { wiql = query, results });
    }

    /// <summary>WIQL string literals are single-quoted; a literal quote is escaped by doubling it.</summary>
    private static string Escape(string value) => value.Replace("'", "''", StringComparison.Ordinal);
}
