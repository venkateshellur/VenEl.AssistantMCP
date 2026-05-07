namespace VenEl.MCPAssistant.Atlassian.Configuration;

/// <summary>
/// Root configuration for the Atlassian feature module.
/// Bound from the "Atlassian" section in appsettings.json.
/// Env-var overrides use the VENEL_ prefix, e.g. VENEL_Atlassian__ApiToken__Token.
/// </summary>
public sealed class AtlassianOptions
{
    public const string SectionName = "Atlassian";

    /// <summary>
    /// Your Atlassian Cloud domain without https://, e.g. "yourname.atlassian.net".
    /// Used as the base for Jira and Confluence URLs.
    /// </summary>
    public string Domain { get; set; } = "";

    /// <summary>
    /// Bitbucket workspace slug (the short name shown in Bitbucket URLs).
    /// Required only for Bitbucket tools.
    /// </summary>
    public string BitbucketWorkspace { get; set; } = "";

    /// <summary>
    /// Which auth method to try first: "ApiToken" (default) or "OAuth".
    /// If the preferred method's credentials are missing, the other is used automatically.
    /// </summary>
    public string PreferredAuthMethod { get; set; } = "ApiToken";

    /// <summary>API Token credentials (Basic Auth).</summary>
    public ApiTokenOptions ApiToken { get; set; } = new();

    /// <summary>OAuth 2.0 Client Credentials.</summary>
    public OAuthOptions OAuth { get; set; } = new();
}
