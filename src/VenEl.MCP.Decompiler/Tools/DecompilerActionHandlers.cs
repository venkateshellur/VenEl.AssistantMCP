using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VenEl.MCP.Core.Dispatcher;
using VenEl.MCP.Decompiler.Services;

namespace VenEl.MCP.Decompiler.Tools;

public class DecompileActionHandler : IActionHandler<DecompilerCommandArgs>
{
    private readonly IDecompilerService _decompilerService;

    public DecompileActionHandler(IDecompilerService decompilerService)
    {
        _decompilerService = decompilerService;
    }

    public string ActionName => "decompile";

    public string? Validate(DecompilerCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Path))
            return "Path is required.";
        return null;
    }

    public async Task<string> HandleAsync(DecompilerCommandArgs args, CancellationToken ct)
    {
        var result = await _decompilerService.DecompileAsync(args.Path!, ct);
        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}

public class GetOutlineActionHandler : IActionHandler<DecompilerCommandArgs>
{
    private readonly IDecompilerService _decompilerService;

    public GetOutlineActionHandler(IDecompilerService decompilerService)
    {
        _decompilerService = decompilerService;
    }

    public string ActionName => "get_outline";

    public string? Validate(DecompilerCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Path))
            return "Path is required.";
        return null;
    }

    public async Task<string> HandleAsync(DecompilerCommandArgs args, CancellationToken ct)
    {
        var result = await _decompilerService.DecompileAsync(args.Path!, ct);
        // Omit massive decompiled code for fast outline inspection
        result.DecompiledCode = "// Code omitted for outline inspection. Use action 'decompile' for full source.";
        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}

public class GetMetadataActionHandler : IActionHandler<DecompilerCommandArgs>
{
    private readonly IDecompilerService _decompilerService;

    public GetMetadataActionHandler(IDecompilerService decompilerService)
    {
        _decompilerService = decompilerService;
    }

    public string ActionName => "get_metadata";

    public string? Validate(DecompilerCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Path))
            return "Path is required.";
        return null;
    }

    public async Task<string> HandleAsync(DecompilerCommandArgs args, CancellationToken ct)
    {
        var result = await _decompilerService.DecompileAsync(args.Path!, ct);
        var meta = new
        {
            result.Path,
            result.FileName,
            result.Format,
            result.TargetFramework,
            result.Architecture,
            result.Version,
            result.DeterministicSummary,
            result.Dependencies,
            result.IsSuccess,
            result.ErrorMessage
        };
        return JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true });
    }
}

public class ExportProjectActionHandler : IActionHandler<DecompilerCommandArgs>
{
    private readonly IDecompilerService _decompilerService;

    public ExportProjectActionHandler(IDecompilerService decompilerService)
    {
        _decompilerService = decompilerService;
    }

    public string ActionName => "export_project";

    public string? Validate(DecompilerCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Path))
            return "Path is required.";
        return null;
    }

    public async Task<string> HandleAsync(DecompilerCommandArgs args, CancellationToken ct)
    {
        var result = await _decompilerService.ExportProjectAsync(args.Path!, args.DestinationDir, ct);
        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}

public class DisassembleILActionHandler : IActionHandler<DecompilerCommandArgs>
{
    private readonly IDecompilerService _decompilerService;

    public DisassembleILActionHandler(IDecompilerService decompilerService)
    {
        _decompilerService = decompilerService;
    }

    public string ActionName => "disassemble_il";

    public string? Validate(DecompilerCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Path))
            return "Path is required.";
        return null;
    }

    public async Task<string> HandleAsync(DecompilerCommandArgs args, CancellationToken ct)
    {
        var result = await _decompilerService.DecompileAsync(args.Path!, ct);
        var ilResult = new
        {
            result.Path,
            result.FileName,
            result.Format,
            result.Architecture,
            IntermediateLanguage = result.IntermediateLanguageCode ?? "// No IL/bytecode disassembly available.",
            result.IsSuccess,
            result.ErrorMessage
        };
        return JsonSerializer.Serialize(ilResult, new JsonSerializerOptions { WriteIndented = true });
    }
}
