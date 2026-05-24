using Microsoft.Extensions.DependencyInjection;
using VenEl.AssistantMCP.Core.Security;

namespace VenEl.AssistantMCP.Core.Extensions;

public static class CoreServiceExtensions
{
    public static IServiceCollection AddCoreSecurity(this IServiceCollection services)
    {
        services.AddSingleton<SecretManager>();
        return services;
    }
}
