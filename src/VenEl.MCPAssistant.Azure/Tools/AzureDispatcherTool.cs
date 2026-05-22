using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using VenEl.MCPAssistant.Core.Dispatcher;

namespace VenEl.MCPAssistant.Azure.Tools;

[McpServerToolType]
public class AzureDispatcherTool : DispatcherToolBase<AzureCommandArgs>
{
    public AzureDispatcherTool(IServiceProvider serviceProvider) 
        : base(serviceProvider, "Azure")
    {
    }

    protected override string? GetRequestedAction(AzureCommandArgs args) => args.Action;

    [McpServerTool(Name = "azure_commands")]
    [Description("Azure DevOps tools: projects, repositories, pull requests, pipelines, and session credential setup.")]
    public Task<string> ExecuteAsync(AzureCommandArgs args, CancellationToken ct)
    {
        return DispatchAsync(args, ct);
    }
}
