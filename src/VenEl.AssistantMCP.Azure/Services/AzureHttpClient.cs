using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using VenEl.AssistantMCP.Azure.Configuration;
using VenEl.AssistantMCP.Azure.Services.Auth;

namespace VenEl.AssistantMCP.Azure.Services;

/// <summary>
/// Implementation of <see cref="IAzureHttpClient"/>.
/// Resolves auth using preferred method with automatic fallback,
/// routes requests to the correct Azure product base URL,
/// and returns pretty-printed JSON.
/// </summary>
public sealed class AzureHttpClient(
    HttpClient httpClient,
    IOptions<AzureOptions> options,
    AzureSessionCredentials session,
    PatAuthProvider patProvider) : IAzureHttpClient
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };
    private readonly AzureOptions _opts = options.Value;

    private string OrganizationUrl =>
        !string.IsNullOrWhiteSpace(session.OrganizationUrl) ? session.OrganizationUrl : _opts.OrganizationUrl;

    public Task<string> GetAsync(AzureProduct product, string path, string apiVersion = "7.1", CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Get, product, path, body: null, apiVersion, cancellationToken);

    public Task<string> PostAsync(AzureProduct product, string path, object body, string apiVersion = "7.1", CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Post, product, path, body, apiVersion, cancellationToken);

    public Task<string> PutAsync(AzureProduct product, string path, object body, string apiVersion = "7.1", CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Put, product, path, body, apiVersion, cancellationToken);

    private async Task<string> SendAsync(
        HttpMethod method,
        AzureProduct product,
        string path,
        object? body,
        string apiVersion,
        CancellationToken cancellationToken)
    {
        AuthenticationHeaderValue? auth;
        try
        {
            // For now, only PAT is implemented. We can add OAuth later.
            auth = await patProvider.GetAuthHeaderAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return $"[AUTH ERROR] Failed to obtain credentials: {ex.Message}";
        }

        if (auth is null)
            return "[CONFIG ERROR] No Azure credentials found in configuration or session. " +
                   "Please call 'azure_configure' with your organization URL and PAT, " +
                   "or add them to appsettings.json under 'Azure'.";

        if (string.IsNullOrWhiteSpace(OrganizationUrl) && product == AzureProduct.DevOps)
            return "[CONFIG ERROR] Azure Organization URL is not configured.";

        var url = BuildUrl(product, path, apiVersion);
        using var request = new HttpRequestMessage(method, url);
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

    private string BuildUrl(AzureProduct product, string path, string apiVersion) =>
        product switch
        {
            AzureProduct.DevOps => $"{OrganizationUrl.TrimEnd('/')}/{path.TrimStart('/')}?api-version={apiVersion}",
            _ => throw new ArgumentOutOfRangeException(nameof(product)),
        };

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
