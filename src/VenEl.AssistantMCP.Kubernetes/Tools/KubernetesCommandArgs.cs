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

    [JsonPropertyName("resourceType")]
    public string? ResourceType { get; set; }

    [JsonPropertyName("manifestYaml")]
    public string? ManifestYaml { get; set; }

    [JsonPropertyName("releaseName")]
    public string? ReleaseName { get; set; }

    [JsonPropertyName("chartName")]
    public string? ChartName { get; set; }
}
