using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.MCP.Core.Registration;
using VenEl.MCP.Email.Configuration;
using VenEl.MCP.Email.Tools;

namespace VenEl.MCP.Email.Extensions;

public static class EmailFeatureExtensions
{
    public static IServiceCollection AddEmailFeature(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind configuration
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

        // Register action handlers for this feature
        services.AddActionHandlersFromAssembly<EmailCommandArgs>(typeof(EmailFeatureExtensions).Assembly);

        // Register the tools for the MCP server
        services.GetOrAddFeatureRegistry().Register(
            featureName: "Email",
            description: "Email automation tools: send emails via SMTP.",
            toolRegistration: mcpBuilder => mcpBuilder.WithTools<EmailDispatcherTool>()
        );

        return services;
    }
}
