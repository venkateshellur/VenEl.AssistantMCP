using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VenEl.MCP.FileManager.Configuration;

namespace VenEl.MCP.FileManager.Web;

public static class FileManagerWebApp
{
    private static WebApplication? _app;

    public static async Task StartAsync(FileManagerOptions options, CancellationToken ct = default)
    {
        if (_app != null) return; // Already running

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.ListenAnyIP(options.WebPort);
        });

        // Add services
        builder.Services.AddCors();

        _app = builder.Build();

        _app.UseCors(b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

        // Simple API endpoints that wrap the standard file operations
        _app.MapGet("/api/files/list", (string? path) => 
        {
            try 
            {
                if (string.IsNullOrWhiteSpace(path) || path == ".") 
                    path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                
                if (!Directory.Exists(path)) return Results.NotFound("Directory not found.");
                
                var absolutePath = Path.GetFullPath(path);
                var dirs = Directory.GetDirectories(absolutePath).Select(d => new { Name = Path.GetFileName(d), Type = "Directory" });
                var files = Directory.GetFiles(absolutePath).Select(f => new { Name = Path.GetFileName(f), Type = "File", Size = new FileInfo(f).Length });
                
                return Results.Ok(new { CurrentPath = absolutePath, Items = dirs.Concat<object>(files) });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

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
                _ => "application/octet-stream"
            };
            return Results.File(path, mimeType);
        });

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

        // UI Dashboard
        _app.MapGet("/", () => Results.Content(GetIndexHtml(), "text/html"));

        await _app.StartAsync(ct);
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

    private static string GetIndexHtml()
    {
        return """
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>VenEl File Manager</title>
            <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600&display=swap" rel="stylesheet">
            <style>
                :root {
                    --bg-color: #121214;
                    --panel-bg: rgba(255, 255, 255, 0.03);
                    --border-color: rgba(255, 255, 255, 0.1);
                    --text-main: #e0e0e0;
                    --text-muted: #888;
                    --accent: #6c5ce7;
                    --accent-hover: #8172fa;
                    --hover-bg: rgba(255, 255, 255, 0.08);
                }
                * { box-sizing: border-box; margin: 0; padding: 0; }
                body {
                    font-family: 'Inter', sans-serif;
                    background-color: var(--bg-color);
                    color: var(--text-main);
                    height: 100vh;
                    display: flex;
                    flex-direction: column;
                }
                header {
                    padding: 15px 24px;
                    background: var(--panel-bg);
                    border-bottom: 1px solid var(--border-color);
                    backdrop-filter: blur(10px);
                    display: flex;
                    align-items: center;
                    gap: 15px;
                }
                .nav-buttons button {
                    background: transparent;
                    border: 1px solid var(--border-color);
                    color: var(--text-main);
                    padding: 8px 12px;
                    border-radius: 6px;
                    cursor: pointer;
                    transition: 0.2s;
                }
                .nav-buttons button:hover { background: var(--hover-bg); }
                .nav-buttons button.active {
                    background: var(--accent);
                    border-color: var(--accent);
                }
                .path-bar-container {
                    flex-grow: 1;
                    position: relative;
                }
                .path-bar {
                    display: flex;
                    align-items: center;
                    background: var(--bg-color);
                    border: 1px solid var(--border-color);
                    border-radius: 6px;
                    padding: 0 4px 0 10px;
                    transition: border-color 0.2s, box-shadow 0.2s;
                }
                .path-bar:focus-within {
                    border-color: var(--accent);
                    box-shadow: 0 0 0 2px rgba(108, 92, 231, 0.2);
                }
                #path {
                    width: 100%;
                    background: transparent;
                    border: none;
                    color: var(--text-main);
                    padding: 9px 0;
                    font-size: 13px;
                    outline: none;
                    font-family: inherit;
                }
                #btn-path-dropdown {
                    background: transparent;
                    border: none;
                    color: var(--text-muted);
                    padding: 6px 8px;
                    border-radius: 4px;
                    cursor: pointer;
                    font-size: 11px;
                    line-height: 1;
                    transition: 0.15s;
                }
                #btn-path-dropdown:hover {
                    background: var(--hover-bg);
                    color: var(--text-main);
                }
                .path-dropdown {
                    position: absolute;
                    top: calc(100% + 6px);
                    left: 0;
                    right: 0;
                    background: #18181c;
                    border: 1px solid var(--border-color);
                    border-radius: 8px;
                    box-shadow: 0 12px 32px rgba(0, 0, 0, 0.65);
                    max-height: 380px;
                    overflow-y: auto;
                    z-index: 1000;
                    display: flex;
                    flex-direction: column;
                    padding: 6px 0;
                }
                .dropdown-section-title {
                    font-size: 10px;
                    font-weight: 700;
                    text-transform: uppercase;
                    letter-spacing: 0.8px;
                    color: var(--text-muted);
                    padding: 8px 14px 4px;
                }
                .dropdown-item {
                    display: flex;
                    align-items: center;
                    gap: 10px;
                    padding: 8px 14px;
                    cursor: pointer;
                    font-size: 13px;
                    color: var(--text-main);
                    transition: background 0.15s;
                }
                .dropdown-item:hover {
                    background: var(--hover-bg);
                }
                .dropdown-item i {
                    font-style: normal;
                    font-size: 16px;
                }
                .dropdown-item .item-sub {
                    color: var(--text-muted);
                    font-size: 11px;
                    margin-left: auto;
                    max-width: 50%;
                    overflow: hidden;
                    text-overflow: ellipsis;
                    white-space: nowrap;
                }
                .dropdown-divider {
                    height: 1px;
                    background: var(--border-color);
                    margin: 6px 0;
                }
                .main-content {
                    display: flex;
                    flex-grow: 1;
                    overflow: hidden;
                }
                .file-list-container {
                    flex-grow: 1;
                    overflow-y: auto;
                    padding: 10px 20px;
                }
                .preview-pane {
                    width: 350px;
                    background: var(--panel-bg);
                    border-left: 1px solid var(--border-color);
                    padding: 20px;
                    display: flex;
                    flex-direction: column;
                    overflow-y: auto;
                }
                .preview-pane h2 {
                    font-size: 16px;
                    font-weight: 600;
                    margin-bottom: 15px;
                    color: var(--accent);
                    word-break: break-all;
                }
                .preview-content {
                    flex-grow: 1;
                    color: var(--text-muted);
                    font-size: 13px;
                }
                .preview-content pre {
                    background: rgba(0,0,0,0.3);
                    padding: 10px;
                    border-radius: 6px;
                    overflow-x: auto;
                    color: #a8b2d1;
                    white-space: pre-wrap;
                }
                .preview-content img {
                    max-width: 100%;
                    border-radius: 6px;
                    margin-top: 10px;
                }
                
                /* List View (Default) */
                #files.list-view {
                    display: flex;
                    flex-direction: column;
                    gap: 5px;
                }
                #files.list-view .item {
                    display: grid;
                    grid-template-columns: auto 1fr auto;
                    align-items: center;
                    gap: 15px;
                    padding: 12px 15px;
                    border-radius: 8px;
                    cursor: pointer;
                    transition: background 0.15s ease;
                    user-select: none;
                }
                #files.list-view .item i { font-style: normal; font-size: 20px; }
                #files.list-view .item-size { color: var(--text-muted); font-size: 12px; }
                
                /* Grid View */
                #files.grid-view {
                    display: grid;
                    grid-template-columns: repeat(auto-fill, minmax(120px, 1fr));
                    gap: 15px;
                }
                #files.grid-view .item {
                    display: flex;
                    flex-direction: column;
                    align-items: center;
                    justify-content: center;
                    text-align: center;
                    gap: 10px;
                    padding: 20px 10px;
                    border-radius: 8px;
                    cursor: pointer;
                    transition: background 0.15s ease;
                    user-select: none;
                    background: var(--panel-bg);
                    border: 1px solid transparent;
                }
                #files.grid-view .item:hover {
                    border-color: var(--border-color);
                }
                #files.grid-view .item i { font-style: normal; font-size: 40px; }
                #files.grid-view .item-size { display: none; }
                #files.grid-view .item-name { 
                    word-break: break-all;
                    display: -webkit-box;
                    -webkit-line-clamp: 2;
                    -webkit-box-orient: vertical;
                    overflow: hidden;
                }

                .item:hover { background: var(--hover-bg); }
                .item-name { font-weight: 500; font-size: 14px; }
                .folder .item-name { color: #74b9ff; }
                .file .item-name { color: #dfe6e9; }
                
                /* Animations */
                .item { animation: fadeIn 0.3s ease; }
                @keyframes fadeIn {
                    from { opacity: 0; transform: translateY(5px); }
                    to { opacity: 1; transform: translateY(0); }
                }
            </style>
        </head>
        <body>
            <header>
                <div class="nav-buttons">
                    <button onclick="goUp()" title="Go Up One Level">⬆ Up</button>
                    <button onclick="goHome()" title="Go Home">🏠 Home</button>
                </div>
                <div class="path-bar-container">
                    <div class="path-bar">
                        <input type="text" id="path" onkeypress="handlePathEnter(event)" placeholder="Enter path or choose location..." autocomplete="off" />
                        <button id="btn-path-dropdown" onclick="togglePathDropdown(event)" title="Browse drives and folders">▾</button>
                    </div>
                    <div id="path-dropdown" class="path-dropdown" style="display: none;"></div>
                </div>
                <div class="nav-buttons view-toggles">
                    <button id="btn-list" onclick="setView('list')" class="active" title="List View">☰</button>
                    <button id="btn-grid" onclick="setView('grid')" title="Grid View">⊞</button>
                    <button id="btn-preview" onclick="togglePreview()" class="active" title="Toggle Preview Pane" style="margin-left: 10px;">👁</button>
                </div>
            </header>
            <div class="search-bar-container" style="padding: 10px 24px; background: var(--bg-color); border-bottom: 1px solid var(--border-color);">
                <input type="text" id="search" placeholder="🔍 Search in current folder..." onkeyup="handleSearch()" style="width: 100%; padding: 8px 12px; border-radius: 6px; border: 1px solid var(--border-color); background: var(--panel-bg); color: var(--text-main); outline: none;" />
            </div>
            <div class="main-content">
                <div class="file-list-container">
                    <div id="files" class="list-view"></div>
                </div>
                <div class="preview-pane" id="preview-pane">
                    <h2>Preview</h2>
                    <div class="preview-content" id="preview-content">
                        Select a file to preview.
                    </div>
                </div>
            </div>

            <script>
                let currentPath = ".";
                let isWindows = false;
                let currentView = "list";
                let previewVisible = true;
                let currentItems = [];

                function setView(view) {
                    currentView = view;
                    const filesContainer = document.getElementById('files');
                    if (view === 'grid') {
                        filesContainer.className = 'grid-view';
                        document.getElementById('btn-grid').classList.add('active');
                        document.getElementById('btn-list').classList.remove('active');
                    } else {
                        filesContainer.className = 'list-view';
                        document.getElementById('btn-list').classList.add('active');
                        document.getElementById('btn-grid').classList.remove('active');
                    }
                }

                function togglePreview() {
                    previewVisible = !previewVisible;
                    const pane = document.getElementById('preview-pane');
                    const btn = document.getElementById('btn-preview');
                    if (previewVisible) {
                        pane.style.display = 'flex';
                        btn.classList.add('active');
                    } else {
                        pane.style.display = 'none';
                        btn.classList.remove('active');
                    }
                }

                function handleSearch() {
                    const q = document.getElementById('search').value.toLowerCase();
                    const filtered = currentItems.filter(i => i.name.toLowerCase().includes(q));
                    renderItems(filtered);
                }

                function renderItems(items) {
                    let html = '';
                    for (const item of items) {
                        const isDir = item.type === 'Directory';
                        const icon = isDir ? '📁' : '📄';
                        const className = isDir ? 'folder' : 'file';
                        const sizeText = !isDir ? formatBytes(item.size) : '';
                        
                        html += `
                            <div class="item ${className}" onclick="handleSingleClick('${item.name}', ${isDir})" ondblclick="handleDoubleClick('${item.name}', ${isDir})">
                                <i>${icon}</i>
                                <span class="item-name">${item.name}</span>
                                <span class="item-size">${sizeText}</span>
                            </div>
                        `;
                    }
                    document.getElementById('files').innerHTML = html;
                }

                async function loadFiles(targetPath) {
                    try {
                        const res = await fetch('/api/files/list?path=' + encodeURIComponent(targetPath));
                        if (!res.ok) {
                            alert('Error loading directory: Access Denied or Not Found');
                            return;
                        }
                        const data = await res.json();
                        currentPath = data.currentPath;
                        document.getElementById('path').value = currentPath;
                        isWindows = currentPath.includes('\\');
                        currentItems = data.items;
                        
                        document.getElementById('search').value = ''; // clear search
                        renderItems(currentItems);
                        
                        document.getElementById('preview-content').innerHTML = 'Select a file to preview.';
                        document.querySelector('#preview-pane h2').innerText = 'Preview';
                    } catch (err) {
                        console.error(err);
                    }
                }

                async function handleSingleClick(name, isDir) {
                    const sep = isWindows ? '\\' : '/';
                    const fullPath = currentPath.endsWith(sep) ? currentPath + name : currentPath + sep + name;
                    
                    document.querySelector('#preview-pane h2').innerText = name;
                    const previewContent = document.getElementById('preview-content');

                    if (isDir) {
                        previewContent.innerHTML = 'Folder selected.<br><br>Double-click to open.';
                        return;
                    }

                    const ext = name.split('.').pop().toLowerCase();
                    const imageExts = ['png', 'jpg', 'jpeg', 'gif', 'webp', 'svg'];
                    const textExts = ['txt', 'md', 'json', 'cs', 'js', 'html', 'css', 'xml', 'log', 'yaml', 'yml', 'sh', 'bat'];

                    previewContent.innerHTML = 'Loading preview...';

                    if (imageExts.includes(ext)) {
                        previewContent.innerHTML = `<img src="/api/files/raw?path=${encodeURIComponent(fullPath)}" alt="preview" />`;
                    } else if (textExts.includes(ext)) {
                        try {
                            const res = await fetch('/api/files/read?path=' + encodeURIComponent(fullPath));
                            if (!res.ok) {
                                previewContent.innerHTML = 'File too large or unreadable.';
                                return;
                            }
                            const text = await res.text();
                            previewContent.innerHTML = `<pre>${escapeHtml(text)}</pre>`;
                        } catch (err) {
                            previewContent.innerHTML = 'Error loading text preview.';
                        }
                    } else {
                        previewContent.innerHTML = 'Preview not available for this file type.<br>Double-click to open externally (coming soon).';
                    }
                }

                function escapeHtml(unsafe) {
                    return unsafe
                        .replace(/&/g, "&amp;")
                        .replace(/</g, "&lt;")
                        .replace(/>/g, "&gt;")
                        .replace(/"/g, "&quot;")
                        .replace(/'/g, "&#039;");
                }

                function handleDoubleClick(name, isDir) {
                    if (isDir) {
                        const sep = isWindows ? '\\' : '/';
                        const newPath = currentPath.endsWith(sep) ? currentPath + name : currentPath + sep + name;
                        loadFiles(newPath);
                    } else {
                        // In the future, this will trigger the read_file API and show a preview modal
                        alert("File preview coming soon!");
                    }
                }

                function goUp() {
                    const sep = isWindows ? '\\' : '/';
                    if (!isWindows && currentPath === '/') return; // Already at root

                    const parts = currentPath.split(sep);
                    parts.pop();
                    let newPath = parts.join(sep);
                    
                    if (newPath === '') newPath = sep; // Fix for Mac/Linux root
                    if (isWindows && newPath.endsWith(':')) newPath += '\\';
                    
                    loadFiles(newPath);
                }

                function goHome() {
                    loadFiles('.');
                }

                function handlePathEnter(e) {
                    if (e.key === 'Enter') {
                        loadFiles(e.target.value);
                    }
                }

                function formatBytes(bytes) {
                    if (bytes === 0) return '0 B';
                    const k = 1024;
                    const sizes = ['B', 'KB', 'MB', 'GB'];
                    const i = Math.floor(Math.log(bytes) / Math.log(k));
                    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
                }

                let systemRoots = [];

                async function fetchRoots() {
                    try {
                        const res = await fetch('/api/files/roots');
                        if (res.ok) {
                            systemRoots = await res.json();
                        }
                    } catch (e) {
                        console.error('Failed to load roots', e);
                    }
                }

                function togglePathDropdown(e) {
                    if (e) e.stopPropagation();
                    const dropdown = document.getElementById('path-dropdown');
                    if (dropdown.style.display === 'none' || !dropdown.style.display) {
                        openPathDropdown();
                    } else {
                        closePathDropdown();
                    }
                }

                function openPathDropdown() {
                    const dropdown = document.getElementById('path-dropdown');
                    renderPathDropdown();
                    dropdown.style.display = 'flex';
                }

                function closePathDropdown() {
                    const dropdown = document.getElementById('path-dropdown');
                    dropdown.style.display = 'none';
                }

                function renderPathDropdown() {
                    const dropdown = document.getElementById('path-dropdown');
                    let html = '';

                    // 1. Drives & System Locations
                    if (systemRoots && systemRoots.length > 0) {
                        html += '<div class="dropdown-section-title">Drives & System Roots</div>';
                        for (const root of systemRoots) {
                            const icon = root.type === 'Drive' ? '💻' : root.type === 'Home' ? '🏠' : root.type === 'Desktop' ? '🖥️' : '📁';
                            html += `
                                <div class="dropdown-item" onclick="selectDropdownPath('${escapePath(root.path)}')">
                                    <i>${icon}</i>
                                    <span>${escapeHtml(root.name)}</span>
                                    <span class="item-sub">${escapeHtml(root.path)}</span>
                                </div>
                            `;
                        }
                    }

                    // 2. Folder Hierarchy / Breadcrumb trail
                    const sep = isWindows ? '\\' : '/';
                    if (currentPath && currentPath !== '.') {
                        const parts = currentPath.split(sep).filter(p => p !== '');
                        if (parts.length > 0) {
                            html += '<div class="dropdown-divider"></div>';
                            html += '<div class="dropdown-section-title">Folder Hierarchy</div>';
                            
                            // Root level
                            const rootPath = isWindows ? (currentPath.slice(0, 3)) : '/';
                            html += `
                                <div class="dropdown-item" onclick="selectDropdownPath('${escapePath(rootPath)}')">
                                    <i>📁</i>
                                    <span>${isWindows ? rootPath : '/ (Root)'}</span>
                                    <span class="item-sub">${escapeHtml(rootPath)}</span>
                                </div>
                            `;

                            let accum = isWindows ? rootPath : '';
                            for (let i = 0; i < parts.length; i++) {
                                if (isWindows && i === 0 && parts[0].endsWith(':')) continue;
                                accum = isWindows ? (accum.endsWith('\\') ? accum + parts[i] : accum + '\\' + parts[i]) : accum + '/' + parts[i];
                                const isCurrent = (accum === currentPath);
                                html += `
                                    <div class="dropdown-item" style="padding-left: ${(i + 2) * 12}px; ${isCurrent ? 'font-weight: 600; color: #74b9ff;' : ''}" onclick="selectDropdownPath('${escapePath(accum)}')">
                                        <i>📂</i>
                                        <span>${escapeHtml(parts[i])}${isCurrent ? ' (Current)' : ''}</span>
                                        <span class="item-sub">${escapeHtml(accum)}</span>
                                    </div>
                                `;
                            }
                        }
                    }

                    // 3. Child Folders in Current Directory
                    const subdirs = currentItems.filter(i => i.type === 'Directory');
                    if (subdirs.length > 0) {
                        html += '<div class="dropdown-divider"></div>';
                        html += '<div class="dropdown-section-title">Subfolders in Current Directory</div>';
                        for (const sub of subdirs) {
                            const subPath = currentPath.endsWith(sep) ? currentPath + sub.name : currentPath + sep + sub.name;
                            html += `
                                <div class="dropdown-item" onclick="selectDropdownPath('${escapePath(subPath)}')">
                                    <i>📁</i>
                                    <span>${escapeHtml(sub.name)}</span>
                                </div>
                            `;
                        }
                    }

                    dropdown.innerHTML = html;
                }

                function escapePath(path) {
                    return path.replace(/\\/g, '\\\\').replace(/'/g, "\\'");
                }

                function selectDropdownPath(targetPath) {
                    closePathDropdown();
                    loadFiles(targetPath);
                }

                // Close dropdown when clicking outside
                document.addEventListener('click', function(e) {
                    const container = document.querySelector('.path-bar-container');
                    if (container && !container.contains(e.target)) {
                        closePathDropdown();
                    }
                });

                // Escape key closes dropdown
                document.addEventListener('keydown', function(e) {
                    if (e.key === 'Escape') {
                        closePathDropdown();
                    }
                });

                // Initial load
                fetchRoots();
                loadFiles('.');
            </script>
        </body>
        </html>
        """;
    }
}
