using System.IO.Enumeration;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Risebird.Mcp.BugTriage.Configuration;

namespace Risebird.Mcp.BugTriage.Services;

/// <summary>Raised when a fix violates a rail that no amount of user consent can waive.</summary>
public sealed class FixRejectedException(string message) : Exception(message);

/// <summary>Outcome of building the project that owns a changed file.</summary>
public sealed record VerificationResult(
    bool Succeeded,
    string? ProjectPath,
    int ErrorCount,
    IReadOnlyList<string> Errors,
    string Summary);

/// <summary>
/// The write path. Every guard lives here rather than in the tool methods, so a new tool cannot
/// accidentally route around them.
/// </summary>
public sealed partial class FixService(
    RepositoryService repository,
    FixProposalStore store,
    IOptions<TriageOptions> options)
{
    private readonly FixOptions _fix = options.Value.Fix;

    [GeneratedRegex(@"^(?<file>.+?)\((?<line>\d+),(?<col>\d+)\):\s*error\s+(?<code>[A-Z]+\d+):\s*(?<message>.+?)\s*\[",
        RegexOptions.Multiline)]
    private static partial Regex BuildErrorRegex { get; }

    public bool WritesEnabled => _fix.Enabled;

    public int ProposalLifetimeMinutes => _fix.ProposalLifetimeMinutes;

    /// <summary>
    /// Resolves a repo-relative path and enforces the path rails: inside the checkout, an allowed
    /// extension, and not a blocked configuration or secrets file.
    /// </summary>
    public string ResolveWritablePath(string relativePath)
    {
        if (!repository.IsConfigured)
        {
            throw new FixRejectedException("Triage:Repository:RootPath is not set, so no file can be located.");
        }

        var root = Path.GetFullPath(repository.RootPath);
        var absolute = Path.GetFullPath(Path.Combine(root, relativePath));

        // Containment check on the normalised paths, so ..\ cannot escape the checkout.
        if (!absolute.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new FixRejectedException(
                $"Path resolves outside the configured repository root and was rejected: {relativePath}");
        }

        if (!File.Exists(absolute))
        {
            throw new FixRejectedException(
                $"File does not exist: {relativePath}. This server only edits existing files; it never creates them.");
        }

        var fileName = Path.GetFileName(absolute);
        var extension = Path.GetExtension(absolute);

        if (!_fix.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new FixRejectedException(
                $"Files with extension '{extension}' cannot be edited. Allowed: {string.Join(", ", _fix.AllowedExtensions)}.");
        }

        foreach (var pattern in _fix.BlockedPathPatterns)
        {
            if (FileSystemName.MatchesSimpleExpression(pattern, fileName))
            {
                throw new FixRejectedException(
                    $"'{fileName}' matches the blocked pattern '{pattern}'. Configuration and secrets files are " +
                    "never edited by this server, regardless of consent. Change it by hand.");
            }
        }

        if (absolute.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
            absolute.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
        {
            throw new FixRejectedException($"'{relativePath}' is a build artifact, not source.");
        }

        return absolute;
    }

    /// <summary>
    /// Validates a proposed replacement against the file as it exists right now, and records it.
    /// Nothing is written.
    /// </summary>
    public async Task<FixProposal> ProposeAsync(
        string relativePath,
        string originalText,
        string replacementText,
        string rationale,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(originalText))
        {
            throw new FixRejectedException(
                "originalText must be the exact existing snippet to replace. Whole-file replacement is not supported.");
        }

        if (string.Equals(originalText, replacementText, StringComparison.Ordinal))
        {
            throw new FixRejectedException("originalText and replacementText are identical - nothing to change.");
        }

        if (string.IsNullOrWhiteSpace(rationale))
        {
            throw new FixRejectedException(
                "A rationale is required: state what is wrong and why this change fixes it. The human " +
                "reviewing this needs it.");
        }

        var absolute = ResolveWritablePath(relativePath);
        var content = await File.ReadAllTextAsync(absolute, cancellationToken);

        // Callers compose snippets with "\n" while most files here are CRLF, so an exact ordinal match
        // would fail on every multi-line snippet. Re-line-end the snippets to match the file instead of
        // making the caller guess.
        originalText = MatchLineEndings(originalText, content);
        replacementText = MatchLineEndings(replacementText, content);

        var occurrences = CountOccurrences(content, originalText);
        if (occurrences == 0)
        {
            throw new FixRejectedException(
                $"originalText was not found in {relativePath}. It must match the file exactly, including " +
                "indentation and line endings. Read the file first.");
        }

        if (occurrences > 1)
        {
            throw new FixRejectedException(
                $"originalText appears {occurrences} times in {relativePath}, so the change is ambiguous. " +
                "Include more surrounding context to make it unique.");
        }

        var index = content.IndexOf(originalText, StringComparison.Ordinal);
        var firstChangedLine = content.Take(index).Count(c => c == '\n') + 1;

        var oldLineCount = CountLines(originalText);
        var newLineCount = CountLines(replacementText);
        var changedLines = Math.Max(oldLineCount, newLineCount);

        if (changedLines > _fix.MaxChangedLines)
        {
            throw new FixRejectedException(
                $"This change touches {changedLines} lines, over the {_fix.MaxChangedLines}-line limit for a " +
                "single fix. Large rewrites should go through a normal code review, not this tool.");
        }

        var proposal = new FixProposal(
            Id: FixProposalStore.NewId(),
            RelativePath: relativePath,
            OriginalText: originalText,
            ReplacementText: replacementText,
            Rationale: rationale,
            FileHashAtProposal: FixProposalStore.HashContent(content),
            FirstChangedLine: firstChangedLine,
            ChangedLineCount: changedLines,
            Diff: BuildDiff(relativePath, firstChangedLine, originalText, replacementText),
            CreatedAt: DateTimeOffset.UtcNow);

        return store.Add(proposal);
    }

    /// <summary>
    /// Checks every rail that applies at write time and returns the reasons a fix cannot proceed.
    /// An empty list means it is safe to ask the user.
    /// </summary>
    public async Task<(List<string> Blockers, List<string> Warnings, string? Branch)> PreflightAsync(
        FixProposal proposal,
        CancellationToken cancellationToken)
    {
        var blockers = new List<string>();
        var warnings = new List<string>();

        if (!_fix.Enabled)
        {
            blockers.Add(
                "Writing is disabled. Set Triage:Fix:Enabled to true to allow apply_fix. This is off by " +
                "default so the analysis tools cannot silently gain write access.");
        }

        if (proposal.IsExpired(_fix.ProposalLifetimeMinutes))
        {
            blockers.Add(
                $"Proposal {proposal.Id} expired after {_fix.ProposalLifetimeMinutes} minutes. " +
                "Call propose_fix again against the current file.");
        }

        var absolute = ResolveWritablePath(proposal.RelativePath);
        var currentContent = await File.ReadAllTextAsync(absolute, cancellationToken);
        var currentHash = FixProposalStore.HashContent(currentContent);

        if (!string.Equals(currentHash, proposal.FileHashAtProposal, StringComparison.Ordinal))
        {
            blockers.Add(
                $"{proposal.RelativePath} changed since this fix was proposed. Re-read it and propose again - " +
                "applying now could clobber someone else's edit.");
        }
        else if (CountOccurrences(currentContent, proposal.OriginalText) != 1)
        {
            blockers.Add("The text to replace is no longer uniquely present in the file. Propose the fix again.");
        }

        var branch = await repository.GetCurrentBranchAsync(cancellationToken);
        if (branch is null)
        {
            warnings.Add(
                "git is not reachable, so the current branch could not be checked against the protected list. " +
                "Verify by hand that you are not on a protected branch. Undo will rely on the in-memory backup " +
                "(revert_fix) rather than git.");
        }
        else if (_fix.ProtectedBranches.Contains(branch, StringComparer.OrdinalIgnoreCase))
        {
            blockers.Add(
                $"The checkout is on protected branch '{branch}'. Create a fix branch first; this server will " +
                "not write to a protected branch.");
        }

        return (blockers, warnings, branch);
    }

    /// <summary>Writes the change. Call only after <see cref="PreflightAsync"/> is clear and the user has consented.</summary>
    public async Task<AppliedFix> ApplyAsync(FixProposal proposal, CancellationToken cancellationToken)
    {
        var absolute = ResolveWritablePath(proposal.RelativePath);
        var before = await File.ReadAllTextAsync(absolute, cancellationToken);

        // Re-check immediately before writing: PreflightAsync ran before the consent round-trip, and
        // the file could have changed while the user was deciding.
        if (!string.Equals(FixProposalStore.HashContent(before), proposal.FileHashAtProposal, StringComparison.Ordinal))
        {
            throw new FixRejectedException(
                $"{proposal.RelativePath} changed while consent was being obtained. Nothing was written. " +
                "Propose the fix again against the current content.");
        }

        var index = before.IndexOf(proposal.OriginalText, StringComparison.Ordinal);
        var after = string.Concat(
            before.AsSpan(0, index),
            proposal.ReplacementText,
            before.AsSpan(index + proposal.OriginalText.Length));

        await File.WriteAllTextAsync(absolute, after, cancellationToken);

        var applied = new AppliedFix(
            ProposalId: proposal.Id,
            RelativePath: proposal.RelativePath,
            ContentBeforeApply: before,
            FileHashAfterApply: FixProposalStore.HashContent(after),
            AppliedAt: DateTimeOffset.UtcNow);

        store.RecordApplied(applied);
        store.Remove(proposal.Id);

        return applied;
    }

    /// <summary>Restores the file exactly as it was before the fix, if nothing else has touched it since.</summary>
    public async Task<string> RevertAsync(string proposalId, bool force, CancellationToken cancellationToken)
    {
        var applied = store.GetApplied(proposalId)
            ?? throw new FixRejectedException($"No applied fix recorded with id '{proposalId}'.");

        var absolute = ResolveWritablePath(applied.RelativePath);
        var current = await File.ReadAllTextAsync(absolute, cancellationToken);

        if (!force &&
            !string.Equals(FixProposalStore.HashContent(current), applied.FileHashAfterApply, StringComparison.Ordinal))
        {
            throw new FixRejectedException(
                $"{applied.RelativePath} has been edited since the fix was applied. Reverting would discard " +
                "those later edits. Pass force=true only if you are certain that is what you want.");
        }

        await File.WriteAllTextAsync(absolute, applied.ContentBeforeApply, cancellationToken);
        store.RemoveApplied(proposalId);

        return $"Reverted {applied.RelativePath} to its content before fix {proposalId}.";
    }

    /// <summary>
    /// Builds the project that owns a file so a fix is verified rather than assumed. This is the
    /// difference between "I changed the code" and "I changed the code and it still compiles".
    /// </summary>
    public async Task<VerificationResult> VerifyAsync(string relativePath, CancellationToken cancellationToken)
    {
        var absolute = ResolveWritablePath(relativePath);
        var project = FindOwningProject(absolute, Path.GetFullPath(repository.RootPath));

        if (project is null)
        {
            return new VerificationResult(false, null, 0, [],
                $"No .csproj found above {relativePath}, so the change could not be build-verified.");
        }

        var (exitCode, stdout, stderr) = await RepositoryService.RunProcessAsync(
            "dotnet",
            $"build \"{project}\" --nologo -v q",
            repository.RootPath,
            cancellationToken);

        var output = stdout + Environment.NewLine + stderr;
        var errors = BuildErrorRegex.Matches(output)
            .Select(m => $"{m.Groups["file"].Value}({m.Groups["line"].Value}): {m.Groups["code"].Value}: {m.Groups["message"].Value}")
            .Distinct(StringComparer.Ordinal)
            .Take(25)
            .ToList();

        var projectName = Path.GetFileName(project);
        var summary = exitCode == 0
            ? $"{projectName} builds cleanly after the change."
            : $"{projectName} FAILED to build after the change ({errors.Count} error(s) shown). The fix is not safe as written.";

        return new VerificationResult(exitCode == 0, Path.GetRelativePath(repository.RootPath, project), errors.Count, errors, summary);
    }

    private static string? FindOwningProject(string filePath, string root)
    {
        var directory = Path.GetDirectoryName(filePath);

        while (directory is not null && directory.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            var project = Directory.EnumerateFiles(directory, "*.csproj").FirstOrDefault();
            if (project is not null)
            {
                return project;
            }

            directory = Path.GetDirectoryName(directory);
        }

        return null;
    }

    /// <summary>
    /// Renders the change as a line-oriented diff. The replacement is a single contiguous region, so
    /// the affected range is known exactly and needs no LCS pass.
    /// </summary>
    private static string BuildDiff(string relativePath, int firstChangedLine, string original, string replacement)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"--- {relativePath} (line {firstChangedLine})");
        builder.AppendLine($"+++ {relativePath} (proposed)");

        foreach (var line in SplitLines(original))
        {
            builder.Append("- ").AppendLine(line);
        }

        foreach (var line in SplitLines(replacement))
        {
            builder.Append("+ ").AppendLine(line);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Rewrites <paramref name="text"/> to use the same line endings as <paramref name="content"/>,
    /// so a snippet written with "\n" still matches a CRLF file byte for byte.
    /// </summary>
    private static string MatchLineEndings(string text, string content)
    {
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal);

        return content.Contains("\r\n", StringComparison.Ordinal)
            ? normalized.Replace("\n", "\r\n", StringComparison.Ordinal)
            : normalized;
    }

    private static string[] SplitLines(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

    private static int CountLines(string text) => SplitLines(text).Length;

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;

        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;

            if (count > 1)
            {
                // Callers only care about "none", "exactly one" or "more than one".
                break;
            }
        }

        return count;
    }
}
