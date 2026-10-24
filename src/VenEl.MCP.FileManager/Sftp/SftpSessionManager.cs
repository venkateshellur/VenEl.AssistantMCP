using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;

namespace VenEl.MCP.FileManager.Sftp;

public sealed class SftpClientSession : IDisposable
{
    public required string ConnectionId { get; init; }
    public required string Name { get; init; }
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string Username { get; init; }
    public required string InitialPath { get; init; }
    public required DateTime ConnectedAt { get; init; }
    public required SftpClient Client { get; init; }

    public void Dispose()
    {
        try
        {
            if (Client.IsConnected)
            {
                Client.Disconnect();
            }
            Client.Dispose();
        }
        catch
        {
            // Suppress cleanup exceptions on disconnect
        }
    }
}

public class SftpSessionManager : ISftpSessionManager
{
    private readonly ConcurrentDictionary<string, SftpClientSession> _sessions = new(StringComparer.OrdinalIgnoreCase);

    public Task<SftpTestResult> TestConnectionAsync(SftpConnectRequest request, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            try
            {
                var connInfo = CreateConnectionInfo(request);
                using var client = new SftpClient(connInfo);
                
                var sw = Stopwatch.StartNew();
                client.Connect();
                sw.Stop();

                var version = client.ConnectionInfo.ServerVersion;
                client.Disconnect();

                return new SftpTestResult(
                    Success: true,
                    ErrorMessage: null,
                    LatencyMs: (int)sw.ElapsedMilliseconds,
                    ServerVersion: version
                );
            }
            catch (Exception ex)
            {
                return new SftpTestResult(
                    Success: false,
                    ErrorMessage: ex.Message,
                    LatencyMs: null,
                    ServerVersion: null
                );
            }
        }, ct);
    }

    public Task<SftpConnectResult> ConnectAsync(SftpConnectRequest request, CancellationToken ct = default)
    {
        return Task.Run(async () =>
        {
            try
            {
                var connInfo = CreateConnectionInfo(request);
                var client = new SftpClient(connInfo)
                {
                    KeepAliveInterval = TimeSpan.FromSeconds(15),
                    OperationTimeout = TimeSpan.FromMinutes(2)
                };

                client.ErrorOccurred += (sender, e) =>
                {
                    // Suppress unhandled SSH.NET background task exceptions from terminating host process
                };

                client.Connect();

                var connectionId = $"sftp_{Guid.NewGuid():N}";
                var resolvedName = !string.IsNullOrWhiteSpace(request.ConnectionName)
                    ? request.ConnectionName.Trim()
                    : $"{request.Username}@{request.Host}";

                var initialPath = !string.IsNullOrWhiteSpace(request.InitialPath)
                    ? NormalizeUnixPath(request.InitialPath)
                    : NormalizeUnixPath(client.WorkingDirectory);

                var session = new SftpClientSession
                {
                    ConnectionId = connectionId,
                    Name = resolvedName,
                    Host = request.Host,
                    Port = request.Port,
                    Username = request.Username,
                    InitialPath = initialPath,
                    ConnectedAt = DateTime.UtcNow,
                    Client = client
                };

                _sessions[connectionId] = session;

                // Load initial directory contents
                var dirResult = await ListDirectoryInternalAsync(session, initialPath, ct);

                return new SftpConnectResult(
                    Success: true,
                    ConnectionId: connectionId,
                    ErrorMessage: null,
                    CurrentPath: dirResult.CurrentPath,
                    Items: dirResult.Items,
                    Name: resolvedName
                );
            }
            catch (Exception ex)
            {
                return new SftpConnectResult(
                    Success: false,
                    ConnectionId: null,
                    ErrorMessage: ex.Message,
                    CurrentPath: null,
                    Items: null,
                    Name: null
                );
            }
        }, ct);
    }

    public Task<bool> DisconnectAsync(string connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId)) return Task.FromResult(false);

        if (_sessions.TryRemove(connectionId, out var session))
        {
            session.Dispose();
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public IReadOnlyList<SftpSessionInfo> GetActiveSessions()
    {
        return _sessions.Values
            .Where(s => s.Client.IsConnected)
            .Select(s => new SftpSessionInfo(
                s.ConnectionId,
                s.Name,
                s.Host,
                s.Port,
                s.Username,
                s.InitialPath,
                s.ConnectedAt
            ))
            .ToList();
    }

    public Task<SftpDirectoryResult> ListDirectoryAsync(string connectionId, string? path, CancellationToken ct = default)
    {
        var session = GetRequiredSession(connectionId);
        var targetPath = string.IsNullOrWhiteSpace(path) || path == "."
            ? session.InitialPath
            : NormalizeUnixPath(path);

        return ListDirectoryInternalAsync(session, targetPath, ct);
    }

    public Task<string> ReadFileTextAsync(string connectionId, string remotePath, CancellationToken ct = default)
    {
        var session = GetRequiredSession(connectionId);
        var normalized = NormalizeUnixPath(remotePath);

        return Task.Run(() =>
        {
            lock (session.Client)
            {
                using var ms = new MemoryStream();
                session.Client.DownloadFile(normalized, ms);
                ms.Position = 0;
                using var reader = new StreamReader(ms, Encoding.UTF8);
                return reader.ReadToEnd();
            }
        }, ct);
    }

    public Task<byte[]> ReadFileBytesAsync(string connectionId, string remotePath, CancellationToken ct = default)
    {
        var session = GetRequiredSession(connectionId);
        var normalized = NormalizeUnixPath(remotePath);

        return Task.Run(() =>
        {
            lock (session.Client)
            {
                using var ms = new MemoryStream();
                session.Client.DownloadFile(normalized, ms);
                return ms.ToArray();
            }
        }, ct);
    }

    public Task SaveFileTextAsync(string connectionId, string remotePath, string content, CancellationToken ct = default)
    {
        var session = GetRequiredSession(connectionId);
        var normalized = NormalizeUnixPath(remotePath);

        return Task.Run(() =>
        {
            lock (session.Client)
            {
                var bytes = Encoding.UTF8.GetBytes(content);
                using var ms = new MemoryStream(bytes);
                session.Client.UploadFile(ms, normalized, canOverride: true);
            }
        }, ct);
    }

    public Task UploadFileAsync(string connectionId, string remotePath, Stream contentStream, CancellationToken ct = default)
    {
        var session = GetRequiredSession(connectionId);
        var normalized = NormalizeUnixPath(remotePath);

        return Task.Run(() =>
        {
            lock (session.Client)
            {
                session.Client.UploadFile(contentStream, normalized, canOverride: true);
            }
        }, ct);
    }

    public Task CreateDirectoryAsync(string connectionId, string currentPath, string folderName, CancellationToken ct = default)
    {
        var session = GetRequiredSession(connectionId);
        var newDirPath = CombineUnixPath(NormalizeUnixPath(currentPath), folderName.Trim());

        return Task.Run(() =>
        {
            lock (session.Client)
            {
                session.Client.CreateDirectory(newDirPath);
            }
        }, ct);
    }

    public Task CreateFileAsync(string connectionId, string currentPath, string fileName, string? content, CancellationToken ct = default)
    {
        var session = GetRequiredSession(connectionId);
        var newFilePath = CombineUnixPath(NormalizeUnixPath(currentPath), fileName.Trim());

        return Task.Run(() =>
        {
            lock (session.Client)
            {
                var bytes = Encoding.UTF8.GetBytes(content ?? "");
                using var ms = new MemoryStream(bytes);
                session.Client.UploadFile(ms, newFilePath, canOverride: false);
            }
        }, ct);
    }

    public Task RenameAsync(string connectionId, string oldPath, string newName, CancellationToken ct = default)
    {
        var session = GetRequiredSession(connectionId);
        var normalizedOld = NormalizeUnixPath(oldPath);
        var parentDir = GetUnixParentDirectory(normalizedOld);
        var normalizedNew = CombineUnixPath(parentDir, newName.Trim());

        return Task.Run(() =>
        {
            lock (session.Client)
            {
                session.Client.RenameFile(normalizedOld, normalizedNew);
            }
        }, ct);
    }

    public Task DeleteAsync(string connectionId, string targetPath, bool isDirectory, CancellationToken ct = default)
    {
        var session = GetRequiredSession(connectionId);
        var normalized = NormalizeUnixPath(targetPath);

        return Task.Run(() =>
        {
            lock (session.Client)
            {
                if (isDirectory)
                {
                    DeleteDirectoryRecursive(session.Client, normalized);
                }
                else
                {
                    session.Client.DeleteFile(normalized);
                }
            }
        }, ct);
    }

    public void Dispose()
    {
        foreach (var session in _sessions.Values)
        {
            session.Dispose();
        }
        _sessions.Clear();
    }

    // ══════════════════════════════════════════════════════════
    // 🛠️ INTERNAL HELPERS
    // ══════════════════════════════════════════════════════════

    private SftpClientSession GetRequiredSession(string connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId) || !_sessions.TryGetValue(connectionId, out var session))
        {
            throw new KeyNotFoundException($"Active SFTP connection '{connectionId}' was not found. Please connect first.");
        }

        if (!session.Client.IsConnected)
        {
            // Attempt automatic reconnect if dropped
            try
            {
                session.Client.Connect();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"SFTP connection '{session.Name}' is disconnected and could not reconnect: {ex.Message}", ex);
            }
        }

        return session;
    }

    private Task<SftpDirectoryResult> ListDirectoryInternalAsync(SftpClientSession session, string targetPath, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            lock (session.Client)
            {
                var files = session.Client.ListDirectory(targetPath);
                var items = new List<SftpFileItem>();

                foreach (var file in files)
                {
                    if (file.Name == "." || file.Name == "..") continue;

                    var isDir = file.IsDirectory;
                    var permissions = file.Attributes != null ? file.Attributes.ToString() : null;

                    items.Add(new SftpFileItem(
                        Name: file.Name,
                        Type: isDir ? "Directory" : "File",
                        Size: isDir ? 0L : file.Length,
                        Modified: file.LastWriteTimeUtc.ToString("o"),
                        IsSftp: true,
                        RemotePath: file.FullName,
                        Permissions: permissions
                    ));
                }

                // Sort directories first, then alphabetical
                var sorted = items
                    .OrderByDescending(i => i.Type == "Directory")
                    .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return new SftpDirectoryResult(targetPath, sorted);
            }
        }, ct);
    }

    private static ConnectionInfo CreateConnectionInfo(SftpConnectRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Host)) throw new ArgumentException("Host is required.");
        if (string.IsNullOrWhiteSpace(req.Username)) throw new ArgumentException("Username is required.");
        var port = req.Port > 0 ? req.Port : 22;

        var authMethods = new List<AuthenticationMethod>();

        if (string.Equals(req.AuthType, "key", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(req.PrivateKeyContent))
            {
                throw new ArgumentException("Private key content or path is required for key-based authentication.");
            }

            var keyContent = req.PrivateKeyContent.Trim();
            PrivateKeyFile keyFile;

            // If private key is a file path that exists on disk, read it
            if (File.Exists(keyContent))
            {
                keyFile = !string.IsNullOrEmpty(req.PrivateKeyPassphrase)
                    ? new PrivateKeyFile(keyContent, req.PrivateKeyPassphrase)
                    : new PrivateKeyFile(keyContent);
            }
            else
            {
                // Otherwise treat as inline PEM/OpenSSH string
                var keyBytes = Encoding.UTF8.GetBytes(keyContent);
                using var ms = new MemoryStream(keyBytes);
                keyFile = !string.IsNullOrEmpty(req.PrivateKeyPassphrase)
                    ? new PrivateKeyFile(ms, req.PrivateKeyPassphrase)
                    : new PrivateKeyFile(ms);
            }

            authMethods.Add(new PrivateKeyAuthenticationMethod(req.Username, keyFile));
        }
        else
        {
            authMethods.Add(new PasswordAuthenticationMethod(req.Username, req.Password ?? ""));
        }

        return new ConnectionInfo(req.Host, port, req.Username, authMethods.ToArray())
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    private static void DeleteDirectoryRecursive(SftpClient client, string remotePath)
    {
        var entries = client.ListDirectory(remotePath);
        foreach (var entry in entries)
        {
            if (entry.Name == "." || entry.Name == "..") continue;

            if (entry.IsDirectory)
            {
                DeleteDirectoryRecursive(client, entry.FullName);
            }
            else
            {
                client.DeleteFile(entry.FullName);
            }
        }
        client.DeleteDirectory(remotePath);
    }

    private static string NormalizeUnixPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "/";
        var normalized = path.Replace('\\', '/');
        if (!normalized.StartsWith('/')) normalized = "/" + normalized;
        while (normalized.Contains("//")) normalized = normalized.Replace("//", "/");
        if (normalized.Length > 1 && normalized.EndsWith('/')) normalized = normalized.TrimEnd('/');
        return normalized;
    }

    private static string CombineUnixPath(string baseDir, string relativeName)
    {
        var normalizedBase = NormalizeUnixPath(baseDir);
        if (normalizedBase == "/") return "/" + relativeName.TrimStart('/');
        return normalizedBase + "/" + relativeName.TrimStart('/');
    }

    private static string GetUnixParentDirectory(string path)
    {
        var normalized = NormalizeUnixPath(path);
        if (normalized == "/") return "/";
        var lastSlash = normalized.LastIndexOf('/');
        if (lastSlash <= 0) return "/";
        return normalized[..lastSlash];
    }
}
