using System.Text.Json;
using System.Text.RegularExpressions;
using Azure;
using Azure.Core;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Microsoft.Extensions.Options;
using Risebird.Mcp.BugTriage.Configuration;

namespace Risebird.Mcp.BugTriage.Services;

/// <summary>
/// Read-only access to the Risebird Application Insights component.
/// All queries go through <see cref="RunQueryAsync"/> so the KQL guard cannot be bypassed.
/// </summary>
public sealed partial class AppInsightsService(LogsQueryClient client, IOptions<TriageOptions> options)
{
    private readonly AppInsightsOptions _options = options.Value.AppInsights;

    /// <summary>
    /// KQL operators that read from outside the workspace or write to it. The Logs query API is
    /// already read-only, but externaldata/http_request can be used to exfiltrate query results,
    /// so they are blocked here as well.
    /// </summary>
    /// <remarks>
    /// The dot-prefixed management commands need their own alternative with a lookbehind rather than
    /// a leading \b: '.' is not a word character, so '\b\.create' never matches at the start of a
    /// query or after whitespace.
    /// </remarks>
    [GeneratedRegex(
        @"\b(?:externaldata|evaluate\s+http_request)\b|(?<![\w.])\.(?:create|set|append|drop|alter|ingest|replace|rename|move|purge|execute)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex ForbiddenKqlRegex { get; }

    public bool IsConfigured => _options.IsConfigured;

    public int DefaultLookbackHours => _options.DefaultLookbackHours;

    public int MaxRows => _options.MaxRows;

    /// <summary>
    /// Runs a KQL query and returns the first table as a list of column-keyed dictionaries.
    /// </summary>
    public async Task<IReadOnlyList<Dictionary<string, object?>>> RunQueryAsync(
        string kql,
        TimeSpan lookback,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "Application Insights is not configured. Set Triage:AppInsights:ResourceId (preferred) or " +
                "Triage:AppInsights:WorkspaceId.");
        }

        ValidateKql(kql);

        var timeRange = new QueryTimeRange(lookback);

        Response<LogsQueryResult> response;
        if (!string.IsNullOrWhiteSpace(_options.ResourceId))
        {
            response = await client.QueryResourceAsync(
                new ResourceIdentifier(_options.ResourceId),
                kql,
                timeRange,
                cancellationToken: cancellationToken);
        }
        else
        {
            response = await client.QueryWorkspaceAsync(
                _options.WorkspaceId,
                kql,
                timeRange,
                cancellationToken: cancellationToken);
        }

        return Materialize(response.Value.Table);
    }

    private static List<Dictionary<string, object?>> Materialize(LogsTable table)
    {
        var results = new List<Dictionary<string, object?>>(table.Rows.Count);

        foreach (var row in table.Rows)
        {
            var record = new Dictionary<string, object?>(table.Columns.Count, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < table.Columns.Count; i++)
            {
                var value = row[i];
                // Skip nulls and empty strings so the agent is not handed a wall of empty columns.
                if (value is null || (value is string s && string.IsNullOrWhiteSpace(s)))
                {
                    continue;
                }

                record[table.Columns[i].Name] = value;
            }

            results.Add(record);
        }

        return results;
    }

    /// <summary>
    /// Rejects KQL that reads from or writes to anything outside the telemetry workspace. The Logs
    /// query API is read-only regardless; this additionally stops a query being used to pull in
    /// external data or push results out.
    /// </summary>
    public static void ValidateKql(string kql)
    {
        var forbidden = ForbiddenKqlRegex.Match(kql);
        if (forbidden.Success)
        {
            throw new InvalidOperationException(
                $"Query rejected: '{forbidden.Value}' is not allowed. This server is read-only and does not " +
                "permit operators that read from or write to resources outside the telemetry workspace.");
        }
    }

    /// <summary>Escapes a value for safe interpolation into a KQL string literal.</summary>
    public static string EscapeKqlString(string value) =>
        value.Replace(@"\", @"\\", StringComparison.Ordinal)
             .Replace("\"", "\\\"", StringComparison.Ordinal);

    public static string ToJson(object? value) =>
        JsonSerializer.Serialize(value, JsonOptions);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        // Records and anonymous types otherwise mix PascalCase and camelCase in the same result.
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };
}
