using System.ComponentModel;

namespace VenEl.MCPAssistant.Databricks.Tools;

public class DatabricksCommandArgs
{
    [Description("The action to perform. Valid values: databricks_list_jobs, databricks_run_job, databricks_list_clusters, databricks_start_cluster, databricks_stop_cluster, databricks_list_workspace.")]
    public string Action { get; set; } = string.Empty;

    [Description("Required for job operations. The Job ID.")]
    public string? JobId { get; set; }

    [Description("Required for cluster operations. The Cluster ID.")]
    public string? ClusterId { get; set; }

    [Description("Required for list-workspace. The workspace path (e.g., /Users/me@example.com).")]
    public string? WorkspacePath { get; set; }
}
