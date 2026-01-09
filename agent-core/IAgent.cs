namespace AgentCore;

/// <summary>
/// Interface for an agent that processes user input and generates responses.
/// </summary>
public interface IAgent
{
    /// <summary>
    /// Processes user input and returns an agent response.
    /// </summary>
    /// <param name="userInput">The message from the user</param>
    /// <returns>The agent's response including content and telemetry metadata</returns>
    Task<AgentResponse> GetResponseAsync(string userInput);
}
