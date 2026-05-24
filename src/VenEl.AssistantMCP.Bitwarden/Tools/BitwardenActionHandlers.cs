using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenEl.AssistantMCP.Bitwarden.Services;
using VenEl.AssistantMCP.Core.Dispatcher;

namespace VenEl.AssistantMCP.Bitwarden.Tools;

public class BitwardenActionHandlers : IActionHandler<BitwardenCommandArgs>
{
    private readonly IBitwardenService _bitwardenService;
    private readonly ILogger<BitwardenActionHandlers> _logger;

    public BitwardenActionHandlers(IBitwardenService bitwardenService, ILogger<BitwardenActionHandlers> logger)
    {
        _bitwardenService = bitwardenService;
        _logger = logger;
    }

    public string ActionName => "bitwarden_get_secret";

    public string? Validate(BitwardenCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Identifier))
        {
            return "Secret identifier must be provided.";
        }
        return null;
    }

    public async Task<string> HandleAsync(BitwardenCommandArgs args, CancellationToken ct)
    {
        return await HandleGetSecretAsync(args, ct);
    }

    private async Task<string> HandleGetSecretAsync(BitwardenCommandArgs args, CancellationToken ct)
    {

        try
        {
            var secretValue = await _bitwardenService.GetSecretAsync(args.Identifier!, ct);
            return JsonSerializer.Serialize(new {
                Success = true,
                Message = "Secret retrieved successfully.",
                Value = secretValue
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving secret");
            return JsonSerializer.Serialize(new {
                Success = false,
                Error = ex.Message
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
