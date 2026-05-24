namespace VenEl.AssistantMCP.Azure.Services;

/// <summary>
/// A centralized client for calling Azure APIs.
/// Automatically handles auth resolution and URL routing.
/// </summary>
public interface IAzureHttpClient
{
    Task<string> GetAsync(AzureProduct product, string path, string apiVersion = "7.1", CancellationToken cancellationToken = default);
    
    Task<string> PostAsync(AzureProduct product, string path, object body, string apiVersion = "7.1", CancellationToken cancellationToken = default);
    
    Task<string> PutAsync(AzureProduct product, string path, object body, string apiVersion = "7.1", CancellationToken cancellationToken = default);
}
