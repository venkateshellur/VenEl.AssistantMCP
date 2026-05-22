using System.Threading;
using System.Threading.Tasks;
using VenEl.MCPAssistant.Core.Dispatcher;
using VenEl.MCPAssistant.Docker.Services;

namespace VenEl.MCPAssistant.Docker.Tools;

public sealed class DockerComposeUpActionHandler(IDockerCliService dockerCli) : IActionHandler<DockerCommandArgs>
{
    public string ActionName => "docker_compose_up";

    public string? Validate(DockerCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.ComposeFilePath)) return "Missing ComposeFilePath";
        return null;
    }

    public async Task<string> HandleAsync(DockerCommandArgs args, CancellationToken ct)
    {
        return await dockerCli.ExecuteCommandAsync($"compose -f {args.ComposeFilePath} up -d", ct);
    }
}

public sealed class DockerComposeDownActionHandler(IDockerCliService dockerCli) : IActionHandler<DockerCommandArgs>
{
    public string ActionName => "docker_compose_down";

    public string? Validate(DockerCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.ComposeFilePath)) return "Missing ComposeFilePath";
        return null;
    }

    public async Task<string> HandleAsync(DockerCommandArgs args, CancellationToken ct)
    {
        return await dockerCli.ExecuteCommandAsync($"compose -f {args.ComposeFilePath} down", ct);
    }
}
