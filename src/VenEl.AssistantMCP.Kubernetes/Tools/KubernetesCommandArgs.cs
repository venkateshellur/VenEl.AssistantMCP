using System.Text.Json.Serialization;

namespace VenEl.AssistantMCP.Kubernetes.Tools;

public sealed class KubernetesCommandArgs
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("namespace")]
    public string? Namespace { get; set; }

    [JsonPropertyName("resourceName")]
    public string? ResourceName { get; set; }
}
