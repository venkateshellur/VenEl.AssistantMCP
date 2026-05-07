using System.Net.Http.Headers;

namespace VenEl.MCPAssistant.Atlassian.Services.Auth;

/// <summary>
/// Abstraction over Atlassian auth methods.
/// Returns null when the implementation's credentials are not configured,
/// allowing the caller to fall back to another provider.
/// </summary>
public interface IAtlassianAuthProvider
{
    /// <summary>
    /// Returns the Authorization header value, or null if credentials are not configured.
    /// </summary>
    Task<AuthenticationHeaderValue?> GetAuthHeaderAsync(CancellationToken cancellationToken = default);
}
