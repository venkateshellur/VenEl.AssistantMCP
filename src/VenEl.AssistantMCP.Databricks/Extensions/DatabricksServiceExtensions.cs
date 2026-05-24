using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.AssistantMCP.Databricks.Configuration;
using VenEl.AssistantMCP.Databricks.Services;
using VenEl.AssistantMCP.Databricks.Tools;
using VenEl.AssistantMCP.Core.Registration;

namespace VenEl.AssistantMCP.Databricks.Extensions;

public static class DatabricksServiceExtensions
{
    public static IServiceCollection AddDatabricksFeature(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabricksOptions>(configuration.GetSection(DatabricksOptions.SectionName));
        
        services.AddHttpClient<DatabricksHttpClient>();
        
        services.AddActionHandlersFromAssembly<DatabricksCommandArgs>(typeof(DatabricksServiceExtensions).Assembly);

        services.GetOrAddFeatureRegistry().Register(
            featureName: "Databricks",
            description: "Databricks tools: manage jobs, clusters, and workspace files.",
            toolRegistration: mcpBuilder => mcpBuilder.WithTools<DatabricksDispatcherTool>());
        
        return services;
    }
}
