using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Risebird.Mcp.BugTriage.Configuration;

namespace Risebird.Mcp.BugTriage.Services;

/// <summary>A single parsed frame from a .NET stack trace.</summary>
public sealed record StackFrameInfo(
    string Method,
    string? DeclaringType,
    string? BuildFilePath,
    int? Line);

/// <summary>A stack frame resolved onto a file in the local checkout.</summary>
public sealed record ResolvedFrame(
    string Method,
    string? RepositoryPath,
    int? Line,
    string? Source,
    string? Resolution,
    IReadOnlyList<string>? AlternateCandidates);

/// <summary>
/// Correlates telemetry and human bug reports with the local checkout: stack frame to file and
/// line, surrounding source, and the recent commits that touched it.
/// </summary>
public sealed partial class RepositoryService(IOptions<TriageOptions> options)
{
    private readonly RepositoryOptions _options = options.Value.Repository;
    private string? _resolvedGitPath;
    private bool _gitProbed;

    // "   at Risebird.Business.UserService.Get(Int32 id) in C:\src\Risebird.Business\UserService.cs:line 42"
    [GeneratedRegex(
        @"^\s*at\s+(?<method>[^\s].*?)(?:\s+in\s+(?<file>.+?):line\s+(?<line>\d+))?\s*$",
        RegexOptions.Multiline)]
    private static partial Regex StackFrameRegex { get; }

    public bool IsConfigured => _options.IsConfigured;

    public string RootPath => _options.RootPath
        ?? throw new InvalidOperationException(
            "Triage:Repository:RootPath is not set, so stack traces cannot be mapped to source files.");

    /// <summary>Extracts every "at ..." frame from a stack trace, in order.</summary>
    public static IReadOnlyList<StackFrameInfo> ParseStackTrace(string stackTrace)
    {
        var frames = new List<StackFrameInfo>();

        foreach (Match match in StackFrameRegex.Matches(stackTrace))
        {
            var method = match.Groups["method"].Value.Trim();
            if (method.Length == 0)
            {
                continue;
            }

            frames.Add(new StackFrameInfo(
                Method: method,
                DeclaringType: ExtractDeclaringType(method),
                BuildFilePath: match.Groups["file"].Success ? match.Groups["file"].Value.Trim() : null,
                Line: match.Groups["line"].Success ? int.Parse(match.Groups["line"].Value) : null));
        }

        return frames;
    }

    /// <summary>
    /// Pulls "Namespace.Type" out of "Namespace.Type.Method(Int32 id)", tolerating generics and
    /// async state machine frames like "Namespace.Type+&lt;Method&gt;d__12.MoveNext()".
    /// </summary>
    private static string? ExtractDeclaringType(string method)
    {
        var parenIndex = method.IndexOf('(', StringComparison.Ordinal);
        var signature = parenIndex >= 0 ? method[..parenIndex] : method;

        // Async/iterator state machines: the real type is what sits before the '+'.
        var plusIndex = signature.IndexOf('+', StringComparison.Ordinal);
        if (plusIndex >= 0)
        {
            signature = signature[..plusIndex];
            return signature.Length > 0 ? signature : null;
        }

        var lastDot = signature.LastIndexOf('.');
        return lastDot > 0 ? signature[..lastDot] : null;
    }

    /// <summary>
    /// Resolves frames onto real files. A build-machine path is matched by its longest trailing
    /// segment that exists locally; otherwise the type's short name is used to search for a .cs file.
    /// </summary>
    public IReadOnlyList<ResolvedFrame> ResolveFrames(
        IReadOnlyList<StackFrameInfo> frames,
        int contextLines)
    {
        var root = RootPath;
        var resolved = new List<ResolvedFrame>(frames.Count);

        foreach (var frame in frames)
        {
            string? relativePath = null;
            string? resolution = null;
            List<string>? alternates = null;

            if (frame.BuildFilePath is not null)
            {
                relativePath = MatchByPathSuffix(root, frame.BuildFilePath);
                resolution = relativePath is not null
                    ? "matched by build path suffix"
                    : "build path recorded but no matching file in the checkout";
            }

            if (relativePath is null && frame.DeclaringType is not null)
            {
                var shortName = frame.DeclaringType[(frame.DeclaringType.LastIndexOf('.') + 1)..];
                // Strip generic arity, e.g. "Repository`1" -> "Repository".
                var backtick = shortName.IndexOf('`', StringComparison.Ordinal);
                if (backtick > 0)
                {
                    shortName = shortName[..backtick];
                }

                var matches = FindFilesByName(root, shortName + ".cs");
                if (matches.Count == 1)
                {
                    relativePath = matches[0];
                    resolution = "matched by type name";
                }
                else if (matches.Count > 1)
                {
                    alternates = matches;
                    resolution = $"ambiguous: {matches.Count} files named {shortName}.cs";
                }
                else
                {
                    resolution ??= "no source file found for this frame";
                }
            }

            string? source = null;
            if (relativePath is not null && frame.Line is > 0)
            {
                source = ReadSourceWindow(Path.Combine(root, relativePath), frame.Line.Value, contextLines);
            }

            resolved.Add(new ResolvedFrame(
                Method: frame.Method,
                RepositoryPath: relativePath,
                Line: frame.Line,
                Source: source,
                Resolution: resolution,
                AlternateCandidates: alternates));
        }

        return resolved;
    }

