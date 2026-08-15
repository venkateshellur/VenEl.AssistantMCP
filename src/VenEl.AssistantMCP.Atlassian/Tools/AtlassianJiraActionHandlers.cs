using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenEl.AssistantMCP.Atlassian.Services;
using VenEl.AssistantMCP.Core.Dispatcher;

namespace VenEl.AssistantMCP.Atlassian.Tools;

internal static class JiraHelper
{
    // ── Helper: Atlassian Document Format wrapper for plain text ─────────────
    public static object ToAdf(string text) => new
    {
        type = "doc",
        version = 1,
        content = new[]
        {
            new
            {
                type = "paragraph",
                content = new[] { new { type = "text", text } }
            }
        }
    };
}

public sealed class JiraListProjectsActionHandler(IAtlassianHttpClient client, ILogger<JiraListProjectsActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "jira_list_projects";

    public string? Validate(AtlassianCommandArgs args) => null;

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        int maxResults = Math.Clamp(args.MaxResults ?? 50, 1, 100);
        logger.LogDebug("Listing Jira projects (maxResults={Max})", maxResults);
        return await client.GetAsync(AtlassianProduct.Jira, $"project/search?maxResults={maxResults}", ct);
    }
}

public sealed class JiraGetIssueActionHandler(IAtlassianHttpClient client, ILogger<JiraGetIssueActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "jira_get_issue";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.IssueKey)) return "Missing required parameter 'IssueKey'.";
        return null;
    }

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Getting Jira issue {Key}", args.IssueKey);
        return await client.GetAsync(AtlassianProduct.Jira, $"issue/{args.IssueKey}?expand=renderedFields,names", ct);
    }
}

public sealed class JiraSearchIssuesActionHandler(IAtlassianHttpClient client, ILogger<JiraSearchIssuesActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "jira_search_issues";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Jql)) return "Missing required parameter 'Jql'.";
        return null;
    }

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        int maxResults = Math.Clamp(args.MaxResults ?? 25, 1, 100);
        int startAt = Math.Max(args.StartAt ?? 0, 0);
        logger.LogDebug("Searching Jira issues with JQL: {Jql}", args.Jql);
        var payload = new
        {
            jql = args.Jql,
            maxResults = maxResults,
            startAt = startAt,
            fields = new[] { "summary", "status", "assignee", "priority", "issuetype", "created", "updated", "labels" }
        };
        return await client.PostAsync(AtlassianProduct.Jira, "search/jql", payload, ct);
    }
}

public sealed class JiraCreateIssueActionHandler(IAtlassianHttpClient client, ILogger<JiraCreateIssueActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "jira_create_issue";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.ProjectKey)) return "Missing required parameter 'ProjectKey'.";
        if (string.IsNullOrWhiteSpace(args.Summary)) return "Missing required parameter 'Summary'.";
        return null;
    }

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Creating Jira issue in project {Project}", args.ProjectKey);

        var fields = new Dictionary<string, object>
        {
            ["project"] = new { key = args.ProjectKey },
            ["summary"] = args.Summary!,
            ["issuetype"] = new { name = args.IssueType ?? "Task" },
        };

        if (args.Description is not null)
            fields["description"] = JiraHelper.ToAdf(args.Description);
        if (args.Priority is not null)
            fields["priority"] = new { name = args.Priority };
        if (args.ParentKey is not null)
            fields["parent"] = new { key = args.ParentKey };
        if (args.AssigneeAccountId is not null)
            fields["assignee"] = new { accountId = args.AssigneeAccountId };
        if (args.Labels is not null && args.Labels.Length > 0)
            fields["labels"] = args.Labels;
            
        if (args.RawFields is not null)
        {
            foreach (var kvp in args.RawFields)
            {
                fields[kvp.Key] = kvp.Value;
            }
        }

        return await client.PostAsync(AtlassianProduct.Jira, "issue", new { fields }, ct);
    }
}

