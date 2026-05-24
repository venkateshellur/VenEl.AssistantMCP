using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace VenEl.AssistantMCP.Core.Dispatcher;

/// <summary>
/// Base class for the Domain-level MCP Tool (the Facade).
/// This intercepts requests from the MCP client, locates the requested action handler,
/// performs validation, and returns either the result or a helpful error for the LLM to self-correct.
/// </summary>
public abstract class DispatcherToolBase<TArgs> where TArgs : class
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string _domainName;

    protected DispatcherToolBase(IServiceProvider serviceProvider, string domainName)
    {
        _serviceProvider = serviceProvider;
        _domainName = domainName;
    }

    /// <summary>
    /// Implementers must extract the "Action" string from their specific TArgs object.
    /// </summary>
    protected abstract string? GetRequestedAction(TArgs args);

    /// <summary>
    /// Core routing logic to be called by the `[McpServerTool]` method.
    /// </summary>
    protected async Task<string> DispatchAsync(TArgs args, CancellationToken ct)
    {
        var actionName = GetRequestedAction(args);
        
        if (string.IsNullOrWhiteSpace(actionName))
        {
            return $"Error: You must provide a valid 'Action' parameter for the {_domainName} domain.";
        }

        // Lazy load all handlers for this domain
        var handlers = _serviceProvider.GetServices<IActionHandler<TArgs>>();
        
        var handler = handlers.FirstOrDefault(h => string.Equals(h.ActionName, actionName, StringComparison.OrdinalIgnoreCase));

        if (handler == null)
        {
            var available = string.Join(", ", handlers.Select(h => $"'{h.ActionName}'"));
            return $"Error: Unknown action '{actionName}' in the {_domainName} domain. Available actions are: {available}.";
        }

        // Validate LLM arguments specifically for this handler
        var validationError = handler.Validate(args);
        if (!string.IsNullOrWhiteSpace(validationError))
        {
            // The magic self-correction loop: Return the validation error politely
            return $"Error: The action '{handler.ActionName}' requires missing or different parameters. " +
                   $"Validation failed: {validationError}. " +
                   $"Please retry the tool call with the corrected parameters.";
        }

        try
        {
            // Execute business logic
            return await handler.HandleAsync(args, ct);
        }
        catch (Exception ex)
        {
            return $"Error executing '{handler.ActionName}': {ex.Message}";
        }
    }
}
