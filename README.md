# VenEl MCP Assistant

A **Model Context Protocol (MCP) STDIO server** built with **.NET 10** and the official [`ModelContextProtocol`](https://www.nuget.org/packages/ModelContextProtocol) SDK.

## Solution Structure

```
VenEl.MCPAssistant/
├── VenEl.MCPAssistant.sln
└── src/
    ├── VenEl.MCPAssistant.Server/   ← STDIO MCP Server (console app)
    └── VenEl.MCPAssistant.Core/     ← Business-logic class library (tools live here)
```

## How It Works

```
Claude / Cursor / any MCP Host
        │  STDIO (JSON-RPC 2.0)
        ▼
VenEl.MCPAssistant.Server  ←── references ───  VenEl.MCPAssistant.Core
  (transport only)                              (all tools / resources / prompts)
```

## Running Locally

```bash
dotnet run --project src/VenEl.MCPAssistant.Server
```

## Claude Desktop Integration

Add to `~/Library/Application Support/Claude/claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "venel-mcp-assistant": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/Users/venkateshellur/Venky/Git/Venel.MCPAssistant/VenEl.MCPAssistant/src/VenEl.MCPAssistant.Server",
        "--configuration", "Release"
      ]
    }
  }
}
```

Or publish a self-contained binary and point `command` at the executable instead.

## Adding Functionality

All new tools, resources, and prompts go into `VenEl.MCPAssistant.Core`. See [`src/VenEl.MCPAssistant.Core/README.md`](src/VenEl.MCPAssistant.Core/README.md) for details.
