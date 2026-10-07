namespace Risebird.Mcp.BugTriage.Configuration;

/// <summary>
/// Everything the triage server needs to reach the four issue sources.
/// Secrets (PATs, tokens) must come from user-secrets or environment variables,
/// never from appsettings.json.
/// </summary>
public sealed class TriageOptions
{
    public const string SectionName = "Triage";

    public AppInsightsOptions AppInsights { get; set; } = new();

    public AzureDevOpsOptions AzureDevOps { get; set; } = new();

    public GitHubOptions GitHub { get; set; } = new();

    public RepositoryOptions Repository { get; set; } = new();

    public FixOptions Fix { get; set; } = new();
}

/// <summary>
/// Controls the only tools in this server that can modify anything. Disabled by default: writing
/// has to be switched on deliberately, not inherited by someone who just wanted the analysis tools.
/// </summary>
public sealed class FixOptions
{
    /// <summary>Master switch. While false, propose_fix still works but apply_fix always refuses.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Branches a fix may never be applied to. Checked whenever git is reachable; when it is not,
    /// apply_fix says so explicitly rather than assuming it is safe.
    /// </summary>
    public string[] ProtectedBranches { get; set; } = ["master", "main", "dev2026", "release"];

    /// <summary>
    /// Filename globs that may never be written, whatever the user consents to. These carry
    /// credentials and deployment configuration; a "minor bug fix" has no business rewriting them.
    /// </summary>
    public string[] BlockedPathPatterns { get; set; } =
    [
        "appsettings*.json",
        "local.settings.json",
        "secrets.json",
        "*.pubxml",
        "*.arm.json",
        "serviceDependencies*.json",
        "*.env",
        "web.config",
        "app.config",
    ];

    /// <summary>Extensions that may be written. Anything else is refused.</summary>
    public string[] AllowedExtensions { get; set; } = [".cs", ".cshtml", ".razor", ".js", ".css", ".sql", ".md"];

    /// <summary>Upper bound on how much of a file one fix may replace, as a guard against whole-file rewrites.</summary>
    public int MaxChangedLines { get; set; } = 120;

    /// <summary>How long an unapplied proposal stays valid.</summary>
    public int ProposalLifetimeMinutes { get; set; } = 30;
}

public sealed class AppInsightsOptions
{
    /// <summary>
    /// Full ARM resource id of the Application Insights component, e.g.
    /// /subscriptions/{sub}/resourceGroups/{rg}/providers/microsoft.insights/components/{name}.
    /// Preferred over <see cref="WorkspaceId"/> because it keeps classic AI table names.
    /// </summary>
    public string? ResourceId { get; set; }

    /// <summary>Log Analytics workspace GUID. Used when <see cref="ResourceId"/> is not set.</summary>
    public string? WorkspaceId { get; set; }

    /// <summary>Default lookback for telemetry queries when the caller does not specify one.</summary>
    public int DefaultLookbackHours { get; set; } = 24;

    /// <summary>Hard cap on rows returned to the agent, to keep tool results readable.</summary>
    public int MaxRows { get; set; } = 50;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ResourceId) || !string.IsNullOrWhiteSpace(WorkspaceId);
}

public sealed class AzureDevOpsOptions
{
    /// <summary>Organisation name only, e.g. "risebird" for https://dev.azure.com/risebird.</summary>
    public string? Organization { get; set; }

    public string? Project { get; set; }

    /// <summary>Personal access token with **Work Items: Read** scope only.</summary>
    public string? PersonalAccessToken { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Organization) &&
        !string.IsNullOrWhiteSpace(Project) &&
        !string.IsNullOrWhiteSpace(PersonalAccessToken);
}

public sealed class GitHubOptions
{
    /// <summary>Default repository in "owner/name" form, so callers can omit it.</summary>
    public string? DefaultRepository { get; set; }

    /// <summary>Fine-grained token with read-only Issues and Pull requests access.</summary>
    public string? Token { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Token);
}

public sealed class RepositoryOptions
{
    /// <summary>Absolute path to the checkout the stack traces refer to.</summary>
    public string? RootPath { get; set; }

    /// <summary>Path to git.exe. Left null, the server probes PATH and the usual Windows install locations.</summary>
    public string? GitExecutable { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(RootPath) && Directory.Exists(RootPath);
}
