using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.MCPAssistant.Databricks.Configuration;
using VenEl.MCPAssistant.Databricks.Services;
using VenEl.MCPAssistant.Databricks.Tools;
using VenEl.MCPAssistant.Core.Registration;

namespace VenEl.MCPAssistant.Databricks.Extensions;

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
