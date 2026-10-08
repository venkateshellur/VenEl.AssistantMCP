using System.Text.Json.Serialization;

namespace VenEl.MCP.FileManager.Tools;

public class FileManagerCommandArgs
{
    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("destinationPath")]
    public string? DestinationPath { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("forceExternal")]
    public bool ForceExternal { get; set; }
}
