using Microsoft.Extensions.DependencyInjection;
using VenEl.MCP.Core.Dispatcher;
using VenEl.MCP.Core.Registration;
using VenEl.MCP.WebAutomator.Services;
using VenEl.MCP.WebAutomator.Tools;

namespace VenEl.MCP.WebAutomator.Extensions;

public static class WebAutomatorServiceExtensions
{
    public static IServiceCollection AddWebAutomator(this IServiceCollection services)
    {
        services.AddSingleton<PlaywrightBrowserManager>();
        
        services.GetOrAddFeatureRegistry().Register(
            featureName: "WebAutomator",
            description: "Playwright-based web automation tools.",
            toolRegistration: mcpBuilder => mcpBuilder.WithTools<WebAutomatorDispatcherTool>());
            
        services.AddActionHandlersFromAssembly<WebAutomatorArgs>(typeof(WebAutomatorServiceExtensions).Assembly);
        
        return services;
    }
}
