using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenEl.AssistantMCP.Core.Dispatcher;
using VenEl.AssistantMCP.GitHub.Services;

namespace VenEl.AssistantMCP.GitHub.Tools;

public sealed class GitHubListWorkflowsActionHandler(IGitHubHttpClient client, ILogger<GitHubListWorkflowsActionHandler> logger) : IActionHandler<GitHubCommandArgs>
{
    public string ActionName => "github_list_workflows";

    public string? Validate(GitHubCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Owner)) return "Missing required parameter 'Owner'.";
        if (string.IsNullOrWhiteSpace(args.Repo)) return "Missing required parameter 'Repo'.";
        return null;
    }

    public async Task<string> HandleAsync(GitHubCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Listing GitHub workflows for {Owner}/{Repo}", args.Owner, args.Repo);
        return await client.GetAsync($"repos/{args.Owner}/{args.Repo}/actions/workflows", ct);
    }
}

public sealed class GitHubTriggerWorkflowActionHandler(IGitHubHttpClient client, ILogger<GitHubTriggerWorkflowActionHandler> logger) : IActionHandler<GitHubCommandArgs>
{
    public string ActionName => "github_trigger_workflow";

    public string? Validate(GitHubCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Owner)) return "Missing required parameter 'Owner'.";
        if (string.IsNullOrWhiteSpace(args.Repo)) return "Missing required parameter 'Repo'.";
        if (string.IsNullOrWhiteSpace(args.WorkflowId)) return "Missing required parameter 'WorkflowId'.";
        if (string.IsNullOrWhiteSpace(args.Ref)) return "Missing required parameter 'Ref'.";
        return null;
    }

    public async Task<string> HandleAsync(GitHubCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Triggering workflow {WorkflowId} for {Owner}/{Repo} on {Ref}", args.WorkflowId, args.Owner, args.Repo, args.Ref);
        var payload = new { @ref = args.Ref };
        return await client.PostAsync($"repos/{args.Owner}/{args.Repo}/actions/workflows/{args.WorkflowId}/dispatches", payload, ct);
    }
}
