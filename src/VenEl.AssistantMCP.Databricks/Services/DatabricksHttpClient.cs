using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using VenEl.AssistantMCP.Databricks.Configuration;
using VenEl.AssistantMCP.Core.Security;

namespace VenEl.AssistantMCP.Databricks.Services;

public class DatabricksHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly DatabricksOptions _options;
    private readonly SecretManager _secretManager;

    public DatabricksHttpClient(HttpClient httpClient, IOptions<DatabricksOptions> options, SecretManager secretManager)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _secretManager = secretManager;

        if (string.IsNullOrWhiteSpace(_options.WorkspaceUrl) || string.IsNullOrWhiteSpace(_options.PersonalAccessToken))
        {
            throw new InvalidOperationException("Databricks WorkspaceUrl and PersonalAccessToken must be configured.");
        }

        var baseUrl = _options.WorkspaceUrl.TrimEnd('/');
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private async Task EnsureAuthorizedAsync(CancellationToken ct)
    {
        var token = await _secretManager.ResolveSecretAsync(_options.PersonalAccessToken, ct);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<string> GetAsync(string endpoint, CancellationToken ct)
    {
        await EnsureAuthorizedAsync(ct);
        var response = await _httpClient.GetAsync(endpoint, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Databricks API error ({response.StatusCode}): {error}");
        }
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<string> PostAsync(string endpoint, object? data, CancellationToken ct)
    {
        await EnsureAuthorizedAsync(ct);
        var content = data != null
            ? new StringContent(JsonSerializer.Serialize(data), System.Text.Encoding.UTF8, "application/json")
            : null;

        var response = await _httpClient.PostAsync(endpoint, content, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Databricks API error ({response.StatusCode}): {error}");
        }
        return await response.Content.ReadAsStringAsync(ct);
    }
}
