using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using VenEl.AssistantMCP.Core.Dispatcher;

namespace VenEl.AssistantMCP.Docker.Tools;

[McpServerToolType]
public sealed class DockerDispatcherTool : DispatcherToolBase<DockerCommandArgs>
{
    public DockerDispatcherTool(IServiceProvider serviceProvider) 
        : base(serviceProvider, "Docker")
    {
    }

    protected override string? GetRequestedAction(DockerCommandArgs args) => args.Action;

    [McpServerTool(Name = "mcp_venel_docker_commands")]
    [Description("Docker Management tools: list/start/stop/restart containers, view logs, list/build images, and compose up/down via the Docker CLI.")]
    public Task<string> DispatchDockerCommandAsync(
        [Description("The arguments for the Docker command")] DockerCommandArgs args,
        CancellationToken ct)
    {
        return DispatchAsync(args, ct);
    }
}
