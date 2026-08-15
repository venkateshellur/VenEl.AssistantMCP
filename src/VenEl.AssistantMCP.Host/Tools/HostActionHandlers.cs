using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenEl.AssistantMCP.Core.Dispatcher;

namespace VenEl.AssistantMCP.Host.Tools;

public sealed class HostReadFileActionHandler(ILogger<HostReadFileActionHandler> logger) : IActionHandler<HostCommandArgs>
{
    public string ActionName => "local_read_file";

    public string? Validate(HostCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.FilePath)) return "Missing required parameter 'FilePath'.";
        return null;
    }

    public async Task<string> HandleAsync(HostCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Reading local file {Path}", args.FilePath);
        try
        {
            if (!File.Exists(args.FilePath)) return $"[ERROR] File not found: {args.FilePath}";
            return await File.ReadAllTextAsync(args.FilePath, ct);
        }
        catch (Exception ex)
        {
            return $"[ERROR] Failed to read file: {ex.Message}";
        }
    }
}

public sealed class HostWriteFileActionHandler(ILogger<HostWriteFileActionHandler> logger) : IActionHandler<HostCommandArgs>
{
    public string ActionName => "local_write_file";

    public string? Validate(HostCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.FilePath)) return "Missing required parameter 'FilePath'.";
        if (string.IsNullOrWhiteSpace(args.FileContent)) return "Missing required parameter 'FileContent'.";
        return null;
    }

    public async Task<string> HandleAsync(HostCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Writing local file {Path}", args.FilePath);
        try
        {
            var dir = Path.GetDirectoryName(args.FilePath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            await File.WriteAllTextAsync(args.FilePath, args.FileContent, ct);
            return $"[OK] Successfully wrote to {args.FilePath}";
        }
        catch (Exception ex)
        {
            return $"[ERROR] Failed to write file: {ex.Message}";
        }
    }
}

public sealed class HostRunCommandActionHandler(ILogger<HostRunCommandActionHandler> logger) : IActionHandler<HostCommandArgs>
{
    public string ActionName => "local_run_command";

    public string? Validate(HostCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Command)) return "Missing required parameter 'Command'.";
        return null;
    }

    public async Task<string> HandleAsync(HostCommandArgs args, CancellationToken ct)
    {
        logger.LogDebug("Running local command: {Command}", args.Command);
        
        var isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
        
        var fileName = isWindows ? "cmd.exe" : "/bin/bash";
        var arguments = isWindows ? $"/c \"{args.Command}\"" : $"-c \"{args.Command.Replace("\"", "\\\"")}\"";

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Environment.CurrentDirectory
        };

        try
        {
            using var process = Process.Start(psi);
            if (process is null) return "[ERROR] Failed to start shell process.";

            var outputTask = process.StandardOutput.ReadToEndAsync(ct);
            var errorTask = process.StandardError.ReadToEndAsync(ct);

            await process.WaitForExitAsync(ct);

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                return $"[ERROR] Command exited with code {process.ExitCode}\n{output}\n{error}";
            }

            return string.IsNullOrWhiteSpace(output) ? "[OK]" : output;
        }
        catch (Exception ex)
        {
            return $"[ERROR] Failed to execute command: {ex.Message}";
        }
    }
}
