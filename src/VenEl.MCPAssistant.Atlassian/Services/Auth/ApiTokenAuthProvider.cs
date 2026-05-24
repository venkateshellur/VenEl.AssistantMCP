using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using VenEl.MCPAssistant.Atlassian.Configuration;
using VenEl.MCPAssistant.Atlassian.Services;

using VenEl.MCPAssistant.Core.Security;

namespace VenEl.MCPAssistant.Atlassian.Services.Auth;

/// <summary>
/// Authenticates using Atlassian API Token (HTTP Basic Auth).
/// Session credentials (supplied via atlassian_configure) take precedence over appsettings.json.
/// Returns null when no credentials are available, allowing fallback to OAuth.
/// </summary>
public sealed class ApiTokenAuthProvider(
    IOptions<AtlassianOptions> options,
    AtlassianSessionCredentials session,
    SecretManager secretManager) : IAtlassianAuthProvider
{
    public async Task<AuthenticationHeaderValue?> GetAuthHeaderAsync(CancellationToken cancellationToken = default)
    {
        // Session credentials (conversation-supplied) take precedence.
        var email = !string.IsNullOrWhiteSpace(session.Email)    ? session.Email    : options.Value.ApiToken.Email;
        var rawToken = !string.IsNullOrWhiteSpace(session.ApiToken) ? session.ApiToken : options.Value.ApiToken.Token;
        
        var token = await secretManager.ResolveSecretAsync(rawToken, cancellationToken);

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            return null;

        var encoded = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{email}:{token}"));

        return new AuthenticationHeaderValue("Basic", encoded);
    }
}
