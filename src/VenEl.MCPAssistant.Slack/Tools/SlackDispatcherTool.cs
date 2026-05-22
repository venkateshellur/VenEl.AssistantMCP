using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using VenEl.MCPAssistant.Core.Dispatcher;

namespace VenEl.MCPAssistant.Slack.Tools;

[McpServerToolType]
public sealed class SlackDispatcherTool : DispatcherToolBase<SlackCommandArgs>
{
    public SlackDispatcherTool(IServiceProvider serviceProvider) 
        : base(serviceProvider, "Slack")
    {
    }

    protected override string? GetRequestedAction(SlackCommandArgs args) => args.Action;

    [McpServerTool(Name = "mcp_venel_slack_commands")]
    [Description("Slack integration tools: Post messages via webhooks.")]
    public Task<string> DispatchSlackCommandAsync(
        [Description("The arguments for the Slack command")] SlackCommandArgs args,
        CancellationToken ct)
    {
        return DispatchAsync(args, ct);
    }
}
