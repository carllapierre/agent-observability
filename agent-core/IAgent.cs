using AgentCore.ChatCompletion.Models;

namespace AgentCore;

/// <summary>
/// Interface for an agent that processes user input and generates responses.
/// </summary>
public interface IAgent
{
    /// <summary>
    /// Processes chat history and returns an agent response.
    /// The agent is stateless - full conversation history must be provided on each call.
    /// </summary>
    /// <param name="history">The conversation history (user and assistant messages)</param>
    /// <returns>The agent's response including content and telemetry metadata</returns>
    Task<AgentResponse> GetResponseAsync(IReadOnlyList<ChatMessage> history);

    /// <summary>
    /// Submits user feedback for a specific trace.
    /// </summary>
    /// <param name="traceId">The trace ID to attach feedback to</param>
    /// <param name="comment">The user's feedback comment</param>
    Task SubmitFeedbackAsync(string traceId, string comment);
}
