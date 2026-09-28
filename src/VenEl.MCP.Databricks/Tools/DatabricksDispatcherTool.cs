using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using VenEl.MCP.Core.Dispatcher;

namespace VenEl.MCP.Databricks.Tools;

[McpServerToolType]
public class DatabricksDispatcherTool : DispatcherToolBase<DatabricksCommandArgs>
{
    public DatabricksDispatcherTool(IServiceProvider serviceProvider) 
        : base(serviceProvider, "Databricks")
    {
    }

    protected override string? GetRequestedAction(DatabricksCommandArgs args) => args.Action;

    [McpServerTool(Name = "mcp_databricks_commands")]
    [Description("Databricks tools: manage jobs, clusters, and workspace files.")]
    public Task<string> ExecuteAsync(DatabricksCommandArgs args, CancellationToken ct)
    {
        return DispatchAsync(args, ct);
    }
}
