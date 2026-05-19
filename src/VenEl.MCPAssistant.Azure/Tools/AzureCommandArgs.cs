using System.ComponentModel;

namespace VenEl.MCPAssistant.Azure.Tools;

public class AzureCommandArgs
{
    [Description("The action to perform. Options: azure_list_projects, azure_list_repos, azure_list_pull_requests, azure_configure, azure_show_config")]
    public string Action { get; set; } = string.Empty;

    // DevOps specific
    [Description("Maximum projects or PRs to return (default 50 or 25). Used by azure_list_projects, azure_list_pull_requests.")]
    public int? Top { get; set; }

    [Description("The name or ID of the project. Used by azure_list_repos, azure_list_pull_requests.")]
    public string? Project { get; set; }

    [Description("The name or ID of the repository. Used by azure_list_pull_requests.")]
    public string? RepositoryId { get; set; }

    // Setup specific
    [Description("Your Azure DevOps organization URL, e.g., 'https://dev.azure.com/your-org'. Used by azure_configure.")]
    public string? OrganizationUrl { get; set; }

    [Description("Your Azure Personal Access Token (PAT). Used by azure_configure.")]
    public string? PatToken { get; set; }
}
