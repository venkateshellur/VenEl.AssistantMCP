using Microsoft.Extensions.DependencyInjection;
using VenEl.MCPAssistant.Core.Dispatcher;
using VenEl.MCPAssistant.Core.Registration;
using VenEl.MCPAssistant.Kubernetes.Tools;

namespace VenEl.MCPAssistant.Kubernetes.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKubernetesFeature(this IServiceCollection services)
    {
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
