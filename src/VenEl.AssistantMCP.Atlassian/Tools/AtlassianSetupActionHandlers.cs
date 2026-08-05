using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VenEl.AssistantMCP.Atlassian.Services;
using VenEl.AssistantMCP.Core.Configuration;
using VenEl.AssistantMCP.Core.Dispatcher;

namespace VenEl.AssistantMCP.Atlassian.Tools;

public sealed class AtlassianConfigureActionHandler(AtlassianSessionCredentials session, AppSettingsUpdater appSettingsUpdater) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "atlassian_configure";

    public string? Validate(AtlassianCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Domain)) return "Missing required parameter 'Domain'.";
        if (string.IsNullOrWhiteSpace(args.Email)) return "Missing required parameter 'Email'.";
        if (string.IsNullOrWhiteSpace(args.ApiToken)) return "Missing required parameter 'ApiToken'.";
        return null;
    }

    public Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        session.Domain = args.Domain?.Trim().TrimStart('h', 't', 'p', 's', ':', '/');
        session.Email = args.Email?.Trim();
        session.ApiToken = args.ApiToken?.Trim();

        if (!string.IsNullOrWhiteSpace(args.BitbucketWorkspace))
            session.BitbucketWorkspace = args.BitbucketWorkspace.Trim();

        var workspaceNote = session.BitbucketWorkspace is not null
            ? $" Bitbucket workspace set to '{session.BitbucketWorkspace}'."
            : " Note: Bitbucket workspace not set — call atlassian_configure again with BitbucketWorkspace if needed.";

        var configValues = new Dictionary<string, object?>
        {
            { "Domain", session.Domain },
            { "Email", session.Email },
            { "ApiToken", session.ApiToken }
        };

        if (session.BitbucketWorkspace is not null)
        {
            configValues["BitbucketWorkspace"] = session.BitbucketWorkspace;
        }

        appSettingsUpdater.UpdateSection("Atlassian", configValues);

        return Task.FromResult($"✅ Atlassian session credentials configured for domain '{session.Domain}' and saved to appsettings.json." +
               workspaceNote +
               " You can now use jira_*, confluence_*, and bitbucket_* tools.");
    }
}

public sealed class AtlassianShowConfigActionHandler(AtlassianSessionCredentials session) : IActionHandler<AtlassianCommandArgs>
{
    public string ActionName => "atlassian_show_config";

    public string? Validate(AtlassianCommandArgs args) => null;

    public Task<string> HandleAsync(AtlassianCommandArgs args, CancellationToken ct)
    {
        var domainSource = !string.IsNullOrWhiteSpace(session.Domain) ? $"'{session.Domain}' (session)" : "not set in session";
        var emailSource = !string.IsNullOrWhiteSpace(session.Email) ? "set (session)" : "not set in session";
        var tokenSource = !string.IsNullOrWhiteSpace(session.ApiToken) ? "set (session)" : "not set in session";
        var workspaceSource = !string.IsNullOrWhiteSpace(session.BitbucketWorkspace) ? $"'{session.BitbucketWorkspace}' (session)" : "not set in session";

        return Task.FromResult($"""
                Atlassian Configuration Status
                ══════════════════════════════
                Domain             : {domainSource}
                Email              : {emailSource}
                API Token          : {tokenSource}
                Bitbucket Workspace: {workspaceSource}

                Note: appsettings.json values are used as fallback when session values are not set.
                To set session credentials, call 'atlassian_configure'.
                """);
    }
}
