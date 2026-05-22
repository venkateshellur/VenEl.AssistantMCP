namespace VenEl.MCPAssistant.Databricks.Configuration;

public class DatabricksOptions
{
    public const string SectionName = "Databricks";

    /// <summary>
    /// The Databricks workspace URL. E.g., https://adb-1234567890123456.7.azuredatabricks.net
    /// </summary>
    public string? WorkspaceUrl { get; set; }

    /// <summary>
    /// The Personal Access Token (PAT) to authenticate with the Workspace.
    /// </summary>
    public string? PersonalAccessToken { get; set; }
}
