using System;
using System.Threading;
using System.Threading.Tasks;
using Bitwarden.Sdk;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VenEl.MCPAssistant.Bitwarden.Extensions;

namespace VenEl.MCPAssistant.Bitwarden.Services;

public class BitwardenSdkService : IBitwardenService
{
    private readonly BitwardenOptions _options;
    private readonly ILogger<BitwardenSdkService> _logger;

    public BitwardenSdkService(IOptions<BitwardenOptions> options, ILogger<BitwardenSdkService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<string> GetSecretAsync(string identifier, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.MachineToken))
        {
            throw new InvalidOperationException("MachineToken is not configured for Bitwarden Secrets Manager SDK.");
        }

        try
        {
            _logger.LogInformation("Retrieving secret via Bitwarden SDK...");
            var settings = new BitwardenSettings();
            using var client = new BitwardenClient(settings);
            client.Auth.LoginAccessToken(_options.MachineToken, "");
            
            if (Guid.TryParse(identifier, out var secretId))
            {
                var secretResponse = client.Secrets.Get(secretId);
                return Task.FromResult(secretResponse?.Value ?? string.Empty);
            }
            else
            {
                throw new ArgumentException("The Bitwarden Secrets SDK requires a valid Secret ID (Guid) for retrieval.", nameof(identifier));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve secret via SDK.");
            throw;
        }
    }
}
