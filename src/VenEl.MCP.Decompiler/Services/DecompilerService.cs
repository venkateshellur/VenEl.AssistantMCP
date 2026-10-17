using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using VenEl.MCP.Decompiler.Models;

namespace VenEl.MCP.Decompiler.Services;

public class DecompilerService : IDecompilerService
{
    public bool CanDecompile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return false;
        return DotNetDecompilerEngine.IsSupported(filePath) 
            || JavaDecompilerEngine.IsSupported(filePath)
            || WasmDecompilerEngine.IsSupported(filePath)
            || NativeBinaryEngine.IsSupported(filePath);
    }

    public async Task<DecompileResult> DecompileAsync(string filePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return new DecompileResult
            {
                IsSuccess = false,
                ErrorMessage = "File path is required."
            };
        }

        if (!File.Exists(filePath))
        {
            return new DecompileResult
            {
                Path = filePath,
                FileName = Path.GetFileName(filePath),
                IsSuccess = false,
                ErrorMessage = $"File not found: {filePath}"
            };
        }

        if (DotNetDecompilerEngine.IsSupported(filePath))
        {
            var res = await DotNetDecompilerEngine.DecompileAsync(filePath, ct);
            // If it turned out to be a native PE binary, fall back to NativeBinaryEngine
            if (!res.IsSuccess && res.Format.StartsWith("Native PE Binary", StringComparison.OrdinalIgnoreCase))
            {
                return await NativeBinaryEngine.DecompileAsync(filePath, ct);
            }
            return res;
        }

        if (JavaDecompilerEngine.IsSupported(filePath))
        {
            return await JavaDecompilerEngine.DecompileAsync(filePath, ct);
        }

        if (WasmDecompilerEngine.IsSupported(filePath))
        {
            return await WasmDecompilerEngine.DecompileAsync(filePath, ct);
        }

        if (NativeBinaryEngine.IsSupported(filePath))
        {
            return await NativeBinaryEngine.DecompileAsync(filePath, ct);
        }

        return new DecompileResult
        {
            Path = filePath,
            FileName = Path.GetFileName(filePath),
            Format = "Unsupported Binary",
            IsSuccess = false,
            ErrorMessage = $"Unsupported file format: {Path.GetExtension(filePath)}. Supported formats are .NET assemblies (.dll, .exe), Java (.class, .jar), WebAssembly (.wasm), and Native binaries (.dylib, .so, native .dll)."
        };
    }

    public async Task<ExportProjectResult> ExportProjectAsync(string filePath, string? destinationDir = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return new ExportProjectResult
            {
                IsSuccess = false,
                ErrorMessage = "File path is required."
            };
        }

        if (!File.Exists(filePath))
        {
            return new ExportProjectResult
            {
                IsSuccess = false,
                ErrorMessage = $"File not found: {filePath}"
            };
        }

        if (DotNetDecompilerEngine.IsSupported(filePath))
        {
            return await DotNetDecompilerEngine.ExportProjectAsync(filePath, destinationDir, ct);
        }

        if (JavaDecompilerEngine.IsSupported(filePath))
        {
            return await JavaDecompilerEngine.ExportProjectAsync(filePath, destinationDir, ct);
        }

        if (WasmDecompilerEngine.IsSupported(filePath))
        {
            return await WasmDecompilerEngine.ExportProjectAsync(filePath, destinationDir, ct);
        }

        if (NativeBinaryEngine.IsSupported(filePath))
        {
            return await NativeBinaryEngine.ExportProjectAsync(filePath, destinationDir, ct);
        }

        return new ExportProjectResult
        {
            IsSuccess = false,
            ErrorMessage = $"Export to project is not supported for {Path.GetExtension(filePath)}."
        };
    }
}
