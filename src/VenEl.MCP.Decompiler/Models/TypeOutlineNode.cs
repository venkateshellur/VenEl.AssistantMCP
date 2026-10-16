using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VenEl.MCP.Decompiler.Models;

public class TypeOutlineNode
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "class"; // namespace, class, interface, struct, enum, record, method, property, field

    [JsonPropertyName("signature")]
    public string? Signature { get; set; }

    [JsonPropertyName("children")]
    public List<TypeOutlineNode> Children { get; set; } = new();
}
