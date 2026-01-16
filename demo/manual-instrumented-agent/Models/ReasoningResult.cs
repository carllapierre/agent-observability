namespace SimpleAgent.Models;

/// <summary>
/// Route decision from the reasoning node.
/// </summary>
public enum Route
{
    /// <summary>Use tools to gather information</summary>
    Tool,
    /// <summary>Answer directly without tools</summary>
    Answer
}

/// <summary>
/// Structured output from the reasoning node.
/// </summary>
public record ReasoningResult(
    string Reasoning,
    Route Route
);
