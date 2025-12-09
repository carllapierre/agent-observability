namespace SimpleAgent.Core.ChatCompletion.Models;

/// <summary>
/// Result of a chat completion request.
/// </summary>
public record ChatCompletionResult(
    string? Content,
    IReadOnlyList<ToolCall>? ToolCalls = null
)
{
    /// <summary>
    /// Gets whether this result contains tool calls.
    /// </summary>
    public bool HasToolCalls => ToolCalls is { Count: > 0 };
}

/// <summary>
/// A tool call requested by the model.
/// </summary>
public record ToolCall(
    string Id,
    string Name,
    string Arguments
);
