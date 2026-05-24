using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using VenEl.MCPAssistant.Core.Dispatcher;
using VenEl.MCPAssistant.LocalOffice.Configuration;
using Microsoft.Extensions.Options;

namespace VenEl.MCPAssistant.LocalOffice.Tools;

public sealed class LocalReadExcelCellActionHandler : IActionHandler<LocalOfficeCommandArgs>
{
    public string ActionName => "local_read_excel_cell";

    public string? Validate(LocalOfficeCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.FilePath)) return "Missing FilePath";
        if (string.IsNullOrWhiteSpace(args.CellAddress)) return "Missing CellAddress";
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
                
            var cell = sheet.Cell(args.CellAddress!);
            return Task.FromResult(cell.Value.ToString() ?? string.Empty);
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Error reading cell: {ex.Message}");
        }
    }
}

public sealed class LocalWriteExcelCellActionHandler(IOptions<LocalOfficeOptions> options) : IActionHandler<LocalOfficeCommandArgs>
{
    public string ActionName => "local_write_excel_cell";

    public string? Validate(LocalOfficeCommandArgs args)
    {
        if (!options.Value.AllowFileOverwrite) return "File modification is disabled by safety switch.";
        if (string.IsNullOrWhiteSpace(args.FilePath)) return "Missing FilePath";
        if (string.IsNullOrWhiteSpace(args.CellAddress)) return "Missing CellAddress";
        if (args.Value == null) return "Missing Value";
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

            sheet.Cell(args.CellAddress!).Value = args.Value;
            workbook.SaveAs(args.FilePath);

            return Task.FromResult($"Successfully wrote '{args.Value}' to cell {args.CellAddress} in {args.FilePath}");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Error writing cell: {ex.Message}");
        }
    }
}
