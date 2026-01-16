using AgentCore.ChatCompletion.Models;

namespace AgentCore.ChatCompletion.Extensions;

/// <summary>
/// Extension methods for ChatMessage conversions.
/// </summary>
public static class ChatMessageExtensions
{
    /// <summary>
    /// Converts messages to a text-friendly format (no tool calling).
    /// Tool messages become assistant messages with labeled content.
    /// Useful for reasoning nodes that don't use tool calling.
    /// </summary>
    public static IEnumerable<ChatMessage> ToTextFormat(this IEnumerable<ChatMessage> messages)
    {
        foreach (var msg in messages)
        {
            yield return msg.ToTextFormat();
        }
    }

    /// <summary>
    /// Converts a single message to text-friendly format.
    /// </summary>
    public static ChatMessage ToTextFormat(this ChatMessage msg)
    {
        // Tool result messages → convert to assistant message with text
        if (msg.Role == ChatRole.Tool)
        {
            return new ChatMessage(
                ChatRole.Assistant,
                $"[Tool Result: {msg.ToolName}]\n{msg.Content}");
        }

        // Assistant messages with tool calls → convert to text description
        if (msg.Role == ChatRole.Assistant && msg.ToolCallRequests is { Count: > 0 })
        {
            var toolCallsText = string.Join("\n", msg.ToolCallRequests.Select(tc =>
                $"- {tc.Name}({tc.Arguments})"));
            return new ChatMessage(
                ChatRole.Assistant,
                $"[Tool Calls]\n{toolCallsText}");
        }

        // Other messages pass through unchanged
        return msg;
    }
}
