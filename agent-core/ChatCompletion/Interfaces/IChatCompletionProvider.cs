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

    /// <summary>
    /// Sends messages and returns a structured response of type T.
    /// Uses structured outputs for guaranteed schema compliance.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response into</typeparam>
    /// <param name="messages">The conversation history</param>
    /// <param name="schemaName">Name for the JSON schema</param>
    /// <returns>The deserialized response object</returns>
    Task<T> CompleteAsync<T>(
        IReadOnlyList<ChatMessage> messages,
        string schemaName) where T : class;
}

