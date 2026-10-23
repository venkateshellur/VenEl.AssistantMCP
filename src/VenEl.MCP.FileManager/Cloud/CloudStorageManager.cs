using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace VenEl.MCP.FileManager.Cloud;

internal interface ICloudSession : IDisposable
{
    CloudSessionInfo Info { get; }
    Task<CloudDirectoryResult> ListDirectoryAsync(string? prefix, CancellationToken ct);
    Task<string> ReadFileTextAsync(string key, CancellationToken ct);
    Task<byte[]> ReadFileBytesAsync(string key, CancellationToken ct);
    Task SaveFileTextAsync(string key, string content, CancellationToken ct);
    Task UploadFileAsync(string key, Stream contentStream, CancellationToken ct);
    Task CreateFolderAsync(string currentPrefix, string folderName, CancellationToken ct);
    Task DeleteAsync(string key, bool isDirectory, CancellationToken ct);
}

public class CloudStorageManager : ICloudStorageManager
{
    private readonly ConcurrentDictionary<string, ICloudSession> _sessions = new();

    public async Task<CloudTestResult> TestConnectionAsync(CloudConnectRequest request, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            if (request.Provider.Equals("azure_blob", StringComparison.OrdinalIgnoreCase) || request.Provider.Equals("azure", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(request.ConnectionString))
                    return new CloudTestResult(false, "Azure Connection String is required.");

                var serviceClient = new BlobServiceClient(request.ConnectionString);
                var count = 0;
                await foreach (var _ in serviceClient.GetBlobContainersAsync(cancellationToken: ct))
                {
                    count++;
                    if (count >= 5) break;
                }
                sw.Stop();
                return new CloudTestResult(true, null, (int)sw.ElapsedMilliseconds, count);
            }
            else // S3 / MinIO / R2
            {
                using var client = CreateS3Client(request);
                if (!string.IsNullOrWhiteSpace(request.BucketName))
                {
                    var response = await client.ListObjectsV2Async(new ListObjectsV2Request
                    {
                        BucketName = request.BucketName,
                        MaxKeys = 5
                    }, ct);
                    sw.Stop();
                    return new CloudTestResult(true, null, (int)sw.ElapsedMilliseconds, response.KeyCount);
                }
                else
                {
                    var response = await client.ListBucketsAsync(ct);
                    sw.Stop();
                    return new CloudTestResult(true, null, (int)sw.ElapsedMilliseconds, response.Buckets.Count);
                }
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new CloudTestResult(false, ex.Message, (int)sw.ElapsedMilliseconds);
        }
    }

    public async Task<CloudConnectResult> ConnectAsync(CloudConnectRequest request, CancellationToken ct = default)
    {
        try
        {
            var connectionId = "cloud_" + Guid.NewGuid().ToString("N")[..8];
            ICloudSession session;

            if (request.Provider.Equals("azure_blob", StringComparison.OrdinalIgnoreCase) || request.Provider.Equals("azure", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(request.ConnectionString))
                    return new CloudConnectResult(false, null, "Azure Connection String is required.", null, null, null, "azure_blob");

                var displayName = !string.IsNullOrWhiteSpace(request.Name)
                    ? request.Name
                    : (!string.IsNullOrWhiteSpace(request.BucketName) ? $"Azure: {request.BucketName}" : "Azure Storage");

                session = new AzureBlobCloudSession(connectionId, displayName, request);
            }
            else // S3 / MinIO
            {
                var displayName = !string.IsNullOrWhiteSpace(request.Name)
                    ? request.Name
                    : (!string.IsNullOrWhiteSpace(request.BucketName) ? $"S3: {request.BucketName}" : (!string.IsNullOrWhiteSpace(request.Endpoint) ? $"MinIO: {request.Endpoint}" : "AWS S3"));

                session = new S3CloudSession(connectionId, displayName, request);
            }

            _sessions[connectionId] = session;

            var initialList = await session.ListDirectoryAsync(request.InitialPrefix, ct);

            return new CloudConnectResult(
                Success: true,
                ConnectionId: connectionId,
                ErrorMessage: null,
                CurrentPath: initialList.CurrentPrefix,
                Items: initialList.Items,
                Name: session.Info.Name,
                Provider: session.Info.Provider
            );
        }
        catch (Exception ex)
        {
            return new CloudConnectResult(false, null, ex.Message, null, null, null, request.Provider);
        }
    }

