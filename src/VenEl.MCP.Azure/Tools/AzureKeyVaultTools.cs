using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenEl.MCP.Azure.Services;
using VenEl.MCP.Core.Dispatcher;

namespace VenEl.MCP.Azure.Tools;

public sealed class AzureGetSecretActionHandler(IAzureHttpClient client, ILogger<AzureGetSecretActionHandler> logger) : IActionHandler<AzureCommandArgs>
{
    public string ActionName => "azure_get_secret";

    public string? Validate(AzureCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.VaultName)) return "Missing required parameter 'VaultName'.";
        if (string.IsNullOrWhiteSpace(args.SecretName)) return "Missing required parameter 'SecretName'.";
        return null;
    }

    public async Task<string> HandleAsync(AzureCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Getting secret {SecretName} from Key Vault {VaultName}", args.SecretName, args.VaultName);
        return await client.GetAsync(AzureProduct.KeyVault, $"secrets/{args.SecretName}", "7.4", args.VaultName, ct);
    }
}

public sealed class AzureListSecretsActionHandler(IAzureHttpClient client, ILogger<AzureListSecretsActionHandler> logger) : IActionHandler<AzureCommandArgs>
{
    public string ActionName => "azure_list_secrets";

    public string? Validate(AzureCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.VaultName)) return "Missing required parameter 'VaultName'.";
        return null;
    }

    public async Task<string> HandleAsync(AzureCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Listing secrets in Key Vault {VaultName}", args.VaultName);
        return await client.GetAsync(AzureProduct.KeyVault, "secrets", "7.4", args.VaultName, ct);
    }
}

public sealed class AzureGetCertificateActionHandler(IAzureHttpClient client, ILogger<AzureGetCertificateActionHandler> logger) : IActionHandler<AzureCommandArgs>
{
    public string ActionName => "azure_get_certificate";

    public string? Validate(AzureCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.VaultName)) return "Missing required parameter 'VaultName'.";
        if (string.IsNullOrWhiteSpace(args.CertificateName)) return "Missing required parameter 'CertificateName'.";
        return null;
    }

    public async Task<string> HandleAsync(AzureCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Getting certificate {CertificateName} from Key Vault {VaultName}", args.CertificateName, args.VaultName);
        return await client.GetAsync(AzureProduct.KeyVault, $"certificates/{args.CertificateName}", "7.4", args.VaultName, ct);
    }
}
