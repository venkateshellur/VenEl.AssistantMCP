namespace VenEl.AssistantMCP.Atlassian.Services;

/// <summary>
/// Holds Atlassian credentials supplied at runtime via the <c>atlassian_configure</c> MCP tool.
/// Registered as a singleton so the values persist for the lifetime of the server process.
/// Session credentials take precedence over appsettings.json values.
/// </summary>
public sealed class AtlassianSessionCredentials
{
    /// <summary>Atlassian Cloud domain, e.g. "yourname.atlassian.net".</summary>
    public string? Domain { get; set; }

    /// <summary>Bitbucket workspace slug.</summary>
    public string? BitbucketWorkspace { get; set; }

    /// <summary>Atlassian account email (for API Token auth).</summary>
    public string? Email { get; set; }

    /// <summary>Atlassian API token (for API Token auth).</summary>
    public string? ApiToken { get; set; }

    /// <summary>Returns true when session has valid API Token credentials.</summary>
    public bool HasApiToken =>
        !string.IsNullOrWhiteSpace(Email) &&
        !string.IsNullOrWhiteSpace(ApiToken);
}
