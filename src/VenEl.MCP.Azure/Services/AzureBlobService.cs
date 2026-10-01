using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

namespace VenEl.MCP.Azure.Services;

public class AzureBlobService : IAzureBlobService
{
    public async Task<IEnumerable<string>> ListContainersAsync(string connectionString, CancellationToken ct)
    {
        var serviceClient = new BlobServiceClient(connectionString);
        var containers = new List<string>();
        await foreach (var container in serviceClient.GetBlobContainersAsync(cancellationToken: ct))
        {
            containers.Add(container.Name);
        }
        return containers;
    }

    public async Task<IEnumerable<string>> ListBlobsAsync(string connectionString, string containerName, CancellationToken ct)
    {
        var containerClient = new BlobContainerClient(connectionString, containerName);
        var blobs = new List<string>();
        await foreach (var blob in containerClient.GetBlobsAsync(cancellationToken: ct))
        {
            blobs.Add(blob.Name);
        }
        return blobs;
    }

    public async Task<string> ReadBlobTextAsync(string connectionString, string containerName, string blobName, CancellationToken ct)
    {
        var containerClient = new BlobContainerClient(connectionString, containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        
        var response = await blobClient.DownloadContentAsync(ct);
        return response.Value.Content.ToString();
    }

    public async Task UploadBlobTextAsync(string connectionString, string containerName, string blobName, string content, CancellationToken ct)
    {
        var containerClient = new BlobContainerClient(connectionString, containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: ct);
        
        var blobClient = containerClient.GetBlobClient(blobName);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await blobClient.UploadAsync(stream, overwrite: true, cancellationToken: ct);
    }

}
