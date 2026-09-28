using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.MCP.Core.Registration;

namespace VenEl.MCP.Host.Extensions;

public static class HostServiceExtensions
{
    public static IServiceCollection AddHostFeature(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddActionHandlersFromAssembly<Tools.HostCommandArgs>(typeof(HostServiceExtensions).Assembly);

        services.GetOrAddFeatureRegistry().Register(
            featureName: "Host",
            description: "Native Host Operating System integrations (Files/Shell)",
            toolRegistration: builder =>
            {
                builder.WithTools<Tools.HostDispatcherTool>();
            });

        return services;
    }
}
