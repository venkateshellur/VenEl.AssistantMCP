using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenEl.MCP.Azure.Services;
using VenEl.MCP.Core.Dispatcher;

namespace VenEl.MCP.Azure.Tools;

public sealed class AzureListProjectsActionHandler(IAzureHttpClient client, ILogger<AzureListProjectsActionHandler> logger) : IActionHandler<AzureCommandArgs>
{
    public string ActionName => "azure_list_projects";

    public string? Validate(AzureCommandArgs args) => null;

    public async Task<string> HandleAsync(AzureCommandArgs args, CancellationToken ct)
    {
        int top = Math.Clamp(args.Top ?? 50, 1, 100);
        logger.LogDebug("Listing Azure DevOps projects (top={Top})", top);
        return await client.GetAsync(AzureProduct.DevOps, $"_apis/projects?$top={top}", "7.1", null, ct);
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
        return await client.GetAsync(AzureProduct.DevOps, $"{args.Project}/_apis/git/repositories", "7.1", null, ct);
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
            "7.1", null, ct);
    }
}

public sealed class AzureGetPullRequestActionHandler(IAzureHttpClient client, ILogger<AzureGetPullRequestActionHandler> logger) : IActionHandler<AzureCommandArgs>
{
    public string ActionName => "azure_get_pull_request";

    public string? Validate(AzureCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Project)) return "Missing required parameter 'Project'.";
        if (string.IsNullOrWhiteSpace(args.RepositoryId)) return "Missing required parameter 'RepositoryId'.";
        if (args.PullRequestId == null) return "Missing required parameter 'PullRequestId'.";
        return null;
    }

    public async Task<string> HandleAsync(AzureCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Getting PR {PullRequestId} for {Project}/{Repo}", args.PullRequestId, args.Project, args.RepositoryId);
        return await client.GetAsync(AzureProduct.DevOps,
            $"{args.Project}/_apis/git/repositories/{args.RepositoryId}/pullrequests/{args.PullRequestId}",
            "7.1", null, ct);
    }
}

public sealed class AzureCreatePullRequestActionHandler(IAzureHttpClient client, ILogger<AzureCreatePullRequestActionHandler> logger) : IActionHandler<AzureCommandArgs>
{
    public string ActionName => "azure_create_pull_request";

    public string? Validate(AzureCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Project)) return "Missing required parameter 'Project'.";
        if (string.IsNullOrWhiteSpace(args.RepositoryId)) return "Missing required parameter 'RepositoryId'.";
        if (string.IsNullOrWhiteSpace(args.SourceRefName)) return "Missing required parameter 'SourceRefName'.";
        if (string.IsNullOrWhiteSpace(args.TargetRefName)) return "Missing required parameter 'TargetRefName'.";
        if (string.IsNullOrWhiteSpace(args.Title)) return "Missing required parameter 'Title'.";
        return null;
    }

    public async Task<string> HandleAsync(AzureCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Creating PR for {Project}/{Repo}", args.Project, args.RepositoryId);
        var payload = new
        {
            sourceRefName = args.SourceRefName,
            targetRefName = args.TargetRefName,
            title = args.Title,
            description = args.Description ?? string.Empty
        };
        
        return await client.PostAsync(AzureProduct.DevOps,
            $"{args.Project}/_apis/git/repositories/{args.RepositoryId}/pullrequests",
            payload, "7.1", null, ct);
    }
}
