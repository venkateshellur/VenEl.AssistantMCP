namespace VenEl.AssistantMCP.Docker.Configuration;

public class DockerOptions
{
    public const string SectionName = "Docker";

    public bool AllowDestructiveOperations { get; set; } = false;
}
