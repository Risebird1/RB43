using Azure;
using Azure.Identity;

namespace Risebird.Mcp.BugTriage.Tools;

/// <summary>
/// Converts the expected failures - missing configuration, rejected queries, upstream HTTP errors,
/// Azure sign-in problems - into readable tool output.
/// </summary>
/// <remarks>
/// Without this, an unhandled exception reaches the MCP client as a bare "An error occurred invoking
/// '&lt;tool&gt;'", which tells the user nothing about which setting is missing. Genuinely unexpected
/// exceptions are deliberately left to propagate.
/// </remarks>
public static class ToolGuard
{
    /// <summary>
    /// Returns a readable explanation for an expected failure, or null if the exception is genuinely
    /// unexpected and should propagate.
    /// </summary>
    public static string? Describe(Exception ex) => ex switch
    {
        // Thrown by the services for "not configured" and for a rejected KQL query.
        InvalidOperationException => ex.Message,

        // Azure.Identity could not find or use a credential.
        CredentialUnavailableException =>
            $"Azure sign-in failed: {ex.Message}\n\n" +
            "Application Insights uses DefaultAzureCredential. Run 'az login', or sign in to Visual Studio " +
            "with an account that has the Monitoring Reader role on the App Insights component.",

        AuthenticationFailedException =>
            $"Azure authentication failed: {ex.Message}\n\nTry 'az login' and confirm the account can read the " +
            "App Insights component named in Triage:AppInsights:ResourceId.",

        // The Logs query API rejected the request - bad resource id, no permission, malformed KQL.
        RequestFailedException requestFailed =>
            $"Application Insights returned {requestFailed.Status}: {requestFailed.Message}\n\n" +
            "Check that Triage:AppInsights:ResourceId is the full ARM resource id and that your account has " +
            "the Monitoring Reader role on it.",

        // Azure DevOps and GitHub surface their failures as HttpRequestException with the body attached.
        HttpRequestException => ex.Message,

        // Bad repository format, malformed arguments.
        ArgumentException => ex.Message,

        _ => null,
    };
}
