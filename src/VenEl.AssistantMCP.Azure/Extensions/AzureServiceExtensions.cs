using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.AssistantMCP.Azure.Configuration;
using VenEl.AssistantMCP.Azure.Services;
using VenEl.AssistantMCP.Azure.Services.Auth;
using VenEl.AssistantMCP.Azure.Tools;
using VenEl.AssistantMCP.Core.Registration;
using VenEl.AssistantMCP.Core.Extensions;

namespace VenEl.AssistantMCP.Azure.Extensions;

/// <summary>
/// DI registration extensions for the Azure feature module.
/// </summary>
public static class AzureServiceExtensions
{
    public static IServiceCollection AddAzureFeature(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Configuration ─────────────────────────────────────────────────────
        services.Configure<AzureOptions>(
            configuration.GetSection(AzureOptions.SectionName));

        // ── Auth providers ────────────────────────────────────────────────────
        services.AddSingleton<PatAuthProvider>();

        // ── Session credentials ───────────────────────────────────────────────
        services.AddSingleton<AzureSessionCredentials>();

        // ── HTTP client ───────────────────────────────────────────────────────
        services.AddHttpClient<IAzureHttpClient, AzureHttpClient>().AddMcpCaching();

        // ── Action Handlers ───────────────────────────────────────────────────
        services.AddActionHandlersFromAssembly<AzureCommandArgs>(typeof(AzureServiceExtensions).Assembly);

        // ── Self-register MCP tools into the shared registry ──────────────────
        services.GetOrAddFeatureRegistry().Register(
            featureName: "Azure",
            description: "Azure tools: Azure DevOps projects, repositories, pull requests, " +
                         "and session credential setup.",
            toolRegistration: mcpBuilder => mcpBuilder.WithTools<AzureDispatcherTool>());

        return services;
    }
}
