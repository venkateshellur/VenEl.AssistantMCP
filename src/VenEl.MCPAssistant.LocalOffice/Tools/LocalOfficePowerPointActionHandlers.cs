using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;
using VenEl.MCPAssistant.Core.Dispatcher;
using VenEl.MCPAssistant.LocalOffice.Configuration;
using Microsoft.Extensions.Options;

namespace VenEl.MCPAssistant.LocalOffice.Tools;

public sealed class LocalReadPowerPointTextActionHandler : IActionHandler<LocalOfficeCommandArgs>
{
    public string ActionName => "local_read_powerpoint_text";

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
            using var presentationDoc = PresentationDocument.Open(args.FilePath!, false);
            var presentationPart = presentationDoc.PresentationPart;
            if (presentationPart == null) return Task.FromResult(string.Empty);

            var sb = new StringBuilder();
            int slideIndex = 1;

            foreach (var slidePart in presentationPart.SlideParts)
            {
                sb.AppendLine($"--- Slide {slideIndex++} ---");
                var texts = slidePart.Slide.Descendants<A.Text>()
                    .Select(t => t.Text)
                    .Where(txt => !string.IsNullOrWhiteSpace(txt));
                    
                foreach (var text in texts)
                {
                    sb.AppendLine(text);
                }
            }

            return Task.FromResult(sb.ToString());
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Error reading PowerPoint presentation: {ex.Message}");
        }
    }
}

public sealed class LocalCreatePowerPointPresentationActionHandler(IOptions<LocalOfficeOptions> options) : IActionHandler<LocalOfficeCommandArgs>
{
    public string ActionName => "local_create_powerpoint_presentation";

    public string? Validate(LocalOfficeCommandArgs args)
    {
        if (!options.Value.AllowFileOverwrite) return "File modification is disabled by safety switch.";
        if (string.IsNullOrWhiteSpace(args.FilePath)) return "Missing FilePath";
        return null;
    }

    public Task<string> HandleAsync(LocalOfficeCommandArgs args, CancellationToken ct)
    {
        try
        {
            CreateMinimalPowerPoint(args.FilePath!);
            return Task.FromResult($"Successfully created blank PowerPoint presentation {args.FilePath}");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Error creating PowerPoint presentation: {ex.Message}");
        }
    }

    private static void CreateMinimalPowerPoint(string filePath)
    {
        using var presentationDoc = PresentationDocument.Create(filePath, PresentationDocumentType.Presentation);
        
        var presentationPart = presentationDoc.AddPresentationPart();
        presentationPart.Presentation = new Presentation();
        
        var slideMasterPart = presentationPart.AddNewPart<SlideMasterPart>();
        slideMasterPart.SlideMaster = new SlideMaster(
            new CommonSlideData(new ShapeTree(
                new NonVisualGroupShapeProperties(
                    new NonVisualDrawingProperties { Id = 1, Name = "" },
                    new NonVisualGroupShapeDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()),
                new GroupShapeProperties(new A.TransformGroup()),
                new Shape(
                    new NonVisualShapeProperties(
                        new NonVisualDrawingProperties { Id = 2, Name = "Title" },
                        new NonVisualShapeDrawingProperties(new A.ShapeLocks { NoGrouping = true }),
                        new ApplicationNonVisualDrawingProperties(new PlaceholderShape { Type = PlaceholderValues.Title })),
                    new ShapeProperties(),
                    new TextBody(
                        new A.BodyProperties(),
                        new A.ListStyle(),
                        new A.Paragraph(new A.EndParagraphRunProperties()))))
            ),
            new ColorMap { Background1 = A.ColorSchemeIndexValues.Accent1, Text1 = A.ColorSchemeIndexValues.Dark1, Text2 = A.ColorSchemeIndexValues.Dark2 },
            new SlideLayoutIdList()
        );

        var slideLayoutPart = slideMasterPart.AddNewPart<SlideLayoutPart>();
        slideLayoutPart.SlideLayout = new SlideLayout(
            new CommonSlideData(new ShapeTree(
                new NonVisualGroupShapeProperties(
                    new NonVisualDrawingProperties { Id = 1, Name = "" },
                    new NonVisualGroupShapeDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()),
                new GroupShapeProperties(new A.TransformGroup()))
            )
        );
        
        slideMasterPart.SlideMaster.SlideLayoutIdList!.AppendChild(new SlideLayoutId
        {
            Id = 2147483649U,
            RelationshipId = slideMasterPart.GetIdOfPart(slideLayoutPart)
        });

        var slideIdList = new SlideIdList();
        presentationPart.Presentation.AppendChild(new SlideMasterIdList(new SlideMasterId { Id = 2147483648U, RelationshipId = presentationPart.GetIdOfPart(slideMasterPart) }));
        presentationPart.Presentation.AppendChild(slideIdList);
        
        presentationPart.Presentation.Save();
    }
}

public sealed class LocalAddPowerPointSlideActionHandler(IOptions<LocalOfficeOptions> options) : IActionHandler<LocalOfficeCommandArgs>
{
    public string ActionName => "local_add_powerpoint_slide";

    public string? Validate(LocalOfficeCommandArgs args)
    {
        if (!options.Value.AllowFileOverwrite) return "File modification is disabled by safety switch.";
        if (string.IsNullOrWhiteSpace(args.FilePath)) return "Missing FilePath";
        if (!File.Exists(args.FilePath)) return $"File not found: {args.FilePath}";
        return null;
    }