    /// <summary>
    /// Walks a build-machine path from the right, returning the longest trailing segment that
    /// exists under <paramref name="root"/>. "C:\agent\_work\1\s\A\B.cs" resolves to "A\B.cs".
    /// </summary>
    private static string? MatchByPathSuffix(string root, string buildPath)
    {
        var segments = buildPath.Replace('/', '\\').Split('\\', StringSplitOptions.RemoveEmptyEntries);

        for (var start = segments.Length - 1; start >= 0; start--)
        {
            var candidate = Path.Combine(segments[start..]);
            if (File.Exists(Path.Combine(root, candidate)))
            {
                return candidate;
            }
        }

        return null;
    }

    private static List<string> FindFilesByName(string root, string fileName)
    {
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            MatchCasing = MatchCasing.CaseInsensitive,
        };

        return Directory.EnumerateFiles(root, fileName, options)
            .Where(path => !IsBuildArtifact(path))
            .Select(path => Path.GetRelativePath(root, path))
            .Take(20)
            .ToList();
    }

    private static bool IsBuildArtifact(string path) =>
        path.Contains(@"\bin\", StringComparison.OrdinalIgnoreCase) ||
        path.Contains(@"\obj\", StringComparison.OrdinalIgnoreCase) ||
        path.Contains(@"\node_modules\", StringComparison.OrdinalIgnoreCase);

    /// <summary>Reads a window of source around a line, with 1-based line numbers attached.</summary>
    public static string? ReadSourceWindow(string absolutePath, int line, int contextLines)
    {
        if (!File.Exists(absolutePath))
        {
            return null;
        }

        var lines = File.ReadAllLines(absolutePath);
        var start = Math.Max(1, line - contextLines);
        var end = Math.Min(lines.Length, line + contextLines);

        var builder = new StringBuilder();
        for (var i = start; i <= end; i++)
        {
            builder.Append(i == line ? "->" : "  ")
                   .Append(i.ToString().PadLeft(6))
                   .Append(": ")
                   .AppendLine(lines[i - 1]);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Recent commits touching a file. Returns a diagnostic string rather than throwing when git is
    /// unavailable, so a missing git install degrades one field instead of failing the whole tool.
    /// </summary>
    public async Task<string> GetRecentCommitsAsync(string relativePath, int count, CancellationToken cancellationToken)
    {
        var git = ResolveGitExecutable();
        if (git is null)
        {
            return "git executable not found; set Triage:Repository:GitExecutable to enable commit history.";
        }

        var arguments = $"log -n {count} --date=short --format=\"%h %ad %an: %s\" -- \"{relativePath}\"";
        var (exitCode, stdout, stderr) = await RunProcessAsync(git, arguments, RootPath, cancellationToken);

        if (exitCode != 0)
        {
            return $"git log failed (exit {exitCode}): {stderr.Trim()}";
        }

        return string.IsNullOrWhiteSpace(stdout) ? "no commits recorded for this path" : stdout.Trim();
    }

    /// <summary>
    /// The branch currently checked out, or null when git is unreachable. Callers must treat null as
    /// "unknown", never as "safe".
    /// </summary>
    public async Task<string?> GetCurrentBranchAsync(CancellationToken cancellationToken)
    {
        var git = ResolveGitExecutable();
        if (git is null)
        {
            return null;
        }

        var (exitCode, stdout, _) = await RunProcessAsync(
            git, "rev-parse --abbrev-ref HEAD", RootPath, cancellationToken);

        return exitCode == 0 && !string.IsNullOrWhiteSpace(stdout) ? stdout.Trim() : null;
    }

    public bool IsGitAvailable => ResolveGitExecutable() is not null;

    internal string? ResolveGitExecutable()
    {
        if (_gitProbed)
        {
            return _resolvedGitPath;
        }

        _gitProbed = true;

        if (!string.IsNullOrWhiteSpace(_options.GitExecutable) && File.Exists(_options.GitExecutable))
        {
            return _resolvedGitPath = _options.GitExecutable;
        }

        string[] candidates =
        [
            @"C:\Program Files\Git\cmd\git.exe",
            @"C:\Program Files (x86)\Git\cmd\git.exe",
        ];

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return _resolvedGitPath = candidate;
            }
        }

        // Fall back to PATH resolution and let the process launch decide.
        var pathDirs = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? [];
        foreach (var dir in pathDirs)
        {
            try
            {
                var candidate = Path.Combine(dir, "git.exe");
                if (File.Exists(candidate))
                {
                    return _resolvedGitPath = candidate;
                }
            }
            catch (ArgumentException)
            {
                // Malformed PATH entry; ignore it.
            }
        }

        return _resolvedGitPath = null;
    }

    internal static async Task<(int ExitCode, string StdOut, string StdErr)> RunProcessAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo(fileName, arguments)
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };

        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        return (process.ExitCode, await stdoutTask, await stderrTask);
    }
}
