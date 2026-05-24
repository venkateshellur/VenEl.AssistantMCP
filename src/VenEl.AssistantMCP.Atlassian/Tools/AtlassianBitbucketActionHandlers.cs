using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VenEl.AssistantMCP.Atlassian.Configuration;
using VenEl.AssistantMCP.Atlassian.Services;
using VenEl.AssistantMCP.Core.Dispatcher;

namespace VenEl.AssistantMCP.Atlassian.Tools;

internal static class BitbucketHelper
{
    public static string RequireWorkspace(AtlassianSessionCredentials session, IOptions<AtlassianOptions> options)
    {
        var workspace = !string.IsNullOrWhiteSpace(session.BitbucketWorkspace)
            ? session.BitbucketWorkspace
            : options.Value.BitbucketWorkspace;

        if (string.IsNullOrWhiteSpace(workspace))
            throw new InvalidOperationException(
                "[CONFIG ERROR] Bitbucket workspace is not set. " +
                "Call 'atlassian_configure' with your BitbucketWorkspace, " +
                "or set Atlassian:BitbucketWorkspace in appsettings.json.");
        return workspace;
    }
}

public sealed class BitbucketListRepositoriesActionHandler(
    IAtlassianHttpClient client,
    IOptions<AtlassianOptions> options,
    AtlassianSessionCredentials session,
    ILogger<BitbucketListRepositoriesActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "bitbucket_list_repositories";

    public string? Validate(AtlassianCommandArgs args) => null;

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        int pagelen = Math.Clamp(args.Pagelen ?? 25, 1, 100);
        var workspace = BitbucketHelper.RequireWorkspace(session, options);
        logger.LogDebug("Listing Bitbucket repos for workspace {Workspace}", workspace);
        return await client.GetAsync(AtlassianProduct.Bitbucket,
            $"repositories/{workspace}?pagelen={pagelen}&fields=values.slug,values.full_name,values.description,values.language,values.is_private,values.clone",
            ct);
    }
}

public sealed class BitbucketGetRepositoryActionHandler(
    IAtlassianHttpClient client,
    IOptions<AtlassianOptions> options,
    AtlassianSessionCredentials session,
    ILogger<BitbucketGetRepositoryActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "bitbucket_get_repository";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.RepoSlug)) return "Missing required parameter 'RepoSlug'.";
        return null;
    }

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        var workspace = BitbucketHelper.RequireWorkspace(session, options);
        logger.LogDebug("Getting Bitbucket repo {Workspace}/{Repo}", workspace, args.RepoSlug);
        return await client.GetAsync(AtlassianProduct.Bitbucket, $"repositories/{workspace}/{args.RepoSlug}", ct);
    }
}

public sealed class BitbucketListBranchesActionHandler(
    IAtlassianHttpClient client,
    IOptions<AtlassianOptions> options,
    AtlassianSessionCredentials session,
    ILogger<BitbucketListBranchesActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "bitbucket_list_branches";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.RepoSlug)) return "Missing required parameter 'RepoSlug'.";
        return null;
    }

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        int pagelen = Math.Clamp(args.Pagelen ?? 25, 1, 100);
        var workspace = BitbucketHelper.RequireWorkspace(session, options);
        logger.LogDebug("Listing branches for {Workspace}/{Repo}", workspace, args.RepoSlug);
        return await client.GetAsync(AtlassianProduct.Bitbucket,
            $"repositories/{workspace}/{args.RepoSlug}/refs/branches?pagelen={pagelen}", ct);
    }
}

