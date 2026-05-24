using Microsoft.Extensions.DependencyInjection;

namespace VenEl.AssistantMCP.Core.Registration;

/// <summary>
/// Extension methods that make it easy for the <c>Program.cs</c> host and
/// feature libraries to interact with <see cref="McpFeatureRegistry"/> through DI.
/// </summary>
public static class McpFeatureRegistryExtensions
{
    /// <summary>
    /// Returns the singleton <see cref="McpFeatureRegistry"/> from the service collection,
    /// creating and registering it if this is the first call.
    /// </summary>
    /// <remarks>
    /// This is intentionally called at registration-time (before
    /// <c>builder.Build()</c>) so feature modules can self-register their tool
    /// callbacks into the registry during DI setup.
    /// </remarks>
    public static McpFeatureRegistry GetOrAddFeatureRegistry(
        this IServiceCollection services)
    {
        if (services.FirstOrDefault(
                d => d.ServiceType == typeof(McpFeatureRegistry)
                  && d.Lifetime    == ServiceLifetime.Singleton
                  && d.ImplementationInstance is McpFeatureRegistry)
            ?.ImplementationInstance is McpFeatureRegistry existing)
        {
            return existing;
        }

        var registry = new McpFeatureRegistry();
        services.AddSingleton(registry);
        return registry;
    }

    /// <summary>
    /// Scans the given assembly for classes implementing IActionHandler{TArgs} and registers them in DI.
    /// </summary>
    public static IServiceCollection AddActionHandlersFromAssembly<TArgs>(
        this IServiceCollection services, 
        System.Reflection.Assembly assembly) where TArgs : class
    {
        var handlerType = typeof(VenEl.AssistantMCP.Core.Dispatcher.IActionHandler<TArgs>);
        var types = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && handlerType.IsAssignableFrom(t));

        foreach (var type in types)
        {
            services.AddTransient(handlerType, type);
        }

        return services;
    }
}
