namespace VenEl.MCPAssistant.Azure.Configuration;

/// <summary>
/// Credentials for Azure Personal Access Token (PAT) authentication.
/// </summary>
public sealed class PatOptions
{
    /// <summary>The Personal Access Token (PAT) generated from Azure DevOps.</summary>
    public string Token { get; set; } = "";

    /// <summary>Returns true when Token is present.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Token);
}
