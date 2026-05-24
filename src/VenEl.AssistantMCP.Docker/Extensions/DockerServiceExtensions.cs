using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.AssistantMCP.Core.Registration;
using VenEl.AssistantMCP.Core.Dispatcher;
using VenEl.AssistantMCP.Docker.Configuration;
using VenEl.AssistantMCP.Docker.Services;
using VenEl.AssistantMCP.Docker.Tools;

namespace VenEl.AssistantMCP.Docker.Extensions;

public static class DockerServiceExtensions
{
    public static IServiceCollection AddDockerFeature(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DockerOptions>(configuration.GetSection(DockerOptions.SectionName));
        
        // ── Core Services ─────────────────────────────────────────────────────
        services.AddSingleton<IDockerCliService, DockerCliService>();

        // ── Self-register MCP tools into the shared registry ──────────────────
        services.GetOrAddFeatureRegistry().Register(
            featureName: "Docker",
            description: "Docker Management tools: list/start/stop/restart containers, view logs, and list images.",
            toolRegistration: mcpBuilder => mcpBuilder.WithTools<DockerDispatcherTool>());

        // ── Action Handlers ───────────────────────────────────────────────────
        services.AddActionHandlersFromAssembly<DockerCommandArgs>(typeof(DockerServiceExtensions).Assembly);

        return services;
    }
}
