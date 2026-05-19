using System.Text.Json.Serialization;

namespace VenEl.MCPAssistant.Atlassian.Tools;

public sealed class AtlassianCommandArgs
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    // ── Shared Config Parameters ──
    [JsonPropertyName("domain")]
    public string? Domain { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("apiToken")]
    public string? ApiToken { get; set; }

    [JsonPropertyName("bitbucketWorkspace")]
    public string? BitbucketWorkspace { get; set; }

    // ── Jira Parameters ──
    [JsonPropertyName("issueKey")]
    public string? IssueKey { get; set; }

    [JsonPropertyName("projectKey")]
    public string? ProjectKey { get; set; }

    [JsonPropertyName("jql")]
    public string? Jql { get; set; }

    [JsonPropertyName("issueType")]
    public string? IssueType { get; set; }

    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    [JsonPropertyName("transitionId")]
    public string? TransitionId { get; set; }

    [JsonPropertyName("maxResults")]
    public int? MaxResults { get; set; }

    [JsonPropertyName("startAt")]
    public int? StartAt { get; set; }

    // ── Confluence Parameters ──
    [JsonPropertyName("pageId")]
    public string? PageId { get; set; }

    [JsonPropertyName("spaceKey")]
    public string? SpaceKey { get; set; }

    [JsonPropertyName("cql")]
    public string? Cql { get; set; }

    [JsonPropertyName("bodyContent")]
    public string? BodyContent { get; set; }

    [JsonPropertyName("currentVersion")]
    public int? CurrentVersion { get; set; }

    [JsonPropertyName("parentId")]
    public string? ParentId { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    // ── Bitbucket Parameters ──
    [JsonPropertyName("repoSlug")]
    public string? RepoSlug { get; set; }

    [JsonPropertyName("pullRequestId")]
    public int? PullRequestId { get; set; }

    [JsonPropertyName("sourceBranch")]
    public string? SourceBranch { get; set; }

    [JsonPropertyName("destinationBranch")]
    public string? DestinationBranch { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("closeSourceBranch")]
    public bool? CloseSourceBranch { get; set; }

    [JsonPropertyName("pagelen")]
    public int? Pagelen { get; set; }

    // ── General Shared Parameters ──
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }
}
