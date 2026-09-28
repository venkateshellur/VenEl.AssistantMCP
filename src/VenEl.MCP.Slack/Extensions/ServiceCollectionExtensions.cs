using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.MCP.Core.Dispatcher;
using VenEl.MCP.Core.Registration;
using VenEl.MCP.Slack.Configuration;
using VenEl.MCP.Slack.Tools;

namespace VenEl.MCP.Slack.Extensions;

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
