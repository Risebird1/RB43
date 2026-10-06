using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Risebird.Mcp.BugTriage.Services;

/// <summary>A change that has been described and validated but not written to disk.</summary>
public sealed record FixProposal(
    string Id,
    string RelativePath,
    string OriginalText,
    string ReplacementText,
    string Rationale,
    string FileHashAtProposal,
    int FirstChangedLine,
    int ChangedLineCount,
    string Diff,
    DateTimeOffset CreatedAt)
{
    public bool IsExpired(int lifetimeMinutes) =>
        DateTimeOffset.UtcNow - CreatedAt > TimeSpan.FromMinutes(lifetimeMinutes);
}

/// <summary>A fix that has been written, retaining the original content so it can be undone.</summary>
public sealed record AppliedFix(
    string ProposalId,
    string RelativePath,
    string ContentBeforeApply,
    string FileHashAfterApply,
    DateTimeOffset AppliedAt);

/// <summary>
/// In-process record of proposed and applied fixes. Deliberately not persisted: a proposal should
/// not outlive the session that reviewed it, and the undo path for anything older is git.
/// </summary>
public sealed class FixProposalStore
{
    private readonly ConcurrentDictionary<string, FixProposal> _proposals = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, AppliedFix> _applied = new(StringComparer.Ordinal);

    public FixProposal Add(FixProposal proposal)
    {
        _proposals[proposal.Id] = proposal;
        return proposal;
    }

    public FixProposal? Get(string id) =>
        _proposals.TryGetValue(id, out var proposal) ? proposal : null;

    public bool Remove(string id) => _proposals.TryRemove(id, out _);

    public IReadOnlyCollection<FixProposal> PendingProposals => _proposals.Values.ToArray();

    public void RecordApplied(AppliedFix applied) => _applied[applied.ProposalId] = applied;

    public AppliedFix? GetApplied(string proposalId) =>
        _applied.TryGetValue(proposalId, out var applied) ? applied : null;

    public bool RemoveApplied(string proposalId) => _applied.TryRemove(proposalId, out _);

    public IReadOnlyCollection<AppliedFix> AppliedFixes => _applied.Values.ToArray();

    public static string NewId() => $"fix-{Guid.NewGuid().ToString("n")[..8]}";

    /// <summary>
    /// Hash of a file's exact bytes, used to detect that a file changed between proposing a fix and
    /// applying it. Not a security boundary - it catches concurrent edits, not tampering.
    /// </summary>
    public static string HashContent(string content) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)))[..16];
}
