using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using VenEl.AssistantMCP.Databricks.Configuration;

namespace VenEl.AssistantMCP.Databricks.Services;

public class DatabricksHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly DatabricksOptions _options;

    public DatabricksHttpClient(HttpClient httpClient, IOptions<DatabricksOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.WorkspaceUrl) || string.IsNullOrWhiteSpace(_options.PersonalAccessToken))
        {
            throw new InvalidOperationException("Databricks WorkspaceUrl and PersonalAccessToken must be configured.");
        }

        var baseUrl = _options.WorkspaceUrl.TrimEnd('/');
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.PersonalAccessToken);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<string> GetAsync(string endpoint, CancellationToken ct)
    {
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
