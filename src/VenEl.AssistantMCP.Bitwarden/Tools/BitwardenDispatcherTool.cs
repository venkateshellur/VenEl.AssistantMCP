using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using VenEl.AssistantMCP.Core.Dispatcher;

namespace VenEl.AssistantMCP.Bitwarden.Tools;

[McpServerToolType]
public sealed class BitwardenDispatcherTool : DispatcherToolBase<BitwardenCommandArgs>
{
    public BitwardenDispatcherTool(IServiceProvider serviceProvider) 
        : base(serviceProvider, "Bitwarden")
    {
    }

    protected override string? GetRequestedAction(BitwardenCommandArgs args) => args.Action;

    [McpServerTool(Name = "mcp_venel_bitwarden_commands")]
    [Description("Bitwarden tools: Get secrets from the Bitwarden vault or Secrets Manager.")]
    public Task<string> DispatchBitwardenCommandAsync(
        [Description("The arguments for the Bitwarden command")] BitwardenCommandArgs args,
        CancellationToken ct)
    {
        return DispatchAsync(args, ct);
    }
}
