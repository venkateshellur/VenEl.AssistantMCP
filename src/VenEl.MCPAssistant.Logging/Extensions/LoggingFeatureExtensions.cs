using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using VenEl.MCPAssistant.Core.Registration;
using VenEl.MCPAssistant.Logging.Configuration;
using VenEl.MCPAssistant.Logging.Providers;
using VenEl.MCPAssistant.Logging.Tools;

namespace VenEl.MCPAssistant.Logging.Extensions;

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
