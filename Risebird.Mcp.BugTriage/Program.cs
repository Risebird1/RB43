using Azure.Identity;
using Azure.Monitor.Query;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using Risebird.Mcp.BugTriage.Configuration;
using Risebird.Mcp.BugTriage.Services;
using Risebird.Mcp.BugTriage.Tools;

// An MCP client launches this process with an arbitrary working directory, so the content root has
// to be pinned to the binary's own folder or appsettings.json is silently never found.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

// stdio is the MCP transport: ANY stray byte on stdout corrupts the protocol frame.
// Every log line has to go to stderr.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(console => console.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Configuration.AddUserSecrets<Program>(optional: true);
builder.Configuration.AddEnvironmentVariables("RISEBIRD_TRIAGE_");

builder.Services.Configure<TriageOptions>(builder.Configuration.GetSection(TriageOptions.SectionName));

builder.Services.AddSingleton(sp =>
{
    // DefaultAzureCredential picks up the developer's az login / VS account locally and a
    // managed identity when this runs on a build agent.
    var credential = new DefaultAzureCredential();
    return new LogsQueryClient(credential);
});

builder.Services.AddSingleton<AppInsightsService>();
builder.Services.AddSingleton<RepositoryService>();
builder.Services.AddSingleton<FixProposalStore>();
builder.Services.AddSingleton<FixService>();

builder.Services.AddHttpClient<AzureDevOpsService>((sp, http) =>
{
    var options = sp.GetRequiredService<IOptions<TriageOptions>>().Value.AzureDevOps;
    if (!string.IsNullOrWhiteSpace(options.Organization))
    {
        http.BaseAddress = new Uri($"https://dev.azure.com/{options.Organization}/");
    }

    http.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<GitHubService>(http =>
{
    http.BaseAddress = new Uri("https://api.github.com/");
    http.DefaultRequestHeaders.UserAgent.ParseAdd("Risebird-BugTriage-MCP/1.0");
    http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    http.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new() { Name = "risebird-bug-triage", Version = "1.0.0" };
    })
    .WithStdioServerTransport()
    .WithToolsFromAssembly()
    // Without this, a missing setting surfaces to the user as a bare "An error occurred invoking
    // '<tool>'", which says nothing about what to configure. Expected failures become readable text;
    // anything genuinely unexpected still propagates.
    .WithRequestFilters(filters => filters.AddCallToolFilter(next => async (context, cancellationToken) =>
    {
        try
        {
            return await next(context, cancellationToken);
        }
        catch (Exception ex) when (ToolGuard.Describe(ex) is { } explanation)
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = explanation }],
            };
        }
    }));

await builder.Build().RunAsync();
