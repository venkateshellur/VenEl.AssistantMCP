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
        return DotNetDecompilerEngine.IsSupported(filePath) || JavaDecompilerEngine.IsSupported(filePath);
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
            return await DotNetDecompilerEngine.DecompileAsync(filePath, ct);
        }

        if (JavaDecompilerEngine.IsSupported(filePath))
        {
            return await JavaDecompilerEngine.DecompileAsync(filePath, ct);
        }

        return new DecompileResult
        {
            Path = filePath,
            FileName = Path.GetFileName(filePath),
            Format = "Unsupported Binary",
            IsSuccess = false,
            ErrorMessage = $"Unsupported file format: {Path.GetExtension(filePath)}. Supported formats are .NET assemblies (.dll, .exe) and Java binaries (.class, .jar)."
        };
    }
}
