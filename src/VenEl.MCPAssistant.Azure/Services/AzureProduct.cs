namespace VenEl.MCPAssistant.Azure.Services;

/// <summary>
/// Delineates the target Azure product for HTTP requests,
/// as routing and base URLs differ between them.
/// </summary>
public enum AzureProduct
{
    DevOps,
    // Future products can be added here, e.g., Databricks, ResourceTracker
}
