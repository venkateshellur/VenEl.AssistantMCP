using Microsoft.Extensions.DependencyInjection;
using VenEl.AssistantMCP.Core.Dispatcher;
using VenEl.AssistantMCP.Core.Registration;
using VenEl.AssistantMCP.WebAutomator.Services;
using VenEl.AssistantMCP.WebAutomator.Tools;

namespace VenEl.AssistantMCP.WebAutomator.Extensions;

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
