using Microsoft.Extensions.DependencyInjection;

namespace VenEl.MCPAssistant.Core.Registration;

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
}
