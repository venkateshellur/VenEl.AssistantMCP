namespace VenEl.MCPAssistant.Docker.Configuration;

public class DockerOptions
{
    public const string SectionName = "Docker";

    public bool AllowDestructiveOperations { get; set; } = false;
}
