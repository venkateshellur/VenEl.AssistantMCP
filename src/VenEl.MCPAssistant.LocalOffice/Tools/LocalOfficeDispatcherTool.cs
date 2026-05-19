using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using VenEl.MCPAssistant.Core.Dispatcher;

namespace VenEl.MCPAssistant.LocalOffice.Tools;

[McpServerToolType]
public sealed class LocalOfficeDispatcherTool : DispatcherToolBase<LocalOfficeCommandArgs>
{
    public LocalOfficeDispatcherTool(IServiceProvider serviceProvider) 
        : base(serviceProvider, "LocalOffice")
    {
    }

    protected override string? GetRequestedAction(LocalOfficeCommandArgs args) => args.Action;

    [McpServerTool(Name = "mcp_venel_localoffice_commands")]
    [Description("Local Office Management tools: Read and write to local Excel files (.xlsx) using OpenXML.")]
    public Task<string> DispatchLocalOfficeCommandAsync(
        [Description("The arguments for the Local Office command")] LocalOfficeCommandArgs args,
        CancellationToken ct)
    {
        return DispatchAsync(args, ct);
    }
}