public sealed class JiraUpdateIssueActionHandler(IAtlassianHttpClient client, ILogger<JiraUpdateIssueActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "jira_update_issue";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.IssueKey)) return "Missing required parameter 'IssueKey'.";
        return null;
    }

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Updating Jira issue {Key}", args.IssueKey);

        var fields = new Dictionary<string, object>();
        if (args.Summary is not null) fields["summary"] = args.Summary;
        if (args.Description is not null) fields["description"] = JiraHelper.ToAdf(args.Description);
        if (args.Priority is not null) fields["priority"] = new { name = args.Priority };
        if (args.ParentKey is not null) fields["parent"] = new { key = args.ParentKey };
        if (args.AssigneeAccountId is not null) fields["assignee"] = new { accountId = args.AssigneeAccountId };
        if (args.Labels is not null && args.Labels.Length > 0) fields["labels"] = args.Labels;
        
        if (args.RawFields is not null)
        {
            foreach (var kvp in args.RawFields)
            {
                fields[kvp.Key] = kvp.Value;
            }
        }

        if (fields.Count == 0)
            return "[ERROR] No fields provided to update. Specify at least one of: Summary, Description, Priority, ParentKey, AssigneeAccountId, Labels, or RawFields.";

        return await client.PutAsync(AtlassianProduct.Jira, $"issue/{args.IssueKey}", new { fields }, ct);
    }
}

public sealed class JiraAddCommentActionHandler(IAtlassianHttpClient client, ILogger<JiraAddCommentActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "jira_add_comment";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.IssueKey)) return "Missing required parameter 'IssueKey'.";
        if (string.IsNullOrWhiteSpace(args.Comment)) return "Missing required parameter 'Comment'.";
        return null;
    }

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Adding comment to Jira issue {Key}", args.IssueKey);
        return await client.PostAsync(AtlassianProduct.Jira, $"issue/{args.IssueKey}/comment", new { body = JiraHelper.ToAdf(args.Comment!) }, ct);
    }
}

public sealed class JiraGetTransitionsActionHandler(IAtlassianHttpClient client, ILogger<JiraGetTransitionsActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "jira_get_transitions";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.IssueKey)) return "Missing required parameter 'IssueKey'.";
        return null;
    }

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Getting transitions for Jira issue {Key}", args.IssueKey);
        return await client.GetAsync(AtlassianProduct.Jira, $"issue/{args.IssueKey}/transitions", ct);
    }
}

public sealed class JiraTransitionIssueActionHandler(IAtlassianHttpClient client, ILogger<JiraTransitionIssueActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "jira_transition_issue";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.IssueKey)) return "Missing required parameter 'IssueKey'.";
        if (string.IsNullOrWhiteSpace(args.TransitionId)) return "Missing required parameter 'TransitionId'.";
        return null;
    }

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Transitioning Jira issue {Key} with transition {Id}", args.IssueKey, args.TransitionId);
        return await client.PostAsync(AtlassianProduct.Jira, $"issue/{args.IssueKey}/transitions",
            new { transition = new { id = args.TransitionId } }, ct);
    }
}

public sealed class JiraListBoardsActionHandler(IAtlassianHttpClient client, ILogger<JiraListBoardsActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "jira_list_boards";

    public string? Validate(AtlassianCommandArgs args) => null;

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        int maxResults = Math.Clamp(args.MaxResults ?? 50, 1, 100);
        logger.LogDebug("Listing Jira boards (maxResults={Max})", maxResults);
        return await client.GetAsync(AtlassianProduct.JiraAgile, $"board?maxResults={maxResults}", ct);
    }
}

public sealed class JiraListSprintsActionHandler(IAtlassianHttpClient client, ILogger<JiraListSprintsActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "jira_list_sprints";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (args.BoardId == null) return "Missing required parameter 'BoardId'.";
        return null;
    }

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        int maxResults = Math.Clamp(args.MaxResults ?? 50, 1, 100);
        logger.LogDebug("Listing Jira sprints for board {BoardId} (maxResults={Max})", args.BoardId, maxResults);
        return await client.GetAsync(AtlassianProduct.JiraAgile, $"board/{args.BoardId}/sprint?maxResults={maxResults}", ct);
    }
}

public sealed class JiraMoveIssuesToSprintActionHandler(IAtlassianHttpClient client, ILogger<JiraMoveIssuesToSprintActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "jira_move_issues_to_sprint";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (args.SprintId == null) return "Missing required parameter 'SprintId'.";
        if (string.IsNullOrWhiteSpace(args.IssueKey)) return "Missing required parameter 'IssueKey'.";
        return null;
    }

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Moving issue {IssueKey} to sprint {SprintId}", args.IssueKey, args.SprintId);
        // The API accepts an array of issue keys
        var payload = new { issues = new[] { args.IssueKey } };
        return await client.PostAsync(AtlassianProduct.JiraAgile, $"sprint/{args.SprintId}/issue", payload, ct);
    }
}

