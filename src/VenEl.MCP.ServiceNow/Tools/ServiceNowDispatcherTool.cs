using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using VenEl.MCP.Core.Dispatcher;

namespace VenEl.MCP.ServiceNow.Tools;

[McpServerToolType]
public class ServiceNowDispatcherTool : DispatcherToolBase<ServiceNowCommandArgs>
{
    public ServiceNowDispatcherTool(IServiceProvider serviceProvider) 
        : base(serviceProvider, "ServiceNow")
    {
    }

    protected override string? GetRequestedAction(ServiceNowCommandArgs args) => args.Action;

    [McpServerTool(Name = "servicenow_commands")]
    [Description("Interact with ServiceNow API. Actions: get_record, search_records, create_record, update_record")]
    public Task<string> ExecuteAsync(ServiceNowCommandArgs args, CancellationToken ct)
    {
        return DispatchAsync(args, ct);
    }
}
