using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VenEl.AssistantMCP.AWS.Configuration;
using VenEl.AssistantMCP.Core.Dispatcher;
using VenEl.AssistantMCP.Core.Security;

namespace VenEl.AssistantMCP.AWS.Tools;

public sealed class AwsListS3BucketsActionHandler(
    IOptions<AwsOptions> options,
    SecretManager secretManager,
    ILogger<AwsListS3BucketsActionHandler> logger) : IActionHandler<AwsCommandArgs>
{
    public string ActionName => "aws_list_s3_buckets";

    public string? Validate(AwsCommandArgs args) => null;

    public async Task<string> HandleAsync(AwsCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Listing S3 buckets");
        try
        {
            var accessKey = await secretManager.ResolveSecretAsync(options.Value.AccessKey, ct);
            var secretKey = await secretManager.ResolveSecretAsync(options.Value.SecretKey, ct);
            var region = RegionEndpoint.GetBySystemName(options.Value.Region ?? "us-east-1");

            using var client = !string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrWhiteSpace(secretKey)
                ? new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), region)
                : new AmazonS3Client(region);

            var response = await client.ListBucketsAsync(new ListBucketsRequest(), ct);
            return string.Join("\n", response.Buckets.Select(b => b.BucketName));
        }
        catch (Exception ex)
        {
            return $"[ERROR] Failed to list S3 buckets: {ex.Message}";
        }
    }
}

public sealed class AwsListEc2InstancesActionHandler(
    IOptions<AwsOptions> options,
    SecretManager secretManager,
    ILogger<AwsListEc2InstancesActionHandler> logger) : IActionHandler<AwsCommandArgs>
{
    public string ActionName => "aws_list_ec2_instances";

    public string? Validate(AwsCommandArgs args) => null;

    public async Task<string> HandleAsync(AwsCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Listing EC2 instances");
        try
        {
            var accessKey = await secretManager.ResolveSecretAsync(options.Value.AccessKey, ct);
            var secretKey = await secretManager.ResolveSecretAsync(options.Value.SecretKey, ct);
            var region = RegionEndpoint.GetBySystemName(options.Value.Region ?? "us-east-1");

            using var client = !string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrWhiteSpace(secretKey)
                ? new AmazonEC2Client(new BasicAWSCredentials(accessKey, secretKey), region)
                : new AmazonEC2Client(region);

            var response = await client.DescribeInstancesAsync(new DescribeInstancesRequest(), ct);
            
            var instances = response.Reservations
                .SelectMany(r => r.Instances)
                .Select(i => $"{i.InstanceId} ({i.State.Name})")
                .ToList();

            if (!instances.Any()) return "No EC2 instances found.";
            return string.Join("\n", instances);
        }
        catch (Exception ex)
        {
            return $"[ERROR] Failed to list EC2 instances: {ex.Message}";
        }
    }
}
