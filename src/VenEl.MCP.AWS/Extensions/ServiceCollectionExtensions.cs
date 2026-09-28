using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.MCP.AWS.Configuration;
using VenEl.MCP.AWS.Tools;
using VenEl.MCP.Core.Dispatcher;
using VenEl.MCP.Core.Registration;

namespace VenEl.MCP.AWS.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAwsFeature(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<AwsOptions>(config.GetSection("AWS"));
        
        services.AddSingleton<IActionHandler<AwsCommandArgs>, AwsListS3BucketsActionHandler>();
        services.AddSingleton<IActionHandler<AwsCommandArgs>, AwsListEc2InstancesActionHandler>();

        services.GetOrAddFeatureRegistry()
            .Register("AWS", "AWS tools for S3 and EC2", mcpBuilder =>
            {
                mcpBuilder.WithTools<AwsDispatcherTool>();
            });

        return services;
    }
}
