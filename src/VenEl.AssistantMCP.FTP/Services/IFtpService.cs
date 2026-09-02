using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VenEl.AssistantMCP.FTP.Services;

public interface IFtpService : IDisposable
{
    Task ConnectAsync(FtpConnectionOptions options, CancellationToken token = default);
    Task<List<FtpItem>> ListDirectoryAsync(string path, CancellationToken token = default);
    Task<string> ReadTextAsync(string path, CancellationToken token = default);
    Task DownloadFileAsync(string remotePath, string localPath, CancellationToken token = default);
    Task CopyFileAsync(string sourcePath, string destinationPath, CancellationToken token = default);
    Task MoveFileAsync(string sourcePath, string destinationPath, CancellationToken token = default);
}
