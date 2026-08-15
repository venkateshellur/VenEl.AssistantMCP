using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenEl.AssistantMCP.Core.Dispatcher;

namespace VenEl.AssistantMCP.Kubernetes.Tools;

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

public sealed class KubectlGetLogsActionHandler(ILogger<KubectlGetLogsActionHandler> logger) : IActionHandler<KubernetesCommandArgs>
{
    public string ActionName => "kubectl_get_logs";

    public string? Validate(KubernetesCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.ResourceName)) return "Missing required parameter 'ResourceName' (the pod name).";
        return null;
    }

    public async Task<string> HandleAsync(KubernetesCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Getting logs for pod {Pod} in namespace {Namespace}", args.ResourceName, args.Namespace ?? "default");
        var nsArg = string.IsNullOrWhiteSpace(args.Namespace) ? "" : $"-n {args.Namespace}";
        return await KubectlHelper.RunKubectlAsync($"logs {args.ResourceName} {nsArg} --tail=200", ct);
    }
}

public sealed class KubectlDescribeActionHandler(ILogger<KubectlDescribeActionHandler> logger) : IActionHandler<KubernetesCommandArgs>
{
    public string ActionName => "kubectl_describe";

    public string? Validate(KubernetesCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.ResourceType)) return "Missing required parameter 'ResourceType' (e.g. pod, service).";
        if (string.IsNullOrWhiteSpace(args.ResourceName)) return "Missing required parameter 'ResourceName'.";
        return null;
    }

    public async Task<string> HandleAsync(KubernetesCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Describing {Type} {Name} in namespace {Namespace}", args.ResourceType, args.ResourceName, args.Namespace ?? "default");
        var nsArg = string.IsNullOrWhiteSpace(args.Namespace) ? "" : $"-n {args.Namespace}";
        return await KubectlHelper.RunKubectlAsync($"describe {args.ResourceType} {args.ResourceName} {nsArg}", ct);
    }
}

public sealed class KubectlApplyActionHandler(ILogger<KubectlApplyActionHandler> logger) : IActionHandler<KubernetesCommandArgs>
{
    public string ActionName => "kubectl_apply";

    public string? Validate(KubernetesCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.ManifestYaml)) return "Missing required parameter 'ManifestYaml'.";
        return null;
    }

    public async Task<string> HandleAsync(KubernetesCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Applying manifest to namespace {Namespace}", args.Namespace ?? "default");
        var nsArg = string.IsNullOrWhiteSpace(args.Namespace) ? "" : $"-n {args.Namespace}";
        
        // Write manifest to temp file
        var tempFile = System.IO.Path.GetTempFileName() + ".yaml";
        await System.IO.File.WriteAllTextAsync(tempFile, args.ManifestYaml, ct);
        
        try
        {
            return await KubectlHelper.RunKubectlAsync($"apply -f {tempFile} {nsArg}", ct);
        }
        finally
        {
            if (System.IO.File.Exists(tempFile)) System.IO.File.Delete(tempFile);
        }
    }
}

internal static class HelmHelper
{
    public static async Task<string> RunHelmAsync(string arguments, CancellationToken ct)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "helm",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = System.Diagnostics.Process.Start(psi);
            if (process is null) return "[ERROR] Failed to start helm process.";

            var outputTask = process.StandardOutput.ReadToEndAsync(ct);
            var errorTask = process.StandardError.ReadToEndAsync(ct);

            await process.WaitForExitAsync(ct);

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                return $"[ERROR] helm exited with code {process.ExitCode}\n{error}";
            }

            return string.IsNullOrWhiteSpace(output) ? "[OK]" : output;
        }
        catch (Exception ex)
        {
            return $"[ERROR] Failed to execute helm: {ex.Message}";
        }
    }
}

public sealed class HelmListActionHandler(ILogger<HelmListActionHandler> logger) : IActionHandler<KubernetesCommandArgs>
{
    public string ActionName => "helm_list";

    public string? Validate(KubernetesCommandArgs args) => null;

    public async Task<string> HandleAsync(KubernetesCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Listing Helm releases in namespace {Namespace}", args.Namespace ?? "all");
        var nsArg = string.IsNullOrWhiteSpace(args.Namespace) ? "--all-namespaces" : $"-n {args.Namespace}";
        return await HelmHelper.RunHelmAsync($"list {nsArg}", ct);
    }
}

public sealed class HelmUpgradeActionHandler(ILogger<HelmUpgradeActionHandler> logger) : IActionHandler<KubernetesCommandArgs>
{
    public string ActionName => "helm_upgrade";

    public string? Validate(KubernetesCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.ReleaseName)) return "Missing required parameter 'ReleaseName'.";
        if (string.IsNullOrWhiteSpace(args.ChartName)) return "Missing required parameter 'ChartName'.";
        return null;
    }

    public async Task<string> HandleAsync(KubernetesCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Upgrading/Installing Helm release {Release} with chart {Chart} in namespace {Namespace}", args.ReleaseName, args.ChartName, args.Namespace ?? "default");
        var nsArg = string.IsNullOrWhiteSpace(args.Namespace) ? "" : $"-n {args.Namespace}";
        return await HelmHelper.RunHelmAsync($"upgrade --install {args.ReleaseName} {args.ChartName} {nsArg}", ct);
    }
}
