using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace VenEl.AssistantMCP.Core.Security;

public class SecretManager
{
    private readonly IEnumerable<ISecretResolver> _resolvers;
    private readonly ILogger<SecretManager> _logger;

    public SecretManager(IEnumerable<ISecretResolver> resolvers, ILogger<SecretManager> logger)
    {
        _resolvers = resolvers;
        _logger = logger;
    }

    public async Task<string?> ResolveSecretAsync(string? configuredValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(configuredValue))
        {
            return configuredValue;
        }

        foreach (var resolver in _resolvers)
        {
            try
            {
                var (handled, resolvedValue) = await resolver.TryResolveAsync(configuredValue, cancellationToken);
                if (handled)
                {
                    if (string.IsNullOrEmpty(resolvedValue))
                    {
                        _logger.LogWarning("Secret resolver '{ResolverType}' handled the identifier '{Identifier}' but returned a null or empty value. Falling back to configured value.", resolver.GetType().Name, configuredValue);
                        return configuredValue;
                    }
                    return resolvedValue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving secret using '{ResolverType}' for identifier '{Identifier}'. Falling back to configured value.", resolver.GetType().Name, configuredValue);
                // On failure, fall through and use the original raw string as a fallback.
                return configuredValue;
            }
        }

        // If no resolver handled it, or they all failed, return the raw value
        return configuredValue;
    }
}
