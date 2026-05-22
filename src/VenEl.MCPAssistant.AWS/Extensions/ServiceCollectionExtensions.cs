using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.MCPAssistant.AWS.Configuration;
using VenEl.MCPAssistant.AWS.Tools;
using VenEl.MCPAssistant.Core.Dispatcher;
using VenEl.MCPAssistant.Core.Registration;

namespace VenEl.MCPAssistant.AWS.Extensions;

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
