using Microsoft.Extensions.DependencyInjection;
using VenEl.AssistantMCP.Core.Security;
using VenEl.AssistantMCP.Core.Configuration;
using VenEl.AssistantMCP.Core.Updates;
using VenEl.AssistantMCP.Core.Http;

namespace VenEl.AssistantMCP.Core.Extensions;

public static class CoreServiceExtensions
{
    public static IServiceCollection AddCoreSecurity(this IServiceCollection services)
    {
        services.AddSingleton<SecretManager>();
        services.AddSingleton<AppSettingsUpdater>();
        services.AddHttpClient<IUpdateChecker, UpdateChecker>();
        
        services.AddMemoryCache();
        services.AddTransient<CachingDelegatingHandler>();
        
        // Proactive Notifications
        services.AddSingleton<VenEl.AssistantMCP.Core.Proactive.IAlertsManager, VenEl.AssistantMCP.Core.Proactive.AlertsManager>();
        services.AddHostedService<VenEl.AssistantMCP.Core.Workers.ProactiveNotificationWorker>();
        
        return services;
    }

    public static IHttpClientBuilder AddMcpCaching(this IHttpClientBuilder builder)
    {
        return builder.AddHttpMessageHandler<CachingDelegatingHandler>();
    }
}
