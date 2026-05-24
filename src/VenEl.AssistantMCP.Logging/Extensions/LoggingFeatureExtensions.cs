using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using VenEl.AssistantMCP.Core.Registration;
using VenEl.AssistantMCP.Logging.Configuration;
using VenEl.AssistantMCP.Logging.Providers;
using VenEl.AssistantMCP.Logging.Tools;

namespace VenEl.AssistantMCP.Logging.Extensions;

public static class LoggingFeatureExtensions
{
    public static IServiceCollection AddLoggingFeature(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind configuration
        services.Configure<FileLoggerOptions>(configuration.GetSection(FileLoggerOptions.SectionName));

        // Register custom logger provider
        services.AddSingleton<ILoggerProvider, FileLoggerProvider>();

        // Register action handlers for this feature
        services.AddActionHandlersFromAssembly<LoggingCommandArgs>(typeof(LoggingFeatureExtensions).Assembly);

        // Register the tools for the MCP server
        services.GetOrAddFeatureRegistry().Register(
            featureName: "Logging",
            description: "Server diagnostic tools: read recent server logs.",
            toolRegistration: mcpBuilder => mcpBuilder.WithTools<LoggingDispatcherTool>()
        );

        return services;
    }
}
