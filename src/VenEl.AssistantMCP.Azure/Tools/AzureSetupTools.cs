using System.Threading;
using System.Threading.Tasks;
using VenEl.AssistantMCP.Azure.Services;
using VenEl.AssistantMCP.Core.Dispatcher;

namespace VenEl.AssistantMCP.Azure.Tools;

public sealed class AzureConfigureActionHandler(AzureSessionCredentials session) : IActionHandler<AzureCommandArgs>
{
    public string ActionName => "azure_configure";

    public string? Validate(AzureCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.OrganizationUrl)) return "Missing required parameter 'OrganizationUrl'.";
        if (string.IsNullOrWhiteSpace(args.PatToken)) return "Missing required parameter 'PatToken'.";
        return null;
    }

    public Task<string> HandleAsync(AzureCommandArgs args, CancellationToken ct)
    {
        session.OrganizationUrl = args.OrganizationUrl!.Trim().TrimEnd('/');
        session.PatToken = args.PatToken!.Trim();

        return Task.FromResult($"✅ Azure session credentials configured for organization '{session.OrganizationUrl}'. You can now use azure commands.");
    }
}

public sealed class AzureShowConfigActionHandler(AzureSessionCredentials session) : IActionHandler<AzureCommandArgs>
{
    public string ActionName => "azure_show_config";

    public string? Validate(AzureCommandArgs args) => null;

    public Task<string> HandleAsync(AzureCommandArgs args, CancellationToken ct)
    {
        var orgSource = !string.IsNullOrWhiteSpace(session.OrganizationUrl) ? $"'{session.OrganizationUrl}' (session)" : "not set in session";
        var patSource = !string.IsNullOrWhiteSpace(session.PatToken) ? "set (session)" : "not set in session";

        return Task.FromResult($"""
                Azure Configuration Status
                ══════════════════════════
                Organization URL: {orgSource}
                PAT Token       : {patSource}

                Note: appsettings.json values are used as fallback when session values are not set.
                To set session credentials, call 'azure_configure'.
                """);
    }
}
