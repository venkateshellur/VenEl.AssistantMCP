using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using VenEl.AssistantMCP.Atlassian.Configuration;

using VenEl.AssistantMCP.Core.Security;

namespace VenEl.AssistantMCP.Atlassian.Services.Auth;

/// <summary>
/// Authenticates using Atlassian OAuth 2.0 Client Credentials flow.
/// Fetches a Bearer token from auth.atlassian.com and caches it until expiry.
/// Returns null when credentials are not configured, allowing fallback to API Token.
/// </summary>
public sealed class OAuthAuthProvider(
    IOptions<AtlassianOptions> options,
    IHttpClientFactory httpClientFactory,
    SecretManager secretManager) : IAtlassianAuthProvider
{
    private const string TokenEndpoint = "https://auth.atlassian.com/oauth/token";

    private readonly OAuthOptions _opts = options.Value.OAuth;
    private string? _cachedToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<AuthenticationHeaderValue?> GetAuthHeaderAsync(CancellationToken cancellationToken = default)
    {
        if (!_opts.IsConfigured)
            return null;

        // Return cached token if still valid (with 60-second buffer).
        if (_cachedToken is not null && DateTimeOffset.UtcNow < _tokenExpiry)
            return new AuthenticationHeaderValue("Bearer", _cachedToken);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock.
            if (_cachedToken is not null && DateTimeOffset.UtcNow < _tokenExpiry)
                return new AuthenticationHeaderValue("Bearer", _cachedToken);

            await RefreshTokenAsync(cancellationToken);
            return new AuthenticationHeaderValue("Bearer", _cachedToken!);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task RefreshTokenAsync(CancellationToken cancellationToken)
    {
        using var http = httpClientFactory.CreateClient();
        
        var clientSecret = await secretManager.ResolveSecretAsync(_opts.ClientSecret, cancellationToken);

        var body = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type",    "client_credentials"),
            new KeyValuePair<string, string>("client_id",     _opts.ClientId),
            new KeyValuePair<string, string>("client_secret", clientSecret ?? string.Empty),
            new KeyValuePair<string, string>("audience",      "api.atlassian.com"),
        ]);

        var response = await http.PostAsync(TokenEndpoint, body, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        _cachedToken = root.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("OAuth token response missing access_token.");

        var expiresIn = root.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 3600;
        _tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 60);
    }
}
