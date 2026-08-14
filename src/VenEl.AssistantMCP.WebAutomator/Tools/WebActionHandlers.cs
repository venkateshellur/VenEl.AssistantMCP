using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenEl.AssistantMCP.Core.Dispatcher;
using VenEl.AssistantMCP.WebAutomator.Services;

namespace VenEl.AssistantMCP.WebAutomator.Tools;

public sealed class WebNavigateActionHandler(PlaywrightBrowserManager browserManager, ILogger<WebNavigateActionHandler> logger) : IActionHandler<WebAutomatorArgs>
{
    public string ActionName => "web_navigate";

    public string? Validate(WebAutomatorArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Url)) return "Missing required parameter 'url'.";
        return null;
    }

    public async Task<string> HandleAsync(WebAutomatorArgs args, CancellationToken ct)
    {
        logger.LogInformation("Navigating to {Url}", args.Url);
        var page = await browserManager.GetOrCreatePageAsync();
        await page.GotoAsync(args.Url!, new Microsoft.Playwright.PageGotoOptions { WaitUntil = Microsoft.Playwright.WaitUntilState.NetworkIdle });
        return await page.EvaluateAsync<string>("document.body.innerText");
    }
}

public sealed class WebClickActionHandler(PlaywrightBrowserManager browserManager, ILogger<WebClickActionHandler> logger) : IActionHandler<WebAutomatorArgs>
{
    public string ActionName => "web_click";

    public string? Validate(WebAutomatorArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Selector)) return "Missing required parameter 'selector'.";
        return null;
    }

    public async Task<string> HandleAsync(WebAutomatorArgs args, CancellationToken ct)
    {
        logger.LogInformation("Clicking selector {Selector}", args.Selector);
        var page = await browserManager.GetOrCreatePageAsync();
        await page.ClickAsync(args.Selector!);
        // Wait a bit for SPA to re-render
        await Task.Delay(1000, ct); 
        return await page.EvaluateAsync<string>("document.body.innerText");
    }
}

public sealed class WebFillActionHandler(PlaywrightBrowserManager browserManager, ILogger<WebFillActionHandler> logger) : IActionHandler<WebAutomatorArgs>
{
    public string ActionName => "web_fill";

    public string? Validate(WebAutomatorArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Selector)) return "Missing required parameter 'selector'.";
        if (string.IsNullOrWhiteSpace(args.Text)) return "Missing required parameter 'text'.";
        return null;
    }

    public async Task<string> HandleAsync(WebAutomatorArgs args, CancellationToken ct)
    {
        logger.LogInformation("Filling selector {Selector}", args.Selector);
        var page = await browserManager.GetOrCreatePageAsync();
        await page.FillAsync(args.Selector!, args.Text!);
        return $"Successfully filled {args.Selector} with provided text.";
    }
}

public sealed class WebEvaluateActionHandler(PlaywrightBrowserManager browserManager, ILogger<WebEvaluateActionHandler> logger) : IActionHandler<WebAutomatorArgs>
{
    public string ActionName => "web_evaluate";

    public string? Validate(WebAutomatorArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Script)) return "Missing required parameter 'script'.";
        return null;
    }

    public async Task<string> HandleAsync(WebAutomatorArgs args, CancellationToken ct)
    {
        logger.LogInformation("Evaluating JS script");
        var page = await browserManager.GetOrCreatePageAsync();
        var result = await page.EvaluateAsync<object>(args.Script!);
        return result?.ToString() ?? "null";
    }
}
