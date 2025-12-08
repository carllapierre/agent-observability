namespace SimpleAgent.Core.ChatCompletion.Models;

/// <summary>
/// Result of a chat completion request.
/// </summary>
public record ChatCompletionResult(
    string? Content,
    ToolCall? ToolCall = null
);

/// <summary>
/// A tool call requested by the model.
/// </summary>
public record ToolCall(
    string Id,
    string Name,
    string Arguments
);

