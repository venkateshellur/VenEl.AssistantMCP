using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using VenEl.MCP.Core.Dispatcher;

namespace VenEl.MCP.MSSql.Tools;

[McpServerToolType]
public class MSSqlDispatcherTool : DispatcherToolBase<MSSqlCommandArgs>
{
    public MSSqlDispatcherTool(IServiceProvider serviceProvider) 
        : base(serviceProvider, "MSSql")
    {
    }

    protected override string? GetRequestedAction(MSSqlCommandArgs args) => args.Action;

    [McpServerTool(Name = "mssql_commands")]
    [Description("Microsoft SQL Server tools: execute queries, modify data, call stored procedures, inspect schema, export to Excel, and generate markdown schema docs.")]
    public Task<string> ExecuteAsync(MSSqlCommandArgs args, CancellationToken ct)
    {
        return DispatchAsync(args, ct);
    }
}
