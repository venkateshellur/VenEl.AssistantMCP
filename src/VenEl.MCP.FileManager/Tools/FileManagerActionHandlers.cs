using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using VenEl.MCP.Core.Dispatcher;

namespace VenEl.MCP.FileManager.Tools;

public class ListDirActionHandler : IActionHandler<FileManagerCommandArgs>
{
    public string ActionName => "list_dir";
    public string? Validate(FileManagerCommandArgs args) => string.IsNullOrEmpty(args.Path) ? "Path is required." : null;

    public Task<string> HandleAsync(FileManagerCommandArgs args, CancellationToken ct)
    {
        if (!Directory.Exists(args.Path)) return Task.FromResult($"Directory not found: {args.Path}");
        var dirs = Directory.GetDirectories(args.Path).Select(d => new { Name = Path.GetFileName(d), Type = "Directory" });
        var files = Directory.GetFiles(args.Path).Select(f => new { Name = Path.GetFileName(f), Type = "File", Size = new FileInfo(f).Length });
        return Task.FromResult(JsonSerializer.Serialize(dirs.Concat<object>(files)));
    }
}

public class ReadFileActionHandler : IActionHandler<FileManagerCommandArgs>
{
    public string ActionName => "read_file";
    public string? Validate(FileManagerCommandArgs args) => string.IsNullOrEmpty(args.Path) ? "Path is required." : null;

    public async Task<string> HandleAsync(FileManagerCommandArgs args, CancellationToken ct)
    {
        if (!File.Exists(args.Path)) return $"File not found: {args.Path}";
        return await File.ReadAllTextAsync(args.Path!, ct);
    }
}

public class WriteFileActionHandler : IActionHandler<FileManagerCommandArgs>
{
    public string ActionName => "write_file";
    public string? Validate(FileManagerCommandArgs args) => string.IsNullOrEmpty(args.Path) ? "Path is required." : null;

    public async Task<string> HandleAsync(FileManagerCommandArgs args, CancellationToken ct)
    {
        await File.WriteAllTextAsync(args.Path!, args.Content ?? "", ct);
        return $"File written successfully to {args.Path}";
    }
}

public class DeleteFileActionHandler : IActionHandler<FileManagerCommandArgs>
{
    public string ActionName => "delete_file";
    public string? Validate(FileManagerCommandArgs args) => string.IsNullOrEmpty(args.Path) ? "Path is required." : null;

    public Task<string> HandleAsync(FileManagerCommandArgs args, CancellationToken ct)
    {
        if (File.Exists(args.Path))
        {
            File.Delete(args.Path);
            return Task.FromResult($"File deleted: {args.Path}");
        }
        else if (Directory.Exists(args.Path))
        {
            Directory.Delete(args.Path, true);
            return Task.FromResult($"Directory deleted: {args.Path}");
        }
        return Task.FromResult($"Path not found: {args.Path}");
    }
}

public class MoveFileActionHandler : IActionHandler<FileManagerCommandArgs>
{
    public string ActionName => "move_file";
    public string? Validate(FileManagerCommandArgs args) 
    {
        if (string.IsNullOrEmpty(args.Path)) return "Path is required.";
        if (string.IsNullOrEmpty(args.DestinationPath)) return "DestinationPath is required.";
        return null;
    }

    public Task<string> HandleAsync(FileManagerCommandArgs args, CancellationToken ct)
    {
        if (File.Exists(args.Path))
        {
            File.Move(args.Path, args.DestinationPath!, true);
            return Task.FromResult($"File moved to {args.DestinationPath}");
        }
        else if (Directory.Exists(args.Path))
        {
            Directory.Move(args.Path, args.DestinationPath!);
            return Task.FromResult($"Directory moved to {args.DestinationPath}");
        }
        return Task.FromResult($"Source path not found: {args.Path}");
    }
}

public class CreateDirActionHandler : IActionHandler<FileManagerCommandArgs>
{
    public string ActionName => "create_dir";
    public string? Validate(FileManagerCommandArgs args) => string.IsNullOrEmpty(args.Path) ? "Path is required." : null;

    public Task<string> HandleAsync(FileManagerCommandArgs args, CancellationToken ct)
    {
        try
        {
            if (Directory.Exists(args.Path))
            {
                return Task.FromResult($"Directory already exists: {args.Path}");
            }
            Directory.CreateDirectory(args.Path!);
            return Task.FromResult($"Directory created successfully at {args.Path}");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Failed to create directory: {ex.Message}");
        }
    }
}

