using System.ComponentModel;
using ModelContextProtocol.Server;
using Risebird.Mcp.BugTriage.Services;

namespace Risebird.Mcp.BugTriage.Tools;

/// <summary>The consent form presented to the user when a fix is about to be written.</summary>
public sealed class FixConsentForm
{
    [Description("Type APPLY to write this change to disk. Anything else cancels.")]
    public string Decision { get; set; } = string.Empty;
}

/// <summary>
/// The write path: propose a change, get the user's consent, apply it, verify it builds, and be able
/// to undo it. Everything here is deliberately two-phase - no single call can both decide on a change
/// and write it.
/// </summary>
[McpServerToolType]
public static class FixTools
{
    private const string ApplyKeyword = "APPLY";

    [McpServerTool(Name = "propose_fix", ReadOnly = true, Idempotent = true)]
    [Description(
        "Propose a fix without writing anything. Validates that the snippet exists exactly once in the " +
        "file, checks it against the safety rails, and returns a diff plus a proposalId for review. " +
        "This never modifies the file - apply_fix does that, and only with the user's consent. " +
        "Always call this first and show the returned diff to the user.")]
    public static async Task<string> ProposeFixAsync(
        FixService fixService,
        [Description(@"Repository-relative path, e.g. 'Risebird.Business\UserService.cs'.")]
        string path,
        [Description("The exact existing text to replace, including indentation. Must appear exactly once in the file.")]
        string originalText,
        [Description("The replacement text.")]
        string replacementText,
        [Description("Why the current code is wrong and why this change fixes it. Shown to the user for review.")]
        string rationale,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var proposal = await fixService.ProposeAsync(
                path, originalText, replacementText, rationale, cancellationToken);

            return AppInsightsService.ToJson(new
            {
                proposalId = proposal.Id,
                file = proposal.RelativePath,
                firstChangedLine = proposal.FirstChangedLine,
                changedLines = proposal.ChangedLineCount,
                rationale = proposal.Rationale,
                diff = proposal.Diff,
                expiresInMinutes = fixService.ProposalLifetimeMinutes,
                writesEnabled = fixService.WritesEnabled,
                nextStep = fixService.WritesEnabled
                    ? "Show this diff to the user. If they want it applied, call apply_fix with this proposalId. " +
                      "Do not call apply_fix without showing them the diff first."
                    : "Writing is disabled (Triage:Fix:Enabled is false), so this proposal cannot be applied. " +
                      "Show the diff to the user so they can apply it by hand.",
            });
        }
        catch (FixRejectedException ex)
        {
            return $"Proposal rejected: {ex.Message}";
        }
    }

    [McpServerTool(Name = "apply_fix", Destructive = true, ReadOnly = false, Idempotent = false, OpenWorld = false)]
    [Description(
        "Write a previously proposed fix to disk, after obtaining the user's explicit consent. " +
        "Requires a proposalId from propose_fix. Refuses if writing is disabled, the file changed since " +
        "the proposal, or the checkout is on a protected branch. By default it builds the affected " +
        "project afterwards and automatically reverts if the build breaks. " +
        "Never call this without first showing the user the diff from propose_fix.")]
    public static async Task<string> ApplyFixAsync(
        McpServer server,
        FixService fixService,
        FixProposalStore store,
        [Description("The proposalId returned by propose_fix.")]
        string proposalId,
        [Description(
            "Fallback consent for clients that cannot show a prompt. Leave unset: the server asks the " +
            "user directly. Only set this to APPLY when the user has explicitly told you to apply this " +
            "specific fix and the server reported that it cannot prompt them itself.")]
        string? confirm = null,
        [Description("Build the affected project after writing, and revert automatically if it fails. Strongly recommended.")]
        bool verifyBuild = true,
        CancellationToken cancellationToken = default)
    {
        var proposal = store.Get(proposalId);
        if (proposal is null)
        {
            return $"No pending proposal with id '{proposalId}'. It may have been applied already, or expired. " +
                   "Call propose_fix again.";
        }

        List<string> blockers;
        List<string> warnings;
        string? branch;

        try
        {
            (blockers, warnings, branch) = await fixService.PreflightAsync(proposal, cancellationToken);
        }
        catch (FixRejectedException ex)
        {
            return $"Fix rejected: {ex.Message}";
        }

        if (blockers.Count > 0)
        {
            return AppInsightsService.ToJson(new
            {
                applied = false,
                proposalId,
                blockers,
                message = "Nothing was written. Every blocker above must be resolved first.",
            });
        }

        // Consent gate. The client's own tool-approval prompt is the first gate; this is the second,
        // and it names the specific file so the user is not approving an abstract "apply_fix".
        var consent = await RequestConsentAsync(server, proposal, branch, warnings, confirm, cancellationToken);
        if (!consent.Granted)
        {
            return AppInsightsService.ToJson(new
            {
                applied = false,
                proposalId,
                reason = consent.Reason,
                diff = proposal.Diff,
            });
        }

        AppliedFix applied;
        try
        {
            applied = await fixService.ApplyAsync(proposal, cancellationToken);
        }
        catch (FixRejectedException ex)
        {
            return $"Fix rejected at write time: {ex.Message}";
        }

        if (!verifyBuild)
        {
            return AppInsightsService.ToJson(new
            {
                applied = true,
                proposalId,
                file = applied.RelativePath,
                branch,
                warnings,
                consentVia = consent.Reason,
                verification = "skipped - the change was NOT build-verified",
                undo = $"call revert_fix with proposalId '{proposalId}'",
            });
        }

        var verification = await fixService.VerifyAsync(applied.RelativePath, cancellationToken);

        if (!verification.Succeeded)
        {
            var revertMessage = await fixService.RevertAsync(proposalId, force: true, cancellationToken);

            return AppInsightsService.ToJson(new
            {
                applied = false,
                revertedAutomatically = true,
                proposalId,
                file = applied.RelativePath,
                verification.Summary,
                verification.Errors,
                revertMessage,
                message = "The change broke the build, so it was rolled back. The file is as it was. " +
                          "Work out why from the errors above before proposing again.",
            });
        }

        return AppInsightsService.ToJson(new
        {
            applied = true,
            proposalId,
            file = applied.RelativePath,
            branch,
            warnings,
            consentVia = consent.Reason,
            verification = verification.Summary,
            verifiedProject = verification.ProjectPath,
            undo = $"call revert_fix with proposalId '{proposalId}'",
            reminder = "The project compiles, which is not the same as the bug being fixed. Tests and a " +
                       "human review still apply.",
        });
    }

    [McpServerTool(Name = "revert_fix", Destructive = true, ReadOnly = false, Idempotent = true, OpenWorld = false)]
    [Description(
        "Undo a fix applied earlier in this session, restoring the file exactly as it was. Refuses if " +
        "the file has been edited since, unless force is set.")]
    public static async Task<string> RevertFixAsync(
        FixService fixService,
        [Description("The proposalId of the applied fix.")] string proposalId,
        [Description("Revert even though the file changed after the fix was applied. This discards those later edits.")]
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await fixService.RevertAsync(proposalId, force, cancellationToken);
        }
        catch (FixRejectedException ex)
        {
            return $"Revert refused: {ex.Message}";
        }
    }

    [McpServerTool(Name = "verify_build", ReadOnly = true)]
    [Description(
        "Build the project that owns a file and report any compiler errors. Use it to check the state " +
        "of the tree, or after editing a file directly rather than through apply_fix.")]
    public static async Task<string> VerifyBuildAsync(
        FixService fixService,
        [Description(@"Repository-relative path of a file in the project to build.")] string path,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await fixService.VerifyAsync(path, cancellationToken);
            return AppInsightsService.ToJson(result);
        }
        catch (FixRejectedException ex)
        {
            return $"Cannot verify: {ex.Message}";
        }
    }

    [McpServerTool(Name = "list_fixes", ReadOnly = true, Idempotent = true)]
    [Description("List the fix proposals pending review and the fixes applied so far in this session.")]
    public static string ListFixes(FixService fixService, FixProposalStore store) =>
        AppInsightsService.ToJson(new
        {
            writesEnabled = fixService.WritesEnabled,
            pending = store.PendingProposals.Select(p => new
            {
                p.Id,
                p.RelativePath,
                p.FirstChangedLine,
                p.ChangedLineCount,
                p.Rationale,
                p.CreatedAt,
                expired = p.IsExpired(fixService.ProposalLifetimeMinutes),
            }),
            applied = store.AppliedFixes.Select(a => new { a.ProposalId, a.RelativePath, a.AppliedAt }),
        });

    /// <summary>
    /// Asks the user to confirm, preferring MCP elicitation so the prompt names the actual file and
    /// diff. Clients without elicitation fall back to the explicit confirm argument, which the agent
    /// can only set after the user has said so.
    /// </summary>
    private static async Task<(bool Granted, string Reason)> RequestConsentAsync(
        McpServer server,
        FixProposal proposal,
        string? branch,
        IReadOnlyList<string> warnings,
        string? confirm,
        CancellationToken cancellationToken)
    {
        var prompt =
            $"Apply this fix to {proposal.RelativePath} (line {proposal.FirstChangedLine}, " +
            $"{proposal.ChangedLineCount} line(s))?\n\n" +
            $"Why: {proposal.Rationale}\n\n" +
            $"Branch: {branch ?? "unknown - git not reachable"}\n" +
            (warnings.Count > 0 ? $"Warnings: {string.Join(" | ", warnings)}\n" : string.Empty) +
            $"\n{proposal.Diff}";

        try
        {
            var result = await server.ElicitAsync<FixConsentForm>(prompt, options: null, cancellationToken);

            if (!string.Equals(result.Action, "accept", StringComparison.OrdinalIgnoreCase))
            {
                return (false, $"The user {result.Action ?? "cancelled"} the change at the consent prompt.");
            }

            var decision = result.Content?.Decision?.Trim();
            return string.Equals(decision, ApplyKeyword, StringComparison.OrdinalIgnoreCase)
                ? (true, "user confirmed via MCP elicitation prompt")
                : (false, $"Consent prompt was not confirmed with {ApplyKeyword} (got '{decision}'). Nothing was written.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException)
        {
            // The client cannot prompt. Fall back to the explicit argument, which is still a second
            // deliberate step: the agent had to be told to set it after seeing the diff.
            var supplied = confirm?.Trim();

            if (string.Equals(supplied, ApplyKeyword, StringComparison.OrdinalIgnoreCase))
            {
                return (true, "client does not support elicitation; applied via explicit confirm argument");
            }

            return (false, string.IsNullOrEmpty(supplied)
                ? "This client cannot show a consent prompt, and no confirmation was supplied. Show the diff " +
                  $"to the user, and call apply_fix again with confirm='{ApplyKeyword}' only if they agree."
                : $"Consent value '{supplied}' is not '{ApplyKeyword}', so nothing was written. If the user has " +
                  $"seen the diff and agreed, call apply_fix again with confirm='{ApplyKeyword}' exactly.");
        }
    }
}
