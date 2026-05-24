using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenEl.AssistantMCP.Atlassian.Services;
using VenEl.AssistantMCP.Core.Dispatcher;

namespace VenEl.AssistantMCP.Atlassian.Tools;

public sealed class ConfluenceListSpacesActionHandler(IAtlassianHttpClient client, ILogger<ConfluenceListSpacesActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "confluence_list_spaces";

    public string? Validate(AtlassianCommandArgs args) => null;

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        int limit = Math.Clamp(args.Limit ?? 50, 1, 100);
        logger.LogDebug("Listing Confluence spaces (limit={Limit})", limit);
        return await client.GetAsync(AtlassianProduct.Confluence, $"space?limit={limit}&expand=description.plain", ct);
    }
}

public sealed class ConfluenceSearchPagesActionHandler(IAtlassianHttpClient client, ILogger<ConfluenceSearchPagesActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "confluence_search_pages";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Cql)) return "Missing required parameter 'Cql'.";
        return null;
    }

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        int limit = Math.Clamp(args.Limit ?? 25, 1, 100);
        logger.LogDebug("Searching Confluence with CQL: {Cql}", args.Cql);
        var encoded = Uri.EscapeDataString(args.Cql!);
        return await client.GetAsync(AtlassianProduct.Confluence, $"content/search?cql={encoded}&limit={limit}&expand=space,version", ct);
    }
}

public sealed class ConfluenceGetPageActionHandler(IAtlassianHttpClient client, ILogger<ConfluenceGetPageActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "confluence_get_page";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.PageId)) return "Missing required parameter 'PageId'.";
        return null;
    }

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Getting Confluence page {Id}", args.PageId);
        return await client.GetAsync(AtlassianProduct.Confluence, $"content/{args.PageId}?expand=body.storage,version,space", ct);
    }
}

public sealed class ConfluenceCreatePageActionHandler(IAtlassianHttpClient client, ILogger<ConfluenceCreatePageActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "confluence_create_page";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.SpaceKey)) return "Missing required parameter 'SpaceKey'.";
        if (string.IsNullOrWhiteSpace(args.Title)) return "Missing required parameter 'Title'.";
        if (string.IsNullOrWhiteSpace(args.BodyContent)) return "Missing required parameter 'BodyContent'.";
        return null;
    }

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Creating Confluence page '{Title}' in space {Space}", args.Title, args.SpaceKey);

        var payload = new Dictionary<string, object>
        {
            ["type"] = "page",
            ["title"] = args.Title!,
            ["space"] = new { key = args.SpaceKey! },
            ["body"] = new
            {
                storage = new { value = args.BodyContent!, representation = "storage" }
            },
        };

        if (args.ParentId is not null)
            payload["ancestors"] = new[] { new { id = args.ParentId } };

        return await client.PostAsync(AtlassianProduct.Confluence, "content", payload, ct);
    }
}

public sealed class ConfluenceUpdatePageActionHandler(IAtlassianHttpClient client, ILogger<ConfluenceUpdatePageActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "confluence_update_page";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.PageId)) return "Missing required parameter 'PageId'.";
        if (!args.CurrentVersion.HasValue) return "Missing required parameter 'CurrentVersion'.";
        if (string.IsNullOrWhiteSpace(args.Title)) return "Missing required parameter 'Title'.";
        if (string.IsNullOrWhiteSpace(args.BodyContent)) return "Missing required parameter 'BodyContent'.";
        return null;
    }

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Updating Confluence page {Id}", args.PageId);

        var payload = new
        {
            version = new { number = args.CurrentVersion!.Value + 1 },
            type = "page",
            title = args.Title,
            body = new
            {
                storage = new { value = args.BodyContent, representation = "storage" }
            },
        };

        return await client.PutAsync(AtlassianProduct.Confluence, $"content/{args.PageId}", payload, ct);
    }
}

public sealed class ConfluenceAddCommentActionHandler(IAtlassianHttpClient client, ILogger<ConfluenceAddCommentActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "confluence_add_comment";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.PageId)) return "Missing required parameter 'PageId'.";
        if (string.IsNullOrWhiteSpace(args.Comment)) return "Missing required parameter 'Comment'.";
        return null;
    }

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Adding comment to Confluence page {Id}", args.PageId);

        var payload = new
        {
            type = "comment",
            container = new { id = args.PageId, type = "page" },
            body = new
            {
                storage = new
                {
                    value = $"<p>{args.Comment}</p>",
                    representation = "storage"
                }
            },
        };

        return await client.PostAsync(AtlassianProduct.Confluence, $"content/{args.PageId}/child/comment", payload, ct);
    }
}
