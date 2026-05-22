namespace VenEl.MCPAssistant.Atlassian.Services;

/// <summary>
/// Identifies the Atlassian product being called.
/// Determines the base URL used by <see cref="IAtlassianHttpClient"/>.
/// </summary>
public enum AtlassianProduct
{
    /// <summary>Jira Cloud REST API v3 — https://{domain}/rest/api/3/</summary>
    Jira,

    /// <summary>Confluence Cloud REST API — https://{domain}/wiki/rest/api/</summary>
    Confluence,

    /// <summary>Bitbucket Cloud REST API v2 — https://api.bitbucket.org/2.0/</summary>
    Bitbucket,

    /// <summary>Jira Agile REST API v1.0 — https://{domain}/rest/agile/1.0/</summary>
    JiraAgile,
}
