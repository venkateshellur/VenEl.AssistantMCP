using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        // Register tools & handlers
        services.AddSingleton<TeamsDispatcherTool>();
        services.AddTransient<IActionHandler<TeamsCommandArgs>, TeamsPostMessageActionHandler>();

        return services;
    }
}
