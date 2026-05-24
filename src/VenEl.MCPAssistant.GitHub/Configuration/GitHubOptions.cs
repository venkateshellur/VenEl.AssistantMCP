namespace VenEl.MCPAssistant.GitHub.Configuration;

public class GitHubOptions
{
    public string? PatToken { get; set; }
    public bool AllowDestructiveActions { get; set; } = false;
}
