using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace VenEl.MCP.FileManager.Cloud;

public interface ICloudStorageManager : IDisposable
{
    Task<CloudTestResult> TestConnectionAsync(CloudConnectRequest request, CancellationToken ct = default);
    Task<CloudConnectResult> ConnectAsync(CloudConnectRequest request, CancellationToken ct = default);
    Task<bool> DisconnectAsync(string connectionId);
    IReadOnlyList<CloudSessionInfo> GetActiveSessions();
    Task<CloudDirectoryResult> ListDirectoryAsync(string connectionId, string? prefix, CancellationToken ct = default);
    Task<string> ReadFileTextAsync(string connectionId, string key, CancellationToken ct = default);
    Task<byte[]> ReadFileBytesAsync(string connectionId, string key, CancellationToken ct = default);
    Task SaveFileTextAsync(string connectionId, string key, string content, CancellationToken ct = default);
    Task UploadFileAsync(string connectionId, string key, Stream contentStream, CancellationToken ct = default);
    Task CreateFolderAsync(string connectionId, string currentPrefix, string folderName, CancellationToken ct = default);
    Task DeleteAsync(string connectionId, string key, bool isDirectory, CancellationToken ct = default);
}
