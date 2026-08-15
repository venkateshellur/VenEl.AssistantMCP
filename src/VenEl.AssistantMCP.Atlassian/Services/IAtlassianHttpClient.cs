namespace VenEl.AssistantMCP.Atlassian.Services;

/// <summary>
/// Thin HTTP client for Atlassian Cloud APIs.
/// Routes requests to the correct base URL per product and injects the resolved auth header.
/// All methods return raw JSON strings; HTTP or config errors are returned as [ERROR] strings.
/// </summary>
public interface IAtlassianHttpClient
{
    /// <summary>Sends a GET request and returns the JSON response body.</summary>
    Task<string> GetAsync(AtlassianProduct product, string path, CancellationToken cancellationToken = default);

    /// <summary>Sends a POST request with a JSON body and returns the JSON response body.</summary>
    Task<string> PostAsync(AtlassianProduct product, string path, object body, CancellationToken cancellationToken = default);

    /// <summary>Sends a PUT request with a JSON body and returns the JSON response body.</summary>
    Task<string> PutAsync(AtlassianProduct product, string path, object body, CancellationToken cancellationToken = default);

    /// <summary>Sends a DELETE request and returns the JSON response body (or empty string on success).</summary>
    Task<string> DeleteAsync(AtlassianProduct product, string path, CancellationToken cancellationToken = default);

    /// <summary>Sends a POST request with multipart/form-data content, useful for attachments.</summary>
    Task<string> PostMultipartAsync(AtlassianProduct product, string path, System.Net.Http.MultipartFormDataContent content, CancellationToken cancellationToken = default);
}