    public Task<bool> DisconnectAsync(string connectionId)
    {
        if (_sessions.TryRemove(connectionId, out var session))
        {
            session.Dispose();
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public IReadOnlyList<CloudSessionInfo> GetActiveSessions()
    {
        return _sessions.Values.Select(s => s.Info).ToList();
    }

    public Task<CloudDirectoryResult> ListDirectoryAsync(string connectionId, string? prefix, CancellationToken ct = default)
    {
        if (!_sessions.TryGetValue(connectionId, out var session))
            throw new KeyNotFoundException($"Cloud session '{connectionId}' not found.");

        return session.ListDirectoryAsync(prefix, ct);
    }

    public Task<string> ReadFileTextAsync(string connectionId, string key, CancellationToken ct = default)
    {
        if (!_sessions.TryGetValue(connectionId, out var session))
            throw new KeyNotFoundException($"Cloud session '{connectionId}' not found.");

        return session.ReadFileTextAsync(key, ct);
    }

    public Task<byte[]> ReadFileBytesAsync(string connectionId, string key, CancellationToken ct = default)
    {
        if (!_sessions.TryGetValue(connectionId, out var session))
            throw new KeyNotFoundException($"Cloud session '{connectionId}' not found.");

        return session.ReadFileBytesAsync(key, ct);
    }

    public Task SaveFileTextAsync(string connectionId, string key, string content, CancellationToken ct = default)
    {
        if (!_sessions.TryGetValue(connectionId, out var session))
            throw new KeyNotFoundException($"Cloud session '{connectionId}' not found.");

        return session.SaveFileTextAsync(key, content, ct);
    }

    public Task UploadFileAsync(string connectionId, string key, Stream contentStream, CancellationToken ct = default)
    {
        if (!_sessions.TryGetValue(connectionId, out var session))
            throw new KeyNotFoundException($"Cloud session '{connectionId}' not found.");

        return session.UploadFileAsync(key, contentStream, ct);
    }

    public Task CreateFolderAsync(string connectionId, string currentPrefix, string folderName, CancellationToken ct = default)
    {
        if (!_sessions.TryGetValue(connectionId, out var session))
            throw new KeyNotFoundException($"Cloud session '{connectionId}' not found.");

        return session.CreateFolderAsync(currentPrefix, folderName, ct);
    }

    public Task DeleteAsync(string connectionId, string key, bool isDirectory, CancellationToken ct = default)
    {
        if (!_sessions.TryGetValue(connectionId, out var session))
            throw new KeyNotFoundException($"Cloud session '{connectionId}' not found.");

        return session.DeleteAsync(key, isDirectory, ct);
    }

    public void Dispose()
    {
        foreach (var session in _sessions.Values)
        {
            try { session.Dispose(); } catch {}
        }
        _sessions.Clear();
    }

    internal static IAmazonS3 CreateS3Client(CloudConnectRequest request)
    {
        var config = new AmazonS3Config();
        if (!string.IsNullOrWhiteSpace(request.Endpoint))
        {
            config.ServiceURL = request.Endpoint;
            config.ForcePathStyle = request.ForcePathStyle || true;
        }
        else if (!string.IsNullOrWhiteSpace(request.Region))
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(request.Region);
        }
        else
        {
            config.RegionEndpoint = RegionEndpoint.USEast1;
        }

        if (!string.IsNullOrWhiteSpace(request.AccessKey) && !string.IsNullOrWhiteSpace(request.SecretKey))
        {
            var creds = new BasicAWSCredentials(request.AccessKey, request.SecretKey);
            return new AmazonS3Client(creds, config);
        }

        return new AmazonS3Client(config);
    }
}

internal class S3CloudSession : ICloudSession
{
    private readonly IAmazonS3 _client;
    private readonly string _bucketName;

    public CloudSessionInfo Info { get; }

    public S3CloudSession(string connectionId, string name, CloudConnectRequest req)
    {
        _client = CloudStorageManager.CreateS3Client(req);
        _bucketName = req.BucketName?.Trim() ?? "";

        Info = new CloudSessionInfo(
            ConnectionId: connectionId,
            Name: name,
            Provider: "s3",
            BucketOrContainer: _bucketName,
            Endpoint: req.Endpoint,
            Region: req.Region,
            ConnectedAt: DateTime.UtcNow
        );
    }