public sealed class BitbucketListPullRequestsActionHandler(
    IAtlassianHttpClient client,
    IOptions<AtlassianOptions> options,
    AtlassianSessionCredentials session,
    ILogger<BitbucketListPullRequestsActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "bitbucket_list_pull_requests";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.RepoSlug)) return "Missing required parameter 'RepoSlug'.";
        return null;
    }

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        int pagelen = Math.Clamp(args.Pagelen ?? 25, 1, 100);
        string state = args.State ?? "OPEN";
        var workspace = BitbucketHelper.RequireWorkspace(session, options);
        logger.LogDebug("Listing {State} PRs for {Workspace}/{Repo}", state, workspace, args.RepoSlug);
        return await client.GetAsync(AtlassianProduct.Bitbucket,
            $"repositories/{workspace}/{args.RepoSlug}/pullrequests?state={state}&pagelen={pagelen}", ct);
    }
}

public sealed class BitbucketGetPullRequestActionHandler(
    IAtlassianHttpClient client,
    IOptions<AtlassianOptions> options,
    AtlassianSessionCredentials session,
    ILogger<BitbucketGetPullRequestActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "bitbucket_get_pull_request";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.RepoSlug)) return "Missing required parameter 'RepoSlug'.";
        if (!args.PullRequestId.HasValue) return "Missing required parameter 'PullRequestId'.";
        return null;
    }

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        var workspace = BitbucketHelper.RequireWorkspace(session, options);
        logger.LogDebug("Getting PR #{PrId} for {Workspace}/{Repo}", args.PullRequestId, workspace, args.RepoSlug);
        return await client.GetAsync(AtlassianProduct.Bitbucket,
            $"repositories/{workspace}/{args.RepoSlug}/pullrequests/{args.PullRequestId}", ct);
    }
}

public sealed class BitbucketCreatePullRequestActionHandler(
    IAtlassianHttpClient client,
    IOptions<AtlassianOptions> options,
    AtlassianSessionCredentials session,
    ILogger<BitbucketCreatePullRequestActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "bitbucket_create_pull_request";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.RepoSlug)) return "Missing required parameter 'RepoSlug'.";
        if (string.IsNullOrWhiteSpace(args.Title)) return "Missing required parameter 'Title'.";
        if (string.IsNullOrWhiteSpace(args.SourceBranch)) return "Missing required parameter 'SourceBranch'.";
        if (string.IsNullOrWhiteSpace(args.DestinationBranch)) return "Missing required parameter 'DestinationBranch'.";
        return null;
    }

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        var workspace = BitbucketHelper.RequireWorkspace(session, options);
        logger.LogDebug("Creating PR '{Title}' in {Workspace}/{Repo}", args.Title, workspace, args.RepoSlug);

        var payload = new
        {
            title = args.Title,
            description = args.Description ?? "",
            source = new { branch = new { name = args.SourceBranch } },
            destination = new { branch = new { name = args.DestinationBranch } },
            close_source_branch = args.CloseSourceBranch ?? false,
        };

        return await client.PostAsync(AtlassianProduct.Bitbucket,
            $"repositories/{workspace}/{args.RepoSlug}/pullrequests", payload, ct);
    }
}

public sealed class BitbucketTriggerPipelineActionHandler(
    IAtlassianHttpClient client,
    IOptions<AtlassianOptions> options,
    AtlassianSessionCredentials session,
    ILogger<BitbucketTriggerPipelineActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "bitbucket_trigger_pipeline";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.RepoSlug)) return "Missing required parameter 'RepoSlug'.";
        if (string.IsNullOrWhiteSpace(args.PipelineTarget)) return "Missing required parameter 'PipelineTarget' (e.g. branch name).";
        return null;
    }

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        var workspace = BitbucketHelper.RequireWorkspace(session, options);
        logger.LogDebug("Triggering pipeline on {Target} for {Workspace}/{Repo}", args.PipelineTarget, workspace, args.RepoSlug);

        var payload = new
        {
            target = new
            {
                ref_type = "branch",
                type = "pipeline_ref_target",
                ref_name = args.PipelineTarget
            }
        };

        return await client.PostAsync(AtlassianProduct.Bitbucket,
            $"repositories/{workspace}/{args.RepoSlug}/pipelines/", payload, ct);
    }
}
