using System.Text.Json.Serialization;

namespace VenEl.MCP.FileManager.Configuration;

public class FileManagerOptions
{
    public const string SectionName = "FileManager";

    [JsonPropertyName("webPort")]
    public int WebPort { get; set; } = 5050;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;
}
