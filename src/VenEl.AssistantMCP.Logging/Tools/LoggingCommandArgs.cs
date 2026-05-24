using System.ComponentModel;

namespace VenEl.AssistantMCP.Logging.Tools;

public class LoggingCommandArgs
{
    [Description("The action to perform. Options: get_server_logs")]
    public string Action { get; set; } = string.Empty;

    [Description("Number of lines to read from the end of the file. Default is 50, Max is 500. Used for get_server_logs.")]
    public int? Lines { get; set; }
}
