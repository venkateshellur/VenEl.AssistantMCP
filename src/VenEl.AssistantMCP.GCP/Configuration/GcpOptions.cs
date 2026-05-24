namespace VenEl.AssistantMCP.GCP.Configuration;

public class GcpOptions
{
    public string? ProjectId { get; set; }
    public string? CredentialsPath { get; set; }
    public bool AllowResourceDeletion { get; set; } = false;
}
