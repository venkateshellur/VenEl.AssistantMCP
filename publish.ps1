<#
.SYNOPSIS
    Builds and publishes VenEl.MCPAssistant to the ./publish folder,
    then ensures both Antigravity and project MCP config files are correct.

.USAGE
    .\publish.ps1
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$SolutionRoot   = $PSScriptRoot
$ServerCsproj   = Join-Path $SolutionRoot "src\VenEl.MCPAssistant.Server\VenEl.MCPAssistant.Server.csproj"
$PublishDir     = Join-Path $SolutionRoot "publish"
$ServerExe      = Join-Path $PublishDir "VenEl.MCPAssistant.Server.exe"

$AntigravityConfig = "$env:USERPROFILE\.gemini\antigravity\mcp_config.json"
$ProjectConfig     = Join-Path $SolutionRoot "mcp_config.json"

# ── Step 1: Stop the running server ──────────────────────────────────────────
Write-Host "Stopping any running MCP server instances..." -ForegroundColor Cyan
Stop-Process -Name "VenEl.MCPAssistant.Server" -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500

# ── Step 2: Publish ───────────────────────────────────────────────────────────
Write-Host "Publishing to: $PublishDir" -ForegroundColor Cyan
dotnet publish $ServerCsproj -c Release -o $PublishDir --no-self-contained
if ($LASTEXITCODE -ne 0) {
    Write-Error "Publish failed (exit code $LASTEXITCODE)."
    exit $LASTEXITCODE
}
Write-Host "Publish succeeded." -ForegroundColor Green

# ── Step 3: Build MCP config JSON ────────────────────────────────────────────
$ExeEscaped = $ServerExe.Replace('\', '\\')
$ConfigJson = '{' + [System.Environment]::NewLine +
    '  "mcpServers": {' + [System.Environment]::NewLine +
    '    "venel": {' + [System.Environment]::NewLine +
    '      "command": "' + $ExeEscaped + '",' + [System.Environment]::NewLine +
    '      "args": []' + [System.Environment]::NewLine +
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
