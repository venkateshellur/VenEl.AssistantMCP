namespace VenEl.MCPAssistant.Azure.Services;

/// <summary>
/// Holds Azure credentials supplied at runtime via the <c>azure_configure</c> MCP tool.
/// Session credentials take precedence over appsettings.json values.
/// </summary>
public sealed class AzureSessionCredentials
{
    /// <summary>Azure DevOps organization URL.</summary>
    public string? OrganizationUrl { get; set; }

    /// <summary>Azure Personal Access Token (PAT).</summary>
    public string? PatToken { get; set; }

    /// <summary>Returns true when session has valid PAT credentials.</summary>
    public bool HasPatToken => !string.IsNullOrWhiteSpace(PatToken);
}
