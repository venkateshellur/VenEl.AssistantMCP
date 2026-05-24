using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenEl.MCPAssistant.Core.Security;

namespace VenEl.MCPAssistant.Bitwarden.Services;

public class BitwardenSecretResolver : ISecretResolver
{
    private const string Prefix = "bw://";
    private readonly IBitwardenService _bitwardenService;
    private readonly ILogger<BitwardenSecretResolver> _logger;

    public BitwardenSecretResolver(IBitwardenService bitwardenService, ILogger<BitwardenSecretResolver> logger)
    {
        _bitwardenService = bitwardenService;
        _logger = logger;
    }

    public async Task<(bool Handled, string? Value)> TryResolveAsync(string identifier, CancellationToken cancellationToken = default)
    {
        if (identifier.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            var secretId = identifier.Substring(Prefix.Length);
            _logger.LogInformation("Resolving Bitwarden secret: {SecretId}", secretId);
            
            try
            {
                var secret = await _bitwardenService.GetSecretAsync(secretId, cancellationToken);
                return (true, secret);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve Bitwarden secret '{SecretId}'", secretId);
                return (true, null); // Handled but failed
            }
        }

        return (false, null);
    }
}
