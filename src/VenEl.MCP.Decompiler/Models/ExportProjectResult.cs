using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VenEl.MCP.Decompiler.Models;

public class ExportProjectResult
{
    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; set; } = true;

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("projectDirectory")]
    public string ProjectDirectory { get; set; } = string.Empty;

    [JsonPropertyName("projectFilePath")]
    public string? ProjectFilePath { get; set; }

    [JsonPropertyName("filesWritten")]
    public int FilesWritten { get; set; }

    [JsonPropertyName("generatedFiles")]
    public List<string> GeneratedFiles { get; set; } = new();

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;
}
