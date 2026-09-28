using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenEl.MCP.Azure.Services;
using VenEl.MCP.Core.Dispatcher;

namespace VenEl.MCP.Azure.Tools;

public sealed class AzureGetWorkItemActionHandler(IAzureHttpClient client, ILogger<AzureGetWorkItemActionHandler> logger) : IActionHandler<AzureCommandArgs>
{
    public string ActionName => "azure_get_work_item";

    public string? Validate(AzureCommandArgs args)
    {
        if (args.WorkItemId == null) return "Missing required parameter 'WorkItemId'.";
        return null;
    }

    public async Task<string> HandleAsync(AzureCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Getting work item {WorkItemId}", args.WorkItemId);
        
        string path = args.Project != null 
            ? $"{args.Project}/_apis/wit/workitems/{args.WorkItemId}" 
            : $"_apis/wit/workitems/{args.WorkItemId}";
            
        return await client.GetAsync(AzureProduct.DevOps, path, "7.1", null, ct);
    }
}
