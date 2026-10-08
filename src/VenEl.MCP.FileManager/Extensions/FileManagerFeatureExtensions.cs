using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.MCP.Core.Registration;
using VenEl.MCP.FileManager.Configuration;
using VenEl.MCP.FileManager.Tools;
using VenEl.MCP.FileManager.Web;
using System.Threading.Tasks;

namespace VenEl.MCP.FileManager.Extensions;

public static class FileManagerFeatureExtensions
{
    public static IServiceCollection AddFileManagerFeature(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind Configuration
        services.Configure<FileManagerOptions>(configuration.GetSection(FileManagerOptions.SectionName));

        var options = configuration.GetSection(FileManagerOptions.SectionName).Get<FileManagerOptions>() ?? new FileManagerOptions();

        if (options.Enabled)
        {
            services.AddActionHandlersFromAssembly<FileManagerCommandArgs>(typeof(FileManagerFeatureExtensions).Assembly);

            services.GetOrAddFeatureRegistry().Register(
                featureName: "FileManager",
                description: "Manage local files and directories (CRUD) via MCP.",
                toolRegistration: mcpBuilder => mcpBuilder.WithTools<FileManagerDispatcherTool>()
            );

            // Start Web UI asynchronously in background
            _ = Task.Run(() => FileManagerWebApp.StartAsync(options));
        }

        return services;
    }
}
