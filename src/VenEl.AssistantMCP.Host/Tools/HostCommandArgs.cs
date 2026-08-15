using System.Text.Json.Serialization;

namespace VenEl.AssistantMCP.Host.Tools;

public sealed class HostCommandArgs
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("filePath")]
    public string? FilePath { get; set; }

    [JsonPropertyName("fileContent")]
    public string? FileContent { get; set; }

    [JsonPropertyName("command")]
    public string? Command { get; set; }
}
