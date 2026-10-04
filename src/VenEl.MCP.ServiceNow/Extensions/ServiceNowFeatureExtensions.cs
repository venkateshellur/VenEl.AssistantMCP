using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.MCP.Core.Registration;
using VenEl.MCP.ServiceNow.Configuration;
using VenEl.MCP.ServiceNow.Client;
using VenEl.MCP.ServiceNow.Tools;

namespace VenEl.MCP.ServiceNow.Extensions;

public static class ServiceNowFeatureExtensions
{
    public static IServiceCollection AddServiceNowFeature(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind configuration
        services.Configure<ServiceNowOptions>(configuration.GetSection(ServiceNowOptions.SectionName));

        // Register the smart client
        services.AddHttpClient<ResilientServiceNowClient>();

        // Register action handlers for this feature
        services.AddActionHandlersFromAssembly<ServiceNowCommandArgs>(typeof(ServiceNowFeatureExtensions).Assembly);

        // Register the tools for the MCP server
        services.GetOrAddFeatureRegistry().Register(
            featureName: "ServiceNow",
            description: "ServiceNow API tools: read and manage records via intelligent API version fallback.",
            toolRegistration: mcpBuilder => mcpBuilder.WithTools<ServiceNowDispatcherTool>()
        );

        return services;
    }
}
