using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.AssistantMCP.Core.Dispatcher;
using VenEl.AssistantMCP.Core.Registration;
using VenEl.AssistantMCP.Slack.Configuration;
using VenEl.AssistantMCP.Slack.Tools;

namespace VenEl.AssistantMCP.Slack.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSlackFeature(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<SlackOptions>(config.GetSection("Slack"));
        
        services.AddHttpClient("SlackClient");

        services.AddSingleton<IActionHandler<SlackCommandArgs>, SlackPostMessageActionHandler>();

        services.GetOrAddFeatureRegistry()
            .Register("Slack", "Slack integration tools", mcpBuilder =>
            {
                mcpBuilder.WithTools<SlackDispatcherTool>();
            });

        return services;
    }
}
