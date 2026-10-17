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
    [Description("Decompiler & code reverse-engineering tools: decompile and inspect .NET assemblies (.dll, .exe), Java (.class, .jar), WebAssembly (.wasm), and native binaries (.dylib, .so, native .dll) to view types, outline, dependencies, high-level source, and raw IL/bytecode disassembly. Also reconstructs full source projects to disk. Actions: decompile, get_outline, get_metadata, export_project, disassemble_il")]
    public Task<string> DispatchDecompilerCommandAsync(
        [Description("The arguments for the Decompiler command")] DecompilerCommandArgs args,
        CancellationToken ct)
    {
        return DispatchAsync(args, ct);
    }
}
