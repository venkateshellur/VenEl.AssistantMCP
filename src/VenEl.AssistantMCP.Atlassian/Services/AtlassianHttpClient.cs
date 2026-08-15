using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VenEl.AssistantMCP.Atlassian.Configuration;
using VenEl.AssistantMCP.Atlassian.Services.Auth;

namespace VenEl.AssistantMCP.Atlassian.Services;

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
    OAuthAuthProvider oauthProvider,
    ILogger<AtlassianHttpClient> logger) : IAtlassianHttpClient
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };
    private static readonly ConcurrentDictionary<string, string> _jiraApiVersions = new(StringComparer.OrdinalIgnoreCase);
    private static readonly SemaphoreSlim _jiraApiVersionLock = new(1, 1);
    private readonly AtlassianOptions _opts = options.Value;

    // Resolves domain — session (conversation-supplied) takes precedence over config.
    private string Domain =>
        !string.IsNullOrWhiteSpace(session.Domain) ? session.Domain : _opts.Domain;

    // ── Public API ───────────────────────────────────────────────────────────

    public Task<string> GetAsync(AtlassianProduct product, string path, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Get, product, path, content: null, cancellationToken);

    public Task<string> PostAsync(AtlassianProduct product, string path, object body, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Post, product, path, JsonContent.Create(body), cancellationToken);

    public Task<string> PutAsync(AtlassianProduct product, string path, object body, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Put, product, path, JsonContent.Create(body), cancellationToken);

    public Task<string> DeleteAsync(AtlassianProduct product, string path, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Delete, product, path, content: null, cancellationToken);

    public async Task<string> PostMultipartAsync(AtlassianProduct product, string path, System.Net.Http.MultipartFormDataContent content, CancellationToken cancellationToken = default)
    {
        // For Jira attachments, we must pass X-Atlassian-Token: no-check
        var auth = await ResolveAuthAsync(cancellationToken);
        if (auth is null) return "[CONFIG ERROR] No Atlassian credentials found.";
        
        var url = await BuildUrlAsync(product, path, auth, cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = auth;
        request.Headers.Add("X-Atlassian-Token", "no-check");
        request.Content = content;
        
        try
        {
            var response = await httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                return $"[HTTP {(int)response.StatusCode} {response.ReasonPhrase}] {responseContent}";
            return PrettyPrint(responseContent);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return $"[ERROR] {ex.Message}";
        }
    }

    // ── Core send logic ──────────────────────────────────────────────────────

    private async Task<string> SendAsync(
        HttpMethod method,
        AtlassianProduct product,
        string path,
        HttpContent? content,
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

        var url = await BuildUrlAsync(product, path, auth, cancellationToken);
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = auth;
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (content is not null)
            request.Content = content;

        try
        {
            var response = await httpClient.SendAsync(request, cancellationToken);
            var responseContent  = await response.Content.ReadAsStringAsync(cancellationToken);

            // ── Auto-healing logic for 410 Gone ──────────────────────────────
            if (response.StatusCode == System.Net.HttpStatusCode.Gone)
            {
                var match = Regex.Match(responseContent, @"migrate to the (/[^\s]+) API");
                if (match.Success)
                {
                    var newPath = match.Groups[1].Value;
                    var oldUri = new Uri(url);
                    var newUrl = $"https://{oldUri.Host}{newPath}{oldUri.Query}";
                    
                    logger.LogWarning("Atlassian API deprecated endpoint used. Auto-healing by migrating from {OldUrl} to {NewUrl}", url, newUrl);

                    using var retryRequest = new HttpRequestMessage(method, newUrl);
                    retryRequest.Headers.Authorization = auth;
                    retryRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    
                    if (content is not null)
                    {
                        // Note: If content is already consumed, this might fail, but for JsonContent it's usually fine
                        retryRequest.Content = content;
                    }

                    var retryResponse = await httpClient.SendAsync(retryRequest, cancellationToken);
                    var retryContent = await retryResponse.Content.ReadAsStringAsync(cancellationToken);

                    if (retryResponse.IsSuccessStatusCode)
                        return PrettyPrint(retryContent);

                    return $"[HTTP {(int)retryResponse.StatusCode} {retryResponse.ReasonPhrase}] {retryContent}";
                }
            }

            if (!response.IsSuccessStatusCode)
                return $"[HTTP {(int)response.StatusCode} {response.ReasonPhrase}] {responseContent}";

            return PrettyPrint(responseContent);
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

    private async Task<string> BuildUrlAsync(AtlassianProduct product, string path, AuthenticationHeaderValue auth, CancellationToken ct) =>
        product switch
        {
            AtlassianProduct.Jira       => $"https://{Domain}/rest/api/{await DiscoverJiraApiVersionAsync(auth, ct)}/{path.TrimStart('/')}",
            AtlassianProduct.JiraAgile  => $"https://{Domain}/rest/agile/1.0/{path.TrimStart('/')}",
            AtlassianProduct.Confluence => $"https://{Domain}/wiki/rest/api/{path.TrimStart('/')}",
            AtlassianProduct.Bitbucket  => $"https://api.bitbucket.org/2.0/{path.TrimStart('/')}",
            _                           => throw new ArgumentOutOfRangeException(nameof(product)),
        };

    private async Task<string> DiscoverJiraApiVersionAsync(AuthenticationHeaderValue auth, CancellationToken ct)
    {
        var domain = Domain;
        if (_jiraApiVersions.TryGetValue(domain, out var version))
            return version;

        await _jiraApiVersionLock.WaitAsync(ct);
        try
        {
            if (_jiraApiVersions.TryGetValue(domain, out version))
                return version;

            // Try v3
            using var req3 = new HttpRequestMessage(HttpMethod.Get, $"https://{domain}/rest/api/3/serverInfo");
            req3.Headers.Authorization = auth;
            var res3 = await httpClient.SendAsync(req3, ct);
            if (res3.IsSuccessStatusCode)
                return _jiraApiVersions.GetOrAdd(domain, "3");

            // Try v2
            using var req2 = new HttpRequestMessage(HttpMethod.Get, $"https://{domain}/rest/api/2/serverInfo");
            req2.Headers.Authorization = auth;
            var res2 = await httpClient.SendAsync(req2, ct);
            if (res2.IsSuccessStatusCode)
                return _jiraApiVersions.GetOrAdd(domain, "2");

            // Fallback
            return _jiraApiVersions.GetOrAdd(domain, "latest");
        }
        finally
        {
            _jiraApiVersionLock.Release();
        }
    }

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
