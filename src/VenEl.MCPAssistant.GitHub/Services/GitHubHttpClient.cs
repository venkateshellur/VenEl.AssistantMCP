using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using VenEl.MCPAssistant.GitHub.Configuration;

using VenEl.MCPAssistant.Core.Security;

namespace VenEl.MCPAssistant.GitHub.Services;

public interface IGitHubHttpClient
{
    Task<string> GetAsync(string path, CancellationToken cancellationToken = default, string? acceptHeader = null);
    Task<string> PostAsync(string path, object body, CancellationToken cancellationToken = default, string? acceptHeader = null);
    Task<string> PutAsync(string path, object body, CancellationToken cancellationToken = default, string? acceptHeader = null);
}

public sealed class GitHubHttpClient(HttpClient httpClient, GitHubSession session, IOptions<GitHubOptions> options, SecretManager secretManager) : IGitHubHttpClient
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };
    private const string BaseUrl = "https://api.github.com";

    public Task<string> GetAsync(string path, CancellationToken cancellationToken = default, string? acceptHeader = null)
        => SendAsync(HttpMethod.Get, path, body: null, cancellationToken, acceptHeader);

    public Task<string> PostAsync(string path, object body, CancellationToken cancellationToken = default, string? acceptHeader = null)
        => SendAsync(HttpMethod.Post, path, body, cancellationToken, acceptHeader);

    public Task<string> PutAsync(string path, object body, CancellationToken cancellationToken = default, string? acceptHeader = null)
        => SendAsync(HttpMethod.Put, path, body, cancellationToken, acceptHeader);

    private async Task<string> SendAsync(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken,
        string? acceptHeader = null)
    {
        var token = session.PatToken 
            ?? Environment.GetEnvironmentVariable("GITHUB_PAT") 
            ?? await secretManager.ResolveSecretAsync(options.Value.PatToken, cancellationToken);

        if (string.IsNullOrWhiteSpace(token))
            return "[CONFIG ERROR] No GitHub PAT found in session, environment variables (GITHUB_PAT), or appsettings.json (GitHub:PatToken). Please call 'github_configure' with your PAT first.";

        var url = $"{BaseUrl}/{path.TrimStart('/')}";
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var accept = acceptHeader ?? "application/vnd.github.v3+json";
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("VenEl.MCPAssistant", "1.0"));

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

    private static string PrettyPrint(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc.RootElement, JsonOpts);
        }
        catch
        {
            return json;
        }
    }
}
