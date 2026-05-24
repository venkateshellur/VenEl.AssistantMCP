using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VenEl.AssistantMCP.Bitwarden.Extensions;

namespace VenEl.AssistantMCP.Bitwarden.Services;

public class BitwardenStrategyService : IBitwardenService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly BitwardenOptions _options;
    private readonly ILogger<BitwardenStrategyService> _logger;

    public BitwardenStrategyService(IServiceProvider serviceProvider, IOptions<BitwardenOptions> options, ILogger<BitwardenStrategyService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    public Task<string> GetSecretAsync(string identifier, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_options.MachineToken))
        {
            _logger.LogInformation("MachineToken found. Utilizing Bitwarden SDK Service.");
            var sdkService = _serviceProvider.GetRequiredService<BitwardenSdkService>();
            return sdkService.GetSecretAsync(identifier, cancellationToken);
        }

        _logger.LogInformation("No MachineToken configured. Falling back to Bitwarden CLI Service.");
        var cliService = _serviceProvider.GetRequiredService<BitwardenCliService>();
        return cliService.GetSecretAsync(identifier, cancellationToken);
    }
}
