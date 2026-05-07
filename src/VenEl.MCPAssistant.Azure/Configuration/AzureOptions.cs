namespace VenEl.MCPAssistant.Azure.Configuration;

public sealed class AzureOptions
{
    public const string SectionName = "Azure";

    /// <summary>Your Azure DevOps organization URL, e.g., 'https://dev.azure.com/your-org'.</summary>
    public string OrganizationUrl { get; set; } = "";

    /// <summary>Auth method: set to 'Pat' or 'OAuth'.</summary>
    public string PreferredAuthMethod { get; set; } = "Pat";

    public PatOptions Pat { get; set; } = new();
}
