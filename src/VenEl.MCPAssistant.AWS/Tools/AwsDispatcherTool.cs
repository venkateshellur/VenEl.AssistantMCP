using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using VenEl.MCPAssistant.Core.Dispatcher;

namespace VenEl.MCPAssistant.AWS.Tools;

[McpServerToolType]
public sealed class AwsDispatcherTool : DispatcherToolBase<AwsCommandArgs>
{
    public AwsDispatcherTool(IServiceProvider serviceProvider) 
        : base(serviceProvider, "AWS")
    {
    }

    protected override string? GetRequestedAction(AwsCommandArgs args) => args.Action;

    [McpServerTool(Name = "mcp_venel_aws_commands")]
    [Description("AWS integration tools: List S3 buckets and EC2 instances.")]
    public Task<string> DispatchAwsCommandAsync(
        [Description("The arguments for the AWS command")] AwsCommandArgs args,
        CancellationToken ct)
    {
        return DispatchAsync(args, ct);
    }
}
