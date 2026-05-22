using System.Text.Json.Serialization;

namespace VenEl.MCPAssistant.GitHub.Tools;

public sealed class GitHubCommandArgs
{
    /// <summary>
    /// The action to perform. Options: github_configure, github_list_pull_requests, github_get_pull_request, github_create_pull_request, github_add_pr_comment, github_merge_pull_request
    /// </summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Your GitHub Personal Access Token (classic or fine-grained). Used only by github_configure.
    /// </summary>
    [JsonPropertyName("patToken")]
    public string? PatToken { get; set; }

    /// <summary>
    /// The owner of the repository (e.g., 'octocat').
    /// </summary>
    [JsonPropertyName("owner")]
    public string? Owner { get; set; }

    /// <summary>
    /// The repository name (e.g., 'Hello-World').
    /// </summary>
    [JsonPropertyName("repo")]
    public string? Repo { get; set; }

    /// <summary>
    /// The pull request number.
    /// </summary>
    [JsonPropertyName("pullRequestNumber")]
    public int? PullRequestNumber { get; set; }

    /// <summary>
    /// The text for a comment to add to a pull request.
    /// </summary>
    [JsonPropertyName("commentBody")]
    public string? CommentBody { get; set; }

    /// <summary>
    /// The title for creating a new pull request.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// The source branch name for creating a pull request (e.g., 'feature-branch').
    /// </summary>
    [JsonPropertyName("head")]
    public string? Head { get; set; }

    /// <summary>
    /// The target branch name for creating a pull request (e.g., 'main').
    /// </summary>
    [JsonPropertyName("base")]
    public string? Base { get; set; }

    /// <summary>
    /// The workflow ID or filename. Used for GitHub Actions.
    /// </summary>
    [JsonPropertyName("workflowId")]
    public string? WorkflowId { get; set; }

    /// <summary>
    /// The git reference for the workflow run (e.g., 'main').
    /// </summary>
    [JsonPropertyName("ref")]
    public string? Ref { get; set; }
}
