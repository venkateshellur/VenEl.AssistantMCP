using System.Threading;
using System.Threading.Tasks;

namespace VenEl.AssistantMCP.Core.Dispatcher;

/// <summary>
/// Represents a single fine-grained operation within a larger domain (e.g., "list_projects" inside "azure_commands").
/// </summary>
/// <typeparam name="TArgs">The domain-wide argument schema class containing all optional fields.</typeparam>
public interface IActionHandler<TArgs> where TArgs : class
{
    /// <summary>
    /// The unique identifier for this action (e.g., "create_issue").
    /// This must match the string the LLM passes into the `Action` parameter.
    /// </summary>
    string ActionName { get; }

    /// <summary>
    /// Validates that the provided arguments contain all required fields for this specific action.
    /// Returns an error message if invalid, or null if valid.
    /// </summary>
    string? Validate(TArgs args);

    /// <summary>
    /// Executes the action logic.
    /// </summary>
    Task<string> HandleAsync(TArgs args, CancellationToken ct);
}
