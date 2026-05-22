using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.MCPAssistant.Core.Registration;
using VenEl.MCPAssistant.GitHub.Configuration;
using VenEl.MCPAssistant.GitHub.Services;
using VenEl.MCPAssistant.GitHub.Tools;

namespace VenEl.MCPAssistant.GitHub.Extensions;

public static class GitHubServiceExtensions
{
    public static IServiceCollection AddGitHubFeature(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<GitHubOptions>(config.GetSection("GitHub"));

        // Register Session State
        services.AddSingleton<GitHubSession>();

        // Register HTTP Client for GitHub
        services.AddHttpClient<IGitHubHttpClient, GitHubHttpClient>();

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
