using FluentFTP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VenEl.AssistantMCP.FTP.Services;

public class FluentFtpService : IFtpService
{
    private AsyncFtpClient? _client;

    public async Task ConnectAsync(FtpConnectionOptions options, CancellationToken token = default)
    {
        _client = new AsyncFtpClient(options.Host, options.Username, options.Password, options.Port > 0 ? options.Port : 21);
        _client.Config.EncryptionMode = FtpEncryptionMode.Auto; // Automatically use FTPS if available
        _client.Config.ValidateAnyCertificate = true; // For testing, usually you'd want proper validation
        await _client.AutoConnect(token);
    }

    public async Task<List<FtpItem>> ListDirectoryAsync(string path, CancellationToken token = default)
    {
        EnsureConnected();
        var items = await _client!.GetListing(path, FtpListOption.Auto, token);
        return items.Select(i => new FtpItem
        {
            Name = i.Name,
            FullName = i.FullName,
            IsDirectory = i.Type == FtpObjectType.Directory,
            Size = i.Size,
            Modified = i.Modified
        }).ToList();
    }

    public async Task<string> ReadTextAsync(string path, CancellationToken token = default)
    {
        EnsureConnected();
        using var memoryStream = new MemoryStream();
        if (await _client!.DownloadStream(memoryStream, path, token: token))
        {
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
        throw new Exception($"Failed to read file at {path}");
    }

    public async Task DownloadFileAsync(string remotePath, string localPath, CancellationToken token = default)
    {
        EnsureConnected();
        // Determine if directory or file
        var isDir = await _client!.DirectoryExists(remotePath, token);
        if (isDir)
        {
            var results = await _client.DownloadDirectory(localPath, remotePath, FtpFolderSyncMode.Update, token: token);
            if (results.Any(r => r.IsFailed))
                throw new Exception("Some files failed to download.");
        }
        else
        {
            if (File.Exists(localPath)) throw new Exception($"Local file {localPath} already exists. Overwrite not allowed.");
            await _client.DownloadFile(localPath, remotePath, FtpLocalExists.Skip, FtpVerify.None, null, token);
        }
    }

    public async Task CopyFileAsync(string sourcePath, string destinationPath, CancellationToken token = default)
    {
        EnsureConnected();
        if (await _client!.FileExists(destinationPath, token) || await _client.DirectoryExists(destinationPath, token))
        {
            throw new Exception($"Destination {destinationPath} already exists. Overwrite not allowed.");
        }
        // FluentFTP might not support native copy across all servers, but many do. 
        // We will try native, or throw if not supported.
        throw new NotSupportedException("Server-side copy is not universally supported in standard FTP. Consider downloading and re-uploading.");
    }

    public async Task MoveFileAsync(string sourcePath, string destinationPath, CancellationToken token = default)
    {
        EnsureConnected();
        if (await _client!.FileExists(destinationPath, token) || await _client.DirectoryExists(destinationPath, token))
        {
            throw new Exception($"Destination {destinationPath} already exists. Overwrite not allowed.");
        }
        await _client.Rename(sourcePath, destinationPath, token);
    }

    public void Dispose()
    {
        _client?.Dispose();
    }

    private void EnsureConnected()
    {
        if (_client == null || !_client.IsConnected)
            throw new InvalidOperationException("Not connected to FTP server.");
    }
}
