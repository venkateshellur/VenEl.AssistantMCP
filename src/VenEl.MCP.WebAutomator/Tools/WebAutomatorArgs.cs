using System.ComponentModel;
using System.Text.Json.Serialization;

namespace VenEl.MCP.WebAutomator.Tools;

public sealed class WebAutomatorArgs
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("selector")]
    public string? Selector { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("script")]
    public string? Script { get; set; }

    [JsonPropertyName("outputPath")]
    public string? OutputPath { get; set; }

    [JsonPropertyName("extractAttribute")]
    public string? ExtractAttribute { get; set; }
}
