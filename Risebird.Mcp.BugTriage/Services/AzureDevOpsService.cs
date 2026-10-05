using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Risebird.Mcp.BugTriage.Configuration;

namespace Risebird.Mcp.BugTriage.Services;

/// <summary>
/// Read-only Azure DevOps work item access. Only GET endpoints and the WIQL query endpoint are
/// reachable from here; nothing in this class can create, update or transition a work item.
/// </summary>
public sealed class AzureDevOpsService(HttpClient http, IOptions<TriageOptions> options)
{
    private const string ApiVersion = "7.1";

    private readonly AzureDevOpsOptions _options = options.Value.AzureDevOps;

    public bool IsConfigured => _options.IsConfigured;

    private void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "Azure DevOps is not configured. Set Triage:AzureDevOps:Organization, :Project and " +
                ":PersonalAccessToken (a read-only Work Items PAT).");
        }

        if (http.DefaultRequestHeaders.Authorization is null)
        {
            // ADO basic auth: empty username, PAT as password.
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_options.PersonalAccessToken}"));
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }
    }

    public async Task<JsonElement> GetWorkItemAsync(int id, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        var url = $"{_options.Project}/_apis/wit/workitems/{id}?$expand=all&api-version={ApiVersion}";
        return await GetJsonAsync(url, cancellationToken);
    }

    public async Task<JsonElement> GetWorkItemCommentsAsync(int id, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        // Comments are still on a preview api-version as of 7.1.
        var url = $"{_options.Project}/_apis/wit/workItems/{id}/comments?api-version=7.1-preview.3";
        return await GetJsonAsync(url, cancellationToken);
    }

    /// <summary>
    /// Runs a WIQL query and hydrates the matching work items, because WIQL itself returns ids only.
    /// </summary>
    public async Task<JsonElement> QueryWorkItemsAsync(string wiql, int maxResults, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        var queryUrl = $"{_options.Project}/_apis/wit/wiql?$top={maxResults}&api-version={ApiVersion}";
        using var response = await http.PostAsJsonAsync(queryUrl, new { query = wiql }, cancellationToken);
        await ThrowOnFailureAsync(response, cancellationToken);

        var queryResult = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        if (!queryResult.TryGetProperty("workItems", out var idElements) || idElements.GetArrayLength() == 0)
        {
            return queryResult;
        }

        var ids = idElements.EnumerateArray()
            .Take(maxResults)
            .Select(item => item.GetProperty("id").GetInt32())
            .ToArray();

        var fields = string.Join(',',
            "System.Id", "System.Title", "System.State", "System.WorkItemType",
            "System.AssignedTo", "System.CreatedDate", "System.ChangedDate",
            "System.Tags", "Microsoft.VSTS.Common.Priority", "Microsoft.VSTS.Common.Severity");

        var batchUrl = $"_apis/wit/workitems?ids={string.Join(',', ids)}&fields={fields}&api-version={ApiVersion}";
        return await GetJsonAsync(batchUrl, cancellationToken);
    }

    private async Task<JsonElement> GetJsonAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(url, cancellationToken);
        await ThrowOnFailureAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
    }

    private static async Task ThrowOnFailureAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"Azure DevOps returned {(int)response.StatusCode} {response.ReasonPhrase}. {Truncate(body, 500)}");
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}
