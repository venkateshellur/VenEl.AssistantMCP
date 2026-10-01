using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.MCP.Azure.Configuration;
using VenEl.MCP.Azure.Services;
using VenEl.MCP.Azure.Services.Auth;
using VenEl.MCP.Azure.Tools;
using VenEl.MCP.Core.Registration;
using VenEl.MCP.Core.Extensions;

namespace VenEl.MCP.Azure.Extensions;

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

        services.AddSingleton<IAzureBlobService, AzureBlobService>();

        // ── HTTP client ───────────────────────────────────────────────────────
        services.AddHttpClient<IAzureHttpClient, AzureHttpClient>().AddMcpCaching();

        // ── Action Handlers ───────────────────────────────────────────────────
        services.AddActionHandlersFromAssembly<AzureCommandArgs>(typeof(AzureServiceExtensions).Assembly);

        // ── Self-register MCP tools into the shared registry ──────────────────
        services.GetOrAddFeatureRegistry().Register(
            featureName: "Azure",
            description: "Azure tools: Azure DevOps projects, repositories, pull requests, pipelines, work items, Key Vault secrets/certificates, Blob Storage, and session credential setup.",
            toolRegistration: mcpBuilder => mcpBuilder.WithTools<AzureDispatcherTool>());

        return services;
    }
}
