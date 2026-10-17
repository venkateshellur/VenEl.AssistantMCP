using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VenEl.MCP.Decompiler.Models;

public class DecompileResult
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("format")]
    public string Format { get; set; } = string.Empty; // .NET Assembly, Java Class, Java JAR

    [JsonPropertyName("targetFramework")]
    public string? TargetFramework { get; set; }

    [JsonPropertyName("architecture")]
    public string? Architecture { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("deterministicSummary")]
    public string DeterministicSummary { get; set; } = string.Empty;

    [JsonPropertyName("dependencies")]
    public List<string> Dependencies { get; set; } = new();

    [JsonPropertyName("outline")]
    public List<TypeOutlineNode> Outline { get; set; } = new();

    [JsonPropertyName("decompiledCode")]
    public string DecompiledCode { get; set; } = string.Empty;

    [JsonPropertyName("intermediateLanguageCode")]
    public string? IntermediateLanguageCode { get; set; }

    [JsonPropertyName("canExportProject")]
    public bool CanExportProject { get; set; } = true;

    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; set; } = true;

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
}
