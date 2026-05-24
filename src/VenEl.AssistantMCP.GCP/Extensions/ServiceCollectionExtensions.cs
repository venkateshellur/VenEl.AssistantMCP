using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.AssistantMCP.GCP.Configuration;
using VenEl.AssistantMCP.GCP.Tools;
using VenEl.AssistantMCP.Core.Dispatcher;
using VenEl.AssistantMCP.Core.Registration;

namespace VenEl.AssistantMCP.GCP.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGcpFeature(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<GcpOptions>(config.GetSection("GCP"));
        
        services.AddSingleton<IActionHandler<GcpCommandArgs>, GcpListStorageBucketsActionHandler>();

        services.GetOrAddFeatureRegistry()
            .Register("GCP", "GCP tools for Cloud Storage", mcpBuilder =>
            {
                mcpBuilder.WithTools<GcpDispatcherTool>();
            });

        return services;
    }
}
