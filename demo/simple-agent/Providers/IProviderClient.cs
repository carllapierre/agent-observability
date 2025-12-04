namespace SimpleAgent.Providers;

/// <summary>
/// Interface for AI provider clients.
/// </summary>
public interface IProviderClient
{
    Task<string> GetResponseAsync(string userInput);
}

