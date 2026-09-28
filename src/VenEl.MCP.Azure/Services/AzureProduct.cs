namespace VenEl.MCP.Azure.Services;

/// <summary>
/// Delineates the target Azure product for HTTP requests,
/// as routing and base URLs differ between them.
/// </summary>
public enum AzureProduct
{
    DevOps,
    KeyVault
    // Future products can be added here, e.g., Databricks, ResourceTracker
}
