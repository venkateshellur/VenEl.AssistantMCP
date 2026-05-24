namespace VenEl.AssistantMCP.Kubernetes.Configuration;

public class KubernetesOptions
{
    public const string SectionName = "Kubernetes";

    public bool AllowDestructiveOperations { get; set; } = false;
}
