using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using VenEl.MCP.Core.Dispatcher;
using VenEl.MCP.ServiceNow.Configuration;
using VenEl.MCP.ServiceNow.Client;

namespace VenEl.MCP.ServiceNow.Tools;

public class GetRecordActionHandler : IActionHandler<ServiceNowCommandArgs>
{
    private readonly ResilientServiceNowClient _client;
    private readonly ServiceNowOptions _options;

    public GetRecordActionHandler(ResilientServiceNowClient client, IOptions<ServiceNowOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public string ActionName => "get_record";

    public string? Validate(ServiceNowCommandArgs args)
    {
        if (string.IsNullOrEmpty(_options.InstanceUrl)) return "ServiceNow InstanceUrl is missing in configuration.";
        if (string.IsNullOrEmpty(args.Table)) return "Table name is required (e.g., 'incident').";
        if (string.IsNullOrEmpty(args.SysId)) return "SysId is required to get a specific record.";
        return null;
    }

    public async Task<string> HandleAsync(ServiceNowCommandArgs args, CancellationToken ct)
    {
        // Table API path: table/{tableName}/{sys_id}
        string path = $"{args.Table}/{args.SysId}";
        return await _client.GetAsync("table", path, ct);
    }
}
