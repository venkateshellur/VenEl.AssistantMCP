using System.Text.Json.Serialization;

namespace VenEl.MCP.Decompiler.Configuration;

public class DecompilerOptions
{
    public const string SectionName = "Decompiler";

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("maxDecompiledChars")]
    public int MaxDecompiledChars { get; set; } = 500000;
}
