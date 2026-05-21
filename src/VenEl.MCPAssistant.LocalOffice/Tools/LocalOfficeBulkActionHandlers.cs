using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using VenEl.MCPAssistant.Core.Dispatcher;

namespace VenEl.MCPAssistant.LocalOffice.Tools;

public sealed class LocalWriteExcelTableActionHandler : IActionHandler<LocalOfficeCommandArgs>
{
    public string ActionName => "local_write_excel_table";

    public string? Validate(LocalOfficeCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.FilePath)) return "Missing FilePath";
        if (string.IsNullOrWhiteSpace(args.JsonData)) return "Missing JsonData";
        return null;
    }

    public Task<string> HandleAsync(LocalOfficeCommandArgs args, CancellationToken ct)
    {
        try
        {
            using var workbook = File.Exists(args.FilePath) ? new XLWorkbook(args.FilePath) : new XLWorkbook();
            
            IXLWorksheet sheet;
            if (!File.Exists(args.FilePath) && workbook.Worksheets.Count == 0)
            {
                var defaultSheetName = string.IsNullOrWhiteSpace(args.SheetName) ? "Sheet1" : args.SheetName;
                sheet = workbook.Worksheets.Add(defaultSheetName);
            }
            else
            {
                sheet = string.IsNullOrWhiteSpace(args.SheetName) 
                    ? workbook.Worksheet(1) 
                    : workbook.Worksheet(args.SheetName);
            }

            if (args.ClearExisting == true)
            {
                sheet.Clear();
            }

            var cellAddress = string.IsNullOrWhiteSpace(args.CellAddress) ? "A1" : args.CellAddress;
            var startCell = sheet.Cell(cellAddress);

            using var doc = JsonDocument.Parse(args.JsonData!);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Array)
            {
                return Task.FromResult("Error: JsonData must be a JSON array of objects or an array of arrays.");
            }

            int currentRowOffset = 0;
            bool headersWritten = false;

            foreach (var rowElement in root.EnumerateArray())
            {
                int currentColOffset = 0;

                if (rowElement.ValueKind == JsonValueKind.Object)
                {
                    if (args.IncludeHeaders == true && !headersWritten)
                    {
                        foreach (var prop in rowElement.EnumerateObject())
                        {
                            startCell.CellBelow(currentRowOffset).CellRight(currentColOffset).Value = prop.Name;
                            currentColOffset++;
                        }
                        currentRowOffset++;
                        currentColOffset = 0;
                        headersWritten = true;
                    }

                    foreach (var prop in rowElement.EnumerateObject())
                    {
                        startCell.CellBelow(currentRowOffset).CellRight(currentColOffset).Value = prop.Value.ToString();
                        currentColOffset++;
                    }
                }
                else if (rowElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var cellElement in rowElement.EnumerateArray())
                    {
                        startCell.CellBelow(currentRowOffset).CellRight(currentColOffset).Value = cellElement.ToString();
                        currentColOffset++;
                    }
                }
                else
                {
                    startCell.CellBelow(currentRowOffset).Value = rowElement.ToString();
                }

                currentRowOffset++;
            }

            workbook.SaveAs(args.FilePath);

            return Task.FromResult($"Successfully wrote {currentRowOffset - (headersWritten ? 1 : 0)} rows to {args.FilePath} starting at {cellAddress}");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Error writing table: {ex.Message}");
        }
    }
}

public sealed class LocalReadExcelTableActionHandler : IActionHandler<LocalOfficeCommandArgs>
{
    public string ActionName => "local_read_excel_table";

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
            using var workbook = new XLWorkbook(args.FilePath);
            var sheet = string.IsNullOrWhiteSpace(args.SheetName) 
                ? workbook.Worksheet(1) 
                : workbook.Worksheet(args.SheetName);

            IXLRange? range = sheet.RangeUsed();
            if (range == null) return Task.FromResult("[]");

            var rows = range.RowsUsed().ToList();
            if (!rows.Any()) return Task.FromResult("[]");

            var result = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, string>>();
            
