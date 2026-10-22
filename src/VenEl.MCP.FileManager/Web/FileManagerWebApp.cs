using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VenEl.MCP.FileManager.Configuration;
using VenEl.MCP.FileManager.Sftp;

namespace VenEl.MCP.FileManager.Web;

public record NewFolderRequest(string CurrentPath, string FolderName);
public record NewFileRequest(string CurrentPath, string FileName, string? Content);
public record SaveFileRequest(string? FilePath, string? Path, string? Content);
public record RenameRequest(string OldPath, string NewName);
public record DeleteRequest(string? TargetPath, string? Path, List<string>? Paths);
public record CopyMoveRequest(string Source, string DestinationDir);
public record BatchRenameItem(string OldName, string NewName);
public record BatchRenameRequest(string CurrentPath, List<BatchRenameItem> Renames);
public record CompressRequest(string? CurrentPath, string? Folder, List<string>? Items, string? ArchiveName);
public record ExtractRequest(string? ArchivePath, string? DestinationDir, string? Folder, string? ArchiveName, string? DestinationFolder);
public record ExportProjectRequest(string Path, string? DestinationDir);
public record DisconnectSftpRequest(string ConnectionId);
public record OpenPathRequest(string? Path);

public static class FileManagerWebApp
{
    private static WebApplication? _app;
    private static readonly ISftpSessionManager _sftp = new SftpSessionManager();

