using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VenEl.MCP.ServiceNow.Configuration;

namespace VenEl.MCP.ServiceNow.Client;

public class ResilientServiceNowClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ResilientServiceNowClient> _logger;
    private readonly ServiceNowOptions _options;
    
    // In-memory cache of working API versions (e.g. "table" -> "v2")
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _versionCache = new();

    public ResilientServiceNowClient(HttpClient httpClient, IOptions<ServiceNowOptions> options, ILogger<ResilientServiceNowClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;

        if (!string.IsNullOrEmpty(_options.InstanceUrl))
        {
            _httpClient.BaseAddress = new Uri(_options.InstanceUrl.TrimEnd('/'));
        }

        if (_options.AuthMode?.ToLowerInvariant() == "basic" && 
            !string.IsNullOrEmpty(_options.Username) && 
            !string.IsNullOrEmpty(_options.Password))
        {
            var authBytes = Encoding.ASCII.GetBytes($"{_options.Username}:{_options.Password}");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
        }
        
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// Executes a GET request with smart version fallback.
    /// </summary>
    public async Task<string> GetAsync(string apiCategory, string endpointPath, CancellationToken ct)
    {
        string[] versionsToTry = { _options.DefaultApiVersion, "v1", "" };
        
        // If we previously discovered a working version, try it first
        if (_versionCache.TryGetValue(apiCategory, out var cachedVersion))
        {
            versionsToTry = new[] { cachedVersion, _options.DefaultApiVersion, "v1", "" };
        }

        foreach (var version in versionsToTry)
        {
            string versionSegment = string.IsNullOrEmpty(version) ? "" : $"{version}/";
            string fullPath = $"/api/now/{versionSegment}{apiCategory}/{endpointPath}";

            try
            {
                var response = await _httpClient.GetAsync(fullPath, ct);
                
                if (response.IsSuccessStatusCode)
                {
                    _versionCache[apiCategory] = version;
                    return await response.Content.ReadAsStringAsync(ct);
                }
                
                // If 404 or 400 with API version error, we fallback and try the next version
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound || 
                    response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    _logger.LogWarning($"ServiceNow API version {version} failed for {apiCategory}. Trying fallback...");
                    continue;
                }
                
                // Other errors (e.g., 401 Unauthorized, 500 Internal Error) should fail immediately
                string errorContent = await response.Content.ReadAsStringAsync(ct);
                throw new Exception($"ServiceNow API Error ({response.StatusCode}): {errorContent}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"Network error while calling ServiceNow API version {version}.");
            }
        }
        
        throw new Exception($"All API version fallbacks failed for {apiCategory}/{endpointPath}.");
    }
}
