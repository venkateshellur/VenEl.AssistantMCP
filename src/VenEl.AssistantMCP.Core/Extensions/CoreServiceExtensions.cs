using Microsoft.Extensions.DependencyInjection;
using VenEl.AssistantMCP.Core.Security;
using VenEl.AssistantMCP.Core.Configuration;

namespace VenEl.AssistantMCP.Core.Extensions;

public static class CoreServiceExtensions
{
    public static IServiceCollection AddCoreSecurity(this IServiceCollection services)
    {
        services.AddSingleton<SecretManager>();
        services.AddSingleton<AppSettingsUpdater>();
        return services;
    }
}
