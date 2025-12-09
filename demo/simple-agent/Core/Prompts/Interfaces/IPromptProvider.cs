namespace SimpleAgent.Core.Prompts.Interfaces;

/// <summary>
/// Interface for providing prompts to the agent.
/// </summary>
public interface IPromptProvider
{
    /// <summary>
    /// Gets a prompt by key.
    /// </summary>
    /// <param name="key">The prompt key/name</param>
    /// <param name="label">Optional label (e.g., "production", "staging")</param>
    /// <param name="version">Optional specific version number</param>
    /// <returns>The prompt content, or null if not available</returns>
    string? GetPrompt(string key, string? label = null, int? version = null);
}

