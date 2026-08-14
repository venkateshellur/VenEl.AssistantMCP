using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VenEl.AssistantMCP.Core.Dispatcher;
using VenEl.AssistantMCP.WebAutomator.Extensions;
using VenEl.AssistantMCP.WebAutomator.Tools;

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole());
services.AddWebAutomator();

var sp = services.BuildServiceProvider();

// The tool is normally instantiated by the MCP registry, so we just instantiate it manually here for testing.
var dispatcher = new WebAutomatorDispatcherTool(sp);

Console.WriteLine("Navigating to example.com...");
try {
    var result = await dispatcher.ExecuteAsync(new WebAutomatorArgs { Action = "web_navigate", Url = "https://example.com" }, CancellationToken.None);
    Console.WriteLine("=== RESULT ===");
    Console.WriteLine(result);
    Console.WriteLine("==============");
    
    Console.WriteLine("\nTesting JS Evaluation...");
    var jsResult = await dispatcher.ExecuteAsync(new WebAutomatorArgs { Action = "web_evaluate", Script = "1 + 2" }, CancellationToken.None);
    Console.WriteLine("1 + 2 = " + jsResult);
} catch (Exception ex) {
    Console.WriteLine("Error: " + ex.ToString());
}
