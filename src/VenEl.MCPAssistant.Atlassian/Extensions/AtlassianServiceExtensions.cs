using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.MCPAssistant.Atlassian.Configuration;
using VenEl.MCPAssistant.Atlassian.Services;
using VenEl.MCPAssistant.Atlassian.Services.Auth;
using VenEl.MCPAssistant.Atlassian.Tools;
using VenEl.MCPAssistant.Core.Registration;

namespace VenEl.MCPAssistant.Atlassian.Extensions;

/// <summary>
/// DI registration extensions for the Atlassian feature module.
/// </summary>
public static class AtlassianServiceExtensions
{
    /// <summary>
    /// Registers all Atlassian services and self-registers Jira, Confluence, and
    /// Bitbucket MCP tools into the shared <see cref="McpFeatureRegistry"/>.
    ///
    /// <para>
    /// In <c>Program.cs</c>, add one line:
    /// <code>
    ///     builder.Services.AddAtlassianFeature(builder.Configuration);
    /// </code>
    /// </para>
    /// </summary>
    public static IServiceCollection AddAtlassianFeature(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Configuration ─────────────────────────────────────────────────────
        services.Configure<AtlassianOptions>(
            configuration.GetSection(AtlassianOptions.SectionName));

        // ── Auth providers (singletons — OAuth caches its token) ──────────────
        services.AddSingleton<ApiTokenAuthProvider>();
        services.AddSingleton<OAuthAuthProvider>();

        // ── Session credentials (in-memory, conversation-supplied fallback) ────
        services.AddSingleton<AtlassianSessionCredentials>();

        // ── HTTP client ───────────────────────────────────────────────────────
        services.AddHttpClient<IAtlassianHttpClient, AtlassianHttpClient>();

        // ── Self-register MCP tools into the shared registry ──────────────────
        services.GetOrAddFeatureRegistry().Register(
            featureName: "Atlassian",
            description: "Atlassian Cloud tools: Jira issues/projects, " +
                         "Confluence pages/spaces, Bitbucket repositories/pull requests, " +
                         "and session credential setup.",
            toolRegistration: mcpBuilder => mcpBuilder
                .WithTools<AtlassianSetupTools>()
                .WithTools<AtlassianJiraTools>()
                .WithTools<AtlassianConfluenceTools>()
                .WithTools<AtlassianBitbucketTools>());

        return services;
    }
}
