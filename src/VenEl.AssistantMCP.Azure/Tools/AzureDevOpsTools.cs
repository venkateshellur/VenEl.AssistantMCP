using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenEl.AssistantMCP.Azure.Services;
using VenEl.AssistantMCP.Core.Dispatcher;

namespace VenEl.AssistantMCP.Azure.Tools;

public sealed class AzureListProjectsActionHandler(IAzureHttpClient client, ILogger<AzureListProjectsActionHandler> logger) : IActionHandler<AzureCommandArgs>
{
    public string ActionName => "azure_list_projects";

    public string? Validate(AzureCommandArgs args) => null;

    public async Task<string> HandleAsync(AzureCommandArgs args, CancellationToken ct)
    {
        int top = Math.Clamp(args.Top ?? 50, 1, 100);
        logger.LogDebug("Listing Azure DevOps projects (top={Top})", top);
        return await client.GetAsync(AzureProduct.DevOps, $"_apis/projects?$top={top}", "7.1", ct);
    }
}

public sealed class AzureListReposActionHandler(IAzureHttpClient client, ILogger<AzureListReposActionHandler> logger) : IActionHandler<AzureCommandArgs>
{
    public string ActionName => "azure_list_repos";

    public string? Validate(AzureCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Project)) return "Missing required parameter 'Project'.";
        return null;
    }

    public async Task<string> HandleAsync(AzureCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Listing Azure DevOps repos for project {Project}", args.Project);
        return await client.GetAsync(AzureProduct.DevOps, $"{args.Project}/_apis/git/repositories", "7.1", ct);
    }
}

public sealed class AzureListPullRequestsActionHandler(IAzureHttpClient client, ILogger<AzureListPullRequestsActionHandler> logger) : IActionHandler<AzureCommandArgs>
{
    public string ActionName => "azure_list_pull_requests";

    public string? Validate(AzureCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Project)) return "Missing required parameter 'Project'.";
        if (string.IsNullOrWhiteSpace(args.RepositoryId)) return "Missing required parameter 'RepositoryId'.";
        return null;
    }

    public async Task<string> HandleAsync(AzureCommandArgs args, CancellationToken ct)
    {
        int top = Math.Clamp(args.Top ?? 25, 1, 100);
        logger.LogDebug("Listing active PRs for {Project}/{Repo}", args.Project, args.RepositoryId);
        return await client.GetAsync(AzureProduct.DevOps,
            $"{args.Project}/_apis/git/repositories/{args.RepositoryId}/pullrequests?searchCriteria.status=active&$top={top}",
            "7.1", ct);
    }
}
