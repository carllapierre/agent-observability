namespace SimpleAgent.CLI;

/// <summary>
/// Interface for a chat agent that processes user input and generates responses.
/// </summary>
public interface IChatAgent
{
    /// <summary>
    /// Processes user input and returns an agent response.
    /// </summary>
    /// <param name="userInput">The message from the user</param>
    /// <returns>The agent's response</returns>
    Task<string> GetResponseAsync(string userInput);
}

