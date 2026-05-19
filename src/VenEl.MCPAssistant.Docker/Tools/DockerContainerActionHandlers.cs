using System;
using System.Threading;
using System.Threading.Tasks;
using VenEl.MCPAssistant.Core.Dispatcher;
using VenEl.MCPAssistant.Docker.Services;

namespace VenEl.MCPAssistant.Docker.Tools;

public sealed class DockerListContainersActionHandler(IDockerCliService dockerCli) : IActionHandler<DockerCommandArgs>
{
    public string ActionName => "docker_list_containers";

    public string? Validate(DockerCommandArgs args) => null;

    public async Task<string> HandleAsync(DockerCommandArgs args, CancellationToken ct)
    {
        var flag = args.All == true ? "-a " : "";
        return await dockerCli.ExecuteCommandAsync($"ps {flag}--format \"{{{{json .}}}}\"", ct);
    }
}

public sealed class DockerStartContainerActionHandler(IDockerCliService dockerCli) : IActionHandler<DockerCommandArgs>
{
    public string ActionName => "docker_start_container";

    public string? Validate(DockerCommandArgs args) => string.IsNullOrWhiteSpace(args.ContainerId) ? "Missing ContainerId" : null;

    public async Task<string> HandleAsync(DockerCommandArgs args, CancellationToken ct)
    {
        return await dockerCli.ExecuteCommandAsync($"start {args.ContainerId}", ct);
    }
}

public sealed class DockerStopContainerActionHandler(IDockerCliService dockerCli) : IActionHandler<DockerCommandArgs>
{
    public string ActionName => "docker_stop_container";

    public string? Validate(DockerCommandArgs args) => string.IsNullOrWhiteSpace(args.ContainerId) ? "Missing ContainerId" : null;

    public async Task<string> HandleAsync(DockerCommandArgs args, CancellationToken ct)
    {
        return await dockerCli.ExecuteCommandAsync($"stop {args.ContainerId}", ct);
    }
}

public sealed class DockerRestartContainerActionHandler(IDockerCliService dockerCli) : IActionHandler<DockerCommandArgs>
{
    public string ActionName => "docker_restart_container";

    public string? Validate(DockerCommandArgs args) => string.IsNullOrWhiteSpace(args.ContainerId) ? "Missing ContainerId" : null;

    public async Task<string> HandleAsync(DockerCommandArgs args, CancellationToken ct)
    {
        return await dockerCli.ExecuteCommandAsync($"restart {args.ContainerId}", ct);
    }
}

public sealed class DockerRemoveContainerActionHandler(IDockerCliService dockerCli) : IActionHandler<DockerCommandArgs>
{
    public string ActionName => "docker_remove_container";

    public string? Validate(DockerCommandArgs args) => string.IsNullOrWhiteSpace(args.ContainerId) ? "Missing ContainerId" : null;

    public async Task<string> HandleAsync(DockerCommandArgs args, CancellationToken ct)
    {
        return await dockerCli.ExecuteCommandAsync($"rm {args.ContainerId}", ct);
    }
}

public sealed class DockerGetLogsActionHandler(IDockerCliService dockerCli) : IActionHandler<DockerCommandArgs>
{
    public string ActionName => "docker_get_logs";

    public string? Validate(DockerCommandArgs args) => string.IsNullOrWhiteSpace(args.ContainerId) ? "Missing ContainerId" : null;

    public async Task<string> HandleAsync(DockerCommandArgs args, CancellationToken ct)
    {
        int tailLines = Math.Clamp(args.Lines ?? 100, 1, 1000);
        return await dockerCli.ExecuteCommandAsync($"logs --tail {tailLines} {args.ContainerId}", ct);
    }
}
