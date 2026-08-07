using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace VenEl.AssistantMCP.Core.Http;

public sealed class CachingDelegatingHandler : DelegatingHandler
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachingDelegatingHandler> _logger;

    public CachingDelegatingHandler(IMemoryCache cache, ILogger<CachingDelegatingHandler> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method != HttpMethod.Get)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        // Generate a cache key that includes the full URI and Authorization header
        // This ensures different APIs or different users don't pollute each other's cache
        var authHeader = request.Headers.Authorization?.ToString() ?? "Anonymous";
        var cacheKey = $"MCP_HTTP_{request.RequestUri?.AbsoluteUri}_{authHeader}";

        if (_cache.TryGetValue(cacheKey, out CachedHttpResponse? cachedResponse) && cachedResponse != null)
        {
            _logger.LogDebug("Cache HIT for {Url}", request.RequestUri);
            return cachedResponse.ToResponseMessage(request);
        }

        _logger.LogDebug("Cache MISS for {Url}", request.RequestUri);
        
        var response = await base.SendAsync(request, cancellationToken);

        // Only cache successful requests
        if (response.IsSuccessStatusCode && response.Content != null)
        {
            var contentBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            
            var entry = new CachedHttpResponse
            {
                StatusCode = response.StatusCode,
                ContentBytes = contentBytes,
                ContentType = response.Content.Headers.ContentType?.ToString()
            };

            _cache.Set(cacheKey, entry, TimeSpan.FromSeconds(15));
            
            // Reconstruct the response since we consumed the stream
            response = entry.ToResponseMessage(request);
        }

        return response;
    }

    private sealed class CachedHttpResponse
    {
        public System.Net.HttpStatusCode StatusCode { get; set; }
        public byte[] ContentBytes { get; set; } = Array.Empty<byte>();
        public string? ContentType { get; set; }

        public HttpResponseMessage ToResponseMessage(HttpRequestMessage request)
        {
            var msg = new HttpResponseMessage(StatusCode);
            msg.RequestMessage = request;
            msg.Content = new ByteArrayContent(ContentBytes);
            if (!string.IsNullOrEmpty(ContentType))
            {
                msg.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(ContentType);
            }
            return msg;
        }
    }
}