    public static async Task StartAsync(FileManagerOptions options, CancellationToken ct = default)
    {
        if (_app != null) return; // Already running

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.ListenAnyIP(options.WebPort);
        });

        builder.Services.AddCors();

        _app = builder.Build();

        _app.UseCors(b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

        _app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Engine"] = "VenEl-Kestrel";
            await next();
        });

        // 1. Directory Listing
        _app.MapGet("/api/files/list", (string? path) => 
        {
            try 
            {
                if (string.IsNullOrWhiteSpace(path) || path == ".") 
                    path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                
                if (!Directory.Exists(path)) return Results.NotFound("Directory not found.");
                
                var absolutePath = Path.GetFullPath(path);
                var dirs = Directory.GetDirectories(absolutePath).Select(d => new { 
                    Name = Path.GetFileName(d), 
                    Type = "Directory",
                    Size = 0L,
                    Modified = new DirectoryInfo(d).LastWriteTimeUtc.ToString("o")
                });
                var files = Directory.GetFiles(absolutePath).Select(f => new { 
                    Name = Path.GetFileName(f), 
                    Type = "File", 
                    Size = new FileInfo(f).Length,
                    Modified = new FileInfo(f).LastWriteTimeUtc.ToString("o")
                });
                
                return Results.Ok(new { CurrentPath = absolutePath, Items = dirs.Concat<object>(files) });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        // 2. Read File Text
        _app.MapGet("/api/files/read", async (string path) => 
        {
            try
            {
                if (!File.Exists(path)) return Results.NotFound("File not found.");
                var content = await File.ReadAllTextAsync(path);
                return Results.Content(content, "text/plain");
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        // 2b. Download File
        _app.MapGet("/api/files/download", (string path) =>
        {
            if (!File.Exists(path)) return Results.NotFound("File not found.");
            return Results.File(path, "application/octet-stream", Path.GetFileName(path));
        });

        // 3. Raw File Stream
        _app.MapGet("/api/files/raw", (string path) => 
        {
            if (!File.Exists(path)) return Results.NotFound("File not found.");
            var ext = Path.GetExtension(path).ToLowerInvariant();
            var mimeType = ext switch {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".svg" => "image/svg+xml",
                ".webp" => "image/webp",
                ".ico" => "image/x-icon",
                ".bmp" => "image/bmp",
                ".pdf" => "application/pdf",
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".ogg" => "audio/ogg",
                ".m4a" => "audio/mp4",
                ".aac" => "audio/aac",
                ".flac" => "audio/flac",
                ".mp4" => "video/mp4",
                ".webm" => "video/webm",
                ".mov" => "video/quicktime",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".xls" => "application/vnd.ms-excel",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".csv" => "text/csv; charset=utf-8",
                ".tsv" => "text/tab-separated-values; charset=utf-8",
                ".txt" => "text/plain; charset=utf-8",
                ".json" => "application/json",
                ".md" => "text/markdown; charset=utf-8",
                _ => "application/octet-stream"
            };
            return Results.File(path, mimeType, enableRangeProcessing: true);
        });

        // 4. Roots / Drives
        _app.MapGet("/api/files/roots", () => 
        {
            var roots = new List<object>();
            try 
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    try
                    {
                        var path = drive.RootDirectory.FullName;
                        if (path.StartsWith("/System/Volumes") || path.StartsWith("/dev") || path.StartsWith("/private"))
                            continue;

                        var label = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? drive.Name : $"{drive.Name} ({drive.VolumeLabel})";
                        roots.Add(new { 
                            name = label, 
                            path = path, 
                            type = "Drive"
                        });
                    }
                    catch { }
                }
            }
            catch { }

            void TryAddSpecial(Environment.SpecialFolder folder, string name, string type)
            {
                try
                {
                    var p = Environment.GetFolderPath(folder);
                    if (!string.IsNullOrWhiteSpace(p) && Directory.Exists(p))
                    {
                        roots.Add(new { name = name, path = p, type = type });
                    }
                }
                catch { }
            }

            TryAddSpecial(Environment.SpecialFolder.UserProfile, "Home (~)", "Home");
            TryAddSpecial(Environment.SpecialFolder.Desktop, "Desktop", "Desktop");
            TryAddSpecial(Environment.SpecialFolder.MyDocuments, "Documents", "Documents");

            return Results.Ok(roots);
        });

        // 4b. IDE Environment & Workspace Information
        _app.MapGet("/api/env/info", () =>
        {
            var ideName = Environment.GetEnvironmentVariable("VENEL_IDE_NAME") ?? "IDE";
            var version = Environment.GetEnvironmentVariable("VENEL_EXTENSION_VERSION") ?? "1.1.3";
            return Results.Ok(new 
            { 
                ideName = ideName, 
                version = version,
                engine = "Kestrel (.NET)",
                workspaceFolders = Array.Empty<object>(), 
                primaryWorkspace = (string?)null 
            });
        });

        // 4b.2 Diagnostic Logs API
        _app.MapGet("/api/logs/recent", () =>
        {
            try
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var logDir = Path.Combine(home, ".venel", "logs");
                if (Directory.Exists(logDir))
                {
                    var todayFile = Path.Combine(logDir, $"venel-{DateTime.UtcNow:yyyy-MM-dd}.log");
                    if (File.Exists(todayFile))
                    {
                        var lines = File.ReadAllLines(todayFile);
                        var count = Math.Min(lines.Length, 40);
                        var recent = string.Join("\n", lines.TakeLast(count));
                        return Results.Ok(new { logs = recent });
                    }
                }
            }
            catch {}
            return Results.Ok(new { logs = $"[{DateTime.UtcNow:O}] [INFO] Kestrel (.NET) engine active and healthy." });
        });

        // 4c. Open File with OS System Default App (Excel, Word, Preview, VLC, etc.)
        _app.MapMethods("/api/files/openWithSystem", new[] { "GET", "POST" }, async (HttpContext ctx) =>
        {
            string? p = ctx.Request.Query["path"];
            if (string.IsNullOrWhiteSpace(p) && ctx.Request.HasJsonContentType())
            {
                try
                {
                    var req = await ctx.Request.ReadFromJsonAsync<OpenPathRequest>();
                    p = req?.Path;
                }
                catch {}
            }

            if (string.IsNullOrWhiteSpace(p) || (!File.Exists(p) && !Directory.Exists(p)))
                return Results.BadRequest(new { error = "Valid path required" });

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = p,
                    UseShellExecute = true
                };
                Process.Start(psi);
                return Results.Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Could not launch with system: {ex.Message}");
            }
        });

        // 4c2. Run Script in Terminal / Standalone Process
        _app.MapMethods("/api/files/runScript", new[] { "GET", "POST" }, async (HttpContext ctx) =>
        {
            string? p = ctx.Request.Query["path"];
            if (string.IsNullOrWhiteSpace(p) && ctx.Request.HasJsonContentType())
            {
                try
                {
                    var req = await ctx.Request.ReadFromJsonAsync<OpenPathRequest>();
                    p = req?.Path;
                }
                catch {}
            }

            if (string.IsNullOrWhiteSpace(p) || !File.Exists(p))
                return Results.BadRequest(new { error = "Valid file path required" });

            try
            {
                var dir = Path.GetDirectoryName(p) ?? Directory.GetCurrentDirectory();
                var ext = Path.GetExtension(p).ToLowerInvariant();
                ProcessStartInfo psi;

                if (OperatingSystem.IsWindows())
                {
                    if (ext == ".ps1")
                    {
                        psi = new ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = $"-NoExit -ExecutionPolicy Bypass -File \"{p}\"",
                            WorkingDirectory = dir,
                            UseShellExecute = true
                        };
                    }
                    else
                    {
                        psi = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/k \"{p}\"",
                            WorkingDirectory = dir,
                            UseShellExecute = true
                        };
                    }
                }
                else if (OperatingSystem.IsMacOS())
                {
                    psi = new ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = $"-a Terminal \"{p}\"",
                        WorkingDirectory = dir,
                        UseShellExecute = false
                    };
                }
                else
                {
                    psi = new ProcessStartInfo
                    {
                        FileName = "xdg-open",
                        Arguments = $"\"{p}\"",
                        WorkingDirectory = dir,
                        UseShellExecute = false
                    };
                }

                Process.Start(psi);
                return Results.Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Could not run script: {ex.Message}");
            }
        });

        // 4d. Open File in Editor stub
        _app.MapGet("/api/files/openInEditor", (string? path) =>
        {
            return Results.Ok(new { success = true });
        });

        // 5. New Folder
        _app.MapPost("/api/files/newFolder", (NewFolderRequest req) =>
        {
            if (string.IsNullOrWhiteSpace(req.CurrentPath) || string.IsNullOrWhiteSpace(req.FolderName))
                return Results.BadRequest("CurrentPath and FolderName are required.");
            var target = Path.Combine(req.CurrentPath, req.FolderName);
            Directory.CreateDirectory(target);
            return Results.Ok(new { success = true, path = target });
        });

        // 6. New File
        _app.MapPost("/api/files/newFile", async (NewFileRequest req) =>
        {
            if (string.IsNullOrWhiteSpace(req.CurrentPath) || string.IsNullOrWhiteSpace(req.FileName))
                return Results.BadRequest("CurrentPath and FileName are required.");
            var target = Path.Combine(req.CurrentPath, req.FileName);
            await File.WriteAllTextAsync(target, req.Content ?? "");
            return Results.Ok(new { success = true, path = target });
        });

        // 6b. Save File (In-line Quick Editor)
        _app.MapPost("/api/files/saveFile", async (SaveFileRequest req) =>
        {
            var target = req.FilePath ?? req.Path;
            if (string.IsNullOrWhiteSpace(target))
                return Results.BadRequest("Path is required.");
            await File.WriteAllTextAsync(target, req.Content ?? "");
            return Results.Ok(new { success = true, path = target });
        });

        // 7. Rename
        _app.MapPost("/api/files/rename", (RenameRequest req) =>
        {
            if (string.IsNullOrWhiteSpace(req.OldPath) || string.IsNullOrWhiteSpace(req.NewName))
                return Results.BadRequest("OldPath and NewName are required.");
            var dir = Path.GetDirectoryName(req.OldPath);
            if (dir == null) return Results.BadRequest("Invalid old path.");
            var target = Path.Combine(dir, req.NewName);
            if (File.Exists(req.OldPath)) File.Move(req.OldPath, target, true);
            else if (Directory.Exists(req.OldPath)) Directory.Move(req.OldPath, target);
            else return Results.NotFound("Path not found.");
            return Results.Ok(new { success = true, path = target });
        });

        // 8. Delete -> Move to OS Trash / Recycle Bin
        _app.MapPost("/api/files/delete", (DeleteRequest req) =>
        {
            var paths = new List<string>();
            var single = req.TargetPath ?? req.Path;
            if (!string.IsNullOrWhiteSpace(single)) paths.Add(single);
            if (req.Paths != null)
            {
                foreach (var p in req.Paths)
                {
                    if (!string.IsNullOrWhiteSpace(p) && !paths.Contains(p))
                        paths.Add(p);
                }
            }

            if (paths.Count == 0)
                return Results.BadRequest(new { error = "TargetPath or Paths is required." });

            int trashedCount = 0;
            var errors = new List<string>();
            foreach (var p in paths)
            {
                try
                {
                    if (File.Exists(p) || Directory.Exists(p))
                    {
                        MoveToTrash(p);
                        trashedCount++;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(ex.Message);
                }
            }

            if (trashedCount > 0 || errors.Count == 0)
                return Results.Ok(new { success = true, count = trashedCount });

            return Results.Problem($"Failed to move to trash: {string.Join(", ", errors)}");
        });

        // 9. Copy
        _app.MapPost("/api/files/copy", (CopyMoveRequest req) =>
        {
            if (string.IsNullOrWhiteSpace(req.Source) || string.IsNullOrWhiteSpace(req.DestinationDir))
                return Results.BadRequest("Source and DestinationDir are required.");
            var name = Path.GetFileName(req.Source);
            var dest = Path.Combine(req.DestinationDir, name);
            if (File.Exists(req.Source))
            {
                if (File.Exists(dest) && Path.GetDirectoryName(req.Source) == req.DestinationDir)
                {
                    var ext = Path.GetExtension(name);
                    var nameNoExt = Path.GetFileNameWithoutExtension(name);
                    dest = Path.Combine(req.DestinationDir, $"{nameNoExt} - Copy{ext}");
                }
                File.Copy(req.Source, dest, true);
            }
            else if (Directory.Exists(req.Source))
            {
                CopyDirectory(req.Source, dest);
            }
            else return Results.NotFound("Source not found.");
            return Results.Ok(new { success = true, path = dest });
        });

        // 10. Move
        _app.MapPost("/api/files/move", (CopyMoveRequest req) =>
        {
            if (string.IsNullOrWhiteSpace(req.Source) || string.IsNullOrWhiteSpace(req.DestinationDir))
                return Results.BadRequest("Source and DestinationDir are required.");
            var name = Path.GetFileName(req.Source);
            var dest = Path.Combine(req.DestinationDir, name);
            if (File.Exists(req.Source)) File.Move(req.Source, dest, true);
            else if (Directory.Exists(req.Source)) Directory.Move(req.Source, dest);
            else return Results.NotFound("Source not found.");
            return Results.Ok(new { success = true, path = dest });
        });

        // 10b. Batch Rename
        _app.MapPost("/api/files/batch-rename", (BatchRenameRequest req) =>
        {
            if (string.IsNullOrWhiteSpace(req.CurrentPath) || req.Renames == null || req.Renames.Count == 0)
                return Results.BadRequest("CurrentPath and Renames are required.");

            int successCount = 0;
            foreach (var item in req.Renames)
            {
                if (string.IsNullOrWhiteSpace(item.OldName) || string.IsNullOrWhiteSpace(item.NewName) || item.OldName == item.NewName)
                    continue;

                var oldPath = Path.Combine(req.CurrentPath, item.OldName);
                var newPath = Path.Combine(req.CurrentPath, item.NewName);

                if (!File.Exists(oldPath) && !Directory.Exists(oldPath))
                    continue;

                try
                {
                    bool isCaseOnly = string.Equals(item.OldName, item.NewName, StringComparison.OrdinalIgnoreCase);
                    if (isCaseOnly)
                    {
                        var tempPath = Path.Combine(req.CurrentPath, $"__venel_tmp_{Guid.NewGuid():N}_{item.OldName}");
                        if (File.Exists(oldPath))
                        {
                            File.Move(oldPath, tempPath);
                            File.Move(tempPath, newPath);
                        }
                        else
                        {
                            Directory.Move(oldPath, tempPath);
                            Directory.Move(tempPath, newPath);
                        }
                    }
                    else
                    {
                        if (File.Exists(oldPath)) File.Move(oldPath, newPath);
                        else Directory.Move(oldPath, newPath);
                    }
                    successCount++;
                }
                catch {}
            }

            return Results.Ok(new { success = true, count = successCount });
        });

        // 10c. Compress to .zip
        _app.MapPost("/api/files/compress", (CompressRequest req) =>
        {
            var currentPath = (req.CurrentPath ?? req.Folder ?? "").Trim();
            if (string.IsNullOrWhiteSpace(currentPath) || req.Items == null || req.Items.Count == 0)
                return Results.BadRequest(new { error = "CurrentPath and Items are required." });

            var archiveName = (req.ArchiveName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(archiveName))
            {
                archiveName = req.Items.Count == 1 ? $"{req.Items[0]}.zip" : "Archive.zip";
            }
            if (!archiveName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                archiveName += ".zip";
            }

            int counter = 1;
            var finalArchiveName = archiveName;
            var archiveFullPath = Path.Combine(currentPath, finalArchiveName);
            while (File.Exists(archiveFullPath) || Directory.Exists(archiveFullPath))
            {
                var baseWithoutExt = Path.GetFileNameWithoutExtension(archiveName);
                finalArchiveName = $"{baseWithoutExt}_{counter}.zip";
                archiveFullPath = Path.Combine(currentPath, finalArchiveName);
                counter++;
            }

            try
            {
                using (var zip = ZipFile.Open(archiveFullPath, ZipArchiveMode.Create))
                {
                    foreach (var item in req.Items)
                    {
                        var fullItemPath = Path.Combine(currentPath, item);
                        var entryName = Path.GetFileName(item);
                        if (File.Exists(fullItemPath))
                        {
                            zip.CreateEntryFromFile(fullItemPath, entryName, CompressionLevel.Optimal);
                        }
                        else if (Directory.Exists(fullItemPath))
                        {
                            AddDirectoryToZip(zip, fullItemPath, entryName);
                        }
                    }
                }

                if (File.Exists(archiveFullPath) && new FileInfo(archiveFullPath).Length > 0)
                {
                    return Results.Ok(new { success = true, archiveName = finalArchiveName, path = archiveFullPath });
                }
            }
            catch {}

            // Fallback: Use native system archiver (bsdtar or zip)
            try
            {
                var escapedItems = string.Join(" ", req.Items.Select(i => $"\"{i.Replace("\"", "\\\"")}\""));
                var psi = new ProcessStartInfo
                {
                    FileName = "tar",
                    Arguments = $"-a -cf \"{archiveFullPath}\" {escapedItems}",
                    WorkingDirectory = currentPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(30000);
                if (proc?.ExitCode == 0 && File.Exists(archiveFullPath))
                {
                    return Results.Ok(new { success = true, archiveName = finalArchiveName, path = archiveFullPath });
                }
            }
            catch {}

            if (File.Exists(archiveFullPath))
            {
                return Results.Ok(new { success = true, archiveName = finalArchiveName, path = archiveFullPath });
            }

            return Results.Ok(new { success = false, error = "Compression failed with all available archivers." });
        });

        // 10d. Extract Archive
        _app.MapPost("/api/files/extract", (ExtractRequest req) =>
        {
            var archivePath = (req.ArchivePath ?? "").Trim();
            if (string.IsNullOrWhiteSpace(archivePath) && !string.IsNullOrWhiteSpace(req.Folder) && !string.IsNullOrWhiteSpace(req.ArchiveName))
            {
                archivePath = Path.Combine(req.Folder, req.ArchiveName);
            }

            if (string.IsNullOrWhiteSpace(archivePath) || !File.Exists(archivePath))
                return Results.BadRequest(new { error = "Valid ArchivePath is required." });

            var targetDir = req.DestinationDir ?? req.DestinationFolder;
            if (string.IsNullOrWhiteSpace(targetDir))
            {
                targetDir = Path.GetDirectoryName(archivePath) ?? archivePath;
            }

            Directory.CreateDirectory(targetDir);

            try
            {
                ZipFile.ExtractToDirectory(archivePath, targetDir, overwriteFiles: true);
                return Results.Ok(new { success = true, destinationDir = targetDir });
            }
            catch {}

            // Fallback: Use native tar extractor (handles .zip, .tar, .tar.gz, .tgz across platforms)
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "tar",
                    Arguments = $"-xf \"{archivePath}\" -C \"{targetDir}\"",
                    WorkingDirectory = targetDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(30000);
                if (proc?.ExitCode == 0)
                {
                    return Results.Ok(new { success = true, destinationDir = targetDir });
                }
            }
            catch {}

            return Results.Ok(new { success = false, error = "Extraction failed with all available extractors." });
        });

        // 11. Details
        _app.MapGet("/api/files/details", (string path) =>
        {
            if (File.Exists(path))
            {
                var fi = new FileInfo(path);
                return Results.Ok(new { name = fi.Name, path = fi.FullName, isDirectory = false, size = fi.Length, modified = fi.LastWriteTimeUtc });
            }
            if (Directory.Exists(path))
            {
                var di = new DirectoryInfo(path);
                var count = 0;
                try { count = di.GetFileSystemInfos().Length; } catch {}
                return Results.Ok(new { name = di.Name, path = di.FullName, isDirectory = true, size = 0L, itemCount = count, modified = di.LastWriteTimeUtc, created = di.CreationTimeUtc });
            }
            return Results.NotFound();
        });

        // 11b. Open in Terminal
        _app.MapGet("/api/files/openInTerminal", (string path) =>
        {
            try
            {
                var dir = Directory.Exists(path) ? path : (File.Exists(path) ? Path.GetDirectoryName(path) : path);
                if (dir != null && Directory.Exists(dir))
                {
                    if (OperatingSystem.IsWindows())
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe", $"/K cd /d \"{dir}\"") { UseShellExecute = true });
                    }
                    else if (OperatingSystem.IsMacOS())
                    {
                        System.Diagnostics.Process.Start("open", $"-a Terminal \"{dir}\"");
                    }
                }
                return Results.Ok(new { ok = true });
            }
            catch
            {
                return Results.Ok(new { ok = false });
            }
        });

        // 11c. Open Settings & Toggle Sidebar
        _app.MapGet("/api/files/openSettings", () => Results.Ok(new { ok = true }));
        _app.MapGet("/api/files/toggleSidebar", () => Results.Ok(new { ok = true }));

        // 11c2. Agent Machine Search (Spotlight on macOS / PowerShell on Windows / find on Linux)
        _app.MapGet("/api/files/agentSearch", async (string? query, string? scope, string? currentPath, int? maxResults) =>
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Results.BadRequest(new { error = "Query is required" });
            }

            var effectiveScope = string.IsNullOrWhiteSpace(scope) ? "machine" : scope;
            var effectiveMax = Math.Min(maxResults ?? 80, 150);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var (engine, items) = await PerformAgentSearchAsync(query.Trim(), effectiveScope, currentPath ?? "", effectiveMax);
            sw.Stop();

            return Results.Ok(new
            {
                query = query.Trim(),
                scope = effectiveScope,
                executionTimeMs = sw.ElapsedMilliseconds,
                engine,
                count = items.Count,
                items
            });
        });

        // 11d. Reveal in OS (Finder / File Explorer)
        _app.MapGet("/api/files/revealInOS", (string path) =>
        {
            try
            {
                if (File.Exists(path) || Directory.Exists(path))
                {
                    if (OperatingSystem.IsMacOS())
                    {
                        System.Diagnostics.Process.Start("open", $"-R \"{path}\"");
                    }
                    else if (OperatingSystem.IsWindows())
                    {
                        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\"");
                    }
                    else
                    {
                        var dir = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
                        System.Diagnostics.Process.Start("xdg-open", $"\"{dir}\"");
                    }
                }
                return Results.Ok(new { ok = true });
            }
            catch
            {
                return Results.Ok(new { ok = false });
            }
        });

        // 11e. Git Status
        _app.MapGet("/api/files/gitStatus", (string path) =>
        {
            try
            {
                var dir = Directory.Exists(path) ? path : (File.Exists(path) ? Path.GetDirectoryName(path) : path);
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                {
                    return Results.Ok(new { isRepo = false });
                }

                var isInside = RunProcess("git", "rev-parse --is-inside-work-tree", dir);
                if (isInside != "true")
                {
                    return Results.Ok(new { isRepo = false });
                }

                var branch = RunProcess("git", "rev-parse --abbrev-ref HEAD", dir);
                if (string.IsNullOrEmpty(branch)) branch = "HEAD";

                var aheadBehind = RunProcess("git", "rev-list --left-right --count HEAD...@{u}", dir);
                int ahead = 0, behind = 0;
                if (!string.IsNullOrEmpty(aheadBehind))
                {
                    var parts = aheadBehind.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        int.TryParse(parts[0], out ahead);
                        int.TryParse(parts[1], out behind);
                    }
                }

                var repoRoot = RunProcess("git", "rev-parse --show-toplevel", dir);
                var statuses = new Dictionary<string, string>();

                if (!string.IsNullOrEmpty(repoRoot))
                {
                    var porcelain = RunProcess("git", "status --porcelain=v1 -uall", dir);
                    if (!string.IsNullOrEmpty(porcelain))
                    {
                        var lines = porcelain.Split('\n');
                        foreach (var rawLine in lines)
                        {
                            var line = rawLine.TrimEnd('\r');
                            if (line.Length < 4) continue;
                            var xy = line.Substring(0, 2);
                            string code = "M";
                            if (xy.Contains('?')) code = "U";
                            else if (xy.Contains('A')) code = "A";
                            else if (xy.Contains('D')) code = "D";
                            else if (xy.Contains('R')) code = "R";
                            else if (xy.Contains('M')) code = "M";

                            var relToRepo = line.Substring(3).Trim();
                            if (relToRepo.Contains(" -> "))
                            {
                                relToRepo = relToRepo.Split(" -> ")[1];
                            }
                            relToRepo = relToRepo.Trim('"');
                            var absFilePath = Path.GetFullPath(Path.Combine(repoRoot, relToRepo));
                            var relToDir = Path.GetRelativePath(dir, absFilePath);

                            if (!relToDir.StartsWith("..") && !Path.IsPathRooted(relToDir))
                            {
                                var topSegment = relToDir.Split(Path.DirectorySeparatorChar)[0];
                                if (!string.IsNullOrEmpty(topSegment))
                                {
                                    if (!statuses.ContainsKey(topSegment) || code == "M")
                                    {
                                        statuses[topSegment] = code;
                                    }
                                }
                            }
                        }
                    }
                }

                return Results.Ok(new { isRepo = true, branch, ahead, behind, statuses });
            }
            catch
            {
                return Results.Ok(new { isRepo = false });
            }
        });

        // 11f. Diff with HEAD
        _app.MapGet("/api/files/diffWithHead", (string path) => Results.Ok(new { ok = true }));

        // 11f2. In-App Visual Diff / File Comparison
        _app.MapGet("/api/files/diff", async (string fileA, string? fileB) =>
        {
            try
            {
                if (string.IsNullOrEmpty(fileA) || !File.Exists(fileA))
                {
                    return Results.BadRequest(new { ok = false, error = "File A is required and must exist." });
                }

                string textA = "";
                string textB = "";
                string labelA = Path.GetFileName(fileA);
                string labelB = !string.IsNullOrEmpty(fileB) ? Path.GetFileName(fileB) : "HEAD";

                if (string.IsNullOrEmpty(fileB) || fileB == "HEAD")
                {
                    var dir = Path.GetDirectoryName(fileA) ?? "";
                    var fileName = Path.GetFileName(fileA);
                    var relPath = RunProcess("git", $"ls-files --full-name \"{fileName}\"", dir).Trim();
                    if (string.IsNullOrEmpty(relPath))
                    {
                        return Results.Ok(new { ok = false, error = "File is not tracked in Git." });
                    }

                    textA = RunProcess("git", $"show HEAD:\"{relPath}\"", dir);
                    textB = await File.ReadAllTextAsync(fileA);
                    labelA = $"{fileName} (Git HEAD)";
                    labelB = $"{fileName} (Working Copy)";
                }
                else
                {
                    if (!File.Exists(fileB))
                    {
                        return Results.BadRequest(new { ok = false, error = $"File B does not exist: {fileB}" });
                    }
                    textA = await File.ReadAllTextAsync(fileA);
                    textB = await File.ReadAllTextAsync(fileB);
                }

                if (textA.Contains('\0') || textB.Contains('\0'))
                {
                    return Results.Ok(new
                    {
                        ok = true,
                        isBinary = true,
                        fileA = new { path = fileA, name = Path.GetFileName(fileA), label = labelA, size = new FileInfo(fileA).Length },
                        fileB = new { path = fileB ?? fileA, name = !string.IsNullOrEmpty(fileB) ? Path.GetFileName(fileB) : Path.GetFileName(fileA), label = labelB, size = File.Exists(fileB ?? "") ? new FileInfo(fileB!).Length : new FileInfo(fileA).Length },
                        message = "Binary files cannot be displayed as text diff."
                    });
                }

                var (stats, lines) = ComputeDiff(textA, textB);
                return Results.Ok(new
                {
                    ok = true,
                    isBinary = false,
                    fileA = new { path = fileA, name = Path.GetFileName(fileA), label = labelA, size = new FileInfo(fileA).Length },
                    fileB = new { path = fileB ?? fileA, name = !string.IsNullOrEmpty(fileB) ? Path.GetFileName(fileB) : Path.GetFileName(fileA), label = labelB, size = File.Exists(fileB ?? "") ? new FileInfo(fileB!).Length : new FileInfo(fileA).Length },
                    stats,
                    lines
                });
            }
            catch (Exception ex)
            {
                return Results.Ok(new { ok = false, error = ex.Message });
            }
        });

        // 11g. Decompile & Inspect Binary (.NET DLL/EXE, Java class/jar)
        _app.MapGet("/api/files/decompile", async (string path, CancellationToken ct) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return Results.BadRequest(new { isSuccess = false, errorMessage = "Path parameter is required." });

                var decompiler = new VenEl.MCP.Decompiler.Services.DecompilerService();
                var result = await decompiler.DecompileAsync(path, ct);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Ok(new { isSuccess = false, errorMessage = ex.Message });
            }
        });

        // 11h. Export Reconstructed Project from Binary (.NET, Java, WASM, Native)
        _app.MapPost("/api/files/decompile/export", async (ExportProjectRequest req, CancellationToken ct) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.Path))
                    return Results.BadRequest(new { isSuccess = false, errorMessage = "Path parameter is required." });

                var decompiler = new VenEl.MCP.Decompiler.Services.DecompilerService();
                var result = await decompiler.ExportProjectAsync(req.Path, req.DestinationDir, ct);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Ok(new { isSuccess = false, errorMessage = ex.Message });
            }
        });

        _app.MapGet("/api/files/decompile/export", async (string path, string? destinationDir, CancellationToken ct) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return Results.BadRequest(new { isSuccess = false, errorMessage = "Path parameter is required." });

                var decompiler = new VenEl.MCP.Decompiler.Services.DecompilerService();
                var result = await decompiler.ExportProjectAsync(path, destinationDir, ct);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Ok(new { isSuccess = false, errorMessage = ex.Message });
            }
        });

        // 11i. AI Context & LLM Token Intelligence Endpoint
        _app.MapGet("/api/files/ai/context", async (string path) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return Results.BadRequest(new { ok = false, error = "Path is required" });

                if (Directory.Exists(path))
                {
                    var dirInfo = new DirectoryInfo(path);
                    var codeExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                        ".cs", ".ts", ".js", ".jsx", ".tsx", ".py", ".java", ".go", ".rs", ".cpp", ".c", ".h",
                        ".html", ".css", ".scss", ".json", ".xml", ".yaml", ".yml", ".md", ".sql", ".sh"
                    };

                    int totalFiles = 0;
                    int totalCodeFiles = 0;
                    long totalBytes = 0;
                    long totalLines = 0;
                    var extCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    var files = dirInfo.EnumerateFiles("*", new EnumerationOptions { 
                        RecurseSubdirectories = true, 
                        MaxRecursionDepth = 5,
                        IgnoreInaccessible = true 
                    });

                    foreach (var f in files)
                    {
                        if (f.FullName.Contains("/.git/") || f.FullName.Contains("\\.git\\") ||
                            f.FullName.Contains("/node_modules/") || f.FullName.Contains("\\node_modules\\") ||
                            f.FullName.Contains("/bin/") || f.FullName.Contains("\\bin\\") ||
                            f.FullName.Contains("/obj/") || f.FullName.Contains("\\obj\\"))
                            continue;

                        totalFiles++;
                        totalBytes += f.Length;
                        var ext = f.Extension.ToLowerInvariant();
                        if (!string.IsNullOrEmpty(ext))
                        {
                            extCounts[ext] = extCounts.TryGetValue(ext, out var count) ? count + 1 : 1;
                        }

                        if (codeExtensions.Contains(ext) && f.Length < 1024 * 1024)
                        {
                            totalCodeFiles++;
                            try
                            {
                                using var reader = f.OpenText();
                                while (reader.ReadLine() != null)
                                {
                                    totalLines++;
                                }
                            }
                            catch { }
                        }

                        if (totalFiles >= 500) break;
                    }

                    long estimatedTokens = Math.Max(1, (long)Math.Ceiling(totalBytes / 3.8));

                    return Results.Ok(new
                    {
                        ok = true,
                        isDirectory = true,
                        name = dirInfo.Name,
                        path = dirInfo.FullName,
                        totalFiles,
                        totalCodeFiles,
                        totalBytes,
                        totalLines,
                        estimatedTokens,
                        extensionBreakdown = extCounts
                    });
                }
                else if (File.Exists(path))
                {
                    var fileInfo = new FileInfo(path);
                    var ext = fileInfo.Extension.ToLowerInvariant();
                    bool isText = true;
                    string content = "";
                    long lineCount = 0;

                    if (fileInfo.Length > 5 * 1024 * 1024)
                    {
                        isText = false;
                    }
                    else
                    {
                        try
                        {
                            content = await File.ReadAllTextAsync(path);
                            if (content.IndexOf('\0') >= 0)
                            {
                                isText = false;
                                content = "";
                            }
                            else
                            {
                                lineCount = content.Split('\n').Length;
                            }
                        }
                        catch
                        {
                            isText = false;
                        }
                    }

                    long estimatedTokens = isText ? Math.Max(1, (long)Math.Ceiling(content.Length / 3.8)) : 0;
                    string snippet = isText && content.Length > 2000 ? content.Substring(0, 2000) + "\n... (truncated)" : content;

                    return Results.Ok(new
                    {
                        ok = true,
                        isDirectory = false,
                        name = fileInfo.Name,
                        path = fileInfo.FullName,
                        size = fileInfo.Length,
                        extension = ext,
                        isText,
                        lineCount,
                        estimatedTokens,
                        snippet,
                        content = isText && fileInfo.Length < 500 * 1024 ? content : snippet
                    });
                }
                else
                {
                    return Results.NotFound(new { ok = false, error = "Path does not exist." });
                }
            }
            catch (Exception ex)
            {
                return Results.Ok(new { ok = false, error = ex.Message });
            }
        });

        // ══════════════════════════════════════════════════════════
        // 🌐 SFTP REMOTE ENDPOINTS
        // ══════════════════════════════════════════════════════════

        // 11a. Test SFTP Connection
        _app.MapPost("/api/sftp/test", async (SftpConnectRequest req, CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await _sftp.TestConnectionAsync(req, cancellationToken);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Ok(new SftpTestResult(false, ex.Message, null, null));
            }
        });

        // 11b. Connect to SFTP Server
        _app.MapPost("/api/sftp/connect", async (SftpConnectRequest req, CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await _sftp.ConnectAsync(req, cancellationToken);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Ok(new SftpConnectResult(false, null, ex.Message, null, null, null));
            }
        });

        // 11c. Disconnect Active SFTP Session
        _app.MapPost("/api/sftp/disconnect", async (DisconnectSftpRequest req) =>
        {
            var disconnected = await _sftp.DisconnectAsync(req.ConnectionId);
            return Results.Ok(new { success = disconnected });
        });

        // 11d. Active SFTP Sessions
        _app.MapGet("/api/sftp/sessions", () =>
        {
            return Results.Ok(_sftp.GetActiveSessions());
        });

        // 11e. List Remote Directory
        _app.MapGet("/api/sftp/list", async (string connectionId, string? path, CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await _sftp.ListDirectoryAsync(connectionId, path, cancellationToken);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        // 11f. Read Remote File Text (for In-Line Editor & Quick Look)
        _app.MapGet("/api/sftp/read", async (string connectionId, string path, CancellationToken cancellationToken) =>
        {
            try
            {
                var content = await _sftp.ReadFileTextAsync(connectionId, path, cancellationToken);
                return Results.Content(content, "text/plain; charset=utf-8");
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        // 11g. Download Remote File
        _app.MapGet("/api/sftp/download", async (string connectionId, string path, CancellationToken cancellationToken) =>
        {
            try
            {
                var bytes = await _sftp.ReadFileBytesAsync(connectionId, path, cancellationToken);
                var fileName = Path.GetFileName(path.Replace('\\', '/'));
                return Results.File(bytes, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        // 11h. Save Remote File Text
        _app.MapPost("/api/sftp/save", async (SftpSaveFileRequest req, CancellationToken cancellationToken) =>
        {
            try
            {
                await _sftp.SaveFileTextAsync(req.ConnectionId, req.RemotePath, req.Content, cancellationToken);
                return Results.Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        // 11i. Create Remote Folder
        _app.MapPost("/api/sftp/newFolder", async (SftpNewFolderRequest req, CancellationToken cancellationToken) =>
        {
            try
            {
                await _sftp.CreateDirectoryAsync(req.ConnectionId, req.CurrentPath, req.FolderName, cancellationToken);
                return Results.Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        // 11j. Create Remote File
        _app.MapPost("/api/sftp/newFile", async (SftpNewFileRequest req, CancellationToken cancellationToken) =>
        {
            try
            {
                await _sftp.CreateFileAsync(req.ConnectionId, req.CurrentPath, req.FileName, req.Content, cancellationToken);
                return Results.Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        // 11k. Rename Remote File or Directory
        _app.MapPost("/api/sftp/rename", async (SftpRenameRequest req, CancellationToken cancellationToken) =>
        {
            try
            {
                await _sftp.RenameAsync(req.ConnectionId, req.OldPath, req.NewName, cancellationToken);
                return Results.Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        // 11l. Delete Remote Item
        _app.MapPost("/api/sftp/delete", async (SftpDeleteRequest req, CancellationToken cancellationToken) =>
        {
            try
            {
                await _sftp.DeleteAsync(req.ConnectionId, req.TargetPath, req.IsDirectory, cancellationToken);
                return Results.Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        // 11m. Upload File to SFTP
        _app.MapPost("/api/sftp/upload", async (HttpRequest httpRequest, string connectionId, string remotePath, CancellationToken cancellationToken) =>
        {
            try
            {
                if (!httpRequest.HasFormContentType || httpRequest.Form.Files.Count == 0)
                {
                    using var stream = httpRequest.Body;
                    await _sftp.UploadFileAsync(connectionId, remotePath, stream, cancellationToken);
                }
                else
                {
                    var file = httpRequest.Form.Files[0];
                    using var stream = file.OpenReadStream();
                    await _sftp.UploadFileAsync(connectionId, remotePath, stream, cancellationToken);
                }
                return Results.Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        // 12. UI Dashboard & Static Assets
        _app.MapGet("/", (HttpContext ctx) => ServeStaticFile("index.html", "text/html; charset=utf-8", ctx));
        _app.MapGet("/index.html", (HttpContext ctx) => ServeStaticFile("index.html", "text/html; charset=utf-8", ctx));
        _app.MapGet("/styles.css", (HttpContext ctx) => ServeStaticFile("styles.css", "text/css; charset=utf-8", ctx));
        _app.MapGet("/app.js", (HttpContext ctx) => ServeStaticFile("app.js", "application/javascript; charset=utf-8", ctx));
        _app.MapGet("/logo.png", (HttpContext ctx) => ServeStaticFile("logo.png", "image/png", ctx));
        _app.MapGet("/logo.svg", (HttpContext ctx) => ServeStaticFile("logo.svg", "image/svg+xml", ctx));
        _app.MapGet("/icon.svg", (HttpContext ctx) => ServeStaticFile("icon.svg", "image/svg+xml", ctx));
        _app.MapGet("/media/{fileName}", (string fileName, HttpContext ctx) => ServeStaticFile(fileName, null, ctx));

        await _app.StartAsync(ct);
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var target = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, target, true);
        }
        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var target = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, target);
        }
    }

    private static void AddDirectoryToZip(ZipArchive zip, string sourceDir, string entryPrefix)
    {
        try
        {
            var dirFiles = Directory.GetFiles(sourceDir);
            var dirSubDirs = Directory.GetDirectories(sourceDir);

            if (dirFiles.Length == 0 && dirSubDirs.Length == 0)
            {
                zip.CreateEntry(entryPrefix.TrimEnd('/') + "/");
                return;
            }

            foreach (var file in dirFiles)
            {
                try
                {
                    var entryName = Path.Combine(entryPrefix, Path.GetFileName(file)).Replace('\\', '/');
                    zip.CreateEntryFromFile(file, entryName, CompressionLevel.Optimal);
                }
                catch {}
            }
            foreach (var subDir in dirSubDirs)
            {
                try
                {
                    var dirName = Path.GetFileName(subDir);
                    AddDirectoryToZip(zip, subDir, Path.Combine(entryPrefix, dirName).Replace('\\', '/'));
                }
                catch {}
            }
        }
        catch {}
    }

    private static void MoveToTrash(string path)
    {
        if (OperatingSystem.IsMacOS())
        {
            var escaped = path.Replace("\\", "\\\\").Replace("\"", "\\\"");
            var psi = new ProcessStartInfo("osascript", $"-e \"tell application \\\"Finder\\\" to delete POSIX file \\\"{escaped}\\\"\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var proc = Process.Start(psi);
            proc?.WaitForExit(5000);
            if (proc != null && proc.ExitCode != 0)
            {
                var err = proc.StandardError.ReadToEnd();
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(err) ? "Failed to move file to macOS Trash" : err);
            }
        }
        else if (OperatingSystem.IsWindows())
        {
            var escaped = path.Replace("'", "''");
            var script = $"Add-Type -AssemblyName Microsoft.VisualBasic; if (Test-Path -LiteralPath '{escaped}' -PathType Container) {{ [Microsoft.VisualBasic.FileIO.FileSystem]::DeleteDirectory('{escaped}', 'OnlyErrorDialogs', 'SendToRecycleBin') }} else {{ [Microsoft.VisualBasic.FileIO.FileSystem]::DeleteFile('{escaped}', 'OnlyErrorDialogs', 'SendToRecycleBin') }}";
            var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -NonInteractive -Command \"{script}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var proc = Process.Start(psi);
            proc?.WaitForExit(5000);
            if (proc != null && proc.ExitCode != 0)
            {
                var err = proc.StandardError.ReadToEnd();
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(err) ? "Failed to move file to Windows Recycle Bin" : err);
            }
        }
        else
        {
            // Linux
            var psi = new ProcessStartInfo("gio", $"trash \"{path}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var proc = Process.Start(psi);
            proc?.WaitForExit(5000);
        }
    }

    public static async Task StopAsync()
    {
        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
            _app = null;
        }
    }

        private static async Task<(string engine, List<object> items)> PerformAgentSearchAsync(string query, string scope, string currentPath, int maxResults)
    {
        var isMac = OperatingSystem.IsMacOS();
        var isWindows = OperatingSystem.IsWindows();
        var items = new List<object>();
        var cleanQuery = query.Replace("\"", "").Replace("'", "").Replace(";", "").Trim();

        if (isMac)
        {
            var scopeArg = "";
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (scope == "home") scopeArg = $"-onlyin \"{home}\"";
            else if (scope == "current" && !string.IsNullOrWhiteSpace(currentPath) && Directory.Exists(currentPath))
                scopeArg = $"-onlyin \"{currentPath}\"";

            string mdfindExpr;
            if (cleanQuery.StartsWith("*."))
                mdfindExpr = $"\"kMDItemFSName == '*.{cleanQuery.Substring(2)}'c\"";
            else if (cleanQuery.StartsWith("."))
                mdfindExpr = $"\"kMDItemFSName == '*.{cleanQuery.Substring(1)}'c\"";
            else
                mdfindExpr = $"\"kMDItemFSName == '*{cleanQuery}*'c\"";

            var cmd = $"mdfind {scopeArg} {mdfindExpr} | head -n {maxResults}";
            var output = await RunBashCommandAsync(cmd);
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var filePath = line.Trim();
                try
                {
                    if (File.Exists(filePath))
                    {
                        var fi = new FileInfo(filePath);
                        items.Add(new
                        {
                            name = fi.Name,
                            path = fi.FullName,
                            directory = fi.DirectoryName,
                            isDirectory = false,
                            type = "File",
                            size = fi.Length,
                            modified = fi.LastWriteTimeUtc.ToString("o")
                        });
                    }
                    else if (Directory.Exists(filePath))
                    {
                        var di = new DirectoryInfo(filePath);
                        items.Add(new
                        {
                            name = di.Name,
                            path = di.FullName,
                            directory = di.Parent?.FullName ?? di.FullName,
                            isDirectory = true,
                            type = "Directory",
                            size = 0L,
                            modified = di.LastWriteTimeUtc.ToString("o")
                        });
                    }
                }
                catch { }
            }
            if (items.Count > 0)
            {
                return ("Apple Spotlight Index (mdfind)", items);
            }

            // Fallback: If mdfind returns 0 results or path is unindexed, search with fast find
            try
            {
                var fallbackRoot = scope == "home" ? home : (scope == "current" && !string.IsNullOrWhiteSpace(currentPath) && Directory.Exists(currentPath) ? currentPath : home);
                var fallbackCmd = $"find \"{fallbackRoot}\" -iname \"*{cleanQuery}*\" -maxdepth 5 2>/dev/null | head -n {maxResults}";
                var fallbackOut = await RunBashCommandAsync(fallbackCmd);
                var fbLines = fallbackOut.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in fbLines)
                {
                    var filePath = line.Trim();
                    try
                    {
                        if (File.Exists(filePath))
                        {
                            var fi = new FileInfo(filePath);
                            items.Add(new
                            {
                                name = fi.Name,
                                path = fi.FullName,
                                directory = fi.DirectoryName,
                                isDirectory = false,
                                type = "File",
                                size = fi.Length,
                                modified = fi.LastWriteTimeUtc.ToString("o")
                            });
                        }
                        else if (Directory.Exists(filePath))
                        {
                            var di = new DirectoryInfo(filePath);
                            items.Add(new
                            {
                                name = di.Name,
                                path = di.FullName,
                                directory = di.Parent?.FullName ?? di.FullName,
                                isDirectory = true,
                                type = "Directory",
                                size = 0L,
                                modified = di.LastWriteTimeUtc.ToString("o")
                            });
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return ("Apple Spotlight Index (mdfind)", items);
        }
        else if (isWindows)
        {
            var rootPath = scope == "home" ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) : "C:\\Users";
            if (scope == "current" && !string.IsNullOrWhiteSpace(currentPath) && Directory.Exists(currentPath)) rootPath = currentPath;

            try
            {
                var dir = new DirectoryInfo(rootPath);
                var files = dir.EnumerateFileSystemInfos($"*{cleanQuery}*", new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true,
                    MaxRecursionDepth = 5
                }).Take(maxResults);

                foreach (var info in files)
                {
                    bool isDir = (info.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
                    items.Add(new
                    {
                        name = info.Name,
                        path = info.FullName,
                        directory = Path.GetDirectoryName(info.FullName),
                        isDirectory = isDir,
                        type = isDir ? "Directory" : "File",
                        size = isDir ? 0L : ((FileInfo)info).Length,
                        modified = info.LastWriteTimeUtc.ToString("o")
                    });
                }
            }
            catch { }
            return ("Windows Search Index", items);
        }
        else
        {
            try
            {
                var targetDir = scope == "home" ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) : (scope == "current" && !string.IsNullOrWhiteSpace(currentPath) && Directory.Exists(currentPath) ? currentPath : "/");
                var cmd = $"find \"{targetDir}\" -iname \"*{cleanQuery}*\" -maxdepth 6 2>/dev/null | head -n {maxResults}";
                var output = await RunBashCommandAsync(cmd);
                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var filePath = line.Trim();
                    try
                    {
                        if (File.Exists(filePath))
                        {
                            var fi = new FileInfo(filePath);
                            items.Add(new
                            {
                                name = fi.Name,
                                path = fi.FullName,
                                directory = fi.DirectoryName,
                                isDirectory = false,
                                type = "File",
                                size = fi.Length,
                                modified = fi.LastWriteTimeUtc.ToString("o")
                            });
                        }
                        else if (Directory.Exists(filePath))
                        {
                            var di = new DirectoryInfo(filePath);
                            items.Add(new
                            {
                                name = di.Name,
                                path = di.FullName,
                                directory = di.Parent?.FullName ?? di.FullName,
                                isDirectory = true,
                                type = "Directory",
                                size = 0L,
                                modified = di.LastWriteTimeUtc.ToString("o")
                            });
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return ("Linux Find Index", items);
        }
    }

    private static Task<string> RunBashCommandAsync(string command)
    {
        return Task.Run(() =>
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                psi.ArgumentList.Add("-c");
                psi.ArgumentList.Add(command);
                using var p = System.Diagnostics.Process.Start(psi);
                if (p == null) return string.Empty;
                var outStr = p.StandardOutput.ReadToEnd();
                p.WaitForExit(5000);
                return outStr;
            }
            catch { return string.Empty; }
        });
    }

    private static (object stats, List<object> lines) ComputeDiff(string textA, string textB)
    {
        var linesA = textA.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var linesB = textB.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        if (textA == textB)
        {
            var identicalLines = new List<object>();
            for (int idx = 0; idx < linesA.Length; idx++)
            {
                identicalLines.Add(new { type = "unchanged", lineA = (int?)(idx + 1), lineB = (int?)(idx + 1), text = linesA[idx] });
            }
            return (new { added = 0, removed = 0, unchanged = linesA.Length }, identicalLines);
        }

        int maxLines = 2500;
        var a = linesA.Take(maxLines).ToArray();
        var b = linesB.Take(maxLines).ToArray();

        int[,] dp = new int[a.Length + 1, b.Length + 1];
        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                if (a[i - 1] == b[j - 1])
                    dp[i, j] = dp[i - 1, j - 1] + 1;
                else
                    dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
            }
        }

        var outLines = new List<object>();
        int curI = a.Length;
        int curJ = b.Length;
        int added = 0, removed = 0, unchanged = 0;

        while (curI > 0 || curJ > 0)
        {
            if (curI > 0 && curJ > 0 && a[curI - 1] == b[curJ - 1])
            {
                outLines.Add(new { type = "unchanged", lineA = (int?)curI, lineB = (int?)curJ, text = a[curI - 1] });
                curI--;
                curJ--;
                unchanged++;
            }
            else if (curJ > 0 && (curI == 0 || dp[curI, curJ - 1] >= dp[curI - 1, curJ]))
            {
                outLines.Add(new { type = "added", lineA = (int?)null, lineB = (int?)curJ, text = b[curJ - 1] });
                curJ--;
                added++;
            }
            else if (curI > 0 && (curJ == 0 || dp[curI, curJ - 1] < dp[curI - 1, curJ]))
            {
                outLines.Add(new { type = "removed", lineA = (int?)curI, lineB = (int?)null, text = a[curI - 1] });
                curI--;
                removed++;
            }
        }

        outLines.Reverse();
        return (new { added, removed, unchanged }, outLines);
    }

    private static string RunProcess(string fileName, string args, string workingDir)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc == null) return string.Empty;
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(3000);
            return proc.ExitCode == 0 ? output.TrimEnd() : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static IResult ServeStaticFile(string fileName, string? customContentType = null, HttpContext? ctx = null)
    {
        if (ctx != null)
        {
            ctx.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            ctx.Response.Headers.Pragma = "no-cache";
            ctx.Response.Headers.Expires = "0";
        }
        var safeFileName = Path.GetFileName(fileName);
        var ext = Path.GetExtension(safeFileName).ToLowerInvariant();
        var contentType = customContentType ?? ext switch
        {
            ".html" => "text/html; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".js" => "application/javascript; charset=utf-8",
            ".png" => "image/png",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            _ => "application/octet-stream"
        };

        // 1. Check physical disk locations (extension media/ folder, current dir, etc.)
        var candidateDirs = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "..", "media"),
            Path.Combine(AppContext.BaseDirectory, "media"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "media"),
            Path.Combine(Directory.GetCurrentDirectory(), "media"),
            Path.Combine(AppContext.BaseDirectory, "Web", "Static"),
            Path.Combine(Directory.GetCurrentDirectory(), "Web", "Static"),
            AppContext.BaseDirectory
        };

        foreach (var dir in candidateDirs)
        {
            var filePath = Path.Combine(dir, safeFileName);
            if (File.Exists(filePath))
            {
                return Results.File(Path.GetFullPath(filePath), contentType);
            }
        }

        // 2. Check embedded resource fallback in assembly
        var asm = typeof(FileManagerWebApp).Assembly;
        var resName = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(safeFileName, StringComparison.OrdinalIgnoreCase));
        if (resName != null)
        {
            var stream = asm.GetManifestResourceStream(resName);
            if (stream != null)
            {
                return Results.Stream(stream, contentType);
            }
        }

        return Results.NotFound($"{safeFileName} not found");
    }
}
