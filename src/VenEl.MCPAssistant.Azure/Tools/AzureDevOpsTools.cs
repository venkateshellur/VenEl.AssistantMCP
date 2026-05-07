using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using VenEl.MCPAssistant.Azure.Services;

namespace VenEl.MCPAssistant.Azure.Tools;

/// <summary>MCP tools for Azure DevOps REST API.</summary>
[McpServerToolType]
public sealed class AzureDevOpsTools(
    IAzureHttpClient client,
    ILogger<AzureDevOpsTools> logger)
{
    // ═════════════════════════════════════════════════════════════════════════
    // Projects
    // ═════════════════════════════════════════════════════════════════════════

    [McpServerTool(Name = "azure_list_projects")]
    [Description("Lists all projects in the configured Azure DevOps organization.")]
    public async Task<string> AzureListProjectsAsync(
        [Description("Maximum projects to return (default 50).")] int top = 50,
        CancellationToken cancellationToken = default)
    {
        top = Math.Clamp(top, 1, 100);
        logger.LogDebug("Listing Azure DevOps projects (top={Top})", top);
        return await client.GetAsync(AzureProduct.DevOps,
            $"_apis/projects?$top={top}", "7.1", cancellationToken);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Repositories
    // ═════════════════════════════════════════════════════════════════════════

    [McpServerTool(Name = "azure_list_repos")]
    [Description("Lists all Git repositories within a specific Azure DevOps project.")]
    public async Task<string> AzureListReposAsync(
        [Description("The name or ID of the project.")] string project,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Listing Azure DevOps repos for project {Project}", project);
        return await client.GetAsync(AzureProduct.DevOps,
            $"{project}/_apis/git/repositories", "7.1", cancellationToken);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Pull Requests
    // ═════════════════════════════════════════════════════════════════════════

    [McpServerTool(Name = "azure_list_pull_requests")]
    [Description("Lists active pull requests for a specific repository.")]
    public async Task<string> AzureListPullRequestsAsync(
        [Description("The name or ID of the project.")] string project,
        [Description("The name or ID of the repository.")] string repositoryId,
        [Description("Maximum PRs to return (default 25).")] int top = 25,
        CancellationToken cancellationToken = default)
    {
        top = Math.Clamp(top, 1, 100);
        logger.LogDebug("Listing active PRs for {Project}/{Repo}", project, repositoryId);
        return await client.GetAsync(AzureProduct.DevOps,
            $"{project}/_apis/git/repositories/{repositoryId}/pullrequests?searchCriteria.status=active&$top={top}",
            "7.1", cancellationToken);
    }
}
