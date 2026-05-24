using System.Text.Json.Serialization;

namespace VenEl.AssistantMCP.LocalOffice.Tools;

public sealed class LocalOfficeCommandArgs
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("filePath")]
    public string? FilePath { get; set; }

    [JsonPropertyName("sheetName")]
    public string? SheetName { get; set; }

    [JsonPropertyName("cellAddress")]
    public string? CellAddress { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }

    [JsonPropertyName("jsonData")]
    public string? JsonData { get; set; }

    [JsonPropertyName("includeHeaders")]
    public bool? IncludeHeaders { get; set; }

    [JsonPropertyName("clearExisting")]
    public bool? ClearExisting { get; set; }

    [JsonPropertyName("newSheetName")]
    public string? NewSheetName { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("slideIndex")]
    public int? SlideIndex { get; set; }
}
