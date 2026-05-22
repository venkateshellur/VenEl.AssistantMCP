using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using VenEl.MCPAssistant.Core.Dispatcher;

namespace VenEl.MCPAssistant.GitHub.Tools;

[McpServerToolType]
public sealed class GitHubDispatcherTool : DispatcherToolBase<GitHubCommandArgs>
{
    public GitHubDispatcherTool(IServiceProvider serviceProvider) 
        : base(serviceProvider, "GitHub")
    {
    }

    protected override string? GetRequestedAction(GitHubCommandArgs args) => args.Action;

    [McpServerTool(Name = "mcp_venel_github_commands")]
    [Description("GitHub tools: projects, repositories, pull requests, diffs, GitHub Actions (workflows), and session credential setup.")]
    public Task<string> DispatchGitHubCommandAsync(
        [Description("The arguments for the GitHub command")] GitHubCommandArgs args,
        CancellationToken ct)
    {
        return DispatchAsync(args, ct);
    }
}
