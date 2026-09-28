using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VenEl.MCP.Core.Dispatcher;
using VenEl.MCP.FTP.Services;

namespace VenEl.MCP.FTP.Tools;

public sealed class FtpListActionHandler(FtpServiceFactory factory) : IActionHandler<FtpCommandArgs>
{
    public string ActionName => "list";

    public string? Validate(FtpCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Host)) return "Missing host";
        if (string.IsNullOrWhiteSpace(args.SourcePath)) return "Missing sourcePath";
        return null;
    }

    public async Task<string> HandleAsync(FtpCommandArgs args, CancellationToken ct)
    {
        var options = CreateOptions(args);
        using var service = factory.CreateService(options);
        await service.ConnectAsync(options, ct);
        var items = await service.ListDirectoryAsync(args.SourcePath, ct);
        return JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
    }

    private static FtpConnectionOptions CreateOptions(FtpCommandArgs args) => new()
    {
        Host = args.Host,
        Port = args.Port,
        Username = args.Username,
        Password = args.Password,
        IsSftp = args.IsSftp
    };
}

public sealed class FtpReadActionHandler(FtpServiceFactory factory) : IActionHandler<FtpCommandArgs>
{
    public string ActionName => "read";

    public string? Validate(FtpCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Host)) return "Missing host";
        if (string.IsNullOrWhiteSpace(args.SourcePath)) return "Missing sourcePath";
        return null;
    }

    public async Task<string> HandleAsync(FtpCommandArgs args, CancellationToken ct)
    {
        var options = new FtpConnectionOptions { Host = args.Host, Port = args.Port, Username = args.Username, Password = args.Password, IsSftp = args.IsSftp };
        using var service = factory.CreateService(options);
        await service.ConnectAsync(options, ct);
        return await service.ReadTextAsync(args.SourcePath, ct);
    }
}

public sealed class FtpDownloadActionHandler(FtpServiceFactory factory) : IActionHandler<FtpCommandArgs>
{
    public string ActionName => "download";

    public string? Validate(FtpCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Host)) return "Missing host";
        if (string.IsNullOrWhiteSpace(args.SourcePath)) return "Missing sourcePath";
        if (string.IsNullOrWhiteSpace(args.DestinationPath)) return "Missing destinationPath";
        return null;
    }

    public async Task<string> HandleAsync(FtpCommandArgs args, CancellationToken ct)
    {
        var options = new FtpConnectionOptions { Host = args.Host, Port = args.Port, Username = args.Username, Password = args.Password, IsSftp = args.IsSftp };
        using var service = factory.CreateService(options);
        await service.ConnectAsync(options, ct);
        await service.DownloadFileAsync(args.SourcePath, args.DestinationPath, ct);
        return $"Successfully downloaded {args.SourcePath} to {args.DestinationPath}";
    }
}

public sealed class FtpMoveActionHandler(FtpServiceFactory factory) : IActionHandler<FtpCommandArgs>
{
    public string ActionName => "move";

    public string? Validate(FtpCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Host)) return "Missing host";
        if (string.IsNullOrWhiteSpace(args.SourcePath)) return "Missing sourcePath";
        if (string.IsNullOrWhiteSpace(args.DestinationPath)) return "Missing destinationPath";
        return null;
    }

    public async Task<string> HandleAsync(FtpCommandArgs args, CancellationToken ct)
    {
        var options = new FtpConnectionOptions { Host = args.Host, Port = args.Port, Username = args.Username, Password = args.Password, IsSftp = args.IsSftp };
        using var service = factory.CreateService(options);
        await service.ConnectAsync(options, ct);
        await service.MoveFileAsync(args.SourcePath, args.DestinationPath, ct);
        return $"Successfully moved {args.SourcePath} to {args.DestinationPath}";
    }
}

public sealed class FtpCopyActionHandler(FtpServiceFactory factory) : IActionHandler<FtpCommandArgs>
{
    public string ActionName => "copy";

    public string? Validate(FtpCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Host)) return "Missing host";
        if (string.IsNullOrWhiteSpace(args.SourcePath)) return "Missing sourcePath";
        if (string.IsNullOrWhiteSpace(args.DestinationPath)) return "Missing destinationPath";
        return null;
    }

    public async Task<string> HandleAsync(FtpCommandArgs args, CancellationToken ct)
    {
        var options = new FtpConnectionOptions { Host = args.Host, Port = args.Port, Username = args.Username, Password = args.Password, IsSftp = args.IsSftp };
        using var service = factory.CreateService(options);
        await service.ConnectAsync(options, ct);
        await service.CopyFileAsync(args.SourcePath, args.DestinationPath, ct);
        return $"Successfully copied {args.SourcePath} to {args.DestinationPath}";
    }
}
