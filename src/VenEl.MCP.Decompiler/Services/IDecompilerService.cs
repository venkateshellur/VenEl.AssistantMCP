using System.Threading;
using System.Threading.Tasks;
using VenEl.MCP.Decompiler.Models;

namespace VenEl.MCP.Decompiler.Services;

public interface IDecompilerService
{
    bool CanDecompile(string filePath);
    Task<DecompileResult> DecompileAsync(string filePath, CancellationToken ct = default);
}
