using Microsoft.Extensions.DependencyInjection;
using VenEl.MCPAssistant.Core.Security;

namespace VenEl.MCPAssistant.Core.Extensions;

public static class CoreServiceExtensions
{
    public static IServiceCollection AddCoreSecurity(this IServiceCollection services)
    {
        services.AddSingleton<SecretManager>();
        return services;
    }
}
