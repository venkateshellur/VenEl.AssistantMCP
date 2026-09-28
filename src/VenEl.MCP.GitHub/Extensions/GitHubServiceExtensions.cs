using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.MCP.Core.Registration;
using VenEl.MCP.GitHub.Configuration;
using VenEl.MCP.GitHub.Services;
using VenEl.MCP.GitHub.Tools;
using VenEl.MCP.Core.Extensions;
using VenEl.MCP.Core.Proactive;
using VenEl.MCP.GitHub.Proactive;

namespace VenEl.MCP.GitHub.Extensions;

public static class GitHubServiceExtensions
{
    public static IServiceCollection AddGitHubFeature(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<GitHubOptions>(config.GetSection("GitHub"));

        // Register Session State
        services.AddSingleton<GitHubSession>();
        services.AddSingleton<IProactiveSource, GitHubProactiveSource>();

        // Register HTTP Client for GitHub
        services.AddHttpClient<IGitHubHttpClient, GitHubHttpClient>().AddMcpCaching();

        // ── Self-register MCP tools into the shared registry ──────────────────
        services.GetOrAddFeatureRegistry().Register(
            featureName: "GitHub",
            description: "GitHub tools: projects, repositories, pull requests, diffs, and session credential setup.",
            toolRegistration: mcpBuilder => mcpBuilder.WithTools<GitHubDispatcherTool>());

        // Automatically discover and register all IActionHandler<GitHubCommandArgs> implementations
        services.AddActionHandlersFromAssembly<GitHubCommandArgs>(typeof(GitHubServiceExtensions).Assembly);

        return services;
    }
}