public sealed class JiraGetSprintIssuesActionHandler(IAtlassianHttpClient client, ILogger<JiraGetSprintIssuesActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "jira_get_sprint_issues";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (args.SprintId == null) return "Missing required parameter 'SprintId'.";
        return null;
    }

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        int maxResults = Math.Clamp(args.MaxResults ?? 50, 1, 100);
        int startAt = Math.Max(args.StartAt ?? 0, 0);
        logger.LogDebug("Getting issues for sprint {SprintId} (maxResults={Max})", args.SprintId, maxResults);
        return await client.GetAsync(AtlassianProduct.JiraAgile, $"sprint/{args.SprintId}/issue?maxResults={maxResults}&startAt={startAt}", ct);
    }
}

public sealed class JiraSearchUsersActionHandler(IAtlassianHttpClient client, ILogger<JiraSearchUsersActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "jira_search_users";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Query)) return "Missing required parameter 'Query'.";
        return null;
    }

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        int maxResults = Math.Clamp(args.MaxResults ?? 50, 1, 100);
        logger.LogDebug("Searching for Jira users with query: {Query}", args.Query);
        var uriQuery = Uri.EscapeDataString(args.Query!);
        return await client.GetAsync(AtlassianProduct.Jira, $"user/search?query={uriQuery}&maxResults={maxResults}", ct);
    }
}

public sealed class JiraLinkIssuesActionHandler(IAtlassianHttpClient client, ILogger<JiraLinkIssuesActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "jira_link_issues";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.OutwardIssueKey)) return "Missing required parameter 'OutwardIssueKey'.";
        if (string.IsNullOrWhiteSpace(args.InwardIssueKey)) return "Missing required parameter 'InwardIssueKey'.";
        if (string.IsNullOrWhiteSpace(args.LinkType)) return "Missing required parameter 'LinkType'. (e.g. 'Blocks', 'Relates')";
        return null;
    }

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Linking issue {Outward} to {Inward} with type {Type}", args.OutwardIssueKey, args.InwardIssueKey, args.LinkType);
        var payload = new
        {
            type = new { name = args.LinkType },
            inwardIssue = new { key = args.InwardIssueKey },
            outwardIssue = new { key = args.OutwardIssueKey }
        };
        return await client.PostAsync(AtlassianProduct.Jira, "issueLink", payload, ct);
    }
}

public sealed class JiraAddWorklogActionHandler(IAtlassianHttpClient client, ILogger<JiraAddWorklogActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "jira_add_worklog";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.IssueKey)) return "Missing required parameter 'IssueKey'.";
        if (string.IsNullOrWhiteSpace(args.TimeSpent)) return "Missing required parameter 'TimeSpent' (e.g. '1h 30m').";
        return null;
    }

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Adding worklog to {IssueKey} for {Time}", args.IssueKey, args.TimeSpent);
        var payload = new Dictionary<string, object> { ["timeSpent"] = args.TimeSpent! };
        if (!string.IsNullOrWhiteSpace(args.Comment))
        {
            payload["comment"] = JiraHelper.ToAdf(args.Comment);
        }
        return await client.PostAsync(AtlassianProduct.Jira, $"issue/{args.IssueKey}/worklog", payload, ct);
    }
}

public sealed class JiraDeleteIssueActionHandler() : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "jira_delete_issue";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.IssueKey)) return "Missing required parameter 'IssueKey'.";
        return null;
    }

    public Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        return Task.FromResult("[ERROR] Destructive operations (like deletion) are permanently disabled in this MCP Server for safety reasons.");
    }
}

public sealed class JiraAddAttachmentActionHandler(IAtlassianHttpClient client, ILogger<JiraAddAttachmentActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "jira_add_attachment";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.IssueKey)) return "Missing required parameter 'IssueKey'.";
        if (string.IsNullOrWhiteSpace(args.FilePath)) return "Missing required parameter 'FilePath'.";
        if (!System.IO.File.Exists(args.FilePath)) return $"File not found at path: {args.FilePath}";
        return null;
    }

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Adding attachment {FilePath} to issue {IssueKey}", args.FilePath, args.IssueKey);
        
        var fileName = System.IO.Path.GetFileName(args.FilePath!);
        var fileStream = System.IO.File.OpenRead(args.FilePath!);
        
        using var formData = new System.Net.Http.MultipartFormDataContent();
        var fileContent = new System.Net.Http.StreamContent(fileStream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        formData.Add(fileContent, "file", fileName);

        return await client.PostMultipartAsync(AtlassianProduct.Jira, $"issue/{args.IssueKey}/attachments", formData, ct);
    }
}
