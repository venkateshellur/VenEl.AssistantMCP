using System;
using System.Threading;
using System.Threading.Tasks;
using VenEl.MCPAssistant.Core.Dispatcher;
using VenEl.MCPAssistant.Databricks.Services;

namespace VenEl.MCPAssistant.Databricks.Tools;

public sealed class DatabricksListJobsActionHandler(DatabricksHttpClient client) : IActionHandler<DatabricksCommandArgs>
{
    public string ActionName => "databricks_list_jobs";

    public string? Validate(DatabricksCommandArgs args) => null;

    public async Task<string> HandleAsync(DatabricksCommandArgs args, CancellationToken ct)
    {
        return await client.GetAsync("/api/2.1/jobs/list?limit=50", ct);
    }
}

public sealed class DatabricksRunJobActionHandler(DatabricksHttpClient client) : IActionHandler<DatabricksCommandArgs>
{
    public string ActionName => "databricks_run_job";

    public string? Validate(DatabricksCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.JobId)) return "Missing required parameter 'JobId'.";
        return null;
    }

    public async Task<string> HandleAsync(DatabricksCommandArgs args, CancellationToken ct)
    {
        var payload = new { job_id = long.Parse(args.JobId!) };
        return await client.PostAsync("/api/2.1/jobs/run-now", payload, ct);
    }
}

public sealed class DatabricksListClustersActionHandler(DatabricksHttpClient client) : IActionHandler<DatabricksCommandArgs>
{
    public string ActionName => "databricks_list_clusters";

    public string? Validate(DatabricksCommandArgs args) => null;

    public async Task<string> HandleAsync(DatabricksCommandArgs args, CancellationToken ct)
    {
        return await client.GetAsync("/api/2.0/clusters/list", ct);
    }
}

public sealed class DatabricksStartClusterActionHandler(DatabricksHttpClient client) : IActionHandler<DatabricksCommandArgs>
{
    public string ActionName => "databricks_start_cluster";

    public string? Validate(DatabricksCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.ClusterId)) return "Missing required parameter 'ClusterId'.";
        return null;
    }

    public async Task<string> HandleAsync(DatabricksCommandArgs args, CancellationToken ct)
    {
        var payload = new { cluster_id = args.ClusterId };
        return await client.PostAsync("/api/2.0/clusters/start", payload, ct);
    }
}

public sealed class DatabricksStopClusterActionHandler(DatabricksHttpClient client) : IActionHandler<DatabricksCommandArgs>
{
    public string ActionName => "databricks_stop_cluster";

    public string? Validate(DatabricksCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.ClusterId)) return "Missing required parameter 'ClusterId'.";
        return null;
    }

    public async Task<string> HandleAsync(DatabricksCommandArgs args, CancellationToken ct)
    {
        var payload = new { cluster_id = args.ClusterId };
        return await client.PostAsync("/api/2.0/clusters/delete", payload, ct);
    }
}

public sealed class DatabricksListWorkspaceActionHandler(DatabricksHttpClient client) : IActionHandler<DatabricksCommandArgs>
{
    public string ActionName => "databricks_list_workspace";

    public string? Validate(DatabricksCommandArgs args) => null;

    public async Task<string> HandleAsync(DatabricksCommandArgs args, CancellationToken ct)
    {
        var path = args.WorkspacePath ?? "/";
        return await client.GetAsync($"/api/2.0/workspace/list?path={Uri.EscapeDataString(path)}", ct);
    }
}
