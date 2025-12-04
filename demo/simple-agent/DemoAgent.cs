using AgentCLI;
using SimpleAgent.Providers;

namespace SimpleAgent;

/// <summary>
/// Demo chat agent implementation.
/// Provider-agnostic - receives any IProviderClient via dependency injection.
/// </summary>
public class DemoAgent : IChatAgent
{
    private readonly IProviderClient _client;

    public DemoAgent(IProviderClient client)
    {
        _client = client;
    }

    public Task<string> GetResponseAsync(string userInput) => _client.GetResponseAsync(userInput);
}
