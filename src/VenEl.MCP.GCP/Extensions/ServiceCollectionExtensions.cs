using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.MCP.GCP.Configuration;
using VenEl.MCP.GCP.Tools;
using VenEl.MCP.Core.Dispatcher;
using VenEl.MCP.Core.Registration;

namespace VenEl.MCP.GCP.Extensions;

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
