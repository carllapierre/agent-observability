namespace SimpleAgent.Core.ChatCompletion.Models;

/// <summary>
/// Role of the message in the conversation.
/// </summary>
public enum ChatRole
{
    System,
    User,
    Assistant,
    Tool
}

/// <summary>
/// A message in the chat conversation.
/// </summary>
public record ChatMessage(
    ChatRole Role,
    string Content,
    string? ToolCallId = null,
    string? ToolName = null,
    ToolCall? ToolCallRequest = null  // For assistant messages that request tool calls
);

