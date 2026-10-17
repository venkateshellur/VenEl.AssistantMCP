using System.Text.Json.Serialization;

namespace VenEl.MCP.Decompiler.Tools;

public sealed class DecompilerCommandArgs
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("includeCode")]
    public bool IncludeCode { get; set; } = true;

    [JsonPropertyName("destinationDir")]
    public string? DestinationDir { get; set; }
}
