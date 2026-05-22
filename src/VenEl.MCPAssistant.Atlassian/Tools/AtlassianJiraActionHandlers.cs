using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenEl.MCPAssistant.Atlassian.Services;
using VenEl.MCPAssistant.Core.Dispatcher;

namespace VenEl.MCPAssistant.Atlassian.Tools;

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
        var encoded = Uri.EscapeDataString(args.Jql!);
        return await client.GetAsync(AtlassianProduct.Jira,
            $"search?jql={encoded}&maxResults={maxResults}&startAt={startAt}&fields=summary,status,assignee,priority,issuetype,created,updated,labels",
            ct);
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

        if (fields.Count == 0)
            return "[ERROR] No fields provided to update. Specify at least one of: Summary, Description, Priority.";

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
