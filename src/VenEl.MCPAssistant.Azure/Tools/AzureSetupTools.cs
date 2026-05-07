using System.ComponentModel;
using ModelContextProtocol.Server;
using VenEl.MCPAssistant.Azure.Services;

namespace VenEl.MCPAssistant.Azure.Tools;

/// <summary>
/// MCP tool for configuring Azure credentials at runtime via the conversation window.
/// Use this when credentials are not present in appsettings.json.
/// </summary>
[McpServerToolType]
public sealed class AzureSetupTools(AzureSessionCredentials session)
{
    [McpServerTool(Name = "azure_configure")]
    [Description(
        "Configures Azure DevOps credentials for this session when they are not set in appsettings.json. " +
        "Call this tool with credentials provided by the user in the conversation. " +
        "Credentials are held in memory for the lifetime of the server process.")]
    public string AzureConfigure(
        [Description("Your Azure DevOps organization URL, e.g., 'https://dev.azure.com/your-org'.")] string organizationUrl,
        [Description("Your Azure Personal Access Token (PAT).")] string patToken)
    {
        session.OrganizationUrl = organizationUrl.Trim().TrimEnd('/');
        session.PatToken = patToken.Trim();

        return $"✅ Azure session credentials configured for organization '{session.OrganizationUrl}'. " +
               "You can now use azure_* tools.";
    }

    [McpServerTool(Name = "azure_show_config")]
    [Description(
        "Shows the current Azure configuration status — which credentials are set " +
        "(from appsettings.json or session) without revealing sensitive values.")]
    public string AzureShowConfig()
    {
        var orgSource = !string.IsNullOrWhiteSpace(session.OrganizationUrl) ? $"'{session.OrganizationUrl}' (session)" : "not set in session";
        var patSource = !string.IsNullOrWhiteSpace(session.PatToken) ? "set (session)" : "not set in session";

        return $"""
                Azure Configuration Status
                ══════════════════════════
                Organization URL: {orgSource}
                PAT Token       : {patSource}

                Note: appsettings.json values are used as fallback when session values are not set.
                To set session credentials, call 'azure_configure'.
                """;
    }
}
