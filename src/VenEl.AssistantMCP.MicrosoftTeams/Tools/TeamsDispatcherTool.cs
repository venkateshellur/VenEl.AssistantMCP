using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using VenEl.AssistantMCP.Core.Dispatcher;

namespace VenEl.AssistantMCP.MicrosoftTeams.Tools;

[McpServerToolType]
public sealed class TeamsDispatcherTool : DispatcherToolBase<TeamsCommandArgs>
{
    public TeamsDispatcherTool(IServiceProvider serviceProvider) 
        : base(serviceProvider, "MicrosoftTeams")
    {
    }

    protected override string? GetRequestedAction(TeamsCommandArgs args) => args.Action;

    [McpServerTool(Name = "mcp_venel_teams_commands")]
    [Description("Microsoft Teams integration tools: Post messages via Graph API or Webhooks.")]
    public Task<string> DispatchTeamsCommandAsync(
        [Description("The arguments for the Microsoft Teams command")] TeamsCommandArgs args,
        CancellationToken ct)
    {
        return DispatchAsync(args, ct);
    }
}
