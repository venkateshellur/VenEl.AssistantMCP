using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VenEl.AssistantMCP.FTP.Services;

public class SftpService : IFtpService
{
    private SftpClient? _client;

    public Task ConnectAsync(FtpConnectionOptions options, CancellationToken token = default)
    {
        _client = new SftpClient(options.Host, options.Port > 0 ? options.Port : 22, options.Username, options.Password);
        _client.Connect();
        return Task.CompletedTask;
    }

    public Task<List<FtpItem>> ListDirectoryAsync(string path, CancellationToken token = default)
    {
        EnsureConnected();
        var files = _client!.ListDirectory(path);
        var result = files
            .Where(f => f.Name != "." && f.Name != "..")
            .Select(f => new FtpItem
            {
                Name = f.Name,
                FullName = f.FullName,
                IsDirectory = f.IsDirectory,
                Size = f.Length,
                Modified = f.LastWriteTime
            }).ToList();
            
        return Task.FromResult(result);
    }

    public Task<string> ReadTextAsync(string path, CancellationToken token = default)
    {
        EnsureConnected();
        var text = _client!.ReadAllText(path);
        return Task.FromResult(text);
    }

    public Task DownloadFileAsync(string remotePath, string localPath, CancellationToken token = default)
    {
        EnsureConnected();
        var isDir = _client!.GetAttributes(remotePath).IsDirectory;
        if (isDir)
        {
            throw new NotSupportedException("Recursive directory download for SFTP is not implemented natively. Only single file downloads are supported in this basic version.");
        }
        else
        {
            if (File.Exists(localPath)) throw new Exception($"Local file {localPath} already exists. Overwrite not allowed.");
            using var file = File.OpenWrite(localPath);
            _client.DownloadFile(remotePath, file);
        }
        return Task.CompletedTask;
    }

    public Task CopyFileAsync(string sourcePath, string destinationPath, CancellationToken token = default)
    {
        throw new NotSupportedException("SFTP does not support server-side copying natively.");
    }

    public Task MoveFileAsync(string sourcePath, string destinationPath, CancellationToken token = default)
    {
        EnsureConnected();
        if (_client!.Exists(destinationPath))
        {
            throw new Exception($"Destination {destinationPath} already exists. Overwrite not allowed.");
        }
        _client.RenameFile(sourcePath, destinationPath);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _client?.Dispose();
    }

    private void EnsureConnected()
    {
        if (_client == null || !_client.IsConnected)
            throw new InvalidOperationException("Not connected to SFTP server.");
    }
}
