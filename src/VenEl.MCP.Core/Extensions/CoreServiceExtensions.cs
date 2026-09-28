using Microsoft.Extensions.DependencyInjection;
using VenEl.MCP.Core.Security;
using VenEl.MCP.Core.Configuration;
using VenEl.MCP.Core.Updates;
using VenEl.MCP.Core.Http;
using VenEl.MCP.Core.Registration;

namespace VenEl.MCP.Core.Extensions;

public static class CoreServiceExtensions
{
    public static IServiceCollection AddCoreSecurity(this IServiceCollection services)
    {
        services.AddSingleton<SecretManager>();
        services.AddSingleton<AppSettingsUpdater>();
        services.AddHttpClient<IUpdateChecker, UpdateChecker>();
        services.AddTransient<VenEl.MCP.Core.Proactive.IProactiveSource>(sp => (VenEl.MCP.Core.Proactive.IProactiveSource)sp.GetRequiredService<IUpdateChecker>());
        
        services.AddMemoryCache();
        services.AddTransient<CachingDelegatingHandler>();
        
        // Proactive Notifications
        services.AddSingleton<VenEl.MCP.Core.Proactive.IAlertsManager, VenEl.MCP.Core.Proactive.AlertsManager>();
        services.AddHostedService<VenEl.MCP.Core.Workers.ProactiveNotificationWorker>();
        
        services.GetOrAddFeatureRegistry().Register("Core", "Core Features", mcp => {
            mcp.WithResources<VenEl.MCP.Core.Proactive.AlertsResource>();
        });
        
        return services;
    }

    public static IHttpClientBuilder AddMcpCaching(this IHttpClientBuilder builder)
    {
        return builder.AddHttpMessageHandler<CachingDelegatingHandler>();
    }
}
