using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.AssistantMCP.Core.Registration;
using VenEl.AssistantMCP.Email.Configuration;
using VenEl.AssistantMCP.Email.Tools;

namespace VenEl.AssistantMCP.Email.Extensions;

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