public class CopyFileActionHandler : IActionHandler<FileManagerCommandArgs>
{
    public string ActionName => "copy_file";
    public string? Validate(FileManagerCommandArgs args)
    {
        if (string.IsNullOrEmpty(args.Path)) return "Path is required.";
        if (string.IsNullOrEmpty(args.DestinationPath)) return "DestinationPath is required.";
        return null;
    }

    public Task<string> HandleAsync(FileManagerCommandArgs args, CancellationToken ct)
    {
        try
        {
            if (File.Exists(args.Path))
            {
                var destDir = Path.GetDirectoryName(args.DestinationPath);
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }
                File.Copy(args.Path, args.DestinationPath!, true);
                return Task.FromResult($"File copied to {args.DestinationPath}");
            }
            else if (Directory.Exists(args.Path))
            {
                CopyDirectoryRecursive(args.Path, args.DestinationPath!);
                return Task.FromResult($"Directory copied to {args.DestinationPath}");
            }
            return Task.FromResult($"Source path not found: {args.Path}");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Failed to copy: {ex.Message}");
        }
    }

    private static void CopyDirectoryRecursive(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), true);
        }
        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            CopyDirectoryRecursive(dir, Path.Combine(targetDir, Path.GetFileName(dir)));
        }
    }
}

public class GetDetailsActionHandler : IActionHandler<FileManagerCommandArgs>
{
    public string ActionName => "get_details";
    public string? Validate(FileManagerCommandArgs args) => string.IsNullOrEmpty(args.Path) ? "Path is required." : null;

    public Task<string> HandleAsync(FileManagerCommandArgs args, CancellationToken ct)
    {
        try
        {
            if (File.Exists(args.Path))
            {
                var fi = new FileInfo(args.Path);
                var details = new
                {
                    Name = fi.Name,
                    Path = fi.FullName,
                    Type = "File",
                    Extension = fi.Extension,
                    SizeBytes = fi.Length,
                    Created = fi.CreationTimeUtc.ToString("o"),
                    Modified = fi.LastWriteTimeUtc.ToString("o")
                };
                return Task.FromResult(JsonSerializer.Serialize(details, new JsonSerializerOptions { WriteIndented = true }));
            }
            else if (Directory.Exists(args.Path))
            {
                var di = new DirectoryInfo(args.Path);
                var details = new
                {
                    Name = di.Name,
                    Path = di.FullName,
                    Type = "Directory",
                    Created = di.CreationTimeUtc.ToString("o"),
                    Modified = di.LastWriteTimeUtc.ToString("o")
                };
                return Task.FromResult(JsonSerializer.Serialize(details, new JsonSerializerOptions { WriteIndented = true }));
            }
            return Task.FromResult($"Path not found: {args.Path}");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Failed to get details: {ex.Message}");
        }
    }
}

public class OpenUiActionHandler : IActionHandler<FileManagerCommandArgs>
{
    public string ActionName => "open_ui";
    public string? Validate(FileManagerCommandArgs args) => null;

    public Task<string> HandleAsync(FileManagerCommandArgs args, CancellationToken ct)
    {
        var url = "http://localhost:5050";

        if (!args.ForceExternal)
        {
            return Task.FromResult($"The FileManager UI is actively running at {url}. AI INSTRUCTION: Ask the user if they want you to open it in their external OS browser, or if they prefer to open it manually in their integrated IDE browser. If they say external, call this tool again with ForceExternal = true.");
        }

        try
        {
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("msedge", $"--app={url}") { UseShellExecute = true });
                }
                catch
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            {
                try
                {
                    System.Diagnostics.Process.Start("google-chrome", $"--app={url}");
                }
                catch
                {
                    System.Diagnostics.Process.Start("xdg-open", url);
                }
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
            {
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo("open", $"-na \"Google Chrome\" --args --app={url}") { UseShellExecute = false };
                    var p = System.Diagnostics.Process.Start(psi);
                    p?.WaitForExit(1000);
                    if (p != null && p.ExitCode != 0)
                    {
                        System.Diagnostics.Process.Start("open", url);
                    }
                }
                catch
                {
                    System.Diagnostics.Process.Start("open", url);
                }
            }
            return Task.FromResult($"Successfully opened the File Manager UI in clean app mode (without address bar) at {url}");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Failed to force open external UI: {ex.Message}. You can manually navigate to {url}");
        }
    }
}
