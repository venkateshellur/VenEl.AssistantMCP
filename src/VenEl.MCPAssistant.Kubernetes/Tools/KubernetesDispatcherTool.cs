using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using VenEl.MCPAssistant.Core.Dispatcher;

namespace VenEl.MCPAssistant.Kubernetes.Tools;

[McpServerToolType]
public sealed class KubernetesDispatcherTool : DispatcherToolBase<KubernetesCommandArgs>
{
    public KubernetesDispatcherTool(IServiceProvider serviceProvider) 
        : base(serviceProvider, "Kubernetes")
    {
    }

    protected override string? GetRequestedAction(KubernetesCommandArgs args) => args.Action;

    [McpServerTool(Name = "mcp_venel_kubernetes_commands")]
    [Description("Kubernetes integration tools: Get pods and deployments via kubectl.")]
    public Task<string> DispatchKubernetesCommandAsync(
        [Description("The arguments for the Kubernetes command")] KubernetesCommandArgs args,
        CancellationToken ct)
    {
        return DispatchAsync(args, ct);
    }
}
