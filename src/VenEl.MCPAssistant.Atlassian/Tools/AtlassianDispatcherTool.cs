using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using VenEl.MCPAssistant.Core.Dispatcher;

namespace VenEl.MCPAssistant.Atlassian.Tools;

[McpServerToolType]
public sealed class AtlassianDispatcherTool : DispatcherToolBase<AtlassianCommandArgs>
{
    public AtlassianDispatcherTool(IServiceProvider serviceProvider) 
        : base(serviceProvider, "Atlassian")
    {
    }

    protected override string? GetRequestedAction(AtlassianCommandArgs args) => args.Action;

    [McpServerTool(Name = "mcp_venel_atlassian_commands")]
    [Description("Atlassian Cloud tools: Jira issues/projects/sprints, Confluence pages/spaces, Bitbucket repositories/pull requests/pipelines, and session credential setup.")]
    public Task<string> DispatchAtlassianCommandAsync(
        [Description("The arguments for the Atlassian command")] AtlassianCommandArgs args,
        CancellationToken ct)
    {
        return DispatchAsync(args, ct);
    }
}
