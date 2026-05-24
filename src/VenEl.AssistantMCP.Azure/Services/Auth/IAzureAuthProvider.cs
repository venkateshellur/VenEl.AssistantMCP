using System.Net.Http.Headers;

namespace VenEl.AssistantMCP.Azure.Services.Auth;

/// <summary>
/// Abstract authentication provider for Azure.
/// Allows swapping between PAT and OAuth transparently.
/// </summary>
public interface IAzureAuthProvider
{
    /// <summary>
    /// Returns a valid Authorization header, or null if credentials are missing/invalid.
    /// </summary>
    Task<AuthenticationHeaderValue?> GetAuthHeaderAsync(CancellationToken cancellationToken);
}
