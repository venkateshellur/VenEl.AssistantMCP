namespace VenEl.MCPAssistant.Atlassian.Configuration;

/// <summary>
/// Credentials for Atlassian API Token (Basic Auth) authentication.
/// Generate a token at: https://id.atlassian.net/manage-profile/security/api-tokens
/// </summary>
public sealed class ApiTokenOptions
{
    /// <summary>The Atlassian account email address.</summary>
    public string Email { get; set; } = "";

    /// <summary>The API token generated from id.atlassian.net.</summary>
    public string Token { get; set; } = "";

    /// <summary>Returns true when both Email and Token are present.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Email) &&
        !string.IsNullOrWhiteSpace(Token);
}
