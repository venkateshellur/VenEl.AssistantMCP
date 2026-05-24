namespace VenEl.AssistantMCP.Logging.Configuration;

public class FileLoggerOptions
{
    public const string SectionName = "LoggingFeature";

    public string LogDirectory { get; set; } = "logs";
    public string LogFileNamePrefix { get; set; } = "mcp-server";
    public int RetainedFileCount { get; set; } = 7;
}
