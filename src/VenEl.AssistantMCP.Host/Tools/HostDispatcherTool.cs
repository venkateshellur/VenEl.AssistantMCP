using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using VenEl.AssistantMCP.Core.Dispatcher;

namespace VenEl.AssistantMCP.Host.Tools;

[McpServerToolType]
public sealed class HostDispatcherTool(IServiceProvider serviceProvider) : DispatcherToolBase<HostCommandArgs>(serviceProvider, "Host")
{
    protected override string? GetRequestedAction(HostCommandArgs args) => args.Action;

    [McpServerTool(Name = "host_commands")]
    [Description("Interact natively with the Host Operating System (Read/Write Files, Execute shell commands). Note: Executing commands runs with the same permissions as the MCP Server.")]
    public Task<string> ExecuteAsync(
        [Description("The host command arguments. Action can be: local_read_file, local_write_file, local_run_command.")] HostCommandArgs args, 
        CancellationToken ct)
    {
        return base.DispatchAsync(args, ct);
    }
}
