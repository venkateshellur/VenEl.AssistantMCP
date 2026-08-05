using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenEl.AssistantMCP.Core.Configuration;
using VenEl.AssistantMCP.Core.Dispatcher;
using VenEl.AssistantMCP.GitHub.Services;

namespace VenEl.AssistantMCP.GitHub.Tools;

public sealed class GitHubConfigureActionHandler(GitHubSession session, ILogger<GitHubConfigureActionHandler> logger, AppSettingsUpdater appSettingsUpdater) : IActionHandler<GitHubCommandArgs>
{
    public string ActionName => "github_configure";

    public string? Validate(GitHubCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.PatToken)) return "Missing required parameter 'PatToken'.";
        return null;
    }

    public Task<string> HandleAsync(GitHubCommandArgs args, CancellationToken ct)
    {
        session.PatToken = args.PatToken;
        
        var configValues = new Dictionary<string, object?>
        {
            { "PatToken", session.PatToken }
        };
        appSettingsUpdater.UpdateSection("GitHub", configValues);

        logger.LogInformation("GitHub session configured successfully and saved to appsettings.json.");
        return Task.FromResult("✅ GitHub session credentials configured successfully and saved to appsettings.json. You can now use the github_* tools.");
    }
}

public sealed class GitHubListPullRequestsActionHandler(IGitHubHttpClient client, ILogger<GitHubListPullRequestsActionHandler> logger) : IActionHandler<GitHubCommandArgs>
{
    public string ActionName => "github_list_pull_requests";

    public string? Validate(GitHubCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Owner)) return "Missing required parameter 'Owner'.";
        if (string.IsNullOrWhiteSpace(args.Repo)) return "Missing required parameter 'Repo'.";
        return null;
    }

    public async Task<string> HandleAsync(GitHubCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Listing GitHub pull requests for {Owner}/{Repo}", args.Owner, args.Repo);
        return await client.GetAsync($"repos/{args.Owner}/{args.Repo}/pulls?state=open&per_page=25", ct);
    }
}

public sealed class GitHubGetPullRequestActionHandler(IGitHubHttpClient client, ILogger<GitHubGetPullRequestActionHandler> logger) : IActionHandler<GitHubCommandArgs>
{
    public string ActionName => "github_get_pull_request";

    public string? Validate(GitHubCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Owner)) return "Missing required parameter 'Owner'.";
        if (string.IsNullOrWhiteSpace(args.Repo)) return "Missing required parameter 'Repo'.";
        if (!args.PullRequestNumber.HasValue) return "Missing required parameter 'PullRequestNumber'.";
        return null;
    }

    public async Task<string> HandleAsync(GitHubCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Getting PR {Number} for {Owner}/{Repo}", args.PullRequestNumber, args.Owner, args.Repo);
        return await client.GetAsync($"repos/{args.Owner}/{args.Repo}/pulls/{args.PullRequestNumber}", ct);
    }
}

public sealed class GitHubCreatePullRequestActionHandler(IGitHubHttpClient client, ILogger<GitHubCreatePullRequestActionHandler> logger) : IActionHandler<GitHubCommandArgs>
{
    public string ActionName => "github_create_pull_request";

    public string? Validate(GitHubCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Owner)) return "Missing required parameter 'Owner'.";
        if (string.IsNullOrWhiteSpace(args.Repo)) return "Missing required parameter 'Repo'.";
        if (string.IsNullOrWhiteSpace(args.Title)) return "Missing required parameter 'Title'.";
        if (string.IsNullOrWhiteSpace(args.Head)) return "Missing required parameter 'Head'.";
        if (string.IsNullOrWhiteSpace(args.Base)) return "Missing required parameter 'Base'.";
        return null;
    }

    public async Task<string> HandleAsync(GitHubCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Creating PR for {Owner}/{Repo} from {Head} to {Base}", args.Owner, args.Repo, args.Head, args.Base);
        var body = new
        {
            title = args.Title,
            head = args.Head,
            @base = args.Base,
            body = args.CommentBody
        };
        return await client.PostAsync($"repos/{args.Owner}/{args.Repo}/pulls", body, ct);
    }
}

