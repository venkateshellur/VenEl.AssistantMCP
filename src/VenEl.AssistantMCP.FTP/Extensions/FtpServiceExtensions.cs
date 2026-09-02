using Microsoft.Extensions.DependencyInjection;
using VenEl.AssistantMCP.Core.Registration;
using VenEl.AssistantMCP.FTP.Services;
using VenEl.AssistantMCP.FTP.Tools;

namespace VenEl.AssistantMCP.FTP.Extensions;

public static class FtpServiceExtensions
{
    public static IServiceCollection AddFtpFeature(this IServiceCollection services)
    {
        services.AddSingleton<FtpServiceFactory>();
        
        services.GetOrAddFeatureRegistry().Register(
            featureName: "FTP",
            description: "FTP/FTPS/SFTP file and directory management tools.",
            toolRegistration: builder =>
            {
                builder.WithTools<FtpDispatcherTool>();
            });
            
        services.AddActionHandlersFromAssembly<FtpCommandArgs>(typeof(FtpServiceExtensions).Assembly);
        
        return services;
    }
}
