using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenEl.MCPAssistant.Core.Dispatcher;

namespace VenEl.MCPAssistant.Kubernetes.Tools;

internal static class KubectlHelper
{
    public static async Task<string> RunKubectlAsync(string arguments, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "kubectl",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = Process.Start(psi);
            if (process is null) return "[ERROR] Failed to start kubectl process.";

            var outputTask = process.StandardOutput.ReadToEndAsync(ct);
            var errorTask = process.StandardError.ReadToEndAsync(ct);

            await process.WaitForExitAsync(ct);

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                return $"[ERROR] kubectl exited with code {process.ExitCode}\n{error}";
            }

            return string.IsNullOrWhiteSpace(output) ? "[OK]" : output;
        }
        catch (Exception ex)
        {
            return $"[ERROR] Failed to execute kubectl: {ex.Message}";
        }
    }
}

public sealed class KubectlGetPodsActionHandler(ILogger<KubectlGetPodsActionHandler> logger) : IActionHandler<KubernetesCommandArgs>
{
    public string ActionName => "kubectl_get_pods";

    public string? Validate(KubernetesCommandArgs args) => null;

    public async Task<string> HandleAsync(KubernetesCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Getting pods in namespace {Namespace}", args.Namespace ?? "default");
        var nsArg = string.IsNullOrWhiteSpace(args.Namespace) ? "" : $"-n {args.Namespace}";
        return await KubectlHelper.RunKubectlAsync($"get pods {nsArg}", ct);
    }
}

public sealed class KubectlGetDeploymentsActionHandler(ILogger<KubectlGetDeploymentsActionHandler> logger) : IActionHandler<KubernetesCommandArgs>
{
    public string ActionName => "kubectl_get_deployments";

    public string? Validate(KubernetesCommandArgs args) => null;

    public async Task<string> HandleAsync(KubernetesCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Getting deployments in namespace {Namespace}", args.Namespace ?? "default");
        var nsArg = string.IsNullOrWhiteSpace(args.Namespace) ? "" : $"-n {args.Namespace}";
        return await KubectlHelper.RunKubectlAsync($"get deployments {nsArg}", ct);
    }
}
