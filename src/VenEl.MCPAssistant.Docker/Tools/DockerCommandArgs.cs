using System.Text.Json.Serialization;

namespace VenEl.MCPAssistant.Docker.Tools;

public sealed class DockerCommandArgs
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("containerId")]
    public string? ContainerId { get; set; }

    [JsonPropertyName("imageId")]
    public string? ImageId { get; set; }

    [JsonPropertyName("lines")]
    public int? Lines { get; set; }

    [JsonPropertyName("all")]
    public bool? All { get; set; }

    [JsonPropertyName("composeFilePath")]
    public string? ComposeFilePath { get; set; }

    [JsonPropertyName("dockerfilePath")]
    public string? DockerfilePath { get; set; }

    [JsonPropertyName("imageTag")]
    public string? ImageTag { get; set; }
}
