using Microsoft.Extensions.DependencyInjection;
using VenEl.MCPAssistant.Core.Registration;
using VenEl.MCPAssistant.LocalOffice.Tools;

namespace VenEl.MCPAssistant.LocalOffice.Extensions;

public static class LocalOfficeServiceExtensions
{
    public static IServiceCollection AddLocalOfficeTools(this IServiceCollection services)
    {
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
