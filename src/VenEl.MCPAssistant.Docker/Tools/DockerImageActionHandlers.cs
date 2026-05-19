using System.Threading;
using System.Threading.Tasks;
using VenEl.MCPAssistant.Core.Dispatcher;
using VenEl.MCPAssistant.Docker.Services;

namespace VenEl.MCPAssistant.Docker.Tools;

public sealed class DockerListImagesActionHandler(IDockerCliService dockerCli) : IActionHandler<DockerCommandArgs>
{
    public string ActionName => "docker_list_images";

    public string? Validate(DockerCommandArgs args) => null;

    public async Task<string> HandleAsync(DockerCommandArgs args, CancellationToken ct)
    {
        return await dockerCli.ExecuteCommandAsync($"images --format \"{{{{json .}}}}\"", ct);
    }
}
