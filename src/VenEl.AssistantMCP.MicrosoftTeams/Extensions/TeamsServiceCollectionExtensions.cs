using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.AssistantMCP.Core.Registration;
using VenEl.AssistantMCP.Core.Dispatcher;
using VenEl.AssistantMCP.MicrosoftTeams.Configuration;
using VenEl.AssistantMCP.MicrosoftTeams.Tools;

namespace VenEl.AssistantMCP.MicrosoftTeams.Extensions;

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
