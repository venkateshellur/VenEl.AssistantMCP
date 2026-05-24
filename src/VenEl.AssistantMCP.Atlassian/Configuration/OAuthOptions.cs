namespace VenEl.AssistantMCP.Atlassian.Configuration;

/// <summary>
/// Credentials for Atlassian OAuth 2.0 (Client Credentials) authentication.
/// Register your app at: https://developer.atlassian.com/console/myapps/
/// One set of credentials covers Jira, Confluence, and Bitbucket.
/// </summary>
public sealed class OAuthOptions
{
    /// <summary>OAuth 2.0 Client ID from the Atlassian Developer Console.</summary>
    public string ClientId { get; set; } = "";

    /// <summary>OAuth 2.0 Client Secret from the Atlassian Developer Console.</summary>
    public string ClientSecret { get; set; } = "";

    /// <summary>Returns true when both ClientId and ClientSecret are present.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret);
}
