using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace VenEl.AssistantMCP.WebAutomator.Services;

public sealed class PlaywrightBrowserManager : IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;
    private readonly SemaphoreSlim _lock = new(1, 1);
    
    private bool _isPlaywrightInstalled;
    private bool _isDisabled;
    private string? _disableReason;

    public async Task<IPage> GetOrCreatePageAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_isDisabled)
            {
                throw new InvalidOperationException($"Web Automation is currently disabled on this machine. Reason: {_disableReason}");
            }

            if (_page != null && !_page.IsClosed)
            {
                return _page;
            }

            if (!_isPlaywrightInstalled)
            {
                try
                {
                    // Attempt to auto-install Playwright browsers (Chromium) programmaticallly
                    int exitCode = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
                    if (exitCode != 0)
                    {
                        _isDisabled = true;
                        _disableReason = $"Playwright installer failed with exit code {exitCode}.";
                        throw new InvalidOperationException($"Web Automation is currently disabled on this machine. Reason: {_disableReason}");
                    }
                    _isPlaywrightInstalled = true;
                }
                catch (Exception ex)
                {
                    _isDisabled = true;
                    _disableReason = ex.Message;
                    throw new InvalidOperationException($"Web Automation is currently disabled on this machine. Reason: {_disableReason}");
                }
            }

            if (_playwright == null)
            {
                _playwright = await Playwright.CreateAsync();
                _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
                _context = await _browser.NewContextAsync();
            }

            if (_page == null || _page.IsClosed)
            {
                _page = await _context!.NewPageAsync();
                await _page.RouteAsync("**/*", async route =>
                {
                    if (route.Request.ResourceType == "image" ||
                        route.Request.ResourceType == "stylesheet" ||
                        route.Request.ResourceType == "font" ||
                        route.Request.ResourceType == "media")
                    {
                        await route.AbortAsync();
                    }
                    else
                    {
                        await route.ContinueAsync();
                    }
                });
            }
            return _page;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser != null) await _browser.DisposeAsync();
        _playwright?.Dispose();
    }
}
