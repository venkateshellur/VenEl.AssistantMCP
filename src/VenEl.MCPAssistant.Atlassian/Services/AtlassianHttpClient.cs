using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using VenEl.MCPAssistant.Atlassian.Configuration;
using VenEl.MCPAssistant.Atlassian.Services.Auth;

namespace VenEl.MCPAssistant.Atlassian.Services;

/// <summary>
/// Implementation of <see cref="IAtlassianHttpClient"/>.
/// Resolves auth using preferred method with automatic fallback,
/// routes requests to the correct Atlassian product base URL,
/// and returns pretty-printed JSON (or a descriptive error string).
/// </summary>
public sealed class AtlassianHttpClient(
    HttpClient httpClient,
    IOptions<AtlassianOptions> options,
    AtlassianSessionCredentials session,
    ApiTokenAuthProvider apiTokenProvider,
    OAuthAuthProvider oauthProvider) : IAtlassianHttpClient
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };
    private readonly AtlassianOptions _opts = options.Value;

    // Resolves domain — session (conversation-supplied) takes precedence over config.
    private string Domain =>
        !string.IsNullOrWhiteSpace(session.Domain) ? session.Domain : _opts.Domain;

    // ── Public API ───────────────────────────────────────────────────────────

    public Task<string> GetAsync(AtlassianProduct product, string path, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Get, product, path, body: null, cancellationToken);

    public Task<string> PostAsync(AtlassianProduct product, string path, object body, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Post, product, path, body, cancellationToken);

    public Task<string> PutAsync(AtlassianProduct product, string path, object body, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Put, product, path, body, cancellationToken);

    // ── Core send logic ──────────────────────────────────────────────────────

    private async Task<string> SendAsync(
        HttpMethod method,
        AtlassianProduct product,
        string path,
        object? body,
        CancellationToken cancellationToken)
    {
        // Resolve auth (preferred → fallback).
        AuthenticationHeaderValue? auth;
        try
        {
            auth = await ResolveAuthAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return $"[AUTH ERROR] Failed to obtain credentials: {ex.Message}";
        }

        if (auth is null)
            return "[CONFIG ERROR] No Atlassian credentials found in configuration or session. " +
                   "Please call 'atlassian_configure' with your domain, email, and API token, " +
                   "or add them to appsettings.json under 'Atlassian'.";

        using var request = new HttpRequestMessage(method, BuildUrl(product, path));
        request.Headers.Authorization = auth;
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (body is not null)
            request.Content = JsonContent.Create(body);

        try
        {
            var response = await httpClient.SendAsync(request, cancellationToken);
            var content  = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return $"[HTTP {(int)response.StatusCode} {response.ReasonPhrase}] {content}";

            return PrettyPrint(content);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return $"[ERROR] {ex.Message}";
        }
    }

    // ── Auth resolution ──────────────────────────────────────────────────────

    private async Task<AuthenticationHeaderValue?> ResolveAuthAsync(CancellationToken ct)
    {
        var preferOAuth = _opts.PreferredAuthMethod
            .Equals("OAuth", StringComparison.OrdinalIgnoreCase);

        IAtlassianAuthProvider primary  = preferOAuth ? oauthProvider    : apiTokenProvider;
        IAtlassianAuthProvider fallback = preferOAuth ? apiTokenProvider : oauthProvider;

        return await primary.GetAuthHeaderAsync(ct)
            ?? await fallback.GetAuthHeaderAsync(ct);
    }

    // ── URL routing ──────────────────────────────────────────────────────────

    private string BuildUrl(AtlassianProduct product, string path) =>
        product switch
        {
            AtlassianProduct.Jira       => $"https://{Domain}/rest/api/3/{path.TrimStart('/')}",
            AtlassianProduct.JiraAgile  => $"https://{Domain}/rest/agile/1.0/{path.TrimStart('/')}",
            AtlassianProduct.Confluence => $"https://{Domain}/wiki/rest/api/{path.TrimStart('/')}",
            AtlassianProduct.Bitbucket  => $"https://api.bitbucket.org/2.0/{path.TrimStart('/')}",
            _                           => throw new ArgumentOutOfRangeException(nameof(product)),
        };

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string PrettyPrint(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc.RootElement, JsonOpts);
        }
        catch
        {
            return json; // Return as-is if not valid JSON.
        }
    }
}
