# VenEl.MCPAssistant.Core

This class library holds all **business-logic** that the MCP Server exposes as tools, resources, and prompts.

## Adding a New Tool

1. Create a new class (or folder) in this project.
2. Decorate the class with `[McpServerToolType]`.
3. Decorate each method with `[McpServerTool]` and add `[Description("…")]`.
4. In **VenEl.MCPAssistant.Server / Program.cs** call `.WithTools<YourToolClass>()` (or `.WithTools()` to auto-discover all marked types).

## Example skeleton

```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;

namespace VenEl.MCPAssistant.Core.Tools;

[McpServerToolType]
public static class SampleTool
{
    [McpServerTool, Description("Returns a greeting message.")]
    public static string Greet(string name) => $"Hello, {name}!";
}
```
