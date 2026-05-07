using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using VenEl.MCPAssistant.Atlassian.Configuration;
using VenEl.MCPAssistant.Atlassian.Services;

namespace VenEl.MCPAssistant.Atlassian.Tools;

/// <summary>MCP tools for Jira Cloud REST API v3.</summary>
[McpServerToolType]
public sealed class AtlassianJiraTools(
    IAtlassianHttpClient client,
    IOptions<AtlassianOptions> options,
    ILogger<AtlassianJiraTools> logger)
{
    private readonly string _domain = options.Value.Domain;

    // ── Helper: Atlassian Document Format wrapper for plain text ─────────────
    private static object ToAdf(string text) => new
    {
        type    = "doc",
        version = 1,
        content = new[]
        {
            new
            {
                type    = "paragraph",
                content = new[] { new { type = "text", text } }
            }
        }
    };

    // ═════════════════════════════════════════════════════════════════════════
    // Projects
    // ═════════════════════════════════════════════════════════════════════════

    [McpServerTool(Name = "jira_list_projects")]
    [Description(
        "Lists all Jira projects accessible to the configured account. " +
        "Returns project key, name, type, and lead.")]
    public async Task<string> JiraListProjectsAsync(
        [Description("Maximum number of projects to return (default 50, max 100).")] int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
        maxResults = Math.Clamp(maxResults, 1, 100);
        logger.LogDebug("Listing Jira projects (maxResults={Max})", maxResults);
        return await client.GetAsync(AtlassianProduct.Jira,
            $"project/search?maxResults={maxResults}", cancellationToken);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Issues
    // ═════════════════════════════════════════════════════════════════════════

    [McpServerTool(Name = "jira_get_issue")]
    [Description(
        "Returns full details of a Jira issue by its key (e.g. PROJ-123). " +
        "Includes summary, status, assignee, description, comments, and labels.")]
    public async Task<string> JiraGetIssueAsync(
        [Description("The issue key, e.g. PROJ-123.")] string issueKey,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting Jira issue {Key}", issueKey);
        return await client.GetAsync(AtlassianProduct.Jira,
            $"issue/{issueKey}?expand=renderedFields,names", cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "jira_search_issues")]
    [Description(
        "Searches Jira issues using JQL (Jira Query Language). " +
        "Example JQL: 'project = PROJ AND status = \"In Progress\" ORDER BY created DESC'.")]
    public async Task<string> JiraSearchIssuesAsync(
        [Description("JQL query string.")] string jql,
        [Description("Maximum results to return (default 25, max 100).")] int maxResults = 25,
        [Description("Zero-based offset for pagination.")] int startAt = 0,
        CancellationToken cancellationToken = default)
    {
        maxResults = Math.Clamp(maxResults, 1, 100);
        logger.LogDebug("Searching Jira issues with JQL: {Jql}", jql);
        var encoded = Uri.EscapeDataString(jql);
        return await client.GetAsync(AtlassianProduct.Jira,
            $"search?jql={encoded}&maxResults={maxResults}&startAt={startAt}" +
            "&fields=summary,status,assignee,priority,issuetype,created,updated,labels",
            cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "jira_create_issue")]
    [Description(
        "Creates a new Jira issue. " +
        "issueType examples: 'Bug', 'Task', 'Story', 'Epic'. " +
        "description accepts plain text (automatically wrapped in Atlassian Document Format).")]
    public async Task<string> JiraCreateIssueAsync(
        [Description("Project key, e.g. PROJ.")] string projectKey,
        [Description("Issue summary (title).")] string summary,
        [Description("Issue type name, e.g. 'Task', 'Bug', 'Story'.")] string issueType = "Task",
        [Description("Plain text description.")] string? description = null,
        [Description("Priority name: 'Highest', 'High', 'Medium', 'Low', 'Lowest'.")] string? priority = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Creating Jira issue in project {Project}", projectKey);

        var fields = new Dictionary<string, object>
        {
            ["project"]   = new { key = projectKey },
            ["summary"]   = summary,
            ["issuetype"] = new { name = issueType },
        };

        if (description is not null)
            fields["description"] = ToAdf(description);
        if (priority is not null)
            fields["priority"] = new { name = priority };

        return await client.PostAsync(AtlassianProduct.Jira, "issue",
            new { fields }, cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "jira_update_issue")]
    [Description(
        "Updates fields on an existing Jira issue. " +
        "Only supply the fields you want to change; omit others.")]
    public async Task<string> JiraUpdateIssueAsync(
        [Description("The issue key, e.g. PROJ-123.")] string issueKey,
        [Description("New summary (title).")] string? summary = null,
        [Description("New plain-text description.")] string? description = null,
        [Description("New priority name: 'Highest', 'High', 'Medium', 'Low', 'Lowest'.")] string? priority = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Updating Jira issue {Key}", issueKey);

        var fields = new Dictionary<string, object>();
        if (summary is not null)     fields["summary"]     = summary;
        if (description is not null) fields["description"] = ToAdf(description);
        if (priority is not null)    fields["priority"]    = new { name = priority };

        if (fields.Count == 0)
            return "[ERROR] No fields provided to update. Specify at least one of: summary, description, priority.";

        return await client.PutAsync(AtlassianProduct.Jira, $"issue/{issueKey}",
            new { fields }, cancellationToken);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Comments
    // ═════════════════════════════════════════════════════════════════════════

    [McpServerTool(Name = "jira_add_comment")]
    [Description("Adds a plain-text comment to a Jira issue.")]
    public async Task<string> JiraAddCommentAsync(
        [Description("The issue key, e.g. PROJ-123.")] string issueKey,
        [Description("The comment text.")] string comment,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Adding comment to Jira issue {Key}", issueKey);
        return await client.PostAsync(AtlassianProduct.Jira,
            $"issue/{issueKey}/comment",
            new { body = ToAdf(comment) },
            cancellationToken);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Transitions / Status
    // ═════════════════════════════════════════════════════════════════════════

    [McpServerTool(Name = "jira_get_transitions")]
    [Description(
        "Returns the available status transitions for a Jira issue. " +
        "Use the returned transition IDs with jira_transition_issue.")]
    public async Task<string> JiraGetTransitionsAsync(
        [Description("The issue key, e.g. PROJ-123.")] string issueKey,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting transitions for Jira issue {Key}", issueKey);
        return await client.GetAsync(AtlassianProduct.Jira,
            $"issue/{issueKey}/transitions", cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "jira_transition_issue")]
    [Description(
        "Moves a Jira issue to a new status using a transition ID. " +
        "Get available transition IDs first with jira_get_transitions.")]
    public async Task<string> JiraTransitionIssueAsync(
        [Description("The issue key, e.g. PROJ-123.")] string issueKey,
        [Description("The transition ID (from jira_get_transitions).")] string transitionId,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Transitioning Jira issue {Key} with transition {Id}", issueKey, transitionId);
        return await client.PostAsync(AtlassianProduct.Jira,
            $"issue/{issueKey}/transitions",
            new { transition = new { id = transitionId } },
            cancellationToken);
    }
}