    public async Task<CloudDirectoryResult> ListDirectoryAsync(string? prefix, CancellationToken ct)
    {
        prefix = (prefix ?? "").TrimStart('/');
        if (!string.IsNullOrEmpty(prefix) && !prefix.EndsWith('/'))
        {
            prefix += "/";
        }

        var items = new List<CloudFileItem>();

        // If no bucket specified, list buckets as top-level directories
        if (string.IsNullOrWhiteSpace(_bucketName))
        {
            var bucketResponse = await _client.ListBucketsAsync(ct);
            foreach (var b in bucketResponse.Buckets)
            {
                items.Add(new CloudFileItem(
                    Name: b.BucketName,
                    Type: "Directory",
                    Size: 0,
                    Modified: b.CreationDate?.ToString("o") ?? DateTime.UtcNow.ToString("o"),
                    IsCloud: true,
                    Provider: "s3",
                    Key: b.BucketName + "/"
                ));
            }
            return new CloudDirectoryResult(Info.ConnectionId, "s3", "", prefix, items);
        }

        var request = new ListObjectsV2Request
        {
            BucketName = _bucketName,
            Prefix = prefix,
            Delimiter = "/"
        };

        var response = await _client.ListObjectsV2Async(request, ct);

        // 1. Add directories (CommonPrefixes)
        foreach (var commonPrefix in response.CommonPrefixes)
        {
            var folderName = commonPrefix;
            if (folderName.StartsWith(prefix))
            {
                folderName = folderName[prefix.Length..];
            }
            folderName = folderName.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(folderName)) continue;

            items.Add(new CloudFileItem(
                Name: folderName,
                Type: "Directory",
                Size: 0,
                Modified: DateTime.UtcNow.ToString("o"),
                IsCloud: true,
                Provider: "s3",
                Key: commonPrefix
            ));
        }

        // 2. Add files
        foreach (var obj in response.S3Objects)
        {
            if (obj.Key == prefix || obj.Key.EndsWith('/')) continue; // Skip directory marker object

            var fileName = obj.Key;
            if (fileName.StartsWith(prefix))
            {
                fileName = fileName[prefix.Length..];
            }
            if (fileName.Contains('/')) continue; // Belongs to subfolder

            items.Add(new CloudFileItem(
                Name: fileName,
                Type: "File",
                Size: obj.Size ?? 0L,
                Modified: obj.LastModified?.ToString("o") ?? DateTime.UtcNow.ToString("o"),
                IsCloud: true,
                Provider: "s3",
                Key: obj.Key,
                StorageClass: obj.StorageClass?.Value,
                ETag: obj.ETag?.Trim('"')
            ));
        }

        return new CloudDirectoryResult(Info.ConnectionId, "s3", _bucketName, prefix, items);
    }

    public async Task<string> ReadFileTextAsync(string key, CancellationToken ct)
    {
        var bucket = ResolveBucket(ref key);
        using var response = await _client.GetObjectAsync(bucket, key, ct);
        using var reader = new StreamReader(response.ResponseStream, Encoding.UTF8);
        return await reader.ReadToEndAsync(ct);
    }

    public async Task<byte[]> ReadFileBytesAsync(string key, CancellationToken ct)
    {
        var bucket = ResolveBucket(ref key);
        using var response = await _client.GetObjectAsync(bucket, key, ct);
        using var ms = new MemoryStream();
        await response.ResponseStream.CopyToAsync(ms, ct);
        return ms.ToArray();
    }

