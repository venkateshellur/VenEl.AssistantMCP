using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.MCPAssistant.Core.Dispatcher;
using VenEl.MCPAssistant.Core.Registration;
using VenEl.MCPAssistant.Slack.Configuration;
using VenEl.MCPAssistant.Slack.Tools;

namespace VenEl.MCPAssistant.Slack.Extensions;

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
