using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenEl.MCP.Azure.Services;
using VenEl.MCP.Core.Dispatcher;

namespace VenEl.MCP.Azure.Tools;

public sealed class AzureListPipelinesActionHandler(IAzureHttpClient client, ILogger<AzureListPipelinesActionHandler> logger) : IActionHandler<AzureCommandArgs>
{
    public string ActionName => "azure_list_pipelines";

    public string? Validate(AzureCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Project)) return "Missing required parameter 'Project'.";
        return null;
    }

    public async Task<string> HandleAsync(AzureCommandArgs args, CancellationToken ct)
    {
        int top = Math.Clamp(args.Top ?? 50, 1, 100);
        logger.LogDebug("Listing Azure DevOps pipelines for project {Project}", args.Project);
        return await client.GetAsync(AzureProduct.DevOps, $"{args.Project}/_apis/pipelines?$top={top}", "7.1-preview.1", null, ct);
    }
}

public sealed class AzureRunPipelineActionHandler(IAzureHttpClient client, ILogger<AzureRunPipelineActionHandler> logger) : IActionHandler<AzureCommandArgs>
{
    public string ActionName => "azure_run_pipeline";

    public string? Validate(AzureCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Project)) return "Missing required parameter 'Project'.";
        if (args.PipelineId == null) return "Missing required parameter 'PipelineId'.";
        return null;
    }

    public async Task<string> HandleAsync(AzureCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Running pipeline {PipelineId} in project {Project}", args.PipelineId, args.Project);
        var payload = new { };
        return await client.PostAsync(AzureProduct.DevOps, $"{args.Project}/_apis/pipelines/{args.PipelineId}/runs", payload, "7.1-preview.1", null, ct);
    }
}

public sealed class AzureGetPipelineRunActionHandler(IAzureHttpClient client, ILogger<AzureGetPipelineRunActionHandler> logger) : IActionHandler<AzureCommandArgs>
{
    public string ActionName => "azure_get_pipeline_run";

    public string? Validate(AzureCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Project)) return "Missing required parameter 'Project'.";
        if (args.PipelineId == null) return "Missing required parameter 'PipelineId'.";
        if (args.RunId == null) return "Missing required parameter 'RunId'.";
        return null;
    }

    public async Task<string> HandleAsync(AzureCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Getting pipeline run {RunId} for pipeline {PipelineId} in project {Project}", args.RunId, args.PipelineId, args.Project);
        return await client.GetAsync(AzureProduct.DevOps, $"{args.Project}/_apis/pipelines/{args.PipelineId}/runs/{args.RunId}", "7.1-preview.1", null, ct);
    }
}
