using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace VenEl.MCP.FileManager.Sftp;

public interface ISftpSessionManager : IDisposable
{
    Task<SftpTestResult> TestConnectionAsync(SftpConnectRequest request, CancellationToken ct = default);
    Task<SftpConnectResult> ConnectAsync(SftpConnectRequest request, CancellationToken ct = default);
    Task<bool> DisconnectAsync(string connectionId);
    IReadOnlyList<SftpSessionInfo> GetActiveSessions();
    Task<SftpDirectoryResult> ListDirectoryAsync(string connectionId, string? path, CancellationToken ct = default);
    Task<string> ReadFileTextAsync(string connectionId, string remotePath, CancellationToken ct = default);
    Task<byte[]> ReadFileBytesAsync(string connectionId, string remotePath, CancellationToken ct = default);
    Task SaveFileTextAsync(string connectionId, string remotePath, string content, CancellationToken ct = default);
    Task UploadFileAsync(string connectionId, string remotePath, Stream contentStream, CancellationToken ct = default);
    Task CreateDirectoryAsync(string connectionId, string currentPath, string folderName, CancellationToken ct = default);
    Task CreateFileAsync(string connectionId, string currentPath, string fileName, string? content, CancellationToken ct = default);
    Task RenameAsync(string connectionId, string oldPath, string newName, CancellationToken ct = default);
    Task DeleteAsync(string connectionId, string targetPath, bool isDirectory, CancellationToken ct = default);
}
