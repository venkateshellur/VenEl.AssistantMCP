using System.Text.Json.Serialization;

namespace VenEl.MCP.Email.Tools;

public class EmailCommandArgs
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("to")]
    public string? To { get; set; }

    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("isHtml")]
    public bool IsHtml { get; set; } = false;

    [JsonPropertyName("maxCount")]
    public int MaxCount { get; set; } = 10;

    [JsonPropertyName("folderName")]
    public string? FolderName { get; set; }

    [JsonPropertyName("searchSubject")]
    public string? SearchSubject { get; set; }

    [JsonPropertyName("searchSender")]
    public string? SearchSender { get; set; }

    [JsonPropertyName("searchBody")]
    public string? SearchBody { get; set; }

    [JsonPropertyName("searchLocation")]
    public string? SearchLocation { get; set; }

    [JsonPropertyName("entryId")]
    public string? EntryId { get; set; }

    [JsonPropertyName("eventStart")]
    public DateTime? EventStart { get; set; }

    [JsonPropertyName("eventEnd")]
    public DateTime? EventEnd { get; set; }

    [JsonPropertyName("eventLocation")]
    public string? EventLocation { get; set; }

    [JsonPropertyName("manageAction")]
    public string? ManageAction { get; set; }
}
