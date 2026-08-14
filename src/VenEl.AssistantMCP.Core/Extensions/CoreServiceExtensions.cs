using Microsoft.Extensions.DependencyInjection;
using VenEl.AssistantMCP.Core.Security;
using VenEl.AssistantMCP.Core.Configuration;
using VenEl.AssistantMCP.Core.Updates;
using VenEl.AssistantMCP.Core.Http;
using VenEl.AssistantMCP.Core.Registration;

namespace VenEl.AssistantMCP.Core.Extensions;

public static class CoreServiceExtensions
{
    public static IServiceCollection AddCoreSecurity(this IServiceCollection services)
    {
        services.AddSingleton<SecretManager>();
        services.AddSingleton<AppSettingsUpdater>();
        services.AddHttpClient<IUpdateChecker, UpdateChecker>();
        services.AddTransient<VenEl.AssistantMCP.Core.Proactive.IProactiveSource>(sp => (VenEl.AssistantMCP.Core.Proactive.IProactiveSource)sp.GetRequiredService<IUpdateChecker>());
        
        services.AddMemoryCache();
        services.AddTransient<CachingDelegatingHandler>();
        
        // Proactive Notifications
        services.AddSingleton<VenEl.AssistantMCP.Core.Proactive.IAlertsManager, VenEl.AssistantMCP.Core.Proactive.AlertsManager>();
        services.AddHostedService<VenEl.AssistantMCP.Core.Workers.ProactiveNotificationWorker>();
        
        services.GetOrAddFeatureRegistry().Register("Core", "Core Features", mcp => {
            mcp.WithResources<VenEl.AssistantMCP.Core.Proactive.AlertsResource>();
        });
        
        return services;
    }

    public static IHttpClientBuilder AddMcpCaching(this IHttpClientBuilder builder)
    {
        return builder.AddHttpMessageHandler<CachingDelegatingHandler>();
    }
}
