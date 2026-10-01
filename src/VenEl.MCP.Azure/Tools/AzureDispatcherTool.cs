using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using VenEl.MCP.Core.Dispatcher;

namespace VenEl.MCP.Azure.Tools;

[McpServerToolType]
public class AzureDispatcherTool : DispatcherToolBase<AzureCommandArgs>
{
    public AzureDispatcherTool(IServiceProvider serviceProvider) 
        : base(serviceProvider, "Azure")
    {
    }

    protected override string? GetRequestedAction(AzureCommandArgs args) => args.Action;

    [McpServerTool(Name = "azure_commands")]
    [Description("Azure DevOps tools (projects, repos, PRs, pipelines, work items), Key Vault access (secrets, certificates), Blob Storage, and session credential setup.")]
    public Task<string> ExecuteAsync(AzureCommandArgs args, CancellationToken ct)
    {
        return DispatchAsync(args, ct);
    }
}
