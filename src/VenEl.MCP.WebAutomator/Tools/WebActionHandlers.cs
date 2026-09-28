using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenEl.MCP.Core.Dispatcher;
using VenEl.MCP.WebAutomator.Services;

namespace VenEl.MCP.WebAutomator.Tools;

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
        await page.GotoAsync(args.Url!, new Microsoft.Playwright.PageGotoOptions { WaitUntil = Microsoft.Playwright.WaitUntilState.DOMContentLoaded });
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

public sealed class WebTakeScreenshotActionHandler(PlaywrightBrowserManager browserManager, ILogger<WebTakeScreenshotActionHandler> logger) : IActionHandler<WebAutomatorArgs>
{
    public string ActionName => "web_take_screenshot";

    public string? Validate(WebAutomatorArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.OutputPath)) return "Missing required parameter 'outputPath'.";
        return null;
    }

    public async Task<string> HandleAsync(WebAutomatorArgs args, CancellationToken ct)
    {
        logger.LogInformation("Taking full page screenshot to {OutputPath}", args.OutputPath);
        var page = await browserManager.GetOrCreatePageAsync();
        await page.ScreenshotAsync(new Microsoft.Playwright.PageScreenshotOptions
        {
            Path = args.OutputPath,
            FullPage = true
        });
        return $"Screenshot saved to {args.OutputPath}";
    }
}

public sealed class WebExtractDataActionHandler(PlaywrightBrowserManager browserManager, ILogger<WebExtractDataActionHandler> logger) : IActionHandler<WebAutomatorArgs>
{
    public string ActionName => "web_extract_data";

    public string? Validate(WebAutomatorArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Selector)) return "Missing required parameter 'selector'.";
        return null;
    }

    public async Task<string> HandleAsync(WebAutomatorArgs args, CancellationToken ct)
    {
        logger.LogInformation("Extracting data for selector {Selector}", args.Selector);
        var page = await browserManager.GetOrCreatePageAsync();
        var element = await page.QuerySelectorAsync(args.Selector!);
        if (element == null) return "Element not found.";

        if (!string.IsNullOrWhiteSpace(args.ExtractAttribute))
        {
            var attr = await element.GetAttributeAsync(args.ExtractAttribute!);
            return attr ?? "null";
        }
        else
        {
            return await element.InnerTextAsync();
        }
    }
}
