using AgentCore.ChatCompletion.Models;
using AgentCore.Tools.Models;

namespace AgentCore.ChatCompletion.Interfaces;

/// <summary>
/// Interface for chat completion providers.
/// Providers translate the common message format to their specific API.
/// </summary>
public interface IChatCompletionProvider
{
    /// <summary>
    /// Sends messages to the model and returns a completion result.
    /// </summary>
    /// <param name="messages">The conversation history</param>
    /// <param name="tools">Optional list of tools the model can call</param>
    /// <returns>The completion result, which may include content or a tool call</returns>
    Task<ChatCompletionResult> CompleteAsync(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<ToolDescriptor>? tools = null
    );
}

