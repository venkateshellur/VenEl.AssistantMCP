using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VenEl.MCP.Azure.Services;

public interface IAzureBlobService
{
    Task<IEnumerable<string>> ListContainersAsync(string connectionString, CancellationToken ct);
    Task<IEnumerable<string>> ListBlobsAsync(string connectionString, string containerName, CancellationToken ct);
    Task<string> ReadBlobTextAsync(string connectionString, string containerName, string blobName, CancellationToken ct);
    Task UploadBlobTextAsync(string connectionString, string containerName, string blobName, string content, CancellationToken ct);
}
