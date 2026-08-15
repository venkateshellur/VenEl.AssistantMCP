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

public sealed class ConfluenceAddAttachmentActionHandler(IAtlassianHttpClient client, ILogger<ConfluenceAddAttachmentActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "confluence_add_attachment";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.PageId)) return "Missing required parameter 'PageId'.";
        if (string.IsNullOrWhiteSpace(args.FilePath)) return "Missing required parameter 'FilePath'.";
        if (!System.IO.File.Exists(args.FilePath)) return $"File not found at path: {args.FilePath}";
        return null;
    }

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Adding attachment {FilePath} to Confluence page {PageId}", args.FilePath, args.PageId);
        
        var fileName = System.IO.Path.GetFileName(args.FilePath!);
        var fileStream = System.IO.File.OpenRead(args.FilePath!);
        
        using var formData = new System.Net.Http.MultipartFormDataContent();
        var fileContent = new System.Net.Http.StreamContent(fileStream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        formData.Add(fileContent, "file", fileName);
        formData.Add(new System.Net.Http.StringContent("Attached by AssistantMCP"), "comment");

        return await client.PostMultipartAsync(AtlassianProduct.Confluence, $"content/{args.PageId}/child/attachment", formData, ct);
    }
}

public sealed class ConfluenceGetPageChildrenActionHandler(IAtlassianHttpClient client, ILogger<ConfluenceGetPageChildrenActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "confluence_get_page_children";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.PageId)) return "Missing required parameter 'PageId'.";
        return null;
    }

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        int limit = Math.Clamp(args.Limit ?? 50, 1, 100);
        logger.LogDebug("Getting children for Confluence page {PageId}", args.PageId);
        return await client.GetAsync(AtlassianProduct.Confluence, $"content/{args.PageId}/child/page?limit={limit}&expand=version,space", ct);
    }
}

public sealed class ConfluenceAddLabelActionHandler(IAtlassianHttpClient client, ILogger<ConfluenceAddLabelActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "confluence_add_label";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.PageId)) return "Missing required parameter 'PageId'.";
        if (string.IsNullOrWhiteSpace(args.LabelName)) return "Missing required parameter 'LabelName'.";
        return null;
    }

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Adding label '{Label}' to Confluence page {PageId}", args.LabelName, args.PageId);
        var payload = new[] { new { prefix = "global", name = args.LabelName } };
        return await client.PostAsync(AtlassianProduct.Confluence, $"content/{args.PageId}/label", payload, ct);
    }
}

public sealed class ConfluenceExportPageActionHandler(IAtlassianHttpClient client, ILogger<ConfluenceExportPageActionHandler> logger) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "confluence_export_page";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.PageId)) return "Missing required parameter 'PageId'.";
        if (string.IsNullOrWhiteSpace(args.DownloadPath)) return "Missing required parameter 'DownloadPath'.";
        return null;
    }

    public async Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Exporting Confluence page {PageId} to {Path}", args.PageId, args.DownloadPath);
        
        var response = await client.GetAsync(AtlassianProduct.Confluence, $"content/{args.PageId}?expand=body.export_view", ct);
        if (response.StartsWith("[HTTP") || response.StartsWith("[ERROR]"))
        {
            return response;
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(response);
            if (doc.RootElement.TryGetProperty("body", out var body) && 
                body.TryGetProperty("export_view", out var exportView) && 
                exportView.TryGetProperty("value", out var htmlValue))
            {
                var html = htmlValue.GetString();
                await System.IO.File.WriteAllTextAsync(args.DownloadPath!, html, ct);
                return $"Successfully exported page as HTML to: {args.DownloadPath}\n\nNote: Native PDF/Word export via API tokens is restricted by Atlassian Cloud, so HTML was exported instead.";
            }
            return "[ERROR] Could not parse export_view body from Confluence response.";
        }
        catch (Exception ex)
        {
            return $"[ERROR] Failed to save exported page: {ex.Message}";
        }
    }
}
