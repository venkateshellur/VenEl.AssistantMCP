<#
.SYNOPSIS
    Builds and publishes VenEl.MCPAssistant to the ./publish folder,
    then ensures both Antigravity and project MCP config files are correct.

.USAGE
    .\publish.ps1
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$IsWindows = $env:OS -eq "Windows_NT"

$SolutionRoot   = $PSScriptRoot
$ServerCsproj   = Join-Path $SolutionRoot "src\VenEl.AssistantMCP.Server\VenEl.AssistantMCP.Server.csproj"

if ($IsWindows) {
    $PublishDir        = "C:\Venky\MCPs\VenEl.MCPAssistant"
    $ServerExe         = Join-Path $PublishDir "VenEl.AssistantMCP.Server.exe"
    $AntigravityConfig = Join-Path $env:USERPROFILE ".gemini\antigravity\mcp-config.json"
    $McpCommand        = $ServerExe.Replace('\', '\\')
    $McpArgs           = "[]"
} else {
    $PublishDir        = Join-Path $SolutionRoot "publish"
    $ServerExe         = Join-Path $PublishDir "VenEl.AssistantMCP.Server"
    $AntigravityConfig = Join-Path $HOME ".gemini\antigravity\mcp-config.json"
    $McpCommand        = "dotnet"
    $DllPath           = Join-Path $PublishDir "VenEl.AssistantMCP.Server.dll"
    $McpArgs           = '["' + $DllPath.Replace('\', '\\') + '"]'
}

$ProjectConfig     = Join-Path $SolutionRoot "mcp_config.json"

# ── Step 1: Stop the running server ──────────────────────────────────────────
Write-Host "Stopping any running MCP server instances..." -ForegroundColor Cyan
if ($IsWindows) {
    Stop-Process -Name "VenEl.AssistantMCP.Server" -Force -ErrorAction SilentlyContinue
} else {
    # Simple kill for Mac/Linux if running via dotnet
    Get-Process | Where-Object { $_.ProcessName -eq "dotnet" -and $_.CommandLine -match "VenEl.AssistantMCP.Server.dll" } | Stop-Process -Force -ErrorAction SilentlyContinue
}
Start-Sleep -Milliseconds 500

# ── Step 2: Publish ───────────────────────────────────────────────────────────
$AppsettingsDest = Join-Path $PublishDir "appsettings.json"
$AppsettingsBackup = Join-Path $PublishDir "appsettings.json.bak"
$appsettingsExisted = $false

if (Test-Path $AppsettingsDest) {
    Write-Host "Backing up existing appsettings.json..." -ForegroundColor Yellow
    Copy-Item -Path $AppsettingsDest -Destination $AppsettingsBackup -Force
    $appsettingsExisted = $true
}

Write-Host "Publishing to: $PublishDir" -ForegroundColor Cyan
dotnet publish $ServerCsproj -c Release -o $PublishDir --self-contained true
if ($LASTEXITCODE -ne 0) {
    Write-Error "Publish failed (exit code $LASTEXITCODE)."
    exit $LASTEXITCODE
}

if ($appsettingsExisted) {
    Write-Host "Restoring existing appsettings.json..." -ForegroundColor Yellow
    Copy-Item -Path $AppsettingsBackup -Destination $AppsettingsDest -Force
    Remove-Item -Path $AppsettingsBackup -Force
}

Write-Host "Publish succeeded." -ForegroundColor Green

# ── Step 3: Build MCP config JSON ────────────────────────────────────────────
$ConfigJson = '{' + [System.Environment]::NewLine +
    '  "mcpServers": {' + [System.Environment]::NewLine +
    '    "venel": {' + [System.Environment]::NewLine +
    '      "command": "' + $McpCommand + '",' + [System.Environment]::NewLine +
    '      "args": ' + $McpArgs + [System.Environment]::NewLine +
    '    }' + [System.Environment]::NewLine +
    '  }' + [System.Environment]::NewLine +
    '}'

# ── Step 4: Update MCP config files if needed ─────────────────────────────────
foreach ($ConfigPath in @($AntigravityConfig, $ProjectConfig)) {
    $needsUpdate = $true
    if (Test-Path $ConfigPath) {
        $current = (Get-Content $ConfigPath -Raw).Trim()
        if ($current -eq $ConfigJson.Trim()) { $needsUpdate = $false }
    }

    if ($needsUpdate) {
        Write-Host "Updating MCP config: $ConfigPath" -ForegroundColor Yellow
        $ConfigJson | Set-Content $ConfigPath -Encoding UTF8
    } else {
        Write-Host "MCP config already up to date: $(Split-Path $ConfigPath -Leaf)" -ForegroundColor DarkGray
    }
}

# ── Done ──────────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "Ready! Restart your MCP client to pick up the new server." -ForegroundColor Green
Write-Host "Server: $ServerExe"
