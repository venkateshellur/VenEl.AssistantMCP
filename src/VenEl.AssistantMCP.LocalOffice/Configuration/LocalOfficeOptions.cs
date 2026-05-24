namespace VenEl.AssistantMCP.LocalOffice.Configuration;

public class LocalOfficeOptions
{
    public const string SectionName = "LocalOffice";

    public bool AllowFileDeletion { get; set; } = false;
    public bool AllowFileOverwrite { get; set; } = false;
}
