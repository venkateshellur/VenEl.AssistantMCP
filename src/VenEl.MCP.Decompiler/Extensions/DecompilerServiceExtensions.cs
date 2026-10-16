using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.MCP.Core.Registration;
using VenEl.MCP.Core.Dispatcher;
using VenEl.MCP.Decompiler.Configuration;
using VenEl.MCP.Decompiler.Services;
using VenEl.MCP.Decompiler.Tools;

namespace VenEl.MCP.Decompiler.Extensions;

public static class DecompilerServiceExtensions
{
    public static IServiceCollection AddDecompilerFeature(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DecompilerOptions>(configuration.GetSection(DecompilerOptions.SectionName));

        var options = configuration.GetSection(DecompilerOptions.SectionName).Get<DecompilerOptions>() ?? new DecompilerOptions();

        // ── Core Services ─────────────────────────────────────────────────────
        services.AddSingleton<IDecompilerService, DecompilerService>();

        if (options.Enabled)
        {
            // ── Self-register MCP tools into the shared registry ──────────────────
            services.GetOrAddFeatureRegistry().Register(
                featureName: "Decompiler",
                description: "Decompiler & Binary Inspection tools: decompile and inspect .NET assemblies (.dll, .exe) and Java binaries (.class, .jar).",
                toolRegistration: mcpBuilder => mcpBuilder.WithTools<DecompilerDispatcherTool>());

            // ── Action Handlers ───────────────────────────────────────────────────
            services.AddActionHandlersFromAssembly<DecompilerCommandArgs>(typeof(DecompilerServiceExtensions).Assembly);
        }

        return services;
    }
}
