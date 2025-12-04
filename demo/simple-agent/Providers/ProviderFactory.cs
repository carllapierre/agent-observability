using SimpleAgent.Configuration;
using SimpleAgent.Providers.Google;
using SimpleAgent.Providers.OpenAI;

namespace SimpleAgent.Providers;

/// <summary>
/// Factory for creating provider clients based on configuration.
/// </summary>
public static class ProviderFactory
{
    public static IProviderClient Create(AppConfiguration config)
    {
        return config.Provider switch
        {
            ProviderType.OpenAI => new OpenAIClient(config.OpenAI.ApiKey, config.OpenAI.Model),
            ProviderType.Google => new GoogleClient(config.Google.ApiKey, config.Google.Model),
            _ => throw new ArgumentException($"Unsupported provider: {config.Provider}")
        };
    }
}
