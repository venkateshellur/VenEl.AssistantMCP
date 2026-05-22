using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.MCPAssistant.GCP.Configuration;
using VenEl.MCPAssistant.GCP.Tools;
using VenEl.MCPAssistant.Core.Dispatcher;
using VenEl.MCPAssistant.Core.Registration;

namespace VenEl.MCPAssistant.GCP.Extensions;

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
