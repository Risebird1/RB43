using System.ComponentModel;
using ModelContextProtocol.Server;
using Risebird.Mcp.BugTriage.Services;

namespace Risebird.Mcp.BugTriage.Tools;

/// <summary>Read-only GitHub issue and pull request access.</summary>
[McpServerToolType]
public static class GitHubTools
{
    [McpServerTool(Name = "get_github_issue")]
    [Description(
        "Fetch one GitHub issue or pull request with its comment thread. Use this when the user " +
        "references an issue by number. The body and comments usually carry the repro steps and stack " +
        "trace that the telemetry and stack trace tools need.")]
    public static async Task<string> GetIssueAsync(
        GitHubService gitHub,
        [Description("Issue or pull request number.")] int number,
        [Description("Repository as 'owner/name'. Omit to use the configured default repository.")]
        string? repository = null,
        [Description("Set false to skip the comment thread.")] bool includeComments = true,
        CancellationToken cancellationToken = default)
    {
        var repo = gitHub.ResolveRepository(repository);
        var issue = await gitHub.GetIssueAsync(repo, number, cancellationToken);

        if (!includeComments)
        {
            return AppInsightsService.ToJson(new { repository = repo, issue });
        }

        var comments = await gitHub.GetIssueCommentsAsync(repo, number, cancellationToken);
        return AppInsightsService.ToJson(new { repository = repo, issue, comments });
    }

    [McpServerTool(Name = "search_github_issues")]
    [Description(
        "Search GitHub issues and pull requests using GitHub search syntax (for example " +
        "'is:issue is:open label:bug timeout'). A repo: qualifier is added automatically when the " +
        "query does not already carry one. Useful for finding whether a bug was reported before.")]
    public static async Task<string> SearchIssuesAsync(
        GitHubService gitHub,
        [Description("GitHub search query, without the repo: qualifier unless you want to override it.")]
        string query,
        [Description("Repository as 'owner/name'. Omit to use the configured default repository.")]
        string? repository = null,
        [Description("Maximum results to return.")] int maxResults = 20,
        CancellationToken cancellationToken = default)
    {
        var effectiveQuery = query;
        if (!query.Contains("repo:", StringComparison.OrdinalIgnoreCase))
        {
            effectiveQuery = $"repo:{gitHub.ResolveRepository(repository)} {query}";
        }

        var results = await gitHub.SearchIssuesAsync(
            effectiveQuery,
            Math.Clamp(maxResults, 1, 100),
            cancellationToken);

        return AppInsightsService.ToJson(new { query = effectiveQuery, results });
    }
}
