using System.ComponentModel;
using ModelContextProtocol.Server;
using Risebird.Mcp.BugTriage.Services;

namespace Risebird.Mcp.BugTriage.Tools;

/// <summary>
/// Correlates a failure with the local checkout. This is the step that turns "NullReferenceException
/// in Risebird.Business" into "Risebird.Business\Services\FooService.cs:118, last touched three weeks
/// ago in commit abc1234".
/// </summary>
[McpServerToolType]
public static class CodeTools
{
    [McpServerTool(Name = "resolve_stack_trace")]
    [Description(
        "Map a .NET stack trace onto files in the local checkout. For every frame it returns the " +
        "repository-relative path, the line, the surrounding source, and the recent commits that " +
        "touched that file. Paste the raw stack trace from Application Insights, a work item, or an " +
        "issue - build-machine paths are matched by suffix, and frames without a path are matched by " +
        "type name.")]
    public static async Task<string> ResolveStackTraceAsync(
        RepositoryService repository,
        [Description("The raw stack trace text. Lines that do not look like frames are ignored.")]
        string stackTrace,
        [Description("Lines of source to show either side of each frame's line.")]
        int contextLines = 8,
        [Description("How many recent commits to list per resolved file. Set 0 to skip git history.")]
        int commitsPerFile = 5,
        [Description("Only resolve the first N frames. Deep traces are mostly framework noise.")]
        int maxFrames = 12,
        CancellationToken cancellationToken = default)
    {
        if (!repository.IsConfigured)
        {
            return "Triage:Repository:RootPath is not set (or does not exist), so stack traces cannot be " +
                   "mapped to source files.";
        }

        var frames = RepositoryService.ParseStackTrace(stackTrace);
        if (frames.Count == 0)
        {
            return "No stack frames found. Expected lines of the form: " +
                   @"'   at Namespace.Type.Method(args) in C:\path\File.cs:line 42'.";
        }

        var considered = frames.Take(Math.Clamp(maxFrames, 1, 50)).ToList();
        var resolved = repository.ResolveFrames(considered, Math.Clamp(contextLines, 0, 40));

        var enriched = new List<object>(resolved.Count);
        foreach (var frame in resolved)
        {
            string? history = null;
            if (commitsPerFile > 0 && frame.RepositoryPath is not null)
            {
                history = await repository.GetRecentCommitsAsync(
                    frame.RepositoryPath,
                    Math.Clamp(commitsPerFile, 1, 20),
                    cancellationToken);
            }

            enriched.Add(new
            {
                frame.Method,
                file = frame.RepositoryPath,
                frame.Line,
                frame.Resolution,
                frame.AlternateCandidates,
                recentCommits = history,
                source = frame.Source,
            });
        }

        return AppInsightsService.ToJson(new
        {
            framesFound = frames.Count,
            framesResolved = resolved.Count(f => f.RepositoryPath is not null),
            frames = enriched,
        });
    }

    [McpServerTool(Name = "get_file_history")]
    [Description(
        "Recent commits that touched a file in the checkout. Use it to answer 'did this break " +
        "recently?' once a suspect file has been identified.")]
    public static async Task<string> GetFileHistoryAsync(
        RepositoryService repository,
        [Description(@"Repository-relative path, e.g. 'Risebird.Business\Services\UserService.cs'.")]
        string path,
        [Description("How many commits to return.")] int count = 10,
        CancellationToken cancellationToken = default)
    {
        if (!repository.IsConfigured)
        {
            return "Triage:Repository:RootPath is not set (or does not exist).";
        }

        var history = await repository.GetRecentCommitsAsync(path, Math.Clamp(count, 1, 50), cancellationToken);
        return AppInsightsService.ToJson(new { path, recentCommits = history });
    }

    [McpServerTool(Name = "read_source_window")]
    [Description(
        "Read a numbered window of source around a specific line. Use it to widen the context after " +
        "resolve_stack_trace has pointed at a file and line.")]
    public static string ReadSourceWindow(
        RepositoryService repository,
        [Description(@"Repository-relative path, e.g. 'Risebird.Business\Services\UserService.cs'.")]
        string path,
        [Description("The 1-based line to centre the window on.")] int line,
        [Description("Lines of context either side.")] int contextLines = 20)
    {
        if (!repository.IsConfigured)
        {
            return "Triage:Repository:RootPath is not set (or does not exist).";
        }

        var absolute = Path.GetFullPath(Path.Combine(repository.RootPath, path));
        var root = Path.GetFullPath(repository.RootPath);

        // Keep path traversal inside the configured checkout.
        if (!absolute.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return "Path resolves outside the configured repository root and was rejected.";
        }

        var window = RepositoryService.ReadSourceWindow(absolute, line, Math.Clamp(contextLines, 0, 200));
        return window ?? $"File not found: {path}";
    }
}