            if (args.IncludeHeaders == true)
            {
                var headerRow = rows.First();
                var headers = headerRow.Cells().Select(c => c.Value.ToString()).ToList();
                
                foreach (var row in rows.Skip(1))
                {
                    var dict = new System.Collections.Generic.Dictionary<string, string>();
                    int colIndex = 0;
                    foreach (var cell in row.Cells())
                    {
                        var header = colIndex < headers.Count ? headers[colIndex] : $"Column{colIndex+1}";
                        dict[header] = cell.Value.ToString() ?? string.Empty;
                        colIndex++;
                    }
                    result.Add(dict);
                }
            }
            else
            {
                foreach (var row in rows)
                {
                    var dict = new System.Collections.Generic.Dictionary<string, string>();
                    int colIndex = 1;
                    foreach (var cell in row.Cells())
                    {
                        dict[$"Column{colIndex}"] = cell.Value.ToString() ?? string.Empty;
                        colIndex++;
                    }
                    result.Add(dict);
                }
            }

            return Task.FromResult(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Error reading table: {ex.Message}");
        }
    }
}

public sealed class LocalListExcelSheetsActionHandler : IActionHandler<LocalOfficeCommandArgs>
{
    public string ActionName => "local_list_excel_sheets";

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
            using var workbook = new XLWorkbook(args.FilePath);
            var names = workbook.Worksheets.Select(ws => ws.Name).ToList();
            return Task.FromResult(JsonSerializer.Serialize(names));
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Error listing sheets: {ex.Message}");
        }
    }
}

public sealed class LocalCreateExcelSheetActionHandler : IActionHandler<LocalOfficeCommandArgs>
{
    public string ActionName => "local_create_excel_sheet";

    public string? Validate(LocalOfficeCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.FilePath)) return "Missing FilePath";
        if (string.IsNullOrWhiteSpace(args.NewSheetName)) return "Missing NewSheetName";
        return null;
    }

    public Task<string> HandleAsync(LocalOfficeCommandArgs args, CancellationToken ct)
    {
        try
        {
            using var workbook = File.Exists(args.FilePath) ? new XLWorkbook(args.FilePath) : new XLWorkbook();
            workbook.Worksheets.Add(args.NewSheetName!);
            workbook.SaveAs(args.FilePath);
            return Task.FromResult($"Successfully created sheet '{args.NewSheetName}' in {args.FilePath}");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Error creating sheet: {ex.Message}");
        }
    }
}

public sealed class LocalClearExcelSheetActionHandler : IActionHandler<LocalOfficeCommandArgs>
{
    public string ActionName => "local_clear_excel_sheet";

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
            using var workbook = new XLWorkbook(args.FilePath);
            var sheet = string.IsNullOrWhiteSpace(args.SheetName) 
                ? workbook.Worksheet(1) 
                : workbook.Worksheet(args.SheetName);
            
            sheet.Clear();
            workbook.SaveAs(args.FilePath);
            return Task.FromResult($"Successfully cleared sheet '{sheet.Name}' in {args.FilePath}");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Error clearing sheet: {ex.Message}");
        }
    }
}

public sealed class LocalDeleteExcelSheetActionHandler : IActionHandler<LocalOfficeCommandArgs>
{
    public string ActionName => "local_delete_excel_sheet";

    public string? Validate(LocalOfficeCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.FilePath)) return "Missing FilePath";
        if (!File.Exists(args.FilePath)) return $"File not found: {args.FilePath}";
        if (string.IsNullOrWhiteSpace(args.SheetName)) return "Missing SheetName";
        return null;
    }

    public Task<string> HandleAsync(LocalOfficeCommandArgs args, CancellationToken ct)
    {
        try
        {
            using var workbook = new XLWorkbook(args.FilePath);
            workbook.Worksheets.Delete(args.SheetName!);
            workbook.SaveAs(args.FilePath);
            return Task.FromResult($"Successfully deleted sheet '{args.SheetName}' in {args.FilePath}");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Error deleting sheet: {ex.Message}");
        }
    }
}
