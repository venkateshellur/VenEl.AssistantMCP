using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.MCP.Core.Registration;
using VenEl.MCP.Core.Dispatcher;
using VenEl.MCP.MicrosoftTeams.Configuration;
using VenEl.MCP.MicrosoftTeams.Tools;

namespace VenEl.MCP.MicrosoftTeams.Extensions;

public static class TeamsServiceCollectionExtensions
{
    public static IServiceCollection AddTeamsMcp(this IServiceCollection services, IConfiguration configSection)
    {
        services.Configure<TeamsOptions>(configSection);
        services.AddHttpClient("TeamsWebhookClient");

        // ── Self-register MCP tools into the shared registry ──────────────────
        services.GetOrAddFeatureRegistry().Register(
            featureName: "MicrosoftTeams",
            description: "Microsoft Teams integration tools: Post messages via Graph API or Webhooks.",
            toolRegistration: mcpBuilder => mcpBuilder.WithTools<TeamsDispatcherTool>());

        // Register handlers
        services.AddActionHandlersFromAssembly<TeamsCommandArgs>(typeof(TeamsServiceCollectionExtensions).Assembly);

        return services;
    }
}
