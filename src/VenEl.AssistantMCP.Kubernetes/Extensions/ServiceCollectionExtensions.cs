using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.AssistantMCP.Core.Dispatcher;
using VenEl.AssistantMCP.Core.Registration;
using VenEl.AssistantMCP.Kubernetes.Configuration;
using VenEl.AssistantMCP.Kubernetes.Tools;

namespace VenEl.AssistantMCP.Kubernetes.Extensions;

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