    public async Task SaveFileTextAsync(string key, string content, CancellationToken ct)
    {
        var bucket = ResolveBucket(ref key);
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = ms
        }, ct);
    }

    public async Task UploadFileAsync(string key, Stream contentStream, CancellationToken ct)
    {
        var bucket = ResolveBucket(ref key);
        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = contentStream
        }, ct);
    }

    public async Task CreateFolderAsync(string currentPrefix, string folderName, CancellationToken ct)
    {
        currentPrefix = (currentPrefix ?? "").TrimStart('/');
        if (!string.IsNullOrEmpty(currentPrefix) && !currentPrefix.EndsWith('/'))
        {
            currentPrefix += "/";
        }
        var folderKey = $"{currentPrefix}{folderName.Trim().TrimEnd('/')}/";
        var bucket = ResolveBucket(ref folderKey);

        using var emptyStream = new MemoryStream(Array.Empty<byte>());
        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket,
            Key = folderKey,
            InputStream = emptyStream
        }, ct);
    }

    public async Task DeleteAsync(string key, bool isDirectory, CancellationToken ct)
    {
        var bucket = ResolveBucket(ref key);
        if (isDirectory)
        {
            var prefix = key.EndsWith('/') ? key : key + "/";
            string? continuationToken = null;
            do
            {
                var listResponse = await _client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = bucket,
                    Prefix = prefix,
                    ContinuationToken = continuationToken
                }, ct);

                if (listResponse.S3Objects.Count > 0)
                {
                    var deleteRequest = new DeleteObjectsRequest
                    {
                        BucketName = bucket,
                        Objects = listResponse.S3Objects.Select(o => new KeyVersion { Key = o.Key }).ToList()
                    };
                    await _client.DeleteObjectsAsync(deleteRequest, ct);
                }

                continuationToken = listResponse.NextContinuationToken;
            } while (!string.IsNullOrEmpty(continuationToken));
        }
        else
        {
            await _client.DeleteObjectAsync(bucket, key, ct);
        }
    }

    private string ResolveBucket(ref string key)
    {
        key = key.TrimStart('/');
        if (!string.IsNullOrWhiteSpace(_bucketName)) return _bucketName;

        var slashIdx = key.IndexOf('/');
        if (slashIdx > 0)
        {
            var b = key[..slashIdx];
            key = key[(slashIdx + 1)..];
            return b;
        }
        throw new InvalidOperationException($"Cannot determine S3 bucket from key: '{key}'.");
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}

internal class AzureBlobCloudSession : ICloudSession
{
    private readonly BlobServiceClient _serviceClient;
    private readonly string _containerName;

    public CloudSessionInfo Info { get; }

    public AzureBlobCloudSession(string connectionId, string name, CloudConnectRequest req)
    {
        _serviceClient = new BlobServiceClient(req.ConnectionString);
        _containerName = req.BucketName?.Trim() ?? "";

        Info = new CloudSessionInfo(
            ConnectionId: connectionId,
            Name: name,
            Provider: "azure_blob",
            BucketOrContainer: _containerName,
            Endpoint: null,
            Region: null,
            ConnectedAt: DateTime.UtcNow
        );
    }

    public async Task<CloudDirectoryResult> ListDirectoryAsync(string? prefix, CancellationToken ct)
    {
        prefix = (prefix ?? "").TrimStart('/');
        if (!string.IsNullOrEmpty(prefix) && !prefix.EndsWith('/'))
        {
            prefix += "/";
        }

        var items = new List<CloudFileItem>();

        // If no container specified, list containers as top-level folders
        if (string.IsNullOrWhiteSpace(_containerName))
        {
            await foreach (var container in _serviceClient.GetBlobContainersAsync(cancellationToken: ct))
            {
                items.Add(new CloudFileItem(
                    Name: container.Name,
                    Type: "Directory",
                    Size: 0,
                    Modified: container.Properties.LastModified.ToString("o"),
                    IsCloud: true,
                    Provider: "azure_blob",
                    Key: container.Name + "/"
                ));
            }
            return new CloudDirectoryResult(Info.ConnectionId, "azure_blob", "", prefix, items);
        }

        var containerClient = _serviceClient.GetBlobContainerClient(_containerName);
        await foreach (var item in containerClient.GetBlobsByHierarchyAsync(BlobTraits.None, BlobStates.None, "/", prefix, ct))
        {
            if (item.IsPrefix)
            {
                var folderName = item.Prefix;
                if (folderName.StartsWith(prefix))
                {
                    folderName = folderName[prefix.Length..];
                }
                folderName = folderName.TrimEnd('/');
                if (string.IsNullOrWhiteSpace(folderName)) continue;

                items.Add(new CloudFileItem(
                    Name: folderName,
                    Type: "Directory",
                    Size: 0,
                    Modified: DateTime.UtcNow.ToString("o"),
                    IsCloud: true,
                    Provider: "azure_blob",
                    Key: item.Prefix
                ));
            }
            else if (item.IsBlob)
            {
                var blob = item.Blob;
                if (blob.Name == prefix || blob.Name.EndsWith('/')) continue;

                var fileName = blob.Name;
                if (fileName.StartsWith(prefix))
                {
                    fileName = fileName[prefix.Length..];
                }

                items.Add(new CloudFileItem(
                    Name: fileName,
                    Type: "File",
                    Size: blob.Properties.ContentLength ?? 0,
                    Modified: blob.Properties.LastModified?.ToString("o") ?? DateTime.UtcNow.ToString("o"),
                    IsCloud: true,
                    Provider: "azure_blob",
                    Key: blob.Name,
                    StorageClass: blob.Properties.AccessTier?.ToString(),
                    ETag: blob.Properties.ETag?.ToString().Trim('"')
                ));
            }
        }

        return new CloudDirectoryResult(Info.ConnectionId, "azure_blob", _containerName, prefix, items);
    }

