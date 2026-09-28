using System.ComponentModel;

namespace VenEl.MCP.Azure.Tools;

public class AzureCommandArgs
{
    [Description("The action to perform. Options: azure_list_projects, azure_list_repos, azure_list_pull_requests, azure_configure, azure_show_config")]
    public string Action { get; set; } = string.Empty;

    // DevOps specific
    [Description("Maximum projects or PRs to return (default 50 or 25). Used by azure_list_projects, azure_list_pull_requests.")]
    public int? Top { get; set; }

    [Description("The name or ID of the project. Used by azure_list_repos, azure_list_pull_requests.")]
    public string? Project { get; set; }

    [Description("The name or ID of the repository. Used by azure_list_pull_requests.")]
    public string? RepositoryId { get; set; }

    // Setup specific
    [Description("Your Azure DevOps organization URL, e.g., 'https://dev.azure.com/your-org'. Used by azure_configure.")]
    public string? OrganizationUrl { get; set; }

    [Description("Your Azure Personal Access Token (PAT). Used by azure_configure.")]
    public string? PatToken { get; set; }

    // Pipelines specific
    [Description("The ID of the pipeline. Used by azure_list_pipelines, azure_run_pipeline.")]
    public int? PipelineId { get; set; }

    [Description("The ID of the pipeline run. Used by azure_run_pipeline, azure_get_pipeline_run.")]
    public int? RunId { get; set; }

    [Description("The ID of the pull request. Used by azure_get_pull_request.")]
    public int? PullRequestId { get; set; }

    [Description("The source ref name (e.g., refs/heads/branch). Used by azure_create_pull_request.")]
    public string? SourceRefName { get; set; }

    [Description("The target ref name (e.g., refs/heads/main). Used by azure_create_pull_request.")]
    public string? TargetRefName { get; set; }

    [Description("The title of the pull request. Used by azure_create_pull_request.")]
    public string? Title { get; set; }

    [Description("The description of the pull request. Used by azure_create_pull_request.")]
    public string? Description { get; set; }

    [Description("The ID of the work item. Used by azure_get_work_item.")]
    public int? WorkItemId { get; set; }

    [Description("The name of the Azure Key Vault. Used by azure_get_secret, azure_list_secrets, azure_get_certificate.")]
    public string? VaultName { get; set; }

    [Description("The name of the secret. Used by azure_get_secret.")]
    public string? SecretName { get; set; }

    [Description("The name of the certificate. Used by azure_get_certificate.")]
    public string? CertificateName { get; set; }
}
