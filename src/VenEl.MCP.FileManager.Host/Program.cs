using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VenEl.MCP.FileManager.Extensions;
using VenEl.MCP.Decompiler.Extensions;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddFileManagerFeature(context.Configuration);
        services.AddDecompilerFeature(context.Configuration);
    })
    .Build();

Console.WriteLine("VenEl File Manager & Decompiler engine started.");
await host.RunAsync();
