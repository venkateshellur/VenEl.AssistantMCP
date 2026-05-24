using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using VenEl.AssistantMCP.Core.Dispatcher;

namespace VenEl.AssistantMCP.LocalOffice.Tools;

[McpServerToolType]
public sealed class LocalOfficeDispatcherTool : DispatcherToolBase<LocalOfficeCommandArgs>
{
    public LocalOfficeDispatcherTool(IServiceProvider serviceProvider) 
        : base(serviceProvider, "LocalOffice")
    {
    }

    protected override string? GetRequestedAction(LocalOfficeCommandArgs args) => args.Action;

    [McpServerTool(Name = "mcp_venel_localoffice_commands")]
    [Description("Local Office Management tools: Read and write to local Excel (.xlsx), Word (.docx), and PowerPoint (.pptx) files using OpenXML. Supported actions: local_read_excel_cell, local_write_excel_cell, local_write_excel_table, local_read_excel_table, local_list_excel_sheets, local_create_excel_sheet, local_clear_excel_sheet, local_delete_excel_sheet, local_read_word_text, local_write_word_text, local_replace_word_placeholder, local_read_powerpoint_text, local_create_powerpoint_presentation, local_add_powerpoint_slide, local_replace_powerpoint_placeholder.")]
    public Task<string> DispatchLocalOfficeCommandAsync(
        [Description("The arguments for the Local Office command")] LocalOfficeCommandArgs args,
        CancellationToken ct)
    {
        return DispatchAsync(args, ct);
    }
}
