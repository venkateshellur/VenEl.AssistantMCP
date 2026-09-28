using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.MCP.Core.Registration;
using VenEl.MCP.Core.Dispatcher;
using VenEl.MCP.Docker.Configuration;
using VenEl.MCP.Docker.Services;
using VenEl.MCP.Docker.Tools;

namespace VenEl.MCP.Docker.Extensions;

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
