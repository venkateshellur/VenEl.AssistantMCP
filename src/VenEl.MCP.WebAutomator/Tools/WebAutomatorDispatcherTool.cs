using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using VenEl.MCP.Core.Dispatcher;

namespace VenEl.MCP.WebAutomator.Tools;

[McpServerToolType]
public class WebAutomatorDispatcherTool : DispatcherToolBase<WebAutomatorArgs>
{
    public WebAutomatorDispatcherTool(IServiceProvider serviceProvider) 
        : base(serviceProvider, "WebAutomator")
    {
    }

    protected override string? GetRequestedAction(WebAutomatorArgs args) => args.Action;

    [McpServerTool(Name = "web_commands")]
    [Description("Web automation tools: navigate, click, fill, evaluate scripts, extract data, and take full page screenshots.")]
    public Task<string> ExecuteAsync(WebAutomatorArgs args, CancellationToken ct)
    {
        return DispatchAsync(args, ct);
    }
}
