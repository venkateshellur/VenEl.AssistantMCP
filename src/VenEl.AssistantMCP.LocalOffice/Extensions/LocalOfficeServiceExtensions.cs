using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.AssistantMCP.Core.Registration;
using VenEl.AssistantMCP.LocalOffice.Configuration;
using VenEl.AssistantMCP.LocalOffice.Tools;

namespace VenEl.AssistantMCP.LocalOffice.Extensions;

public static class LocalOfficeServiceExtensions
{
    public static IServiceCollection AddLocalOfficeTools(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LocalOfficeOptions>(configuration.GetSection(LocalOfficeOptions.SectionName));
        // ── Self-register MCP tools into the shared registry ──────────────────
        services.GetOrAddFeatureRegistry().Register(
            featureName: "LocalOffice",
            description: "Local Office Management tools: Read and write to local Excel files (.xlsx) using OpenXML.",
            toolRegistration: mcpBuilder => mcpBuilder.WithTools<LocalOfficeDispatcherTool>());

        // ── Action Handlers ───────────────────────────────────────────────────
        services.AddActionHandlersFromAssembly<LocalOfficeCommandArgs>(typeof(LocalOfficeServiceExtensions).Assembly);

        return services;
    }
}
