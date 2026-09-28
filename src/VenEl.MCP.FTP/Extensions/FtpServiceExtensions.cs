using Microsoft.Extensions.DependencyInjection;
using VenEl.MCP.Core.Registration;
using VenEl.MCP.FTP.Services;
using VenEl.MCP.FTP.Tools;

namespace VenEl.MCP.FTP.Extensions;

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
