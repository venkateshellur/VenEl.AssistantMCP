using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.MCP.Core.Dispatcher;
using VenEl.MCP.Core.Registration;
using VenEl.MCP.Kubernetes.Configuration;
using VenEl.MCP.Kubernetes.Tools;

namespace VenEl.MCP.Kubernetes.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKubernetesFeature(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KubernetesOptions>(configuration.GetSection(KubernetesOptions.SectionName));
        
        services.AddSingleton<IActionHandler<KubernetesCommandArgs>, KubectlGetPodsActionHandler>();
        services.AddSingleton<IActionHandler<KubernetesCommandArgs>, KubectlGetDeploymentsActionHandler>();

        services.GetOrAddFeatureRegistry()
            .Register("Kubernetes", "Kubernetes integration tools", mcpBuilder =>
            {
                mcpBuilder.WithTools<KubernetesDispatcherTool>();
            });

        return services;
    }
}
