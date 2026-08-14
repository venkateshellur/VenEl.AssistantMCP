using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using VenEl.AssistantMCP.Core.Dispatcher;

namespace VenEl.AssistantMCP.WebAutomator.Tools;

[McpServerToolType]
public class WebAutomatorDispatcherTool : DispatcherToolBase<WebAutomatorArgs>
{
    public WebAutomatorDispatcherTool(IServiceProvider serviceProvider) 
        : base(serviceProvider, "WebAutomator")
    {
    }

    protected override string? GetRequestedAction(WebAutomatorArgs args) => args.Action;

    [McpServerTool(Name = "mcp_web_automator")]
    [Description("Use a headless Chromium browser to navigate the web, extract rendered HTML/text from SPAs, click buttons, and fill forms.")]
    public Task<string> ExecuteAsync(WebAutomatorArgs args, CancellationToken ct)
    {
        return DispatchAsync(args, ct);
    }
}
