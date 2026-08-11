using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.AssistantMCP.Atlassian.Configuration;
using VenEl.AssistantMCP.Atlassian.Services;
using VenEl.AssistantMCP.Atlassian.Services.Auth;
using VenEl.AssistantMCP.Atlassian.Tools;
using VenEl.AssistantMCP.Core.Extensions;
using VenEl.AssistantMCP.Core.Registration;

namespace VenEl.AssistantMCP.Atlassian.Extensions;

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
        services.AddSingleton<VenEl.AssistantMCP.Core.Proactive.IProactiveSource, VenEl.AssistantMCP.Atlassian.Proactive.AtlassianProactiveSource>();

        // ── HTTP client ───────────────────────────────────────────────────────
        services.AddHttpClient<IAtlassianHttpClient, AtlassianHttpClient>().AddMcpCaching();

        // ── Self-register MCP tools into the shared registry ──────────────────
        services.GetOrAddFeatureRegistry().Register(
            featureName: "Atlassian",
            description: "Atlassian Cloud tools: Jira issues/projects, Confluence pages/spaces, Bitbucket repositories/pull requests, and session credential setup.",
            toolRegistration: mcpBuilder => mcpBuilder.WithTools<AtlassianDispatcherTool>());

        // ── Action Handlers ───────────────────────────────────────────────────
        services.AddActionHandlersFromAssembly<AtlassianCommandArgs>(typeof(AtlassianServiceExtensions).Assembly);

        return services;
    }
}
