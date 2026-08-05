using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace VenEl.AssistantMCP.Core.Configuration;

public class AppSettingsUpdater
{
    private readonly string _appSettingsPath;

    public AppSettingsUpdater()
    {
        _appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    }

    /// <summary>
    /// Updates or adds a section in appsettings.json with the provided values.
    /// Preserves existing unrelated values and sections.
    /// </summary>
    public void UpdateSection(string sectionName, Dictionary<string, object?> values)
    {
        JsonObject rootObject;

        if (File.Exists(_appSettingsPath))
        {
            var jsonString = File.ReadAllText(_appSettingsPath);
            try
            {
                var document = JsonSerializer.Deserialize<JsonObject>(jsonString, new JsonSerializerOptions
                {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });
                rootObject = document ?? new JsonObject();
            }
            catch (JsonException)
            {
                // Fallback to empty if file is corrupt
                rootObject = new JsonObject();
            }
        }
        else
        {
            rootObject = new JsonObject();
        }

        JsonObject sectionNode;
        if (rootObject.TryGetPropertyValue(sectionName, out var existingSection) && existingSection is JsonObject jsonObject)
        {
            sectionNode = jsonObject;
        }
        else
        {
            sectionNode = new JsonObject();
            rootObject[sectionName] = sectionNode;
        }

        foreach (var kvp in values)
        {
            if (kvp.Value == null)
            {
                sectionNode.Remove(kvp.Key);
            }
            else if (kvp.Value is string s)
            {
                sectionNode[kvp.Key] = s;
            }
            else if (kvp.Value is bool b)
            {
                sectionNode[kvp.Key] = b;
            }
            else if (kvp.Value is int i)
            {
                sectionNode[kvp.Key] = i;
            }
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        var updatedJsonString = JsonSerializer.Serialize(rootObject, options);
        File.WriteAllText(_appSettingsPath, updatedJsonString);
    }
}
