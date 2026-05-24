using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using VenEl.AssistantMCP.Core.Dispatcher;
using VenEl.AssistantMCP.LocalOffice.Configuration;
using Microsoft.Extensions.Options;

namespace VenEl.AssistantMCP.LocalOffice.Tools;

public sealed class LocalReadWordTextActionHandler : IActionHandler<LocalOfficeCommandArgs>
{
    public string ActionName => "local_read_word_text";

    public string? Validate(LocalOfficeCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.FilePath)) return "Missing FilePath";
        if (!File.Exists(args.FilePath)) return $"File not found: {args.FilePath}";
        return null;
    }

    public Task<string> HandleAsync(LocalOfficeCommandArgs args, CancellationToken ct)
    {
        try
        {
            using var doc = WordprocessingDocument.Open(args.FilePath!, false);
            var body = doc.MainDocumentPart?.Document.Body;
            if (body == null) return Task.FromResult(string.Empty);

            var paragraphs = body.Descendants<Paragraph>()
                .Select(p => p.InnerText)
                .Where(t => !string.IsNullOrEmpty(t));
                
            return Task.FromResult(string.Join(Environment.NewLine, paragraphs));
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Error reading Word document: {ex.Message}");
        }
    }
}

public sealed class LocalWriteWordTextActionHandler(IOptions<LocalOfficeOptions> options) : IActionHandler<LocalOfficeCommandArgs>
{
    public string ActionName => "local_write_word_text";

    public string? Validate(LocalOfficeCommandArgs args)
    {
        if (!options.Value.AllowFileOverwrite) return "File modification is disabled by safety switch.";
        if (string.IsNullOrWhiteSpace(args.FilePath)) return "Missing FilePath";
        if (string.IsNullOrWhiteSpace(args.Content)) return "Missing Content";
        return null;
    }

    public Task<string> HandleAsync(LocalOfficeCommandArgs args, CancellationToken ct)
    {
        try
        {
            var content = args.Content!;
            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            if (args.ClearExisting == true || !File.Exists(args.FilePath))
            {
                using var doc = WordprocessingDocument.Create(args.FilePath!, WordprocessingDocumentType.Document);
                var mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = new Body();
                
                foreach (var line in lines)
                {
                    body.AppendChild(new Paragraph(new Run(new Text(line))));
                }
                
                mainPart.Document.AppendChild(body);
                doc.Save();
            }
            else
            {
                using var doc = WordprocessingDocument.Open(args.FilePath!, true);
                var body = doc.MainDocumentPart?.Document.Body;
                if (body == null)
                {
                    return Task.FromResult("Error: Document has no body structure.");
                }

                foreach (var line in lines)
                {
                    body.AppendChild(new Paragraph(new Run(new Text(line))));
                }
                
                doc.Save();
            }

            return Task.FromResult($"Successfully wrote content to Word document {args.FilePath}");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Error writing Word document: {ex.Message}");
        }
    }
}

public sealed class LocalReplaceWordPlaceholderActionHandler(IOptions<LocalOfficeOptions> options) : IActionHandler<LocalOfficeCommandArgs>
{
    public string ActionName => "local_replace_word_placeholder";

    public string? Validate(LocalOfficeCommandArgs args)
    {
        if (!options.Value.AllowFileOverwrite) return "File modification is disabled by safety switch.";
        if (string.IsNullOrWhiteSpace(args.FilePath)) return "Missing FilePath";
        if (!File.Exists(args.FilePath)) return $"File not found: {args.FilePath}";
        if (string.IsNullOrWhiteSpace(args.JsonData)) return "Missing JsonData";
        return null;
    }

    public Task<string> HandleAsync(LocalOfficeCommandArgs args, CancellationToken ct)
    {
        try
        {
            using var doc = WordprocessingDocument.Open(args.FilePath!, true);
            var body = doc.MainDocumentPart?.Document.Body;
            if (body == null)
            {
                return Task.FromResult("Error: Document body not found.");
            }

            using var jsonDoc = JsonDocument.Parse(args.JsonData!);
            var root = jsonDoc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return Task.FromResult("Error: JsonData must be a JSON object containing key-value pairs of placeholders and replacements.");
            }

            // Word often splits placeholders like "{{placeholder}}" across multiple runs in XML.
            // We can replace on the inner XML level of the body.
            var xml = body.InnerXml;
            int replacedCount = 0;
            foreach (var prop in root.EnumerateObject())
            {
                var target = prop.Name;
                var replacement = prop.Value.GetString() ?? string.Empty;
                if (xml.Contains(target))
                {
                    xml = xml.Replace(target, replacement);
                    replacedCount++;
                }
            }

            body.InnerXml = xml;
            doc.Save();

            return Task.FromResult($"Successfully completed find-and-replace for {replacedCount} placeholders in {args.FilePath}");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Error replacing placeholders: {ex.Message}");
        }
    }
}
