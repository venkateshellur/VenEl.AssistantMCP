using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using VenEl.MCPAssistant.Atlassian.Configuration;
using VenEl.MCPAssistant.Atlassian.Services;

namespace VenEl.MCPAssistant.Atlassian.Services.Auth;

/// <summary>
/// Authenticates using Atlassian API Token (HTTP Basic Auth).
/// Session credentials (supplied via atlassian_configure) take precedence over appsettings.json.
/// Returns null when no credentials are available, allowing fallback to OAuth.
/// </summary>
public sealed class ApiTokenAuthProvider(
    IOptions<AtlassianOptions> options,
    AtlassianSessionCredentials session) : IAtlassianAuthProvider
{
    public Task<AuthenticationHeaderValue?> GetAuthHeaderAsync(CancellationToken cancellationToken = default)
    {
        // Session credentials (conversation-supplied) take precedence.
        var email = !string.IsNullOrWhiteSpace(session.Email)    ? session.Email    : options.Value.ApiToken.Email;
        var token = !string.IsNullOrWhiteSpace(session.ApiToken) ? session.ApiToken : options.Value.ApiToken.Token;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            return Task.FromResult<AuthenticationHeaderValue?>(null);

        var encoded = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{email}:{token}"));

        return Task.FromResult<AuthenticationHeaderValue?>(
            new AuthenticationHeaderValue("Basic", encoded));
    }
}
