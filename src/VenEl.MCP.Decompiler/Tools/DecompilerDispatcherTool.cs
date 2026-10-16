using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using VenEl.MCP.Core.Dispatcher;

namespace VenEl.MCP.Decompiler.Tools;

[McpServerToolType]
public sealed class DecompilerDispatcherTool : DispatcherToolBase<DecompilerCommandArgs>
{
    public DecompilerDispatcherTool(IServiceProvider serviceProvider) 
        : base(serviceProvider, "Decompiler")
    {
    }

    protected override string? GetRequestedAction(DecompilerCommandArgs args) => args.Action;

    [McpServerTool(Name = "mcp_venel_decompiler_commands")]
    [Description("Decompiler & code reverse-engineering tools: decompile and inspect .NET assemblies (.dll, .exe) and Java binaries (.class, .jar) to view types, methods, dependencies, and decompiled source. Actions: decompile, get_outline, get_metadata")]
    public Task<string> DispatchDecompilerCommandAsync(
        [Description("The arguments for the Decompiler command")] DecompilerCommandArgs args,
        CancellationToken ct)
    {
        return DispatchAsync(args, ct);
    }
}
