using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VenEl.MCPAssistant.GCP.Configuration;
using VenEl.MCPAssistant.Core.Dispatcher;

namespace VenEl.MCPAssistant.GCP.Tools;

public sealed class GcpListStorageBucketsActionHandler(
    IOptions<GcpOptions> options,
    ILogger<GcpListStorageBucketsActionHandler> logger) : IActionHandler<GcpCommandArgs>
{
    public string ActionName => "gcp_list_storage_buckets";

    public string? Validate(GcpCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ProjectId))
            return "GCP ProjectId is not configured.";
        return null;
    }

    public async Task<string> HandleAsync(GcpCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Listing GCP Storage buckets");
        try
        {
            if (!string.IsNullOrWhiteSpace(options.Value.CredentialsPath))
            {
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", options.Value.CredentialsPath);
            }

            var client = await StorageClient.CreateAsync();
            var buckets = client.ListBucketsAsync(options.Value.ProjectId);
            
            var bucketNames = new System.Collections.Generic.List<string>();
            await foreach (var b in buckets.WithCancellation(ct))
            {
                bucketNames.Add(b.Name);
            }
            if (!bucketNames.Any()) return "No storage buckets found.";
            return string.Join("\n", bucketNames);
        }
        catch (Exception ex)
        {
            return $"[ERROR] Failed to list GCP storage buckets: {ex.Message}";
        }
    }
}
