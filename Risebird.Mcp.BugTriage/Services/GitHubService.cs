using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Risebird.Mcp.BugTriage.Configuration;

namespace Risebird.Mcp.BugTriage.Services;

/// <summary>
/// Read-only GitHub issue and pull request access. Only GET endpoints are reachable from here.
/// </summary>
public sealed class GitHubService(HttpClient http, IOptions<TriageOptions> options)
{
    private readonly GitHubOptions _options = options.Value.GitHub;

    public bool IsConfigured => _options.IsConfigured;

    /// <summary>Resolves an explicit "owner/name" argument, falling back to the configured default.</summary>
    public string ResolveRepository(string? repository)
    {
        var resolved = string.IsNullOrWhiteSpace(repository) ? _options.DefaultRepository : repository;

        if (string.IsNullOrWhiteSpace(resolved))
        {
            throw new InvalidOperationException(
                "No repository given and Triage:GitHub:DefaultRepository is not set. Pass repository as \"owner/name\".");
        }

        if (resolved.Count(c => c == '/') != 1)
        {
            throw new InvalidOperationException($"Repository must be in \"owner/name\" form, got \"{resolved}\".");
        }

        return resolved;
    }

    private void EnsureAuthenticated()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "GitHub is not configured. Set Triage:GitHub:Token (a fine-grained read-only token).");
        }

        http.DefaultRequestHeaders.Authorization ??= new AuthenticationHeaderValue("Bearer", _options.Token);
    }

    public async Task<JsonElement> GetIssueAsync(string repository, int number, CancellationToken cancellationToken)
    {
        EnsureAuthenticated();
        return await GetJsonAsync($"repos/{repository}/issues/{number}", cancellationToken);
    }

    public async Task<JsonElement> GetIssueCommentsAsync(string repository, int number, CancellationToken cancellationToken)
    {
        EnsureAuthenticated();
        return await GetJsonAsync($"repos/{repository}/issues/{number}/comments?per_page=100", cancellationToken);
    }

    public async Task<JsonElement> SearchIssuesAsync(string query, int maxResults, CancellationToken cancellationToken)
    {
        EnsureAuthenticated();
        var encoded = Uri.EscapeDataString(query);
        return await GetJsonAsync($"search/issues?q={encoded}&per_page={maxResults}", cancellationToken);
    }

    private async Task<JsonElement> GetJsonAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"GitHub returned {(int)response.StatusCode} {response.ReasonPhrase}. " +
                (body.Length <= 500 ? body : body[..500] + "…"));
        }

        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
    }
}
