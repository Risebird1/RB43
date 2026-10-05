using Risebird.Mcp.BugTriage.Services;

namespace Risebird.Mcp.BugTriage.Tests;

/// <summary>
/// The rails that decide whether this server may write to source. These are the tests that matter:
/// a regression here means the server can modify something it should not.
/// </summary>
public sealed class FixSafetyRailTests
{
    private const string SampleSource = """
        namespace Sandbox;

        public static class Discount
        {
            public static decimal Apply(decimal price, int percent)
            {
                return price - (price * percent);
            }
        }
        """;

    [Fact]
    public void ResolveWritablePath_RejectsPathEscapingTheRepositoryRoot()
    {
        using var sandbox = new SandboxRepository();
        var (fix, _) = sandbox.CreateFixService();

        var ex = Assert.Throws<FixRejectedException>(
            () => fix.ResolveWritablePath(@"..\..\outside.cs"));

        Assert.Contains("outside the configured repository root", ex.Message);
    }

    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("local.settings.json")]
    [InlineData("web.config")]
    [InlineData("deploy.pubxml")]
    public void ResolveWritablePath_RejectsConfigurationAndSecretsFiles(string fileName)
    {
        using var sandbox = new SandboxRepository();
        sandbox.WriteFile(fileName, "{}");
        var (fix, _) = sandbox.CreateFixService();

        Assert.Throws<FixRejectedException>(() => fix.ResolveWritablePath(fileName));
    }

    [Fact]
    public void ResolveWritablePath_BlockedPatternsStillApplyWhenTheExtensionIsAllowed()
    {
        // Second layer: if .json were ever added to AllowedExtensions, appsettings.json must still
        // be refused by name.
        using var sandbox = new SandboxRepository();
        sandbox.WriteFile("appsettings.json", "{}");
        var (fix, _) = sandbox.CreateFixService(f => f.AllowedExtensions = [".cs", ".json"]);

        var ex = Assert.Throws<FixRejectedException>(() => fix.ResolveWritablePath("appsettings.json"));

        Assert.Contains("blocked pattern", ex.Message);
    }

    [Fact]
    public void ResolveWritablePath_RejectsBuildArtifacts()
    {
        using var sandbox = new SandboxRepository();
        sandbox.WriteFile(@"src\bin\Debug\Generated.cs", "// generated");
        var (fix, _) = sandbox.CreateFixService();

        var ex = Assert.Throws<FixRejectedException>(
            () => fix.ResolveWritablePath(@"src\bin\Debug\Generated.cs"));

        Assert.Contains("build artifact", ex.Message);
    }

    [Fact]
    public void ResolveWritablePath_RefusesToCreateNewFiles()
    {
        using var sandbox = new SandboxRepository();
        var (fix, _) = sandbox.CreateFixService();

        var ex = Assert.Throws<FixRejectedException>(() => fix.ResolveWritablePath(@"src\DoesNotExist.cs"));

        Assert.Contains("never creates them", ex.Message);
    }

    [Fact]
    public async Task ProposeAsync_RejectsSnippetThatIsNotPresent()
    {
        using var sandbox = new SandboxRepository();
        sandbox.WriteFile(@"src\Discount.cs", SampleSource);
        var (fix, _) = sandbox.CreateFixService();

        var ex = await Assert.ThrowsAsync<FixRejectedException>(() =>
            fix.ProposeAsync(@"src\Discount.cs", "no such text", "x", "because", CancellationToken.None));

        Assert.Contains("was not found", ex.Message);
    }

    [Fact]
    public async Task ProposeAsync_RejectsAmbiguousSnippet()
    {
        using var sandbox = new SandboxRepository();
        sandbox.WriteFile(@"src\Dup.cs", "var a = 1;\nvar a = 1;\n");
        var (fix, _) = sandbox.CreateFixService();

        var ex = await Assert.ThrowsAsync<FixRejectedException>(() =>
            fix.ProposeAsync(@"src\Dup.cs", "var a = 1;", "var a = 2;", "because", CancellationToken.None));

        Assert.Contains("ambiguous", ex.Message);
    }

    [Fact]
    public async Task ProposeAsync_RequiresARationale()
    {
        using var sandbox = new SandboxRepository();
        sandbox.WriteFile(@"src\Discount.cs", SampleSource);
        var (fix, _) = sandbox.CreateFixService();

        await Assert.ThrowsAsync<FixRejectedException>(() =>
            fix.ProposeAsync(@"src\Discount.cs", "price * percent", "price * percent / 100m", "   ",
                CancellationToken.None));
    }

    [Fact]
    public async Task ProposeAsync_RejectsChangesLargerThanTheLineLimit()
    {
        using var sandbox = new SandboxRepository();
        var content = string.Join('\n', Enumerable.Range(0, 50).Select(i => $"var line{i} = {i};"));
        sandbox.WriteFile(@"src\Big.cs", content);
        var (fix, _) = sandbox.CreateFixService(f => f.MaxChangedLines = 10);

        var ex = await Assert.ThrowsAsync<FixRejectedException>(() =>
            fix.ProposeAsync(@"src\Big.cs", content, "var replaced = 0;", "shrink it", CancellationToken.None));

        Assert.Contains("over the 10-line limit", ex.Message);
    }

    [Fact]
    public async Task ProposeAsync_MatchesAMultiLineSnippetAgainstACrlfFile()
    {
        // Callers compose snippets with "\n"; most files in this repository are CRLF. Every earlier
        // test used a single-line snippet, which hid this.
        using var sandbox = new SandboxRepository();
        sandbox.WriteFile(@"src\Crlf.cs", "class A\r\n{\r\n    void M()\r\n    {\r\n        Go();\r\n    }\r\n}\r\n");
        var (fix, _) = sandbox.CreateFixService();

        var proposal = await fix.ProposeAsync(
            @"src\Crlf.cs",
            "    void M()\n    {\n        Go();\n    }",
            "    void M()\n    {\n        GoSafely();\n    }",
            "rename the call",
            CancellationToken.None);

        await fix.ApplyAsync(proposal, CancellationToken.None);

        var result = sandbox.ReadFile(@"src\Crlf.cs");
        Assert.Contains("GoSafely();", result);
        // The file must still be CRLF throughout - no mixed endings introduced by the replacement.
        Assert.DoesNotContain("\n", result.Replace("\r\n", string.Empty, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ProposeAsync_StillMatchesAMultiLineSnippetAgainstAnLfFile()
    {
        using var sandbox = new SandboxRepository();
        sandbox.WriteFile(@"src\Lf.cs", "class A\n{\n    void M()\n    {\n        Go();\n    }\n}\n");
        var (fix, _) = sandbox.CreateFixService();

        var proposal = await fix.ProposeAsync(
            @"src\Lf.cs",
            "    void M()\n    {\n        Go();\n    }",
            "    void M()\n    {\n        GoSafely();\n    }",
            "rename the call",
            CancellationToken.None);

        await fix.ApplyAsync(proposal, CancellationToken.None);

        var result = sandbox.ReadFile(@"src\Lf.cs");
        Assert.Contains("GoSafely();", result);
        Assert.DoesNotContain("\r\n", result);
    }

    [Fact]
    public async Task ProposeAsync_DoesNotModifyTheFile()
    {
        using var sandbox = new SandboxRepository();
        sandbox.WriteFile(@"src\Discount.cs", SampleSource);
        var (fix, _) = sandbox.CreateFixService();

        await fix.ProposeAsync(@"src\Discount.cs", "price * percent", "price * percent / 100m",
            "percent must be a fraction", CancellationToken.None);

        Assert.Equal(SampleSource, sandbox.ReadFile(@"src\Discount.cs"));
    }

    [Fact]
    public async Task PreflightAsync_BlocksWhenWritingIsDisabled()
    {
        using var sandbox = new SandboxRepository();
        sandbox.WriteFile(@"src\Discount.cs", SampleSource);
        var (fix, _) = sandbox.CreateFixService(f => f.Enabled = false);

        var proposal = await fix.ProposeAsync(@"src\Discount.cs", "price * percent",
            "price * percent / 100m", "percent must be a fraction", CancellationToken.None);

        var (blockers, _, _) = await fix.PreflightAsync(proposal, CancellationToken.None);

        Assert.Contains(blockers, b => b.Contains("Writing is disabled"));
    }

    [Fact]
    public async Task PreflightAsync_BlocksWhenTheFileChangedAfterTheProposal()
    {
        using var sandbox = new SandboxRepository();
        sandbox.WriteFile(@"src\Discount.cs", SampleSource);
        var (fix, _) = sandbox.CreateFixService();

        var proposal = await fix.ProposeAsync(@"src\Discount.cs", "price * percent",
            "price * percent / 100m", "percent must be a fraction", CancellationToken.None);

        // Somebody else edits the file while the proposal is pending.
        sandbox.WriteFile(@"src\Discount.cs", SampleSource + "\n// touched by someone else");

        var (blockers, _, _) = await fix.PreflightAsync(proposal, CancellationToken.None);

        Assert.Contains(blockers, b => b.Contains("changed since this fix was proposed"));
    }

    [Fact]
    public async Task PreflightAsync_BlocksExpiredProposals()
    {
        using var sandbox = new SandboxRepository();
        sandbox.WriteFile(@"src\Discount.cs", SampleSource);
        var (fix, _) = sandbox.CreateFixService(f => f.ProposalLifetimeMinutes = 0);

        var proposal = await fix.ProposeAsync(@"src\Discount.cs", "price * percent",
            "price * percent / 100m", "percent must be a fraction", CancellationToken.None);

        await Task.Delay(50);

        var (blockers, _, _) = await fix.PreflightAsync(proposal, CancellationToken.None);

        Assert.Contains(blockers, b => b.Contains("expired"));
    }

    [Fact]
    public async Task ApplyAsync_RefusesWhenTheFileChangedWhileConsentWasBeingObtained()
    {
        using var sandbox = new SandboxRepository();
        sandbox.WriteFile(@"src\Discount.cs", SampleSource);
        var (fix, _) = sandbox.CreateFixService();

        var proposal = await fix.ProposeAsync(@"src\Discount.cs", "price * percent",
            "price * percent / 100m", "percent must be a fraction", CancellationToken.None);

        // Preflight passes, then the file changes before the write - the race the second hash check exists for.
        var (blockers, _, _) = await fix.PreflightAsync(proposal, CancellationToken.None);
        Assert.DoesNotContain(blockers, b => b.Contains("changed"));

        var edited = SampleSource + "\n// concurrent edit";
        sandbox.WriteFile(@"src\Discount.cs", edited);

        var ex = await Assert.ThrowsAsync<FixRejectedException>(
            () => fix.ApplyAsync(proposal, CancellationToken.None));

        Assert.Contains("changed while consent was being obtained", ex.Message);
        Assert.Equal(edited, sandbox.ReadFile(@"src\Discount.cs"));
    }

    [Fact]
    public async Task ApplyAsync_WritesTheChangeAndRevertRestoresItExactly()
    {
        using var sandbox = new SandboxRepository();
        sandbox.WriteFile(@"src\Discount.cs", SampleSource);
        var (fix, _) = sandbox.CreateFixService();

        var proposal = await fix.ProposeAsync(@"src\Discount.cs", "price * percent",
            "price * percent / 100m", "percent must be a fraction", CancellationToken.None);

        await fix.ApplyAsync(proposal, CancellationToken.None);

        var afterApply = sandbox.ReadFile(@"src\Discount.cs");
        Assert.Contains("price * percent / 100m", afterApply);
        Assert.NotEqual(SampleSource, afterApply);

        await fix.RevertAsync(proposal.Id, force: false, CancellationToken.None);

        Assert.Equal(SampleSource, sandbox.ReadFile(@"src\Discount.cs"));
    }

    [Fact]
    public async Task RevertAsync_RefusesWhenTheFileChangedAfterApply_UnlessForced()
    {
        using var sandbox = new SandboxRepository();
        sandbox.WriteFile(@"src\Discount.cs", SampleSource);
        var (fix, _) = sandbox.CreateFixService();

        var proposal = await fix.ProposeAsync(@"src\Discount.cs", "price * percent",
            "price * percent / 100m", "percent must be a fraction", CancellationToken.None);

        await fix.ApplyAsync(proposal, CancellationToken.None);
        sandbox.WriteFile(@"src\Discount.cs", sandbox.ReadFile(@"src\Discount.cs") + "\n// later work");

        await Assert.ThrowsAsync<FixRejectedException>(
            () => fix.RevertAsync(proposal.Id, force: false, CancellationToken.None));

        // The later work is still there because the revert was refused.
        Assert.Contains("// later work", sandbox.ReadFile(@"src\Discount.cs"));

        await fix.RevertAsync(proposal.Id, force: true, CancellationToken.None);
        Assert.Equal(SampleSource, sandbox.ReadFile(@"src\Discount.cs"));
    }

    [Fact]
    public async Task ProposalCannotBeAppliedTwice()
    {
        using var sandbox = new SandboxRepository();
        sandbox.WriteFile(@"src\Discount.cs", SampleSource);
        var (fix, store) = sandbox.CreateFixService();

        var proposal = await fix.ProposeAsync(@"src\Discount.cs", "price * percent",
            "price * percent / 100m", "percent must be a fraction", CancellationToken.None);

        await fix.ApplyAsync(proposal, CancellationToken.None);

        // Applying consumes the proposal, so a replayed apply_fix call finds nothing to apply.
        Assert.Null(store.Get(proposal.Id));
    }
}