    public Task<string> HandleAsync(LocalOfficeCommandArgs args, CancellationToken ct)
    {
        try
        {
            using var presentationDoc = PresentationDocument.Open(args.FilePath!, true);
            var presentationPart = presentationDoc.PresentationPart;
            if (presentationPart == null)
            {
                return Task.FromResult("Error: Invalid presentation document part.");
            }

            var slidePart = presentationPart.AddNewPart<SlidePart>();
            var slideLayoutPart = presentationPart.SlideMasterParts
                .SelectMany(m => m.SlideLayoutParts)
                .FirstOrDefault();
                
            if (slideLayoutPart != null)
            {
                slidePart.AddPart(slideLayoutPart);
            }
            
            var shapeTree = new ShapeTree(
                new NonVisualGroupShapeProperties(
                    new NonVisualDrawingProperties { Id = 1, Name = "" },
                    new NonVisualGroupShapeDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()),
                new GroupShapeProperties(new A.TransformGroup())
            );
            
            int shapeId = 2;
            if (!string.IsNullOrEmpty(args.Title))
            {
                var titleShape = CreateTextShape(shapeId++, args.Title, true);
                shapeTree.AppendChild(titleShape);
            }
            
            if (!string.IsNullOrEmpty(args.Content))
            {
                var bodyShape = CreateTextShape(shapeId++, args.Content, false);
                shapeTree.AppendChild(bodyShape);
            }
            
            slidePart.Slide = new Slide(new CommonSlideData(shapeTree));
            slidePart.Slide.Save();
            
            var slideIdList = presentationPart.Presentation.SlideIdList;
            if (slideIdList == null)
            {
                slideIdList = new SlideIdList();
                presentationPart.Presentation.AppendChild(slideIdList);
            }
            
            uint maxId = 255;
            if (slideIdList.ChildElements.Any())
            {
                maxId = slideIdList.ChildElements.Cast<SlideId>().Max(s => s.Id?.Value ?? 255U);
            }
            
            var slideId = new SlideId
            {
                Id = maxId + 1,
                RelationshipId = presentationPart.GetIdOfPart(slidePart)
            };
            slideIdList.AppendChild(slideId);
            
            presentationPart.Presentation.Save();
            return Task.FromResult($"Successfully added slide with title '{args.Title}' to {args.FilePath}");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Error adding slide to PowerPoint: {ex.Message}");
        }
    }

    private static Shape CreateTextShape(int id, string text, bool isTitle)
    {
        var textBody = new TextBody(
            new A.BodyProperties(),
            new A.ListStyle()
        );

        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        foreach (var line in lines)
        {
            textBody.AppendChild(
                new A.Paragraph(
                    new A.Run(
                        new A.RunProperties { Language = "en-US" },
                        new A.Text(line)
                    )
                )
            );
        }

        var shape = new Shape(
            new NonVisualShapeProperties(
                new NonVisualDrawingProperties { Id = (uint)id, Name = isTitle ? "Title" : "Content" },
                new NonVisualShapeDrawingProperties(new A.ShapeLocks { NoGrouping = true }),
                new ApplicationNonVisualDrawingProperties(new PlaceholderShape { Type = isTitle ? PlaceholderValues.Title : PlaceholderValues.Body })
            ),
            new ShapeProperties(
                new A.Transform2D(
                    new A.Offset { X = isTitle ? 500000L : 500000L, Y = isTitle ? 500000L : 1500000L },
                    new A.Extents { Cx = 8000000L, Cy = isTitle ? 1000000L : 5000000L }
                ),
                new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }
            ),
            textBody
        );
        return shape;
    }
}

public sealed class LocalReplacePowerPointPlaceholderActionHandler(IOptions<LocalOfficeOptions> options) : IActionHandler<LocalOfficeCommandArgs>
{
    public string ActionName => "local_replace_powerpoint_placeholder";

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
            using var presentationDoc = PresentationDocument.Open(args.FilePath!, true);
            var presentationPart = presentationDoc.PresentationPart;
            if (presentationPart == null)
            {
                return Task.FromResult("Error: Invalid presentation structure.");
            }

            using var jsonDoc = JsonDocument.Parse(args.JsonData!);
            var root = jsonDoc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return Task.FromResult("Error: JsonData must be a JSON object containing key-value pairs of placeholders and replacements.");
            }

            int replacedCount = 0;
            foreach (var slidePart in presentationPart.SlideParts)
            {
                var slide = slidePart.Slide;
                if (slide == null) continue;

                var xml = slide.InnerXml;
                bool replacedInSlide = false;
                foreach (var prop in root.EnumerateObject())
                {
                    var target = prop.Name;
                    var replacement = prop.Value.GetString() ?? string.Empty;
                    if (xml.Contains(target))
                    {
                        xml = xml.Replace(target, replacement);
                        replacedInSlide = true;
                    }
                }

                if (replacedInSlide)
                {
                    slide.InnerXml = xml;
                    slide.Save();
                    replacedCount++;
                }
            }
            presentationPart.Presentation.Save();

            return Task.FromResult($"Successfully completed find-and-replace in {replacedCount} slides in {args.FilePath}");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Error replacing placeholders: {ex.Message}");
        }
    }
}
