using System.ComponentModel;
using ModelContextProtocol.Server;
using VenEl.MCPAssistant.Atlassian.Services;

namespace VenEl.MCPAssistant.Atlassian.Tools;

/// <summary>
/// MCP tool for configuring Atlassian credentials at runtime via the conversation window.
/// Use this when credentials are not present in appsettings.json.
/// </summary>
[McpServerToolType]
public sealed class AtlassianSetupTools(AtlassianSessionCredentials session)
{
    [McpServerTool(Name = "atlassian_configure")]
    [Description(
        "Configures Atlassian credentials for this session when they are not set in appsettings.json. " +
        "Call this tool with credentials provided by the user in the conversation. " +
        "Credentials are held in memory for the lifetime of the server process. " +
        "Covers all products: Jira, Confluence, and Bitbucket.")]
    public string AtlassianConfigure(
        [Description("Your Atlassian Cloud domain without https://, e.g. 'yourname.atlassian.net'.")] string domain,
        [Description("Your Atlassian account email address.")] string email,
        [Description("Your Atlassian API token (generate at https://id.atlassian.net/manage-profile/security/api-tokens).")] string apiToken,
        [Description("Your Bitbucket workspace slug — the short name shown in Bitbucket URLs (optional, only needed for Bitbucket tools).")] string? bitbucketWorkspace = null)
    {
        session.Domain    = domain.Trim().TrimStart('h', 't', 'p', 's', ':','/');
        session.Email     = email.Trim();
        session.ApiToken  = apiToken.Trim();

        if (!string.IsNullOrWhiteSpace(bitbucketWorkspace))
            session.BitbucketWorkspace = bitbucketWorkspace.Trim();

        var workspaceNote = session.BitbucketWorkspace is not null
            ? $" Bitbucket workspace set to '{session.BitbucketWorkspace}'."
            : " Note: Bitbucket workspace not set — call atlassian_configure again with bitbucketWorkspace if needed.";

        return $"✅ Atlassian session credentials configured for domain '{session.Domain}'." +
               workspaceNote +
               " You can now use jira_*, confluence_*, and bitbucket_* tools.";
    }

    [McpServerTool(Name = "atlassian_show_config")]
    [Description(
        "Shows the current Atlassian configuration status — which credentials are set " +
        "(from appsettings.json or session) without revealing sensitive values.")]
    public string AtlassianShowConfig()
    {
        var domainSource   = !string.IsNullOrWhiteSpace(session.Domain)    ? $"'{session.Domain}' (session)" : "not set in session";
        var emailSource    = !string.IsNullOrWhiteSpace(session.Email)     ? "set (session)"                 : "not set in session";
        var tokenSource    = !string.IsNullOrWhiteSpace(session.ApiToken)  ? "set (session)"                 : "not set in session";
        var workspaceSource = !string.IsNullOrWhiteSpace(session.BitbucketWorkspace) ? $"'{session.BitbucketWorkspace}' (session)" : "not set in session";

        return $"""
                Atlassian Configuration Status
                ══════════════════════════════
                Domain             : {domainSource}
                Email              : {emailSource}
                API Token          : {tokenSource}
                Bitbucket Workspace: {workspaceSource}

                Note: appsettings.json values are used as fallback when session values are not set.
                To set session credentials, call 'atlassian_configure'.
                """;
    }
}
