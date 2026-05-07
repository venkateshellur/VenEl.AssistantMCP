using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using VenEl.MCPAssistant.Atlassian.Services;

namespace VenEl.MCPAssistant.Atlassian.Tools;

/// <summary>MCP tools for Confluence Cloud REST API.</summary>
[McpServerToolType]
public sealed class AtlassianConfluenceTools(
    IAtlassianHttpClient client,
    ILogger<AtlassianConfluenceTools> logger)
{
    // ═════════════════════════════════════════════════════════════════════════
    // Spaces
    // ═════════════════════════════════════════════════════════════════════════

    [McpServerTool(Name = "confluence_list_spaces")]
    [Description(
        "Lists all Confluence spaces accessible to the configured account. " +
        "Returns space key, name, type (global/personal), and status.")]
    public async Task<string> ConfluenceListSpacesAsync(
        [Description("Maximum spaces to return (default 50, max 100).")] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 100);
        logger.LogDebug("Listing Confluence spaces (limit={Limit})", limit);
        return await client.GetAsync(AtlassianProduct.Confluence,
            $"space?limit={limit}&expand=description.plain", cancellationToken);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Pages
    // ═════════════════════════════════════════════════════════════════════════

    [McpServerTool(Name = "confluence_search_pages")]
    [Description(
        "Searches Confluence content using CQL (Confluence Query Language). " +
        "Example CQL: 'space = \"DEV\" AND title ~ \"API\" ORDER BY lastmodified DESC'.")]
    public async Task<string> ConfluenceSearchPagesAsync(
        [Description("CQL query string.")] string cql,
        [Description("Maximum results to return (default 25, max 100).")] int limit = 25,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 100);
        logger.LogDebug("Searching Confluence with CQL: {Cql}", cql);
        var encoded = Uri.EscapeDataString(cql);
        return await client.GetAsync(AtlassianProduct.Confluence,
            $"content/search?cql={encoded}&limit={limit}&expand=space,version",
            cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "confluence_get_page")]
    [Description(
        "Returns the content of a Confluence page by its numeric ID. " +
        "Includes title, version number, space, and the page body in storage format.")]
    public async Task<string> ConfluenceGetPageAsync(
        [Description("The numeric Confluence page ID.")] string pageId,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting Confluence page {Id}", pageId);
        return await client.GetAsync(AtlassianProduct.Confluence,
            $"content/{pageId}?expand=body.storage,version,space", cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "confluence_create_page")]
    [Description(
        "Creates a new Confluence page in the specified space. " +
        "Body content should be in Confluence Storage Format (XHTML-based). " +
        "For simple content, plain HTML tags like <p>, <h1>, <ul> are accepted.")]
    public async Task<string> ConfluenceCreatePageAsync(
        [Description("The space key to create the page in, e.g. 'DEV'.")] string spaceKey,
        [Description("The page title.")] string title,
        [Description("Page body in Confluence Storage Format (XHTML). Use <p>text</p> for plain paragraphs.")] string bodyContent,
        [Description("Numeric ID of a parent page (optional — creates as top-level if omitted).")] string? parentId = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Creating Confluence page '{Title}' in space {Space}", title, spaceKey);

        var payload = new Dictionary<string, object>
        {
            ["type"]  = "page",
            ["title"] = title,
            ["space"] = new { key = spaceKey },
            ["body"]  = new
            {
                storage = new { value = bodyContent, representation = "storage" }
            },
        };

        if (parentId is not null)
            payload["ancestors"] = new[] { new { id = parentId } };

        return await client.PostAsync(AtlassianProduct.Confluence, "content", payload, cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "confluence_update_page")]
    [Description(
        "Updates the title and/or body of an existing Confluence page. " +
        "You must supply the current version number (retrieved via confluence_get_page). " +
        "The version is incremented automatically.")]
    public async Task<string> ConfluenceUpdatePageAsync(
        [Description("The numeric Confluence page ID.")] string pageId,
        [Description("Current version number of the page (required by Confluence API).")] int currentVersion,
        [Description("New page title (pass the existing title if unchanged).")] string title,
        [Description("New body in Confluence Storage Format.")] string bodyContent,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Updating Confluence page {Id}", pageId);

        var payload = new
        {
            version = new { number = currentVersion + 1 },
            type    = "page",
            title,
            body = new
            {
                storage = new { value = bodyContent, representation = "storage" }
            },
        };

        return await client.PutAsync(AtlassianProduct.Confluence,
            $"content/{pageId}", payload, cancellationToken);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Comments
    // ═════════════════════════════════════════════════════════════════════════

    [McpServerTool(Name = "confluence_add_comment")]
    [Description("Adds a plain-text comment to a Confluence page.")]
    public async Task<string> ConfluenceAddCommentAsync(
        [Description("The numeric Confluence page ID.")] string pageId,
        [Description("The comment text.")] string comment,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Adding comment to Confluence page {Id}", pageId);

        var payload = new
        {
            type      = "comment",
            container = new { id = pageId, type = "page" },
            body = new
            {
                storage = new
                {
                    value          = $"<p>{comment}</p>",
                    representation = "storage"
                }
            },
        };

        return await client.PostAsync(AtlassianProduct.Confluence,
            $"content/{pageId}/child/comment", payload, cancellationToken);
    }
}
