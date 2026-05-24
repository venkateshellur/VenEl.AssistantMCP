using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.AssistantMCP.Bitwarden.Services;
using VenEl.AssistantMCP.Bitwarden.Tools;
using VenEl.AssistantMCP.Core.Dispatcher;
using VenEl.AssistantMCP.Core.Registration;
using VenEl.AssistantMCP.Core.Security;

namespace VenEl.AssistantMCP.Bitwarden.Extensions;

public static class BitwardenFeatureExtensions
{
    public static IServiceCollection AddBitwardenFeature(this IServiceCollection services, IConfiguration configuration)
    {
        var isEnabled = configuration.GetValue<bool>("Bitwarden:IsEnabled", true);
        if (!isEnabled)
        {
            return services;
        }

        services.AddOptions<BitwardenOptions>().Bind(configuration.GetSection("Bitwarden"));

        // Register the specific services
        services.AddTransient<BitwardenSdkService>();
        services.AddTransient<BitwardenCliService>();

        // Register the Strategy Service as the primary implementation
        services.AddTransient<IBitwardenService, BitwardenStrategyService>();
        services.AddSingleton<ISecretResolver, BitwardenSecretResolver>();

        services.AddTransient<IActionHandler<BitwardenCommandArgs>, BitwardenActionHandlers>();
        
        services.GetOrAddFeatureRegistry().Register(
            featureName: "Bitwarden",
            description: "Bitwarden tools: Get secrets from the Bitwarden vault or Secrets Manager.",
            toolRegistration: mcpBuilder => mcpBuilder.WithTools<BitwardenDispatcherTool>());

        services.AddActionHandlersFromAssembly<BitwardenCommandArgs>(typeof(BitwardenFeatureExtensions).Assembly);

        return services;
    }
}

public class BitwardenOptions
{
    public bool IsEnabled { get; set; } = true;
    public string? MachineToken { get; set; }
}