    public async Task<string> ReadFileTextAsync(string key, CancellationToken ct)
    {
        var (container, blobKey) = ResolveContainer(key);
        var blobClient = _serviceClient.GetBlobContainerClient(container).GetBlobClient(blobKey);
        var content = await blobClient.DownloadContentAsync(ct);
        return content.Value.Content.ToString();
    }

    public async Task<byte[]> ReadFileBytesAsync(string key, CancellationToken ct)
    {
        var (container, blobKey) = ResolveContainer(key);
        var blobClient = _serviceClient.GetBlobContainerClient(container).GetBlobClient(blobKey);
        using var ms = new MemoryStream();
        await blobClient.DownloadToAsync(ms, ct);
        return ms.ToArray();
    }

    public async Task SaveFileTextAsync(string key, string content, CancellationToken ct)
    {
        var (container, blobKey) = ResolveContainer(key);
        var blobClient = _serviceClient.GetBlobContainerClient(container).GetBlobClient(blobKey);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await blobClient.UploadAsync(stream, overwrite: true, cancellationToken: ct);
    }

    public async Task UploadFileAsync(string key, Stream contentStream, CancellationToken ct)
    {
        var (container, blobKey) = ResolveContainer(key);
        var blobClient = _serviceClient.GetBlobContainerClient(container).GetBlobClient(blobKey);
        await blobClient.UploadAsync(contentStream, overwrite: true, cancellationToken: ct);
    }

    public async Task CreateFolderAsync(string currentPrefix, string folderName, CancellationToken ct)
    {
        currentPrefix = (currentPrefix ?? "").TrimStart('/');
        if (!string.IsNullOrEmpty(currentPrefix) && !currentPrefix.EndsWith('/'))
        {
            currentPrefix += "/";
        }
        var folderKey = $"{currentPrefix}{folderName.Trim().TrimEnd('/')}/";
        var (container, blobKey) = ResolveContainer(folderKey);
        var blobClient = _serviceClient.GetBlobContainerClient(container).GetBlobClient(blobKey);
        using var emptyStream = new MemoryStream(Array.Empty<byte>());
        await blobClient.UploadAsync(emptyStream, overwrite: true, cancellationToken: ct);
    }

    public async Task DeleteAsync(string key, bool isDirectory, CancellationToken ct)
    {
        var (container, blobKey) = ResolveContainer(key);
        var containerClient = _serviceClient.GetBlobContainerClient(container);
        if (isDirectory)
        {
            var prefix = blobKey.EndsWith('/') ? blobKey : blobKey + "/";
            await foreach (var blob in containerClient.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix, ct))
            {
                await containerClient.DeleteBlobIfExistsAsync(blob.Name, cancellationToken: ct);
            }
        }
        else
        {
            await containerClient.DeleteBlobIfExistsAsync(blobKey, cancellationToken: ct);
        }
    }

    private (string container, string blobKey) ResolveContainer(string key)
    {
        key = key.TrimStart('/');
        if (!string.IsNullOrWhiteSpace(_containerName)) return (_containerName, key);

        var slashIdx = key.IndexOf('/');
        if (slashIdx > 0)
        {
            return (key[..slashIdx], key[(slashIdx + 1)..]);
        }
        throw new InvalidOperationException($"Cannot determine Azure container from key: '{key}'.");
    }

    public void Dispose() {}
}
