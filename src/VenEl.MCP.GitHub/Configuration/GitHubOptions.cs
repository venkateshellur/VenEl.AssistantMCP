namespace VenEl.MCP.GitHub.Configuration;

public class GitHubOptions
{
    public string? PatToken { get; set; }
    public bool AllowDestructiveActions { get; set; } = false;
}
