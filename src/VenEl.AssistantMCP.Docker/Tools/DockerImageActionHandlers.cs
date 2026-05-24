using System.Threading;
using System.Threading.Tasks;
using VenEl.AssistantMCP.Core.Dispatcher;
using VenEl.AssistantMCP.Docker.Services;

namespace VenEl.AssistantMCP.Docker.Tools;

public sealed class DockerListImagesActionHandler(IDockerCliService dockerCli) : IActionHandler<DockerCommandArgs>
{
    public string ActionName => "docker_list_images";

    public string? Validate(DockerCommandArgs args) => null;

    public async Task<string> HandleAsync(DockerCommandArgs args, CancellationToken ct)
    {
        return await dockerCli.ExecuteCommandAsync($"images --format \"{{{{json .}}}}\"", ct);
    }
}

public sealed class DockerBuildImageActionHandler(IDockerCliService dockerCli) : IActionHandler<DockerCommandArgs>
{
    public string ActionName => "docker_build_image";

    public string? Validate(DockerCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.DockerfilePath)) return "Missing DockerfilePath";
        if (string.IsNullOrWhiteSpace(args.ImageTag)) return "Missing ImageTag";
        return null;
    }

    public async Task<string> HandleAsync(DockerCommandArgs args, CancellationToken ct)
    {
        var contextDir = System.IO.Path.GetDirectoryName(args.DockerfilePath);
        if (string.IsNullOrEmpty(contextDir)) contextDir = ".";
        return await dockerCli.ExecuteCommandAsync($"build -t {args.ImageTag} -f {args.DockerfilePath} {contextDir}", ct);
    }
}
