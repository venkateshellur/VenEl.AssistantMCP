using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenEl.MCP.Azure.Services;
using VenEl.MCP.Core.Dispatcher;

namespace VenEl.MCP.Azure.Tools;

public sealed class AzureListBlobContainersActionHandler(IAzureBlobService blobService, ILogger<AzureListBlobContainersActionHandler> logger) : IActionHandler<AzureCommandArgs>
{
    public string ActionName => "azure_list_blob_containers";

    public string? Validate(AzureCommandArgs args)
    {
        if (string.IsNullOrEmpty(args.ConnectionString)) return "Missing required parameter 'ConnectionString'.";
        return null;
    }

    public async Task<string> HandleAsync(AzureCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Listing blob containers");
        var containers = await blobService.ListContainersAsync(args.ConnectionString!, ct);
        return JsonSerializer.Serialize(containers, new JsonSerializerOptions { WriteIndented = true });
    }
}

public sealed class AzureListBlobsActionHandler(IAzureBlobService blobService, ILogger<AzureListBlobsActionHandler> logger) : IActionHandler<AzureCommandArgs>
{
    public string ActionName => "azure_list_blobs";

    public string? Validate(AzureCommandArgs args)
    {
        if (string.IsNullOrEmpty(args.ConnectionString)) return "Missing required parameter 'ConnectionString'.";
        if (string.IsNullOrEmpty(args.ContainerName)) return "Missing required parameter 'ContainerName'.";
        return null;
    }

    public async Task<string> HandleAsync(AzureCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Listing blobs in container {ContainerName}", args.ContainerName);
        var blobs = await blobService.ListBlobsAsync(args.ConnectionString!, args.ContainerName!, ct);
        return JsonSerializer.Serialize(blobs, new JsonSerializerOptions { WriteIndented = true });
    }
}

public sealed class AzureReadBlobActionHandler(IAzureBlobService blobService, ILogger<AzureReadBlobActionHandler> logger) : IActionHandler<AzureCommandArgs>
{
    public string ActionName => "azure_read_blob";

    public string? Validate(AzureCommandArgs args)
    {
        if (string.IsNullOrEmpty(args.ConnectionString)) return "Missing required parameter 'ConnectionString'.";
        if (string.IsNullOrEmpty(args.ContainerName)) return "Missing required parameter 'ContainerName'.";
        if (string.IsNullOrEmpty(args.BlobName)) return "Missing required parameter 'BlobName'.";
        return null;
    }

    public async Task<string> HandleAsync(AzureCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Reading blob {BlobName} in container {ContainerName}", args.BlobName, args.ContainerName);
        return await blobService.ReadBlobTextAsync(args.ConnectionString!, args.ContainerName!, args.BlobName!, ct);
    }
}

public sealed class AzureUploadBlobActionHandler(IAzureBlobService blobService, ILogger<AzureUploadBlobActionHandler> logger) : IActionHandler<AzureCommandArgs>
{
    public string ActionName => "azure_upload_blob";

    public string? Validate(AzureCommandArgs args)
    {
        if (string.IsNullOrEmpty(args.ConnectionString)) return "Missing required parameter 'ConnectionString'.";
        if (string.IsNullOrEmpty(args.ContainerName)) return "Missing required parameter 'ContainerName'.";
        if (string.IsNullOrEmpty(args.BlobName)) return "Missing required parameter 'BlobName'.";
        if (args.Content == null) return "Missing required parameter 'Content'.";
        return null;
    }

    public async Task<string> HandleAsync(AzureCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Uploading blob {BlobName} to container {ContainerName}", args.BlobName, args.ContainerName);
        await blobService.UploadBlobTextAsync(args.ConnectionString!, args.ContainerName!, args.BlobName!, args.Content!, ct);
        return $"Successfully uploaded blob '{args.BlobName}' to container '{args.ContainerName}'.";
    }
}