public sealed class GitHubAddPrCommentActionHandler(IGitHubHttpClient client, ILogger<GitHubAddPrCommentActionHandler> logger) : IActionHandler<GitHubCommandArgs>
{
    public string ActionName => "github_add_pr_comment";

    public string? Validate(GitHubCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Owner)) return "Missing required parameter 'Owner'.";
        if (string.IsNullOrWhiteSpace(args.Repo)) return "Missing required parameter 'Repo'.";
        if (!args.PullRequestNumber.HasValue) return "Missing required parameter 'PullRequestNumber'.";
        if (string.IsNullOrWhiteSpace(args.CommentBody)) return "Missing required parameter 'CommentBody'.";
        return null;
    }

    public async Task<string> HandleAsync(GitHubCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Adding comment to PR {Number} in {Owner}/{Repo}", args.PullRequestNumber, args.Owner, args.Repo);
        var body = new { body = args.CommentBody };
        // GitHub uses issue comments API for PR general comments
        return await client.PostAsync($"repos/{args.Owner}/{args.Repo}/issues/{args.PullRequestNumber}/comments", body, ct);
    }
}

public sealed class GitHubMergePullRequestActionHandler(IGitHubHttpClient client, ILogger<GitHubMergePullRequestActionHandler> logger) : IActionHandler<GitHubCommandArgs>
{
    public string ActionName => "github_merge_pull_request";

    public string? Validate(GitHubCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Owner)) return "Missing required parameter 'Owner'.";
        if (string.IsNullOrWhiteSpace(args.Repo)) return "Missing required parameter 'Repo'.";
        if (!args.PullRequestNumber.HasValue) return "Missing required parameter 'PullRequestNumber'.";
        return null;
    }

    public async Task<string> HandleAsync(GitHubCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Merging PR {Number} in {Owner}/{Repo}", args.PullRequestNumber, args.Owner, args.Repo);
        var body = new { commit_title = args.Title ?? $"Merge PR #{args.PullRequestNumber}" };
        return await client.PutAsync($"repos/{args.Owner}/{args.Repo}/pulls/{args.PullRequestNumber}/merge", body, ct);
    }
}

public sealed class GitHubGetPrDiffActionHandler(IGitHubHttpClient client, ILogger<GitHubGetPrDiffActionHandler> logger) : IActionHandler<GitHubCommandArgs>
{
    public string ActionName => "github_get_pr_diff";

    public string? Validate(GitHubCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Owner)) return "Missing required parameter 'Owner'.";
        if (string.IsNullOrWhiteSpace(args.Repo)) return "Missing required parameter 'Repo'.";
        if (!args.PullRequestNumber.HasValue) return "Missing required parameter 'PullRequestNumber'.";
        return null;
    }

    public async Task<string> HandleAsync(GitHubCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Getting PR Diff for {Owner}/{Repo} PR #{Number}", args.Owner, args.Repo, args.PullRequestNumber);
        return await client.GetAsync($"repos/{args.Owner}/{args.Repo}/pulls/{args.PullRequestNumber}", ct, acceptHeader: "application/vnd.github.v3.diff");
    }
}

public sealed class GitHubListCommitsActionHandler(IGitHubHttpClient client, ILogger<GitHubListCommitsActionHandler> logger) : IActionHandler<GitHubCommandArgs>
{
    public string ActionName => "github_list_commits";

    public string? Validate(GitHubCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Owner)) return "Missing required parameter 'Owner'.";
        if (string.IsNullOrWhiteSpace(args.Repo)) return "Missing required parameter 'Repo'.";
        return null;
    }

    public async Task<string> HandleAsync(GitHubCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Listing GitHub commits for {Owner}/{Repo}", args.Owner, args.Repo);
        return await client.GetAsync($"repos/{args.Owner}/{args.Repo}/commits?per_page=5", ct);
    }
}
