namespace SimpleAgent.Core.Prompts.Interfaces;

/// <summary>
/// Interface for providing system prompts to the agent.
/// </summary>
public interface IPromptProvider
{
    /// <summary>
    /// Gets the system prompt for the agent.
    /// </summary>
    /// <returns>The system prompt, or null if not available</returns>
    string? GetSystemPrompt();
}

