using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using VenEl.MCPAssistant.Atlassian.Configuration;
using VenEl.MCPAssistant.Atlassian.Services;

namespace VenEl.MCPAssistant.Atlassian.Tools;

/// <summary>MCP tools for Bitbucket Cloud REST API v2.</summary>
[McpServerToolType]
public sealed class AtlassianBitbucketTools(
    IAtlassianHttpClient client,
    IOptions<AtlassianOptions> options,
    AtlassianSessionCredentials session,
    ILogger<AtlassianBitbucketTools> logger)
{
    // Session workspace takes precedence over config.
    private string Workspace =>
        !string.IsNullOrWhiteSpace(session.BitbucketWorkspace)
            ? session.BitbucketWorkspace
            : options.Value.BitbucketWorkspace;

    private string RequireWorkspace()
    {
        if (string.IsNullOrWhiteSpace(Workspace))
            throw new InvalidOperationException(
                "[CONFIG ERROR] Bitbucket workspace is not set. " +
                "Call 'atlassian_configure' with your bitbucketWorkspace, " +
                "or set Atlassian:BitbucketWorkspace in appsettings.json.");
        return Workspace;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Repositories
    // ═════════════════════════════════════════════════════════════════════════

    [McpServerTool(Name = "bitbucket_list_repositories")]
    [Description(
        "Lists all repositories in the configured Bitbucket workspace. " +
        "Returns repo slug, full name, description, language, and clone URLs.")]
    public async Task<string> BitbucketListRepositoriesAsync(
        [Description("Maximum repositories to return (default 25, max 100).")] int pagelen = 25,
        CancellationToken cancellationToken = default)
    {
        pagelen = Math.Clamp(pagelen, 1, 100);
        var workspace = RequireWorkspace();
        logger.LogDebug("Listing Bitbucket repos for workspace {Workspace}", workspace);
        return await client.GetAsync(AtlassianProduct.Bitbucket,
            $"repositories/{workspace}?pagelen={pagelen}&fields=values.slug,values.full_name,values.description,values.language,values.is_private,values.clone",
            cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "bitbucket_get_repository")]
    [Description("Returns full details of a specific Bitbucket repository.")]
    public async Task<string> BitbucketGetRepositoryAsync(
        [Description("The repository slug (short name).")] string repoSlug,
        CancellationToken cancellationToken = default)
    {
        var workspace = RequireWorkspace();
        logger.LogDebug("Getting Bitbucket repo {Workspace}/{Repo}", workspace, repoSlug);
        return await client.GetAsync(AtlassianProduct.Bitbucket,
            $"repositories/{workspace}/{repoSlug}", cancellationToken);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Branches
    // ═════════════════════════════════════════════════════════════════════════

    [McpServerTool(Name = "bitbucket_list_branches")]
    [Description("Lists all branches of a Bitbucket repository.")]
    public async Task<string> BitbucketListBranchesAsync(
        [Description("The repository slug.")] string repoSlug,
        [Description("Maximum branches to return (default 25, max 100).")] int pagelen = 25,
        CancellationToken cancellationToken = default)
    {
        pagelen = Math.Clamp(pagelen, 1, 100);
        var workspace = RequireWorkspace();
        logger.LogDebug("Listing branches for {Workspace}/{Repo}", workspace, repoSlug);
        return await client.GetAsync(AtlassianProduct.Bitbucket,
            $"repositories/{workspace}/{repoSlug}/refs/branches?pagelen={pagelen}",
            cancellationToken);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Pull Requests
    // ═════════════════════════════════════════════════════════════════════════

    [McpServerTool(Name = "bitbucket_list_pull_requests")]
    [Description(
        "Lists pull requests in a Bitbucket repository. " +
        "State options: 'OPEN' (default), 'MERGED', 'DECLINED', 'SUPERSEDED'.")]
    public async Task<string> BitbucketListPullRequestsAsync(
        [Description("The repository slug.")] string repoSlug,
        [Description("PR state filter: 'OPEN', 'MERGED', 'DECLINED'.")] string state = "OPEN",
        [Description("Maximum PRs to return (default 25, max 100).")] int pagelen = 25,
        CancellationToken cancellationToken = default)
    {
        pagelen = Math.Clamp(pagelen, 1, 100);
        var workspace = RequireWorkspace();
        logger.LogDebug("Listing {State} PRs for {Workspace}/{Repo}", state, workspace, repoSlug);
        return await client.GetAsync(AtlassianProduct.Bitbucket,
            $"repositories/{workspace}/{repoSlug}/pullrequests?state={state}&pagelen={pagelen}",
            cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "bitbucket_get_pull_request")]
    [Description("Returns full details of a specific Bitbucket pull request, including reviewers and diff stats.")]
    public async Task<string> BitbucketGetPullRequestAsync(
        [Description("The repository slug.")] string repoSlug,
        [Description("The pull request ID (numeric).")] int pullRequestId,
        CancellationToken cancellationToken = default)
    {
        var workspace = RequireWorkspace();
        logger.LogDebug("Getting PR #{PrId} for {Workspace}/{Repo}", pullRequestId, workspace, repoSlug);
        return await client.GetAsync(AtlassianProduct.Bitbucket,
            $"repositories/{workspace}/{repoSlug}/pullrequests/{pullRequestId}",
            cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "bitbucket_create_pull_request")]
    [Description(
        "Creates a new pull request in a Bitbucket repository. " +
        "sourceBranch is the feature branch; destinationBranch is typically 'main' or 'develop'.")]
    public async Task<string> BitbucketCreatePullRequestAsync(
        [Description("The repository slug.")] string repoSlug,
        [Description("PR title.")] string title,
        [Description("Source (feature) branch name.")] string sourceBranch,
        [Description("Destination (target) branch name, e.g. 'main'.")] string destinationBranch,
        [Description("PR description (optional).")] string? description = null,
        [Description("Set to true to close the source branch after merge.")] bool closeSourceBranch = false,
        CancellationToken cancellationToken = default)
    {
        var workspace = RequireWorkspace();
        logger.LogDebug("Creating PR '{Title}' in {Workspace}/{Repo}", title, workspace, repoSlug);

        var payload = new
        {
            title,
            description = description ?? "",
            source      = new { branch = new { name = sourceBranch } },
            destination = new { branch = new { name = destinationBranch } },
            close_source_branch = closeSourceBranch,
        };

        return await client.PostAsync(AtlassianProduct.Bitbucket,
            $"repositories/{workspace}/{repoSlug}/pullrequests", payload, cancellationToken);
    }
}
