using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using VenEl.MCPAssistant.Core.Dispatcher;

namespace VenEl.MCPAssistant.GCP.Tools;

[McpServerToolType]
public sealed class GcpDispatcherTool : DispatcherToolBase<GcpCommandArgs>
{
    public GcpDispatcherTool(IServiceProvider serviceProvider) 
        : base(serviceProvider, "GCP")
    {
    }

    protected override string? GetRequestedAction(GcpCommandArgs args) => args.Action;

    [McpServerTool(Name = "mcp_venel_gcp_commands")]
    [Description("GCP integration tools: List Cloud Storage buckets.")]
    public Task<string> DispatchGcpCommandAsync(
        [Description("The arguments for the GCP command")] GcpCommandArgs args,
        CancellationToken ct)
    {
        return DispatchAsync(args, ct);
    }
}
